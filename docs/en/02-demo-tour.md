# Demo tour

This tour needs no technical background. There is no setup, account or password: open the
[live demo](https://educonnect-tez.vercel.app) and click a role card. The demo runs entirely in your browser; every
change you make is stored only in your browser and affects nobody else.

![The landing page with role cards](../screenshots/landing.webp)

## Roles

| Card | Who? | What it shows best |
|---|---|---|
| **Experienced student** | Zeynep Kaya, 3rd-year Computer Engineering | A full feed, messages, EduAI history; member management, because she founded the Software Club |
| **New student** | Emre Yıldız, 1st-year Electrical and Electronics Engineering | Day one on the platform: an empty following feed, follow suggestions, discovering communities |
| **Moderator** | Elif Demir, platform moderator | Removing posts, comments and events that break the rules, on anyone's behalf |
| **Admin** | Mert Aksoy, platform admin | The same powers as a moderator in the UI; adding discounts and EduAI analytics are API-only for now |

The UI is in Turkish, so the steps below give the Turkish label next to each English name. The **Demo** badge in the
bottom-left corner is visible on every page. Click it to switch roles or start the demo over.

## A 5-minute tour

**1. Login (0:00).** Click the "Deneyimli öğrenci" (experienced student) card. The real login flow runs; the card only
fills in the e-mail and password for you.

**2. Feed (0:30).** Every post in the "Sana özel" (for you) tab says why it was recommended: from people you follow,
from your community or close to your interests. The ranking comes from the people you follow, your communities, the
tags you engage with and how fresh the post is. Like a post, save it or write a comment.

![Personalized feed](../screenshots/feed.webp)

**3. Community (1:30).** From the left menu, open **Topluluklar** (communities) → **RTEÜ Yazılım Topluluğu**. Zeynep
founded it, so she sees the "Üye yönetimi" (member management) button. Make a member a moderator, then undo it. The
founder cannot leave the community and a moderator cannot remove the founder; these rules are the same as in the
real API.

**4. EduAI (2:30).** Switch to **EduAI** in the left menu and ask "Bütünleme sınavına kimler girebilir?" (who can take
the make-up exam?). The answer comes from a 412-entry knowledge base compiled from the university's official
sources; below it you see the source link, the topic of the question (for example `exam_and_grading`) and the
confidence band.

![EduAI answers with its source](../screenshots/eduai.webp)

**5. Market and visual search (3:15).** Browse the listings in **Pazar** (market), then press **Görsel Ara** (visual
search) and upload a product photo. If the file name contains the product name (for example `hesap-makinesi.jpg`,
"calculator"), the demo recognizes the product and ranks similar listings with a score.

**6. Messages (3:45).** Open the conversation with Burak Şahin under **Sohbet** (chat) and send a message. A reply
arrives within a few seconds and the number on the notification bell goes up on its own. In the real system this
runs on SignalR.

**7. Switch roles (4:15).** Demo badge → **Rol değiştir** (switch role) → **Yeni öğrenci** (new student). The
following feed is empty, but follow suggestions appear on the right. Then log in as **Moderatör** and notice the
delete icon on comments under someone else's post.

## The demo vs. the real system

The demo speaks the same contract as the real API: the same paths, field names and error codes. The screen code is
identical in both modes. The differences:

| | Live demo | Real system |
|---|---|---|
| **Data** | Sample data in the browser's `localStorage` | PostgreSQL |
| **EduAI** | A keyword classifier and the same knowledge base, in the browser | The BERTurk model, the knowledge base and Gemini |
| **Visual search** | The product is guessed from the file name; the scoring is the same | Gemini analyzes the image and ResNet-50 similarity is added |
| **E-mail verification** | The code is shown on screen | The code goes to the institutional e-mail address |
| **Live messages and notifications** | An in-browser event bus | SignalR hubs |
| **Session refresh** | `localStorage` | A refresh token in an HttpOnly cookie |
| **Some system texts** | With Turkish characters | Some without Turkish characters ("gonderini begendi") |

Dates are computed relative to today on every visit. Even if you open the demo a week later, a post shared "2 hours
ago" still looks new and next week's event is still next week.

## Resetting the demo

Demo badge → **Demoyu sıfırla** (reset the demo). Every change you made in this browser is deleted and the sample
data loads again.

## If you want to see the real system

- [Architecture](03-architecture.md) and [Problems I solved](11-problems-i-solved.md): the decisions behind the code.
- [Running and API](10-running-and-api.md): PostgreSQL, the API and a mail catcher on your machine with one command.
- How the demo was built and what it cost: [Deployment strategy](09-deployment-strategy.md).
