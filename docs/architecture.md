# EduConnect — Sistem Mimarisi

## Genel Bakış

EduConnect, üç ayrı servise dağıtılmış loosely-coupled bir mimariyle çalışır.

```
┌──────────────────────┐         ┌──────────────────────┐         ┌──────────────────────┐
│   Vercel (Frontend)  │ ─HTTPS─►│   Render (Backend)   │ ─Npgsql►│ Render PostgreSQL    │
│   Vite + React + TS  │         │   ASP.NET Core 10    │         │   (Free Tier, EU)    │
│   Ant Design + Z.    │         │   EF Core + JWT      │         └──────────────────────┘
└──────────────────────┘         │   Docker container   │
                                 └──────────┬───────────┘
                                            │
                                     ┌──────┴──────────┐
                                     │                 │
                                     ▼                 ▼
                          ┌──────────────────┐  ┌──────────────────┐
                          │ HF Spaces (FastAPI)│ │ Google Gemini API│
                          │ BERTurk intent +  │  │ 2.5 Flash Lite   │
                          │ ResNet-50 vision  │  │ (LLM cevap)      │
                          └──────────────────┘  └──────────────────┘
```

## Katmanlar (Clean Architecture)

| Katman | Proje | Sorumluluk |
|--------|-------|------------|
| **Api** | `EduConnect.Api` | Controller'lar, Middleware, SignalR Hub'ları, Program.cs |
| **Application** | `EduConnect.Application` | DTO'lar (Request/Response), servis interface'leri |
| **Domain** | `EduConnect.Domain` | Entity'ler, enum'lar — saf C#, dış bağımlılık yok |
| **Infrastructure** | `EduConnect.Infrastructure` | EF Core DbContext, servis implementasyonları, dış API client'ları |

## Servisler Arası İletişim

- **Backend → NLP Servisi**: HTTPS (Hugging Face Spaces REST API)
- **Backend → Gemini**: HTTPS (Google AI API)
- **Backend → Brevo**: HTTPS Transactional API (SMTP portu Render'da kapalı)
- **Frontend → Backend**: REST + SignalR WebSocket

## EduAI Chatbot Karar Akışı

```
Kullanıcı sorusu
      │
      ▼
[1] NLP API → BERTurk intent + entity extraction + KB araması
      │
      ▼
[2] Confidence kontrolü:
      ├─ < 0.5  → "Anlayamadım" yanıtı
      ├─ 0.5–0.7 → Alt seçenekler sun
      ├─ ≥ 0.7 + KB hit → KB + Gemini ile doğal dil cevabı
      └─ ≥ 0.7 + KB yok → Gemini fallback (isFallback=true)
```

## Neden NLP Ayrı Serviste?

- Ana backend RAM'i 512MB (Render free) — PyTorch/Transformers ~1.5GB ek yük katar
- Python ekosistemi (Hugging Face, torchvision) .NET'ten bağımsız güncellenir
- NLP modeli değişince backend deploy gerekmez
