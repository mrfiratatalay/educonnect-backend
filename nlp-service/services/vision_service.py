import io
import logging

import numpy as np
from PIL import Image

logger = logging.getLogger(__name__)


class VisionService:
    """ResNet-50 based image feature extraction for visual search."""

    def __init__(self):
        self.model = None
        self.transform = None
        self.is_loaded = False
        self.model_name = "resnet50-pretrained"
        self._load_model()

    def _load_model(self):
        try:
            import torch
            from torchvision.models import resnet50, ResNet50_Weights

            weights = ResNet50_Weights.DEFAULT
            base_model = resnet50(weights=weights)

            self.model = torch.nn.Sequential(*list(base_model.children())[:-1])
            self.model.eval()

            self.transform = weights.transforms()
            self.is_loaded = True
            logger.info("ResNet-50 feature extractor loaded successfully")
        except Exception as e:
            logger.error("Failed to load ResNet-50: %s", e)

    def extract_features(self, image_bytes: bytes) -> dict:
        import torch

        image = Image.open(io.BytesIO(image_bytes)).convert("RGB")
        tensor = self.transform(image).unsqueeze(0)

        with torch.no_grad():
            features = self.model(tensor)

        feature_vector = features.squeeze().numpy().tolist()

        return {
            "features": feature_vector,
            "dimension": len(feature_vector),
            "model_used": self.model_name,
        }

    def compute_similarity(
        self, query_features: list[float], candidate_features: list[list[float]]
    ) -> list[float]:
        query = np.array(query_features)
        query_norm = query / (np.linalg.norm(query) + 1e-8)

        similarities = []
        for candidate in candidate_features:
            cand = np.array(candidate)
            cand_norm = cand / (np.linalg.norm(cand) + 1e-8)
            sim = float(np.dot(query_norm, cand_norm))
            similarities.append(round(sim, 4))

        return similarities
