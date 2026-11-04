# Çözdüğüm problemler

Bu doküman bir mülakatta "zorlandığın bir problemi anlat" sorusuna hazırlık. Her hikâye git geçmişinden ya da
Kasım 2026'daki yeniden okumadan geliyor; yanına commit'ini ve kodun yerini yazdım. Takım olarak çözdüklerimizi
"biz", Kasım'da kendi bulduklarımı "ben" diye anlatıyorum.

| # | Problem | Konu |
|---|---|---|
| 1 | Render'da SQL Server yok | Veritabanı geçişi |
| 2 | Yapay zekâ 512 MB'a sığmadı | Mimari |
| 3 | Doğrulama e-postaları gitmiyordu | Altyapı |
| 4 | Gemini kotası dolunca EduAI susuyordu | Dayanıklılık |
| 5 | "Duyurulardan takip edin" diyen asistan | Yapay zekâ kalitesi |
| 6 | "Biraz daha açıklar mısın?" | Konuşma bağlamı |
| 7 | Aynı anda gelen beğeniler 500 dönüyordu | Eşzamanlılık |
| 8 | Refresh token ve kayıt yarışları | Eşzamanlılık |
| 9 | Sayfalarda tekrar eden gönderiler, belleğe dolan etiket araması | Sorgular |
| 10 | Kullanıcının dosya adı veritabanına yazılıyordu | Güvenlik |
| 11 | Canlı site boş açılıyordu | Yayın |
| 12 | Yeniden okurken bulduğum üç hata | Sözleşmeler |

## 1. Render'da SQL Server yok

**Problem.** Projeye SQL Server ile başladık. Yayına alırken Render'ın ücretsiz katmanında SQL Server olmadığını
gördük; elimizde 11 migration ve SQL Server'da duran veri vardı.

**Çözüm.** Npgsql'e geçtik, 11 migration'ın yerine tek bir `InitialPostgresCreate` ürettik ve veriyi taşımak için
küçük bir konsol aracı yazdık ([tools/MigrateData](../tools/MigrateData/Program.cs)). Araç hedefte migration'ı
uyguluyor, bağlantıyı açık tutup `SET session_replication_role = replica` ile yabancı anahtar tetikleyicilerini
kapatıyor, tabloları bağımlılık sırasına göre kopyalıyor ve sonunda ayarı `origin`'e döndürüyor.

**Neden.** Bu ayar oturuma ait. EF Core her kayıtta havuzdan başka bir bağlantı alsaydı ayar sıfırlanırdı; bu
yüzden bağlantıyı açık tutmak şarttı. Commit: `c6c9305` (26 Mayıs 2026).

Tarihler için de bir karar gerekti. Npgsql 6 ve sonrası `DateTime` değerlerinin türü (`Kind`) konusunda katı.
Geçişi büyütmemek için API'de, migration fabrikasında ve taşıma aracında eski davranışı seçen
`Npgsql.EnableLegacyTimestampBehavior` anahtarını açtık ([Program.cs](../src/EduConnect.Api/Program.cs)).

**Bedeli.** Kopyalama sırasında yabancı anahtar kontrolü yapılmıyor; verinin tutarlılığı kaynağa güvenmeye kalıyor.
Ayar yüksek yetki de istiyor. Zaman damgası anahtarı ise bir geçiş kolaylığı; doğrusu bütün tarihleri UTC olarak
tutup anahtarı kaldırmak.

## 2. Yapay zekâ 512 MB'a sığmadı

**Problem.** BERTurk ve ResNet-50, PyTorch ile birlikte gigabaytlarca bellek istiyor. Render'ın ücretsiz sunucusu
512 MB.

**Çözüm.** Modelleri FastAPI ile yazılmış ayrı bir servise taşıdık ve Hugging Face Spaces'te çalıştırdık. Docker
imajında yalnızca CPU sürümlü PyTorch var ([nlp-service/Dockerfile](../nlp-service/Dockerfile)).

