# EduConnect — Backend & AI Servisleri

> Üniversite öğrencileri için yapay zekâ destekli sosyal medya, akademik etkileşim ve öğrenci pazaryeri platformu.

Bu repo, EduConnect projesinin **.NET 10 tabanlı ana backend API**'sini ve **Python/FastAPI tabanlı doğal dil işleme servisini** içerir. TÜBİTAK 2209-A Üniversite Öğrencileri Araştırma Projeleri kapsamında, Recep Tayyip Erdoğan Üniversitesi Bilgisayar Mühendisliği bitirme tezim olarak geliştirdim.

---

## 🌍 Canlı Sistem

| Bileşen | Adres |
|---|---|
| 🎨 Frontend (Vercel) | https://educonnect-tez.vercel.app |
| ⚙️ Backend API (Render) | https://educonnect-backend-qr03.onrender.com |
| 📖 Swagger / API Docs | https://educonnect-backend-qr03.onrender.com/swagger |
| 🤖 NLP & Vision API (Hugging Face Spaces) | https://firatatalay-educonnect-nlp.hf.space |

> ⚠️ Render free tier kullandığım için 15 dakika işlem yapılmazsa servis "uyku moduna" geçer. İlk istek 30-60 saniye sürebilir; sonraki istekler normal hızda yanıtlanır.

---

## 🎯 Projenin Amacı

Üniversiteye yeni başladığım dönemde, kampüsteki sosyal ortamı yakalamak, hangi etkinliğin nerede olduğunu öğrenmek, ders dışı toplulukları keşfetmek, kullanmadığım kitapları ihtiyacı olan arkadaşlarıma ulaştırmak gibi günlük gereksinimleri tek bir platformdan karşılayan bir yapı bulamadım. WhatsApp grupları çok dağınık, sosyal medyalar üniversite bağlamından kopuk, e-postalar ise eski usul kalıyordu.

EduConnect'i, **üniversite öğrencilerinin tüm dijital ihtiyaçlarını tek bir noktada toplayan**, üstelik **kendi üniversitesi hakkında soruları yapay zekâ ile anında cevaplayabilen** bir sosyal platform olarak tasarladım. Hedef kullanıcı: lisans öğrencisi. Hedef üniversite: ilk etapta Recep Tayyip Erdoğan Üniversitesi (RTEU), sonrasında diğer üniversitelere genişletilebilir yapıda.

---

## 🧩 Modüller

Sistem 11 ana modülden oluşuyor. Her modül kendi başına anlamlı, birbiriyle de doğal olarak entegre:

| # | Modül | Ne Yapıyor |
|---|---|---|
| 1 | **Kimlik Doğrulama (Auth)** | JWT + refresh token tabanlı oturum, kurumsal e-posta zorunluluğu (örn. `@erdogan.edu.tr`), 6 haneli kod ile e-posta doğrulama, şifremi unuttum akışı |
| 2 | **Profil** | Öğrenci profili (bölüm, sınıf, biyografi, avatar, kapak görseli), takipçi/takip edilen sayıları, kendi gönderileri ve etkinlikleri |
| 3 | **Anasayfa / Feed** | Takip edilen kullanıcı + topluluk gönderileri, beğeni, yorum, bookmark, görüntülenme sayısı |
| 4 | **Topluluklar (Communities)** | Grup oluşturma, üyelik, grup gönderileri, grup yönetimi (rol bazlı), grup kuralları, soft delete |
| 5 | **Keşfet (Explore)** | Üniversite içi popüler etiketler, trend gönderiler, öneri sistemleri |
| 6 | **Etkinlikler (Events)** | Kampüs etkinlikleri, katılımcı listesi, hatırlatma |
| 7 | **Mesajlaşma** | Birebir direkt mesajlaşma (SignalR ile gerçek zamanlı), konuşma listesi |
| 8 | **Bildirimler** | Yeni takipçi, beğeni, yorum, etkinlik gibi olaylar için anlık bildirim, target path ile derin link |
| 9 | **Marketplace** | Öğrenci ikinci el ürün ilanı (kitap, hesap makinesi, kıyafet), kategori bazlı filtreleme, **görselden benzer ürün bulma** (Visual Search) |
| 10 | **EduAI Chatbot** | Üniversite hakkında doğal dilde soru sorma. BERTurk ile intent tahmini, knowledge base araması, Gemini ile cevap oluşturma |
| 11 | **Bookmark & Ayarlar** | Kullanıcı tercihleri, gönderi kaydetme, gizlilik ayarları |

