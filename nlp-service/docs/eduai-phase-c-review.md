# EduAI Phase C Review

## Faz C Amaci

Faz C'nin amaci, Faz B sonrasinda yuksek gorunen intent skorlarinin gercek kullanici diline ne kadar dayandigini test etmektir. Bu fazda kontrollu train/validation skorlarindan cikilip daha zor, daha kirli ve daha sahaya yakin sorularla runtime seviyesinde olcum yapilmistir.

## Kullanilan Test Yaklasimi

- Ayrik bir `phase_c_hard_test.json` dosyasi olusturuldu.
- Test seti 7 intent omurgasina gore kuruldu.
- Orneklerde su tip kaliplar kullanildi:
  - typo
  - kisa soru
  - yarim baglam
  - cift anlami olan soru
  - birbirine yakin intentleri zorlayan ifade bicimleri
- Olcum runtime classifier ustunden yapildi.
- Ayni anda KB hit rate de olculdu.

## Faz C Sonuclari

### Intent Sonuclari

- Faz B kontrollu evaluate BERT skoru: `0.9331`
- Faz C hard runtime accuracy: `0.82`
- Faz C macro F1: `0.82`

Bu sonuc cok onemlidir. Faz B'deki yuksek skor korunmamistir. Model gercekci ve kirli kullanici dilinde asagi dusmektedir.

### KB Sonuclari

- KB hit rate: `0.9333`

Bu da su anlama gelir:

`Asil darboğaz knowledge base degil, intent genelleme kalitesidir.`

KB omurgasi buyuk olcude gorevini yapmaktadir. Sistem daha cok intent ayriminda ve sinir durumlarinda zorlanmaktadir.

## En Guclu Alanlar

- `scholarship_support`
  - precision: `1.00`
  - recall: `1.00`
  - F1: `1.00`
- `exam_and_grading`
  - precision yuksek
  - belirgin sinyal tasiyan sorularda guclu
- `student_life`
  - genis olmasina ragmen kabul edilebilir bir seviyede

## En Zayif Alanlar

- `library`
  - recall: `0.62`
  - kutuphane ile dijital sistem siniri karisiyor
- `digital_systems`
  - kutuphane dis erisim ve VPN benzeri sorularda karisiyor
- `course_registration`
  - bazi CAP, muafiyet, kayit sildirme tarzinda sorularda sinir zayif
- `student_services`
  - psikolojik destek / engelli ogrenci destegi gibi konularda `student_life` ile karisiyor

## Sert Teknik Teshis

Faz C bize sunu net sekilde gosterdi:

1. Faz B dataset'i yararli ama hala fazla temizdir.
2. Model baglam ogrenmis olsa da sinir durumlarinda yuzeysel sinyallere kaymaktadir.
3. `student_services` ve `student_life` siniri hala yeterince temiz degildir.
4. `library` ve `digital_systems` arasi ortak terimler karisiklik uretmektedir.
5. Kontrollu validasyon skoru tek basina guvenilir kalite gostergesi degildir.

## Faz C Sonrasi Dürüst Sonuc

EduAI intent katmani artik kirik degildir. Ancak henüz "saha sertligine tam hazir" seviyede de degildir.

En dogru tanim sudur:

`EduAI, grounded KB omurgasi guclu olan; fakat intent ayriminda gercek dunya varyasyonlarina karsi hala iyilestirme gerektiren hibrit bir universite asistanidir.`

## Faz D Icın Oncelikler

Bir sonraki fazda odak su olmalidir:

1. `student_services` ve `student_life` sinirini netlestirmek
2. `library` ve `digital_systems` icin hard-negative veri arttirmak
3. typo, kisa soru ve cift niyetli ornekleri dataset'e daha fazla katmak
4. intent confidence dusuk oldugunda retrieval agirligini arttirmak
5. runtime hata analizine gore altin veri setini yeniden temizlemek

## Kisa Hukum

Faz C basarili olmustur; cunku sistemi pohpohlamamis, gercek zayifliklari ortaya cikarmistir.

Bu fazin sonucu bir zafer raporu degil, gercek durum raporudur:

`KB guclu, intent iyi ama hala kirilgan.`
