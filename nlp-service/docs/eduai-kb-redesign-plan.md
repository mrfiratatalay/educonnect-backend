# EduAI KB Redesign Plan

## Neden Bu Faz Gerekiyor

Su an EduAI tarafinda en buyuk asimetri su:

- `dataset.json` = 1098 intent ornegi
- `knowledge_base.json` = 48 kayit

Bu yapi rapordaki EduAI hedefine ters. Raporda asil deger:

- universiteye ozel bilgi tabani
- intent recognition
- entity recognition
- guvenilir ve hizli cevap

Bu nedenle Faz A1'in ana isi `daha cok sentetik soru yazmak` degil, `KB omurgasini dogru tasarlamak`.

## Nihai Domain Onerisi

EduAI icin KB tarafinda 7 domain yeterli:

1. `exam_and_grading`
2. `course_registration`
3. `digital_systems`
4. `student_services`
5. `library`
6. `student_life`
7. `scholarship_support`

## Neden 7 Domain

### Ayrik kalmasi gerekenler

- `exam_and_grading`
  Sinav, not, butunleme, mazeret ve tek ders gibi akademik kurallar baska domainlerle gereksiz karismamali.
- `course_registration`
  Ders kaydi, kayit yenileme, intibak, muafiyet ve danisman onayi ayri bir karar alani.
- `digital_systems`
  REBIS, OBS, sifre, e-posta, Office 365 ve VPN gibi sorular teknik dogalari yuzunden ayri olmali.
- `student_services`
  Transkript, ogrenci belgesi, diploma, ilisik kesme, OIDB ve ODK tipi resmi surecler burada toplanmali.
- `library`
  Kutuphane retrieval'i ayri tutulursa hem keyword hem entity hem de source kalitesi daha yuksek olur.
- `scholarship_support`
  Burs, maddi destek ve yemek bursu kendi sinyalini tasiyor.

### Birlestirilmesi gerekenler

- `cafeteria` -> `student_life`
- `campus_life` -> `student_life`
- `campus_logistics` -> `student_life`

Bu uc alanin ayri intent olarak kalmasi classifier'a gereksiz yuk bindiriyor. KB tarafinda bunlar su alt topic'lerle ayni domain icinde tutulabilir:

- `yemekhane`
- `ulasim`
- `barinma`
- `kampus`
- `etkinlik`
- `topluluk`
- `spor`
- `psikolojik_destek`

## Marketplace Karari

`marketplace` genel urun mimarisinde olabilir ama EduAI cekirdeginde birincil universite asistani konusu degil.

Bu nedenle Faz A1 karari:

- `marketplace` intenti EduAI KB cekirdeginden cikacak
- gerekiyorsa ayri urun modulu olarak ele alinacak
- EduAI sadece gerekli oldugunda pazar modulune yonlendirme yapacak

## KB Kayit Semasi

Her kayitta asgari olarak su alanlar olmali:

- `id`
- `intent`
- `intent_aliases`
- `topic`
- `question`
- `answer`
- `keywords`
- `source_url`
- `source_title`
- `confidence`
- `time_sensitive`
- `faculty_scope`

## Kabul Kriterleri

Bir kayit KB'ye girecekse:

- resmi veya birinci taraf kaynaga dayanacak
- tek bir ogrenci sorusuna net cevap verecek
- cevap 1-3 kisa paragrafta savunulabilir olacak
- intent secimi domain mantigina uygun olacak
- en az bir anlamli keyword tasiyacak
- mumkunse bir entity ile baglanabilecek

## Red Kriterleri

Asagidakiler ilk turda alinmayacak:

- tek seferlik duyurular
- tarihi gecmis kampanya ve ilanlar
- kaynagi acik olmayan bilgiler
- sadece uzun prose olan, soru-cevaplastirilmasi zayif metinler
- birden fazla konuya dagilan belirsiz aciklamalar

## Hedef Dagilim

Faz A2 sonunda hedef kayit sayilari:

- `exam_and_grading`: 40
- `course_registration`: 35
- `digital_systems`: 30
- `student_services`: 45
- `library`: 20
- `student_life`: 30
- `scholarship_support`: 25

