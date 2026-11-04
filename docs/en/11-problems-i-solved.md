# Problems I solved

This document prepares for the interview question "tell me about a hard problem you solved". Every story comes from
the git history or from my reread in November 2026; next to each one I wrote its commit and where the code lives. I
write "we" for what we solved as a team and "I" for what I found myself in November.

| # | Problem | Topic |
|---|---|---|
| 1 | No SQL Server on Render | Database migration |
| 2 | The AI did not fit in 512 MB | Architecture |
| 3 | Verification e-mails were not delivered | Infrastructure |
| 4 | EduAI went silent when the Gemini quota ran out | Resilience |
| 5 | An assistant that said "follow the announcements" | AI quality |
| 6 | "Could you explain a bit more?" | Conversation context |
| 7 | Simultaneous likes returned 500 | Concurrency |
| 8 | Refresh token and registration races | Concurrency |
| 9 | Posts repeating across pages, a tag search that filled memory | Queries |
| 10 | The user's file name was written to the database | Security |
| 11 | The live site opened empty | Deployment |
| 12 | Three bugs I found while rereading | Contracts |

## 1. No SQL Server on Render

**Problem.** We started with SQL Server. When deploying, we found that Render's free tier has no SQL Server, and we had
11 migrations plus data sitting in SQL Server.

**Solution.** We switched to Npgsql, replaced the 11 migrations with a single `InitialPostgresCreate`, and wrote a
small console tool to move the data ([tools/MigrateData](../../tools/MigrateData/Program.cs)). The tool applies the
migration on the target, keeps the connection open, turns off foreign key triggers with
`SET session_replication_role = replica`, copies the tables in dependency order and finally sets it back to `origin`.

**Why.** The setting belongs to the session. If EF Core took a different connection from the pool for each save, the
setting would be reset, so keeping the connection open was essential. Commit: `c6c9305` (26 May 2026).

Dates needed a decision too. Npgsql 6 and later is strict about the kind (`Kind`) of `DateTime` values. To keep the
migration small, we turned on the `Npgsql.EnableLegacyTimestampBehavior` switch, which selects the old behavior, in the
API, the migration factory and the copy tool ([Program.cs](../../src/EduConnect.Api/Program.cs)).

**Cost.** Foreign keys are not checked during the copy, so data consistency relies on trusting the source. The setting
also needs elevated privileges. The timestamp switch is a migration shortcut; the proper fix is to keep every date in
UTC and remove the switch.

## 2. The AI did not fit in 512 MB

**Problem.** BERTurk and ResNet-50, together with PyTorch, need gigabytes of memory. Render's free server has 512 MB.

**Solution.** We moved the models into a separate service written with FastAPI and ran it on Hugging Face Spaces. The
Docker image uses the CPU-only build of PyTorch ([nlp-service/Dockerfile](../../nlp-service/Dockerfile)).

**Why, and the cost.** The API stayed light, and the model can be updated independently of the API. The cost is one
more network call per question and a Space that goes to sleep. If the service is unreachable, the backend continues
with a safe fallback result ([NlpApiService](../../src/EduConnect.Infrastructure/Services/NlpApiService.cs)). Details
at the end of [Architecture](03-architecture.md).

## 3. Verification e-mails were not delivered

**Problem.** Registration depends on a 6-digit code sent to the institutional e-mail address. After moving to Render,
the codes stopped arriving: Render's free tier blocks the SMTP ports (25, 465, 587).

**Solution.** We added Brevo's HTTPS API. The service checks for a Brevo key first and falls back to SMTP without one
([EmailService](../../src/EduConnect.Infrastructure/Services/EmailService.cs)). Commit: `ec00784`.

**Why.** HTTPS is open everywhere, and the SMTP path did not disappear: in the local setup e-mails go to Mailpit over
SMTP.

**Cost.** One more external service to depend on, and the free plan has a daily sending limit.

## 4. EduAI went silent when the Gemini quota ran out

**Problem.** When Gemini's free quota ran out, the generation call failed and the user got no answer.

**Solution.** We wrapped the Gemini call in a `try/catch`. On failure the answer is built locally
(`BuildLocalResponse`): the entry's answer and source if the knowledge base match is reliable, otherwise a close answer
that starts with "Tam emin değilim ama..." ("I'm not entirely sure, but..."), and a short apology if there is neither
([ChatbotService](../../src/EduConnect.Infrastructure/Services/ChatbotService.cs)). Commit: `a68690b` (30 April 2026).

**Why.** The user should never see an empty screen. Where the answer came from is not hidden either: the model label
becomes `+local-kb`.

## 5. An assistant that said "follow the announcements"

**Problem.** Some knowledge base entries said "follow the announcements of the relevant office" instead of giving
concrete information. The assistant repeated them word for word, and some topics had no entries at all.

**Solution.** We took two steps:

1. **A deflection detector.** An entry shorter than 300 characters that contains such phrases counts as a deflection.
   Gemini is asked to give a concrete answer instead of repeating it and to add the link only as a source
   (`IsDeflectingKbAnswer`, commit `cb5b9cb`).
2. **Official sources.** We compiled new entries from the Student Handbook, the Scholarship Regulation, the Course
   Registration Guide, the university entrance registration documents and the annual activity report. In one day the
   knowledge base grew from 225 to 412 entries: 70 + 32 + 16 + 25 + 25 + 19
   ([knowledge_base.json](../../nlp-service/data/knowledge_base.json)).

**Why.** The real problem was not the model but the data. A "better prompt" does not fill in missing information.

## 6. "Could you explain a bit more?"

