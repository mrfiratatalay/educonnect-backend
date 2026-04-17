"""Knowledge Base lookup service.

Supports both the legacy per-intent FAQ map and the new structured
entry-based knowledge base schema.
"""

import json
import logging
from pathlib import Path
import re
import unicodedata

logger = logging.getLogger(__name__)

KB_PATH = Path(__file__).parent.parent / "data" / "knowledge_base.json"


class KnowledgeService:
    def __init__(self):
        self.kb: dict[str, list[dict]] = {}
        self.schema_version = "legacy"
        self._load()

    def _load(self):
        if not KB_PATH.exists():
            logger.warning("Knowledge base file not found at %s", KB_PATH)
            return

        try:
            with open(KB_PATH, "r", encoding="utf-8-sig") as f:
                raw = json.load(f)

            if isinstance(raw, dict) and isinstance(raw.get("entries"), list):
                self.schema_version = str(raw.get("schema_version", "2.0"))
                self.kb = self._build_index(raw["entries"])
            else:
                self.schema_version = "legacy"
                self.kb = raw

            total = sum(len(faqs) for faqs in self.kb.values())
            logger.info(
                "Knowledge base loaded: schema=%s, %d categories, %d entries",
                self.schema_version,
                len(self.kb),
                total,
            )
        except Exception as e:
            logger.error("Failed to load knowledge base: %s", e)

    def _build_index(self, entries: list[dict]) -> dict[str, list[dict]]:
        indexed: dict[str, list[dict]] = {}
        for entry in entries:
            intents = [entry.get("intent", "student_services"), *entry.get("intent_aliases", [])]
            for intent in intents:
                indexed.setdefault(intent, []).append(entry)
        return indexed

    def lookup(self, intent: str, user_text: str, entities: list[dict] | None = None) -> dict | None:
        """Find the best matching FAQ for the given intent and user text."""
        faqs = self.kb.get(intent, [])
        if not faqs:
            return None

        normalized_text = self._normalize_text(user_text)
        best_match = None
        best_score = 0.0

        for faq in faqs:
            score = self._compute_score(normalized_text, faq, entities or [])
            if score > best_score:
                best_score = score
                best_match = faq

        if not best_match:
            return None

        threshold = self._threshold_for(best_match)
        if best_score < threshold:
            return None

        return {
            "question": best_match["question"],
            "answer": best_match["answer"],
            "score": round(best_score, 4),
            "source_url": best_match.get("source_url", ""),
            "source_title": best_match.get("source_title", ""),
            "confidence": best_match.get("confidence", "medium"),
            "time_sensitive": best_match.get("time_sensitive", False),
            "faculty_scope": best_match.get("faculty_scope", "general"),
            "topic": best_match.get("topic", ""),
        }

    def _compute_score(self, normalized_text: str, faq: dict, entities: list[dict]) -> float:
        keywords = [self._normalize_text(keyword) for keyword in faq.get("keywords", [])]
        topic = faq.get("topic", "")
        entity_values = [self._normalize_text(entity.get("value", "")) for entity in entities]
        text_words = self._tokenize(normalized_text)

        keyword_score = 0.0
        if keywords:
            matched = sum(1 for kw in keywords if self._matches_keyword(kw, normalized_text, text_words))
            keyword_score = matched / len(keywords)

        question_words = self._tokenize(self._normalize_text(faq["question"]))
        overlap = len(question_words & text_words)
        question_score = overlap / max(len(question_words), 1)

        topic_score = 0.0
        if topic:
            topic_terms = set(self._normalize_text(topic).split("_"))
            topic_overlap = len(topic_terms & text_words)
            topic_score = topic_overlap / max(len(topic_terms), 1)

        entity_score = 0.0
        if entity_values:
            matched_entities = sum(1 for value in entity_values if value and value in normalized_text)
            entity_score = matched_entities / max(len(entity_values), 1)

        exact_question_bonus = 0.10 if self._normalize_text(faq["question"]) in normalized_text else 0.0
        confidence_bonus = 0.05 if faq.get("confidence") == "high" else 0.0
        time_penalty = 0.05 if faq.get("time_sensitive", False) else 0.0

        score = (
            (keyword_score * 0.45)
            + (question_score * 0.25)
            + (topic_score * 0.10)
            + (entity_score * 0.20)
            + exact_question_bonus
            + confidence_bonus
            - time_penalty
        )

        return max(score, 0.0)

    def _threshold_for(self, faq: dict) -> float:
        threshold = 0.18
        if faq.get("confidence") == "medium":
            threshold += 0.06
        if faq.get("time_sensitive", False):
            threshold += 0.05
        return threshold

    def _matches_keyword(self, keyword: str, normalized_text: str, text_words: set[str]) -> bool:
        if not keyword:
            return False

        if keyword in normalized_text:
            return True

        keyword_tokens = self._tokenize(keyword)
        if not keyword_tokens:
            return False

        matched_tokens = 0
        for keyword_token in keyword_tokens:
            if any(self._token_similar(keyword_token, text_token) for text_token in text_words):
                matched_tokens += 1

        ratio = matched_tokens / len(keyword_tokens)
        return ratio >= 0.6

    def _token_similar(self, left: str, right: str) -> bool:
        if left == right:
            return True

        if len(left) >= 4 and right.startswith(left):
            return True

        if len(right) >= 4 and left.startswith(right):
            return True

        return False

    def _normalize_text(self, text: str) -> str:
        text = text.lower().strip()
        text = (
            text.replace("ı", "i")
            .replace("ğ", "g")
            .replace("ü", "u")
            .replace("ş", "s")
            .replace("ö", "o")
            .replace("ç", "c")
        )
        text = unicodedata.normalize("NFKD", text)
        return "".join(char for char in text if not unicodedata.combining(char))

    def _tokenize(self, text: str) -> set[str]:
        return {token for token in re.split(r"\W+", text) if token}
