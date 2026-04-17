# EduAI — Bilinen Sınırlar ve Riskler

> Bu doküman, EduAI chatbot sisteminin mevcut sınırlarını ve bilinen risklerini
> tez raporu ve savunma hazırlığı için açıkça belgelemektedir.

---

## 1. Model Performansı

### BERTurk Fine-tuning ve Overfitting Sınırı
- **Veri seti boyutu:** Sentetik veri artırımı (Data Augmentation) ile genişletilmiş ~400 örnek (10 intent kategorisi)
- **Validation accuracy:** 0.3625 (≈ %36.2)
- **Sonuç:** Minimum eşik 0.60 karşılanamadığı için model runtime'da devre dışı bırakılır.
- **Etki:** Güvenlik katmanı olarak Keyword-fallback stratejisi kullanılmaktadır.
- **Kök neden:** 110 Milyon parametreli Transformer modelleri için 400 veri satırı aşırı uyuma (overfitting) sebep olmaktadır. Model eğitim verisini hızla ezberlerken, test verisindeki varyasyonları genelleyememektedir (Generalization error).

### Keyword-Fallback'in Güçlü ve Zayıf Yönleri
| Güçlü Yön | Zayıf Yön |
|-----------|-----------|
| Deterministik, hızlı, öngörülebilir | Eşanlamlı ve dolaylı ifadeleri yakalayamaz |
| Bakımı ve genişletmesi kolay | İlk eşleşmede durur (birden fazla intent olasılığı yok) |
| Düşük latency (~1ms) | Confidence skoru sabit (0.77-0.83), gerçek model güvenini yansıtmaz |
| Çalışması garanti (transformer bağımlılığı yok) | Ağırlıklı veya kısmi eşleşme yok |

---

## 2. Knowledge Base Kapsamı

### Kapsanan Alanlar
- Sınav ve notlandırma (11 madde)
- Dijital sistemler — REBIS, OBS, WiFi, VPN (10 madde)
- Öğrenci hizmetleri — belge, transkript, diploma (7 madde)
- Kütüphane (7 madde)
- Yemekhane (5 madde)
- Burs ve mali destek (4 madde)
- Kampüs lojistik (4 madde)
- Ders kaydı (5 madde)
- Kampüs yaşamı (3 madde)

### Kapsam Dışı / Eksik Alanlar
- **Fakülteye özel müfredatlar** — Yalnızca genel kurallar var, fakülte bazlı özel yönergeler yok
- **Yüksek lisans / doktora** — Sadece ön lisans/lisans odaklı
- **Yurt başvuru süreçleri** — KYK/üniversite yurt detayları sınırlı
- **Staj ve iş deneyimi** — Staj yönergeleri KB'de yok
- **Uluslararası öğrenci** — Erasmus, yatay geçiş uluslararası boyutu yok
- **Güncel akademik takvim** — Takvim tarihleri zaman hassas, KB'de statik

---

## 3. Zaman Hassas Bilgi Riski

KB'deki bazı maddeler `time_sensitive: true` olarak işaretlenmiştir:
- Kütüphane çalışma saatleri
- Yemek rezervasyon dönemi
- Akademik takvim tarihleri

**Risk:** Bu bilgiler eskiyebilir. Sistem doğru bir şekilde zaman hassas bilgi için resmi kaynak kontrolü önermektedir, ancak KB güncelleme mekanizması manuel'dir.

**Mevcut hafifletme:**
- `time_sensitive: true` olan maddeler ek eşik cezası alır
- Prompt'ta "resmi kaynak kontrolü öner" talimatı Gemini'ye verilir
- Otomatik güncelleme mekanizması **yoktur**

---

## 4. Gemini API Bağımlılığı

| Risk | Etki | Hafifletme |
|------|------|------------|
| API kullanılamaz ise | Bot yanıt üretemez | Error fallback mesajı döner |
| API maliyeti | Yüksek kullanımda maliyet artışı | Rate limiting uygulanmış |
| API güncelleme / model değişikliği | Yanıt kalitesi değişebilir | Model adı config'te parametrik |
| Halüsinasyon riski | Gemini yanlış bilgi üretebilir | Grounded prompt ile KB cevabına bağlanma |

**Önemli:** EduAI, Gemini'yi **sıfırdan cevap üretmek** için değil, **KB'den gelen doğrulanmış bilgiyi doğallaştırmak** için kullanır. Bu tasarım halüsinasyon riskini önemli ölçüde azaltır.

---

## 5. Entity Extraction Sınırları

- Sözlük tabanlı (regex match), ML tabanlı NER değil
- Yalnızca `entity_dictionary.json`'da tanımlı alias'lar yakalanır
- Yazım hataları tolere edilmez (fuzzy matching yok)
- Bağlam-bağımsız: "OIDB" her yerde aynı entity olarak çıkar

---

## 6. Genel Sistem Sınırları

| Sınır | Detay |
|-------|-------|
| Tek dil | Yalnızca Türkçe desteklenir |
| Tek üniversite | RTEU'ye özel, genel üniversite bilgisi değil |
| Conversation memory | Gemini'ye oturum geçmişi gönderilir, ancak NLP sınıflandırma tek mesaj bazlı |
| Offline modeli yok | Gemini API olmadan yalnızca KB cevabı döndürülebilir, doğal dil üretimi yapılamaz |
| Geri bildirim döngüsü | Feedback toplanır ama model yeniden eğitimi otomatik değil |

---

## 7. Tez İçin Pozisyonlama

Bu sınırlar, tezde şu şekilde çerçevelenmelidir:

1. **"Küçük veri setlerinde (Small Data) Overfitting (Aşırı Uyum) sınırları"** — Verinin 400 örneğe sentetik olarak artırılması sonucu accuracy %36'ya çıksa dahi 110 Milyon hücreli modeller donanımsal doyuma ulaşamamış; buna karşılık Keyword-fallback ile fonksiyonel bir kalkan üretilmiştir.
2. **"KB genişletilebilir tasarım"** — JSON tabanlı, kolay genişletilebilir yapı
3. **"Grounding ile halüsinasyon kontrolü"** — Gemini'nin serbest üretim yerine KB'ye dayandırılması bilinçli bir tasarım kararıdır
4. **"Prototip / PoC kapsamı"** — Üniversite geneli değil, belirli bir kampüs için pilot uygulama
