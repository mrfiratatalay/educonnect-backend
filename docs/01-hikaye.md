# Hikâye

EduConnect, üniversite öğrencileri için yapay zekâ destekli bir kampüs platformu: sosyal akış, topluluklar,
etkinlikler, ikinci el pazar, birebir mesajlaşma ve üniversiteyle ilgili soruları kaynağıyla cevaplayan bir
asistan (EduAI) tek bir yerde. Recep Tayyip Erdoğan Üniversitesi (RTEÜ) Bilgisayar Mühendisliği bölümündeki
bitirme tezimiz olarak geliştirdik; proje TÜBİTAK 2209-A Üniversite Öğrencileri Araştırma Projeleri Destekleme
Programı kapsamında desteklendi.

## Neden bu proje?

Üniversiteye başladığımız dönemde kampüsteki hayatı yakalamak zordu. Hangi etkinliğin nerede olduğu, hangi
toplulukların var olduğu, kullanmadığımız kitabı kime verebileceğimiz ya da bütünleme sınavına kimlerin
girebileceği gibi bilgiler farklı yerlere dağılmıştı. WhatsApp grupları çok dağınıktı, genel sosyal medya
üniversite bağlamından kopuktu, resmî duyurular ise uzun PDF'lerin içinde kayboluyordu.

EduConnect'i bu ihtiyaçları tek noktada toplamak için tasarladık. İlk hedef kitle RTEÜ öğrencileri; ama
üniversiteler bir tablo olarak tutulduğu için platform başka üniversitelere de açılabiliyor. Kayıt olmak için
seçilen üniversitenin kurumsal e-posta adresi gerekiyor; bu yüzden platformdaki herkes gerçekten bir öğrenci.

## Ekip

Projeyi **Ayşe Mandıralı** ile birlikte geliştirdik. Sorumlulukları baştan sona paylaştık: tasarım kararları,
backend, yapay zekâ servisi, arayüz ve yayına alma süreci ikimizin ortak işi oldu. Kaptan ya da tek bir sahip
yoktu; zorlandığımız her noktadan birlikte geçtik. Bu dokümanlarda takım olarak verdiğimiz kararları "biz",
kendi gözlemlerimi ve bu son düzenlemeyi "ben" diye anlatıyorum.

Kodun önemli bir kısmını yapay zekâ kodlama asistanlarıyla (Claude Code) birlikte yazdım. Repo geçmişinde bu
asistanlar için tuttuğumuz çalışma kuralı dosyaları da duruyor. Mimariyi, kuralları ve her değişikliğin
doğruluğunu biz belirledik; bu dokümanlardaki her iddiayı da koda bakarak yazdım.

## Zaman çizelgesi

Tarihler iki reponun git geçmişinden.

| Tarih | Ne oldu? |
|---|---|
| 24 Mart 2026 | Backend ve frontend repolarının ilk commit'leri |
| Nisan 2026 | Modüller, Gemini entegrasyonu, topluluk görselleri; 18 Nisan'da arayüzün "derin turkuaz" yeniden tasarımı |
| 30 Nisan 2026 | Gemini kotası dolunca yerel bilgi tabanına düşme, modelin Git LFS'e taşınması, Windows başlatma betikleri |
| 26 Mayıs 2026 | SQL Server'dan PostgreSQL'e geçiş, Docker ve Render, e-posta için Brevo, TÜBİTAK sunumu için README |
| 27 Mayıs 2026 | Eşzamanlılık ve güvenlik düzeltmeleri; resmî PDF'lerden bilgi tabanının 225 kayıttan 412 kayda çıkması |
| 4 Haziran 2026 | İlk dokümanlar |
| Kasım 2026 | Bu düzenleme: tarayıcıda çalışan canlı demo, koddan doğrulanmış dokümanlar, üç hata düzeltmesi |

## Benimsediğimiz ilkeler

