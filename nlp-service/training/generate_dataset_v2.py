"""Generate a larger, more discriminative intent dataset for EduAI.

This script expands the current intent taxonomy with clearer boundaries and
multiple language styles per intent: clean, formal, colloquial, short and typo.
"""

from __future__ import annotations

import json
import random
from pathlib import Path


OUTPUT_PATH = Path(__file__).parent.parent / "data" / "dataset.json"
SEED = 42


INTENT_STEMS = {
    "exam_and_grading": [
        "butunleme sinavina kimler girebilir",
        "finalde minimum kac almak gerekir",
        "mazeret sinavina nasil basvuru yapilir",
        "tek ders sinavi basari puani kactir",
        "sinav notuna itiraz suresi ne kadardir",
        "devam sarti saglanmazsa finale girilebilir mi",
        "dc notu alan ogrenci notunu yukseltmek icin ne yapabilir",
        "gano mezuniyet icin en az kac olmalidir",
        "harf notlari hangi puan araliklarina gore verilir",
        "ara sinav ile finalin basari notuna etkisi nasil hesaplanir",
        "sinav programi nerede ilan edilir",
        "tip fakultesinde sinav sistemi farkli midir",
        "uygulamali derste devam sarti kactir",
        "butunleme notu final notunun yerine gecer mi",
        "mazeret sinavi icin rapor nereye teslim edilir",
        "yil sonu sinavinda ellinin altinda kalinca ne olur",
    ],
    "course_registration": [
        "kayit yenileme yapmazsam ne olur",
        "ders kaydinda danisman onayi zorunlu mudur",
        "ders ekleme birakma sureci nasil isler",
        "katki payi odemeden ders secimi yapilabilir mi",
        "mazeretli kayit icin hangi birime basvurulur",
        "muafiyet basvurusu ne zaman yapilir",
        "intibak sureci hangi birim tarafindan yurur",
        "yatay gecis basvurusu nasil yapilir",
        "cift anadal ile yandal sureci arasindaki fark nedir",
        "alttan kalan ders once alinmak zorunda midir",
        "secmeli dersler hangi donemde secilir",
        "farkli fakulteden ders almak mumkun mudur",
        "staj dersi kaydi nasil yapilir",
        "danisman onayi gecikirse ders kaydi etkilenir mi",
        "dersin on kosulu olup olmadigi nereden gorulur",
        "kayit dondurma ile kayit yenilememe ayni sey midir",
    ],
    "campus_life": [
        "ogrenci topluluklarinin duyurulari nerede yayinlanir",
        "kulup basvurulari ne zaman acilir",
        "kampuste resmi sosyal etkinlik takvimi var midir",
        "kariyer merkezi hangi etkinlikleri duzenler",
        "spor turnuvalari ve faaliyetleri nereden takip edilir",
        "topluluk kurmak icin resmi surec var midir",
        "mezuniyet toreni duyurulari hangi kanalda yayinlanir",
        "muzik kulubu etkinliklerine nasil katilinir",
        "gonulluluk kulubu faaliyetleri nasil takip edilir",
        "kampuste seminer ve konferanslar nerede ilan edilir",
        "ogrenci temsilciligi sureci nasil isler",
        "sosyal etkinlikler icin sks duyuru geciyor mu",
        "kampuste aktif kulup ve topluluk listesi var midir",
        "sosyal uyum icin universitenin destekledigi faaliyetler nelerdir",
        "spor tesislerini kullanmak icin basvuru gerekir mi",
        "topluluk standlari ve tanitim gunleri ne zaman olur",
    ],
    "library": [
        "kutuphane hangi saatlerde aciktir",
        "sinav doneminde kutuphane saatleri uzar mi",
        "kutuphane odunc kurallari nerede yaziyor",
        "gec teslim edilen kitaplar icin ceza uygulanir mi",
        "kampus disindan veritabanlarina nasil baglanilir",
        "vetis ile uzaktan erisim ayni amaca mi hizmet eder",
        "kutuphane elektronik kaynaklar sunuyor mu",
        "bireysel calisma odasi icin rezervasyon gerekir mi",
        "kutuphane hangi birime baglidir",
        "misafir kullanici kutuphaneye girebilir mi",
        "kutuphane hesabina nasil giris yapilir",
        "e dergi ve e kitaplara ogrenciler ulasabilir mi",
        "kayip kitap durumunda ne yapmam gerekir",
        "kitap bagislamak istersem kime ulasmaliyim",
        "intihal tarama programi erisimi kutuphane uzerinden mi saglanir",
        "kitap sorgulama sistemi hangi adresten acilir",
    ],
    "scholarship_support": [
        "yemek bursuna nasil basvuru yapilir",
        "maddi destek icin hangi resmi birime gidilir",
        "universite kyk disinda burs duyurusu yapiyor mu",
        "yemek bursu basvurulari hangi tarihte acilir",
        "basari bursu ile yemek bursu ayni sey midir",
        "dis burslar icin resmi yonlendirme nerede yer alir",
        "burs konularinda sks mi oidb mi yetkilidir",
        "universitenin sagladigi ekonomik destekler nelerdir",
        "maddi zorluk yasayan ogrenci hangi resmi kanallara basvurabilir",
        "katki payi veya ogrenim ucreti icin indirim bilgisi var midir",
        "yemek bursu duyurulari hangi sayfada yayinlanir",
        "burs turleri universite ici ve dis kaynak olarak ayriliyor mu",
        "sosyal yardim veya ayni destek basvurusu var midir",
        "odk burs veya ekonomik destek konusunda yonlendirme yapar mi",
        "kyk disindaki resmi burs ilanlari nereden takip edilir",
        "maddi destek ile psikososyal destek ayni birimden mi yurur",
    ],
    "campus_logistics": [
        "kampuse ulasimla ilgili resmi bilgi var midir",
        "yerleskelere sehir merkezinden nasil gidilir",
        "zihni derin yerleskesi nerede bulunur",
        "islampasa yerleskesine ulasim bilgisi var midir",
        "pazar yerleskesine nasil gidilecegi resmi olarak yaziyor mu",
        "kampus ici servis veya ring bilgisi paylasiliyor mu",
        "ogrenciler icin sehir ici ulasim rehberi var midir",
        "universite kampuslerinin konumlari resmi sitede yer aliyor mu",
        "yerleske bazli ulasim bilgisi hangi sayfada olur",
        "kampuslere toplu tasima ile erisim bilgisi ariyorum",
        "rteu yerleske adreslerini nereden gorebilirim",
        "barinma konusunda resmi rehber bilgi var midir",
        "universiteye ait yurt bilgisi resmi olarak yayinlaniyor mu",
        "yurt ve barinma icin hangi resmi kanallar takip edilmelidir",
        "kampus krokisi veya yerleske haritasi var midir",
        "otopark ve kampus giris duzeniyle ilgili resmi bilgi bulunur mu",
    ],
    "cafeteria": [
        "yemekhane hizmetlerini hangi birim yurutur",
        "yemek rezervasyon sistemi var midir",
        "yemekhane ucretleri guncel olarak nereden ogrenilir",
        "yemek listesi hangi resmi kanalda yayinlanir",
        "yemek bursu ile normal yemek ucreti ayni midir",
        "rezervasyon yapmadan yemek alinabilir mi",
        "kart veya bakiye yukleme sistemi var midir",
        "yemekhane uygulamasi doneme gore degisir mi",
        "beslenme hizmetleri duyurulari sks sayfasinda mi olur",
        "ucretsiz yemek uygulamasi varsa nasil duyurulur",
        "yemek fiyatlarinin tarih bagimli oldugu nereden anlasilir",
        "yemek rezervasyonu rebis uzerinden mi yapilir",
        "yemek iptali ayni gun yapilabilir mi",
        "vejetaryen veya diyet menu bilgisi resmi olarak yayinlanir mi",
        "merkez yemekhane hangi yerleskededir",
        "yemek karti kaybolursa ne yapilmalidir",
    ],
    "marketplace": [
        "pazarda ikinci el kitap ilani verilebilir mi",
        "kampus icinde ikinci el esya alisverisi var midir",
        "urun ilanlari hangi kategorilerde listelenir",
        "ders kitabi satmak icin ilan nasil acilir",
        "ikinci el laptop arayan ogrenci icin pazar modulu var mi",
        "gorsel arama ile benzer urun bulma ozelligi nasil calisir",
        "kampuste teslimli satis icin pazar modulu kullanilabilir mi",
        "ilan yayinlamak ucretli midir",
        "pazarda satilan urunler icin mesajlasma var midir",
        "ogrenciye uygun ikinci el urunler nasil bulunur",
        "pazar bolumunde kategori secimi nasil yapilir",
        "benzer urunleri foto ile aramak mumkun mudur",
        "yanlis urun gorseli yuklenirse nasil duzeltilir",
        "sahte ilanlar nereye sikayet edilir",
        "ev eşyasi veya bisiklet satmak icin uygun kategori hangisidir",
        "pazar modulu kampus icinde guvenli iletisim saglar mi",
    ],
    "digital_systems": [
        "obs sifresi unutulursa nasil sifirlanir",
        "rebis ile obs arasindaki fark nedir",
        "kurumsal ogrenci maili nereden ogrenilir",
        "varsayilan sifre nasil olusur",
        "office 365 hesabina nasil giris yapilir",
        "vpn ne icin kullanilir",
        "kampus wifi icin hangi hesap bilgileri kullanilir",
        "ogrenci numarasi e kampus ekranindan ogrenilebilir mi",
        "bilgi islem hangi teknik sorunlarla ilgilenir",
        "sifre sifirlama eski ve yeni sayfalarda neden farkli gorunur",
        "e kampus giris ekranindaki yeni sifre olustur ne ise yarar",
        "rteu net ile eduroam arasindaki fark nedir",
        "authentication service ne ise yarar",
        "webmail ogrenci hesabiyla acilir mi",
        "telefon degisikligi dijital sistemlerde nereden guncellenir",
        "vpn ile evden baglaninca kutuphane neden acilmiyor",
    ],
    "student_services": [
        "transkript nereden alinir",
        "ogrenci belgesi e devlet uzerinden alinabilir mi",
        "diploma durumu nasil sorgulanir",
        "diploma eki otomatik verilir mi",
        "ilisik kesme ne anlama gelir",
        "psikolojik destek almak istersem nereye basvururum",
        "ogrenci destek koordinatorlugu ne is yapar",
        "engelli ogrenciler icin resmi destek var midir",
        "universitede hangi resmi birim hangi sorunla ilgilenir",
        "kurumsal veya sosyal bir problem yasarsam nereye gitmeliyim",
        "kayit sildirme dilekcesi nereden bulunur",
        "diploma vekaletle teslim alinabilir mi",
        "asli gibidir onayi nasil alinir",
        "staj dosyasi tesliminde hangi birimle gorusulur",
        "kayit dondurma basvurusu hangi birimden baslatilir",
        "oidb ile fakulte ogrenci isleri arasindaki gorev farki nedir",
    ],
}


