import json
from pathlib import Path


ROOT = Path(__file__).resolve().parent.parent
DATASET_PATH = ROOT / "data" / "dataset.json"

TARGETED_ROWS = {
    "course_registration": [
        "cap basvurusu icin gano kac olmali",
        "cap yapmak icin ortalama siniri nedir",
        "cift anadal basvurusunda gano sarti kac",
        "yandal icin gano kosulu aranir mi",
        "muafiyet sonucu kayit planimi etkiler mi",
        "intibak sonrasi ders kaydi degisir mi",
        "cap basvuru kosullarini kim acikliyor",
        "cift anadal basvurusunda hangi not ortalamasi istenir",
    ],
    "student_services": [
        "kayit sildirme islemi online mi",
        "kaydimi sildirmek istersem nereyle gorusurum",
        "ilisik kesme ve kayit sildirme ayni sey mi",
        "ogrenci islerinden kayit sildirme yapiliyor mu",
        "diploma sonrasi ilisik kesme nereden yurutulur",
        "kayit sildirme icin dilekce nereye verilir",
        "kendi istegimle okuldan ayrilmak istersem ne yaparim",
        "ilisik kesme basvurusu sistemden mi yapiliyor",
    ],
    "student_life": [
        "engelli ogrenci birimi hangi birime bagli",
        "engelli ogrenci destegi kampus yasami tarafinda mi",
        "engelsiz universite birimi hangi resmi yapida",
        "psikolojik danismanlik ogrenci destek hizmeti olarak mi geciyor",
        "odk sosyal destek birimi olarak mi calisiyor",
        "rpduam hangi ogrenci destek yapisi altinda",
        "engelli ogrenciler icin kampus ici koordinasyon birimi hangisi",
        "ogrenci uyum ve destek birimi resmi olarak nerede geciyor",
    ],
    "digital_systems": [
        "vpn hangi teknik sistemin parcasi",
        "vpn bilgilerini bilgi islem mi veriyor",
        "obs ve vpn farkli seyler mi",
        "mail hesabimla vpn girisi ayni mi",
    ],
    "library": [
        "kutuphane uzaktan erisimde vetis mi kullanilir",
        "proxy yerine vetis ile veritabanina girilir mi",
        "kampus disi kutuphane erisimi library konusu mu",
        "veritabani erisimi kutuphane sistemi mi sayiliyor",
    ],
}

PREFIXES = [
    "",
    "net sorayim ",
    "kisa sorayim ",
    "ogrenci olarak soruyorum ",
]


def load_json(path: Path):
    with open(path, "r", encoding="utf-8-sig") as f:
        return json.load(f)


def save_json(path: Path, data):
    with open(path, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
        f.write("\n")


def normalize(text: str) -> str:
    return " ".join(text.strip().split()).lower()


def typo_variant(text: str) -> str:
    replacements = {
        "ğ": "g",
        "ü": "u",
        "ş": "s",
        "ı": "i",
        "ö": "o",
        "ç": "c",
    }
    result = text
    for old, new in replacements.items():
        result = result.replace(old, new)
    return result


def main():
    dataset = load_json(DATASET_PATH)
    seen = {(normalize(row["text"]), row["intent"]) for row in dataset}

    for intent, texts in TARGETED_ROWS.items():
        for idx, text in enumerate(texts):
            variants = {
                text,
                typo_variant(text),
                f"{PREFIXES[idx % len(PREFIXES)]}{text}",
            }
            for variant in variants:
                key = (normalize(variant), intent)
                if key in seen:
                    continue
                seen.add(key)
                dataset.append({"text": variant, "intent": intent, "entities": []})

    dataset.sort(key=lambda row: (row["intent"], normalize(row["text"])))
    save_json(DATASET_PATH, dataset)
    print(f"Saved {len(dataset)} dataset rows")


if __name__ == "__main__":
    main()
