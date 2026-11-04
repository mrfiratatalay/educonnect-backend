# Domain rules

This document collects the platform's rules together with how the code applies them. The in-browser demo applies the
same rules, so the behavior you see in the demo is the behavior of the real system.

## Registration and login

| Rule | Value | Code |
|---|---|---|
| Institutional e-mail | The chosen university's domain or a subdomain of it (`ogr.erdogan.edu.tr` is valid) | [AuthService](../../src/EduConnect.Infrastructure/Services/AuthService.cs) |
| Verification code | 6 random digits; the database keeps a SHA-256 hash | `GenerateVerificationCode` |
| Code lifetime | 10 minutes | `EmailVerificationExpiry` |
| Wrong attempts | At most 5; then a new code is needed | `VerifyEmailAsync` |
| Requesting a new code | Once every 60 seconds | `EmailVerificationResendCooldown` |
| Unverified account | Cannot log in | `LoginAsync` |
| Year of study | Between 1 and 4 | Lowered from 8 to 4 on 27 May |

If someone registers again with an unverified e-mail, no new account is created; the existing record is updated and
a new code is sent. That way a half-finished registration does not lock the e-mail address.

## Feed

The home page has three tabs: **Sana özel** (for you), **Takip ettiklerin** (following) and **Topluluklar**
(communities). "Following" shows only the non-community posts of the people you follow, and "Communities" shows the
posts of the communities you belong to, newest first. "For you" is a scored ranking.

### The "for you" score

The newest 250 posts of the last 30 days become candidates, and each one is scored with the table below
([PostsController](../../src/EduConnect.Api/Controllers/PostsController.cs), `CalculateForYouScore`).

| Signal | Points |
|---|---|
| Posted by someone you follow | +46 |
| Posted in a community you belong to | +34 |
| From the same university | +18 |
| General post / community post | +8 / +4 |
| Like count | Up to +12 |
| Comment count × 2 | Up to +12 |
| View count ÷ 3 | Up to +10 |
| You liked or saved it before | +8 |
| Matches your interest tags | +18, plus +6 for each matching tag |
| Freshness: within 12 hours / 24 hours / 3 days / 7 days | +14 / +10 / +6 / +3 |

Your interest tags are the 6 most frequent tags in the posts you shared, liked or saved during the last 45 days. On a
tie, the newer post wins.

**A worked example.** A general post shared 5 hours ago by someone you follow who studies at your university, with 6
likes, 3 comments and 9 views, scores: 46 + 18 + 8 + 6 + 6 + 3 + 14 = **101**.

**Why like this?** Social ties (follows, communities) are the strongest signal; engagement signals are capped so a
single popular post cannot crush everything else. The cost is that the weights were chosen by hand: we never had the
chance to tune them with real usage data.

### Why a post was recommended

Every "for you" post comes with a reason label, chosen by the first rule that matches:

1. From people you follow
2. From your community
3. Close to your interests
4. From your university
5. High engagement (likes + comments + views at least 8)
6. Newly shared

## Trending tags

Tagged posts of the last 7 days are collected. Posts from the user's university are tried first; if that university
has no tags at all, the whole platform is used. Ordering: most posts, then most distinct authors, then most recent use
([PostTrendAnalyzer](../../src/EduConnect.Api/Common/PostTrendAnalyzer.cs)). Tags understand Turkish letters;
`#kaçkar` and `#gömülüsistemler` are valid.

## Follow suggestions

Candidates are ordered like this: people you do not follow yet first, then people from the same university, then the
number of shared communities, then those who posted in the last 14 days, and finally by name. Each suggestion comes
with a reason: same department, "2 shared communities", same university, active this week or worth discovering
([UsersController](../../src/EduConnect.Api/Controllers/UsersController.cs)).

## Community roles

Whoever creates a community automatically becomes its founder (`owner`). The other roles are moderator and member.

| Action | Founder | Moderator | Member |
|---|---|---|---|
| Edit and delete the community | Yes | No | No |
| Promote a member to moderator, demote a moderator | Yes | No | No |
| Remove a member from the community | Yes | Only regular members | No |
| Create a community event | Yes | Yes | No |
| Post in the community | Yes | Yes | Yes |
| Leave the community | No | Yes | Yes |

Nobody can change their own role or remove themselves. A deleted community is deactivated. Its address (slug) is
built from the name with Turkish letters simplified, and `-2`, `-3` is appended on a collision
([GroupsController](../../src/EduConnect.Api/Controllers/GroupsController.cs)).

## Events

- The end date must be after the start date.
- Only the founder or a moderator of a community can create an event for it.
- When registrations reach capacity, new ones are rejected ("Etkinlik kontenjani dolu", the event is full).
- A cancelled registration is not deleted; its status changes, and registering again reactivates the same row.
- The creator, platform moderators and admins can edit and delete an event.

I wrote about the weakness of the capacity check under concurrent requests in [Database](04-database.md).

## Market

- The seller, platform moderators and admins can edit and remove a listing; a removed listing is deactivated.
- A product's condition is one of four values: new, like new, good, used.
- Listings can be filtered by text, category, seller, price range and condition; at most 50 listings per page.
- The scoring of visual search is in [AI](07-ai.md).

## Post and comment permissions

- The owner, platform moderators and admins can edit and delete a post.
- The author, the post owner, moderators and admins can delete a comment.
- A post is at most 1,500 characters and a comment at most 500. A post needs text, an image or both.

## Messaging

- You cannot start a conversation with yourself; there is one conversation between two people.
- Messages are paged newest page first; within a page they are ordered oldest to newest.
- Sending messages is limited to 120 requests per minute.

## Notifications

Events that create a notification:

| Event | Who gets the notification? |
|---|---|
| A post was liked | The post owner (not when liking your own post) |
| A post got a comment | The post owner |
| Someone followed you | The followed person |
| Someone joined a community | The community founder |
| Someone registered for an event | The event creator |
| A message was sent | The recipient |

## EduAI sessions

- Opening a new session closes the user's open sessions; there is one active session at a time.
- A closed session cannot receive messages. A message is at most 2,000 characters.
- Asking questions is limited to 30 requests per minute together with visual search, to protect the Gemini quota.
- The session history lists the last 20 sessions.
