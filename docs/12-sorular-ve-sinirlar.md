# Sorular ve sınırlar

Bir mülakatta EduConnect hakkında gelebilecek soruları ve kısa cevaplarımı burada topladım. Ardından projenin
bilinen sınırları ve yol haritası geliyor. Cevapların ayrıntısı bağlantı verdiğim dokümanlarda.

## Olası sorular

**Projeyi bir cümleyle anlatır mısın?**
Üniversite öğrencileri için bir kampüs platformu: akış, topluluklar, etkinlikler, ikinci el pazar, mesajlaşma ve
üniversiteyle ilgili soruları resmî kaynaklara dayanarak cevaplayan bir asistan (EduAI).

**Ekipte sen ne yaptın?**
İki kişilik bir takımdık: takım kaptanı bendim (Fırat Atalay), takım arkadaşım Ayşe Mandıralı'ydı. İşi paylaştık:
backend, yapay zekâ servisi, arayüz ve yayına alma ikimizin ortak işiydi. Kasım 2026'daki düzenlemeyi (demo modu, üç hata
düzeltmesi, bu dokümanlar) ben yaptım.

**Yapay zekâ asistanı kullandın mı?**
Evet. Kodun önemli bir kısmını yapay zekâyla (Claude Code) birlikte, kendi koyduğum kurallar altında yazdım.
Mimariyi, kuralları ve her değişikliğin doğruluğunu biz belirledik. Bu dokümanlardaki her sayıyı ve iddiayı koda
bakarak yazdım. Toplu bir metin düzeltmesinin bir API sözleşmesini nasıl bozduğunu da
[Çözdüğüm problemler](11-cozdugum-problemler.md) dokümanında anlattım.

**Neden mikroservis?**
Tam bir mikroservis mimarisi değil: tek bir API ve ayrı bir yapay zekâ servisi var. Ayırmamızın nedeni bellek oldu.
Modeller Render'ın 512 MB'lık ücretsiz sunucusuna sığmıyordu ([Mimari](03-mimari.md)).

**Clean Architecture diyorsun ama iş kuralları controller'larda. Neden?**
Haklı bir eleştiri. Dört katman var ama kuralların çoğu controller'larda ve controller'lar `AppDbContext`'i doğrudan
kullanıyor. MediatR kayıtlı ama hiç handler yazılmadı. İki kişilik ekipte hızı seçtik. Bugün akış puanlaması ve
topluluk rolleri gibi kuralları test edilebilir uygulama servislerine taşırdım.

**Aynı anda iki beğeni gelirse ne olur?**
`(PostId, UserId)` çifti benzersiz. İkinci istek PostgreSQL'den `23505` alıyor, backend bunu "iş zaten yapılmış" diye
yorumluyor ([Veritabanı](04-veritabani.md)).

**Refresh token'ı nasıl koruyorsun?**
HttpOnly bir çerezde, tek kullanımlık. Her yenilemede eski token tek bir `UPDATE ... WHERE` ile iptal ediliyor;
paralel ikinci istek 401 alıyor. Eksikleri de söylerim: token'lar veritabanında düz metin ve yeniden kullanım
tespit edilince oturum zinciri kapatılmıyor ([Güvenlik](06-guvenlik.md)).

**EduAI yanlış bilgi verirse?**
Cevap ancak bilgi tabanı eşleşmesi güçlüyse "dayanaklı" sayılıyor ve her cevapta niyet, güven bandı ve kaynak
bağlantısı görünüyor. Bedeli de var: eşleşme zayıfsa model genel bilgisiyle yardımcı oluyor ve bu yanlış bilgi
riskini artırıyor ([Yapay zekâ](07-yapay-zeka.md)).

**%96 doğruluk gerçekçi mi?**
Dikkatli okunmalı. Doğrulama seti, eğitim verisiyle aynı dağılımdan, büyük kısmı kurallı biçimde üretilmiş
cümlelerden ayrıldı. Gerçek sorularda başarı daha düşük olabilir. 60 zor vakalık ayrı bir test seti var ama
sonucunu repoya koymadık, o yüzden bir sayı söylemiyorum.

**Gemini ya da NLP servisi düşerse?**
NLP servisi düşerse güvenli bir yedek sonuçla devam ediliyor, Gemini düşerse cevap yerel bilgi tabanından geliyor.
Gemini anahtarı hiç verilmeden de sistem çalışıyor ([Çalıştırma ve API](10-calistirma-ve-api.md)).

**Canlı demo gerçek sistem mi?**
Hayır, tarayıcıda çalışan bir demo. Gerçek API ile aynı sözleşmeyi taşıma katmanında karşılıyor ve ekran kodu iki
modda da aynı. Gerçek sistem tek komutla yerelde çalışıyor ([Yayın stratejisi](09-yayin-stratejisi.md)).

**Neden test yok?**
Tez sürecinde önceliği özelliklere verdik. Bedelini Kasım'da bulduğum üç hatada gördüm; üçü de basit bir sözleşme
ya da duman testiyle ilk gün yakalanırdı. İlk yazacağım testler aşağıdaki yol haritasında.

**Kullanıcı sayısı artarsa ilk ne kırılır?**
Sırasıyla: tarih indeksi olmadığı için akış sorguları; "Sana özel" puanlamasının her istekte 250 adayı bellekte
hesaplaması; görsel aramanın bütün adaylarla kosinüs benzerliği hesaplaması; etkinlik kontenjanındaki yarış. API'yi
birden fazla örnekle çalıştırmak için SignalR'a bir backplane, yüklemelere de ortak bir depolama gerekir.

