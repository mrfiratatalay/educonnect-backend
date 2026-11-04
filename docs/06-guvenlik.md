# Güvenlik

Bu dokümanda kimlik doğrulamayı, yetkiyi, sırları ve hata sözleşmesini anlatıyorum. Sonda bilinen eksikleri bir
tablo hâlinde topladım; bir mülakatta "sistemin en zayıf noktası ne?" sorusunun cevabı orada.

## Kimlik doğrulama

### Access token

- HMAC-SHA256 ile imzalı JWT; varsayılan ömrü 60 dakika ([JwtTokenService](../src/EduConnect.Infrastructure/Services/JwtTokenService.cs)).
- İçinde kullanıcı kimliği, ad, e-posta ve rol var. Doğrulamada imza, yayıncı, hedef kitle ve süre kontrol
  ediliyor; `ClockSkew` sıfır, yani süre dolduğu an token geçersiz.
- Arayüz bu token'ı yalnızca bellekte tutuyor; `localStorage`'a yazmıyor
  ([token.ts](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/src/features/auth/token.ts)). Sayfa
  yenilenince oturum refresh token ile geri geliyor.

### Refresh token ve rotasyon

**Problem.** Refresh token uzun ömürlü (7 gün). Çalınırsa saldırgan sürekli yeni access token alabilir; aynı token'ı
iki sekme aynı anda kullanırsa iki yeni oturum doğabilir.

**Çözüm.** Refresh token 64 baytlık rastgele bir değer ve `educonnect.refreshToken` adlı bir çerezde duruyor:
`HttpOnly` (JavaScript okuyamaz), `SameSite=Lax`, HTTPS'te `Secure`. Her kullanımda eski token iptal edilip yerine
yenisi veriliyor. İptal tek bir atomik sorguyla yapılıyor:

```csharp
var revokedRows = await dbContext.RefreshTokens
    .Where(rt => rt.Token == refreshToken && rt.RevokedAtUtc == null && rt.ExpiresAtUtc > nowUtc)
    .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.RevokedAtUtc, nowUtc), cancellationToken);

if (revokedRows == 0) throw new UnauthorizedAccessException("Oturum yenilenemedi.");
```

**Neden.** Önce okuyup sonra güncelleseydik iki paralel istek aynı token'ı geçerli görebilirdi. `UPDATE ... WHERE`
satırı yalnızca bir kez değiştirebildiği için yarışı veritabanı çözüyor: ikinci istek temiz bir 401 alıyor. Yerel
kurulumda denedim: döndürülmüş eski token'la yapılan istek `{"title":"Unauthorized","status":401,"detail":"Oturum
yenilenemedi."}` dönüyor. Kod: [AuthService.RefreshAsync](../src/EduConnect.Infrastructure/Services/AuthService.cs).

**Bedeli.** Rotasyon, eski token'ın yeniden kullanıldığını fark edip bütün oturum zincirini kapatmıyor;
`ReplacedByToken` alanı bu tespit için hazır ama kullanılmıyor.

Arayüzde aynı anda süresi dolan birkaç istek tek bir refresh çağrısını paylaşıyor; ilk istek yenilemeyi başlatıyor,
diğerleri aynı sonucu bekliyor
([authenticatedRequest.ts](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/src/features/auth/authenticatedRequest.ts)).

## Şifreler ve e-posta doğrulama

- Şifreler ASP.NET Core Identity'nin `PasswordHasher` sınıfıyla, PBKDF2 kullanılarak saklanıyor
  ([PasswordHashService](../src/EduConnect.Infrastructure/Services/PasswordHashService.cs)).
- Yanlış e-posta ve yanlış şifre aynı mesajı alıyor: "Email veya sifre hatali."
- Doğrulama kodu `RandomNumberGenerator` ile üretiliyor, veritabanında yalnızca SHA-256 özeti tutuluyor. Kod 10
  dakika geçerli, 5 hatalı denemeden sonra yenisi gerekiyor, yeni kod 60 saniyede bir istenebiliyor.
- Kayıt yalnızca seçilen üniversitenin alan adıyla (ya da alt alan adıyla) yapılabiliyor.

## Yetki

| Rol | Ne yapabilir? |
|---|---|
| Öğrenci | Kendi içeriğini yönetir; topluluk içindeki yetkisini topluluk rolü belirler |
| Moderatör | Herkesin gönderisini, yorumunu, etkinliğini ve ilanını kaldırabilir |
| Yönetici | Moderatörün yapabildikleri; ayrıca indirim ekleme ve EduAI analitiği |

Rol kontrolü iki biçimde yapılıyor:

- **Uç nokta düzeyinde:** `POST /api/discounts` ve `GET /api/chat/analytics/summary` yalnızca yöneticiye açık
  (`[Authorize(Roles = "Admin")]`).
- **Kayıt düzeyinde:** Gönderi, yorum, etkinlik ve ilan işlemlerinde "sahibi mi ya da moderatör/yönetici mi?"
  kontrolü (`CanManagePost` gibi yardımcılar). Topluluk işlemlerinde kurucu, moderatör ve üye kuralları ayrıca
  uygulanıyor; tablosu [İş kuralları](05-is-kurallari.md) dokümanında.

