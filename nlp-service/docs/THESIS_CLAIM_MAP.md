# EduAI — Tez İddia Eşleştirmesi

> Bu doküman, tez raporundaki ana teknik iddiaları EduAI uygulamasındaki
> somut karşılıklarıyla eşleştirir. Savunmada "bunu nasıl yaptınız?" sorusuna
> doğrudan referans verebilmenizi sağlar.

---

## 1. NLP Tabanlı Intent Classification

**Tez İddiası:** "Kullanıcı mesajları, doğal dil işleme ile otomatik olarak niyet kategorilerine sınıflandırılmaktadır."

**Uygulamadaki Karşılığı:**

| Bileşen | Dosya | Detay |
|---------|-------|-------|
| Intent Service | `services/intent_service.py` | BERTurk fine-tuned + keyword-fallback, 10 intent sınıfı |
| Eğitim Script | `training/train_intent.py` | BERTurk fine-tuning pipeline |
| Değerlendirme | `training/evaluate.py` | Keyword vs TF-IDF+SVM vs BERTurk karşılaştırma |
| Dataset | `data/dataset.json` | 144 etiketlenmiş Türkçe örnek |
| API | `routers/nlp.py` → `POST /api/nlp/classify` | REST endpoint |

**Kanıt:** Gerçek API çağrısı → intent + confidence dönüyor.

---

## 2. Bilgi Tabanı Destekli Grounded Cevap Üretimi

**Tez İddiası:** "Chatbot yanıtları, üniversitenin resmi kaynaklarından derlenen bir bilgi tabanına dayandırılmaktadır (grounding)."

**Uygulamadaki Karşılığı:**

| Bileşen | Dosya | Detay |
|---------|-------|-------|
| Knowledge Base | `data/knowledge_base.json` | 60+ yapılandırılmış FAQ, schema v2.0 |
| KB Service | `services/knowledge_service.py` | Keyword + question similarity scoring |
| Grounding Logic | `ChatbotService.cs` → `IsGrounded()` | KB score ≥ 0.25 + confidence eşiği |
| Grounded Prompt | `ChatbotService.cs` → `BuildGroundedPrompt()` | KB cevabını temel alan prompt |
| Fallback Prompt | `ChatbotService.cs` → `BuildFallbackPrompt()` | Uydurma bilgi engelleyen temkinli prompt |

**Kanıt:** Grounded cevapta kaynak URL ve başlık referans veriliyor. Fallback durumunda resmi birime yönlendirme yapılıyor.

---

## 3. Yapılandırılmış Entity Extraction

**Tez İddiası:** "Kullanıcı mesajlarından birim adları, sistem adları ve zaman ifadeleri otomatik olarak çıkarılmaktadır."

**Uygulamadaki Karşılığı:**

| Bileşen | Dosya | Detay |
|---------|-------|-------|
| Entity Dictionary | `data/entity_dictionary.json` | Birim, sistem, yer adları + alias'ları |
| Extraction Logic | `services/intent_service.py` → `_extract_entities()` | Sözlük tabanlı regex match |
| Temporal Detection | Aynı fonksiyon | "ne zaman", "hangi tarih", "kaçta" gibi zaman ifadeleri |

**Kanıt:** API response'undaki `entities[]` dizisi, her entity'nin type, value, start, end bilgisi.

---

## 4. Kullanıcı Geri Bildirim Mekanizması

**Tez İddiası:** "Chatbot yanıtlarının kalitesi, kullanıcı geri bildirimiyle ölçülmektedir."

**Uygulamadaki Karşılığı:**

| Bileşen | Dosya | Detay |
|---------|-------|-------|
| Feedback Entity | `Domain/Entities/ChatMessageFeedback.cs` | Mesaj bazlı helpful/not-helpful + yorum |
| Feedback Endpoint | `ChatController.cs` → `POST /messages/{id}/feedback` | Geri bildirim gönderme |
| Message Response | `ChatContracts.cs` → `ChatMessageResponse` | `HasFeedback`, `FeedbackIsHelpful` alanları |
| DB Config | `AppDbContext.cs` → `ConfigureChat()` | One-to-one ilişki, unique index |

**Kanıt:** Feedback API çağrısı + mesaj geçmişinde feedback durumu görünüyor.

---

## 5. Analitik ve Performans Ölçümü

**Tez İddiası:** "Chatbot performansı yapılandırılmış metriklerle izlenmekte ve raporlanmaktadır."

**Uygulamadaki Karşılığı:**

| Bileşen | Dosya | Detay |
|---------|-------|-------|
| Metrik Alanları | `ChatMessage.cs` | `ModelUsed`, `KbScore`, `KbHit`, `IsFallback`, `LatencyMs` |
| Latency Ölçümü | `ChatbotService.cs` | `Stopwatch` ile uçtan uca süre |
| NLP Latency | `routers/nlp.py` | `time.perf_counter()` ile NLP pipeline süresi |
| Analytics Endpoint | `ChatController.cs` → `GET /analytics/summary` | Admin seviye özet rapor |
| Unresolved Detection | Analytics sorgusu | Fallback + no KB hit veya düşük confidence → çözülmemiş |

**Kanıt:** Analytics response'u → HelpfulRate, KbHitRate, FallbackRate, IntentDistribution, TopUnresolvedQueries.

---

## 6. Görsel Arama (Visual Search)

**Tez İddiası:** "Platform, ürün görselleri üzerinden benzerlik bazlı arama yapabilmektedir."

**Uygulamadaki Karşılığı:**

| Bileşen | Dosya | Detay |
|---------|-------|-------|
| Vision Service | `services/vision_service.py` | ResNet-50 feature extraction |
| Similarity | `VisionService.compute_similarity()` | Cosine similarity |
| API | `routers/vision.py` → `POST /api/vision/extract` | Embedding extraction |
| C# Entegrasyonu | `VisionEmbeddingApiService.cs` + `VisualSearchService.cs` | Backend entegrasyonu |

---

## 7. Çok Katmanlı Mimari

**Tez İddiası:** "Sistem, temiz mimari (Clean Architecture) prensiplerine uygun olarak katmanlı bir yapıda geliştirilmiştir."

**Uygulamadaki Karşılığı:**

```
EduConnect.Api          → Controller, Hub, Middleware (request handling)
EduConnect.Application  → Interface, Contract, DTO (iş kuralları sözleşmeleri)
EduConnect.Domain       → Entity, Enum (domain modeli)
EduConnect.Infrastructure → EF Core, Servisler, Options (implementasyon)
nlp-service             → Python FastAPI mikro servis (NLP + Vision)
```

**Kanıt:** Proje yapısı, DependencyInjection.cs, interface/implementasyon ayrımı.

---

## Özet Eşleştirme Tablosu

| # | Tez İddiası | Durum | Ana Kanıt |
|---|-------------|-------|-----------|
| 1 | Intent classification | ✅ Tam | API response (intent + confidence) |
| 2 | Grounded cevap üretimi | ✅ Tam | KB score + source referans |
| 3 | Entity extraction | ✅ Tam | API response (entities[]) |
| 4 | Geri bildirim | ✅ Tam | Feedback endpoint + DB kaydı |
| 5 | Analitik ölçüm | ✅ Tam | Analytics summary endpoint |
| 6 | Görsel arama | ✅ Tam | ResNet-50 + cosine similarity |
| 7 | Clean Architecture | ✅ Tam | 4-katman proje yapısı |
