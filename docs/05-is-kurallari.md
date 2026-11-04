# İş kuralları

Bu dokümanda platformun kurallarını, kodda nasıl uygulandıklarıyla birlikte topluyorum. Tarayıcıdaki demo da
aynı kuralları uyguluyor; böylece demoda gördüğün davranış gerçek sistemin davranışı.

## Kayıt ve giriş

| Kural | Değer | Kod |
|---|---|---|
| Kurumsal e-posta | Seçilen üniversitenin alan adı ya da alt alan adı (`ogr.erdogan.edu.tr` geçerli) | [AuthService](../src/EduConnect.Infrastructure/Services/AuthService.cs) |
| Doğrulama kodu | 6 haneli, rastgele, veritabanında SHA-256 özeti | `GenerateVerificationCode` |
| Kodun ömrü | 10 dakika | `EmailVerificationExpiry` |
| Hatalı deneme | En fazla 5; sonra yeni kod gerekir | `VerifyEmailAsync` |
| Yeni kod isteme | 60 saniyede bir | `EmailVerificationResendCooldown` |
| Doğrulanmamış hesap | Giriş yapamaz | `LoginAsync` |
| Sınıf | 1 ile 4 arası | 27 Mayıs'ta 8'den 4'e indirildi |

Doğrulanmamış bir e-postayla yeniden kayıt olunursa yeni hesap açılmıyor; mevcut kayıt güncellenip yeni kod
gönderiliyor. Böylece yarım kalan bir kayıt e-postayı kilitlemiyor.

## Akış

Ana sayfada üç sekme var: **Sana özel**, **Takip ettiklerin** ve **Topluluklar**. "Takip ettiklerin" yalnızca
takip edilen kişilerin topluluk dışı gönderilerini, "Topluluklar" ise üye olunan toplulukların gönderilerini en
yeniden eskiye sıralıyor. "Sana özel" ise puanlı bir sıralama.

### "Sana özel" puanı

Son 30 günün en yeni 250 gönderisi aday oluyor; her biri aşağıdaki tabloya göre puanlanıyor
([PostsController](../src/EduConnect.Api/Controllers/PostsController.cs), `CalculateForYouScore`).

| Sinyal | Puan |
|---|---|
| Takip ettiğin biri paylaşmış | +46 |
| Üyesi olduğun bir toplulukta paylaşılmış | +34 |
| Aynı üniversiteden | +18 |
| Genel gönderi / topluluk gönderisi | +8 / +4 |
| Beğeni sayısı | En fazla +12 |
| Yorum sayısı × 2 | En fazla +12 |
| Görüntülenme sayısı ÷ 3 | En fazla +10 |
| Daha önce beğenmiş ya da kaydetmişsin | +8 |
| İlgi alanı etiketleriyle eşleşme | +18, eşleşen her etiket için +6 |
| Tazelik: 12 saat / 24 saat / 3 gün / 7 gün içinde | +14 / +10 / +6 / +3 |

İlgi alanı etiketleri, son 45 gün içinde senin paylaştığın, beğendiğin ya da kaydettiğin gönderilerdeki en sık 6
etiket. Eşit puanda yeni gönderi öne geçiyor.

**Örnek bir hesap.** Takip ettiğin ve aynı üniversitede okuyan birinin 5 saat önce paylaştığı genel bir gönderi
6 beğeni, 3 yorum ve 9 görüntülenme almışsa puanı: 46 + 18 + 8 + 6 + 6 + 3 + 14 = **101**.

**Neden böyle?** Sosyal bağ (takip, topluluk) en güçlü sinyal; etkileşim sinyalleri bir tavanla sınırlı ki tek
bir popüler gönderi her şeyi ezmesin. Bedeli, ağırlıkların elle seçilmiş olması: bunları gerçek kullanım verisiyle
ayarlama fırsatımız olmadı.

### Gönderinin neden önerildiği

Her "Sana özel" gönderisi bir gerekçe etiketiyle geliyor. Etiket, ilk uyan kurala göre seçiliyor:

1. Takip ettiklerinden
2. Topluluğundan
3. İlgi alanına yakın
4. Aynı üniversiteden
5. Etkileşimi yüksek (beğeni + yorum + görüntülenme en az 8)
6. Yeni paylaşıldı

## Gündemdeki etiketler

Son 7 günün etiketli gönderileri toplanıyor. Önce kullanıcının üniversitesindeki gönderiler deneniyor; o
üniversitede hiç etiket yoksa platformun tamamına bakılıyor. Sıralama: en çok gönderi, sonra en çok farklı yazar,
sonra en son kullanım ([PostTrendAnalyzer](../src/EduConnect.Api/Common/PostTrendAnalyzer.cs)). Etiketler
Türkçe harfleri tanıyor; `#kaçkar` ve `#gömülüsistemler` geçerli.