EduAI mesajlarında da sahiplik kontrolü var: bir kullanıcı başka birinin oturumunu okuyamıyor, başkasının bot
mesajına geri bildirim veremiyor (`Forbid`).

## Hız sınırı

| Politika | Sınır | Nerede? |
|---|---|---|
| `gemini` | Dakikada 30 istek, 5'lik kuyruk | EduAI mesajı ve görsel arama |
| `directmessages` | Dakikada 120 istek, 20'lik kuyruk | Mesaj gönderme |

Sınır aşılınca API 429 dönüyor. Amaç Gemini kotasını ve mesajlaşmayı kötüye kullanıma karşı korumak.

## Dosya yükleme

Avatar, kapak, gönderi ve topluluk görselleri ortak bir doğrulamadan geçiyor
([FileUploadValidation](../src/EduConnect.Api/Common/FileUploadValidation.cs)): boş dosya yok, en fazla 5 MB ve
yalnızca JPEG, PNG, WEBP, GIF. Uç noktalarda ayrıca `RequestSizeLimit` var. Saklarken dosya türü uzantıdan değil,
dosyanın ilk baytlarından anlaşılıyor (JPEG `FF D8 FF`, PNG `89 50 4E 47`) ve dosya adı `Guid` ile yeniden
üretiliyor ([PostMediaStorageService](../src/EduConnect.Infrastructure/Services/PostMediaStorageService.cs)).
Görsel arama geçmişinde de kullanıcının verdiği dosya adı değil, temizlenmiş bir anahtar saklanıyor.

## Sırlar ve yapılandırma

- `appsettings.json` içinde JWT imza anahtarı ve Gemini anahtarı boş. Render'da ortam değişkeni olarak veriliyor
  (`JwtSettings__SecretKey`, `GeminiSettings__ApiKey`, `Email__BrevoApiKey`).
- `.env` dosyaları `.gitignore`'da. Yerel Docker kurulumu hiçbir sır istemiyor.
- CORS izin listesi yapılandırmadan geliyor ve çerez için `AllowCredentials` açık.

## Hata sözleşmesi

Beklenmeyen hatalar [GlobalExceptionHandler](../src/EduConnect.Api/Middleware/GlobalExceptionHandler.cs) üzerinden
RFC 7807 Problem Details olarak dönüyor:

| İstisna | Durum kodu | Başlık |
|---|---|---|
| `UnauthorizedAccessException` | 401 | Unauthorized |
| `KeyNotFoundException` | 404 | Not Found |
| `InvalidOperationException` | 400 | Invalid Operation |
| `SmtpException` | 503 | Email Delivery Failed |
| Diğerleri | 500 | Server Error (ayrıntı gizli) |

Örnek, yerel kurulumdan: `{"title":"Invalid Operation","status":400,"detail":"Dogrulama kodu hatali."}`.

Arayüz önce `detail`, sonra `message` alanını okuyup kullanıcıya gösteriyor. Ama sözleşme tek tip değil: iş
kuralı hataları `{ "message": "..." }`, model doğrulaması `ValidationProblemDetails`, bazı EduAI uç noktaları ise
düz metin dönüyor ("Mesaj boş olamaz."). Bunu tek bir Problem Details biçiminde toplamak yol haritamda.

## Bilinen eksikler

| Eksik | Etkisi | Nasıl kapanır? |
|---|---|---|
| Refresh çerezi `SameSite=Lax`; arayüz `vercel.app`, API `onrender.com` adresinde | Tarayıcı çerezi siteler arası istekte göndermiyor; yayında sayfa yenilenince oturum düşüyor | Aynı site altında alan adları ya da `SameSite=None; Secure` ve CSRF koruması |
| Refresh token'lar veritabanında düz metin | Veritabanı sızarsa token'lar kullanılabilir | Token'ın SHA-256 özetini saklamak |
| Giriş uç noktasında hız sınırı ya da hesap kilidi yok | Şifre denemeye açık | `auth` için ayrı bir hız sınırı politikası |
| `InvalidOperationException` mesajı istemciye gidiyor | Veritabanı hatası gibi iç mesajlar sızabiliyor; canlı API'de "transient failure" mesajını bu yüzden gördüm | Alana özel istisna tipi ve genel bir mesaj |
| Kayıt "Bu email adresi zaten kayitli" diyor | Bir e-postanın kayıtlı olup olmadığı öğrenilebilir | Her durumda aynı cevap |
| Doğrulama kodunun özeti tuzsuz SHA-256 | Veritabanı sızarsa 6 haneli kod hızla bulunur (kodun ömrü 10 dakika) | Gizli anahtarlı HMAC |
| Şifre sıfırlama akışı yok | "Şifremi unuttum" yalnızca bilgilendirme e-postası gönderiyor | Tek kullanımlık sıfırlama token'ı |
| NLP servisinde CORS varsayılanı `*` | `ALLOWED_ORIGINS` verilmezse servis her kaynağa açık | Değişkeni zorunlu yapmak |
| Bildirim hub'ı token'ı okumuyordu | Bildirimler dakikada bir yoklamayla geliyordu | Kasım 2026'da düzeltildi |