---

## 🏗️ Sistem Mimarisi

EduConnect, **üç ayrı serviste deploy edilmiş**, gevşek bağlı (loosely coupled) mikro-servis benzeri bir mimariye sahip:

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
                                     │
                                     │ HTTPS
                                     │
                          ┌──────────┴───────┐
                          │   Brevo SMTP API │
                          │ (mail doğrulama) │
                          └──────────────────┘
```

Bu ayrıştırmayı bilinçli yaptım. Ana backend'in **yapay zekâ modellerini kendi içinde barındırması** hem RAM tüketimi açısından (~1.5GB ek yük) hem de bağımlılık karmaşası açısından sağlıklı değildi. NLP servisini ayrı bir HTTP servisi olarak çıkarınca **backend hafif kaldı**, **Python ekosistemi avantajı korundu** (Hugging Face Transformers, PyTorch, torchvision), **modeli güncellemek için ana backend'i deploy etmem gerekmiyor**, ve **NLP servisi başka projelerden de tüketilebilir** hale geldi.

---

## 🛠️ Teknoloji Yığını

### Backend
- **Dil**: C# (.NET 10)
- **Framework**: ASP.NET Core 10 (Web API + SignalR)
- **ORM**: Entity Framework Core 10 + Npgsql provider
- **Veritabanı**: PostgreSQL 18 (Render free), lokal geliştirmede PostgreSQL 16 (Docker)
- **Kimlik Doğrulama**: JWT Bearer token + refresh token rotation
- **Şifre Hash**: ASP.NET Core Identity PasswordHasher (PBKDF2)
- **Gerçek Zamanlı**: SignalR (mesajlaşma için)
- **API Dokümantasyon**: Swashbuckle (Swagger UI)
- **Rate Limiting**: ASP.NET Core built-in (Gemini için ayrı limit)
- **E-posta**: Brevo HTTPS Transactional API (Render free tier SMTP portlarını bloklar)
- **Container**: Docker (multi-stage build, mcr.microsoft.com/dotnet/aspnet:10.0)

### NLP & Vision Servisi
- **Dil**: Python 3.11
- **Framework**: FastAPI + Uvicorn
- **Intent Sınıflandırma**: [dbmdz/bert-base-turkish-cased](https://huggingface.co/dbmdz/bert-base-turkish-cased) tabanlı, kendi sentetik veri setim üzerinde fine-tune edilmiş BERTurk modeli (423MB safetensors)
- **Görsel Embedding**: ResNet-50 (torchvision pretrained, ImageNet ağırlıklarıyla)
- **Knowledge Base**: JSON tabanlı FAQ + entity lookup (RTEU'ya özgü içerik)
- **Container**: Docker (Python 3.11 slim, CPU-only torch — image boyutunu yarıladı)

### LLM
- **Model**: Google Gemini 2.5 Flash Lite (uygun maliyetli, düşük gecikme)
- **Kullanım**: Sadece NLP servisi yüksek confidence ile intent + knowledge base sonucu bulduğunda Gemini'ye "bu bilgiyle doğal cevap üret" diye prompt gönderiyorum. Bu sayede halüsinasyon riskini minimuma indiriyorum

### Mimari Desen
- **Clean Architecture**: 4 katman (Domain, Application, Infrastructure, Api)
- **Dependency Injection**: Built-in DI container
- **Repository Pattern yerine doğrudan EF Core DbContext** kullanıyorum — bu kadar küçük bir projede gereksiz soyutlama olacaktı
- **Result/Either pattern** yerine **klasik exception-based hata yönetimi** + custom middleware (GlobalExceptionHandler) — debug ve okunabilirlik açısından ekibimin (yani benim) hızlı ilerlemesini sağladı

---

## 🤖 Yapay Zekâ Mimarisi (EduAI Chatbot)

Chatbot'un işleyişi, basit bir Gemini wrapper'dan çok daha sofistike. Şöyle çalışıyor:

```
Kullanıcı sorusu
       │
       ▼