**Neden ve bedeli.** API hafif kaldı, model API'den bağımsız güncellenebiliyor. Bedeli her soruda bir ağ çağrısı
ve uyuyan bir Space. Servise ulaşılamazsa backend güvenli bir yedek sonuçla devam ediyor
([NlpApiService](../src/EduConnect.Infrastructure/Services/NlpApiService.cs)). Ayrıntısı [Mimari](03-mimari.md)
dokümanının sonunda.

## 3. Doğrulama e-postaları gitmiyordu

**Problem.** Kayıt, kurumsal e-postaya giden 6 haneli koda bağlı. Render'a taşıdıktan sonra kodlar gitmedi: Render'ın
ücretsiz katmanı SMTP portlarını (25, 465, 587) kapatıyor.

**Çözüm.** Brevo'nun HTTPS API'sini ekledik. Servis önce Brevo anahtarına bakıyor; anahtar yoksa SMTP'ye düşüyor
([EmailService](../src/EduConnect.Infrastructure/Services/EmailService.cs)). Commit: `ec00784`.

**Neden.** HTTPS her yerde açık ve SMTP yolu da kaybolmadı: yerel kurulumda e-postalar SMTP ile Mailpit'e gidiyor.

**Bedeli.** Bir dış servise daha bağımlıyız ve ücretsiz planın bir günlük gönderim sınırı var.

## 4. Gemini kotası dolunca EduAI susuyordu

**Problem.** Gemini'nin ücretsiz kotası dolunca cevap üretme çağrısı hata veriyor ve kullanıcı cevap alamıyordu.

**Çözüm.** Gemini çağrısını bir `try/catch` içine aldık. Hata olursa cevap yerel olarak kuruluyor
(`BuildLocalResponse`): bilgi tabanı eşleşmesi güvenilirse kaydın cevabı ve kaynağı, değilse "Tam emin değilim
ama..." diye başlayan yakın bir cevap, hiçbiri yoksa kısa bir özür
([ChatbotService](../src/EduConnect.Infrastructure/Services/ChatbotService.cs)). Commit: `a68690b` (30 Nisan 2026).

**Neden.** Kullanıcı boş ekran görmemeli. Cevabın nereden geldiği de saklanmıyor: model etiketi `+local-kb` oluyor.

## 5. "Duyurulardan takip edin" diyen asistan

**Problem.** Bilgi tabanındaki bazı kayıtlar somut bilgi yerine "ilgili birimin duyurularını takip ediniz" diyordu.
Asistan bunu aynen tekrarlıyordu; bazı konularda ise hiç kayıt yoktu.

**Çözüm.** İki adım attık:

1. **Yönlendirme dedektörü.** 300 karakterden kısa ve bu tür ifadeler içeren bir kayıt "yönlendirme" sayılıyor.
   Gemini'den kaydı tekrarlamak yerine somut bir cevap vermesi, bağlantıyı yalnızca kaynak olarak eklemesi
   isteniyor (`IsDeflectingKbAnswer`, commit `cb5b9cb`).
2. **Resmî kaynaklar.** Öğrenci El Kitabı, Burs Yönergesi, Ders Kayıt Kılavuzu, YKS kayıt belgeleri ve faaliyet
   raporundan yeni kayıtlar derledik. Bilgi tabanı bir günde 225 kayıttan 412 kayda çıktı:
   70 + 32 + 16 + 25 + 25 + 19 ([knowledge_base.json](../nlp-service/data/knowledge_base.json)).

**Neden.** Asıl sorun model değil, verinin kendisiydi. "Daha iyi istem" yazmak eksik bilgiyi tamamlamıyor.

## 6. "Biraz daha açıklar mısın?"

**Problem.** Takip sorusu tek başına sınıflandırılınca anlamsız bir niyet çıkıyordu. Gemini de önceki cevabı
tekrarlıyordu. Gemini'ye giden geçmiş yalnızca 10 mesajdı.

