import json
import logging
from pathlib import Path
import re

logger = logging.getLogger(__name__)

INTENT_LABELS = [
    "exam_and_grading",
    "course_registration",
    "digital_systems",
    "student_services",
    "library",
    "student_life",
    "scholarship_support",
]

MODEL_DIR = Path(__file__).parent.parent / "models" / "intent_classifier"
ENTITY_PATH = Path(__file__).parent.parent / "data" / "entity_dictionary.json"
MODEL_META_PATH = MODEL_DIR / "metrics.json"
MIN_MODEL_ACCURACY = 0.60
LOW_CONFIDENCE_THRESHOLD = 0.60
LOW_MARGIN_THRESHOLD = 0.15


class IntentService:
    """Intent classifier + entity extractor with safe keyword fallback."""

    def __init__(self):
        self.model = None
        self.tokenizer = None
        self.is_loaded = False
        self.model_name = "keyword-fallback"
        self.entity_patterns: list[dict] = []
        self._load_entity_dictionary()
        self._try_load_model()

    def _load_entity_dictionary(self):
        if not ENTITY_PATH.exists():
            logger.info("Entity dictionary not found at %s", ENTITY_PATH)
            return

        try:
            with open(ENTITY_PATH, "r", encoding="utf-8-sig") as f:
                data = json.load(f)

            patterns: list[dict] = []
            for items in data.get("groups", {}).values():
                for item in items:
                    for alias in item.get("aliases", []):
                        patterns.append(
                            {
                                "alias": alias.lower(),
                                "value": item["value"],
                                "type": item["type"],
                            }
                        )

            patterns.sort(key=lambda item: len(item["alias"]), reverse=True)
            self.entity_patterns = patterns
            logger.info("Entity dictionary loaded with %d aliases", len(self.entity_patterns))
        except Exception as e:
            logger.warning("Failed to load entity dictionary: %s", e)

    def _try_load_model(self):
        if not MODEL_DIR.exists() or not any(MODEL_DIR.iterdir()):
            logger.info("No fine-tuned model found, using keyword fallback")
            return

        if MODEL_META_PATH.exists():
            try:
                with open(MODEL_META_PATH, "r", encoding="utf-8-sig") as f:
                    metadata = json.load(f)
                best_accuracy = float(metadata.get("best_validation_accuracy", 0.0))
                if best_accuracy < MIN_MODEL_ACCURACY:
                    logger.warning(
                        "Fine-tuned model accuracy %.4f is below threshold %.2f; keeping keyword fallback",
                        best_accuracy,
                        MIN_MODEL_ACCURACY,
                    )
                    return
            except Exception as e:
                logger.warning("Failed to read model metadata: %s", e)
                return
        else:
            logger.info("Model metadata not found, keeping keyword fallback for safety")
            return

        try:
            from transformers import AutoModelForSequenceClassification, AutoTokenizer

            self.tokenizer = AutoTokenizer.from_pretrained(str(MODEL_DIR))
            self.model = AutoModelForSequenceClassification.from_pretrained(str(MODEL_DIR))
            self.model.eval()
            self.is_loaded = True
            self.model_name = "berturk-finetuned"
            logger.info("BERTurk intent model loaded successfully")
        except Exception as e:
            logger.warning("Failed to load BERTurk model: %s", e)

    def classify(self, text: str) -> dict:
        if self.is_loaded and self.model is not None:
            return self._classify_bert(text)
        return self._classify_keyword(text)

    def _confidence_band(self, confidence: float) -> str:
        if confidence >= 0.85:
            return "high"
        if confidence >= 0.60:
            return "medium"
        return "low"

    def _classify_bert(self, text: str) -> dict:
        import torch

        inputs = self.tokenizer(
            text, return_tensors="pt", truncation=True, max_length=128, padding=True
        )
        with torch.no_grad():
            outputs = self.model(**inputs)
            probs = torch.softmax(outputs.logits, dim=-1)
            confidence, predicted = torch.max(probs, dim=-1)
            topk_values, topk_indices = torch.topk(probs, k=min(2, probs.shape[-1]), dim=-1)

        idx = predicted.item()
        if idx >= len(INTENT_LABELS):
            logger.warning(
                "Model output index %d exceeds label count %d, falling back to keyword",
                idx,
                len(INTENT_LABELS),
            )
            return self._classify_keyword(text)

        intent = INTENT_LABELS[idx]
        entities = self._extract_entities(text)
        confidence_value = round(confidence.item(), 4)
        margin = (
            round((topk_values[0][0] - topk_values[0][1]).item(), 4)
            if topk_values.shape[-1] > 1
            else 1.0
        )
        resolved_intent = self._resolve_ambiguous_intent(text, intent, confidence_value, margin)
        model_used = "berturk-finetuned"
        resolver_used = False
        if resolved_intent != intent:
            intent = resolved_intent
            confidence_value = round(max(confidence_value - 0.05, 0.0), 4)
            model_used = "berturk-finetuned+resolver"
            resolver_used = True
        elif confidence_value < LOW_CONFIDENCE_THRESHOLD or margin < LOW_MARGIN_THRESHOLD:
            keyword_result = self._classify_keyword(text)
            if keyword_result["intent"] != intent:
                intent = keyword_result["intent"]
                confidence_value = round(max(keyword_result["confidence"], confidence_value - 0.08), 4)
                model_used = "hybrid-fallback"
                resolver_used = True

        confidence_band = self._confidence_band(confidence_value)

        return {
            "intent": intent,
            "confidence": confidence_value,
            "entities": entities,
            "model_used": model_used,
            "confidence_band": confidence_band,
            "needs_review": confidence_band == "low",
            "resolver_used": resolver_used,
        }

    def _classify_keyword(self, text: str) -> dict:
        normalized = text.strip().lower()
        intent, confidence = "student_services", 0.35

        keyword_map = {
            "exam_and_grading": (
                [
                    "vize",
                    "final",
                    "sinav",
                    "butunleme",
                    "bütünleme",
                    "not",
                    "ortalama",
                    "tek ders",
                    "mazeret",
                    "gano",
                    "yano",
                    "harf notu",
                ],
                0.82,
            ),
            "course_registration": (
                [
                    "ders",
                    "kredi",
                    "program",
                    "kayit",
                    "mufredat",
                    "danisman onayi",
                    "ekleme",
                    "muafiyet",
                    "intibak",
                    "yatay gecis",
                    "cift anadal",
                    "yandal",
                    "katki payi",
                    "cap",
                ],
                0.80,
            ),
            "digital_systems": (
                [
                    "portal",
                    "sifre",
                    "sistem",
                    "obs",
                    "giris yapamiyorum",
                    "e-posta",
                    "eposta",
                    "mail",
                    "rebis",
                    "office 365",
                    "vpn",
                    "wifi",
                    "eduroam",
                    "rteu.net",
                    "e-kampus",
                ],
                0.80,
            ),
            "student_services": (
                [
                    "ogrenci belgesi",
                    "transkript",
                    "not dokum",
                    "diploma",
                    "diploma eki",
                    "ilisik kesme",
                    "mezuniyet belgesi",
                    "ogrenci isleri",
                    "hangi birim",
                    "nereye basvur",
                    "gecici mezuniyet",
                    "asli gibidir",
                    "kayit sildirme",
                ],
                0.78,
            ),
            "library": (
                [
                    "kutuphane",
                    "kitap",
                    "odunc",
                    "calisma salonu",
                    "veritabani",
                    "vetis",
                    "proxy",
                    "ithenticate",
                    "gecikme cezasi",
                    "grup calisma",
                    "uzaktan erisim",
                ],
                0.81,
            ),
            "student_life": (
                [
                    "etkinlik",
                    "konser",
                    "seminer",
                    "konferans",
                    "festival",
                    "senlik",
                    "kulup",
                    "topluluk",
                    "spor",
                    "kariyer",
                    "staj duyurusu",
                    "yemekhane",
                    "menu",
                    "rezervasyon",
                    "bakiye",
                    "barinma",
                    "yurt",
                    "ulasim",
                    "ring",
                    "yerleske",
                    "psikolojik destek",
                    "psikolojik danismanlik",
                    "odk",
                    "ogrenci destek",
                    "engelli ogrenci",
                    "engelsiz universite",
                    "rpduam",
                ],
                0.79,
            ),
            "scholarship_support": (
                ["burs", "mali destek", "kredi ve yurtlar", "kyk", "yemek bursu", "kismi zamanli", "tev"],
                0.83,
            ),
        }
        hits_by_label: dict[str, int] = {}
        for label, (keywords, _) in keyword_map.items():
            hits_by_label[label] = sum(1 for kw in keywords if kw in normalized)

        if "cap" in normalized or "cift anadal" in normalized or "yandal" in normalized:
            intent = "course_registration"
            confidence = 0.84
        elif "kayit sildirme" in normalized or "ilisik kesme" in normalized:
            intent = "student_services"
            confidence = 0.84
        elif "engelli ogrenci" in normalized or "psikolojik danismanlik" in normalized or "rpduam" in normalized:
            intent = "student_life"
            confidence = 0.82
        else:
            best_label = max(hits_by_label, key=hits_by_label.get)
            best_hits = hits_by_label[best_label]
            if best_hits > 0:
                intent = best_label
                confidence = keyword_map[best_label][1]

        entities = self._extract_entities(text)
        confidence_band = self._confidence_band(confidence)

        return {
            "intent": intent,
            "confidence": confidence,
            "entities": entities,
            "model_used": "keyword-fallback",
            "confidence_band": confidence_band,
            "needs_review": confidence_band == "low",
            "resolver_used": False,
        }

    def _extract_entities(self, text: str) -> list[dict]:
        entities = []
        normalized = text.lower()
        seen_spans: set[tuple[int, int, str]] = set()

        for pattern in self.entity_patterns:
            for match in re.finditer(re.escape(pattern["alias"]), normalized):
                span = (match.start(), match.end(), pattern["type"])
                if span in seen_spans:
                    continue
                seen_spans.add(span)
                entities.append(
                    {
                        "value": text[match.start() : match.end()],
                        "type": pattern["type"],
                        "start": match.start(),
                        "end": match.end(),
                    }
                )

        temporal_markers = ["ne zaman", "hangi tarih", "kacta", "saat kac", "kaçta", "saat kaç"]
        for marker in temporal_markers:
            idx = normalized.find(marker)
            if idx != -1:
                span = (idx, idx + len(marker), "temporal")
                if span in seen_spans:
                    continue
                entities.append(
                    {
                        "value": text[idx : idx + len(marker)],
                        "type": "temporal",
                        "start": idx,
                        "end": idx + len(marker),
                    }
                )

        entities.sort(key=lambda item: item["start"])
        return entities

    def _resolve_ambiguous_intent(
        self, text: str, predicted_intent: str, confidence: float, margin: float
    ) -> str:
        normalized = text.lower()
        def has_any(keywords: list[str]) -> bool:
            return any(keyword in normalized for keyword in keywords)

        library_remote_terms = [
            "kutuphane",
            "vetis",
            "proxy",
            "veritabani",
            "e-dergi",
            "ithenticate",
            "odunc",
            "gecikme cezasi",
            "calisma odasi",
            "uzaktan erisim",
            "kampus disi",
        ]
        library_anchor_terms = [
            "vetis",
            "proxy",
            "veritabani",
            "e-dergi",
            "ithenticate",
            "odunc",
            "gecikme cezasi",
            "calisma odasi",
            "uzaktan erisim",
        ]
        digital_terms = [
            "obs",
            "rebis",
            "e-kampus",
            "office 365",
            "mail",
            "e-posta",
            "eposta",
            "sifre",
            "wifi",
            "eduroam",
            "vpn",
            "rteu.net",
        ]
        wellbeing_terms = [
            "psikolojik",
            "danismanlik",
            "odk",
            "ogrenci destek",
            "engelli ogrenci",
            "engelsiz universite",
            "rpduam",
            "sosyal destek",
        ]
        student_service_terms = [
            "ogrenci belgesi",
            "transkript",
            "diploma",
            "diploma eki",
            "ilisik kesme",
            "gecici mezuniyet",
            "asli gibidir",
            "ogrenci isleri",
        ]
        registration_terms = [
            "ders kaydi",
            "kayit yenileme",
            "muafiyet",
            "intibak",
            "cap",
            "cift anadal",
            "yandal",
            "danisman onayi",
            "katki payi",
            "ekle birak",
        ]
        service_anchor_terms = ["kayit sildirme", "ilisik kesme", "ogrenci belgesi", "transkript", "diploma"]

        if has_any(["kayit sildirme", "ilisik kesme"]):
            return "student_services"
        if has_any(["cap", "cift anadal", "yandal", "muafiyet", "intibak", "danisman onayi"]):
            return "course_registration"
        if has_any(wellbeing_terms):
            return "student_life"
        if "vpn" in normalized and not has_any(library_anchor_terms):
            return "digital_systems"
        if has_any(library_anchor_terms):
            return "library"
        if has_any(service_anchor_terms):
            return "student_services"
        if confidence >= 0.75 and margin >= 0.18:
            return predicted_intent

        if has_any(student_service_terms):
            return "student_services"
        if has_any(library_remote_terms):
            return "library"
        if has_any(digital_terms):
            return "digital_systems"
        if has_any(registration_terms):
            return "course_registration"
        return predicted_intent
