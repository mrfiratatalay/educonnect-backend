# Faz B TODO

Amaç: EduAI intent katmanını Faz A’da kurulan 7 domainli knowledge base omurgasına hizalamak.

- [x] Intent taksonomisini 10 etiketten 7 etikete düşür
- [x] Eski dataset intentlerini yeni yapıya map et
- [x] `marketplace` intentini EduAI datasetinden çıkar
- [x] `campus_life`, `campus_logistics`, `cafeteria` örneklerini `student_life` altında birleştir
- [x] KB sorularından destek alan temiz bir Faz B dataset üretim scripti yaz
- [x] `dataset.json` dosyasını yeni 7 domain yapısıyla yeniden üret
- [x] `intent_service.py` içindeki label ve keyword mantığını yeni taksonomiye taşı
- [x] `train_intent.py` label uzayını yeni intent setine göre güncelle
- [x] `evaluate.py` karşılaştırma hattını yeni intent setine göre güncelle
- [x] Yeni dataset dağılımını ve toplam örnek sayısını kontrol et
- [x] Retrain + evaluate
- [x] Sonuçları sert biçimde değerlendir
