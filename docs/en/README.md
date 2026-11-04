# EduConnect documentation

I am Fırat Atalay. These documents explain EduConnect in my own words and in detail: how the project started, which
problems we solved and why we made each technical decision. EduConnect is the graduation project my teammate Ayşe
Mandıralı and I built in the Computer Engineering department of Recep Tayyip Erdoğan University, supported by
TÜBİTAK 2209-A; I was the team captain.

Next to every decision I also wrote down its cost. To me, half of explaining a decision is being able to say what
you gave up for it.

## Where to start

| Who are you? | Suggested path |
|---|---|
| First look, no technical detail needed | [Story](01-story.md) → [Demo tour](02-demo-tour.md) |
| Engineer, curious about the architecture and the code | [Story](01-story.md) → [Architecture](03-architecture.md) → [AI](07-ai.md) → [Problems I solved](11-problems-i-solved.md) |
| Quick review before an interview | [Problems I solved](11-problems-i-solved.md) → [FAQ and limits](12-faq-and-limits.md) |
| Running the project on your machine | [Running and API](10-running-and-api.md) |

## All documents

| # | Document | What is inside |
|---|---|---|
| 01 | [Story](01-story.md) | How it started, the team, timeline, principles, before and after |
| 02 | [Demo tour](02-demo-tour.md) | A 5-minute non-technical tour, roles, demo vs. the real system |
| 03 | [Architecture](03-architecture.md) | Three services, layers, modules, the journey of a request, real time |
| 04 | [Database](04-database.md) | 27 tables, the relationship diagram, rules the database enforces, the move to PostgreSQL |
| 05 | [Domain rules](05-domain-rules.md) | Registration, feed scoring, trends, community roles, event and market rules |
| 06 | [Security](06-security.md) | JWT and refresh rotation, verification codes, authorization, rate limits, error contract, known gaps |
| 07 | [AI](07-ai.md) | EduAI: BERTurk, the knowledge base, Gemini and fallbacks; visual search with ResNet-50 |
| 08 | [Frontend and design](08-frontend-and-design.md) | React architecture, server state, sessions, design language, mobile |
| 09 | [Deployment strategy](09-deployment-strategy.md) | Zero-cost hosting, two channels, the in-browser demo and its cost |
| 10 | [Running and API](10-running-and-api.md) | One-command setup, environment variables, curl examples with real output |
| 11 | [Problems I solved](11-problems-i-solved.md) | Twelve short stories: problem, solution, reasoning |
| 12 | [FAQ and limits](12-faq-and-limits.md) | Likely interview questions, known limits, roadmap |

## Good to know

- The backend and the AI service live in this repository; the user interface lives in
  [educonnect-frontend](https://github.com/mrfiratatalay/educonnect-frontend). Links go straight to the relevant
  file, so you can open the code behind any claim.
- The code is in English; the UI, the knowledge base and the original documents are in Turkish. Some backend
  messages are written without Turkish characters ("gonderini begendi"); I left them as they are in the examples.
- I counted every number in these documents from the code: 89 controller endpoints, 27 tables, 412 knowledge base
  entries and so on.
- No document in this folder is longer than 300 lines.
- Türkçe: [../README.md](../README.md)
