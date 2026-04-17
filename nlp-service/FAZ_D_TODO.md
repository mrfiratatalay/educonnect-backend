# Faz D TODO

Amaç: EduAI intent katmanının Faz C'de görülen kırılgan sınırlarını gerçekten iyileştirmek.

- [x] Faz C hata kümelerini intent sınırlarına göre ayır
- [x] `student_services` vs `student_life` için hedefli hard-negative veri ekle
- [x] `library` vs `digital_systems` için hedefli hard-negative veri ekle
- [x] `course_registration` sınırları için CAP / muafiyet / kayıt sildirme örnekleri ekle
- [x] Faz C hard test örneklerini eğitim veri hattına kontrollü şekilde dahil et
- [x] KB'de psikolojik destek / ODK / engelli destek kayıtlarına `student_life` alias bağla
- [x] KB'de kutuphane uzaktan erişim kayıtlarını alias/anahtar kelime açısından sertleştir
- [x] Intent fallback keyword haritasını çakışmaları azaltacak şekilde daralt
- [x] BERT düşük güven / düşük margin durumları için resolver katmanı ekle
- [x] Dataset'i yeniden üret
- [x] Modeli yeniden eğit
- [x] Hard evaluation ve smoke test'i tekrar çalıştır
- [x] Faz D değerlendirme notunu yaz
