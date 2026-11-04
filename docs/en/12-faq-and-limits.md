# FAQ and limits

Here I collected the questions an interview might bring up about EduConnect, with my short answers. After them come
the project's known limits and the roadmap. The details behind each answer are in the linked documents.

## Likely questions

**Can you describe the project in one sentence?**
A campus platform for university students: a feed, communities, events, a second-hand market, messaging and an
assistant (EduAI) that answers university questions based on official sources.

**What did you do on the team?**
Ayşe Mandıralı and I built it as a two-person team, and I was the team captain. We shared the work: the backend, the
AI service, the frontend and deployment were our joint work. The November 2026 revision (the demo mode, three bug
fixes, these documents) is mine.

**Did you use an AI assistant?**
Yes. I wrote a large part of the code together with AI (Claude Code), under rules I set myself. We decided the
architecture, the rules and whether each change was correct. I wrote every number and claim in these documents by
checking the code. In [Problems I solved](11-problems-i-solved.md) I also describe how a bulk text fix broke an API
contract.

**Why microservices?**
It is not a full microservice architecture: there is one API and a separate AI service. We split them because of
memory: the models did not fit into Render's 512 MB free server ([Architecture](03-architecture.md)).

**You say Clean Architecture, but the business rules are in the controllers. Why?**
A fair criticism. There are four layers, but most rules live in the controllers, and the controllers use
`AppDbContext` directly. MediatR is registered, but no handler was ever written. As a two-person team we chose speed.
Today I would move rules such as feed scoring and community roles into testable application services.

**What happens when two likes arrive at the same time?**
The `(PostId, UserId)` pair is unique. The second request gets `23505` from PostgreSQL, and the backend reads it as
"already done" ([Database](04-database.md)).

**How do you protect the refresh token?**
It lives in an HttpOnly cookie and is single-use. On every refresh the old token is revoked with a single
`UPDATE ... WHERE`; a parallel second request gets a 401. I also name the gaps: tokens are plain text in the database,
and reuse does not close the session chain ([Security](06-security.md)).

**What if EduAI gives wrong information?**
An answer only counts as "grounded" when the knowledge base match is strong, and every answer shows the intent, the
confidence band and a source link. There is a cost: with a weak match, the model helps with its general knowledge, and
that raises the risk of wrong information ([AI](07-ai.md)).

**Is 96% accuracy realistic?**
It needs careful reading. The validation set was split from the same distribution as the training data, most of which
are sentences generated from rules. Accuracy on real questions may be lower. There is a separate set of 60 hard cases,
but we did not commit its results, so I do not quote a number.

**What if Gemini or the NLP service goes down?**
If the NLP service is down, the flow continues with a safe fallback result; if Gemini is down, the answer comes from
the local knowledge base. The system also runs with no Gemini key at all ([Running and API](10-running-and-api.md)).

**Is the live demo the real system?**
No, it is a demo that runs in the browser. It meets the real API's contract at the transport layer, and the screen
code is the same in both modes. The real system runs locally with one command
([Deployment strategy](09-deployment-strategy.md)).

**Why are there no tests?**
During the thesis we prioritized features. I saw the cost in the three bugs I found in November; a simple contract or
smoke test would have caught all three on day one. The first tests I would write are in the roadmap below.

**If the number of users grows, what breaks first?**
In order: feed queries, because there is no date index; the "for you" scoring, which ranks 250 candidates in memory
on every request; visual search, which computes cosine similarity against every candidate; and the race in event
capacity. Running the API on more than one instance would need a SignalR backplane and shared storage for uploads.

**What is the weakest point in security?**
There is no rate limit or account lockout on login, so passwords are open to guessing. Right behind it come
plain-text refresh tokens and `InvalidOperationException` messages leaking to the client.

**What would you do differently if you started over?**
I would start with tests, put the business rules into services and make the error contract uniform from day one. I
would plan the UI and the API under the same domain, and keep tags and embeddings in dedicated structures.

## Known limits

| Area | Limit | How it closes |
|---|---|---|
| Tests | No automated tests | The roadmap below |
| Code | MediatR and FluentValidation are registered but unused; `PostsController` has 1,126 lines, `GroupsController` 757 | Move the rules into application services, or remove the packages |
| Data | Event capacity can be exceeded by simultaneous registrations | A conditional `INSERT` or a row lock |
| Data | No index on `Posts.CreatedAtUtc` | Add the index |
| Data | Embeddings are JSON in a text column | `pgvector` |
| Security | No rate limit or account lockout on login | An `auth` rate limit policy |
| Security | Refresh tokens are plain text; no reuse detection | Store a hash, close the chain |
| Security | `InvalidOperationException` messages reach the client | A domain-specific exception and a generic message |
| Security | The registration response reveals that an e-mail is registered | The same answer in every case |
| Security | The verification code hash is unsalted SHA-256 | HMAC with a secret key |
| Security | No password reset flow | A single-use reset token |
| Security | CORS default in the NLP service is `*` | Make `ALLOWED_ORIGINS` required |
| Security | The refresh cookie is `SameSite=Lax`; the UI and the API are on different sites | The same domain, or `SameSite=None` with CSRF protection |
| API | Error formats are not uniform: Problem Details, `{ "message": ... }` and plain text | Problem Details everywhere |
| API | Some response texts lack Turkish characters ("gonderini begendi") | Fix the texts |
| AI | The keyword fallback in Python does not simplify Turkish characters | Move the demo's simplification into the service |
| AI | The hard test set's results are not in the repository | Run the evaluation and report it |
| AI | The feedback endpoint exists but the UI does not call it; no analytics screen | Helpful / not helpful buttons and an admin screen |
| Frontend | A single bundle: 2,157 KB (639 KB gzipped); `recharts` installed but unused | Route-level splitting, remove the package |
| Frontend | No accessibility audit; 11 `aria-label`s | An audit with Lighthouse and axe |
| Demo | The compiler does not check the shape of demo responses | Tie the mappers to the app's types |
| Infrastructure | Uploaded files are not persistent on Render | Object storage |
| Infrastructure | The free database expires; the Hugging Face Space sleeps | A paid tier; today the live address runs the demo |

## Roadmap

Ordered by how much risk the next step removes:

1. **Tests.** Refresh rotation and verification codes in `AuthService`; unit tests for feed scoring and community
   roles; a few integration tests against a real PostgreSQL; a contract test that compares the UI's role and status
   values with the API contract.
2. **Security.** A login rate limit, hashed refresh tokens with reuse detection, an HMAC verification code, generic
   error messages, password reset.
3. **A uniform error contract.** Problem Details on every endpoint.
4. **Application services.** Move feed scoring and community roles out of the controllers.
5. **Data.** A `CreatedAtUtc` index, a tag table, a conditional `INSERT` for capacity, `pgvector`.
6. **Frontend.** Route-level splitting, EduAI feedback buttons, an admin analytics screen, an accessibility audit.
7. **Hosting.** A shared domain for the UI and the API, object storage and, with a budget, the real system online.
