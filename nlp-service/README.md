---
title: EduConnect NLP & Vision
emoji: 🎓
colorFrom: blue
colorTo: indigo
sdk: docker
app_port: 7860
pinned: false
license: mit
---

# EduConnect NLP & Vision Service

EduConnect projesi için intent classification (BERTurk) ve görsel benzerlik (ResNet-50) sağlayan FastAPI servisi.

## Endpoint'ler

- `GET /health` — servis durumu
- `POST /api/nlp/classify` — intent + entity + knowledge base lookup
- `POST /api/vision/extract` — ResNet-50 görsel embedding
- `POST /api/vision/similarity` — iki embedding arası cosine benzerlik

## Çağrı Örneği

```bash
curl -X POST https://firatatalay-educonnect-nlp.hf.space/api/nlp/classify \
  -H "Content-Type: application/json" \
  -d '{"text": "Bilgisayar mühendisliği bölümü nasıl?"}'
```

## Environment Variables

- `ALLOWED_ORIGINS` (opsiyonel): virgülle ayrılmış CORS origin listesi. Varsayılan: `http://localhost:5160`.
