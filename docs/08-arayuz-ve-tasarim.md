# Arayüz ve tasarım

Arayüz ayrı bir repoda duruyor: [educonnect-frontend](https://github.com/mrfiratatalay/educonnect-frontend).
React 19, TypeScript ve Vite 8 ile yazdık; bileşen kütüphanesi Ant Design 6. Bu dokümanda arayüzün nasıl
kurulduğunu, sunucu verisini nasıl yönettiğimizi, oturumu, tasarım dilini ve mobil tarafı anlatıyorum.

![Kişiselleştirilmiş akış, masaüstü](screenshots/feed.webp)

## Teknolojiler

| İş | Seçim | Neden? |
|---|---|---|
| Arayüz | React 19.2, TypeScript 5.9 | Backend sözleşmelerini tiplerle karşılamak |
| Derleme | Vite 8 | Hızlı geliştirme sunucusu; `--mode demo` ile ikinci bir derleme |
| Bileşenler | Ant Design 6.3, lucide-react | Form, çekmece, sekme gibi hazır bileşenler; bütün temayı tek dosyadan yönetmek |
| Sunucu verisi | TanStack Query 5.95 | Önbellek, yükleniyor durumu, sonsuz kaydırma, iyimser güncelleme |
| Uygulama durumu | Zustand 5 | Oturum, tema, menü gibi az sayıdaki küresel durum için küçük depolar |
| Yönlendirme | React Router 6.30 | İç içe düzenler, korumalı rotalar |
| HTTP ve anlık iletişim | axios 1.13, @microsoft/signalr 10 | Taşıma katmanını tek yerden değiştirebilmek; demo bunun üzerine kurulu |
| Formlar | React Hook Form 7.72, zod 4.3 | Form durumu ile doğrulama şeması ayrı |
| EduAI cevapları | react-markdown, remark-gfm | Cevaplar Markdown olarak geliyor |

`recharts` kurulu ama hiçbir ekranda kullanılmıyor. Yönetici analitiği ekranı için eklenmişti; o ekranı yazmadık.

## Klasör yapısı

```
src/
├── pages/        22 sayfa dosyası; 23 rota App.tsx içinde
├── features/     11 modül; her birinde istekler (api.ts), çoğunda sorgu hook'ları (hooks.ts)
├── components/   uygulama kabuğu (menü, üst bar, mobil gezinme), EduAI paneli, ortak bileşenler
├── store/        5 Zustand deposu: oturum, tema, arayüz, sohbet, gönderi yazma
├── lib/          apiClient.ts, realtime.ts, signalrLifecycle.ts
├── theme/        antdTheme.ts: bütün renkler ve ölçüler
└── demo/         tarayıcıda çalışan demo backend'i; yalnızca demo derlemesine giriyor
```

Modüller backend'in controller'larını izliyor: `posts`, `groups`, `events`, `products`, `messages`,
`notifications`, `chat` gibi. Bir ekran veriyi doğrudan axios ile değil, ilgili modülün hook'u üzerinden alıyor.
Arayüz toplam 82 farklı uç nokta çağırıyor.

## Sunucu durumu

**Problem.** 82 uç noktanın her biri için yükleniyor durumunu, önbelleği ve "bu veri ne zaman tazelenmeli?"
sorusunu elle yönetmek hataya açıktı.

**Çözüm.** Sunucudan gelen her veri TanStack Query'de duruyor. Varsayılanlar
[main.tsx](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/src/main.tsx) içinde: veri 5 dakika taze
sayılıyor, başarısız bir sorgu bir kez yeniden deneniyor.

| Desen | Nerede? |
|---|---|
| Sonsuz kaydırma (`useInfiniteQuery`) | Akışlar, topluluk gönderileri, mesajlar |
| Periyodik yenileme | Mesajlar 30 saniyede, bildirimler 60 saniyede bir; SignalR'ın yanında bir güvenlik ağı |
| İyimser güncelleme | Beğeni, kaydetme, takip, gönderi paylaşma, mesaj gönderme |

**İyimser beğeni.** Beğeni düğmesine basınca sayı sunucuyu beklemeden değişiyor
([posts/hooks.ts](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/src/features/posts/hooks.ts),
`useTogglePostLikeMutation`):

1. Gönderi sorguları durduruluyor ve önbelleğin anlık bir kopyası alınıyor.
2. Gönderi; akış sayfalarında, detay sayfasında ve topluluk akışında aynı anda güncelleniyor.
3. İstek hata verirse kopya geri yükleniyor. Her durumda ilgili sorgular sunucudan yeniden çekiliyor.

**Neden.** Kullanıcı tıklamanın sonucunu hemen görüyor, doğru sayı yine sunucudan geliyor. Backend'deki
benzersiz indeks de iki hızlı tıklamanın sayacı şişirmesini engelliyor ([Veritabanı](04-veritabani.md)).

**Bedeli.** `updatePostAcrossCaches` bir gönderinin bulunabileceği her önbellek biçimini bilmek zorunda. Yeni bir
gönderi listesi eklenirse oraya da eklenmeli; yoksa o listede eski sayı kalır.

## Oturum

- **Access token yalnızca bellekte.**
  [token.ts](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/src/features/auth/token.ts) tek bir modül
  değişkeni tutuyor; token `localStorage`'a hiç yazılmıyor. Böylece tarayıcı deposundan uzun ömürlü bir token
  okunamıyor.
- **Açılışta oturumu geri getirmek.** Uygulama açılınca
  [authStore.ts](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/src/store/authStore.ts)
  `POST /api/auth/refresh` çağırıyor; HttpOnly çerez geçerliyse yeni bir token geliyor. Bu sırada korumalı rotalar
  kullanıcıyı giriş sayfasına atmıyor, yükleniyor göstergesi gösteriyor.
- **Tek refresh.** Süresi dolmuş birkaç istek aynı anda 401 alırsa tek bir yenileme isteğini paylaşıyor
  ([authenticatedRequest.ts](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/src/features/auth/authenticatedRequest.ts)).
  Backend'de refresh token tek kullanımlık; iki paralel yenileme isteği olsaydı biri 401 alıp oturumu düşürürdü.
- **Temiz çıkış.** Çıkışta açık SignalR bağlantıları önce kapatılıyor
  ([signalrLifecycle.ts](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/src/lib/signalrLifecycle.ts));
  yoksa eski token'la yeniden bağlanmayı deneyebiliyorlardı.

**Bedeli.** Token bellekte durduğu için her sayfa yenilemesi bir refresh isteği demek. Yayındaki çerez sorununu
[Güvenlik](06-guvenlik.md) dokümanının sonundaki tabloya yazdım: arayüz ve API farklı sitelerde olduğu için
`SameSite=Lax` çerez gönderilmiyor ve sayfa yenilenince oturum düşüyordu.

## Taşıma katmanı tek yerde

Bütün modüller HTTP istemcisini
[apiClient.ts](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/src/lib/apiClient.ts)'den, anlık
bağlantıyı [realtime.ts](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/src/lib/realtime.ts)'ten
alıyor:

```ts
export function createApiClient() {
  const client = axios.create({ baseURL: API_BASE_URL, withCredentials: true });
  if (__DEMO_MODE__) {
    client.defaults.adapter = async (config) => {
      const { handleDemoRequest } = await import("@/demo/adapter");
      return handleDemoRequest(config);
    };
  }
  return client;
}
```

Gerçek modda SignalR yalnızca WebSocket kullanıyor, token'ı `accessTokenFactory` ile veriyor ve bağlantı koparsa
0, 2, 5, 10 ve 30 saniye sonra yeniden deniyor. Demo modunda aynı arayüz tarayıcı içindeki bir olay yolundan
besleniyor. Ekranlar hangi modda çalıştıklarını bilmiyor. Demonun bütün tasarımı bu iki dosyaya dayanıyor;
ayrıntısı [Yayın stratejisi](09-yayin-stratejisi.md) dokümanında.

## Tasarım dili

18 Nisan 2026'da arayüzü "derin turkuaz" adını verdiğimiz bir dille yeniden tasarladık. Bütün renkler ve ölçüler
tek dosyada: [antdTheme.ts](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/src/theme/antdTheme.ts).

| Öğe | Değer |
|---|---|
| Ana renk | `#0D9488` (turkuaz); bağlantılar ve seçili menü `#0F766E` |
| Yazı tipi | Manrope; yüklenemezse Inter |
| Köşeler | Genel 10 px, kartlar ve pencereler 14 px |
| Yükseklikler | Düğme 38 px, giriş alanı 42 px, menü öğesi 48 px |
| Açık tema | Arduvaz tonları: zemin `#F1F5F9`, kartlar beyaz |
| Koyu tema | Siyah değil, koyu lacivert: zemin `#0F172A`, kartlar `#1E293B` |

Bu belirteçler Ant Design'ın açık ve koyu tema algoritmalarına veriliyor. Kullanıcının tercihi `localStorage`'da
saklanıyor ([themeStore.ts](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/src/store/themeStore.ts)).
Ant Design'ın CSS değişkenleri `edu` önekiyle üretiliyor ve sınıf adları karıştırılmıyor; tarayıcının geliştirici
araçlarında incelemek kolay.

![Koyu tema](screenshots/dark.webp)

Kod İngilizce; menüler, boş durumlar ve hata mesajları Türkçe.

## Mobil

Öğrencilerin çoğunun platformu telefondan açacağını düşündüğümüz için her ekranı dar görünümde de ele aldık.
28 bileşen ve sayfa ekran genişliğini Ant Design'ın `Grid.useBreakpoint` kancasıyla okuyor; `index.css` içinde de
767 px altı için ayrı kurallar var.

- Masaüstündeki sol menü, telefonda alt gezinme çubuğuna ve açılır bir çekmeceye dönüşüyor
  ([MobileNav.tsx](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/src/components/layout/MobileNav.tsx)).
- Gönderi yazma düğmesi telefonda yüzen bir düğmeye dönüşüyor.
- EduAI paneli ve sohbet balonları dar ekrana göre yerleşimini değiştiriyor.

| Akış | Topluluk | EduAI |
|---|---|---|
| ![Telefonda akış](screenshots/mobile-feed.webp) | ![Telefonda topluluk](screenshots/mobile-community.webp) | ![Telefonda EduAI](screenshots/mobile-eduai.webp) |

## Erişilebilirlik ve hata durumu

Erişilebilirlik bu projede güçlü olmadığımız bir alan. Ant Design bileşenleri klavye ve odak desteğini kendisi
sağlıyor. Ama kendi yazdığımız simge düğmelerinde yalnızca 11 `aria-label` var ve bir erişilebilirlik denetimi
(Lighthouse ya da axe) yapmadık.

Beklenmeyen bir arayüz hatası bütün uygulamayı beyaz ekrana çevirmesin diye en dışta bir `ErrorBoundary` var.
API hatalarında kullanıcıya Problem Details'in `detail` alanı ya da iş kuralının `message` alanı gösteriliyor;
hata sözleşmesinin tamamı [Güvenlik](06-guvenlik.md) dokümanında.

## Paket boyutu

**Durum.** Bütün sayfalar `App.tsx` içinde doğrudan içe aktarılıyor; rotalar tembel yüklenmiyor. Gerçek derlemede
ana paket tek parça: 2.157 KB, gzip ile 639 KB.

**Neden böyle kaldı?** Tez sürecinde önceliğimiz özellikleri bitirmekti; ilk açılış süresini hiç ölçmedik.

**Nasıl düzelir?** `React.lazy` ile rota bazında bölme, Ant Design ve Markdown kütüphanelerini ayrı parçalara
ayırma ve kullanılmayan `recharts` paketini kaldırma.

Demo kodu bu paketin içinde değil. `__DEMO_MODE__` derleme sırasında sabit bir değere dönüşüyor
([vite.config.ts](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/vite.config.ts)). Gerçek
derlemede değeri `false` olduğu için demo dalları ve dinamik içe aktarmaları paketten tamamen çıkıyor. Bunu
derlenmiş dosyalarda arama yaparak doğruladım.

## Bu tarafta öğrendiklerim

- Sunucu durumunu arayüz durumundan ayırınca küresel durum ihtiyacı büyük ölçüde kayboluyor: 5 küçük Zustand
  deposu yetti.
- İyimser güncelleme kolay görünüyor. Asıl zor olan, hatada geri almayı ve aynı verinin önbellekteki bütün
  kopyalarını düşünmek.
- Taşıma katmanını tek dosyada toplamak, aylar sonra demo modunu ekranlara dokunmadan kurmamı sağladı.
