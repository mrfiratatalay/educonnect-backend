from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from routers import nlp, vision

app = FastAPI(
    title="EduConnect NLP & Vision Service",
    description="BERTurk intent/entity classification + ResNet-50 feature extraction",
    version="1.0.0",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:5160"],
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(nlp.router, prefix="/api/nlp", tags=["NLP"])
app.include_router(vision.router, prefix="/api/vision", tags=["Vision"])


@app.get("/health")
async def health():
    return {"status": "healthy", "service": "nlp-vision"}
