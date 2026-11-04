# Çalıştırma ve API

Gerçek sistemi kendi makinende tek komutla çalıştırmak için gereken her şey burada. Örneklerdeki bütün çıktılar
Kasım 2026'da yerel kurulumda aldığım gerçek cevaplar; yalnızca token, kimlik ve zaman damgası gibi uzun değerleri
kısalttım.

## Gereksinimler

- Docker ile Docker Compose v2
- Arayüz için Node.js ve npm
- Komut satırı örnekleri için `curl` ve `jq`

Gemini anahtarı, e-posta hesabı ya da başka bir sır gerekmiyor.

## Tek komutla kurulum

```bash
git clone https://github.com/mrfiratatalay/educonnect-backend.git
cd educonnect-backend
docker compose up --build
```

| Ne? | Adres |
|---|---|
| Swagger | http://localhost:5160/swagger |
| Sağlık kontrolü | http://localhost:5160/health |
| Mailpit (doğrulama e-postaları) | http://localhost:8025 |
| PostgreSQL | `localhost:5433`, kullanıcı `educonnect`, şifre `educonnect_dev` |

API açılışta migration'ları uygulayıp başlangıç verisini yüklüyor: 2 üniversite, 4 kategori, 1 yönetici ve 2 indirim.
NLP servisi varsayılan olarak Hugging Face'teki canlı Space. Space uyuyorsa ilk EduAI sorusu biraz bekleyebilir.

**NLP servisini de yerelde çalıştırmak.** PyTorch imajı büyük olduğu için isteğe bağlı:

```bash
NLP_BASE_URL=http://nlp:7860 docker compose --profile ai up --build
```

**Arayüz.** API'nin varsayılan adresi `http://localhost:5160`; ayrı bir `.env` dosyası gerekmiyor.

```bash
git clone https://github.com/mrfiratatalay/educonnect-frontend.git
cd educonnect-frontend
npm install
npm run dev        # http://localhost:5173, yerel API'ye bağlanır
npm run dev:demo   # backend'siz demo modu
```

Durdurmak için backend klasöründe `docker compose down`; veritabanını da silmek için `docker compose down -v`.

## Ortam değişkenleri

| Değişken | Yerel değer | Ne işe yarıyor? |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | `Host=db;Port=5432;Database=educonnect;...` | PostgreSQL bağlantısı |
| `JwtSettings__SecretKey` | `JWT_SECRET` ya da yalnızca yerel için bir varsayılan | JWT imza anahtarı |
| `Cors__AllowedOrigins__0` | `http://localhost:5173` | Arayüzün adresi; çerez için gerekli |
| `NlpService__BaseUrl` | `NLP_BASE_URL` ya da Hugging Face Space | NLP ve görsel servis |
| `GeminiSettings__ApiKey` | `GEMINI_API_KEY` ya da boş | Boşsa EduAI yerel bilgi tabanından cevap veriyor |
| `Email__SmtpHost`, `Email__SmtpPort` | `mail`, `1025` | Mailpit |
| `Email__SenderEmail` | `no-reply@educonnect.local` | Gönderen adresi |
| `Email__BrevoApiKey` | yok | Doluysa e-posta SMTP yerine Brevo API ile gidiyor (yayında böyleydi) |
| `VITE_API_BASE_URL` (arayüz) | yok | API adresi; varsayılan `http://localhost:5160` |
| `VITE_DEMO_MODE` (arayüz) | `.env.demo` içinde `true` | Demo derlemesi |

Hepsi [docker-compose.yml](../docker-compose.yml) içinde. JWT'nin süresi, yayıncısı ve hedef kitlesi
[appsettings.json](../src/EduConnect.Api/appsettings.json) dosyasında.

## Bir öğrencinin ilk dakikaları, curl ile

**1. Üniversiteler.** Kayıt için üniversitenin kimliği gerekiyor.

```bash
curl -s http://localhost:5160/api/universities
```

