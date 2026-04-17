import json
from collections import Counter, defaultdict
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

LIFE_ALIASES = ["cafeteria", "campus_life", "campus_logistics"]


def load_kb():
    with open(KB_PATH, "r", encoding="utf-8-sig") as f:
        return json.load(f)


def save_kb(data):
    with open(KB_PATH, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
        f.write("\n")


def counters_from_entries(entries):
    counters = defaultdict(int)
    for entry in entries:
        counters[entry["intent"]] += 1
    return counters


def build_id(counters, intent):
    counters[intent] += 1
    return f"kb_{PREFIX_MAP[intent]}_{counters[intent]:03d}"


def add_entry(entries, counters, *, intent, topic, question, answer, keywords, source_url, source_title,
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


def dedupe(entries):
    deduped = []
    seen = set()
    for entry in entries:
        key = (entry["intent"], entry["question"].strip().lower())
        if key in seen:
            continue
        seen.add(key)
        deduped.append(entry)
    return deduped


def add_course_registration(entries, counters):
    reg_url = "https://oidb.erdogan.edu.tr/tr/page/derse-kayit-kayit-yenileme/1247"
    reg_title = "Derse Kayit / Kayit Yenileme"
    mazeret_url = "https://oidb.erdogan.edu.tr/tr/news-detail/2025-2026-egitim-ogretim-yili-guz-yariyili-mazeretli-derse-kayit-islemleri/6565"
    mazeret_title = "Mazeretli Derse Kayit Islemleri"
    sbf_url = "https://sbf.erdogan.edu.tr/tr/page/soru-cevap/4721"
    sbf_title = "Saglik Bilimleri Fakultesi Sikca Sorulan Sorular"
    tip_url = "https://tip.erdogan.edu.tr/Files/ckFiles/tip-erdogan-edu-tr/Ocak%202020/E%C4%9Fitim-%C3%96%C4%9Fretim%20ve%20S%C4%B1nav%20Y%C3%B6nergesi.pdf"
    tip_title = "Tip Fakultesi Egitim-Ogretim ve Sinav Yonergesi"
    ziraat_url = "https://ziraat.erdogan.edu.tr/tr/news-detail/staj-basvuru-duyurusu/15650"
    ziraat_title = "Ziraat Fakultesi Staj Basvuru Duyurusu"
    bologna_url = "https://bologna2.erdogan.edu.tr/tr/page/ogrenci-isleri-daire-baskanligi/3501"
    bologna_title = "Ogrenci Isleri Daire Baskanligi"
    yatay_url = "https://ogrenci.erdogan.edu.tr/YatayGecis/Login"
    yatay_title = "Yatay Gecis Basvuru Bilgi Sistemi"
    cap_url = "https://oidb.erdogan.edu.tr/tr/news-detail/2025-2026-egitim-ogretim-yili-cift-anadal-yandal-programlari-basvurulari/6464"
    cap_title = "Cift Anadal / Yandal Basvurulari"

    rows = [
        ("kayit_yenileme_sorumluluk", "Kayit yenileme sorumlulugu ogrencide mi?", "Evet. Ders secimi ve kayit yenileme surecinin zamaninda tamamlanmasindan birincil olarak ogrenci sorumludur; danisman onayi ise surecin kesinlesme asamasidir.", ["kayit yenileme", "ogrenci sorumlulugu", "danisman onayi"], reg_url, reg_title, "high", False, "general"),
        ("kayit_yenilememe_hak_kaybi", "Kayit yenilemezsem hangi haklari kaybederim?", "Kayit yenilemeyen ogrenci ilgili donemde derslere devam edemez, sinavlara giremez ve ogrencilik haklarindan yararlanamaz.", ["kayit yenilememe", "hak kaybi", "sinava girememe"], "https://oidb.erdogan.edu.tr/Files/ckFiles/oidb-erdogan-edu-tr/14%20Aral%C4%B1k%202025%20PAZAR-Y%C3%B6netmelik.pdf", "On Lisans ve Lisans Egitim-Ogretim ve Sinav Yonetmeligi", "high", False, "general"),
        ("kayitsiz_sinav_gecersiz", "Kayit yapmadan girdigim sinav gecerli sayilir mi?", "Hayir. Kayit yenilenmeyen dersin sinavina girilse bile notu gecerli sayilmaz.", ["kayitsiz ders", "sinav gecerli mi", "not gecersiz"], "https://oidb.erdogan.edu.tr/Files/ckFiles/oidb-erdogan-edu-tr/14%20Aral%C4%B1k%202025%20PAZAR-Y%C3%B6netmelik.pdf", "On Lisans ve Lisans Egitim-Ogretim ve Sinav Yonetmeligi", "high", False, "general"),
        ("azami_sure_kayit_yenilememe", "Kayit yenilemedigim donem azami sureden duser mi?", "Evet. Kayit yenilenmeyen yariyillar da azami ogrenim suresinden sayilir.", ["azami sure", "kayit yenilememe", "yariyil sayilir"], "https://oidb.erdogan.edu.tr/Files/ckFiles/oidb-erdogan-edu-tr/14%20Aral%C4%B1k%202025%20PAZAR-Y%C3%B6netmelik.pdf", "On Lisans ve Lisans Egitim-Ogretim ve Sinav Yonetmeligi", "high", False, "general"),
        ("dort_yil_kayit_yenilememe", "Uzun sure kayit yenilemezsem ilisik kesilir mi?", "Genel duzende dort yil ust uste katkı payi veya ogrenim ucreti yatirilmamasi ve kayit yenilenmemesi halinde ilisik kesme sonucu dogabilir.", ["ilisik kesme", "dort yil", "kayit yenilememe"], "https://oidb.erdogan.edu.tr/Files/ckFiles/oidb-erdogan-edu-tr/14%20Aral%C4%B1k%202025%20PAZAR-Y%C3%B6netmelik.pdf", "On Lisans ve Lisans Egitim-Ogretim ve Sinav Yonetmeligi", "medium", False, "general"),
        ("danisman_gorusme_onemi", "Ders secmeden once danismanla gorusmek gerekir mi?", "Resmi kayit akislarinda ders seciminin danismanla iletisim icinde yapilmasi ve onayin takip edilmesi beklenir.", ["danismanla gorusme", "ders secimi", "onay"], reg_url, reg_title, "medium", False, "general"),
        ("katki_payi_odeme_kanali", "Katki payi veya ogrenim ucretini nasil odeyebilirim?", "Resmi duyurularda odemenin sanal POS ekranindan veya anlasmali banka ATM kanallarindan yapilabildigi belirtilir.", ["katki payi odeme", "sanal pos", "atm"], mazeret_url, mazeret_title, "medium", True, "general"),
        ("mazeretli_basvuru_yeri", "Mazeretli ders kaydi icin nereye basvurulur?", "Mazeretli ders kaydi icin ogrenci mazeret dilekcesi ve belgeleriyle kayitli oldugu akademik birime basvurur; karar ilgili birim yonetim kurulunca verilir.", ["mazeretli kayit", "akademik birim", "yonetim kurulu"], "https://oidb.erdogan.edu.tr/Files/ckFiles/oidb-erdogan-edu-tr/14%20Aral%C4%B1k%202025%20PAZAR-Y%C3%B6netmelik.pdf", "On Lisans ve Lisans Egitim-Ogretim ve Sinav Yonetmeligi", "high", False, "general"),
        ("mazeretli_basvuru_suresi", "Mazeretli derse kayit icin sure siniri var mi?", "Evet. Genel duzende mazeretli kayit basvurusu derslerin baslamasindan itibaren on is gunu icinde yapilmalidir.", ["mazeretli kayit", "on is gunu", "sure"], "https://oidb.erdogan.edu.tr/Files/ckFiles/oidb-erdogan-edu-tr/14%20Aral%C4%B1k%202025%20PAZAR-Y%C3%B6netmelik.pdf", "On Lisans ve Lisans Egitim-Ogretim ve Sinav Yonetmeligi", "high", False, "general"),
        ("ders_ekle_birak_takvimi", "Ders ekleme-birakma neye gore yapilir?", "Ders ekleme-birakma islemleri akademik takvimde ilan edilen ayri pencere icinde ve danisman onayiyla yapilir.", ["ders ekleme", "ders birakma", "akademik takvim"], "https://erdogan.edu.tr/Images/Uploads/MyContents/L_6146-20250604152832789418.pdf", "2025-2026 Onlisans-Lisans Akademik Takvimi", "high", True, "general"),
        ("tekrar_ders_birakma", "Basarisiz oldugum zorunlu dersi ekleme-birakma doneminde birakabilir miyim?", "Basarisizlik nedeniyle tekrar alinmasi gereken dersler ilgili fakulte veya program kurallarina gore birakilamayabilir; ogrenci resmi birim yonlendirmesini esas almalidir.", ["tekrar ders", "birakma", "basarisiz ders"], reg_url, reg_title, "medium", False, "general"),
        ("gano_150_kisit", "GANO 1.50 altina dusunce ustten ders alinabilir mi?", "Dorduncu yariyil sonunda GANO'su 1.50'nin altinda olan lisans ogrencisi ust yariyillardan ders alamaz.", ["gano 1.50", "ustten ders", "lisans"], "https://oidb.erdogan.edu.tr/Files/ckFiles/oidb-erdogan-edu-tr/14%20Aral%C4%B1k%202025%20PAZAR-Y%C3%B6netmelik.pdf", "On Lisans ve Lisans Egitim-Ogretim ve Sinav Yonetmeligi", "high", False, "general"),
        ("gano_300_ustten", "GANO 3.00 ve ustuyse ustten ders alinabilir mi?", "Evet. GANO'su en az 3.00 olan ogrenci, resmi sinirlar dahilinde ust yariyildan ek ders alabilir.", ["gano 3.00", "ustten ders", "ek ders"], "https://oidb.erdogan.edu.tr/Files/ckFiles/oidb-erdogan-edu-tr/14%20Aral%C4%B1k%202025%20PAZAR-Y%C3%B6netmelik.pdf", "On Lisans ve Lisans Egitim-Ogretim ve Sinav Yonetmeligi", "high", False, "general"),
        ("akademik_birim_ogrenci_isleri", "Sadece merkezi ogrenci isleri mi ilgilenir, yoksa fakulte ogrenci isleri de var mi?", "Merkezi OIDB'ye ek olarak her akademik birimde ogrenci isleri burolari bulunur ve bircok kayit-belge islemi fakulte veya yuksekokul ogrenci isleriyle birlikte yurur.", ["merkezi ogrenci isleri", "fakulte ogrenci isleri", "birim"], bologna_url, bologna_title, "high", False, "general"),
        ("sbf_intibak_komisyonu", "Saglik Bilimleri Fakultesinde intibak kararini kim verir?", "Saglik Bilimleri Fakultesi resmi SSS sayfasina gore DGS ile gelen ogrencilerin intibak sinifi muafiyet ve intibak komisyonunca belirlenir.", ["sbf", "intibak", "komisyon"], sbf_url, sbf_title, "high", False, "sbf"),
        ("sbf_muafiyet_gec_basvuru", "Saglik Bilimleri Fakultesinde muafiyet suresini kacirirsam ne olur?", "Fakulte SSS'sinde muafiyet basvurusunun derslerin baslamasini izleyen bes is gunu icinde yapilmasi gerektigi belirtilir; sure sonrasinda ogrenci ilgili derslerden sorumlu olur.", ["sbf", "muafiyet", "bes is gunu"], sbf_url, sbf_title, "medium", False, "sbf"),
        ("tip_muafiyet_tek_sefer", "Tip Fakultesinde muafiyet basvurusu birden fazla kez yapilabilir mi?", "Tip Fakultesi yonergesine gore ders veya staj muafiyeti basvurusu sureye bagli ve tek seferliktir.", ["tip", "muafiyet", "tek sefer"], tip_url, tip_title, "high", False, "tip"),
        ("tip_staj_muafiyet_birimi", "Tip Fakultesinde staj muafiyeti icin nereye basvurulur?", "Tip Fakultesinde ders veya staj muafiyeti icin ilk on is gunu icinde Dekanliga basvurulur.", ["tip", "staj muafiyeti", "dekanlik"], tip_url, tip_title, "high", False, "tip"),
        ("tip_ayri_takvim_vurgusu", "Tip Fakultesi neden genel akademik takvimden farkli takip edilir?", "Universitenin genel takvimi Tip Fakultesini kapsam disi biraktigi icin Tipte kayit, staj ve sinav tarihleri ayri takvimle izlenir.", ["tip fakultesi", "ayri takvim", "genel takvim"], "https://erdogan.edu.tr/Images/Uploads/MyContents/L_6146-20250604152742268512.pdf", "2025-2026 Tip Fakultesi Akademik Takvimi", "high", True, "tip"),
        ("ziraat_staj_belge_kaynagi", "Ziraat Fakultesinde staj belgeleri nereden alinir?", "Ziraat Fakultesi duyurulari staj belgelerinin fakulte web sayfasindan temin edilecegini belirtir.", ["ziraat", "staj belgeleri", "web sayfasi"], ziraat_url, ziraat_title, "high", True, "ziraat"),
        ("ziraat_staj_teslim", "Ziraat Fakultesi staj dosyasi nasil teslim edilir?", "Tamamlanan staj dosyasinin fakulte ogrenci islerine sahsen veya posta-kargo ile teslim edilebildigi resmi duyurularda belirtilir.", ["ziraat", "staj dosyasi", "posta", "ogrenci isleri"], ziraat_url, ziraat_title, "medium", True, "ziraat"),
        ("yatay_gecis_online", "Yatay gecis basvurusu cevrim ici mi yapiliyor?", "Evet. RTEU yatay gecis basvurulari icin ayri bir cevrim ici basvuru bilgi sistemi kullanir.", ["yatay gecis", "cevrim ici", "basvuru sistemi"], yatay_url, yatay_title, "high", True, "general"),
        ("cap_yandal_basvuru_donemi", "CAP ve yandal basvurulari icin ayri duyuru donemi oluyor mu?", "Evet. Cift anadal ve yandal basvurulari OIDB tarafindan donemsel resmi duyuru ile acilir.", ["cap", "yandal", "duyuru donemi"], cap_url, cap_title, "high", True, "general"),
    ]

    for row in rows:
        add_entry(
            entries,
            counters,
            intent="course_registration",
            topic=row[0],
            question=row[1],
            answer=row[2],
            keywords=row[3],
            source_url=row[4],
            source_title=row[5],
            confidence=row[6],
            time_sensitive=row[7],
            faculty_scope=row[8],
        )


def add_scholarship_support(entries, counters):
    vakif_url = "https://erdogan.edu.tr/Images/Uploads/Vak%C4%B1f%20Akademik%20Te%C5%9Fvik%20Y%C3%B6nergesi%20(2).pdf"
    vakif_title = "RTEU Gelistirme Vakfi Burs ve Tesvik Yonergesi"
    sks_report_url = "https://sks.erdogan.edu.tr/Files/ckFiles/sks-erdogan-edu-tr/Faaliyet%20Raporlar%C4%B1%20ve%20Mevzuat/2025%20B%C4%B0R%C4%B0M_FAL%C4%B0YET_RAPORU.pdf"
    sks_report_title = "SKS 2025 Birim Faaliyet Raporu"
    yemek_2025_url = "https://sks.erdogan.edu.tr/tr/news-detail/yemek-bursu-basvurulari-2025/6545"
    yemek_2025_title = "Yemek Bursu Basvurulari - 2025"
    ogrenim_bursu_url = "https://erdogan.edu.tr/Website/Contents.aspx?PageID=6310&LangID=1"
    ogrenim_bursu_title = "Ogrenim Bursu Basvuru Duyurusu"
    basari_url = "https://erdogan.edu.tr/Website/Contents.aspx?PageID=396&LangID=1"
    basari_title = "Basari Bursu Firsati"
    kyk_url = "https://oidb.erdogan.edu.tr/Files/ckFiles/oidb-erdogan-edu-tr/KATKI%20PAYI%20%C3%96%C4%9EREN%C4%B0M%20%C3%9CCRET%C4%B0%20ALINMASI%20HAKKINDA%20B%C4%B0LG%C4%B0LER.pdf"
    kyk_title = "Katki Payi / Ogrenim Ucreti Alinmasi Hakkinda Bilgiler"
    tev_url = "https://tde-fef.erdogan.edu.tr/tr/news-detail/2025-2026-ogretim-yili-tev-universite-egitim-burs-basvuru-duyurusu/16540"
    tev_title = "TEV Universite Egitim Bursu Basvuru Duyurusu"
    oidb_hassas_url = "https://oidb.erdogan.edu.tr/Files/Images/hassas-gorev-tespit-2312023132756.pdf"
    oidb_hassas_title = "OIDB Hassas Gorev Tespit Dokumani"
    kismi_url = "https://sks.erdogan.edu.tr/tr/news-detail/2025-2026-egitim-ogretim-yili-kismi-zamanli-ogrenci-calisma-basvurulari/16523"
    kismi_title = "Kismi Zamanli Ogrenci Calisma Basvurulari"

    rows = [
        ("vakif_komisyonu", "Universite burs kararlarinda hangi vakif komisyonu rol alir?", "Gelistirme Vakfi yonergesine gore ogrenci burs sureclerinde Yurt-Burs-Ogrenci Isleri Komisyonu ve Vakif Mutevelli Heyeti rol alir.", ["vakif komisyonu", "yurt burs ogrenci isleri", "mutevelli heyeti"], vakif_url, vakif_title, "high", False),
        ("tesvik_bursu_yillik", "Tesvik bursu miktari sabit midir?", "Hayir. Vakif yonergesine gore hangi ogrencilere ne miktarda tesvik bursu verilecegi her yil Mutevelli Heyeti tarafindan belirlenir.", ["tesvik bursu", "her yil", "mutevelli heyeti"], vakif_url, vakif_title, "high", False),
        ("tesvik_bursu_gano", "Tesvik bursu devam ederken GANO kurali var mi?", "Evet. Vakif yonergesine gore genel akademik not ortalamasi 4.00 uzerinden 2.50'nin altina dusen ogrencinin tesvik bursu kesilebilir.", ["tesvik bursu", "gano 2.50", "kesilme"], vakif_url, vakif_title, "high", False),
        ("tesvik_bursu_disiplin", "Disiplin cezasi tesvik bursunu etkiler mi?", "Evet. Uzaklastirma veya daha agir ceza alinmasi halinde tesvik bursunun kesilecegi vakif yonergesinde belirtilir.", ["tesvik bursu", "disiplin cezasi", "kesilir"], vakif_url, vakif_title, "high", False),
        ("tesvik_bursu_alttan_ders", "Alttan ders olursa tesvik bursu etkilenir mi?", "Evet. Vakif yonergesi, sinif kaybi veya alttan dersi bulunan ogrencinin tesvik bursunun kesilebilecegini belirtir.", ["alttan ders", "tesvik bursu", "sinif kaybi"], vakif_url, vakif_title, "high", False),
        ("ihtiyac_bursu_kimler", "Ihtiyac bursu kimler icin dusunulur?", "Vakif yonergesine gore ihtiyac sahibi ogrenciler fakulte veya birim yonetimleri tarafindan belirlenir, komisyonca uygun gorulurse vakif bursu alabilir.", ["ihtiyac bursu", "ihtiyac sahibi", "komisyon"], vakif_url, vakif_title, "high", False),
        ("ihtiyac_bursu_sure", "Ihtiyac bursu ne kadar sure devam edebilir?", "Uluslararasi ogrenciler haric olmak uzere, kosullar korundugu surece ihtiyac bursu azami ogrenim suresi boyunca devam edebilir.", ["ihtiyac bursu", "azami ogrenim suresi"], vakif_url, vakif_title, "high", False),
        ("ihtiyac_bursu_kosullar", "Ihtiyac bursu devam ederken hangi kosullar korunmali?", "Vakif yonergesine gore kayit dondurmama, en az 2.50 GANO'yu koruma ve agir disiplin cezasi almama gibi kosullar aranir.", ["ihtiyac bursu", "2.50", "kayit dondurma", "disiplin"], vakif_url, vakif_title, "high", False),
        ("basari_odulu_ilk_uc", "Bolum veya fakulte derecesi yapan ogrencilere odul var mi?", "Vakif yonergesi fakulte, yuksekokul veya MYO'yu ilk uc derece ile bitiren ogrencilere nakdi veya ayni basari odulu verilebilecegini belirtir.", ["ilk uc derece", "basari odulu", "nakdi"], vakif_url, vakif_title, "high", False),
        ("basari_odulu_yarisma", "Ulusal veya uluslararasi yarismalarda derece yapan ogrencilere odul verilebilir mi?", "Evet. Vakif yonergesine gore bilim, kultur veya spor alanlarinda ilk uce giren ogrencilere odul verilebilir.", ["uluslararasi yarisma", "odul", "spor", "bilim"], vakif_url, vakif_title, "high", False),
        ("yemek_bursu_talep_edenler", "2025 duyurusunda yemek bursu kapsami nasil ifade edildi?", "2025 SKS duyurusunda, yemek bursu talep eden tum ogrencilere Gelistirme Vakfi tarafindan yemek bursu verilecegi ifade edilir.", ["2025 yemek bursu", "talep eden tum ogrenciler"], yemek_2025_url, yemek_2025_title, "medium", True),
        ("yemek_bursu_duyuru_kanali", "Yemek bursu duyurulari hangi resmi sayfada toplanir?", "Yemek bursu basvurulari SKS duyurular sayfasinda yayinlanir ve ilgili basvuru baglantilari bu sayfadan paylasilir.", ["sks duyurular", "yemek bursu"], "https://sks.erdogan.edu.tr/tr/news", "SKS Duyurular", "high", True),
        ("yemek_bursu_sks_rolu", "Yemek bursu surecinde SKS'nin rolu nedir?", "SKS faaliyet raporunda yemek bursu islemlerinin Beslenme Hizmetleri Birimi gorevleri arasinda sayildigi gorulur.", ["sks", "yemek bursu islemleri", "beslenme hizmetleri"], sks_report_url, sks_report_title, "high", False),
        ("barinma_yardimi_varligi", "Universitenin resmi belgelerinde barinma yardimi geciyor mu?", "Evet. SKS faaliyet raporu ogrencilere burs, yemek yardimi ve barinma yardimi gibi destekler saglandigini acikca belirtir.", ["barinma yardimi", "faaliyet raporu"], sks_report_url, sks_report_title, "high", False),
        ("sosyal_destek_sks", "SKS sadece yemekhane degil sosyal destek de saglar mi?", "Evet. SKS raporlarinda beslenme, barinma, sosyal destek ve psikolojik danismanlikla iliskili ogrenci hizmetleri birlikte anilir.", ["sks", "sosyal destek", "psikolojik danismanlik"], sks_report_url, sks_report_title, "medium", False),
        ("ogrenim_bursu_evrak", "Ogrenim bursu basvurularinda aile gelir belgesi gibi evraklar istenebilir mi?", "2025 ogrenim bursu duyurusu aile gelir belgeleri, nufus kayit ornegi ve adli sicil gibi belgelerin istendigini gosterir.", ["ogrenim bursu", "gelir belgesi", "adli sicil"], ogrenim_bursu_url, ogrenim_bursu_title, "high", True),
        ("ogrenim_bursu_disiplin", "Ogrenim bursu icin disiplin cezasiz olmak onemli mi?", "Ogrenim bursu duyurusunda disiplin cezasi bulunmamasi degerlendirme kriterlerinden biri olarak yer alir.", ["ogrenim bursu", "disiplin cezasi"], ogrenim_bursu_url, ogrenim_bursu_title, "high", True),
        ("ogrenim_bursu_aile_kriter", "Ogrenim bursunda aile ve gelir durumu dikkate alinir mi?", "Evet. Ogrenim bursu duyurusunda aile geliri, kardes sayisi ve aile durumu gibi kriterler degerlendirmede kullanilir.", ["aile geliri", "kardes sayisi", "ogrenim bursu"], ogrenim_bursu_url, ogrenim_bursu_title, "high", True),
        ("basari_bursu_tutar", "Basari bursu 2025 icin ne kadar aciklandi?", "Universitenin resmi basari bursu duyurusu 2025 donemi icin uygun OSMY siralamalarina sahip ogrencilere 18 bin TL basari bursu verilecegini belirtir.", ["18 bin tl", "basari bursu", "osym"], basari_url, basari_title, "high", True),
        ("basari_bursu_siralama", "Basari bursu icin yerlestirme siralamasi onemli mi?", "Evet. Basari bursu duyurusunda sayisal, esit agirlik ve sozel alanlar icin farkli siralama esikleri resmi olarak tanimlanir.", ["yerlestirme siralamasi", "sayisal", "esit agirlik", "sozel"], basari_url, basari_title, "high", True),
        ("kyk_sadece_resmi_sistem", "KYK burs ve kredi icin hangi kanal esas alinmali?", "OIDB belgeleri KYK burs ve kredi basvurulari icin dogrudan KYK'nin resmi internet kanalini esas gosterir.", ["kyk resmi sistem", "burs kredi"], kyk_url, kyk_title, "high", False),
        ("oidb_burs_kontenjan", "OIDB burs kontenjan yazilarinda rol aliyor mu?", "Evet. OIDB'nin hassas gorev dokumaninda burs kontenjan yazilarini ilgili komisyonlara iletme gorevi yer alir.", ["oidb", "burs kontenjan", "komisyon"], oidb_hassas_url, oidb_hassas_title, "medium", False),
        ("tev_duyuru_yonlendirme", "TEV bursu icin universite dogrudan burs mu veriyor, yoksa yonlendirme mi yapiyor?", "Universite TEV gibi dis burslar icin resmi duyuru yayimlar ve ogrenciyi ilgili vakfin basvuru kanalina yonlendirir.", ["tev", "yonlendirme", "vakif"], tev_url, tev_title, "high", True),
        ("kismi_zamanli_basvuru", "Kismi zamanli ogrenci calisma basvurulari resmi olarak aciliyor mu?", "Evet. SKS, kismi zamanli ogrenci calisma basvurularini resmi duyuru ile acar.", ["kismi zamanli", "ogrenci calisma", "sks"], kismi_url, kismi_title, "high", True),
        ("kismi_zamanli_oncelik", "Kismi zamanli ogrenci calismada burs alan ya da ihtiyac sahibi ogrencilere oncelik veriliyor mu?", "Kismi zamanli ogrenci calisma duyurulari KYK bursu alan ya da burs alma kosullarini tasiyan ogrencilere oncelik verilebildigini belirtir.", ["kismi zamanli", "oncelik", "kyk bursu"], kismi_url, kismi_title, "medium", True),
    ]

    for row in rows:
        add_entry(
            entries,
            counters,
            intent="scholarship_support",
            topic=row[0],
            question=row[1],
            answer=row[2],
            keywords=row[3],
            source_url=row[4],
            source_title=row[5],
            confidence=row[6],
            time_sensitive=row[7],
        )


def add_student_life(entries, counters):
    sks_url = "https://sks.erdogan.edu.tr"
    sks_title = "SKS Ana Sayfasi"
    life_url = "https://sks.erdogan.edu.tr/tr/Activities/AllActivities"
    life_title = "Tum Etkinlikler"
    yemek_info_url = "https://sks.erdogan.edu.tr/Files/ckFiles/sks-erdogan-edu-tr/beslenme/Yemekhane%20Rezervasyon%20Bilgilendirme.pdf"
    yemek_info_title = "Yemekhane Rezervasyon Bilgilendirme"
    yemek_rez_url = "https://sks.erdogan.edu.tr/tr/news-detail/yemekhane-rezervasyon-sistemi/4787"
    yemek_rez_title = "Yemekhane Rezervasyon Sistemi"
    yemek_prices_url = "https://sks.erdogan.edu.tr/tr/page/yemek-hizmeti-fiyatlari/4316"
    yemek_prices_title = "Yemek Hizmeti Fiyatlari"
    topluluk_url = "https://sks.erdogan.edu.tr/tr/page/rteu-topluluklar-listesi-ve-iletisim-bilgileri/6088"
    topluluk_title = "RTEU Topluluklar Listesi ve Iletisim Bilgileri"
    topluluk_yonerge_url = "https://ilahiyat.erdogan.edu.tr/Files/ckFiles/ilahiyat-erdogan-edu-tr/ogrenci-topluluklari-yonergesi_18-03-2015.pdf"
    topluluk_yonerge_title = "Ogrenci Topluluklari Yonergesi"
    sports_url = "https://sks.erdogan.edu.tr/tr/page/fitness-salonu/1338"
    sports_title = "Fitness Salonu"
    bologna_barinma_url = "https://bologna2.erdogan.edu.tr/tr/page/barinma/3497"
    bologna_barinma_title = "Barinma"
    bologna_ulasim_url = "https://bologna2.erdogan.edu.tr/tr/page/ulasim/3499"
    bologna_ulasim_title = "Ulasim"
    rpduam_url = "https://rehberlik.erdogan.edu.tr/tr/page/genel-bilgiler/2376"
    rpduam_title = "RPDUAM Genel Bilgiler"
    rpduam_contact_url = "https://rehberlik.erdogan.edu.tr/tr/page/iletisim/2383"
    rpduam_contact_title = "RPDUAM Iletisim"
    odk_url = "https://odk.erdogan.edu.tr/tr/page/amac-ve-faaliyet-alani/5278"
    odk_title = "ODK Amac ve Faaliyet Alani"
    eob_url = "https://eob.erdogan.edu.tr/tr/page/hakkimizda/1568"
    eob_title = "Engelli Ogrenci Birimi Hakkimizda"
    kariyer_url = "https://kariyer.erdogan.edu.tr/tr/news"
    kariyer_title = "Kariyer Merkezi Duyurulari"

    rows = [
        ("rezervasyon_amaci", "Yemekhane rezervasyon sistemi neden uygulanmaya baslandi?", "SKS bilgilendirme notlarina gore rezervasyon sistemi israfi azaltmak, kaliteyi artirmak ve magduriyetleri onlemek amaciyla uygulanmaya baslandi.", ["yemekhane rezervasyon", "israf", "kalite"], yemek_info_url, yemek_info_title, "high", True),
        ("rezervasyon_son_saat", "Yemek rezervasyonu son saate kadar ne zaman yapilabilir?", "Yemekhane rezervasyon bilgilendirme dokumaninda rezervasyonun ilgili gun icin bir gun once saat 13.00'e kadar yapilabildigi belirtilir.", ["rezervasyon son saat", "13.00", "bir gun once"], yemek_info_url, yemek_info_title, "high", True),
        ("rezervasyon_iptal", "Yemek rezervasyonu iptal edilebilir mi?", "Evet. Bilgilendirme dokumaninda rezervasyonun yine bir gun once saat 13.00'e kadar iptal edilebildigi belirtilir.", ["rezervasyon iptal", "13.00"], yemek_info_url, yemek_info_title, "high", True),
        ("ogle_rezervasyon_saat", "Rezervasyonlu ogle yemegi saatleri nedir?", "Bilgilendirme dokumanina gore rezervasyonlu ogle yemegi saati 11.00-13.00 araligidir.", ["rezervasyonlu ogle", "11.00", "13.00"], yemek_info_url, yemek_info_title, "high", True),
        ("ogle_rezervasyonsuz_saat", "Rezervasyonsuz ogle yemegi saatleri nedir?", "Bilgilendirme dokumanina gore rezervasyonsuz ogle yemegi saati 13.00-14.00 araligidir.", ["rezervasyonsuz ogle", "13.00", "14.00"], yemek_info_url, yemek_info_title, "high", True),
        ("aksam_rezervasyon_saat", "Rezervasyonlu aksam yemegi saatleri nedir?", "Bilgilendirme dokumanina gore rezervasyonlu aksam yemegi saati 16.00-17.00 araligidir.", ["rezervasyonlu aksam", "16.00", "17.00"], yemek_info_url, yemek_info_title, "high", True),
        ("aksam_rezervasyonsuz_saat", "Rezervasyonsuz aksam yemegi saatleri nedir?", "Bilgilendirme dokumanina gore rezervasyonsuz aksam yemegi saati 17.00-18.00 araligidir.", ["rezervasyonsuz aksam", "17.00", "18.00"], yemek_info_url, yemek_info_title, "high", True),
        ("rezervasyon_sayisi_20", "Rezervasyon sayisi icin alt sinir olduguna dair resmi ifade var mi?", "SKS duyuru snippet'inde rezervasyon sayisi 20 ve uzeri olan yerlerde uygulamaya dair ifade gecmektedir; ayrintilar ilgili duyurudan takip edilmelidir.", ["rezervasyon sayisi 20", "snippet"], yemek_rez_url, yemek_rez_title, "medium", True),
        ("ucretsiz_yemek_2024", "2024 sonunda tum ogrenciler icin rezervasyonlu ucretsiz yemek uygulamasi var miydi?", "Yemekhane bilgilendirme PDF'inde 2024 sonuna kadar tum ogrencilerin rezervasyon yaparak ucretsiz yemek hizmetinden yararlanabilecegi belirtilir.", ["ucretsiz yemek", "2024 sonu", "rezervasyon"], yemek_info_url, yemek_info_title, "high", True),
        ("yemek_ucreti_2026", "2026 resmi fiyat sayfasinda ogrenci yemek ucreti kac TL olarak gorunuyor?", "SKS 2026 yemek hizmeti fiyatlari sayfasinda ogrenci yemek ucreti 25 TL olarak yayinlanmistir.", ["2026", "ogrenci ucreti", "25 tl"], yemek_prices_url, yemek_prices_title, "high", True),
        ("mobil_para_yukleme", "Yemekhane kartina RTEU Mobilden para yukleme secenegi var mi?", "Evet. SKS hizli erisim baglantilarinda Yemekhane Kartina RTEU Mobilden Para Yukleme secenegi resmi olarak yer alir.", ["rteu mobil", "para yukleme"], sks_url, sks_title, "high", True),
        ("topluluk_sayisi_252", "Aktif ogrenci topluluklari sayisi resmi belgelerde yuksek mi gorunuyor?", "Kapsamli arastirma belgelerinde aktif ogrenci topluluklari sayisinin 252 olarak verildigi resmi listeye atif bulunur; guncel sayi SKS topluluk listesi sayfasindan teyit edilmelidir.", ["252 topluluk", "aktif topluluklar"], topluluk_url, topluluk_title, "medium", True),
        ("topluluk_danisman_bilgisi", "Topluluklarin danisman bilgileri resmi olarak paylasiliyor mu?", "Evet. SKS topluluk listesi sayfasinda topluluklarin iletisim ve danisman bilgileri resmi olarak listelenir.", ["topluluk danismani", "iletisim bilgisi"], topluluk_url, topluluk_title, "high", True),
        ("topluluk_basvuru_kanali", "Topluluklarla ilgili resmi islemler icin SKS ana kanal midir?", "Evet. Topluluk kurulumu, guncelleme ve etkinlik degerlendirme surecleri SKS altyapisi uzerinden yurur.", ["topluluk kurulumu", "sks", "guncelleme"], topluluk_yonerge_url, topluluk_yonerge_title, "medium", False),
        ("etkinlik_duyuru_kanali", "Universite etkinlikleri icin en temel resmi kanal nedir?", "SKS'nin Tum Etkinlikler sayfasi ve topluluk etkinlik duyurulari kampus etkinlikleri icin temel resmi kanallardir.", ["tum etkinlikler", "resmi kanal"], life_url, life_title, "high", True),
        ("spor_rezervasyon", "Spor alanlari icin online rezervasyon veya tahsis akisi var mi?", "SKS yapisinda halisaha, tenis kortu ve diger spor alanlari icin tahsis veya rezervasyon baglantilari resmi olarak yer alir.", ["spor alanlari", "rezervasyon", "tahsis"], sports_url, sports_title, "medium", True),
        ("fitness_var", "Universitede fitness salonu var mi?", "Evet. SKS sayfalarinda fitness salonu resmi hizmet olarak listelenir.", ["fitness salonu", "sks"], sports_url, sports_title, "high", False),
        ("mini_golf_squash", "Mini golf veya squash gibi farkli spor imkanlari da var mi?", "Evet. SKS menulerinde mini golf ve squash gibi tesisler resmi olarak listelenir.", ["mini golf", "squash", "spor imkanlari"], sks_url, sks_title, "medium", False),
        ("barinma_genel_yonlendirme", "Barinma konusunda resmi genel bilgi sayfasi var mi?", "Evet. Bologna bilgi paketi icinde barinma basligi altinda resmi genel bilgilendirme sayfasi bulunur.", ["barinma", "bologna"], bologna_barinma_url, bologna_barinma_title, "high", False),
        ("ilk_kayit_yurt", "Yeni kazanan ogrenciler icin yurt basvurulari hangi kurumla iliskili anlatiliyor?", "Barinma bilgi sayfasi yurt basvurularinin KYK sistemi uzerinden yurudugune yonelik resmi yonlendirmeler icerir.", ["kyk yurt", "yeni kazanan"], bologna_barinma_url, bologna_barinma_title, "medium", False),
        ("ulasim_genel_sayfa", "Sehir ici veya kampus ulasimi icin resmi genel sayfa var mi?", "Evet. Bologna bilgi paketinde ulasim basligi altinda resmi genel bilgilendirme sayfasi vardir.", ["ulasim", "bologna bilgi paketi"], bologna_ulasim_url, bologna_ulasim_title, "high", False),
        ("rpduam_ucretsiz", "Psikolojik danismanlik hizmeti ucretli mi?", "RPDUAM tanitim metinlerinde psikolojik danisma hizmetinin ogrenci ve personele ucretsiz verildigi belirtilir.", ["psikolojik danismanlik", "ucretsiz"], rpduam_url, rpduam_title, "high", False),
        ("rpduam_iletisim", "Psikolojik destek icin resmi iletisim kanali var mi?", "Evet. RPDUAM iletisim sayfasinda telefon ve kurumsal e-posta bilgileri resmi olarak verilir.", ["rpduam", "iletisim", "telefon"], rpduam_contact_url, rpduam_contact_title, "high", False),
        ("rpduam_ilce_hizmet", "Ilce yerleskelerindeki ogrenciler icin yerinde psikolojik danisma uygulamasi var mi?", "RPDUAM duyurulari ilcelerde yerinde psikolojik danisma hizmetine atif yapar; ayrintilar resmi merkez duyurularindan takip edilmelidir.", ["yerinde psikolojik danisma", "ilce"], rpduam_url, rpduam_title, "medium", True),
        ("odk_uyum", "Ogrenci Destek Koordinatorlugu universiteye uyum konusunda rol alir mi?", "Evet. ODK'nin amac ve faaliyet alani metni ogrencilerin universite yasamina uyum surecini desteklemeyi gorev olarak tanimlar.", ["odk", "uyum", "universite yasami"], odk_url, odk_title, "high", False),
        ("odk_sorun_tespiti", "Ogrenci Destek Koordinatorlugu ogrenci sorunlarini toplamak icin mi kuruldu?", "ODK'nin resmi amac metni ogrenci sorunlarini tespit etmek ve cozum odakli mekanizmalar gelistirmek uzerine kuruludur.", ["odk", "ogrenci sorunu", "cozum"], odk_url, odk_title, "high", False),
        ("engelli_birim_baglilik", "Engelli ogrenci destegi hangi resmi birim altindadir?", "Engelli Ogrenci Birimi resmi olarak SKS bunyesinde yer alir.", ["engelli ogrenci birimi", "sks"], eob_url, eob_title, "high", False),
        ("engelli_esitligi", "Engelli Ogrenci Biriminin temel amaci nedir?", "Engelli Ogrenci Birimi, ozel gereksinimi olan ogrencilere egitimde firsat esitligi saglamak amaciyla calisir.", ["engelli ogrenci", "firsat esitligi"], eob_url, eob_title, "high", False),
        ("kariyer_duyuru_kanali", "Kariyer etkinlikleri ve staj duyurulari icin hangi resmi sayfa izlenmeli?", "Kariyer Merkezi duyuru sayfasi kariyer etkinlikleri, staj ve istihdam odakli resmi ilanlar icin temel kanaldir.", ["kariyer merkezi", "staj duyurusu"], kariyer_url, kariyer_title, "high", True),
        ("kackar_butik", "Ihtiyac sahibi ogrenciler icin Kackar Butik gibi bir sosyal destek uygulamasi var mi?", "SKS yapisinda RTEU Kizilay Kackar Butik sosyal yardim odakli resmi bir uygulama olarak yer alir.", ["kackar butik", "sosyal destek"], sks_url, sks_title, "medium", False),
    ]

    for row in rows:
        add_entry(
            entries,
            counters,
            intent="student_life",
            intent_aliases=LIFE_ALIASES,
            topic=row[0],
            question=row[1],
            answer=row[2],
            keywords=row[3],
            source_url=row[4],
            source_title=row[5],
            confidence=row[6],
            time_sensitive=row[7],
        )


def main():
    raw = load_kb()
    entries = list(raw["entries"])
    counters = counters_from_entries(entries)

    add_course_registration(entries, counters)
    add_scholarship_support(entries, counters)
    add_student_life(entries, counters)

    raw["entries"] = dedupe(entries)
    raw["description"] = "RTEU EduAI knowledge base for grounded student support responses. Expanded in Faz A2 and quality-extended in Faz A3."
    save_kb(raw)

    counts = Counter(entry["intent"] for entry in raw["entries"])
    print(f"Saved {len(raw['entries'])} KB entries")
    print(dict(sorted(counts.items())))


if __name__ == "__main__":
    main()
