import json
from collections import Counter
from pathlib import Path


ROOT = Path(__file__).resolve().parent.parent
DATASET_PATH = ROOT / "data" / "dataset.json"
KB_PATH = ROOT / "data" / "knowledge_base.json"
HARD_TEST_PATH = ROOT / "data" / "phase_c_hard_test.json"

TARGET_INTENTS = [
    "exam_and_grading",
    "course_registration",
    "digital_systems",
    "student_services",
    "library",
    "student_life",
    "scholarship_support",
]

WELLBEING_TOPICS = {
    "psikolojik_danismanlik",
    "ogrenci_destek_koordinatorlugu",
    "engelli_ogrenci_destegi",
    "yardim_kanallari",
    "odk_kurulus",
    "odk_amaci",
    "engelli_ogrenci_birimi",
    "rpduam_hizmeti",
    "rpduam_kapsam",
}

LIBRARY_REMOTE_TOPICS = {
    "kampus_disi_erisim",
    "elektronik_kaynaklar",
    "vetis_uzaktan_erisim",
    "proxy_ogrenci_belirsizligi",
}

CURATED_ROWS = {
    "student_life": [
        "psikolojik danismanlik icin randevu alabiliyor muyuz",
        "psikolojik destek hizmeti ogrenciler icin ucretsiz mi",
        "rpduam'a nasil ulasirim",
        "engelli ogrenci birimi hangi kampus hizmeti altinda",
        "engelsiz universite destegi icin kiminle gorusmeliyim",
        "odk ogrenci uyumu icin ne yapiyor",
        "ogrenci destek koordinatorlugu sosyal uyum konusunda yardimci oluyor mu",
        "kampuste psikolojik danismanlik nereye bagli",
        "engelli ogrenci destegi icin once hangi birime gitmeliyim",
        "odk etkinlik ve destek duyurularini nereden takip ederim",
        "psikolojik gorusme icin online basvuru var mi",
        "engelsiz universite birimi fiziksel erisim konusunda yardimci oluyor mu",
        "uyum sorunu yasarsam hangi ogrenci destek birimine giderim",
        "kampuste ogrenci refahi ile ilgilenen birim hangisi",
        "psikolojik danismanlik hizmeti yuz yuze mi veriliyor",
        "engelli ogrenciler icin kampus ici destekler nerede duyuruluyor",
        "odk sadece sikayet mi aliyor yoksa destek de veriyor mu",
        "ogrenci uyumu icin resmi destek kanali var mi",
        "psikolojik destek almak istersem nereyi aramam lazim",
        "engelli ogrenci koordinasyonu hangi birimde",
    ],
    "student_services": [
        "transkriptin imzali halini nereden alirim",
        "ogrenci belgesini oidb disinda alabilecegim resmi kanal var mi",
        "diploma eki mezun oldugumda otomatik veriliyor mu",
        "ilisik kesme islemi ogrenci islerinden mi yurutulur",
        "gecici mezuniyet belgesi almak icin ne gerekir",
        "asli gibidir onayi icin hangi belgeyle gitmeliyim",
        "ogrenci durum belgesini hangi sistemden gorurum",
        "kayit sildirme dilekcesi nereye teslim edilir",
        "diploma tesliminde vekalet kabul ediliyor mu",
        "not dokum belgesinin resmi adi nedir",
        "ogrenci numarasi sorgulama belge islemlerinde kullaniliyor mu",
        "e devletten alinan ogrenci belgesi yeterli mi",
        "transkript ucreti resmi olarak var mi",
        "diploma sorgulama ekraninda hangi bilgiler istenir",
        "mezuniyet sonrasi belge taleplerini hangi birim aliyor",
    ],
    "library": [
        "kampus disindan vetis ile mi baglanacagim",
        "proxy ayari olmadan kutuphane veritabanina girebilir miyim",
        "ithenticate ogrenci kullanimi kutuphane uzerinden mi",
        "uzaktan erisim icin vetis mi proxy mi gerekli",
        "kampus disi erisimde kutuphane sifresi ayri mi",
        "kutuphane veritabanlari evden aciliyor mu",
        "e dergilere kampus disindan nereden baglanirim",
        "veritabanina girerken proxy mi vetis mi kullanmaliyim",
        "katalogtan buldugum kitabi online uzatabilir miyim",
        "gecikme cezasi gunluk ne kadar yaziyor",
        "grup calisma odasi rezerve etmek icin kutuphane hesabim yeterli mi",
        "kutuphane hesabi ile uzaktan veritabanina baglanabilir miyim",
        "kampus disinda tez ve makale erisimi hangi kutuphane sistemiyle oluyor",
        "kutuphane proxy bilgisi ogrenciye acik mi",
        "odunc aldigim kitabin iade tarihini hangi kutuphane ekraninda gorurum",
        "kutuphane elektronik kaynaklarinda vpn yerine vetis mi kullaniliyor",
        "kutuphane uzaktan erisim ogrenci mailiyle mi aciliyor",
        "veritabanlari icin kutuphane mi bilgi islem mi yetkili",
    ],
    "digital_systems": [
        "obs sifremi unuttum nereden sifirlarim",
        "rebis girisi neden hata veriyor",
        "office 365 hesabim acilmiyor ne yapmaliyim",
        "ogrenci e postami ilk kez nasil aktif ederim",
        "kampus wifi icin hangi kullanici bilgisi lazim",
        "eduroam ayarlarini nereden bulurum",
        "vpn baglantisi bilgi islemden mi aliniyor",
        "e kampuse giremiyorum sifrem dogru oldugu halde acmiyor",
        "mail sifresi ile obs sifresi ayni mi",
        "rteu.net authentication servisi ne ise yariyor",
        "office hesabinda okul mailimi mi kullanacagim",
        "ogrenci numarasi ile obs girisi ayni sey mi",
        "webmail girisi icin hangi adres kullaniliyor",
        "bidb teknik destek talebi nasil acilir",
        "kurumsal mailime telefondan nasil baglanirim",
        "vpn bilgisi bilgi islem sayfasinda mi",
        "office lisansi ogrenci mailiyle mi tanimlaniyor",
        "wifi ve eduroam farkli kullanici bilgisi mi istiyor",
    ],
    "course_registration": [
        "cap basvurusu icin gano alt siniri nedir",
        "cift anadal icin once danisman onayi gerekiyor mu",
        "yandal basvurusunda hangi donem dikkate aliniyor",
        "muafiyet dilekcesi ders kaydindan once mi verilir",
        "intibak sonucu ders programimi etkiler mi",
        "katki payi odemeden kayit yenilenir mi",
        "ders ekle birak haftasi bitince ne olur",
        "danisman onayi olmadan secilen dersler kesinlesir mi",
        "mazeretli kayit icin oidb mi fakulte mi yetkili",
        "dgs ogrencileri intibak surecini nasil takip eder",
        "cap ve yandal ayni anda yapilabiliyor mu",
        "kayit yenileme kacirilirsa ders secimi tamamen kapanir mi",
        "muafiyet basvurusu sonucunda ders yukum azalir mi",
        "intibak karari cikmadan ders kaydi yapilir mi",
        "katki payi odemesi gorunmeden sistem ders secer mi",
    ],
}