**Çözüm.** Mesaj tipik bir takip ifadesi içeriyorsa ya da 25 karakterden kısaysa ve oturumda geçmiş varsa
(`LooksLikeFollowUp`), sınıflandırıcıya son kullanıcı sorusuyla birlikte gönderiliyor. Gemini'ye önceki cevabı
tekrarlamaması söyleniyor ve geçmiş penceresi 20 mesaja çıktı
([GeminiApiService](../src/EduConnect.Infrastructure/Services/GeminiApiService.cs)). Commit: `7051df8`.

**Bedeli.** Kısa ama bağımsız bir soru ("Kütüphane saatleri?") da takip sorusu sanılıp önceki soruyla
birleştirilebiliyor.

## 7. Aynı anda gelen beğeniler 500 dönüyordu

**Problem.** Bir kullanıcı-gönderi çifti için tek beğeniyi benzersiz indeks koruyor. Ama aynı anda gelen iki
istekten ikincisi PostgreSQL'den `23505` alıyor ve API 500 dönüyordu. Kaydetme, görüntülenme ve takipte de aynısı
vardı.

**Çözüm.** `IsUniqueViolation()` uzantısı bu hatayı tanıyor
([DbUpdateExceptionExtensions](../src/EduConnect.Infrastructure/Extensions/DbUpdateExceptionExtensions.cs)); ilgili
uç noktalar onu "iş zaten yapılmış" diye yorumluyor
([PostsController](../src/EduConnect.Api/Controllers/PostsController.cs),
[UsersController](../src/EduConnect.Api/Controllers/UsersController.cs)). Commit: `7051df8`.

**Neden.** "Önce kontrol et, sonra ekle" iki isteğin aynı anda kontrolü geçmesini engellemiyor. Doğruluğu
veritabanı garanti ediyor, uygulama yalnızca sonucu doğru yorumluyor. Beğeni sayısı da ayrı bir kolonda değil,
satırlardan sayılıyor; bu yüzden şişemiyor.

## 8. Refresh token ve kayıt yarışları

**Problem.** İki sekme aynı refresh token'ı aynı anda kullanınca ikisi de token'ı geçerli görebiliyordu. Aynı
e-postayla gelen iki paralel kayıttan biri ise 500 alıyordu.

**Çözüm.**

- **Refresh:** Token tek bir `UPDATE ... WHERE` ile, yani `ExecuteUpdateAsync` ile iptal ediliyor. Satırı yalnızca bir
  istek değiştirebiliyor, ikincisi temiz bir 401 alıyor. Kodu [Güvenlik](06-guvenlik.md) dokümanında.
- **Kayıt:** Çakışmada yeniden deneme. `23505` gelirse kayıt bir kez daha deneniyor ve ikinci denemede "mevcut
  kullanıcı" yoluna giriyor ([AuthService](../src/EduConnect.Infrastructure/Services/AuthService.cs)).

Commit: `e1fa5f8` (27 Mayıs 2026). Rotasyonu yerelde denedim: eski çerezle yapılan istek
`{"title":"Unauthorized","status":401,"detail":"Oturum yenilenemedi."}` dönüyor.

## 9. Sayfalarda tekrar eden gönderiler, belleğe dolan etiket araması

**Problem 1.** Kaydedilenler ve beğenilenler listesi zamana göre sıralıydı. Aynı saniyede kaydedilen iki gönderinin
sırası her sorguda değişebildiği için sayfa sınırında bir gönderi iki sayfada görünüyor ya da kayboluyordu.

**Çözüm.** İkinci bir sıralama anahtarı: `ThenByDescending(p => p.Id)`. Sıralama artık kararlı (commit `7051df8`).

**Problem 2.** Etiket araması bütün gönderileri belleğe yükleyip etiketleri C# içinde ayrıştırıyordu.

**Çözüm.** Önce PostgreSQL'de `ILIKE '%#etiket%'` ile adayları daraltıyoruz, en yeni 500 adayla sınırlıyoruz, tam
etiket kontrolünü sonra C#'ta yapıyoruz (commit `e1fa5f8`).

