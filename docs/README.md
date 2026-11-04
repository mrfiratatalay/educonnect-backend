# EduConnect dokümanları

Bu klasörde EduConnect'i kendi anlatımımla, ayrıntısıyla açıklıyorum: projenin nasıl doğduğunu, hangi problemi
nasıl çözdüğümüzü ve her teknik kararın gerekçesini. EduConnect, Recep Tayyip Erdoğan Üniversitesi Bilgisayar
Mühendisliği bölümünde Ayşe Mandıralı ile birlikte geliştirdiğimiz, TÜBİTAK 2209-A destekli bitirme projemiz.

Her kararın yanına bedelini de yazdım. Bence bir kararı anlatmanın yarısı, neyden vazgeçtiğini söyleyebilmek.

## Nereden başlamalı?

| Kimsin? | Önerdiğim sıra |
|---|---|
| Projeye ilk kez bakıyorsun, teknik ayrıntı istemiyorsun | [Hikâye](01-hikaye.md) → [Demo turu](02-demo-turu.md) |
| Yazılımcısın, mimariyi ve kodu merak ediyorsun | [Hikâye](01-hikaye.md) → [Mimari](03-mimari.md) → [Yapay zekâ](07-yapay-zeka.md) → [Çözdüğüm problemler](11-cozdugum-problemler.md) |
| Mülakattan önce hızlıca tekrar edeceksin | [Çözdüğüm problemler](11-cozdugum-problemler.md) → [Sorular ve sınırlar](12-sorular-ve-sinirlar.md) |
| Projeyi kendi makinende çalıştıracaksın | [Çalıştırma ve API](10-calistirma-ve-api.md) |

## Bütün dokümanlar

| # | Doküman | İçinde ne var |
|---|---|---|
| 01 | [Hikâye](01-hikaye.md) | Projenin doğuşu, ekip, zaman çizelgesi, benimsediğimiz ilkeler, önce ve sonra |
| 02 | [Demo turu](02-demo-turu.md) | Teknik bilgi gerektirmeyen 5 dakikalık tur, roller, demo ile gerçek sistemin farkları |
| 03 | [Mimari](03-mimari.md) | Üç servisli yapı, katmanlar, modüller, bir isteğin yolculuğu, gerçek zamanlılık |
| 04 | [Veritabanı](04-veritabani.md) | 27 tablo, ilişki şeması, veritabanında korunan kurallar, PostgreSQL'e geçiş |
| 05 | [İş kuralları](05-is-kurallari.md) | Kayıt, akış puanlaması, trendler, topluluk rolleri, etkinlik ve pazar kuralları |
| 06 | [Güvenlik](06-guvenlik.md) | JWT ve refresh rotasyonu, doğrulama kodu, yetki, hız sınırı, hata sözleşmesi, bilinen eksikler |
| 07 | [Yapay zekâ](07-yapay-zeka.md) | EduAI: BERTurk, bilgi tabanı, Gemini ve yedek yollar; ResNet-50 ile görsel arama |
| 08 | [Arayüz ve tasarım](08-arayuz-ve-tasarim.md) | React mimarisi, sunucu durumu, oturum, tasarım dili, mobil |
| 09 | [Yayın stratejisi](09-yayin-stratejisi.md) | Sıfır maliyetli yayın, iki kanal, tarayıcıda çalışan demo ve bedeli |
| 10 | [Çalıştırma ve API](10-calistirma-ve-api.md) | Tek komutla kurulum, ortam değişkenleri, gerçek çıktılarla curl örnekleri |
| 11 | [Çözdüğüm problemler](11-cozdugum-problemler.md) | Problem, çözüm ve gerekçe biçiminde on iki kısa hikâye |
| 12 | [Sorular ve sınırlar](12-sorular-ve-sinirlar.md) | Olası mülakat soruları, bilinen sınırlar ve yol haritası |

## Okurken bilmen gerekenler

- Backend ve yapay zekâ servisi bu repoda; arayüz
  [educonnect-frontend](https://github.com/mrfiratatalay/educonnect-frontend) reposunda. Bağlantılar doğrudan
  ilgili dosyaya gider; bir iddiayı okuduğunda kodunu da açıp bakabilesin istedim.
- Kod İngilizce; arayüz, bilgi tabanı ve bu dokümanlar Türkçe. Backend'in bazı yanıt metinleri Türkçe karaktersiz
  yazılmış ("gonderini begendi" gibi); örneklerde olduğu gibi bıraktım.
- Dokümanlardaki sayıları koddan saydım: 89 controller uç noktası, 27 tablo, 412 bilgi tabanı kaydı gibi.
- Bu klasördeki hiçbir doküman 300 satırı geçmiyor.
- English: [en/README.md](en/README.md)
