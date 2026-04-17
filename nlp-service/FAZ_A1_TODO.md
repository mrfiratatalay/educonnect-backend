# EduAI Faz A1 TODO

Amaç:
`Knowledge base mimarisini ve domain yapisini rapordaki EduAI hedefine uygun hale getirmek`

## Faz A1 Hedefleri

- [x] EduAI icin nihai KB domainlerini kilitle
- [x] Eski intentlerden KB acisindan birlestirilecek alanlari netlestir
- [x] `marketplace` intentinin EduAI cekirdeginde kalip kalmayacagina karar ver
- [x] `student_life` ust domaininin kapsam sinirlarini yaz
- [x] KB kayit semasini zorunlu alanlariyla sabitle
- [x] KB kabul kriterlerini yaz
- [x] KB red kriterlerini yaz
- [x] Domain bazli hedef kayit sayilarini belirle
- [x] `rteu/openai` ve `rteu/perplexity` icin kaynak oncelik sirasi cikar
- [x] Her domain icin birincil ve ikincil kaynak dosyalari esle
- [x] Standart `topic` isimlendirme kurallarini tanimla
- [x] `faculty_scope`, `time_sensitive`, `confidence` kullanimini netlestir
- [x] Her KB kaydi icin entity baglama zorunlulugunu tanimla
- [x] Faz A2'de kullanilacak markdown -> KB donusum akisini yaz
- [x] Kalite kontrol kontrol listesini olustur

## Faz A1 Karar Taslagi

- [x] Nihai domain seti:
  - `exam_and_grading`
  - `course_registration`
  - `digital_systems`
  - `student_services`
  - `library`
  - `student_life`
  - `scholarship_support`
- [x] Birlestirme taslagi:
  - `cafeteria` -> `student_life`
  - `campus_life` -> `student_life`
  - `campus_logistics` -> `student_life`
- [x] Ayrik tutulacaklar:
  - `library`
  - `digital_systems`
  - `student_services`
  - `exam_and_grading`
  - `course_registration`
  - `scholarship_support`
- [x] Gecici karar:
  - `marketplace` EduAI cekirdeginden cikarilacak veya ayri urun intenti olarak tutulacak

## Faz A1 Cikis Kriteri

- [x] Tek bir KB domain plani dosyasi olusmus olmali
- [x] KB schema netlesmis olmali
- [x] Domain bazli hedefler ve kaynak sirasi yazilmis olmali
- [x] Faz A2 icin veri cikarma rotasi hazir olmali