Toplam hedef:
`225 kayit`

## Kaynak Oncelik Sirasi

### Birincil kaynaklar

1. `rteu/openai/02-rteu-sinav-not-sistemi-ve-akademik-takvim.md`
2. `rteu/openai/03-rteu-dijital-sistemler-ve-teknik-erisim.md`
3. `rteu/openai/04-rteu-kutuphane-ve-akademik-erisim.md`
4. `rteu/openai/06-rteu-ogrenci-belgeleri-ve-resmi-evraklar.md`
5. `rteu/perplexity/08-rteu-yemekhane-ve-beslenme-hizmetleri.md`
6. `rteu/perplexity/09-rteu-burslar-ve-maddi-destekler.md`
7. `rteu/perplexity/10-rteu-psikolojik-ve-ogrenci-destek-hizmetleri.md`
8. `rteu/perplexity/11-rteu-kurumsal-terimler-ve-birim-sozlugu.md`

### Ikincil kaynaklar

1. `rteu/openai/05-rteu-guncel-ogrenci-surecleri-ve-kurumsal-isleyis-ozeti.md`
2. `rteu/perplexity/01-rteu-kapsamli-genel-bilgi-havuzu.md`
3. `rteu/perplexity/03-rteu-eduai-cekirdek-konu-ozetleri-ve-terim-listesi.md`

## Domain -> Kaynak Eslesmesi

### `exam_and_grading`

- Birincil:
  - `rteu/openai/02-rteu-sinav-not-sistemi-ve-akademik-takvim.md`
  - `rteu/perplexity/04-rteu-sinav-not-sistemi-ve-akademik-takvim.md`
- Ikincil:
  - `rteu/openai/05-rteu-guncel-ogrenci-surecleri-ve-kurumsal-isleyis-ozeti.md`
  - `rteu/perplexity/03-rteu-eduai-cekirdek-konu-ozetleri-ve-terim-listesi.md`

### `course_registration`

- Birincil:
  - `rteu/openai/05-rteu-guncel-ogrenci-surecleri-ve-kurumsal-isleyis-ozeti.md`
  - `rteu/perplexity/01-rteu-kapsamli-genel-bilgi-havuzu.md`
- Ikincil:
  - `rteu/perplexity/03-rteu-eduai-cekirdek-konu-ozetleri-ve-terim-listesi.md`
  - `rteu/perplexity/11-rteu-kurumsal-terimler-ve-birim-sozlugu.md`

### `digital_systems`

- Birincil:
  - `rteu/openai/03-rteu-dijital-sistemler-ve-teknik-erisim.md`
  - `rteu/perplexity/05-rteu-dijital-sistemler-ve-teknik-erisim.md`
- Ikincil:
  - `rteu/perplexity/11-rteu-kurumsal-terimler-ve-birim-sozlugu.md`
  - `rteu/openai/00-rteu-resmi-alan-adlari-ve-operasyonel-arastirma-raporu.md`

### `student_services`

- Birincil:
  - `rteu/openai/06-rteu-ogrenci-belgeleri-ve-resmi-evraklar.md`
  - `rteu/perplexity/10-rteu-psikolojik-ve-ogrenci-destek-hizmetleri.md`
- Ikincil:
  - `rteu/openai/05-rteu-guncel-ogrenci-surecleri-ve-kurumsal-isleyis-ozeti.md`
  - `rteu/perplexity/11-rteu-kurumsal-terimler-ve-birim-sozlugu.md`

### `library`

- Birincil:
  - `rteu/openai/04-rteu-kutuphane-ve-akademik-erisim.md`
  - `rteu/perplexity/07-rteu-kutuphane-ve-akademik-erisim.md`
- Ikincil:
  - `rteu/perplexity/11-rteu-kurumsal-terimler-ve-birim-sozlugu.md`

### `student_life`

- Birincil:
  - `rteu/perplexity/08-rteu-yemekhane-ve-beslenme-hizmetleri.md`
  - `rteu/perplexity/02-rteu-ogrenci-yasami-ve-destek-hizmetleri.md`