PREFIXES = [
    "",
    "rteu'de ",
    "recep tayyip erdogan universitesinde ",
    "okulda ",
]

SUFFIXES = [
    "",
    " hakkinda resmi bilgi nedir",
    " ile ilgili resmi surec nasil isliyor",
    " nereden ogrenilir",
]

CASUAL_PREFIXES = [
    "",
    "bir sey soracagim, ",
    "acil soruyorum, ",
    "kisa sorayim, ",
]

CASUAL_SUFFIXES = [
    " ya",
    " acaba",
    " tam olarak nasil oluyor",
    " nereden bakiyorum",
]

SHORTENINGS = {
    "ogrenci": "ogr",
    "universite": "uni",
    "kampus": "kampus",
    "kutuphane": "kutuphane",
    "yemekhane": "yemekhane",
    "sifre": "sifre",
    "girisi": "giris",
    "giris": "giris",
    "butunleme": "but",
    "bütünleme": "but",
    "danisman": "danisman",
}


def typoize(text: str) -> str:
    value = text.lower()
    replacements = {
        "ı": "i",
        "ö": "o",
        "ü": "u",
        "ş": "s",
        "ç": "c",
        "ğ": "g",
        "â": "a",
        "î": "i",
        " nereden ": " nerde ",
        " nasil ": " nasil ",
        " midir": " mi",
        " mıdır": " mi",
        " olur": " oluo",
        " gerekiyor": " gerekiyo",
    }
    for source, target in replacements.items():
        value = value.replace(source, target)

    for source, target in SHORTENINGS.items():
        value = value.replace(source, target)

    return value.replace(" ?", "?")


