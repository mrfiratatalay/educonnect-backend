# EduAI — Demo Senaryoları

> Tez savunması sırasında canlı demo için önerilen senaryolar.
> Her senaryo ne gösterildiğini, hangi soruların sorulacağını ve beklenen davranışı açıklar.

---

## Senaryo 1: Grounded Cevap — Yüksek Güvenli KB Eşleşmesi

**Amaç:** EduAI'nin bilgi tabanındaki doğrulanmış bilgiyi Gemini ile doğallaştırarak sunduğunu göstermek.

**Soru:** "Finalde minimum kaç almak gerekir?"

**Beklenen Davranış:**
- Intent: `exam_and_grading` (confidence ~0.82)
- KB eşleşmesi: `kb_exam_003` — "final barajı" maddesi
- KB score: yüksek (keyword + question overlap)
- Grounded prompt Gemini'ye gönderilir
- Cevap KB bilgisine dayanır: "En az 50 puan" + kaynak referansı
- `KbHit: true`, `IsFallback: false`

**Gösterilecek:** Cevabın KB'den geldiği, halüsinasyon olmadığı, resmi kaynağın referans verildiği.

---

## Senaryo 2: Grounded Cevap — Dijital Sistem Sorusu

**Amaç:** Farklı bir intent kategorisinde de grounded cevabın çalıştığını göstermek.

**Soru:** "Şifremi unuttum ne yapmalıyım?"

**Beklenen Davranış:**
- Intent: `digital_systems`
- KB eşleşmesi: `kb_digital_005` — "şifre sıfırlama" maddesi
- Cevap: "Kayıtlı cep telefonu ile yeni şifre oluşturma" bilgisi
- `KbHit: true`, `IsFallback: false`

**Gösterilecek:** Soru çok doğal, KB'deki "sifremi unuttum" keyword'ü ile eşleşiyor.

---

## Senaryo 3: Fallback Senaryosu — KB'de Olmayan Konu

**Amaç:** KB'de bilgi yokken EduAI'nin uydurma bilgi üretmek yerine temkinli davrandığını göstermek.

**Soru:** "Yaz okulunda hangi dersler açılacak?"

**Beklenen Davranış:**
- Intent: `course_registration` (keyword: "ders")
- KB eşleşmesi: düşük score veya eşik altı (yaz okulu maddesi KB'de yok)
- Fallback prompt Gemini'ye gönderilir
- Cevap: "Bu konuda kesin bilgi veremiyorum, OIDB veya bölüm sekreterliğine danışmanızı öneririm"
- `KbHit: false`, `IsFallback: true` veya düşük KB score

**Gösterilecek:** Sistem bilmediğini biliyor ve kullanıcıyı resmi birime yönlendiriyor.

---

## Senaryo 4: Entity Extraction Gösterimi

**Amaç:** NLP pipeline'ının kullanıcı mesajından yapılandırılmış varlıklar çıkardığını göstermek.

**Soru:** "OIDB'den transkript almak istiyorum, ne zaman açık?"

**Beklenen Davranış:**
- Intent: `student_services`
- Entities:
  - `OIDB` → type: `unit`
  - `transkript` → type: ilgili tip
  - `ne zaman` → type: `temporal`
- KB eşleşmesi: transkript maddesi

**Gösterilecek:** API response'undaki `entities[]` dizisi, yapılandırılmış bilgi çıkarımı.

---

## Senaryo 5: Geri Bildirim Akışı (Faz 5)

**Amaç:** Kullanıcı geri bildirim mekanizmasının çalıştığını göstermek.

**Adımlar:**
1. Bir soru sor → bot yanıtı al
2. Yanıta 👎 "yardımcı olmadı" geri bildirimi gönder (`POST /api/chat/messages/{id}/feedback`)
3. Aynı mesajın geçmişinde `HasFeedback: true`, `FeedbackIsHelpful: false` göster

**Gösterilecek:** Mesaj düzeyinde geri bildirim toplama ve raporlama altyapısı.

---

## Senaryo 6: Analytics Dashboard (Faz 5 — Admin)

**Amaç:** Toplanan metriklerin admin raporlamasını göstermek.

**Adımlar:**
1. Admin olarak login ol
2. `GET /api/chat/analytics/summary` endpoint'ini çağır
3. Dönen metrikleri göster:
   - HelpfulRate
   - AvgConfidence
   - KbHitRate
   - FallbackRate
   - IntentDistribution
   - TopUnresolvedQueries

**Gösterilecek:** EduAI'nin performansının ölçülebilir olduğu, tez iddialarının veri ile desteklenebileceği.

---

## Demo Akış Sırası (Önerilen)

| Sıra | Senaryo | Süre |
|------|---------|------|
| 1 | Senaryo 1 — Grounded KB cevabı | 2 dk |
| 2 | Senaryo 2 — Farklı intent ile grounded | 1 dk |
| 3 | Senaryo 4 — Entity extraction | 2 dk |
| 4 | Senaryo 3 — Fallback davranışı | 2 dk |
| 5 | Senaryo 5 — Geri bildirim | 2 dk |
| 6 | Senaryo 6 — Analytics özet | 2 dk |
| **Toplam** | | **~11 dk** |