## Takip önerileri

Adaylar şu sırayla diziliyor: henüz takip edilmeyenler önce, sonra aynı üniversitedekiler, sonra ortak topluluk
sayısı, sonra son 14 günde paylaşım yapanlar, en sonda isim. Önerinin yanında bir gerekçe yazıyor: aynı bölümden,
"2 ortak topluluk", aynı üniversitede, bu hafta aktif ya da keşfetmeye değer
([UsersController](../src/EduConnect.Api/Controllers/UsersController.cs)).

## Topluluk rolleri

Topluluğu kuran kişi otomatik olarak kurucu (`owner`) oluyor. Diğer roller moderatör ve üye.

| İşlem | Kurucu | Moderatör | Üye |
|---|---|---|---|
| Topluluğu düzenlemek ve silmek | Evet | Hayır | Hayır |
| Üyeyi moderatör yapmak, moderatörü üyeye indirmek | Evet | Hayır | Hayır |
| Üyeyi topluluktan çıkarmak | Evet | Yalnızca normal üyeleri | Hayır |
| Topluluk etkinliği oluşturmak | Evet | Evet | Hayır |
| Toplulukta gönderi paylaşmak | Evet | Evet | Evet |
| Topluluktan ayrılmak | Hayır | Evet | Evet |

Kimse kendi rolünü değiştiremez ve kendini çıkaramaz. Topluluk silinince pasifleşir; adresi (slug) addan Türkçe
harfler sadeleştirilerek üretiliyor ve çakışırsa sonuna `-2`, `-3` ekleniyor
([GroupsController](../src/EduConnect.Api/Controllers/GroupsController.cs)).

## Etkinlikler

- Bitiş tarihi başlangıçtan sonra olmalı.
- Topluluk etkinliğini yalnızca o topluluğun kurucusu ya da moderatörü açabilir.
- Kayıt sayısı kontenjana ulaşınca yeni kayıt reddediliyor ("Etkinlik kontenjani dolu").
- İptal edilen kayıt silinmiyor, durumu değişiyor; yeniden kayıt aynı satırı etkinleştiriyor.
- Etkinliği oluşturan kişi, platform moderatörü ve yönetici etkinliği düzenleyip silebilir.

Kontenjan kontrolünün eşzamanlı isteklerdeki zayıflığını [Veritabanı](04-veritabani.md) dokümanında yazdım.

## Pazar

- İlanı satıcı, platform moderatörü ve yönetici düzenleyip kaldırabilir; kaldırılan ilan pasifleşir.
- Ürün durumu dört değerden biri: sıfır, yeni gibi, iyi, kullanılmış.
- Listeleme metin, kategori, satıcı, fiyat aralığı ve duruma göre filtreleniyor; sayfa başına en fazla 50 ilan.
- Görsel aramanın puanlaması [Yapay zekâ](07-yapay-zeka.md) dokümanında.

## Gönderi ve yorum yetkileri

- Gönderiyi sahibi, platform moderatörü ve yönetici düzenleyip silebilir.
- Yorumu yazarı, gönderinin sahibi, moderatör ve yönetici silebilir.
- Gönderi en fazla 1.500, yorum en fazla 500 karakter. Metin ya da görselden en az biri olmalı.

## Mesajlaşma

- Kendinle sohbet başlatamazsın; iki kişi arasında tek sohbet var.
- Mesajlar en yeni sayfa önce gelecek şekilde sayfalanıyor; sayfanın içi eskiden yeniye sıralı.
- Mesaj gönderme dakikada 120 istekle sınırlı.

## Bildirimler

Bildirim üreten olaylar:

| Olay | Bildirimi kim alıyor? |
|---|---|
| Gönderi beğenildi | Gönderi sahibi (kendi gönderisini beğenen hariç) |
| Gönderiye yorum yapıldı | Gönderi sahibi |
| Takip edildi | Takip edilen kişi |
| Topluluğa katılındı | Topluluğun kurucusu |
| Etkinliğe kayıt olundu | Etkinliği oluşturan |
| Mesaj gönderildi | Mesajı alan kişi |

## EduAI oturumları

- Yeni bir oturum açılınca kullanıcının açık oturumları kapatılıyor; aynı anda tek aktif oturum var.
- Kapanmış oturuma mesaj gönderilemiyor. Mesaj en fazla 2.000 karakter.
- Soru gönderme, görsel aramayla birlikte dakikada 30 istekle sınırlı; bu Gemini kotasını korumak için.
- Oturum geçmişinde son 20 oturum listeleniyor.
