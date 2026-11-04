# Running and API

Everything you need to run the real system on your machine with one command is here. All outputs in the examples are
real responses I got from the local setup in November 2026; I only shortened long values such as tokens, ids and
timestamps.

## Requirements

- Docker with Docker Compose v2
- Node.js and npm for the frontend
- `curl` and `jq` for the command-line examples

No Gemini key, e-mail account or any other secret is needed.

## One-command setup

```bash
git clone https://github.com/mrfiratatalay/educonnect-backend.git
cd educonnect-backend
docker compose up --build
```

| What? | Address |
|---|---|
| Swagger | http://localhost:5160/swagger |
| Health check | http://localhost:5160/health |
| Mailpit (verification e-mails) | http://localhost:8025 |
| PostgreSQL | `localhost:5433`, user `educonnect`, password `educonnect_dev` |

On startup the API applies migrations and loads the seed data: 2 universities, 4 categories, 1 admin and 2 discounts.
By default the NLP service is the live Space on Hugging Face. If the Space is asleep, the first EduAI question may take
a moment.

**Running the NLP service locally too.** Optional, because the PyTorch image is large:

```bash
NLP_BASE_URL=http://nlp:7860 docker compose --profile ai up --build
```

**Frontend.** The API's default address is `http://localhost:5160`, so no `.env` file is needed.

```bash
git clone https://github.com/mrfiratatalay/educonnect-frontend.git
cd educonnect-frontend
npm install
npm run dev        # http://localhost:5173, talks to the local API
npm run dev:demo   # the demo mode without a backend
```

To stop, run `docker compose down` in the backend folder; `docker compose down -v` also deletes the database.

## Environment variables

| Variable | Local value | What is it for? |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | `Host=db;Port=5432;Database=educonnect;...` | The PostgreSQL connection |
| `JwtSettings__SecretKey` | `JWT_SECRET` or a local-only default | The JWT signing key |
| `Cors__AllowedOrigins__0` | `http://localhost:5173` | The UI's address; needed for the cookie |
| `NlpService__BaseUrl` | `NLP_BASE_URL` or the Hugging Face Space | The NLP and vision service |
| `GeminiSettings__ApiKey` | `GEMINI_API_KEY` or empty | When empty, EduAI answers from the local knowledge base |
| `Email__SmtpHost`, `Email__SmtpPort` | `mail`, `1025` | Mailpit |
| `Email__SenderEmail` | `no-reply@educonnect.local` | The sender address |
| `Email__BrevoApiKey` | not set | When set, e-mail goes through the Brevo API instead of SMTP (as in production) |
| `VITE_API_BASE_URL` (frontend) | not set | The API address; defaults to `http://localhost:5160` |
| `VITE_DEMO_MODE` (frontend) | `true` in `.env.demo` | The demo build |

All of them are in [docker-compose.yml](../../docker-compose.yml). The JWT lifetime, issuer and audience are in
[appsettings.json](../../src/EduConnect.Api/appsettings.json).

## A student's first minutes, with curl

**1. Universities.** Registration needs the university's id.

```bash
curl -s http://localhost:5160/api/universities
```

```json
[{"id":"437413f4-…","name":"Karadeniz Teknik Üniversitesi","city":"Trabzon","domain":"ktu.edu.tr"},
 {"id":"72744801-…","name":"Recep Tayyip Erdoğan Üniversitesi","city":"Rize","domain":"erdogan.edu.tr"}]
```

**2. Register.** The e-mail must be on the chosen university's domain or one of its subdomains.

```bash
curl -s -X POST http://localhost:5160/api/auth/register -H "Content-Type: application/json" \
  -d '{"fullName":"Örnek Öğrenci","email":"ornek.ogrenci@ogr.erdogan.edu.tr","password":"Ornek1234!",
       "universityId":"<RTEÜ id>","department":"Bilgisayar Mühendisliği","year":2}'
```

```json
{"email":"ornek.ogrenci@ogr.erdogan.edu.tr","verificationExpiresAtUtc":"…","canResendAtUtc":"…",
 "message":"Dogrulama kodu universite e-posta adresinize gonderildi."}
```

**3. Get the code from Mailpit.** The easiest way is to open http://localhost:8025. From the command line:

```bash
ID=$(curl -s http://localhost:8025/api/v1/messages | jq -r '.messages[0].ID')
curl -s http://localhost:8025/api/v1/message/$ID | jq -r '.Text' | grep -oE '[0-9]{6}' | head -1
```