```json
[{"id":"437413f4-…","name":"Karadeniz Teknik Üniversitesi","city":"Trabzon","domain":"ktu.edu.tr"},
 {"id":"72744801-…","name":"Recep Tayyip Erdoğan Üniversitesi","city":"Rize","domain":"erdogan.edu.tr"}]
```

**2. Kayıt.** E-posta, seçilen üniversitenin alan adında ya da bir alt alan adında olmalı.

```bash
curl -s -X POST http://localhost:5160/api/auth/register -H "Content-Type: application/json" \
  -d '{"fullName":"Örnek Öğrenci","email":"ornek.ogrenci@ogr.erdogan.edu.tr","password":"Ornek1234!",
       "universityId":"<RTEÜ kimliği>","department":"Bilgisayar Mühendisliği","year":2}'
```

```json
{"email":"ornek.ogrenci@ogr.erdogan.edu.tr","verificationExpiresAtUtc":"…","canResendAtUtc":"…",
 "message":"Dogrulama kodu universite e-posta adresinize gonderildi."}
```

**3. Kodu Mailpit'ten al.** En kolayı http://localhost:8025 adresini açmak. Komut satırından:

```bash
ID=$(curl -s http://localhost:8025/api/v1/messages | jq -r '.messages[0].ID')
curl -s http://localhost:8025/api/v1/message/$ID | jq -r '.Text' | grep -oE '[0-9]{6}' | head -1
```

**4. E-postayı doğrula.**

```bash
curl -s -X POST http://localhost:5160/api/auth/verify-email -H "Content-Type: application/json" \
  -d '{"email":"ornek.ogrenci@ogr.erdogan.edu.tr","code":"093562"}'
```

```json
{"message":"Kurumsal e-posta adresiniz dogrulandi. Artik giris yapabilirsiniz."}
```

Yanlış kodda: `{"title":"Invalid Operation","status":400,"detail":"Dogrulama kodu hatali."}`

**5. Giriş.** Access token gövdede, refresh token HttpOnly bir çerezde geliyor. `-c` çerezi bir dosyaya kaydediyor:

```bash
TOKEN=$(curl -s -c cookies.txt -X POST http://localhost:5160/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"ornek.ogrenci@ogr.erdogan.edu.tr","password":"Ornek1234!"}' | jq -r '.accessToken')
grep refreshToken cookies.txt   # #HttpOnly_localhost ... educonnect.refreshToken ...
```

**6. Korumalı bir uç nokta.**

```bash
curl -s http://localhost:5160/api/users/me -H "Authorization: Bearer $TOKEN"
```

```json
{"id":"…","fullName":"Örnek Öğrenci","role":"student","year":2,
 "universityName":"Recep Tayyip Erdoğan Üniversitesi","followersCount":0,…}
```

**7. EduAI'a soru sor.** Önce bir oturum aç, sonra soruyu gönder:

```bash
SID=$(curl -s -X POST http://localhost:5160/api/chat/sessions -H "Authorization: Bearer $TOKEN" | jq -r '.sessionId')
curl -s -X POST http://localhost:5160/api/chat/sessions/$SID/messages -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" -d '{"message":"Bütünleme sınavına kimler girebilir?"}'
```

```json
{
  "content": "Finale girme sartini saglayip basarisiz olanlar, finale girmeyenler ve not yukseltmek isteyen DC notlu ogrenciler butunleme sinavina girebilir. Butunleme notu final notunun yerine gecer.\n\nKaynak: https://oidb.erdogan.edu.tr/tr/news-detail/tek-ders-sinavi/6418",
  "intentDetected": "exam_and_grading",
  "confidence": 0.6645,
  "confidenceBand": "medium",
  "needsReview": false,
  "modelUsed": "berturk-finetuned+local-kb",
  "kbScore": 0.74,
  "kbHit": true,
  "isFallback": false,
  "latencyMs": 1876
}
```

Gemini anahtarı vermediğim için cevap yerel bilgi tabanından geldi; model etiketi bunu `+local-kb` ile söylüyor.
Niyeti ise Hugging Face'teki gerçek BERTurk modeli buldu.

**8. Refresh ve rotasyon.**