[1] HF Spaces NLP API çağrısı
       │  → BERTurk intent sınıflandırma
       │  → Entity extraction (üniversite, bölüm, etkinlik gibi)
       │  → Knowledge base araması (BM25 benzeri skor)
       │
       ▼
[2] Backend "ne yapayım?" kararı:
       │
       ├─ Confidence DÜŞÜK (<0.5) → Kullanıcıya "tam anlayamadım, başka şekilde sorar mısın?" yanıtı
       │
       ├─ Confidence ORTA (0.5-0.7) → "Şunu mu kastediyorsun?" + en alakalı 2-3 cevap önerisi
       │
       ├─ Confidence YÜKSEK (≥0.7) + KB hit varsa:
       │   → KB cevabını + kaynak URL'i + chat geçmişini Gemini'ye gönder
       │   → "Bu bilgiyi kullanıcının diliyle, samimi ve kısa şekilde anlat" prompt'u
       │   → Gemini cevabını döndür
       │
       └─ Confidence YÜKSEK fakat KB hit yok:
           → Gemini'ye doğrudan sor (genel bilgi olabilir)
           → Cevabı "kaynak doğrulanamadı" işaretiyle döndür (isFallback=true)
```

Bu kademeli karar mekanizması sayesinde **kullanıcı her seferinde Gemini API kotamı tüketmiyor**, **yanlış bilgi riski azalıyor**, ve **kullanıcıya cevabın hangi kaynaktan geldiği şeffaf şekilde gösteriliyor** (confidence band: low/medium/high; needsReview bayrağı; kaynak URL'i).

Her chat mesajını veritabanında saklıyorum (ChatMessages tablosu) — model performansını ileride iyileştirmek için. Ayrıca kullanıcı bir cevaba 👍/👎 feedback verebiliyor (ChatMessageFeedbacks tablosu).

---

## 🗃️ Veritabanı Tasarımı

Sistem **27 tablodan** oluşan normalize edilmiş bir PostgreSQL şeması kullanıyor. Tablolar tematik olarak gruplanmış:

### Kullanıcı & Kimlik (4 tablo)
- `Users`, `StudentProfiles`, `RefreshTokens`, `Universities`

### Sosyal Medya (8 tablo)
- `Posts`, `PostComments`, `PostLikes`, `PostBookmarks`, `PostViews`, `UserFollows`, `Categories`, `Discounts`

### Topluluklar & Etkinlikler (4 tablo)
- `Groups`, `GroupMembers`, `Events`, `EventParticipants`

### Mesajlaşma (3 tablo)
- `DirectConversations`, `DirectMessages`, `Notifications`

### Marketplace (3 tablo)
- `Products`, `ProductImages`, `VisualSearchHistories`, `VisualSearchResults`

### EduAI Chatbot Analitiği (3 tablo)
- `ChatSessions`, `ChatMessages`, `ChatMessageFeedbacks`

### Diğer
- `Feedbacks` (genel uygulama içi geri bildirim)

ER diyagramı ve detaylı tablo açıklamaları için ana monorepo'daki `Database/` klasörüne bakabilirsiniz.

### Önemli Tasarım Kararı: PostgreSQL'e Geçiş

Projeye Microsoft SQL Server ile başlamıştım çünkü .NET ekosisteminde bu en yaygın seçim. Ancak production deploy aşamasında **Render'ın free tier'ında SQL Server bulunmaması**, ve **PostgreSQL'in açık kaynak olması + ücretsiz tier'larda her yerde olması** sebebiyle tüm sistemi PostgreSQL'e taşıdım. Geçiş süreci:

1. EF Core provider'ı `Microsoft.EntityFrameworkCore.SqlServer` → `Npgsql.EntityFrameworkCore.PostgreSQL` olarak değiştirdim
2. 11 adet eski migration dosyasını tamamen sildim, yeni `InitialPostgresCreate` migration'ı oluşturdum
3. `nvarchar(max)` → `text`, `float` → `double precision` gibi MS SQL'e özgü tip eşlemelerini düzelttim
4. `Npgsql.EnableLegacyTimestampBehavior` switch'i ile DateTime uyumluluğunu sağladım (Npgsql 6+ varsayılan davranışı çok katı)
5. Mevcut verileri taşımak için `backend/tools/MigrateData/` altında **kendi yazdığım ETL konsol uygulamasını** kullandım — EF Core ile MS SQL'den okuyor, FK trigger'ları geçici devre dışı bırakıp PostgreSQL'e yazıyor. Test ortamında 27 tablo, 815 satır sıfır hata ile taşındı.

---

## 📂 Proje Yapısı

```
backend/
├── src/
│   ├── EduConnect.Api/                # Web API katmanı (Controllers, Program.cs, Dockerfile)
│   │   ├── Controllers/               # 15 controller, modül bazlı
│   │   ├── Middleware/                # GlobalExceptionHandler, CurrentUser middleware
│   │   ├── Hubs/                      # SignalR (mesajlaşma)
│   │   ├── appsettings.json           # Konfigürasyon (secret'lar env'den geliyor)
│   │   └── Dockerfile                 # Multi-stage build (sdk:10.0 → aspnet:10.0)
│   ├── EduConnect.Application/        # İş kuralları, DTO'lar, interface'ler
│   │   ├── Contracts/                 # Request/Response DTO'ları
│   │   └── Interfaces/                # IService sözleşmeleri
│   ├── EduConnect.Domain/             # Entity'ler, enum'lar (saf C#)
│   │   └── Entities/                  # 27 EF Core entity
│   └── EduConnect.Infrastructure/     # EF Core, Identity, dış servis client'ları
│       ├── Data/                      # AppDbContext, DbSeeder, Migrations
│       ├── Services/                  # AuthService, ChatbotService, NlpApiService...
│       └── Options/                   # Strongly-typed config classes
├── tools/
│   └── MigrateData/                   # MS SQL → PostgreSQL tek seferlik ETL
├── nlp-service/                       # Python FastAPI servisi (Hugging Face Spaces'e deploy)
│   ├── main.py                        # FastAPI uygulama
│   ├── routers/                       # nlp.py, vision.py
│   ├── services/                      # intent_service, vision_service, knowledge_service
│   ├── models/intent_classifier/      # BERTurk fine-tuned (Git LFS)
│   ├── data/                          # knowledge_base.json, entity_dictionary.json
│   ├── training/                      # Model eğitim ve veri augmentation scriptleri
│   ├── requirements.txt
│   └── Dockerfile                     # python:3.11-slim + CPU-only torch
├── scripts/                           # Lokal başlatma .bat dosyaları
│   ├── dev/
│   └── prod/
└── database/                          # Şema dokümantasyonu, eski MS SQL .bak
```

---

## 🚀 Lokal Kurulum

### Önkoşullar
- Docker Desktop (Windows/Mac/Linux)
- .NET 10 SDK (sadece kod editle çalışmak istiyorsanız)
- Python 3.11+ (sadece NLP servisini lokalde çalıştıracaksanız)

### Tek Komutla Çalıştırma (Önerilen)

Proje monorepo'sunun kökünde:

```bash
# Windows
scripts\dev\start-docker.bat
```

Bu script:
1. PostgreSQL 16 container'ını ayağa kaldırır (port `5433` — başka projeyle çakışmasın diye)
2. .NET backend container'ını build edip ayağa kaldırır (port `5160` → container 8080)
3. EF Core migration'larını otomatik uygular
4. DbSeeder ile başlangıç verisini yükler (2 üniversite, 4 kategori, 1 admin)

Sonra tarayıcıda:
- **Swagger**: http://localhost:5160/swagger
- **Health**: http://localhost:5160/health

### NLP Servisini Lokalde Çalıştırma (Opsiyonel)

Varsayılan olarak lokal Docker stack, **production HF Spaces NLP'sini kullanır** (kolay olsun diye). Eğer NLP'yi lokalde geliştirmek istiyorsanız:

```bash
scripts\dev\start-nlp-dev.bat
```

Bu script Python venv oluşturur, dependency'leri kurar, Git LFS ile modeli indirir ve `http://localhost:8000`'de servisi başlatır.

