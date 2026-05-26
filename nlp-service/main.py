import os

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from routers import nlp, vision

app = FastAPI(
    title="EduConnect NLP & Vision Service",
    description="BERTurk intent/entity classification + ResNet-50 feature extraction",
    version="1.0.0",
)

_allowed_origins_env = os.getenv("ALLOWED_ORIGINS", "")
allowed_origins = [o.strip() for o in _allowed_origins_env.split(",") if o.strip()] or ["*"]

app.add_middleware(
    CORSMiddleware,
    allow_origins=allowed_origins,
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(nlp.router, prefix="/api/nlp", tags=["NLP"])
app.include_router(vision.router, prefix="/api/vision", tags=["Vision"])


@app.get("/", tags=["Root"])
async def root():
    return {
        "service": "EduConnect NLP & Vision Service",
        "version": "1.0.0",
        "status": "running",
        "description": "BERTurk intent/entity classification + ResNet-50 feature extraction",
        "endpoints": {
            "docs": "/docs",
            "redoc": "/redoc",
            "health": "/health",
            "nlp": {
                "classify": "POST /api/nlp/classify",
                "health": "GET /api/nlp/health",
            },
            "vision": {
                "extract": "POST /api/vision/extract",
                "similarity": "POST /api/vision/similarity",
                "health": "GET /api/vision/health",
            },
        },
    }


@app.get("/health", tags=["Health"])
async def health():
    return {"status": "healthy", "service": "nlp-vision"}
