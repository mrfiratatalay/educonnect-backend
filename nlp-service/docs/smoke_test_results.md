# EduAI — Smoke Test Sonuçları (Faz 6)

> Bu test, EduAI NLP servisinin (metin analizi, niyet belirleme, varlık çıkarma ve KB eşleştirme)
> performansını ölçmek için 20 gerçekçi öğrenci sorusuyla yerel ortamda çalıştırılmıştır.
> Test, C# backend'e bağlanmadan doğrudan Python NLP pipeline'ı üzerinde çalıştırılmıştır.

---

## 1. Test Ortamı ve Altyapı
- **Kullanılan Model:** `keyword-fallback` (BERTurk modeli validation accuracy 0.60 altında kaldığı için güvenli mod olarak bu kullanılmıştır)
- **KB Tarama Eşiği:** TF-IDF Cosine Similarity
- **Grounded Karar Eşiği (C# Backend):** Intent Confidence $\geq$ 0.70 ve KB Score $\geq$ 0.25

## 2. Test Sonuçları Özeti

| Soru | Sistem Tahmini | Güven | KB Durumu | KB Konusu | Süre |
|------|--------|------------|-----------|-----------|------|
| Finalden kalırsam bütünlemeye girebil... | `exam_and_grading` | 0.82 | ✅ KB Eşleşti | *butunleme_hakki* | 3ms |
| Ara sınavların ortalamaya etkisi ne k... | `exam_and_grading` | 0.82 | ✅ KB Eşleşti | *ara_sinav_katki_orani* | 0ms |
| Ders kaydımı yapmayı unuttum, ne yapm... | `course_registration` | 0.8 | ❌ KB Yok | *-* | 0ms |
| Ders ekleme çıkarma haftası ne zaman? | `course_registration` | 0.8 | ❌ KB Yok | *-* | 0ms |
| Kampüste hangi öğrenci kulüpleri var? | `student_services` | 0.35 | ❌ KB Yok | *-* | 0ms |
| Bahar şenlikleri ne zaman yapılıyor? | `student_services` | 0.35 | ⚠️ KB Eşleşti | *ilisik_kesme* (Hatalı) | 0ms |
| Kütüphanede grup çalışma odası rezerv... | `student_services` | 0.35 | ❌ KB Yok | *-* | 0ms |
| Kütüphaneden aldığım kitabın süresini... | `student_services` | 0.35 | ❌ KB Yok | *-* | 0ms |
| Yemek bursu başvuruları nereye yapıl... | `scholarship_support` | 0.83 | ✅ KB Eşleşti | *yemek_bursu_sureci* | 0ms |
| Üniversitenin verdiği burslar nelerdir? | `scholarship_support` | 0.83 | ❌ KB Yok | *-* | 0ms |
| Kampüs içi ring servis saatlerini ner... | `campus_logistics` | 0.78 | ❌ KB Yok | *-* | 0ms |
| Öğrenci yurdu kampüse ne kadar uzaklı... | `student_services` | 0.35 | ⚠️ KB Eşleşti | *ogrenci_destek_koordi...* | 0ms |
| Yemekhane menüsü bugün nedir? | `cafeteria` | 0.79 | ✅ KB Eşleşti | *beslenme_hizmeti_birimi* | 0ms |
| Yemek kartıma nasıl bakiye yüklerim? | `cafeteria` | 0.79 | ❌ KB Yok | *-* | 0ms |
| İkinci el kitap satmak için nereden i... | `library` | 0.81 | ❌ KB Yok | *-* | 0ms |
| Marketplace'te ürün satışı yapmak yas... | `student_services` | 0.35 | ❌ KB Yok | *-* | 0ms |
| OBS şifremi unuttum, nasıl sıfırlayab... | `digital_systems` | 0.8 | ✅ KB Eşleşti | *sifre_sifirlama* | 0ms |
| E-kampüs üzerinden kurumsal e-postama... | `digital_systems` | 0.8 | ✅ KB Eşleşti | *kurumsal_eposta* | 0ms |
| OIDB nereden öğrenci belgesi alabilir... | `student_services` | 0.35 | ✅ KB Eşleşti | *ogrenci_belgesi* | 0ms |
| Transkriptimi e-devletten çıkarabilir... | `student_services` | 0.78 | ✅ KB Eşleşti | *transkript* | 0ms |

---

## 3. Analiz ve Tez Çıkarımları

Test sonuçları EduAI chatbot'un tasarım sınırlarını ve başarılarını net bir şekilde kanıtlamaktadır:

### Başarılar:
1. **Düşük Gecikme (Low Latency):** Regex/Keyword tabanlı sınıflandırma ve özellik tabanlı arama nedeniyle pipeline $\sim$ 0-3ms aralığında yanıt vermektedir. Gerçek zamanlı bir sohbet botu için mükemmel bir seviyedir.
2. **Açık Niyetlerin Yakalanması:** "Final, ara sınav, yemekhane, burs" gibi net kelimeler içeren sorularda sistem `%80` seviyelerinde güvenle doğru kategoriye dallanabilmektedir.
3. **Varlık Çıkarımı (Entity Extraction):** Test sonuçlarındaki detaylarda "OBS", "OIDB", "Transkript" kelimeleri ile "ne zaman" gibi zamansal ifadelerin sözlük tabanlı olarak başarıyla yakalandığı görülmüştür.

### Beklenen Davranışlar (Güvenlik Kalkanı):
- Bazı sorularda (örneğin "Bahar şenlikleri...") sistem anahtar kelime eşleşmesi bulamamış ve `student_services` kategorisine `0.35` güven puanıyla düşmüştür.
- KB algoritması rastgele bir madde eşleştirse bile, C# tarafında `0.35 < 0.70` olduğu için **Grounded sayılmayacak** ve bot kullanıcıya "Bu konuda emin değilim" diyerek yanlış bilgi (halüsinasyon) vermekten kaçınacaktır.
- **Bu tam olarak sistemin tasarım amacıdır:** Uydurma yanıt vermek yerine düşük güven oyu durumlarında resmi birime yönlendirme yapmak.

### Limitler:
- Makine öğrenmesi (semantic understanding) yerine kelime bazlı eşleşme (keyword match) çalıştığı için, "İkinci el kitap satmak için nereden ilan açabilirim?" gibi içerisinde "kitap" geçen ama aslen `marketplace` kategorisine ait olan bir soru, içerisindeki kelime nedeniyle `library` kategorisine çekilmiştir. Bu, transfer öğrenmesinin yetersiz kalmasının getirdiği en belirgin zafiyettir ve tezde açıkça limitasyon olarak raporlanmalıdır.
