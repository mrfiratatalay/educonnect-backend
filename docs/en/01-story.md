# Story

EduConnect is an AI-assisted campus platform for university students: a social feed, communities, events, a
second-hand market, direct messages and an assistant (EduAI) that answers university questions with a source, all in
one place. We built it as our graduation thesis in the Computer Engineering department of Recep Tayyip Erdoğan
University (RTEÜ), and the project was supported by TÜBİTAK's 2209-A Research Projects Support Programme for
Undergraduate Students.

## Why this project?

When we started university, it was hard to keep up with campus life. Which event was where, which student clubs
existed, who could use the textbook we no longer needed, who was allowed to take the make-up exam: all of this was
scattered. WhatsApp groups were chaotic, general social media had nothing to do with the university, and official
announcements were buried in long PDFs.

We designed EduConnect to bring these needs into one place. The first audience is RTEÜ students, but universities
are just rows in a table, so the platform can open up to other universities too. Signing up requires an
institutional e-mail address of the chosen university, so everyone on the platform really is a student.

## Team

The team had two people: **Fırat Atalay** (team captain) and **Ayşe Mandıralı**. We shared the work from start to
finish: design decisions, the backend, the AI service, the frontend and deployment were all our joint work, and we got
through every hard part together. I, Fırat Atalay, wrote these documents; I write "we" for decisions we made as a team
and "I" for my own observations and for this latest revision.

I wrote a large part of the code together with AI coding assistants (Claude Code). The rule files we kept for these
assistants are still visible in the repository history. We decided the architecture, the rules and whether each
change was correct, and I wrote every claim in these documents by checking the code.

## Timeline

Dates come from the git history of both repositories.

| Date | What happened? |
|---|---|
| 24 March 2026 | First commits of the backend and frontend repositories |
| April 2026 | Modules, Gemini integration, community images; on 18 April the "deep teal" redesign of the UI |
| 30 April 2026 | Falling back to the local knowledge base when the Gemini quota runs out, the model moved to Git LFS, Windows start scripts |
| 26 May 2026 | From SQL Server to PostgreSQL, Docker and Render, Brevo for e-mail, a README for the TÜBİTAK presentation |
| 27 May 2026 | Concurrency and security fixes; the knowledge base grew from 225 to 412 entries using official PDFs |
| 4 June 2026 | The first documents |
| November 2026 | This revision: an in-browser live demo, documents verified against the code, three bug fixes |

## Principles we followed

We never wrote these down on a wall, but each one leaves a clear trace in the code.

1. **Any external service can fail; the user should never get an empty answer.** If the NLP service fails, the
   backend continues with a safe fallback result; if Gemini fails, the answer comes from the local knowledge base.
   Details in [AI](07-ai.md).
2. **Secrets do not live in the repository.** The JWT key and the Gemini key are empty in `appsettings.json`; the
   real values come from environment variables ([appsettings.json](../../src/EduConnect.Api/appsettings.json)).
3. **Stay inside the free tiers.** Render's 512 MB free server was the main reason we moved the AI models into a
   separate service ([Deployment strategy](09-deployment-strategy.md)).
4. **The AI's answer should be transparent.** Next to every answer, EduAI shows the detected intent, the confidence
   band, the knowledge base match and a source link.
5. **The code is in English; everything the user sees is in Turkish.**

## Before and after

We submitted the project in May 2026. In November 2026 I reread it from start to finish for my portfolio; this
table is the result of that reading.

| | At submission (May 2026) | After this revision |
|---|---|---|
| **Live site** | Connected to the database on Render. When the free database expired, nobody could even log in | An in-browser demo: one-click, password-free login with every role |
| **Local setup** | Windows scripts; the compose file the README described was not in the repository | `docker compose up --build` |
| **Without a Gemini key** | EduAI and visual search returned 500 | Answers come from the local knowledge base |
| **Community moderators** | A bulk Turkish spelling fix had broken the role value; the moderator list came back empty | Matches the API contract |
| **Live notifications** | The notification hub did not read the token; notifications arrived through polling once a minute | The hub reads the token |
| **Documents** | Short notes describing controllers that did not exist and a 15-minute token | 12 documents verified against the code, in Turkish and English |
| **Repository** | A setup note written for an AI, a database backup, dead code | Cleaned up |

I explain how I found the last three bugs at the end of [Problems I solved](11-problems-i-solved.md).

## What I learned from this project

- **Free infrastructure shapes the architecture too.** The memory limit, the blocked SMTP ports and the expiring
  database each turned into a design decision.
- **The model is not what makes an AI feature valuable; the system around it is.** The real work was feeding the
  knowledge base from official sources, tuning confidence thresholds and designing what happens when a service is
  down.
- **Concurrency starts with two people clicking at the same time.** The race conditions in likes, refresh tokens and
  registration taught us the value of database constraints.
- **Making a big change without tests is expensive.** The project has no automated tests; I wrote that down
  honestly in [FAQ and limits](12-faq-and-limits.md).