**4. Verify the e-mail.**

```bash
curl -s -X POST http://localhost:5160/api/auth/verify-email -H "Content-Type: application/json" \
  -d '{"email":"ornek.ogrenci@ogr.erdogan.edu.tr","code":"093562"}'
```

```json
{"message":"Kurumsal e-posta adresiniz dogrulandi. Artik giris yapabilirsiniz."}
```

With a wrong code: `{"title":"Invalid Operation","status":400,"detail":"Dogrulama kodu hatali."}`

**5. Log in.** The access token comes in the body and the refresh token in an HttpOnly cookie. `-c` saves the cookie
to a file:

```bash
TOKEN=$(curl -s -c cookies.txt -X POST http://localhost:5160/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"ornek.ogrenci@ogr.erdogan.edu.tr","password":"Ornek1234!"}' | jq -r '.accessToken')
grep refreshToken cookies.txt   # #HttpOnly_localhost ... educonnect.refreshToken ...
```

**6. A protected endpoint.**

```bash
curl -s http://localhost:5160/api/users/me -H "Authorization: Bearer $TOKEN"
```

```json
{"id":"…","fullName":"Örnek Öğrenci","role":"student","year":2,
 "universityName":"Recep Tayyip Erdoğan Üniversitesi","followersCount":0,…}
```

**7. Ask EduAI.** Open a session first, then send the question ("who can take the make-up exam?"):

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

I did not provide a Gemini key, so the answer came from the local knowledge base; the model label says so with
`+local-kb`. The intent, however, came from the real BERTurk model on Hugging Face.

**8. Refresh and rotation.**

```bash
cp cookies.txt old-cookie.txt
curl -s -b cookies.txt -c cookies.txt -X POST http://localhost:5160/api/auth/refresh   # 200, new token and cookie
curl -s -b old-cookie.txt -X POST http://localhost:5160/api/auth/refresh
```

```json
{"title":"Unauthorized","status":401,"detail":"Oturum yenilenemedi."}
```

The token in the old cookie was revoked by the first refresh, so the second request is rejected
([Security](06-security.md)).

## Edge cases

| Request | Response |
|---|---|
| Wrong password | 401 `{"title":"Unauthorized","status":401,"detail":"Email veya sifre hatali."}` |
| `GET /api/users/me` without a token | 401 |
| `GET /api/chat/analytics/summary` with a student token | 403; this endpoint is admin-only |
| An empty EduAI message | 400, plain text: `Mesaj boş olamaz.` |
| More than 30 EduAI questions per minute | 429 |

The plain-text response to an empty message shows that error formats are not uniform. I listed it as an open item in
[FAQ and limits](12-faq-and-limits.md).

## The admin account

The seeded admin is `admin@educonnect.local` with the password `Admin123!`. Its e-mail is not seeded as verified, so
verify it first:

```bash
curl -s -X POST http://localhost:5160/api/auth/resend-verification -H "Content-Type: application/json" \
  -d '{"email":"admin@educonnect.local"}'
# Get the code from Mailpit (step 3), then:
curl -s -X POST http://localhost:5160/api/auth/verify-email -H "Content-Type: application/json" \
  -d '{"email":"admin@educonnect.local","code":"<code>"}'
```

EduAI analytics with an admin token:

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

This password is for the local setup only. In any deployed environment the admin password must be changed.

## A map of the API

Swagger shows 89 operations on 69 paths. Their split by module is in [Architecture](03-architecture.md). The most used
ones:

| Endpoint | What does it do? |
|---|---|
| `POST /api/auth/register`, `verify-email`, `resend-verification` | Registration and e-mail verification |
| `POST /api/auth/login`, `refresh`, `logout` | Sessions |
| `GET /api/posts/for-you`, `GET /api/posts/following` | The "for you" and "following" feeds |
| `POST /api/posts/{id}/like` | Like; calling it again removes the like |
| `GET /api/groups/slug/{slug}` | A community page |
| `POST /api/events/{id}/register` | Registering for an event |
| `POST /api/visualsearch/searches` | Searching for similar listings with a photo |
| `POST /api/chat/sessions/{id}/messages` | Asking EduAI |

Hubs: `/hubs/chat`, `/hubs/direct-messages`, `/hubs/notifications`. The browser sends the JWT in the `access_token`
query parameter.
