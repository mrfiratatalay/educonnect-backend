# EduAI Faz A2 TODO

Amac:
`RTEU arastirma havuzundan kullanilabilir knowledge base kayitlari cikarip EduAI'yi grounded cevap verebilir hale getirmek`

## Faz A2 Gorevleri

- [x] Mevcut `knowledge_base.json` yapisini koru
- [x] Eski `cafeteria` kayitlarini `student_life` altina tasi
- [x] `student_life` icin geriye donuk intent aliaslari ekle
- [x] `exam_and_grading` domainini genislet
- [x] `digital_systems` domainini genislet
- [x] `student_services` domainini genislet
- [x] `course_registration` domainini ilk surumde doldur
- [x] `library` domainini genislet
- [x] `scholarship_support` domainini genislet
- [x] `student_life` domainini ilk surumde doldur
- [x] Tekrar eden soru kayitlarini temizle
- [x] Ornek retrieval smoke testleriyle yeni kayitlari dogrula

## Faz A2 Cikti Ozeti

- [x] `knowledge_base.json` genisletildi
- [x] Tum hedef domainler artik KB icinde temsil ediliyor
- [x] Yeni toplu uretim scripti eklendi:
  - `training/build_phase_a2_kb.py`
- [x] Retrieval smoke testleri temel sorularda eslesme uretti

## Faz A2 Sonucu

- Baslangic kayit sayisi: `48`
- Faz A2 sonrasi kayit sayisi: `147`

### Domain dagilimi

- `exam_and_grading`: `25`
- `course_registration`: `15`
- `digital_systems`: `26`
- `student_services`: `26`
- `library`: `19`
- `student_life`: `20`
- `scholarship_support`: `16`

## Faz A2 Cikis Kriteri

- [x] KB artik tum EduAI cekirdek domainlerini kapsiyor
- [x] Grounded retrieval icin yeterli ilk omurga kuruldu
- [x] Faz B'ye gecmek icin gerekli KB domain tabani olustu