**Güvenlikte en zayıf nokta ne?**
Giriş uç noktasında hız sınırı ya da hesap kilidi yok; şifre denemeye açık. Hemen arkasından düz metin refresh
token'lar ve istemciye sızan `InvalidOperationException` mesajları geliyor.

**Bugün baştan yazsan neyi farklı yapardın?**
Testlerle başlardım, iş kurallarını servislere koyardım, hata sözleşmesini ilk günden tek tip yapardım. Arayüzü ve
API'yi aynı alan adı altında planlardım, etiketleri ve embedding'leri de ayrı yapılarda tutardım.

## Bilinen sınırlar

| Alan | Sınır | Nasıl kapanır? |
|---|---|---|
| Test | Otomatik test yok | Aşağıdaki yol haritası |
| Kod | MediatR ve FluentValidation kayıtlı ama kullanılmıyor; `PostsController` 1.126, `GroupsController` 757 satır | Kuralları uygulama servislerine taşımak ya da paketleri kaldırmak |
| Veri | Etkinlik kontenjanı aynı anda gelen kayıtlarda aşılabiliyor | Koşullu `INSERT` ya da satır kilidi |
| Veri | `Posts.CreatedAtUtc` üzerinde indeks yok | İndeks eklemek |
| Veri | Embedding'ler metin kolonunda JSON | `pgvector` |
| Güvenlik | Girişte hız sınırı ve hesap kilidi yok | `auth` hız sınırı politikası |
| Güvenlik | Refresh token düz metin; yeniden kullanım tespiti yok | Özetini saklamak, zinciri kapatmak |
| Güvenlik | `InvalidOperationException` mesajı istemciye gidiyor | Alana özel istisna ve genel mesaj |
| Güvenlik | Kayıt cevabı e-postanın kayıtlı olduğunu söylüyor | Her durumda aynı cevap |
| Güvenlik | Doğrulama kodu tuzsuz SHA-256 | Gizli anahtarlı HMAC |
| Güvenlik | Şifre sıfırlama akışı yok | Tek kullanımlık sıfırlama token'ı |
| Güvenlik | NLP servisinde CORS varsayılanı `*` | `ALLOWED_ORIGINS`'i zorunlu yapmak |
| Güvenlik | Refresh çerezi `SameSite=Lax`; arayüz ve API farklı sitelerde | Aynı alan adı ya da `SameSite=None` ile CSRF koruması |
| API | Hata biçimleri tek tip değil: Problem Details, `{ "message": ... }` ve düz metin | Her yerde Problem Details |
| API | Bazı yanıt metinleri Türkçe karaktersiz ("gonderini begendi") | Metinleri düzeltmek |
| Yapay zekâ | Python'daki anahtar kelime yedeği Türkçe karakterleri sadeleştirmiyor | Demodaki sadeleştirmeyi servise taşımak |
| Yapay zekâ | Zor test setinin sonucu repoda yok | Değerlendirmeyi çalıştırıp raporlamak |
| Yapay zekâ | Geri bildirim uç noktası var ama arayüz çağırmıyor; analitik ekranı yok | Beğendim/beğenmedim düğmeleri ve yönetici ekranı |
| Arayüz | Tek parça paket: 2.157 KB (gzip 639 KB); `recharts` kurulu ama kullanılmıyor | Rota bazında bölme, paketi kaldırmak |
| Arayüz | Erişilebilirlik denetimi yapılmadı; 11 `aria-label` | Lighthouse ve axe ile denetim |
| Demo | Demo cevaplarının biçimini derleyici denetlemiyor | Eşleyicileri uygulamanın tiplerine bağlamak |
| Altyapı | Render'da yüklenen dosyalar kalıcı değil | Nesne depolama |
| Altyapı | Ücretsiz veritabanının süresi doluyor; Hugging Face Space uyuyor | Ücretli katman; canlı adreste bugün demo var |

## Yol haritası

Sıra, bir sonraki adımda en çok riski azaltacak işe göre:

1. **Testler.** `AuthService` için refresh rotasyonu ve doğrulama kodu; akış puanlaması ve topluluk rolleri için
   birim testleri; gerçek PostgreSQL ile birkaç entegrasyon testi; arayüzün rol ve durum değerlerini API
   sözleşmesiyle karşılaştıran bir sözleşme testi.
2. **Güvenlik.** Giriş hız sınırı, refresh token özeti ve yeniden kullanım tespiti, HMAC'li doğrulama kodu, genel
   hata mesajları, şifre sıfırlama.
3. **Tek tip hata sözleşmesi.** Bütün uç noktalarda Problem Details.
4. **Uygulama servisleri.** Akış puanlamasını ve topluluk rollerini controller'lardan çıkarmak.
5. **Veri.** `CreatedAtUtc` indeksi, etiket tablosu, kontenjan için koşullu `INSERT`, `pgvector`.
6. **Arayüz.** Rota bazında bölme, EduAI geri bildirim düğmeleri, yönetici analitiği ekranı, erişilebilirlik
   denetimi.
7. **Yayın.** Arayüz ve API için ortak alan adı, nesne depolama ve bütçe olursa gerçek sistemi canlıya almak.
