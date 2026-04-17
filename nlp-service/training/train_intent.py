"""BERTurk intent classification fine-tuning script.

Usage:
    python training/train_intent.py --dataset data/dataset.json --epochs 5
"""

import argparse
import json
from pathlib import Path

import torch
from sklearn.metrics import accuracy_score, classification_report
from sklearn.model_selection import train_test_split
from torch.utils.data import DataLoader, Dataset
from transformers import (
    AutoModelForSequenceClassification,
    AutoTokenizer,
    get_linear_schedule_with_warmup,
)

INTENT_LABELS = [
    "exam_and_grading",
    "course_registration",
    "digital_systems",
    "student_services",
    "library",
    "student_life",
    "scholarship_support",
]

LABEL_TO_ID = {label: i for i, label in enumerate(INTENT_LABELS)}
MODEL_NAME = "dbmdz/bert-base-turkish-cased"
OUTPUT_DIR = Path(__file__).parent.parent / "models" / "intent_classifier"


class IntentDataset(Dataset):
    def __init__(self, texts, labels, tokenizer, max_length=128):
        self.encodings = tokenizer(
            texts, truncation=True, padding=True, max_length=max_length, return_tensors="pt"
        )
        self.labels = torch.tensor(labels, dtype=torch.long)

    def __len__(self):
        return len(self.labels)

    def __getitem__(self, idx):
        return {
            "input_ids": self.encodings["input_ids"][idx],
            "attention_mask": self.encodings["attention_mask"][idx],
            "labels": self.labels[idx],
        }


def load_dataset(path: str):
    with open(path, "r", encoding="utf-8-sig") as f:
        data = json.load(f)

    texts = [item["text"] for item in data]
    labels = [LABEL_TO_ID[item["intent"]] for item in data]
    return texts, labels


def evaluate(model, data_loader, device):
    model.eval()
    all_preds, all_labels = [], []

    with torch.no_grad():
        for batch in data_loader:
            batch = {k: v.to(device) for k, v in batch.items()}
            outputs = model(**batch)
            preds = torch.argmax(outputs.logits, dim=-1)
            all_preds.extend(preds.cpu().numpy())
            all_labels.extend(batch["labels"].cpu().numpy())

    acc = accuracy_score(all_labels, all_preds)
    report = classification_report(
        all_labels, all_preds, target_names=INTENT_LABELS, zero_division=0
    )
    return acc, report


def train(dataset_path: str, epochs: int, batch_size: int, lr: float):
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    print(f"Device: {device}")

    texts, labels = load_dataset(dataset_path)
    print(f"Loaded {len(texts)} samples across {len(set(labels))} intents")

    train_texts, val_texts, train_labels, val_labels = train_test_split(
        texts, labels, test_size=0.2, random_state=42, stratify=labels
    )

    tokenizer = AutoTokenizer.from_pretrained(MODEL_NAME)
    model = AutoModelForSequenceClassification.from_pretrained(
        MODEL_NAME, num_labels=len(INTENT_LABELS)
    ).to(device)

    train_dataset = IntentDataset(train_texts, train_labels, tokenizer)
    val_dataset = IntentDataset(val_texts, val_labels, tokenizer)
    train_loader = DataLoader(train_dataset, batch_size=batch_size, shuffle=True)
    val_loader = DataLoader(val_dataset, batch_size=batch_size)

    optimizer = torch.optim.AdamW(model.parameters(), lr=lr, weight_decay=0.01)
    total_steps = len(train_loader) * epochs
    scheduler = get_linear_schedule_with_warmup(
        optimizer, num_warmup_steps=int(total_steps * 0.1), num_training_steps=total_steps
    )

    best_val_acc = 0.0
    final_report = ""

    for epoch in range(epochs):
        model.train()
        total_loss = 0.0

        for batch in train_loader:
            batch = {k: v.to(device) for k, v in batch.items()}
            outputs = model(**batch)
            loss = outputs.loss
            total_loss += loss.item()

            loss.backward()
            torch.nn.utils.clip_grad_norm_(model.parameters(), 1.0)
            optimizer.step()
            scheduler.step()
            optimizer.zero_grad()

        avg_loss = total_loss / len(train_loader)
        val_acc, val_report = evaluate(model, val_loader, device)
        final_report = val_report

        print(f"\nEpoch {epoch + 1}/{epochs}")
        print(f"  Train Loss: {avg_loss:.4f}")
        print(f"  Val Accuracy: {val_acc:.4f}")

        if val_acc > best_val_acc:
            best_val_acc = val_acc
            OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
            model.save_pretrained(str(OUTPUT_DIR), safe_serialization=False)
            tokenizer.save_pretrained(str(OUTPUT_DIR))
            with open(OUTPUT_DIR / "metrics.json", "w", encoding="utf-8") as f:
                json.dump(
                    {
                        "best_validation_accuracy": round(best_val_acc, 4),
                        "epochs": epochs,
                        "batch_size": batch_size,
                        "learning_rate": lr,
                        "dataset_size": len(texts),
                        "label_count": len(INTENT_LABELS),
                        "labels": INTENT_LABELS,
                    },
                    f,
                    ensure_ascii=False,
                    indent=2,
                )
            print(f"  Model saved (best accuracy: {best_val_acc:.4f})")

    print(f"\nTraining complete. Best validation accuracy: {best_val_acc:.4f}")
    print("\nFinal classification report:")
    print(final_report)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Train BERTurk intent classifier")
    parser.add_argument("--dataset", default="data/dataset.json")
    parser.add_argument("--epochs", type=int, default=5)
    parser.add_argument("--batch-size", type=int, default=8)
    parser.add_argument("--lr", type=float, default=2e-5)
    args = parser.parse_args()

    train(args.dataset, args.epochs, args.batch_size, args.lr)
