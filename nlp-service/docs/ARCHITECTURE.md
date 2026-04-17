# EduAI — Teknik Mimari Özeti

> Bu doküman, EduConnect platformuna entegre edilen EduAI chatbot ve NLP altyapısının
> tez/sunum amaçlı teknik mimari özetini içerir.

---

## 1. Genel Sistem Görünümü

```
┌─────────────────────────────────────────────────────────────────┐
│                        EduConnect Frontend                       │
│                     (React + Ant Design + Vite)                  │
│                       http://localhost:5173                       │
└───────────┬─────────────────────────────────────┬───────────────┘
            │  REST / SignalR                      │  REST
            ▼                                      ▼
┌───────────────────────┐         ┌──────────────────────────────┐
│  EduConnect Backend   │         │   NLP & Vision Service        │
│  ASP.NET Core (.NET 10)│ ◄─────►│   Python / FastAPI            │
│  http://localhost:5160 │  HTTP   │   http://localhost:8000       │
│                        │         │                               │
│  ┌──────────────┐      │         │  ┌─────────────────────────┐ │
│  │ ChatController│     │         │  │  /api/nlp/classify       │ │
│  │ ChatbotService│     │         │  │  IntentService           │ │
│  │ NlpApiService │─────┼────────►│  │  KnowledgeService        │ │
│  └──────────────┘      │         │  └─────────────────────────┘ │
│                        │         │  ┌─────────────────────────┐ │
│  ┌──────────────┐      │         │  │  /api/vision/extract     │ │
│  │VisualSearch   │─────┼────────►│  │  VisionService (ResNet50)│ │
│  │Service        │     │         │  └─────────────────────────┘ │
│  └──────────────┘      │         └──────────────────────────────┘
│                        │
│  ┌──────────────┐      │         ┌──────────────────────────────┐
│  │GeminiApiService│────┼────────►│  Google Gemini API            │
│  └──────────────┘      │  HTTPS  │  (gemini-2.5-flash)          │
│                        │         └──────────────────────────────┘
│  ┌──────────────┐      │
│  │ SQL Server    │     │
│  │ (EF Core)     │     │
│  └──────────────┘      │
└────────────────────────┘
```

---

## 2. EduAI NLP Pipeline

Bir kullanıcı mesajı geldiğinde aşağıdaki akış işler:

```
Kullanıcı Mesajı
      │
      ▼
┌─────────────────┐
│ 1. Intent        │   BERTurk fine-tuned model (accuracy ≥ 0.60 ise)
│    Classification│   veya keyword-fallback (aksi halde)
└────────┬────────┘
         │ intent + confidence
         ▼
┌─────────────────┐
│ 2. Entity        │   Sözlük tabanlı entity extraction
│    Extraction    │   (entity_dictionary.json — yer, birim, sistem adları)
└────────┬────────┘
         │ entities[]
         ▼
┌─────────────────┐
│ 3. Knowledge     │   Keyword + question similarity scoring
│    Base Lookup   │   (knowledge_base.json — 60+ yapılandırılmış FAQ)
└────────┬────────┘
         │ kb_answer (score, confidence, source)
         ▼
┌─────────────────┐
│ 4. Grounding     │   Grounded mı? (KB score ≥ 0.25 + intent confidence ≥ threshold)
│    Decision      │   Evet → KB cevabını temel al, Gemini ile doğallaştır
│                  │   Hayır → Temkinli fallback prompt, uydurma bilgi engelle
└────────┬────────┘
         │ enriched prompt
         ▼
┌─────────────────┐
│ 5. Gemini        │   Grounded prompt veya fallback prompt ile
│    Generation    │   Türkçe, öğrenci dostu yanıt üretimi
└────────┬────────┘
         │ final response
         ▼
┌─────────────────┐
│ 6. Analytics     │   ModelUsed, KbScore, KbHit, IsFallback, LatencyMs
│    Recording     │   → ChatMessage tablosuna kaydedilir
└─────────────────┘
```

---

## 3. Intent Taksonomisi (10 Kategori)

| Intent | Açıklama | Örnek Soru |
|--------|----------|------------|
| `exam_and_grading` | Sınavlar, notlar, bütünleme, harf notu | "Final barajı kaç?" |
| `course_registration` | Ders kaydı, müfredat, danışman onayı | "Ders ekleme nasıl yapılır?" |
| `campus_life` | Etkinlikler, kulüpler, kampüs yaşamı | "Hangi kulüpler var?" |
| `library` | Kütüphane, ödünç, veritabanları | "Kütüphane saatleri nedir?" |
| `scholarship_support` | Burs, KYK, mali destek | "Burs başvurusu nasıl yapılır?" |
| `campus_logistics` | Yurt, ulaşım, yerleşke bilgisi | "Servis saatleri nedir?" |
| `cafeteria` | Yemekhane, menü, rezervasyon | "Yemekhane menüsü nerede?" |
| `marketplace` | İkinci el, ilan, görsel arama | "Ürün nasıl satarım?" |
| `digital_systems` | REBIS, OBS, e-posta, WiFi, VPN | "Şifremi unuttum ne yapmalıyım?" |
| `student_services` | Belge, transkript, diploma, psikolojik destek | "Transkript nereden alınır?" |

