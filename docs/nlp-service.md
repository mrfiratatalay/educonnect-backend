# EduConnect — NLP & Vision Servisi

Canlı adres: https://firatatalay-educonnect-nlp.hf.space  
Deploy: Hugging Face Spaces (CPU Basic, 16GB RAM ücretsiz)

## Servis Yapısı

```
nlp-service/
├── main.py                        # FastAPI uygulama girişi
├── routers/
│   ├── nlp.py                     # Intent + entity endpoint'leri
│   └── vision.py                  # Görsel embedding endpoint'leri
├── services/
│   ├── intent_service.py          # BERTurk fine-tuned sınıflandırıcı
│   ├── vision_service.py          # ResNet-50 embedding çıkarıcı
│   └── knowledge_service.py       # KB araması (BM25 benzeri skor)
├── models/
│   └── intent_classifier/         # Fine-tuned safetensors (423MB, Git LFS)
├── data/
│   ├── knowledge_base.json        # RTEU'ya özgü FAQ + entity verileri
│   └── entity_dictionary.json     # Bölüm, birim, yer isim eşleştirmeleri
├── training/
│   ├── train.py                   # Fine-tuning scripti
│   └── augment.py                 # Sentetik veri augmentation
├── requirements.txt
└── Dockerfile                     # python:3.11-slim, CPU-only torch
```

## Intent Sınıflandırması

**Model**: [dbmdz/bert-base-turkish-cased](https://huggingface.co/dbmdz/bert-base-turkish-cased) tabanlı fine-tuned BERTurk

**Eğitim verisi**: RTEU'ya özel sentetik Türkçe soru-intent çiftleri (augmentation ile çoğaltıldı)

**Intent kategorileri** (örnek): kayıt, burs, yemekhane, kütüphane, akademik takvim, ulaşım, sağlık, etkinlikler, bölümler, genel bilgi

**Çıktı**: intent label + confidence score (0–1)

## Görsel Arama (Visual Search)

**Model**: ResNet-50 (torchvision, ImageNet ağırlıkları, son FC katmanı çıkarıldı)

**Çalışma şekli**:
1. Ürün yüklendiğinde embedding hesaplanır ve veritabanına kaydedilir
2. Arama sırasında yüklenen görsel de embedding'e dönüştürülür
3. Cosine similarity ile en yakın ürünler sıralanır

## API Endpoint'leri

```
POST /nlp/analyze       → intent + entity + KB araması
POST /vision/embed      → görsel → embedding vektörü
POST /vision/search     → görsel → benzer ürünler (backend'den çağrılır)
GET  /health            → servis sağlık kontrolü
```

## Neden Hugging Face Spaces?

- 16GB RAM ücretsiz — PyTorch + BERTurk'ün ~1.5GB ihtiyacını karşılar
- Backend'in 512MB RAM limitini aşmaz
- Modeli güncellemek için backend deploy gerekmez
- Space uyku moduna girerse ilk request ~10-30sn bekler (ücretsiz tier kısıtı)
