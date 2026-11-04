# Security

This document covers authentication, authorization, secrets and the error contract. At the end I collected the
known gaps in a table; that is where the answer to "what is the weakest point of the system?" lives.

## Authentication

### Access token

- A JWT signed with HMAC-SHA256; its default lifetime is 60 minutes
  ([JwtTokenService](../../src/EduConnect.Infrastructure/Services/JwtTokenService.cs)).
- It carries the user id, name, e-mail and role. Validation checks the signature, issuer, audience and lifetime;
  `ClockSkew` is zero, so the token is invalid the moment it expires.
- The UI keeps this token only in memory and never writes it to `localStorage`
  ([token.ts](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/src/features/auth/token.ts)). After a
  page reload the session comes back through the refresh token.

### Refresh token and rotation

**Problem.** A refresh token lives long (7 days). If it is stolen, an attacker can keep getting new access tokens;
and if two tabs use the same token at the same moment, two new sessions could be born.

**Solution.** The refresh token is a 64-byte random value stored in a cookie named `educonnect.refreshToken`:
`HttpOnly` (JavaScript cannot read it), `SameSite=Lax`, and `Secure` over HTTPS. On every use the old token is revoked
and a new one is issued. Revocation is a single atomic query:

```csharp
var revokedRows = await dbContext.RefreshTokens
    .Where(rt => rt.Token == refreshToken && rt.RevokedAtUtc == null && rt.ExpiresAtUtc > nowUtc)
    .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.RevokedAtUtc, nowUtc), cancellationToken);

if (revokedRows == 0) throw new UnauthorizedAccessException("Oturum yenilenemedi.");
```

**Why.** If we read first and updated later, two parallel requests could both see the token as valid. Because
`UPDATE ... WHERE` can change the row only once, the database settles the race: the second request gets a clean 401.
I tried it on the local setup: a request with the rotated old token returns
`{"title":"Unauthorized","status":401,"detail":"Oturum yenilenemedi."}` ("the session could not be refreshed"). Code:
[AuthService.RefreshAsync](../../src/EduConnect.Infrastructure/Services/AuthService.cs).

**Cost.** Rotation does not notice that an old token was reused and does not close the whole session chain; the
`ReplacedByToken` field is there for that detection but is not used.

In the UI, several requests that expire at the same time share a single refresh call: the first one starts the
refresh and the others wait for the same result
([authenticatedRequest.ts](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/src/features/auth/authenticatedRequest.ts)).

## Passwords and e-mail verification

- Passwords are stored with ASP.NET Core Identity's `PasswordHasher`, which uses PBKDF2
  ([PasswordHashService](../../src/EduConnect.Infrastructure/Services/PasswordHashService.cs)).
- A wrong e-mail and a wrong password get the same message: "Email veya sifre hatali." (wrong e-mail or password).
- The verification code is generated with `RandomNumberGenerator`, and the database keeps only its SHA-256 hash. A
  code is valid for 10 minutes, a new one is needed after 5 wrong attempts, and a new code can be requested once
  every 60 seconds.
- Registration only works with the chosen university's domain (or a subdomain of it).

## Authorization

| Role | What can they do? |
|---|---|
| Student | Manages their own content; inside a community their community role decides |
| Moderator | Can remove anyone's posts, comments, events and listings |
| Admin | Everything a moderator can do, plus adding discounts and EduAI analytics |

Roles are checked in two ways:

- **At the endpoint:** `POST /api/discounts` and `GET /api/chat/analytics/summary` are admin-only
  (`[Authorize(Roles = "Admin")]`).
- **At the record:** for posts, comments, events and listings, an "is this the owner, or a moderator/admin?" check
  (helpers like `CanManagePost`). Community actions add the founder, moderator and member rules; the table is in
  [Domain rules](05-domain-rules.md).

EduAI messages check ownership too: a user cannot read someone else's session or give feedback on someone else's
bot message (`Forbid`).

## Rate limits

| Policy | Limit | Where? |
|---|---|---|
| `gemini` | 30 requests per minute, queue of 5 | EduAI messages and visual search |
| `directmessages` | 120 requests per minute, queue of 20 | Sending messages |

When a limit is exceeded, the API returns 429. The goal is to protect the Gemini quota and messaging from abuse.

## File uploads

Avatars, covers, post images and community images go through one shared validation
([FileUploadValidation](../../src/EduConnect.Api/Common/FileUploadValidation.cs)): no empty files, at most 5 MB, and
only JPEG, PNG, WEBP and GIF. The endpoints also have `RequestSizeLimit`. When storing a file, its type is read from
its first bytes rather than its extension (JPEG `FF D8 FF`, PNG `89 50 4E 47`), and the file name is regenerated with
a `Guid` ([PostMediaStorageService](../../src/EduConnect.Infrastructure/Services/PostMediaStorageService.cs)). The
visual search history also stores a sanitized key instead of the file name the user sent.

## Secrets and configuration

- In `appsettings.json` the JWT signing key and the Gemini key are empty. On Render they were set as environment
  variables (`JwtSettings__SecretKey`, `GeminiSettings__ApiKey`, `Email__BrevoApiKey`).
- `.env` files are in `.gitignore`. The local Docker setup needs no secrets.
- The CORS allow list comes from configuration, and `AllowCredentials` is on for the cookie.

## Error contract

Unexpected errors go through [GlobalExceptionHandler](../../src/EduConnect.Api/Middleware/GlobalExceptionHandler.cs)
and come back as RFC 7807 Problem Details:

| Exception | Status code | Title |
|---|---|---|
| `UnauthorizedAccessException` | 401 | Unauthorized |
| `KeyNotFoundException` | 404 | Not Found |
| `InvalidOperationException` | 400 | Invalid Operation |
| `SmtpException` | 503 | Email Delivery Failed |
| Anything else | 500 | Server Error (details hidden) |

An example from the local setup: `{"title":"Invalid Operation","status":400,"detail":"Dogrulama kodu hatali."}`
(wrong verification code).

The UI reads `detail` first, then `message`, and shows it to the user. But the contract is not uniform: business rule
errors return `{ "message": "..." }`, model validation returns `ValidationProblemDetails`, and some EduAI endpoints
return plain text ("Mesaj boş olamaz.", the message cannot be empty). Moving everything to a single Problem Details
format is on my roadmap.

## Known gaps

| Gap | Impact | How it closes |
|---|---|---|
| The refresh cookie is `SameSite=Lax`; the UI is on `vercel.app`, the API on `onrender.com` | The browser does not send the cookie on cross-site requests; in production a page reload dropped the session | Domains under the same site, or `SameSite=None; Secure` with CSRF protection |
| Refresh tokens are plain text in the database | If the database leaks, the tokens can be used | Store a SHA-256 hash of the token |
| No rate limit or account lockout on login | Open to password guessing | A separate rate limit policy for `auth` |
| `InvalidOperationException` messages reach the client | Internal messages such as database errors can leak; that is why I saw the "transient failure" message on the live API | A domain-specific exception type and a generic message |
| Registration says "Bu email adresi zaten kayitli" (this e-mail is already registered) | Anyone can find out whether an e-mail is registered | The same answer in every case |
| The verification code hash is unsalted SHA-256 | If the database leaks, a 6-digit code is found quickly (it lives 10 minutes) | HMAC with a secret key |
| No password reset flow | "Forgot password" only sends an informational e-mail | A single-use reset token |
| CORS default in the NLP service is `*` | Without `ALLOWED_ORIGINS` the service is open to every origin | Make the variable required |
| The notification hub did not read the token | Notifications arrived through polling once a minute | Fixed in November 2026 |
