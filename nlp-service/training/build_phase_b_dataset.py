import json
from collections import Counter
from pathlib import Path


ROOT = Path(__file__).resolve().parent.parent
DATASET_PATH = ROOT / "data" / "dataset.json"
KB_PATH = ROOT / "data" / "knowledge_base.json"

TARGET_INTENTS = [
    "exam_and_grading",
    "course_registration",
    "digital_systems",
    "student_services",
    "library",
    "student_life",
    "scholarship_support",
]

INTENT_MAP = {
    "exam_and_grading": "exam_and_grading",
    "course_registration": "course_registration",
    "digital_systems": "digital_systems",
    "student_services": "student_services",
    "library": "library",
    "scholarship_support": "scholarship_support",
    "campus_life": "student_life",
    "campus_logistics": "student_life",
    "cafeteria": "student_life",
    "student_life": "student_life",
    "marketplace": None,
}

QUESTION_PREFIXES = [
    "resmi olarak ",
    "kisa sorayim, ",
    "bir sey soracagim, ",
    "net olarak ",
    "tam olarak ",
    "ogrenci gozunden soruyorum, ",
]

SOURCE_SUFFIXES = [
    " nereden ogrenebilirim?",
    " ile ilgili resmi bilgi nedir?",
    " icin resmi kural nedir?",
    " hakkinda resmi aciklama var mi?",
]


def load_json(path: Path):
    with open(path, "r", encoding="utf-8-sig") as f:
        return json.load(f)


def save_json(path: Path, data):
    with open(path, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
        f.write("\n")


def normalize_text(text: str) -> str:
    return " ".join(text.strip().split()).lower()


def clean_generated_text(text: str) -> str:
    cleaned = " ".join(text.strip().split())
    replacements = [
        ("Acil soruyorum, ", ""),
        ("acil soruyorum, ", ""),
        ("Kisa sorayim, ", ""),
        ("kisa sorayim, ", ""),
        ("Bir sey soracagim, ", ""),
        ("bir sey soracagim, ", ""),
        ("Resmi olarak ", ""),
        ("resmi olarak ", ""),
        (" tam olarak nasil oluyor?", "?"),
        (" tam olarak nasil isliyor?", "?"),
        (" ile ilgili resmi surec nasil isliyor?", "?"),
        (" hakkinda resmi bilgi nedir?", "?"),
        (" ile ilgili resmi bilgi nedir?", "?"),
    ]
    for old, new in replacements:
        cleaned = cleaned.replace(old, new)
    cleaned = cleaned.replace("??", "?")
    return " ".join(cleaned.split())


def map_old_dataset(old_rows: list[dict]) -> list[dict]:
    mapped = []
    seen = set()
    for row in old_rows:
        old_intent = row["intent"]
        new_intent = INTENT_MAP.get(old_intent)
        if not new_intent:
            continue
        text = clean_generated_text(row["text"])
        key = (normalize_text(text), new_intent)
        if key in seen:
            continue
        seen.add(key)
        mapped.append({"text": text, "intent": new_intent, "entities": row.get("entities", [])})
    return mapped


def build_kb_variants(entries: list[dict]) -> list[dict]:
    rows = []
    seen = set()

    for idx, entry in enumerate(entries):
        intent = entry["intent"]
        if intent not in TARGET_INTENTS:
            continue

        question = entry["question"].strip()
        base = question[:-1] if question.endswith("?") else question

        variants = [
            question,
            f"{QUESTION_PREFIXES[idx % len(QUESTION_PREFIXES)]}{base.lower()}?",
            f"{base.lower()}{SOURCE_SUFFIXES[idx % len(SOURCE_SUFFIXES)]}",
        ]

        if entry.get("faculty_scope", "general") != "general":
            faculty = entry["faculty_scope"]
            variants.append(f"{faculty} icin {base.lower()}?")

        if entry.get("time_sensitive", False):
            variants.append(f"{base.lower()} tarih olarak nereden takip edilir?")

        for variant in variants:
            clean = " ".join(variant.split())
            key = (normalize_text(clean), intent)
            if key in seen:
                continue
            seen.add(key)
            rows.append({"text": clean, "intent": intent, "entities": []})

    return rows


def rebalance(rows: list[dict], minimum_per_intent: int = 180, maximum_per_intent: int = 260) -> list[dict]:
    grouped: dict[str, list[dict]] = {intent: [] for intent in TARGET_INTENTS}
    seen = set()

    for row in rows:
        key = (normalize_text(row["text"]), row["intent"])
        if key in seen:
            continue
        seen.add(key)
        grouped[row["intent"]].append(row)

    balanced = []
    for intent, items in grouped.items():
        items = items[:maximum_per_intent]
        balanced.extend(items)
        if len(items) >= minimum_per_intent:
            continue

        deficit = minimum_per_intent - len(items)
        for idx in range(deficit):
            seed = items[idx % len(items)]
            text = seed["text"].rstrip("?.!").lower()
            prefix = QUESTION_PREFIXES[(idx + len(intent)) % len(QUESTION_PREFIXES)]
            suffix = SOURCE_SUFFIXES[idx % len(SOURCE_SUFFIXES)]
            variant = f"{prefix}{text}{suffix}"
            key = (normalize_text(variant), intent)
            if key in seen:
                continue
            seen.add(key)
            balanced.append({"text": variant, "intent": intent, "entities": []})

    return balanced


def main():
    old_dataset = load_json(DATASET_PATH)
    kb = load_json(KB_PATH)

    mapped = map_old_dataset(old_dataset)
    kb_rows = build_kb_variants(kb["entries"])
    merged = mapped + kb_rows
    final_rows = rebalance(merged, minimum_per_intent=180)

    final_rows.sort(key=lambda row: (row["intent"], normalize_text(row["text"])))
    save_json(DATASET_PATH, final_rows)

    counts = Counter(row["intent"] for row in final_rows)
    print(f"Saved {len(final_rows)} dataset rows")
    print(dict(sorted(counts.items())))


if __name__ == "__main__":
    main()
