import json
from collections import defaultdict
from pathlib import Path


ROOT = Path(__file__).resolve().parent.parent
KB_PATH = ROOT / "data" / "knowledge_base.json"


PREFIX_MAP = {
    "exam_and_grading": "exam",
    "course_registration": "registration",
    "digital_systems": "digital",
    "student_services": "services",
    "library": "library",
    "student_life": "life",
    "scholarship_support": "scholarship",
}


def load_kb():
    with open(KB_PATH, "r", encoding="utf-8-sig") as f:
        return json.load(f)


def save_kb(data):
    with open(KB_PATH, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
        f.write("\n")


def normalize_existing_entries(entries):
    normalized = []
    for entry in entries:
        item = dict(entry)
        aliases = list(item.get("intent_aliases", []))
        if item["intent"] == "cafeteria":
            item["intent"] = "student_life"
            if "cafeteria" not in aliases:
                aliases.append("cafeteria")
            if "campus_life" not in aliases:
                aliases.append("campus_life")
            if "campus_logistics" not in aliases:
                aliases.append("campus_logistics")
        item["intent_aliases"] = aliases
        normalized.append(item)
    return normalized


def build_id(existing_counters, intent):
    existing_counters[intent] += 1
    return f"kb_{PREFIX_MAP[intent]}_{existing_counters[intent]:03d}"


def next_counters(entries):
    counters = defaultdict(int)
    for entry in entries:
        intent = entry["intent"]
        counters[intent] += 1
    return counters


def add(entries, counters, *, intent, topic, question, answer, keywords, source_url, source_title,
        confidence="high", time_sensitive=False, faculty_scope="general", intent_aliases=None):
    entries.append(
        {
            "id": build_id(counters, intent),
            "intent": intent,
            "intent_aliases": intent_aliases or [],
            "topic": topic,
            "question": question,
            "answer": answer,
            "keywords": keywords,
            "source_url": source_url,
            "source_title": source_title,
            "confidence": confidence,
            "time_sensitive": time_sensitive,
            "faculty_scope": faculty_scope,
        }
    )


def main():
    raw = load_kb()
    entries = normalize_existing_entries(raw["entries"])
    counters = next_counters(entries)

    # exam_and_grading
    exam_url = "https://oidb.erdogan.edu.tr/Files/ckFiles/oidb-erdogan-edu-tr/14%20Aral%C4%B1k%202025%20PAZAR-Y%C3%B6netmelik.pdf"
    exam_title = "On Lisans ve Lisans Egitim-Ogretim ve Sinav Yonetmeligi"
    add(entries, counters, intent="exam_and_grading", topic="sinav_programi_duyurusu",
        question="Sinav programlari ne kadar once ilan edilir?",
        answer="Sinav gunu, saati ve salon bilgileri ilgili birimlerin resmi internet sayfalarinda sinav baslangic tarihinden en az iki hafta once ilan edilir.",
        keywords=["sinav programi", "iki hafta", "duyuru", "salon bilgisi"], source_url="https://ardesen.erdogan.edu.tr/Files/ckFiles/ardesen-erdogan-edu-tr/Recep%20Tayyip%20Erdo%C4%9Fan%20%C3%9Cniversitesi%20S%C4%B1nav%20Uygulama%20Esaslar%C4%B1.pdf", source_title="Sinav Uygulama Esaslari")
    add(entries, counters, intent="exam_and_grading", topic="sinav_programi_degisikligi",
        question="Sinav programi son anda degistirilebilir mi?",
        answer="Zorunlu degisikliklerde ogrencilere en az 48 saat once bilgi verilmesi esastir.",
        keywords=["sinav degisikligi", "48 saat", "program degisikligi"], source_url="https://ardesen.erdogan.edu.tr/Files/ckFiles/ardesen-erdogan-edu-tr/Recep%20Tayyip%20Erdo%C4%9Fan%20%C3%9Cniversitesi%20S%C4%B1nav%20Uygulama%20Esaslar%C4%B1.pdf", source_title="Sinav Uygulama Esaslari")
    add(entries, counters, intent="exam_and_grading", topic="gunde_iki_sinav",
        question="Ayni gun kac sinav yapilabilir?",
        answer="Genel duzende ayni yariyil veya yil programinda yer alan derslerden bir gunde en fazla iki ders sinavi yapilir.",
        keywords=["bir gunde iki sinav", "ayni gun", "sinav sayisi"], source_url=exam_url, source_title=exam_title)
    add(entries, counters, intent="exam_and_grading", topic="butunleme_takvimi",
        question="Butunleme tarihlerini nereden ogrenebilirim?",
        answer="Butunleme tarihleri universitenin ilgili akademik takviminde ve birim duyurularinda ilan edilir. Tip ve Dis Hekimligi disindaki birimler icin genel akademik takvim, Tip icin ise ayrik fakulte takvimi kullanilir.",
        keywords=["butunleme tarihi", "akademik takvim", "birim duyurusu"], source_url="https://erdogan.edu.tr/Images/Uploads/MyContents/L_6146-20250604152832789418.pdf", source_title="2025-2026 Onlisans-Lisans Akademik Takvimi", time_sensitive=True, confidence="medium")
    add(entries, counters, intent="exam_and_grading", topic="mazeret_takvimi",
        question="Mazeret sinav tarihleri nerede yayinlanir?",
        answer="Mazeret sinav tarihleri akademik takvim ve ilgili birim duyurularinda yayinlanir. Tip Fakultesi icin ayri takvim kullanilabilir.",
        keywords=["mazeret tarihi", "akademik takvim", "mazeret sinavi"], source_url="https://erdogan.edu.tr/Images/Uploads/MyContents/L_6146-20250604152832789418.pdf", source_title="2025-2026 Onlisans-Lisans Akademik Takvimi", time_sensitive=True, confidence="medium")
    add(entries, counters, intent="exam_and_grading", topic="tek_ders_takvimi",
        question="Tek ders sinavi tarihi nereden takip edilir?",
        answer="Tek ders sinavi tarihleri akademik takvimde ve OIDB veya ilgili akademik birim duyurularinda ilan edilir.",
        keywords=["tek ders tarihi", "akademik takvim", "oidb"], source_url="https://oidb.erdogan.edu.tr/tr/news-detail/tek-ders-sinavi/6418", source_title="Tek Ders Sinavi", time_sensitive=True, confidence="medium")
    add(entries, counters, intent="exam_and_grading", topic="onur_yuksek_onur",
        question="Onur ve yuksek onur ogrencisi olma kosulu nedir?",
        answer="Onur ve yuksek onur degerlendirmesi yariyil not ortalamasina gore yapilir. Kesin sinirlar ilgili yonetmelik ve birim aciklamalarina gore degerlendirilir.",
        keywords=["onur", "yuksek onur", "yano"], source_url=exam_url, source_title=exam_title, confidence="medium")
    add(entries, counters, intent="exam_and_grading", topic="not_donusum_tablosu",
        question="100'lük notumu 4'lük sisteme nasil cevirebilirim?",
        answer="Not donusumlerinde universitenin kullandigi resmi donusum tablosu ve YOK esdegerlik tablolari esas alinmalidir.",
        keywords=["not donusumu", "4'lük sistem", "100'lük sistem"], source_url="https://oidb.erdogan.edu.tr/tr/page/not-donusum-tablosu/1237", source_title="Not Donusum Tablosu", confidence="medium")
    add(entries, counters, intent="exam_and_grading", topic="sinav_sonucu_itiraz_ilk_adim",
        question="Not itirazinda once hocaya mi basvurulur?",
        answer="Ogrenci once dersin ogretim elemanindan kagidinin yeniden incelenmesini isteyebilir. Sonrasinda ilan tarihinden itibaren sure icinde yazili itiraz yoluna gidebilir.",
        keywords=["not itirazi", "hocaya basvuru", "yeniden inceleme"], source_url="https://ardesen.erdogan.edu.tr/Files/ckFiles/ardesen-erdogan-edu-tr/Recep%20Tayyip%20Erdo%C4%9Fan%20%C3%9Cniversitesi%20S%C4%B1nav%20Uygulama%20Esaslar%C4%B1.pdf", source_title="Sinav Uygulama Esaslari")
    add(entries, counters, intent="exam_and_grading", topic="tip_ders_kurulu",
        question="Tip Fakultesinde ara sinav yerine ne uygulanir?",
        answer="Tip Fakultesinde Donem I-III icin klasik vize yerine ders kurulu sinavlari uygulanir. Donem IV-V icin staj sonu sinavlari, Donem VI icin intornluk degerlendirmesi vardir.",
        keywords=["tip", "ders kurulu", "staj sonu"], source_url="https://tip.erdogan.edu.tr/Files/ckFiles/tip-erdogan-edu-tr/Ocak%202020/E%C4%9Fitim-%C3%96%C4%9Fretim%20ve%20S%C4%B1nav%20Y%C3%B6nergesi.pdf", source_title="Tip Fakultesi Egitim-Ogretim ve Sinav Yonergesi", faculty_scope="tip")
    add(entries, counters, intent="exam_and_grading", topic="tip_donem_notu",
        question="Tip Fakultesinde donem notu nasil hesaplanir?",
        answer="Tip Fakultesinde Donem I-III icin donem basari notu, ders kurulu sinavlari ortalamasinin yuzde 60'i ile final veya butunleme notunun yuzde 40'inin toplamindan olusur.",
        keywords=["tip fakultesi", "donem notu", "yuzde 60", "yuzde 40"], source_url="https://tip.erdogan.edu.tr/Files/ckFiles/tip-erdogan-edu-tr/Ocak%202020/E%C4%9Fitim-%C3%96%C4%9Fretim%20ve%20S%C4%B1nav%20Y%C3%B6nergesi.pdf", source_title="Tip Fakultesi Egitim-Ogretim ve Sinav Yonergesi", faculty_scope="tip")
    add(entries, counters, intent="exam_and_grading", topic="tip_final_muafiyet",
        question="Tip Fakultesinde finale girmeden ust doneme gecmek mumkun mu?",
        answer="Tip Fakultesinde Donem I-III'te ders kurulu ortalamasi 75 ve uzeri olan ve devam sarti saglanan ogrenci, finale girmeden ust doneme gecebilir. Not yukseltmek isterse final veya butunlemeye girebilir.",
        keywords=["tip", "75 ortalama", "finale girmeden", "ust doneme gecis"], source_url="https://tip.erdogan.edu.tr/Files/ckFiles/tip-erdogan-edu-tr/Ocak%202020/E%C4%9Fitim-%C3%96%C4%9Fretim%20ve%20S%C4%B1nav%20Y%C3%B6nergesi.pdf", source_title="Tip Fakultesi Egitim-Ogretim ve Sinav Yonergesi", faculty_scope="tip")
    add(entries, counters, intent="exam_and_grading", topic="sbf_onkosul",
        question="Saglik Bilimleri Fakultesinde uygulamali dersler icin on kosul olabilir mi?",
        answer="Evet. Saglik Bilimleri Fakultesi resmi SSS sayfasinda bazi uygulamali derslerin ve intornluk dersinin belirli on kosullara bagli oldugu acikca belirtilir.",
        keywords=["sbf", "on kosul", "uygulamali ders", "intornluk"], source_url="https://sbf.erdogan.edu.tr/tr/page/soru-cevap/4721", source_title="Saglik Bilimleri Fakultesi Sikca Sorulan Sorular", faculty_scope="sbf")
    add(entries, counters, intent="exam_and_grading", topic="ziraat_staj_sarti",
        question="Ziraat Fakultesinde staj mezuniyet acisindan onemli mi?",
        answer="Evet. Ziraat Fakultesi duyurularinda 40 is gunu staj yukumlulugu ve staj muafiyet/teslim kurallari aciklandigi icin staj mezuniyet surecinin onemli bir parcasidir.",
        keywords=["ziraat", "staj", "40 is gunu", "mezuniyet"], source_url="https://ziraat.erdogan.edu.tr/tr/news-detail/staj-basvuru-duyurusu/15650", source_title="Ziraat Fakultesi Staj Basvuru Duyurusu", faculty_scope="ziraat")

    # course_registration
    reg_url = "https://oidb.erdogan.edu.tr/tr/page/derse-kayit-kayit-yenileme/1247"
    reg_title = "Derse Kayit / Kayit Yenileme"
    add(entries, counters, intent="course_registration", topic="kayit_yenileme_tanimi",
        question="Kayit yenileme ne demektir?",
        answer="Kayit yenileme, ogrencinin ilgili yariyilda ders secerek ogrenciligini aktif tutmasi ve ders kaydini tamamlamasi islemidir.",
        keywords=["kayit yenileme", "ders secimi", "ogrencilik"], source_url=reg_url, source_title=reg_title)
    add(entries, counters, intent="course_registration", topic="ders_kaydi_giris",
        question="Ders kaydi icin hangi sisteme girmek gerekir?",
        answer="Ders kaydi icin ogrenci REBIS uzerinden Ogrenci Bilgi Sistemine girer ve ders secim islemlerini oradan yapar.",
        keywords=["ders kaydi", "rebis", "obs"], source_url="https://oidb.erdogan.edu.tr/tr/news-detail/2025-2026-egitim-ogretim-yili-bahar-yariyili-kayit-yenileme-derse-kayit-islemleri/6942", source_title="Kayit Yenileme / Derse Kayit Islemleri")
    add(entries, counters, intent="course_registration", topic="alt_donem_ders_onceligi",
        question="Ders secerken once alt donem dersleri mi alinmali?",
        answer="Kayit yenileme akisinda ogrencinin once basarisiz veya eksik alt donem derslerini dikkate almasi ve sonra ust donem derslerini secmesi beklenir.",
        keywords=["alt donem dersi", "ders secimi", "basarisiz ders"], source_url="https://oidb.erdogan.edu.tr/tr/news-detail/2025-2026-egitim-ogretim-yili-bahar-yariyili-kayit-yenileme-derse-kayit-islemleri/6942", source_title="Kayit Yenileme / Derse Kayit Islemleri", confidence="medium")
    add(entries, counters, intent="course_registration", topic="taslak_kayit",
        question="Dersleri sectikten sonra islem hemen tamamlanir mi?",
        answer="Hayir. Dersler once sisteme kaydedilir, sonra danisman onayina gonderilir ve kayit sureci danisman onayiyla tamamlanir.",
        keywords=["taslak", "danisman onayi", "kayit tamamlama"], source_url="https://oidb.erdogan.edu.tr/tr/news-detail/2025-2026-egitim-ogretim-yili-bahar-yariyili-kayit-yenileme-derse-kayit-islemleri/6942", source_title="Kayit Yenileme / Derse Kayit Islemleri")
    add(entries, counters, intent="course_registration", topic="danisman_onayi",
        question="Danisman onayi olmadan ders kaydi tamamlanmis sayilir mi?",
        answer="Hayir. Danisman onayi ders kaydinin kesinlesme asamasidir; ogrenci kaydinin tamamlandigini transkript ve sistem uzerinden kontrol etmelidir.",
        keywords=["danisman onayi", "kesinlesme", "transkript kontrolu"], source_url="https://oidb.erdogan.edu.tr/tr/news-detail/2025-2026-egitim-ogretim-yili-bahar-yariyili-kayit-yenileme-derse-kayit-islemleri/6942", source_title="Kayit Yenileme / Derse Kayit Islemleri")
    add(entries, counters, intent="course_registration", topic="mazeretli_ders_kaydi",
        question="Kayit haftasini kacirirsam mazeretli derse kayit yapabilir miyim?",
        answer="Mazeretli derse kayit islemleri universitenin ilgili duyuru ve takvimine gore ayrica ilan edilir. Basvuru resmi belge ve sure kosullarina baglidir.",
        keywords=["mazeretli kayit", "ders kaydi", "kayit haftasi"], source_url="https://oidb.erdogan.edu.tr/tr/news-detail/2025-2026-egitim-ogretim-yili-guz-yariyili-mazeretli-derse-kayit-islemleri/6565", source_title="Mazeretli Derse Kayit Islemleri", confidence="medium", time_sensitive=True)
    add(entries, counters, intent="course_registration", topic="katki_payi_kayit_iliskisi",
        question="Katki payi veya ogrenim ucreti odemesi ders kaydini etkiler mi?",
        answer="Evet. Katki payi veya ogrenim ucreti yukumlulugu bulunan ogrencilerde bu odeme derse kayit hakkini dogrudan etkiler.",
        keywords=["katki payi", "ogrenim ucreti", "ders kaydi"], source_url="https://oidb.erdogan.edu.tr/tr/page/katki-payi-ogrenim-ucretinin-yatirilmasi/1243", source_title="Katki Payi / Ogrenim Ucreti Bilgilendirmesi")
    add(entries, counters, intent="course_registration", topic="muafiyet_genel",
        question="Muafiyet basvurusu ne zaman yapilir?",
        answer="Muafiyet basvurulari fakulte veya program duzenine gore sureye baglidir. Fakultelere gore farklilik olabildigi icin ogrencinin kendi biriminin resmi duyurusunu esas almasi gerekir.",
        keywords=["muafiyet", "basvuru suresi", "fakulte"], source_url="https://sbf.erdogan.edu.tr/tr/page/soru-cevap/4721", source_title="Saglik Bilimleri Fakultesi Sikca Sorulan Sorular", confidence="medium")
    add(entries, counters, intent="course_registration", topic="sbf_muafiyet_suresi",
        question="Saglik Bilimleri Fakultesinde muafiyet basvurusu icin sure nedir?",
        answer="Saglik Bilimleri Fakultesi SSS sayfasina gore muafiyet basvurusu derslerin baslamasini izleyen bes is gunu icinde yapilmalidir.",
        keywords=["sbf", "muafiyet", "bes is gunu"], source_url="https://sbf.erdogan.edu.tr/tr/page/soru-cevap/4721", source_title="Saglik Bilimleri Fakultesi Sikca Sorulan Sorular", faculty_scope="sbf")
    add(entries, counters, intent="course_registration", topic="tip_muafiyet_suresi",
        question="Tip Fakultesinde ders veya staj muafiyeti icin sure nedir?",
        answer="Tip Fakultesi yonergesine gore ders veya staj muafiyeti icin basvuru ilk on is gunu icinde Dekanliga yapilir ve basvuru sureye baglidir.",
        keywords=["tip", "staj muafiyeti", "on is gunu"], source_url="https://tip.erdogan.edu.tr/Files/ckFiles/tip-erdogan-edu-tr/Ocak%202020/E%C4%9Fitim-%C3%96%C4%9Fretim%20ve%20S%C4%B1nav%20Y%C3%B6nergesi.pdf", source_title="Tip Fakultesi Egitim-Ogretim ve Sinav Yonergesi", faculty_scope="tip")
    add(entries, counters, intent="course_registration", topic="dgs_intibak",
        question="DGS ile gelen ogrencinin intibaki nasil belirlenir?",
        answer="Saglik Bilimleri Fakultesi resmi SSS sayfasina gore DGS ile gelen ogrencilerin intibak sinifi muafiyet ve intibak komisyonunca belirlenir.",
        keywords=["dgs", "intibak", "komisyon"], source_url="https://sbf.erdogan.edu.tr/tr/page/soru-cevap/4721", source_title="Saglik Bilimleri Fakultesi Sikca Sorulan Sorular", faculty_scope="sbf")
    add(entries, counters, intent="course_registration", topic="cap_yandal_gano",
        question="Cift anadal veya yandal basvurusu icin GANO siniri var mi?",
        answer="Evet. OIDB duyurularinda cift anadal ve yandal basvurularinda GANO'nun en az 2.71 olmasi gerektigi acikca belirtilir.",
        keywords=["cap", "yandal", "gano", "2.71"], source_url="https://oidb.erdogan.edu.tr/tr/news-detail/2025-2026-egitim-ogretim-yili-cift-anadal-yandal-programlari-basvurulari/6464", source_title="Cift Anadal / Yandal Basvurulari")
    add(entries, counters, intent="course_registration", topic="cap_yandal_ayri_kimlik",
        question="CAP veya yandal ogrencisine ayri ogrenci numarasi verilir mi?",
        answer="Kayit yenileme duyurularinda CAP ve yandal ogrencileri icin anadal programindan ayri ogrenci numarasi ve kurumsal e-posta tanimlandigi belirtilir.",
        keywords=["cap", "yandal", "ogrenci numarasi", "kurumsal e-posta"], source_url="https://oidb.erdogan.edu.tr/tr/news-detail/2025-2026-egitim-ogretim-yili-bahar-yariyili-kayit-yenileme-derse-kayit-islemleri/6942", source_title="Kayit Yenileme / Derse Kayit Islemleri")
    add(entries, counters, intent="course_registration", topic="yatay_gecis_sistemi",
        question="Yatay gecis basvurusu icin ayri bir sistem var mi?",
        answer="Evet. RTEU'de yatay gecis basvurulari icin ayri bir cevrim ici basvuru bilgi sistemi kullanilir.",
        keywords=["yatay gecis", "basvuru sistemi"], source_url="https://ogrenci.erdogan.edu.tr/YatayGecis/Login", source_title="Yatay Gecis Basvuru Bilgi Sistemi")
    add(entries, counters, intent="course_registration", topic="kayit_yenileme_tip_ayri",
        question="Tip Fakultesinde kayit yenileme takvimi ayri midir?",
        answer="Evet. Tip Fakultesi genel onlisans-lisans takviminden ayri bir akademik takvim kullanir; kayit yenileme ve ders ekleme-cikarma tarihleri bu takvimde ayri ilan edilir.",
        keywords=["tip fakultesi", "kayit yenileme", "ayri takvim"], source_url="https://erdogan.edu.tr/Images/Uploads/MyContents/L_6146-20250604152742268512.pdf", source_title="2025-2026 Tip Fakultesi Akademik Takvimi", faculty_scope="tip", time_sensitive=True)

    # digital_systems
    digital_url = "https://ekampus.erdogan.edu.tr/Login?ret=obs"
    digital_title = "Giris | E-Kampus"
    add(entries, counters, intent="digital_systems", topic="ogrenci_eposta_ogrenme",
        question="Kurumsal ogrenci e-postami nereden ogrenebilirim?",
        answer="REBIS/E-Kampus giris sayfasindaki 'Ogrenci eposta adresimi ogrenmek istiyorum' akisi kullanilarak kurumsal ogrenci e-postasi ogrenilebilir.",
        keywords=["ogrenci e-posta", "eposta ogrenme", "rebis"], source_url=digital_url, source_title=digital_title)
    add(entries, counters, intent="digital_systems", topic="ogrenci_numarasi_ogrenme",
        question="Ogrenci numarami internetten ogrenebilir miyim?",
        answer="Evet. REBIS/E-Kampus giris ekranindaki ogrenci numarasi ogrenme baglantisi veya ogrenci sorgulama ekrani kullanilarak ogrenci numarasi ogrenilebilir.",
        keywords=["ogrenci numarasi", "sorgulama", "e-kampus"], source_url=digital_url, source_title=digital_title)
    add(entries, counters, intent="digital_systems", topic="varsayilan_sifre_formulu",
        question="Ilk varsayilan sifre nasil olusur?",
        answer="Varsayilan sifre genel olarak T.C. kimlik numarasinin ilk dort hanesi ile ogrenci numarasinin son dort hanesinin birlestirilmesiyle olusturulur.",
        keywords=["varsayilan sifre", "ilk sifre", "tc ilk 4", "ogrenci no son 4"], source_url="https://oidb.erdogan.edu.tr/Files/ckFiles/oidb-erdogan-edu-tr/eposta-ve-sifre-olusturma-islemleri.pdf", source_title="E-posta ve Sifre Olusturma Islemleri")
    add(entries, counters, intent="digital_systems", topic="sifre_unuttum_mobil",
        question="Sifremi unuttugumda yeni sifre nasil alirim?",
        answer="Guncel kilavuza gore kurumsal e-posta adresi ve sistemde kayitli cep telefonu kullanilarak yeni sifre olusturulabilir; yeni sifre kayitli cep telefonuna gonderilir.",
        keywords=["sifre unuttum", "cep telefonu", "yeni sifre"], source_url="https://oidb.erdogan.edu.tr/Files/ckFiles/oidb-erdogan-edu-tr/eposta-ve-sifre-olusturma-islemleri.pdf", source_title="E-posta ve Sifre Olusturma Islemleri")
    add(entries, counters, intent="digital_systems", topic="eposta_formati",
        question="Kurumsal e-posta adresi hangi formatta olur?",
        answer="RTEU Kimlik Dogrulama Servisi sayfasina gore ogrenci e-postasi ad_soyad ve verilen yil mantigiyla @erdogan.edu.tr uzantisinda tanimlanir.",
        keywords=["eposta formati", "ad soyad", "@erdogan.edu.tr"], source_url="https://bidb.erdogan.edu.tr/tr/page/rteu-kimlik-dogrulama-servisi/1518", source_title="RTEU Kimlik Dogrulama Servisi")
    add(entries, counters, intent="digital_systems", topic="office365_giris",
        question="Office 365'e hangi hesapla girilir?",
        answer="Office 365'e REBIS'te tanimli kurumsal @erdogan.edu.tr uzantili hesapla girilir.",
        keywords=["office 365", "giris", "kurumsal hesap"], source_url="https://bidb.erdogan.edu.tr/tr/page/office-365/1808", source_title="Office 365")
    add(entries, counters, intent="digital_systems", topic="office365_lisans",
        question="Office 365 ogrenciler icin lisansli mi?",
        answer="Evet. Aktif ogrencilere belirli periyotlarla Microsoft 365 lisansi atanir ve hizmet aktif ogrencilik suresince kullanilir.",
        keywords=["office 365", "lisans", "aktif ogrenci"], source_url="https://bidb.erdogan.edu.tr/tr/page/office-365/1808", source_title="Office 365")
    add(entries, counters, intent="digital_systems", topic="office365_cihaz_sayisi",
        question="Microsoft 365 ayni anda kac cihazda kullanilabilir?",
        answer="Resmi Office 365 sayfasina gore hesap bes cihaza kadar kullanilabilir.",
        keywords=["microsoft 365", "bes cihaz", "cihaz siniri"], source_url="https://bidb.erdogan.edu.tr/tr/page/office-365/1808", source_title="Office 365")
    add(entries, counters, intent="digital_systems", topic="microsoft_hesap_islemleri",
        question="Microsoft 365 parolasi REBIS icinden yonetilebilir mi?",
        answer="Evet. REBIS icindeki Microsoft Office Hesap Islemleri uygulamasi uzerinden ilk aktivasyon ve parola degisimi yapilabilir.",
        keywords=["microsoft office hesap islemleri", "parola", "rebis"], source_url="https://bidb.erdogan.edu.tr/tr/page/microsoft-office-365-hesap-olusturma-ve-sifre-sifirlama/1810", source_title="Microsoft Office 365 Hesap Olusturma ve Sifre Sifirlama")
    add(entries, counters, intent="digital_systems", topic="kampus_internet_giris",
        question="Kampus icinde internete nasil giris yapilir?",
        answer="Kampus icinde ag baglantisi kurulduktan sonra tarayici uzerinden universitenin tahsis ettigi kurumsal e-posta ve sifre ile kimlik dogrulama yapilir.",
        keywords=["kampus internet", "kimlik dogrulama", "kurumsal e-posta"], source_url="https://bidb.erdogan.edu.tr/tr/page/internet-erisimi/1672", source_title="Internet Erisimi")
    add(entries, counters, intent="digital_systems", topic="eduroam_giris",
        question="Eduroam icin hangi bilgileri kullanmak gerekir?",
        answer="Eduroam baglantisinda kullanici adi olarak kurumsal e-posta adresi, parola olarak ise kurumsal e-posta sifresi kullanilir.",
        keywords=["eduroam", "kullanici adi", "kurumsal e-posta"], source_url="https://bidb.erdogan.edu.tr/tr/page/eduroam-baglanti-ayarlari/1677", source_title="Eduroam Baglanti Ayarlari")
    add(entries, counters, intent="digital_systems", topic="rteunet_kullanim",
        question="RTEU.Net kimler icin kullanilir?",
        answer="Wireless baglanti ayarlarina gore RTEU.Net baglantisi universite ogrenci ve personeli icin kullanilir.",
        keywords=["rteu.net", "ogrenci", "personel", "wireless"], source_url="https://bidb.erdogan.edu.tr/tr/page/wireless-baglanti-ayarlari/1678", source_title="Wireless Baglanti Ayarlari")
    add(entries, counters, intent="digital_systems", topic="vpn_istemci",
        question="VPN baglantisi icin hangi uygulama kullanilir?",
        answer="BIDB sayfasina gore VPN baglantisi icin FortiClient VPN istemcisi kullanilir.",
        keywords=["vpn", "forticlient", "istemci"], source_url="https://bidb.erdogan.edu.tr/tr/page/vpn-ayarlari/1680", source_title="VPN Ayarlari")
    add(entries, counters, intent="digital_systems", topic="kimlik_dogrulama_kullanici_adi",
        question="RTEU Kimlik Dogrulama Servisi'nde kullanici adina alan adi eklenir mi?",
        answer="Hayir. RTEU Kimlik Dogrulama Servisi'nde kullanici adi REBIS hesabi esas alinir ve @erdogan.edu.tr eklenmeden kullanilir.",
        keywords=["kimlik dogrulama", "kullanici adi", "@erdogan.edu.tr"], source_url="https://bidb.erdogan.edu.tr/tr/page/rteu-kimlik-dogrulama-servisi/1518", source_title="RTEU Kimlik Dogrulama Servisi")
    add(entries, counters, intent="digital_systems", topic="teknik_destek_hatlari",
        question="E-posta veya internet sorunu icin hangi teknik destek hatlari kullanilir?",
        answer="Genel erisim problemleri sayfasinda e-posta ve parola sorunlari icin Web grubu, internet erisimi icin ise Sistem ve Network grubu iletisim numaralari verilir.",
        keywords=["teknik destek", "web grubu", "network grubu"], source_url="https://bidb.erdogan.edu.tr/tr/page/genel-erisim-problemleri/1684", source_title="Genel Erisim Problemleri", confidence="medium")
    add(entries, counters, intent="digital_systems", topic="bidb_ve_oidb_rol_farki",
        question="REBIS ve Ogrenci Bilgi Sisteminden hangi birimler sorumludur?",
        answer="Uygulamalar ve sorumlu birim listesine gore REBIS icin Bilgi Islem Daire Baskanligi, Ogrenci Bilgi Sistemi icin ise Ogrenci Isleri Daire Baskanligi sorumludur.",
        keywords=["bidb", "oidb", "rebis", "ogrenci bilgi sistemi"], source_url="https://bidb.erdogan.edu.tr/tr/page/uygulamalar-ve-sorumlu-birim-personel/1807", source_title="Uygulamalar ve Sorumlu Birim/Personel")

    # student_services
    add(entries, counters, intent="student_services", topic="ogrenci_belgesi_kaynaklari",
        question="Ogrenci belgesi hangi kanallardan alinabilir?",
        answer="Ogrenci belgesi akademik birim ogrenci islerinden, OIDB'den veya e-Devlet uzerinden alinabilir.",
        keywords=["ogrenci belgesi", "e-devlet", "oidb"], source_url="https://oidb.erdogan.edu.tr/tr/page/ogrenci-belgesi/1182", source_title="Ogrenci Belgesi")
    add(entries, counters, intent="student_services", topic="transkript_ucret",
        question="Transkript almak ucretli midir?",
        answer="Hayir. OIDB sayfasina gore transkript veya not dokum belgesi icin ucret alinmaz.",
        keywords=["transkript", "ucret", "not dokum"], source_url="https://oidb.erdogan.edu.tr/tr/page/transkript-not-dokum-belgesi/1184", source_title="Transkript (Not Dokum Belgesi)")
    add(entries, counters, intent="student_services", topic="gecici_mezuniyet_belgesi",
        question="Diploma hazir degilse hangi belge alinabilir?",
        answer="Diploma duzenlenmeden once mezuniyet hakki kazanan ogrenciye gecici mezuniyet belgesi duzenlenebilir.",
        keywords=["gecici mezuniyet", "diploma hazir degil"], source_url="https://oidb.erdogan.edu.tr/tr/page/diploma/1188", source_title="Diploma")
    add(entries, counters, intent="student_services", topic="diploma_sorgulama_bilgi_seti",
        question="Diploma sorgulamada hangi bilgiler istenir?",
        answer="Diploma sorgulama ekraninda ogrenci numarasi, T.C. kimlik numarasi ve aile bilgileri veya T.C. kimlik numarasi ile diploma numarasi kullanilabilir.",
        keywords=["diploma sorgulama", "ogrenci numarasi", "diploma numarasi"], source_url="https://ogrenci.erdogan.edu.tr/OgrenciDiplomaSorgulama/Index", source_title="Diploma Sorgulama")
    add(entries, counters, intent="student_services", topic="diploma_teslim_hazir",
        question="Diploma 'teslime hazir' oldugunda ne yapabilirim?",
        answer="Diploma teslime hazir oldugunda ogrenci diplomasini sahsen, noter onayli vekaletle veya Diploma Talep Sistemi uzerinden kargo talebiyle alabilir.",
        keywords=["teslime hazir", "diploma talep", "vekalet"], source_url="https://ogrenci.erdogan.edu.tr/DiplomaTalep/Login", source_title="Mezun Diploma Talep ve Surec Takip Uygulamasi")
    add(entries, counters, intent="student_services", topic="diploma_eki_otomatik",
        question="Diploma eki otomatik mi verilir?",
        answer="OIDB sayfasina gore diploma eki mezunlara otomatik ve ucretsiz olarak, Ingilizce dilinde verilir.",
        keywords=["diploma eki", "otomatik", "ingilizce"], source_url="https://oidb.erdogan.edu.tr/tr/page/diploma-eki/1190", source_title="Diploma Eki")
    add(entries, counters, intent="student_services", topic="asli_gibidir_onayi",
        question="Asli gibidir onayi nerede yaptirilir?",
        answer="Asli gibidir onayi icin belgenin asli ibraz edilerek OIDB veya ilgili birimde fotokopinin belgeyle uyumunun onaylanmasi gerekir.",
        keywords=["asli gibidir", "onay", "oidb"], source_url="https://oidb.erdogan.edu.tr/tr/page/asli-gibidir/1181", source_title="Asli Gibidir", confidence="medium")
    add(entries, counters, intent="student_services", topic="kendi_istegiyle_ilisik_kesme",
        question="Kendi istegimle kaydimi sildirmek istersem ne yapmaliyim?",
        answer="Kendi istegiyle ilisik kesmek isteyen ogrenci ilgili formu doldurup ogrenci kimlik kartiyla birlikte bagli oldugu akademik birime sahsen basvurur.",
        keywords=["ilisik kesme", "kayit sildirme", "form"], source_url="https://oidb.erdogan.edu.tr/tr/page/kendi-istegi-ile-ilisik-kesme/1179", source_title="Kendi Istegi Ile Ilisik Kesme")
    add(entries, counters, intent="student_services", topic="ogrenci_durum_sorgulama",
        question="Ogrenci durum sorgulamada hangi bilgiler gorulur?",
        answer="Ogrenci durum sorgulama ekraninda ad-soyad, ogrenci numarasi, durum, durum detay ve kayitla ilgili temel bilgiler goruntulenir.",
        keywords=["ogrenci durum sorgulama", "durum detay", "ogrenci numarasi"], source_url="https://ogrenci.erdogan.edu.tr/OgrenciSorgulama/Index", source_title="Ogrenci Durum Sorgulama")
    add(entries, counters, intent="student_services", topic="odk_kurulus",
        question="Ogrenci Destek Koordinatorlugu ne zaman kuruldu?",
        answer="Ogrenci Destek Koordinatorlugu 22 Ocak 2025 tarihli ve 222 sayili Senato karariyla kurulmustur.",
        keywords=["odk", "22 ocak 2025", "222 sayili"], source_url="https://ziraat.erdogan.edu.tr/tr/news-detail/t-c-recep-tayyip-erdogan-universitesi-ogrenci-destek-koordinatorlugu-ogrenci-senato-uyeleri-belirlendi/15359", source_title="ODK Ogrenci Senato Uyeleri Belirlendi")
    add(entries, counters, intent="student_services", topic="odk_amaci",
        question="Ogrenci Destek Koordinatorlugu ne is yapar?",
        answer="ODK'nin amaci ogrencilerin akademik, sosyal ve kisisel gelisimlerini desteklemek; sorunlari tespit edip cozum odakli mekanizmalar gelistirmektir.",
        keywords=["ogrenci destek koordinatorlugu", "akademik sosyal kisisel gelisim"], source_url="https://odk.erdogan.edu.tr/tr/page/amac-ve-faaliyet-alani/5278", source_title="ODK Amac ve Faaliyet Alani")
    add(entries, counters, intent="student_services", topic="eimer_kanali",
        question="Ogrenci sorun ve sikayetleri icin resmi geri bildirim kanali var mi?",
        answer="ODK iletisim sayfasinda dilek, gorus, memnuniyet ve sikayetlerin E-IMER sistemi uzerinden iletilebilecegi belirtilir.",
        keywords=["e-imer", "sikayet", "geri bildirim"], source_url="https://odk.erdogan.edu.tr/tr/page/iletisim/5279", source_title="ODK Iletisim", confidence="medium")
    add(entries, counters, intent="student_services", topic="engelli_ogrenci_birimi",
        question="Engelli ogrenciler icin resmi destek birimi var mi?",
        answer="Evet. Engelli Ogrenci Birimi resmi olarak SKS bunyesinde yer alir ve erisilebilirlik ile ogrenci destek koordinasyonunu yurutur.",
        keywords=["engelli ogrenci", "sks", "erisilebilirlik"], source_url="https://eob.erdogan.edu.tr", source_title="Engelli Ogrenci Birimi")
    add(entries, counters, intent="student_services", topic="rpduam_hizmeti",
        question="Psikolojik danismanlik hizmetini hangi birim verir?",
        answer="Psikolojik danismanlik hizmeti resmi olarak Rehberlik ve Psikolojik Danisma Uygulama ve Arastirma Merkezi tarafindan sunulur.",
        keywords=["psikolojik danismanlik", "rpduam", "rehberlik"], source_url="https://erdogan.edu.tr/Website/Contents.aspx?PageID=391&LangID=1", source_title="RPDUAM")
    add(entries, counters, intent="student_services", topic="rpduam_kapsam",
        question="RPDUAM kimlere hizmet verir?",
        answer="Universite ana tanitim metinlerine gore RPDUAM universite ogrencileri ve personeline rehberlik ve psikolojik danisma hizmeti verir.",
        keywords=["rpduam", "ogrenciler", "personel"], source_url="https://erdogan.edu.tr/Website/Contents.aspx?PageID=391&LangID=1", source_title="RPDUAM")

    # library
    lib_url = "https://kutuphanedb.erdogan.edu.tr/tr/page/sikca-sorulan-sorular/1194"
    lib_title = "Kutuphane Sikca Sorulan Sorular"
    add(entries, counters, intent="library", topic="odunc_sayi_sure",
        question="Lisans ogrencisi kac kitap ve kac gun odunc alabilir?",
        answer="Kutuphane SSS sayfasina gore lisans ve onlisans ogrencileri bir seferde 4 kitabi 15 gun sureyle odunc alabilir.",
        keywords=["4 kitap", "15 gun", "odunc"], source_url=lib_url, source_title=lib_title)
    add(entries, counters, intent="library", topic="odunc_uzatma_kosullari",
        question="Kutuphane kitabinin suresi hangi kosullarda uzatilabilir?",
        answer="Uzatma iade tarihinden en fazla 3 gun once baslar ve her kitap icin en fazla 2 kez yapilabilir. Kitap rezerve edilmemis olmali, borc bulunmamali ve sure gecmemis olmalidir.",
        keywords=["uzatma", "3 gun once", "2 kez", "rezervasyon"], source_url=lib_url, source_title=lib_title)
    add(entries, counters, intent="library", topic="kitap_iade_yeri",
        question="Odunc alinan kitabi nereye iade etmek gerekir?",
        answer="Odunc alinan materyal odunc verme bankosuna veya odunc alindigi yere iade edilmelidir.",
        keywords=["iade", "odunc verme bankosu"], source_url=lib_url, source_title=lib_title)
    add(entries, counters, intent="library", topic="kargo_ile_iade",
        question="Rize disindayken kutuphane kitabini kargoyla iade edebilir miyim?",
        answer="Evet. SSS sayfasina gore Rize disinda bulunan kullanicilar sorumluluk kendilerine ait olmak uzere kitaplari kargoyla iade edebilir.",
        keywords=["kargo ile iade", "rize disi"], source_url=lib_url, source_title=lib_title)
    add(entries, counters, intent="library", topic="gecikme_cezasi_ve_engel",
        question="Kutuphane gecikme cezasi yeni odunc islemini engeller mi?",
        answer="Evet. Gecikme cezasi odenmeden yeni kitap odunc alma veya sure uzatma islemi yapilamaz.",
        keywords=["gecikme cezasi", "borc", "odunc engeli"], source_url=lib_url, source_title=lib_title)
    add(entries, counters, intent="library", topic="gecikme_yuz_gun",
        question="Kutuphane gecikme cezasi sinirsiz mi isler?",
        answer="Kutuphane yonergesi ozetine gore gecikme cezasi uygulamasi yuz gunle sinirlandirilir; kayip bildirimi yapildigi tarihten itibaren ceza durdurulur.",
        keywords=["100 gun", "kayip bildirimi", "gecikme cezasi"], source_url="https://kutuphanedb.erdogan.edu.tr/Files/ckFiles/kutuphane-idari-erdogan-edu-tr/K%C3%9CT%C3%9CPHANE%20H%C4%B0ZMETLER%C4%B0%20Y%C3%96NERGES%C4%B0.pdf", source_title="Kutuphane Hizmetleri Yonergesi", confidence="medium")
    add(entries, counters, intent="library", topic="tez_ve_dergi_odunc",
        question="Tezler ve dergiler odunc verilir mi?",
        answer="Hayir. SSS sayfasina gore tezler, dergiler ve referans kaynaklari odunc verilmez; kutuphane icinde kullanilir.",
        keywords=["tez", "dergi", "odunc verilmez"], source_url=lib_url, source_title=lib_title)
    add(entries, counters, intent="library", topic="misafir_kullanici_hakki",
        question="RTEU mensubu olmayan biri kutuphaneden kitap odunc alabilir mi?",
        answer="Hayir. Misafir kullanicilar kutuphaneyi arastirma amacli kullanabilir ve basili kaynaklardan fotokopi yoluyla yararlanabilir; odunc haklari yoktur.",
        keywords=["misafir kullanici", "odunc alabilir mi", "fotokopi"], source_url=lib_url, source_title=lib_title)
    add(entries, counters, intent="library", topic="kutuphane_hesabi_islevleri",
        question="Kutuphane hesabi uzerinden hangi islemler yapilabilir?",
        answer="Kutuphane hesabi uzerinden uzerinizdeki kitaplari, iade tarihlerini, borclari ve ayirtilan eserleri goruntuleyebilir; sure uzatma ve ayirtma islemleri yapabilirsiniz.",
        keywords=["kutuphane hesabi", "borclarim", "kitaplarim", "sure uzatma"], source_url=lib_url, source_title=lib_title)
    add(entries, counters, intent="library", topic="bireysel_calisma_odasi_sure",
        question="Bireysel calisma odalari ne kadar sureyle kullanilabilir?",
        answer="Perplexity arastirma derlemesine gore bireysel calisma odalari mesai icinde 3 saat, mesai disinda 5 saat kullanilabilir; kullanimda kimlik birakma ve kurallara uyma esastir.",
        keywords=["bireysel calisma odasi", "3 saat", "5 saat"], source_url="https://kutuphanedb.erdogan.edu.tr/tr/page/bireysel-calisma-odasi/1152", source_title="Bireysel Calisma Odasi", confidence="medium")
    add(entries, counters, intent="library", topic="vetis_uzaktan_erisim",
        question="Ogrenci kampus disindan kutuphane veritabanlarina nasil erisir?",
        answer="Ogrenciler icin kampus disi erisimde VETIS sistemi one cikarilir. Elektronik kutuphane ve kampus disi erisim sayfalari bu yontemi resmi yol olarak listeler.",
        keywords=["vetis", "kampus disi", "ogrenci"], source_url="https://kutuphanedb.erdogan.edu.tr/tr/page/kampus-disi-erisim-olanaklari/2947", source_title="Kampus Disi Erisim Olanaklari", confidence="medium")
    add(entries, counters, intent="library", topic="proxy_ogrenci_belirsizligi",
        question="Proxy erisimi ogrenciler icin kesin olarak acik mi?",
        answer="Kutuphane ve BIDB kaynaklarinda proxy erisimi anlatilsa da bazi aciklamalarda bunun daha cok akademik ve idari personel icin oldugu belirtilir. Ogrenciler icin VETIS daha net resmi yol olarak gorunur.",
        keywords=["proxy", "ogrenci", "vetis"], source_url="https://bidb.erdogan.edu.tr/tr/page/kampus-disi-erisim-bilgilendirme/1681", source_title="Kampus Disi Erisim Bilgilendirme", confidence="medium")

    # scholarship_support
    burs_url = "https://erdogan.edu.tr/Images/Uploads/Vak%C4%B1f%20Akademik%20Te%C5%9Fvik%20Y%C3%B6nergesi%20(2).pdf"
    burs_title = "RTEU Gelistirme Vakfi Burs ve Tesvik Yonergesi"
    add(entries, counters, intent="scholarship_support", topic="vakif_burs_turleri",
        question="RTEU Gelistirme Vakfi hangi burs turlerini tanimlar?",
        answer="Gelistirme Vakfi yonergesinde tesvik burslari, ihtiyac burslari ve basari odulleri gibi ogrenciye yonelik destekler tanimlanir.",
        keywords=["gelistirme vakfi", "ihtiyac bursu", "tesvik bursu", "basari odulu"], source_url=burs_url, source_title=burs_title)
    add(entries, counters, intent="scholarship_support", topic="vakif_komisyon",
        question="Vakit burslarinda karar mekanizmasi nasil isler?",
        answer="Yonergeye gore Yurt-Burs-Ogrenci Isleri Komisyonu ogrencilerle ilgili burs kararlarini onerir; uygulama Mu tevelli Heyeti onayiyla yurumeye girer.",
        keywords=["komisyon", "mutevelli heyeti", "vakif burs"], source_url=burs_url, source_title=burs_title)
    add(entries, counters, intent="scholarship_support", topic="yemek_bursu_duyuru_kanali",
        question="Yemek bursu basvurulari nerede duyurulur?",
        answer="Yemek bursu basvurulari SKS duyurular sayfasinda resmi olarak ilan edilir.",
        keywords=["yemek bursu", "sks duyurular"], source_url="https://sks.erdogan.edu.tr/tr/news", source_title="SKS Duyurular")
    add(entries, counters, intent="scholarship_support", topic="yemek_bursu_vakif",
        question="Yemek bursunu hangi kurum saglar?",
        answer="SKS duyurularinda yemek bursunun RTEU Gelistirme Vakfi tarafindan saglandigi belirtilir.",
        keywords=["yemek bursu", "gelistirme vakfi"], source_url="https://sks.erdogan.edu.tr/tr/news-detail/yemek-bursu-basvurulari-2025/6545", source_title="Yemek Bursu Basvurulari - 2025")
    add(entries, counters, intent="scholarship_support", topic="yemek_bursu_kapsami",
        question="Yemek bursu ogrenciye ne saglar?",
        answer="Yemek bursu, hak kazanan ogrenciye yemekhanelerde gunde bir ogun ucretsiz yemek imkani saglayan destek turudur.",
        keywords=["gunde bir ogun", "ucretsiz yemek", "yemek bursu"], source_url="https://sks.erdogan.edu.tr/tr/news-detail/yemek-bursu-basvurulari-2025/6545", source_title="Yemek Bursu Basvurulari - 2025")
    add(entries, counters, intent="scholarship_support", topic="yemek_bursu_kesilme",
        question="Yemek bursu hangi durumda kesilebilir?",
        answer="Erisebilen resmi kurallara gore ogrencinin yemek kartini baskasina kullandirmasi veya ay icinde on defadan az yemek yemesi durumunda yemek bursu kesilebilir.",
        keywords=["yemek bursu kesilir mi", "karti baskasina kullandirma", "10 defa"], source_url="https://sks.erdogan.edu.tr/Files/ckFiles/sks-idari-erdogan-edu-tr/Normal/yemek%20bursu2022.pdf", source_title="Yemek Bursu 2022 Kurallari", confidence="medium")
    add(entries, counters, intent="scholarship_support", topic="barinma_yardimi",
        question="Universite resmi belgelerinde barinma yardimi geciyor mu?",
        answer="SKS faaliyet raporunda ogrencilere yemek yardimi ve barinma yardimi gibi destekler saglandigi acikca ifade edilir.",
        keywords=["barinma yardimi", "sks faaliyet raporu"], source_url="https://sks.erdogan.edu.tr/Files/ckFiles/sks-erdogan-edu-tr/Faaliyet%20Raporlar%C4%B1%20ve%20Mevzuat/2025%20B%C4%B0R%C4%B0M_FAL%20RAPORU.pdf", source_title="SKS 2025 Birim Faaliyet Raporu")
    add(entries, counters, intent="scholarship_support", topic="ogrenim_bursu_duyurusu",
        question="Ogrenim bursu icin resmi duyuru yayimlaniyor mu?",
        answer="Evet. OIDB ve universite ana sayfasinda ogrenim bursu basvuru duyurulari yayinlanir.",
        keywords=["ogrenim bursu", "oidb duyuru"], source_url="https://oidb.erdogan.edu.tr/tr/news-detail/ogrenim-bursu-basvuru-duyurusu/6646", source_title="Ogrenim Bursu Basvuru Duyurusu")
    add(entries, counters, intent="scholarship_support", topic="basari_bursu_osym",
        question="RTEU'de basari bursu var mi?",
        answer="Universitenin resmi basari bursu duyurusunda OSMY yerlestirme basarisina gore belirli ogrencilere nakdi basari bursu verilecegi belirtilir.",
        keywords=["basari bursu", "osym", "nakdi burs"], source_url="https://erdogan.edu.tr/Website/Contents.aspx?PageID=396&LangID=1", source_title="Basari Bursu Firsati", confidence="medium")
    add(entries, counters, intent="scholarship_support", topic="kyk_basvuru_kanali",
        question="KYK burs veya kredi basvurusu universite icinden mi yapilir?",
        answer="Hayir. OIDB belgelerine gore KYK burs ve kredi basvurulari dogrudan KYK'nin resmi internet sistemi uzerinden yapilir; universite bilgilendirme ve durum bildirimi yapar.",
        keywords=["kyk", "basvuru", "universite icinden mi"], source_url="https://oidb.erdogan.edu.tr/Files/ckFiles/oidb-erdogan-edu-tr/KATKI%20PAYI%20%C3%96%C4%9EREN%C4%B0M%20%C3%9CCRET%C4%B0%20ALINMASI%20HAKKINDA%20B%C4%B0LG%C4%B0LER.pdf", source_title="Katki Payi / Ogrenim Ucreti Bilgilendirmesi")
    add(entries, counters, intent="scholarship_support", topic="oidb_kyk_rolu",
        question="OIDB'nin KYK burs surecinde rolu var midir?",
        answer="OIDB'nin is planlarinda KYK burs veya kredi alan ogrencilerin basari durumlarini izleme ve burs kontenjan yazilarini ilgili komisyonlara iletme gorevleri tanimlanir.",
        keywords=["oidb", "kyk", "basari durumu"], source_url="https://oidb.erdogan.edu.tr/Files/Images/hassas-gorev-tespit-2312023132756.pdf", source_title="Hassas Gorev Tespit Dokumani", confidence="medium")
    add(entries, counters, intent="scholarship_support", topic="dis_burs_duyurulari",
        question="TEV gibi dis burslar resmi kanallarda duyuruluyor mu?",
        answer="Evet. TEV ve benzeri dis kaynak burslar universitenin veya fakulte duyuru sayfalarinda resmi olarak ilan edilip ilgili vakfin basvuru sayfasina yonlendirme yapilir.",
        keywords=["tev", "dis burs", "duyuru"], source_url="https://tde-fef.erdogan.edu.tr/tr/news-detail/2025-2026-ogretim-yili-tev-universite-egitim-burs-basvuru-duyurusu/16540", source_title="TEV Universite Egitim Bursu Basvuru Duyurusu", confidence="medium")

    # student_life
    life_aliases = ["cafeteria", "campus_life", "campus_logistics"]
    add(entries, counters, intent="student_life", intent_aliases=life_aliases, topic="sks_gorev_alani",
        question="Ogrenci yasami ile ilgili resmi hizmetleri hangi birim yurutur?",
        answer="Ogrenci yasamina dair beslenme, spor, topluluklar, sosyal hizmetler ve benzeri hizmetlerin ana kurumsal muhatabi SKS'dir.",
        keywords=["sks", "ogrenci yasami", "sosyal hizmetler"], source_url="https://sks.erdogan.edu.tr", source_title="SKS Ana Sayfasi")
    add(entries, counters, intent="student_life", intent_aliases=life_aliases, topic="yemek_listesi_sayfasi",
        question="Gunluk yemek listesi nerede yayinlanir?",
        answer="Gunluk yemek listesi universitenin resmi Yemek Listesi sayfasinda kampus ve birim bazinda yayinlanir.",
        keywords=["yemek listesi", "gunluk menu", "kampus bazli"], source_url="https://erdogan.edu.tr/Website/Contents.aspx?PageID=1156&LangID=1", source_title="Yemek Listesi", time_sensitive=True)
    add(entries, counters, intent="student_life", intent_aliases=life_aliases, topic="yemek_rezervasyon_sistemi",
        question="Universitede yemekhane rezervasyon sistemi var mi?",
        answer="Evet. SKS duyurularinda Yemekhane Rezervasyon Sistemi ve ilgili bilgilendirme duyurulari resmi olarak yer alir.",
        keywords=["yemekhane rezervasyon sistemi", "sks duyuru"], source_url="https://sks.erdogan.edu.tr/tr/news-detail/yemekhane-rezervasyon-sistemi/4787", source_title="Yemekhane Rezervasyon Sistemi", confidence="medium")
    add(entries, counters, intent="student_life", intent_aliases=life_aliases, topic="rezervasyon_zorunlulugu",
        question="Yemek hizmeti icin rezervasyon yapmak gerekiyor mu?",
        answer="Arastirma havuzundaki resmi duyurular rezervasyon sisteminin aktif oldugunu ve ucretsiz yemek uygulamasinda rezervasyon vurgusunu gosterir. Ancak tum ogrenciler icin zorunluluk ayrintisi her sayfada ayni aciklikta yazili degildir.",
        keywords=["rezervasyon zorunlu mu", "ucretsiz yemek", "yemek sistemi"], source_url="https://sks.erdogan.edu.tr/tr/news-detail/yemekhane-rezervasyon-sistemi-bilgilendirme/5397", source_title="Yemekhane Rezervasyon Sistemi Bilgilendirme", confidence="medium", time_sensitive=True)
    add(entries, counters, intent="student_life", intent_aliases=life_aliases, topic="para_yukleme",
        question="Yemekhane kartina online para yuklenebilir mi?",
        answer="SKS hizli erisim baglantilarinda Para Yukleme ve Yemekhane Kartina RTEU Mobilden Para Yukleme secenekleri bulundugu icin online veya mobil bakiye yukleme imkani vardir.",
        keywords=["para yukleme", "rteu mobilden", "yemekhane karti"], source_url="https://sks.erdogan.edu.tr", source_title="SKS Ana Sayfasi", confidence="medium")
    add(entries, counters, intent="student_life", intent_aliases=life_aliases, topic="barinma_bologna",
        question="Barinma bilgileri icin resmi sayfa var mi?",
        answer="Universitenin Bologna bilgi paketinde barinma basligi altinda genel bilgilendirme yer alir. Ancak detayli yurt listeleri ve fiyat bilgileri ayni duzeyde acik olmayabilir.",
        keywords=["barinma", "bologna", "yurt bilgisi"], source_url="https://bologna2.erdogan.edu.tr/tr/page/barinma/3497", source_title="Barinma", confidence="medium")
    add(entries, counters, intent="student_life", intent_aliases=life_aliases, topic="kyk_yurt_yonlendirme",
        question="Yurt basvurulari universite icinden mi yapilir?",
        answer="Barinma kaynaklarinda ilk kayitlanan ogrenciler icin yurt basvurularinin KYK sistemi uzerinden yurudugune yonelik resmi yonlendirmeler bulunur.",
        keywords=["yurt basvurusu", "kyk", "barinma"], source_url="https://bologna2.erdogan.edu.tr/tr/page/barinma/3497", source_title="Barinma", confidence="medium")
    add(entries, counters, intent="student_life", intent_aliases=life_aliases, topic="ulasim_sayfasi",
        question="Kampus veya sehir ici ulasim bilgileri icin resmi bir sayfa var mi?",
        answer="Universitenin Bologna bilgi paketi icinde ulasim basligi altinda resmi genel bilgilendirme sayfasi bulunur.",
        keywords=["ulasim", "kampus", "bologna"], source_url="https://bologna2.erdogan.edu.tr/tr/page/ulasim/3499", source_title="Ulasim", confidence="medium")
    add(entries, counters, intent="student_life", intent_aliases=life_aliases, topic="topluluklar_listesi",
        question="Aktif ogrenci topluluklarinin listesine nereden ulasabilirim?",
        answer="SKS tarafindan yayinlanan RTEU aktif ogrenci topluluklari listesi ve iletisim bilgileri sayfasindan topluluklara ulasilabilir.",
        keywords=["ogrenci topluluklari", "aktif liste", "iletisim"], source_url="https://sks.erdogan.edu.tr/tr/page/rteu-topluluklar-listesi-ve-iletisim-bilgileri/6088", source_title="RTEU Topluluklar Listesi ve Iletisim Bilgileri")
    add(entries, counters, intent="student_life", intent_aliases=life_aliases, topic="topluluk_yonergesi",
        question="Ogrenci topluluklarinin resmi yonergesi var mi?",
        answer="Evet. Ogrenci topluluklarinin kurulus, isleyis ve denetim esaslarini duzenleyen resmi yonerge bulunur.",
        keywords=["topluluk yonergesi", "kurulus", "isleyis"], source_url="https://ilahiyat.erdogan.edu.tr/Files/ckFiles/ilahiyat-erdogan-edu-tr/ogrenci-topluluklari-yonergesi_18-03-2015.pdf", source_title="Ogrenci Topluluklari Yonergesi")
    add(entries, counters, intent="student_life", intent_aliases=life_aliases, topic="topluluk_kurulumu",
        question="Yeni ogrenci toplulugu kurmak veya topluluk bilgisini guncellemek icin resmi bir yol var mi?",
        answer="SKS web yapisinda topluluk kurulumu ve topluluk guncelleme baglantilari yer aldigi icin bu islemler icin cevrim ici resmi basvuru veya guncelleme akisi bulunur.",
        keywords=["topluluk kurulumu", "topluluk guncelleme", "sks"], source_url="https://sks.erdogan.edu.tr/tr/page/topluluk-guncelleme/1562", source_title="Topluluk Guncelleme", confidence="medium")
    add(entries, counters, intent="student_life", intent_aliases=life_aliases, topic="etkinlikler_sayfasi",
        question="Kampus etkinlikleri nerede duyurulur?",
        answer="SKS'nin Tum Etkinlikler sayfasinda topluluk etkinlikleri, senlikler, konserler, spor etkinlikleri ve kultur-sanat faaliyetleri listelenir.",
        keywords=["tum etkinlikler", "duyuru", "senlik", "konser"], source_url="https://sks.erdogan.edu.tr/tr/events", source_title="Tum Etkinlikler", confidence="medium")
    add(entries, counters, intent="student_life", intent_aliases=life_aliases, topic="spor_imkanlari",
        question="Universitede ogrenciler icin hangi spor imkanlari var?",
        answer="SKS menulerinde tenis kortu, mini golf, squash, hamam-sauna, acik spor alanlari ve fitness salonu gibi imkanlar resmi olarak listelenir.",
        keywords=["spor imkanlari", "tenis kortu", "fitness", "sks"], source_url="https://sks.erdogan.edu.tr", source_title="SKS Ana Sayfasi", confidence="medium")
    add(entries, counters, intent="student_life", intent_aliases=life_aliases, topic="kariyer_merkezi_duyurulari",
        question="Staj ve is firsatlari ile ilgili resmi duyurular nerede paylasilir?",
        answer="Kariyer Merkezi duyuru sayfasi kariyer etkinlikleri, staj ve istihdam firsatlari ile ilgili resmi ilanlar yayinlar.",
        keywords=["kariyer merkezi", "staj", "is firsati", "duyuru"], source_url="https://kariyer.erdogan.edu.tr/tr/news", source_title="Kariyer Merkezi Duyurulari", confidence="medium")
    add(entries, counters, intent="student_life", intent_aliases=life_aliases, topic="sosyal_destek_butik",
        question="SKS bunyesinde sosyal yardim odakli bir uygulama var mi?",
        answer="Psikolojik ve ogrenci destek arastirmasinda SKS bunyesinde RTEU Kizilay Kackar Butik gibi sosyal yardim uygulamalarinin bulundugu belirtilir; ayrintili isleyis her sayfada ayni aciklikta yer almaz.",
        keywords=["kackar butik", "sosyal yardim", "sks"], source_url="https://sks.erdogan.edu.tr", source_title="SKS Ana Sayfasi", confidence="medium")

    deduped = []
    seen = set()
    for entry in entries:
        key = (entry["intent"], entry["question"].strip().lower())
        if key in seen:
            continue
        seen.add(key)
        deduped.append(entry)

    raw["entries"] = deduped
    raw["description"] = "RTEU EduAI knowledge base for grounded student support responses. Expanded in Faz A2."
    save_kb(raw)
    print(f"Saved {len(deduped)} KB entries")


if __name__ == "__main__":
    main()
