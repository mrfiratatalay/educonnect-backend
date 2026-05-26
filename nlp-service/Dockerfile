# Hugging Face Spaces (Docker SDK) icin port 7860 beklenir.
FROM python:3.11-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    PIP_DISABLE_PIP_VERSION_CHECK=1 \
    HF_HOME=/app/.cache/huggingface \
    TORCH_HOME=/app/.cache/torch

WORKDIR /app

# Sistem bagimliliklari (Pillow vb. icin minimal set)
RUN apt-get update && apt-get install -y --no-install-recommends \
        libgomp1 \
    && rm -rf /var/lib/apt/lists/*

# CPU-only torch (image boyutunu ~yariya indirir, RAM kullanimini da azaltir)
RUN pip install --no-cache-dir \
        torch==2.5.0 \
        torchvision==0.20.0 \
        --index-url https://download.pytorch.org/whl/cpu

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY main.py .
COPY routers/ ./routers/
COPY services/ ./services/
COPY data/ ./data/
COPY models/ ./models/

RUN mkdir -p /app/.cache/huggingface /app/.cache/torch

EXPOSE 7860

CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "7860"]