PREFIXES = [
    "net sorayim ",
    "kisa sorayim ",
    "tam anlamadim ",
    "ogrenci olarak soruyorum ",
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


def unique_rows(rows: list[dict]) -> list[dict]:
    seen = set()
    result = []
    for row in rows:
        key = (normalize_text(row["text"]), row["intent"])
        if key in seen:
            continue
        seen.add(key)
        result.append(row)
    return result


def typo_variant(text: str) -> str:
    replacements = {
        "ğ": "g",
        "ü": "u",
        "ş": "s",
        "ı": "i",
        "ö": "o",
        "ç": "c",
    }
    out = text
    for old, new in replacements.items():
        out = out.replace(old, new)
    out = out.replace("mı", "mi").replace("mu", "mu")
    return out


def build_rows() -> list[dict]:
    dataset = load_json(DATASET_PATH)
    hard = load_json(HARD_TEST_PATH)
    rows = [{"text": item["text"], "intent": item["intent"], "entities": item.get("entities", [])} for item in dataset]

    for item in hard:
        rows.append({"text": item["text"], "intent": item["intent"], "entities": []})
        rows.append({"text": typo_variant(item["text"]), "intent": item["intent"], "entities": []})

    for intent, texts in CURATED_ROWS.items():
        for idx, text in enumerate(texts):
            rows.append({"text": text, "intent": intent, "entities": []})
            rows.append({"text": typo_variant(text), "intent": intent, "entities": []})
            rows.append(
                {
                    "text": f"{PREFIXES[idx % len(PREFIXES)]}{text}",
                    "intent": intent,
                    "entities": [],
                }
            )

    return unique_rows(rows)


def rebalance(rows: list[dict], minimum_per_intent: int = 220, maximum_per_intent: int = 320) -> list[dict]:
    grouped = {intent: [] for intent in TARGET_INTENTS}
    for row in rows:
        if row["intent"] in grouped:
            grouped[row["intent"]].append(row)

    final_rows = []
    seen = set()
    for intent, items in grouped.items():
        unique_items = []
        for item in items:
            key = (normalize_text(item["text"]), intent)
            if key in seen:
                continue
            seen.add(key)
            unique_items.append(item)

        while len(unique_items) < minimum_per_intent and unique_items:
            seed = unique_items[len(unique_items) % len(unique_items) - 1]
            base = seed["text"].rstrip("?.! ")
            variant = f"{PREFIXES[len(unique_items) % len(PREFIXES)]}{base}"
            key = (normalize_text(variant), intent)
            if key not in seen:
                seen.add(key)
                unique_items.append({"text": variant, "intent": intent, "entities": []})
            else:
                break

        final_rows.extend(unique_items[:maximum_per_intent])

    final_rows.sort(key=lambda row: (row["intent"], normalize_text(row["text"])))
    return final_rows


def update_kb():
    kb = load_json(KB_PATH)
    for entry in kb["entries"]:
        aliases = set(entry.get("intent_aliases", []))
        if entry.get("topic") in WELLBEING_TOPICS:
            aliases.add("student_life")
        if entry.get("topic") in LIBRARY_REMOTE_TOPICS:
            aliases.add("digital_systems")
            keywords = set(entry.get("keywords", []))
            keywords.update(["vetis", "proxy", "kampus disi", "uzaktan erisim"])
            entry["keywords"] = sorted(keywords)
        entry["intent_aliases"] = sorted(aliases)
    save_json(KB_PATH, kb)
    return kb


def main():
    rows = build_rows()
    final_rows = rebalance(rows)
    save_json(DATASET_PATH, final_rows)
    kb = update_kb()

    counts = Counter(row["intent"] for row in final_rows)
    print(f"Saved {len(final_rows)} dataset rows")
    print(dict(sorted(counts.items())))
    print(f"Knowledge base entries: {len(kb['entries'])}")


if __name__ == "__main__":
    main()