- Ikincil:
  - `rteu/perplexity/10-rteu-psikolojik-ve-ogrenci-destek-hizmetleri.md`
  - `rteu/perplexity/01-rteu-kapsamli-genel-bilgi-havuzu.md`

### `scholarship_support`

- Birincil:
  - `rteu/perplexity/09-rteu-burslar-ve-maddi-destekler.md`
- Ikincil:
  - `rteu/perplexity/10-rteu-psikolojik-ve-ogrenci-destek-hizmetleri.md`
  - `rteu/perplexity/01-rteu-kapsamli-genel-bilgi-havuzu.md`

## Topic Isimlendirme Kurali

`topic` alanlari su kuralla yazilacak:

- kucuk harf
- ascii
- bosluk yok
- kelimeler `_` ile ayrilacak
- genel baslik degil, atomik alt konu olacak

Ornekler:

- `butunleme`
- `mazeret_sinavi`
- `tek_ders_sinavi`
- `harf_notu`
- `ders_ekleme_birakma`
- `danisman_onayi`
- `rebis_giris`
- `sifre_sifirlama`
- `ogrenci_belgesi`
- `transkript`
- `kampus_disi_erisim`
- `odunc_kurali`
- `yemek_rezervasyonu`
- `yemek_bursu`
- `psikolojik_destek`

## Metadata Kullanimi

### `confidence`

- `high`
  - resmi yonetmelik, resmi surec sayfasi veya net kurumsal aciklama
- `medium`
  - resmi kaynak var ama ifade eksik, daginik veya dolayli
- `low`
  - ilk A2 turunda kullanilmayacak

### `time_sensitive`

- `true`
  - fiyat, saat, tarih, donemsel uygulama veya duyuru bagimli bilgi
- `false`
  - genel kural, surec mantigi, belge akisi veya sistem tanimi

### `faculty_scope`

- `general`
  - tum ogrenciler icin gecerli
- `tip`
- `sbf`
- `ziraat`

Fakulteye ozel uygulama yoksa varsayilan her zaman `general` olacak.

## Entity Baglama Kurali

Her KB kaydi ideal olarak en az bir entity grubuna baglanmali:

- `units`
- `systems`
- `documents`
- `exam_terms`
- `academic_terms`
- `faculty_names`
- `campus_names`
- `library_terms`
- `support_terms`

Bir kayitta dogrudan entity yoksa:

- en az iki guclu `keyword` bulunmali
- ve `topic` alanı daha spesifik secilmeli

## Markdown -> KB Donusum Akisi

Faz A2'de her kaynak dosya icin ayni is akisi uygulanacak:

1. Konu bolumunu sec
2. Bolumdeki atomik bilgi parcalarini ayir
3. O bilgi icin ogrencinin sorabilecegi net soruyu yaz
4. 1-3 kisa paragrafta resmi cevabi yaz
5. `intent` ata
6. `topic` ata
7. `keywords` sec
8. `source_url` ve `source_title` ekle
9. `confidence`, `time_sensitive`, `faculty_scope` belirle
10. Mumkunse entity bagla
11. Tekrar ve cakisma kontrolu yap

## Kalite Kontrol Listesi

Her 25 kayitta bir asagidaki kontrol yapilacak:

- ayni sorunun kopyasi var mi
- cevap resmi kaynaga sadik mi
- intent yanlis secilmis mi
- topic fazla genel mi
- time-sensitive bilgi etiketsiz kalmis mi
- ayni cevap farkli domainlere dagitilmis mi
- soru ogrenci diliyle sorulabilir mi
- cevap fazla uzun ya da belirsiz mi
- entity veya keyword sinyali zayif mi
- retrieval icin ayirt edicilik yeterli mi

## Faz A2 Veri Cikarma Sirasi

1. `exam_and_grading`
2. `digital_systems`
3. `student_services`
4. `course_registration`
5. `library`
6. `scholarship_support`
7. `student_life`

## Faz A2 Giris Kriteri

Faz A2'ye gecmek icin:

- domain seti kesinlesmeli
- KB schema sabitlenmeli
- kaynak sirasi yazilmis olmali
- `topic` isimlendirme kurali hazir olmali
- ilk uc domain icin veri cikarma sirasi belirlenmis olmali