Sonrasında `docker-compose.yml`'de `NlpService__BaseUrl` env'ini `http://host.docker.internal:8000` yaparsanız backend lokal NLP'yi kullanır.

### Geleneksel `dotnet run` ile (Docker'sız)

Eğer Docker kullanmak istemiyorsanız, sadece PostgreSQL'i container'da bırakıp backend'i lokalde çalıştırabilirsiniz:

```bash
docker compose up -d postgres
cd src/EduConnect.Api
dotnet ef database update
dotnet run
```

---

## 🌐 Production Deployment

Sistem üç farklı sağlayıcıya dağıtılmış:

| Servis | Sağlayıcı | Plan | Sebep |
|---|---|---|---|
| Frontend | Vercel | Hobby (Free) | React/Vite için optimize, CDN, otomatik preview deployment |
| Backend API | Render | Free Web Service | Docker desteği var, GitHub entegrasyonu auto-deploy yapıyor |
| PostgreSQL | Render | Free PostgreSQL | Aynı bölgede backend ile düşük gecikme; 90 gün sonra plan yükseltmem gerekecek |
| NLP & Vision | Hugging Face Spaces | CPU Basic (Free) | 16GB RAM ücretsiz — backend'in 512MB RAM'ine sığmayan ağır AI bağımlılıklarını burada barındırıyorum |
| E-posta | Brevo | Free (300/gün) | Render free tier SMTP portlarını blokladığı için HTTPS API'li servis seçmek zorunda kaldım |

