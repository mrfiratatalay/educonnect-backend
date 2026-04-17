"""Compare different models on the same dataset for thesis report."""

import argparse
import json
import time
from pathlib import Path

from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.metrics import accuracy_score, f1_score
from sklearn.model_selection import train_test_split
from sklearn.svm import LinearSVC

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
MODEL_DIR = Path(__file__).parent.parent / "models" / "intent_classifier"

KEYWORD_MAP = {
    "exam_and_grading": [
        "vize",
        "final",
        "sinav",
        "butunleme",
        "bütünleme",
        "tek ders",
        "mazeret",
        "harf notu",
        "gano",
        "yano",
    ],
    "course_registration": [
        "ders kaydi",
        "kayit yenileme",
        "danisman",
        "muafiyet",
        "intibak",
        "cift anadal",
        "yandal",
        "katki payi",
    ],
    "library": ["kutuphane", "odunc", "vetis", "proxy", "veritabani", "e-dergi", "calisma odasi"],
    "scholarship_support": [
        "burs",
        "kyk",
        "maddi destek",
        "yemek bursu",
        "sosyal yardim",
        "ekonomik destek",
        "tev",
        "kismi zamanli",
    ],
    "digital_systems": [
        "rebis",
        "obs",
        "e-kampus",
        "sifre",
        "office 365",
        "vpn",
        "wifi",
        "eduroam",
        "mail",
    ],
    "student_services": [
        "transkript",
        "ogrenci belgesi",
        "diploma",
        "diploma eki",
        "ilisik kesme",
        "psikolojik destek",
        "odk",
        "engelli ogrenci",
    ],
    "student_life": [
        "etkinlik",
        "duyuru",
        "seminer",
        "konferans",
        "senlik",
        "kulup",
        "topluluk",
        "spor",
        "kampus yasami",
        "yemekhane",
        "menu",
        "rezervasyon",
        "barinma",
        "ulasim",
        "yurt",
        "kariyer",
        "psikolojik destek",
        "odk",
    ],
}


def load_dataset(path: str):
    with open(path, "r", encoding="utf-8-sig") as f:
        data = json.load(f)
    texts = [item["text"] for item in data]
    labels = [LABEL_TO_ID[item["intent"]] for item in data]
    return texts, labels


def evaluate_keyword(texts):
    preds = []
    start = time.perf_counter()

    for text in texts:
        normalized = text.lower()
        predicted = LABEL_TO_ID["student_services"]
        best_hits = 0
        for intent, keywords in KEYWORD_MAP.items():
            hits = sum(1 for kw in keywords if kw in normalized)
            if hits > best_hits:
                best_hits = hits
                predicted = LABEL_TO_ID[intent]
        preds.append(predicted)

    elapsed = (time.perf_counter() - start) * 1000
    return preds, elapsed


def evaluate_tfidf_svm(train_texts, train_labels, test_texts):
    vectorizer = TfidfVectorizer(max_features=5000, ngram_range=(1, 2))
    x_train = vectorizer.fit_transform(train_texts)
    x_test = vectorizer.transform(test_texts)

    model = LinearSVC(max_iter=3000)
    model.fit(x_train, train_labels)

    start = time.perf_counter()
    preds = model.predict(x_test).tolist()
    elapsed = (time.perf_counter() - start) * 1000
    return preds, elapsed


def evaluate_berturk(test_texts):
    if not MODEL_DIR.exists() or not any(MODEL_DIR.iterdir()):
        print("  [SKIP] Fine-tuned BERTurk model not found. Run train_intent.py first.")
        return None, None

    import torch
    from transformers import AutoModelForSequenceClassification, AutoTokenizer

    tokenizer = AutoTokenizer.from_pretrained(str(MODEL_DIR))
    model = AutoModelForSequenceClassification.from_pretrained(str(MODEL_DIR))
    model.eval()
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    model.to(device)

    preds = []
    start = time.perf_counter()

    for text in test_texts:
        inputs = tokenizer(
            text,
            return_tensors="pt",
            truncation=True,
            max_length=128,
            padding=True,
        )
        inputs = {k: v.to(device) for k, v in inputs.items()}
        with torch.no_grad():
            outputs = model(**inputs)
            pred = torch.argmax(outputs.logits, dim=-1).item()
            preds.append(pred)

    elapsed = (time.perf_counter() - start) * 1000
    return preds, elapsed


def print_metrics(name: str, labels, preds, elapsed: float):
    acc = accuracy_score(labels, preds)
    f1_macro = f1_score(labels, preds, average="macro", zero_division=0)
    f1_weighted = f1_score(labels, preds, average="weighted", zero_division=0)
    print(f"{name:<25} {acc:>10.4f} {f1_macro:>10.4f} {f1_weighted:>12.4f} {elapsed:>10.1f}")
    return name, acc, f1_macro, f1_weighted, elapsed


def main(dataset_path: str):
    texts, labels = load_dataset(dataset_path)
    train_texts, test_texts, train_labels, test_labels = train_test_split(
        texts,
        labels,
        test_size=0.2,
        random_state=42,
        stratify=labels,
    )

    print(f"Dataset: {len(texts)} total, {len(test_texts)} test samples\n")
    print(f"{'Model':<25} {'Accuracy':>10} {'F1-macro':>10} {'F1-weighted':>12} {'Time(ms)':>10}")
    print("-" * 70)

    results = []

    kw_preds, kw_time = evaluate_keyword(test_texts)
    results.append(print_metrics("Keyword Baseline", test_labels, kw_preds, kw_time))

    svm_preds, svm_time = evaluate_tfidf_svm(train_texts, train_labels, test_texts)
    results.append(print_metrics("TF-IDF + SVM", test_labels, svm_preds, svm_time))

    bert_preds, bert_time = evaluate_berturk(test_texts)
    if bert_preds is not None:
        results.append(print_metrics("BERTurk Fine-tuned", test_labels, bert_preds, bert_time))

    print("\n" + "=" * 70)
    best = max(results, key=lambda item: item[1])
    print(f"Best model: {best[0]} (accuracy: {best[1]:.4f})")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Compare NLP models for thesis")
    parser.add_argument("--dataset", default="data/dataset.json")
    args = parser.parse_args()
    main(args.dataset)
