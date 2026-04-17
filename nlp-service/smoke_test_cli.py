import time

from services.intent_service import IntentService
from services.knowledge_service import KnowledgeService


test_cases = [
    "Finalden kalirsam butunlemeye girebilir miyim?",
    "Kayit yenilemeyi kacirdim, ne yapmaliyim?",
    "Kutuphanede grup calisma odasi rezerve edebilir miyim?",
    "Yemek bursu basvurulari nereye yapiliyor?",
    "OBS sifremi unuttum, nasil sifirlayabilirim?",
    "OIDB nereden ogrenci belgesi alabilirim?",
    "Topluluk danisman bilgilerini nereden bulurum?",
    "Psikolojik danismanlik hizmeti ucretli mi?",
    "Yemek rezervasyonu en gec ne zamana kadar yapilir?",
    "Engelli ogrenci destegi hangi resmi birim altindadir?",
]


def run_smoke_test():
    intent_service = IntentService()
    knowledge_service = KnowledgeService()
    results = []

    for text in test_cases:
        start_time = time.perf_counter()
        result = intent_service.classify(text)
        kb_match = knowledge_service.lookup(result["intent"], text, result["entities"])
        elapsed_ms = int((time.perf_counter() - start_time) * 1000)

        payload = {
            "Soru": text,
            "Intent": result["intent"],
            "Confidence": result["confidence"],
            "Model Used": result["model_used"],
            "Entities": [{"text": e["value"], "type": e["type"]} for e in result["entities"]],
            "KB_Match": kb_match is not None,
            "Latency_ms": elapsed_ms,
        }
        if kb_match:
            payload["KB_Topic"] = kb_match["topic"]
            payload["KB_Score"] = round(kb_match["score"], 2)
        results.append(payload)

    print("\n" + "=" * 80)
    print(" EDUAI NLP SMOKE TEST SONUCLARI")
    print("=" * 80)
    for r in results:
        print(f"\nSoru: {r['Soru']}")
        print(f"Intent Tahmini:    {r['Intent']} (Guven: {r['Confidence']}, Yontem: {r['Model Used']})")
        if r["Entities"]:
            entities_str = ", ".join([f"{e['text']} [{e['type']}]" for e in r["Entities"]])
            print(f"Bulunan Varliklar: {entities_str}")
        if r["KB_Match"]:
            print(f"[OK] KB ESLESMESI: Evet (Konu: {r['KB_Topic']}, Puan: {r['KB_Score']})")
        else:
            print("[X] KB ESLESMESI: Yok")
        print(f"Sure: {r['Latency_ms']} ms")
        print("-" * 50)


if __name__ == "__main__":
    run_smoke_test()