---

## 4. Teknoloji Yığını

### Backend (C# / .NET)
| Bileşen | Teknoloji |
|---------|-----------|
| Framework | ASP.NET Core (.NET 10) |
| ORM | Entity Framework Core |
| Veritabanı | SQL Server Express |
| Kimlik Doğrulama | JWT (Access + Refresh Token) |
| Gerçek Zamanlı | SignalR (Chat Hub) |
| LLM Entegrasyonu | Google Gemini API (gemini-2.5-flash) |
| Mimari | Clean Architecture (Api → Application → Domain ← Infrastructure) |

### NLP & Vision Service (Python)
| Bileşen | Teknoloji |
|---------|-----------|
| Framework | FastAPI + Uvicorn |
| NLP Model | BERTurk (dbmdz/bert-base-turkish-cased) — fine-tuned |
| ML Runtime | PyTorch + Transformers (HuggingFace) |
| Vision Model | ResNet-50 (pretrained, feature extraction) |
| Değerlendirme | scikit-learn (TF-IDF + SVM karşılaştırma) |

### Frontend
| Bileşen | Teknoloji |
|---------|-----------|
| Framework | React 19 + Vite |
| UI Kütüphanesi | Ant Design |
| State | React Context |
| Routing | React Router |

---

## 5. Veri Yapıları

### knowledge_base.json (Schema v2.0)
```json
{
  "schema_version": "2.0",
  "entries": [
    {
      "id": "kb_exam_001",
      "intent": "exam_and_grading",
      "intent_aliases": ["ExamSupport"],
      "topic": "ara_sinav_ve_final_yapisi",
      "question": "RTEU'de genel sınav sistemi nasıl işler?",
      "answer": "...",
      "keywords": ["sinav sistemi", "ara sinav", "final"],
      "source_url": "https://oidb.erdogan.edu.tr/...",
      "source_title": "Yönetmelik",
      "confidence": "high",
      "time_sensitive": false,
      "faculty_scope": "general"
    }
  ]
}
```

### dataset.json (Intent Eğitim Verisi)
```json
[
  { "text": "Final sınavına ne zaman gireceğiz?", "intent": "exam_and_grading" },
  { "text": "Ders kaydı nasıl yapılır?", "intent": "course_registration" }
]
```

### entity_dictionary.json
```json
{
  "groups": {
    "birimler": [
      { "value": "OIDB", "type": "unit", "aliases": ["oidb", "öğrenci işleri", ...] }
    ]
  }
}
```

---

## 6. Faz Gelişim Kronolojisi

| Faz | Konu | Durum |
|-----|------|-------|
| Faz 1 | Temel NLP servis iskeleti, BERTurk + FastAPI | ✅ |
| Faz 2 | Knowledge base, entity dictionary, scoring | ✅ |
| Faz 3 | Grounded prompt tasarımı, Gemini entegrasyonu | ✅ |
| Faz 4 | Intent taksonomisi iyileştirme, veri hizalama | ✅ |
| Faz 5 | Geri bildirim, analitik toplama, admin raporlama | ✅ |
| Faz 6 | Son doğrulama, tez dokümantasyonu | 🔄 |

---

## 7. Model Performans Notu

## 7. Model Performans Notu

BERTurk fine-tuned model, modeldeki "Veri Yetersizliği" problemini aşmak amaciyla uygulanan Sentetik Veri Çoğaltma (Data Augmentation) tekniği sayesinde **~400 örnek** ile eğitilmiş ve ilk fazlardaki %17 oranından sıçrayışla **0.3625** validation accuracy değerine ulaşmıştır.

Ancak bu oran, hedeflenen deterministik güven eşiği olan **0.60**'ın altında kaldığı için runtime'da otomatik olarak **keyword-fallback** stratejisi devreye girmektedir.

Bu davranış bir tasarım zayıflığı değil, **güvenlik (anti-halüsinasyon) mekanizmasıdır**:
- `metrics.json`'daki `best_validation_accuracy` < `MIN_MODEL_ACCURACY` (0.60) → model güvensiz kabul edilir ve yüklenmez.
- 110 Milyon parametreli Transformer modeli 400 soruda aşırı uygunluk (overfitting) yaptığı için; emin olmadığı kararlarda Keyword-fallback, 10 intent kategorisi için devre kalkanı olarak çalışır.
- Tezde bu durum "Küçük (Small Data) Veri Setlerinde Derin Öğrenmenin Sınırları ve Doğal Dil Sınıflandırmasında Katı-Kural (Fallback) Sigortası" başlığı ile raporlanacaktır.
