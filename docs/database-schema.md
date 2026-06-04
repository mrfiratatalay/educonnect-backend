# EduConnect — Veritabanı Şeması

PostgreSQL 18 üzerinde 27 tablo. EF Core Code-First migration'larıyla yönetilir.

## Tablo Grupları

### Kullanıcı & Kimlik (4 tablo)

| Tablo | Açıklama |
|-------|----------|
| `Users` | Temel kimlik: e-posta, şifre hash (PBKDF2), doğrulama durumu |
| `StudentProfiles` | Öğrenci profili: bölüm, sınıf, biyografi, avatar URL, kapak URL |
| `RefreshTokens` | JWT refresh token'ları, rotasyon ile geçersizleştirme |
| `Universities` | Üniversite bilgisi, e-posta domain eşleştirmesi (örn. `erdogan.edu.tr`) |

### Sosyal Medya (8 tablo)

| Tablo | Açıklama |
|-------|----------|
| `Posts` | Gönderi içeriği, medya URL'leri, görüntülenme sayacı |
| `PostComments` | Yorumlar, iç içe yorum desteği (parentId) |
| `PostLikes` | Kullanıcı-gönderi beğeni ilişkisi |
| `PostBookmarks` | Kaydedilen gönderiler |
| `PostViews` | Görüntülenme kaydı (tekrar sayılmaz) |
| `UserFollows` | Takip eden / takip edilen ilişkisi |
| `Categories` | Gönderi ve ürün kategorileri |
| `Discounts` | İndirim/kampanya duyuruları |

### Topluluklar & Etkinlikler (4 tablo)

| Tablo | Açıklama |
|-------|----------|
| `Groups` | Topluluk adı, açıklama, kurallar, soft delete |
| `GroupMembers` | Üyelik + rol (Owner/Admin/Member) |
| `Events` | Etkinlik başlık, tarih, yer, katılımcı limiti |
| `EventParticipants` | Kullanıcı-etkinlik katılım kaydı |

### Mesajlaşma & Bildirimler (3 tablo)

| Tablo | Açıklama |
|-------|----------|
| `DirectConversations` | İki kullanıcı arasındaki konuşma kaydı |
| `DirectMessages` | Mesaj içeriği, okundu durumu |
| `Notifications` | Bildirim tipi, tetikleyen olay, target path (derin link) |

### Marketplace (4 tablo)

| Tablo | Açıklama |
|-------|----------|
| `Products` | İkinci el ürün ilanı: başlık, fiyat, açıklama, satıcı |
| `ProductImages` | Ürüne ait görsel URL'leri (çoklu) |
| `VisualSearchHistories` | Görsel arama geçmişi |
| `VisualSearchResults` | Arama sonuç ürünleri (benzerlik skoru ile) |

### EduAI Chatbot (3 tablo)

| Tablo | Açıklama |
|-------|----------|
| `ChatSessions` | Kullanıcı bazlı chatbot oturumu |
| `ChatMessages` | Kullanıcı sorusu + AI cevabı, confidence band, isFallback bayrağı |
| `ChatMessageFeedbacks` | 👍/👎 geri bildirim, model iyileştirmesi için |

### Diğer (1 tablo)

| Tablo | Açıklama |
|-------|----------|
| `Feedbacks` | Genel uygulama içi kullanıcı geri bildirimi |

## Önemli Tasarım Notları

- **Soft delete**: `Groups` tablosunda `IsDeleted` / `DeletedAt` — kayıtlar fiziksel silinmez
- **Timestamp tutarlılığı**: Npgsql 6+ `EnableLegacyTimestampBehavior` ile DateTime uyumu sağlandı
- **Index'ler**: Sık sorgulanan FK kolonlarına (UserId, PostId vb.) index eklendi
- **MS SQL → PostgreSQL Geçişi**: 11 migration silindi, `InitialPostgresCreate` tek migration olarak yeniden oluşturuldu. Veri taşıma için `tools/MigrateData/` ETL uygulaması kullanıldı (815 satır, 0 hata)
