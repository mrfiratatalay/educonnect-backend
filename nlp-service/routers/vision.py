from fastapi import APIRouter, File, UploadFile
from pydantic import BaseModel

from services.vision_service import VisionService

router = APIRouter()
vision_service = VisionService()


class EmbeddingResponse(BaseModel):
    features: list[float]
    dimension: int
    model_used: str


@router.post("/extract", response_model=EmbeddingResponse)
async def extract_features(image: UploadFile = File(...)):
    image_bytes = await image.read()
    result = vision_service.extract_features(image_bytes)
    return result


class SimilarityRequest(BaseModel):
    query_features: list[float]
    candidate_features: list[list[float]]


class SimilarityResponse(BaseModel):
    similarities: list[float]


@router.post("/similarity", response_model=SimilarityResponse)
async def compute_similarity(request: SimilarityRequest):
    similarities = vision_service.compute_similarity(
        request.query_features, request.candidate_features
    )
    return SimilarityResponse(similarities=similarities)


@router.get("/health")
async def vision_health():
    return {
        "status": "healthy",
        "model_loaded": vision_service.is_loaded,
        "model_name": vision_service.model_name,
    }