def clean_variant(stem: str, index: int) -> str:
    prefix = PREFIXES[index % len(PREFIXES)]
    suffix = SUFFIXES[index % len(SUFFIXES)]
    text = f"{prefix}{stem}{suffix}".strip()
    return text.capitalize() + "?"


def casual_variant(stem: str, index: int) -> str:
    prefix = CASUAL_PREFIXES[index % len(CASUAL_PREFIXES)]
    suffix = CASUAL_SUFFIXES[index % len(CASUAL_SUFFIXES)]
    text = f"{prefix}{stem} {suffix}".strip()
    return text.capitalize().replace("  ", " ") + "?"


def typo_variant(stem: str, index: int) -> str:
    variant = casual_variant(stem, index)
    return typoize(variant)


def plain_variant(stem: str) -> str:
    return stem.capitalize() + "?"


def campus_variant(stem: str, index: int) -> str:
    campus_prefixes = [
        "rteu'de ",
        "okulda ",
        "universitede ",
    ]
    text = f"{campus_prefixes[index % len(campus_prefixes)]}{stem}"
    return text.capitalize() + "?"


def official_variant(stem: str) -> str:
    return f"Resmi olarak {stem}?".capitalize()


def build_examples():
    random.seed(SEED)
    records = []

    for intent, stems in INTENT_STEMS.items():
        examples = []
        for index, stem in enumerate(stems):
            examples.append(plain_variant(stem))
            examples.append(clean_variant(stem, index))
            examples.append(clean_variant(stem, index + 7))
            examples.append(campus_variant(stem, index))
            examples.append(official_variant(stem))
            examples.append(casual_variant(stem, index))
            examples.append(casual_variant(stem, index + 5))
            examples.append(typo_variant(stem, index))

        # De-duplicate while preserving order.
        seen = set()
        unique_examples = []
        for text in examples:
            normalized = " ".join(text.lower().split())
            if normalized in seen:
                continue
            seen.add(normalized)
            unique_examples.append(text)

        # Keep the first 110 per intent for a 1100-sample dataset.
        trimmed = unique_examples[:110]

        for text in trimmed:
            records.append(
                {
                    "text": text,
                    "intent": intent,
                    "entities": [],
                }
            )

    random.shuffle(records)
    return records


def main():
    records = build_examples()
    OUTPUT_PATH.write_text(json.dumps(records, ensure_ascii=False, indent=2), encoding="utf-8")

    summary = {}
    for record in records:
        summary[record["intent"]] = summary.get(record["intent"], 0) + 1

    print(f"Wrote {len(records)} samples to {OUTPUT_PATH}")
    for intent in sorted(summary):
        print(f"{intent}: {summary[intent]}")


if __name__ == "__main__":
    main()