### Çevre Değişkenleri (Render Backend için)

| Key | Açıklama |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_URLS` | `http://+:8080` |
| `ConnectionStrings__DefaultConnection` | Render Internal PostgreSQL URL'inin Npgsql formatı |
| `JwtSettings__SecretKey` | 256-bit JWT imzalama anahtarı |
| `NlpService__BaseUrl` | `https://firatatalay-educonnect-nlp.hf.space` |
| `GeminiSettings__ApiKey` | Google AI Studio API key |
| `Email__BrevoApiKey` | Brevo Transactional API key |
| `Email__SenderEmail`, `Email__SenderName` | Verified sender bilgileri |
| `Cors__AllowedOrigins__0` | Vercel frontend URL'i |

### Otomatik Deploy

GitHub'a `main` branch'e her push, Render'da otomatik build + deploy tetikliyor. Build süresi ~2-5 dakika (Docker cache kullandığım için sonraki build'ler hızlı).

---

## 🔌 API Endpoint Özeti

15 controller, 80+ endpoint mevcut. Tam liste için Swagger UI'a bakabilirsiniz: https://educonnect-backend-qr03.onrender.com/swagger

Öne çıkan endpoint'ler:

| Endpoint | Açıklama |
|---|---|
| `POST /api/auth/register` | Yeni kullanıcı kaydı, doğrulama kodu mail olarak gönderilir |
| `POST /api/auth/verify-email` | 6 haneli kod ile e-posta onayı |
| `POST /api/auth/login` | JWT + refresh token döndürür |
| `GET /api/posts/feed` | Takip edilenlerin gönderileri |
| `POST /api/chat/sessions` | EduAI chatbot oturumu başlatır |
| `POST /api/chat/sessions/{id}/messages` | Mesaj gönder, AI cevabı dönsün |
| `POST /api/products` | Pazaryeri ürün ilanı oluştur |
| `POST /api/visualsearch/search` | Görsel yükle, benzer ürünleri bul |