**Problem.** Classified on its own, a follow-up question produced a meaningless intent. Gemini also repeated its
earlier answer. Only 10 messages of history went to Gemini.

**Solution.** If a message contains a typical follow-up phrase or is shorter than 25 characters, and the session has
history (`LooksLikeFollowUp`), it is sent to the classifier together with the user's last question. Gemini is told not
to repeat its earlier answer, and the history window grew to 20 messages
([GeminiApiService](../../src/EduConnect.Infrastructure/Services/GeminiApiService.cs)). Commit: `7051df8`.

**Cost.** A short but independent question ("Kütüphane saatleri?", library hours?) can be taken for a follow-up and
merged with the previous question.

## 7. Simultaneous likes returned 500

**Problem.** A unique index allows one like per user and post. But when two requests arrived at the same moment, the
second one got `23505` from PostgreSQL and the API returned 500. Bookmarks, views and follows had the same problem.

**Solution.** The `IsUniqueViolation()` extension recognizes this error
([DbUpdateExceptionExtensions](../../src/EduConnect.Infrastructure/Extensions/DbUpdateExceptionExtensions.cs)), and the
endpoints read it as "already done" ([PostsController](../../src/EduConnect.Api/Controllers/PostsController.cs),
[UsersController](../../src/EduConnect.Api/Controllers/UsersController.cs)). Commit: `7051df8`.

**Why.** "Check first, then insert" does not stop two requests from passing the check together. The database
guarantees correctness; the application only interprets the result correctly. The like count is not stored in a
separate column either; it is counted from the rows, so it cannot inflate.

## 8. Refresh token and registration races

**Problem.** When two tabs used the same refresh token at the same moment, both could see it as valid. Of two parallel
registrations with the same e-mail, one got a 500.

**Solution.**

- **Refresh:** the token is revoked with a single `UPDATE ... WHERE`, that is, with `ExecuteUpdateAsync`. Only one
  request can change the row; the second gets a clean 401. The code is in [Security](06-security.md).
- **Registration:** retry on conflict. If `23505` comes back, registration is tried once more, and the second attempt
  takes the "existing user" path ([AuthService](../../src/EduConnect.Infrastructure/Services/AuthService.cs)).

Commit: `e1fa5f8` (27 May 2026). I tested rotation locally: a request with the old cookie returns
`{"title":"Unauthorized","status":401,"detail":"Oturum yenilenemedi."}`.

## 9. Posts repeating across pages, a tag search that filled memory

**Problem 1.** The bookmarks and liked lists were ordered by time. Two posts saved in the same second could swap order
between queries, so at a page boundary a post showed up on two pages or disappeared.

**Solution.** A second sort key: `ThenByDescending(p => p.Id)`. The ordering is now stable (commit `7051df8`).

**Problem 2.** The tag search loaded every post into memory and parsed the tags in C#.

**Solution.** We first narrow the candidates in PostgreSQL with `ILIKE '%#tag%'`, cap them at the newest 500, and only
then run the exact tag check in C# (commit `e1fa5f8`).

**Cost.** For a very popular tag, matches older than the newest 500 do not make it into the results. The real fix is
storing tags in their own table.

## 10. The user's file name was written to the database

**Problem.** The visual search history stored the name of the uploaded file as is. A file name is fully under the
user's control and becomes a risk once it ends up in a path or a log line. Upload validation also differed from one
endpoint to another.

**Solution.**

- The history now stores a sanitized key in the form `uploaded:<uuid>.<extension>`
  ([VisualSearchController](../../src/EduConnect.Api/Controllers/VisualSearchController.cs)).
- We wrote one shared validation: size and an allow list of file types
  ([FileUploadValidation](../../src/EduConnect.Api/Common/FileUploadValidation.cs)). The six endpoints that upload post,
  avatar, cover and community images use it together with `RequestSizeLimit`.

Commit: `e1fa5f8`. How the file type is read from the first bytes is in [Security](06-security.md).

## 11. The live site opened empty

**Problem.** In November 2026 I opened the live address and could not log in; Render's free database had expired.

**Solution.** I put the UI online with a demo mode that runs without a backend. The axios adapter changes, the screens
stay the same, and the demo code never enters the real build. Details and the cost are in
[Deployment strategy](09-deployment-strategy.md).

## 12. Three bugs I found while rereading

In November 2026 I reread the code from the start and ran the system locally without a Gemini key. Three bugs came out:

| Bug | How I noticed | Fix |
|---|---|---|
| The bulk Turkish spelling fix of 18 April (commit `209e40d`) had changed the role value in the UI from `moderator` to `moderatör`. The API returns `moderator`, so the moderator list came back empty | Comparing the UI's role values with the API's enums | Back to the contract in 15 files (frontend `6e43f05`) |
| Hubs read the token from the `access_token` query parameter. That code covered `/hubs/chat` and `/hubs/direct-messages`, but not `/hubs/notifications`. Live notifications never arrived; they came through polling once a minute | Comparing the path list in [Program.cs](../../src/EduConnect.Api/Program.cs) with the list of hubs | The notification hub was added to the list (`bfef3aa`) |
| With an empty Gemini key, the `GeminiApiService` constructor threw. Because the service could not be created, EduAI and visual search returned 500 and the fallback paths never ran | Running the local setup without a key | The client stays empty, the error happens inside the call and the fallback path takes over (`79c4fba`) |

**What I learned.** All three bugs share one thing: the contract between two pieces drifted while nobody was looking.
A contract test between the UI and the API, a connection test for the hubs or a smoke test without keys would have
caught them on day one. The project has no automated tests; I wrote in [FAQ and limits](12-faq-and-limits.md) that
they come next.
