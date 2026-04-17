# EduAI — Teslim ve Çalıştırma Notları

> Projenin sıfırdan ayağa kaldırılması için gerekli tüm adımlar.

---

## Ön Gereksinimler

| Gereksinim | Versiyon | Not |
|-----------|---------|-----|
| .NET SDK | 10.0+ | `dotnet --version` ile kontrol |
| SQL Server Express | 2019+ | LocalDB veya Express kurulumu |
| Node.js | 20+ | Frontend için |
| Python | 3.11+ | NLP service için |
| Git | 2.x | Kaynak kod yönetimi |

---

## 1. Backend (ASP.NET Core) Çalıştırma

```powershell
# Proje dizinine git
cd backend

# Build
dotnet build

# Migration uygula (SQL Server Express çalışır durumda olmalı)
dotnet ef database update -p src/EduConnect.Infrastructure -s src/EduConnect.Api

# Çalıştır
dotnet run --project src/EduConnect.Api
```

**Varsayılan Adresler:**
- API: `http://localhost:5160`
- Swagger: `http://localhost:5160/swagger`
- Health: `http://localhost:5160/health`

**Varsayılan Admin:**
- Email: `admin@educonnect.local`
- Şifre: `Admin123!`

---

## 2. NLP & Vision Service (Python) Çalıştırma

```powershell
cd backend/nlp-service

# Sanal ortam oluştur (ilk seferde)
python -m venv venv
.\venv\Scripts\Activate.ps1

# Bağımlılıkları kur
pip install -r requirements.txt

# Çalıştır
uvicorn main:app --host 0.0.0.0 --port 8000 --reload
```

**Varsayılan Adresler:**
- NLP API: `http://localhost:8000`
- Health: `http://localhost:8000/health`
- NLP Health: `http://localhost:8000/api/nlp/health`
- Swagger: `http://localhost:8000/docs`

---

## 3. Frontend (React) Çalıştırma

```powershell
cd frontend

# Bağımlılıkları kur
npm install

# Dev server başlat
npm run dev
```

**Varsayılan Adres:** `http://localhost:5173`

---

## 4. Çalıştırma Sırası

Servisleri şu sırayla başlatın:

1. **SQL Server** — Zaten çalışır durumda olmalı
2. **NLP Service** — `uvicorn main:app --port 8000`
3. **Backend API** — `dotnet run --project src/EduConnect.Api`
4. **Frontend** — `npm run dev`

> **Not:** Backend, NLP service olmadan da çalışır. NLP çağrısı başarısız olursa
> `student_services` intent'i ve 0.3 confidence ile fallback döner.

---

## 5. Yapılandırma Dosyaları

### Backend: `src/EduConnect.Api/appsettings.json`

| Ayar | Varsayılan | Açıklama |
|------|-----------|----------|
| `ConnectionStrings:DefaultConnection` | `localhost\SQLEXPRESS` | SQL Server bağlantısı |
| `NlpService:BaseUrl` | `http://localhost:8000` | Python NLP service adresi |
| `NlpService:ConfidenceThreshold` | `0.7` | Grounding kararı eşiği |
| `GeminiSettings:ApiKey` | Config'te tanımlı | Google Gemini API anahtarı |
| `GeminiSettings:ChatModel` | `gemini-2.5-flash` | Kullanılan LLM modeli |

### NLP Service
- Model dizini: `nlp-service/models/intent_classifier/`
- Dataset: `nlp-service/data/dataset.json`
- Knowledge base: `nlp-service/data/knowledge_base.json`
- Entity sözlüğü: `nlp-service/data/entity_dictionary.json`

---

## 6. NLP Model Eğitimi (Opsiyonel)

```powershell
cd backend/nlp-service
.\venv\Scripts\Activate.ps1

# BERTurk fine-tuning
python training/train_intent.py --dataset data/dataset.json

# Model karşılaştırma (keyword vs TF-IDF+SVM vs BERTurk)
python training/evaluate.py --dataset data/dataset.json
```

> **Not:** Mevcut dataset (144 örnek) ile BERTurk 0.17 accuracy üretir. Model accuracy 0.60'ın altında olduğu için runtime'da otomatik olarak keyword-fallback kullanılır.

---

## 7. Smoke Test (Manuel)

NLP service çalışır durumdayken:

```powershell
# Intent classification + KB lookup
curl -X POST http://localhost:8000/api/nlp/classify `
  -H "Content-Type: application/json" `
  -d '{"text": "Final sinavinda minimum kac almam gerekiyor?"}'

# Health check
curl http://localhost:8000/health
```

---

## 8. Bilinen Sorunlar ve Notlar

1. **ResNet-50 model yüklemesi** — İlk çalıştırmada ~400MB model indirilir, sabırlı olun
2. **BERTurk model yüklemesi** — `models/intent_classifier/model.safetensors` ~442MB
3. **SQL Server bağlantısı** — `localhost\SQLEXPRESS` çalışmıyorsa connection string'i güncelleyin
4. **Gemini API** — API anahtarı geçersizse chatbot "yanıt üretilemedi" döner, NLP kısmı çalışmaya devam eder
5. **CORS** — Backend CORS, `localhost:5160` ve `localhost:5173` için açıktır