---

## 🧪 Test Edebileceğiniz Hesap

Hızlı test için seed edilen admin hesabı:

```
Email:    admin@educonnect.local
Şifre:    Admin123!
```

(Production'da bu hesabın e-posta doğrulamasını manuel olarak yaptım. Gerçek kullanıcılar register sonrası mail ile gelen 6 haneli kodu girerek hesabı aktifleştirir.)

---

## ⚠️ Bilinen Kısıtlar

- **Render free cold start**: 15 dakika inaktiviteden sonra ilk istek 30-60sn sürer. Bu free tier'ın doğal kısıtı, paid tier'da yok.
- **Render PostgreSQL 90 gün expire**: Free tier veritabanı 90 gün sonra silinir. Tez teslim süreci dahilinde sorun değil, sonrasında $7/ay paid plana yükseltirim ya da yedek alıp yeni instance açarım.
- **Upload'lar ephemeral**: Render container restart'ta dosya sistemindeki upload'lar (avatar, post resmi) kaybolur. Kullanıcı kitlesi olmadığı için kabul ettim; gerekirse Cloudinary entegrasyonu eklenebilir.
- **HF Spaces sleep**: Uzun süre kullanılmayan Space uyur, ilk request modeli RAM'e yüklerken 10-30sn sürer.
- **DataProtection keys volatile**: Container restart'ta dahili kriptografi anahtarları yenilenir. JWT etkilenmez (env'den okunuyor) ama bazı middleware feature'ları (CSRF token) etkilenebilir.

---

## 🛤️ Geliştirme Yol Haritası

- [ ] Persistent disk eklenmesi (avatar/post upload'larının kalıcı olması için)
- [ ] HF Spaces yerine kendi sunucumda model serving (Ollama veya vLLM ile) — ileride bütçe izin verirse
- [ ] Push notification (FCM ya da Web Push API)
- [ ] Kapsamlı entegrasyon test suite'i (şu an manuel smoke test ile ilerliyorum)
- [ ] Türkçe NLP modelini RTEU-spesifik daha çok sentetik veri ile fine-tune etme
- [ ] Kullanıcı bazlı öneri algoritması (şu an kronolojik feed kullanıyorum)
- [ ] Markdown editör desteği gönderiler için
- [ ] Mobil uygulama (React Native veya Flutter)

---

## 📚 Kullanılan Açık Kaynak Kütüphaneler

Bu proje çok sayıda harika açık kaynak projeye dayanıyor. En önemlilerine teşekkür borçluyum:

- ASP.NET Core, Entity Framework Core (Microsoft)
- Npgsql (PostgreSQL .NET sürücüsü)
- Hugging Face Transformers, PyTorch, torchvision
- BERTurk modeli — [dbmdz](https://huggingface.co/dbmdz)
- FastAPI, Uvicorn, Pydantic
- React, Vite, TypeScript
- Ant Design, TanStack Query, Zustand
- SignalR

---

## 👤 Geliştirici

**Fırat Atalay**
Bilgisayar Mühendisliği Lisans Öğrencisi
Recep Tayyip Erdoğan Üniversitesi
📧 firat_atalay21@erdogan.edu.tr

TÜBİTAK 2209-A Üniversite Öğrencileri Araştırma Projeleri Programı bitirme tezi olarak geliştirildi.

---

## 📄 Lisans

MIT License. Akademik ve eğitim amaçlı her kullanım serbesttir; ticari kullanım öncesi lütfen iletişime geçin.
