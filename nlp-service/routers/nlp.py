from fastapi import APIRouter
from pydantic import BaseModel
import time

from services.intent_service import IntentService
from services.knowledge_service import KnowledgeService

router = APIRouter()
intent_service = IntentService()
knowledge_service = KnowledgeService()


class ClassifyRequest(BaseModel):
    text: str


class EntityItem(BaseModel):
    value: str
    type: str
    start: int
    end: int


class KbMatch(BaseModel):
    question: str
    answer: str
    score: float
    source_url: str = ""
    source_title: str = ""
    confidence: str = "medium"
    time_sensitive: bool = False
    faculty_scope: str = "general"
    topic: str = ""


class ClassifyResponse(BaseModel):
    intent: str
    confidence: float
    confidence_band: str = "low"
    needs_review: bool = False
    resolver_used: bool = False
    entities: list[EntityItem]
    model_used: str
    kb_answer: KbMatch | None = None
    latency_ms: int = 0


@router.post("/classify", response_model=ClassifyResponse)
async def classify(request: ClassifyRequest):
    start_time = time.perf_counter()

    result = intent_service.classify(request.text)

    kb_match = knowledge_service.lookup(result["intent"], request.text, result["entities"])

    elapsed_ms = int((time.perf_counter() - start_time) * 1000)

    return ClassifyResponse(
        intent=result["intent"],
        confidence=result["confidence"],
        confidence_band=result.get("confidence_band", "low"),
        needs_review=result.get("needs_review", False),
        resolver_used=result.get("resolver_used", False),
        entities=result["entities"],
        model_used=result["model_used"],
        kb_answer=KbMatch(**kb_match) if kb_match else None,
        latency_ms=elapsed_ms,
    )


@router.get("/health")
async def nlp_health():
    return {
        "status": "healthy",
        "model_loaded": intent_service.is_loaded,
        "model_name": intent_service.model_name,
        "kb_loaded": len(knowledge_service.kb) > 0,
        "kb_categories": len(knowledge_service.kb),
    }
