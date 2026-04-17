import json
from collections import Counter
from pathlib import Path
import sys

from sklearn.metrics import classification_report, confusion_matrix

ROOT = Path(__file__).resolve().parent.parent
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from services.intent_service import IntentService
from services.knowledge_service import KnowledgeService

TEST_PATH = ROOT / "data" / "phase_c_hard_test.json"
REPORT_PATH = ROOT / "docs" / "phase_c_hard_eval.md"

INTENT_LABELS = [
    "exam_and_grading",
    "course_registration",
    "digital_systems",
    "student_services",
    "library",
    "student_life",
    "scholarship_support",
]


def load_cases():
    with open(TEST_PATH, "r", encoding="utf-8-sig") as f:
        return json.load(f)


def main():
    intent_service = IntentService()
    knowledge_service = KnowledgeService()
    cases = load_cases()

    y_true, y_pred = [], []
    rows = []
    per_intent_total = Counter()
    per_intent_kb_hit = Counter()

    for case in cases:
        text = case["text"]
        expected = case["intent"]
        result = intent_service.classify(text)
        predicted = result["intent"]
        kb_match = knowledge_service.lookup(predicted, text, result["entities"])

        y_true.append(expected)
        y_pred.append(predicted)
        per_intent_total[expected] += 1
        if kb_match:
            per_intent_kb_hit[expected] += 1

        rows.append(
            {
                "text": text,
                "expected": expected,
                "predicted": predicted,
                "confidence": result["confidence"],
                "kb_hit": kb_match is not None,
                "kb_topic": kb_match["topic"] if kb_match else "",
            }
        )

    report_text = classification_report(
        y_true,
        y_pred,
        labels=INTENT_LABELS,
        target_names=INTENT_LABELS,
        zero_division=0,
    )
    matrix = confusion_matrix(y_true, y_pred, labels=INTENT_LABELS)
    mismatches = [row for row in rows if row["expected"] != row["predicted"]]
    kb_hit_rate = sum(1 for row in rows if row["kb_hit"]) / len(rows)

    lines = [
        "# EduAI Phase C Hard Evaluation",
        "",
        f"- Test seti: `{len(rows)}` zor örnek",
        f"- KB hit rate: `{kb_hit_rate:.4f}`",
        "",
        "## Per-Intent KB Hit",
    ]

    for intent in INTENT_LABELS:
        hit = per_intent_kb_hit[intent]
        total = per_intent_total[intent]
        ratio = hit / total if total else 0.0
        lines.append(f"- `{intent}`: `{hit}/{total}` (`{ratio:.4f}`)")

    lines.extend([
        "",
        "## Classification Report",
        "```text",
        report_text.rstrip(),
        "```",
        "",
        "## Confusion Matrix",
        "```text",
        "labels = " + ", ".join(INTENT_LABELS),
    ])

    for idx, label in enumerate(INTENT_LABELS):
        lines.append(f"{label}: {matrix[idx].tolist()}")

    lines.extend(["```", "", "## Mismatches"])
    if mismatches:
        for row in mismatches:
            lines.append(
                f"- `{row['text']}` | beklenen=`{row['expected']}` tahmin=`{row['predicted']}` "
                f"guven=`{row['confidence']}` kb=`{row['kb_hit']}` topic=`{row['kb_topic'] or '-'}'"
            )
    else:
        lines.append("- Hata yok")

    REPORT_PATH.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"Saved report to {REPORT_PATH}")
    print(report_text)
    print("KB hit rate:", round(kb_hit_rate, 4))
    print("Mismatches:", len(mismatches))


if __name__ == "__main__":
    main()