```bash
cp cookies.txt eski-cerez.txt
curl -s -b cookies.txt -c cookies.txt -X POST http://localhost:5160/api/auth/refresh   # 200, yeni token ve çerez
curl -s -b eski-cerez.txt -X POST http://localhost:5160/api/auth/refresh
```

```json
{"title":"Unauthorized","status":401,"detail":"Oturum yenilenemedi."}
```

Eski çerezdeki token ilk yenilemede iptal edildiği için ikinci istek reddediliyor
([Güvenlik](06-guvenlik.md)).

## Sınır durumları

| İstek | Cevap |
|---|---|
| Yanlış şifre | 401 `{"title":"Unauthorized","status":401,"detail":"Email veya sifre hatali."}` |
| Token olmadan `GET /api/users/me` | 401 |
| Öğrenci token'ıyla `GET /api/chat/analytics/summary` | 403; bu uç nokta yalnızca yöneticiye açık |
| Boş EduAI mesajı | 400, düz metin: `Mesaj boş olamaz.` |
| Dakikada 30'dan fazla EduAI sorusu | 429 |

Boş mesajdaki düz metin cevap, hata biçimlerinin tek tip olmadığını gösteriyor. Bunu [Sorular ve
sınırlar](12-sorular-ve-sinirlar.md) dokümanında açık bir iş olarak yazdım.

## Yönetici hesabı

Başlangıç verisindeki yönetici: `admin@educonnect.local`, şifre `Admin123!`. E-postası doğrulanmış olarak gelmediği
için önce doğrulanması gerekiyor:

```bash
curl -s -X POST http://localhost:5160/api/auth/resend-verification -H "Content-Type: application/json" \
  -d '{"email":"admin@educonnect.local"}'
# Kodu Mailpit'ten al (3. adım), sonra:
curl -s -X POST http://localhost:5160/api/auth/verify-email -H "Content-Type: application/json" \
  -d '{"email":"admin@educonnect.local","code":"<kod>"}'
```

Yönetici token'ıyla EduAI analitiği:

```bash
curl -s http://localhost:5160/api/chat/analytics/summary -H "Authorization: Bearer $ADMIN_TOKEN"
```

```json
{"totalSessions":2,"totalBotMessages":2,"feedbackCount":0,"helpfulCount":0,"notHelpfulCount":0,
 "helpfulRate":0,"avgConfidence":0.7748,
 "avgLatencyMs":1781,"kbHitCount":2,"kbHitRate":100,"fallbackCount":0,"fallbackRate":0,
 "intentDistribution":[{"intent":"exam_and_grading","count":1,"avgConfidence":0.6645},
                       {"intent":"library","count":1,"avgConfidence":0.885}],
 "topUnresolvedQueries":[]}
```

Bu şifre yalnızca yerel kurulum için. Yayına alınan bir ortamda yönetici şifresi değiştirilmeli.

## API'nin haritası

Swagger 69 yol üzerinde 89 işlem gösteriyor. Modüllere göre dağılımı [Mimari](03-mimari.md) dokümanında. En çok
kullanılanlar:

| Uç nokta | Ne yapıyor? |
|---|---|
| `POST /api/auth/register`, `verify-email`, `resend-verification` | Kayıt ve e-posta doğrulama |
| `POST /api/auth/login`, `refresh`, `logout` | Oturum |
| `GET /api/posts/for-you`, `GET /api/posts/following` | "Sana özel" ve "Takip ettiklerin" akışları |
| `POST /api/posts/{id}/like` | Beğeni; tekrar çağrılınca geri alıyor |
| `GET /api/groups/slug/{slug}` | Topluluk sayfası |
| `POST /api/events/{id}/register` | Etkinliğe kayıt |
| `POST /api/visualsearch/searches` | Fotoğrafla benzer ilan arama |
| `POST /api/chat/sessions/{id}/messages` | EduAI'a soru |

Hub'lar: `/hubs/chat`, `/hubs/direct-messages`, `/hubs/notifications`. Tarayıcı JWT'yi `access_token` sorgu
parametresiyle gönderiyor.