Bunları bir yere yazıp asmadık; ama koda bakınca her birinin izi açıkça görülüyor.

1. **Her dış servis düşebilir; kullanıcı boş cevap görmemeli.** NLP servisi düşerse backend güvenli bir yedek
   sonuçla devam ediyor, Gemini düşerse cevap yerel bilgi tabanından geliyor. Ayrıntısı
   [Yapay zekâ](07-yapay-zeka.md) dokümanında.
2. **Sırlar repoda durmaz.** `appsettings.json` içinde JWT anahtarı ve Gemini anahtarı boş; gerçek değerler
   ortam değişkenlerinden geliyor ([appsettings.json](../src/EduConnect.Api/appsettings.json)).
3. **Ücretsiz katmanların içinde kal.** Render'ın 512 MB'lık ücretsiz sunucusu, yapay zekâ modellerini ayrı bir
   servise taşımamızın asıl nedeni oldu ([Yayın stratejisi](09-yayin-stratejisi.md)).
4. **Yapay zekânın cevabı şeffaf olmalı.** EduAI her cevabın yanında tespit edilen niyeti, güven bandını, bilgi
   tabanı eşleşmesini ve kaynak bağlantısını gösteriyor.
5. **Kod İngilizce, kullanıcının gördüğü her şey Türkçe.**

## Önce ve sonra

Projeyi Mayıs 2026'da teslim ettik. Kasım 2026'da portfolyom için baştan sona yeniden okudum; bu tablo o
okumanın sonucu.

| | Teslimde (Mayıs 2026) | Bu düzenlemeden sonra |
|---|---|---|
| **Canlı site** | Render'daki veritabanına bağlıydı. Ücretsiz veritabanının süresi dolunca giriş bile yapılamadı | Tarayıcıda çalışan demo: her rolle tek tıkla, şifresiz giriş |
| **Yerel kurulum** | Windows betikleri; README'nin anlattığı compose dosyası repoda yoktu | `docker compose up --build` |
| **Gemini anahtarı yoksa** | EduAI ve görsel arama 500 hatası veriyordu | Cevap yerel bilgi tabanından geliyor |
| **Topluluk moderatörleri** | Toplu Türkçe düzeltmesi rol değerini bozmuştu, moderatör listesi boş geliyordu | API sözleşmesiyle uyumlu |
| **Canlı bildirimler** | Bildirim hub'ı token'ı okumuyordu; bildirimler dakikada bir yoklamayla geliyordu | Hub token'ı okuyor |
| **Dokümanlar** | Olmayan controller'ları ve 15 dakikalık token'ı anlatan kısa notlar | Koddan doğrulanmış 12 doküman, Türkçe ve İngilizce |
| **Repo** | Yapay zekâya yazılmış kurulum notu, veritabanı yedeği, ölü kod | Temizlendi |

Son üç hatanın nasıl bulunduğunu [Çözdüğüm problemler](11-cozdugum-problemler.md) dokümanının sonunda anlattım.

## Bu projeden öğrendiklerim

- **Ücretsiz altyapı, mimariyi de şekillendiriyor.** RAM sınırı, kapalı SMTP portları ve süresi dolan
  veritabanı; her biri bir tasarım kararına dönüştü.
- **Yapay zekâ özelliğini değerli yapan model değil, etrafındaki sistem.** Asıl iş bilgi tabanını resmî
  kaynaklardan beslemek, güven eşiklerini ayarlamak ve servis düştüğünde ne olacağını tasarlamaktı.
- **Eşzamanlılık, iki kişinin aynı anda tıklamasıyla başlıyor.** Beğeni, refresh token ve kayıt akışındaki yarış
  durumları bize veritabanı kısıtlarının değerini öğretti.
- **Test olmadan büyük bir değişiklik yapmak pahalı.** Projede otomatik test yok; bunu [Sorular ve
  sınırlar](12-sorular-ve-sinirlar.md) dokümanında dürüstçe yazdım.