**Bedeli.** Çok popüler bir etiketin 500'den eski eşleşmeleri sonuçlara girmiyor. Gerçek çözüm etiketleri ayrı bir
tabloda tutmak.

## 10. Kullanıcının dosya adı veritabanına yazılıyordu

**Problem.** Görsel arama geçmişine, kullanıcının yüklediği dosyanın adı olduğu gibi yazılıyordu. Dosya adı tamamen
kullanıcının kontrolünde; yola ya da log satırına girdiğinde risk oluşturuyor. Yükleme doğrulaması da uç noktadan uç
noktaya farklıydı.

**Çözüm.**

- Geçmişte artık `uploaded:<uuid>.<uzantı>` biçiminde temizlenmiş bir anahtar duruyor
  ([VisualSearchController](../src/EduConnect.Api/Controllers/VisualSearchController.cs)).
- Ortak bir doğrulama yazdık: boyut ve dosya türü listesi
  ([FileUploadValidation](../src/EduConnect.Api/Common/FileUploadValidation.cs)). Gönderi, avatar, kapak ve topluluk
  görseli yükleyen altı uç noktada bunu ve `RequestSizeLimit` kullanıyoruz.

Commit: `e1fa5f8`. Dosya türünün ilk baytlardan anlaşılması da [Güvenlik](06-guvenlik.md) dokümanında.

## 11. Canlı site boş açılıyordu

**Problem.** Kasım 2026'da canlı adrese girdiğimde giriş yapılamıyordu; Render'daki ücretsiz veritabanının süresi
dolmuştu.

**Çözüm.** Arayüzü backend'siz çalışabilen bir demo moduyla yayına aldım. axios adaptörü değişiyor, ekranlar aynı
kalıyor, demo kodu gerçek derlemeye hiç girmiyor. Ayrıntısı ve bedeli [Yayın stratejisi](09-yayin-stratejisi.md)
dokümanında.

## 12. Yeniden okurken bulduğum üç hata

Kasım 2026'da kodu baştan okudum ve sistemi yerelde, Gemini anahtarı vermeden çalıştırdım. Üç hata çıktı:

| Hata | Nasıl fark ettim? | Düzeltme |
|---|---|---|
| 18 Nisan'daki toplu Türkçe yazım düzeltmesi (commit `209e40d`) arayüzdeki rol değerini `moderator`'dan `moderatör`'e çevirmişti. API `moderator` döndüğü için moderatör listesi boş geliyordu | Arayüzdeki rol değerlerini API'nin enum'larıyla karşılaştırınca | 15 dosyada sözleşmeye dönüş (frontend `6e43f05`) |
| Hub'lar token'ı `access_token` sorgu parametresinden okuyor. Bu kod `/hubs/chat` ve `/hubs/direct-messages` yollarını kapsıyor, `/hubs/notifications` yolunu kapsamıyordu. Anlık bildirimler hiç gelmiyor, dakikada bir yoklamayla geliyordu | [Program.cs](../src/EduConnect.Api/Program.cs) içindeki yol listesini hub listesiyle karşılaştırınca | Bildirim hub'ı listeye eklendi (`bfef3aa`) |
| Gemini anahtarı boşken `GeminiApiService` kurucusu fırlatıyordu. Servis oluşturulamadığı için EduAI ve görsel arama 500 dönüyordu, yedek yollar hiç çalışmıyordu | Yerel kurulumu anahtarsız çalıştırınca | İstemci boş kalıyor, hata çağrının içinde oluşuyor ve yedek yol devreye giriyor (`79c4fba`) |

**Öğrendiğim.** Üç hatanın ortak noktası, iki parçanın arasındaki sözleşmenin kimse bakmadan kayması. Arayüzle API
arasında bir sözleşme testi, hub'lar için bir bağlantı testi ya da anahtarsız bir duman testi bunları ilk gün
yakalardı. Projede otomatik test yok; sırada olduğunu [Sorular ve sınırlar](12-sorular-ve-sinirlar.md) dokümanında
yazdım.
