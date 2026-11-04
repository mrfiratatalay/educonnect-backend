# Demo turu

Bu tur teknik bilgi gerektirmiyor. Kurulum, hesap ya da şifre yok: [canlı demoyu](https://educonnect-tez.vercel.app)
açıp bir rol kartına tıklaman yeterli. Demo tamamen tarayıcında çalışıyor; yaptığın her değişiklik yalnızca senin
tarayıcında saklanıyor ve kimseyi etkilemiyor.

![Rol seçim kartlarıyla giriş sayfası](screenshots/landing.webp)

## Roller

| Kart | Kim? | En iyi neyi gösterir? |
|---|---|---|
| **Deneyimli öğrenci** | Zeynep Kaya, Bilgisayar Mühendisliği 3. sınıf | Dolu bir akış, mesajlar, EduAI geçmişi; Yazılım Topluluğu'nun kurucusu olduğu için üye yönetimi |
| **Yeni öğrenci** | Emre Yıldız, Elektrik-Elektronik Mühendisliği 1. sınıf | Platforma ilk gün: boş takip akışı, takip önerileri, topluluk keşfi |
| **Moderatör** | Elif Demir, platform moderatörü | Kurallara aykırı gönderi, yorum ve etkinlikleri herkes adına kaldırabilme |
| **Yönetici** | Mert Aksoy, platform yöneticisi | Arayüzde moderatörle aynı yetkiler; indirim ekleme ve EduAI analitiği şimdilik yalnızca API'de |

Ekranın sol altındaki **Demo** rozeti her sayfada görünüyor. Rozete tıklayınca rol değiştirebilir ya da demoyu
baştan başlatabilirsin.

## 5 dakikalık tur

**1. Giriş (0:00).** "Deneyimli öğrenci" kartına tıkla. Gerçek sistemdeki giriş akışı çalışıyor; yalnızca
e-posta ve şifreyi senin yerine kart dolduruyor.

**2. Akış (0:30).** "Sana özel" sekmesindeki her gönderinin üstünde neden önerildiği yazıyor: "Takip
ettiklerinden", "Topluluğundan", "İlgi alanına yakın". Sıralamayı takip ettiğin kişiler, toplulukların, ilgi
duyduğun etiketler ve gönderinin tazeliği belirliyor. Bir gönderiyi beğen, kaydet ya da yorum yaz.

![Kişiselleştirilmiş akış](screenshots/feed.webp)

**3. Topluluk (1:30).** Sol menüden **Topluluklar** → **RTEÜ Yazılım Topluluğu**. Zeynep kurucu olduğu için
"Üye yönetimi" düğmesini görüyor. Bir üyeyi moderatör yap, sonra geri al. Kurucu topluluktan ayrılamaz,
moderatör kurucuyu çıkaramaz; bu kurallar gerçek API'dekiyle aynı.

**4. EduAI (2:30).** Sol menüden **EduAI**'a geç ve "Bütünleme sınavına kimler girebilir?" diye sor. Cevap,
üniversitenin resmî kaynaklarından derlenmiş 412 kayıtlık bilgi tabanından geliyor; altında kaynak bağlantısı,
sorunun konusu (ör. `exam_and_grading`) ve güven bandı yazıyor.

![EduAI kaynağıyla birlikte cevap veriyor](screenshots/eduai.webp)

**5. Pazar ve görsel arama (3:15).** **Pazar**'da ilanlara göz at, sonra **Görsel Ara**'ya bas ve bir ürün
fotoğrafı yükle. Dosya adında ürünün adı geçerse (ör. `hesap-makinesi.jpg`) demo ürünü tanıyor ve benzer
ilanları puanıyla sıralıyor.

**6. Mesajlar (3:45).** **Sohbet**'ten Burak Şahin'le konuşmayı aç ve bir mesaj gönder. Birkaç saniye içinde
cevap geliyor ve bildirim zilindeki sayı kendiliğinden artıyor. Gerçek sistemde bu iş SignalR ile yapılıyor.

**7. Rol değiştir (4:15).** Demo rozeti → **Rol değiştir** → **Yeni öğrenci**. Takip akışı boş ama sağda takip
önerileri var. Ardından **Moderatör** ile girip başkasının gönderisindeki yorumlarda silme simgesini gör.

## Demo ile gerçek sistem arasındaki farklar

Demo, gerçek API ile aynı sözleşmeyi konuşuyor: aynı adresler, aynı alan adları, aynı hata kodları. Ekranların
kodu iki modda da aynı. Farklar şunlar:

| | Canlı demo | Gerçek sistem |
|---|---|---|
| **Veri** | Örnek veri, tarayıcının `localStorage`'ında | PostgreSQL |
| **EduAI** | Anahtar kelime sınıflandırıcısı ve aynı bilgi tabanı, tarayıcıda | BERTurk modeli, bilgi tabanı ve Gemini |
| **Görsel arama** | Ürün dosya adından tahmin ediliyor, puanlama aynı | Gemini görseli analiz ediyor, ResNet-50 benzerliği ekleniyor |
| **E-posta doğrulama** | Kod ekranda gösteriliyor | Kod kurumsal e-postaya gidiyor |
| **Anlık mesaj ve bildirim** | Tarayıcı içi olay yolu | SignalR hub'ları |
| **Oturum yenileme** | `localStorage` | HttpOnly çerezdeki refresh token |
| **Bazı sistem metinleri** | Türkçe karakterlerle | Bir kısmı Türkçe karaktersiz ("gonderini begendi") |

Tarihler her açılışta bugüne göre hesaplanıyor; demoyu bir hafta sonra açsan da "2 saat önce" paylaşılan gönderi
yeni, gelecek haftaki etkinlik yine gelecek hafta görünüyor.

## Demoyu sıfırlama

Demo rozeti → **Demoyu sıfırla**. Bu tarayıcıda yaptığın her değişiklik silinir ve örnek veriler baştan yüklenir.

## Gerçek sistemi görmek istersen

- [Mimari](03-mimari.md) ve [Çözdüğüm problemler](11-cozdugum-problemler.md): kodun arkasındaki kararlar.
- [Çalıştırma ve API](10-calistirma-ve-api.md): tek komutla PostgreSQL, API ve posta yakalayıcıyı kendi
  makinende çalıştırma.
- Demonun nasıl kurulduğu ve bedeli: [Yayın stratejisi](09-yayin-stratejisi.md).
