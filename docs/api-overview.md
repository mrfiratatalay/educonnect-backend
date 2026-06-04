# EduConnect — API Genel Bakış

Tam dokümantasyon için Swagger UI: https://educonnect-backend-qr03.onrender.com/swagger

## Controller Listesi (15 adet)

| Controller | Prefix | Açıklama |
|------------|--------|----------|
| `AuthController` | `/api/auth` | Kayıt, giriş, e-posta doğrulama, token yenileme |
| `UsersController` | `/api/users` | Profil görüntüleme, takip, arama |
| `ProfileController` | `/api/profile` | Profil güncelleme, avatar/kapak yükleme |
| `PostsController` | `/api/posts` | CRUD, feed, beğeni, yorum, bookmark |
| `CommentsController` | `/api/comments` | Yorum ekleme/silme |
| `GroupsController` | `/api/groups` | Topluluk CRUD, üyelik yönetimi |
| `EventsController` | `/api/events` | Etkinlik CRUD, katılım |
| `ExploreController` | `/api/explore` | Trend içerik, etiket arama |
| `MessagesController` | `/api/messages` | Konuşma listesi, mesaj geçmişi |
| `NotificationsController` | `/api/notifications` | Bildirim listesi, okundu işaretleme |
| `ProductsController` | `/api/products` | Marketplace CRUD, kategori filtresi |
| `VisualSearchController` | `/api/visualsearch` | Görsel yükle, benzer ürün getir |
| `ChatController` | `/api/chat` | EduAI oturum yönetimi, mesaj gönderme |
| `BookmarksController` | `/api/bookmarks` | Kaydedilen gönderiler |
| `FeedbackController` | `/api/feedback` | Uygulama içi geri bildirim |

## Kimlik Doğrulama

Tüm korumalı endpoint'ler `Authorization: Bearer <access_token>` header'ı ister.

**Token akışı:**
1. `POST /api/auth/login` → `accessToken` (15dk) + `refreshToken` (7gün) döner
2. Access token süresi dolunca `POST /api/auth/refresh` ile yenile
3. Refresh token da geçersizse kullanıcıyı tekrar login'e yönlendir

## Öne Çıkan Endpoint'ler

```
POST /api/auth/register          → Kayıt + doğrulama maili
POST /api/auth/verify-email      → 6 haneli kod ile aktifleştirme
POST /api/auth/login             → JWT + refresh token
GET  /api/posts/feed             → Kişiselleştirilmiş feed
POST /api/chat/sessions          → EduAI oturumu başlat
POST /api/chat/sessions/{id}/messages  → Soru sor, AI cevabı al
POST /api/products               → Ürün ilanı oluştur
POST /api/visualsearch/search    → Görsel ile benzer ürün ara
```

## Rate Limiting

- Genel API: ASP.NET Core built-in rate limiter
- Gemini endpoint'leri: ayrı, daha kısıtlı limit (API kota koruması)

## SignalR Hub

- Adres: `/hubs/messages`
- Kullanım: Gerçek zamanlı birebir mesajlaşma
- Client event: `ReceiveMessage`
- Server method: `SendMessage`
