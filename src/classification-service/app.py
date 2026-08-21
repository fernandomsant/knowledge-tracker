import logging
import os
from collections.abc import Sequence
from contextlib import asynccontextmanager
from dataclasses import dataclass
from uuid import UUID

import gliclass
import torch
from fastapi import FastAPI
from gliclass import GLiClassModel, ZeroShotClassificationPipeline
from pydantic import BaseModel, Field
from transformers import AutoTokenizer

MODEL_NAME = os.getenv("GLICLASS_MODEL", "knowledgator/gliclass-multilang-ultra")
logger = logging.getLogger("uvicorn.error")


class ClassificationNode(BaseModel):
    id: UUID
    name: str = Field(min_length=1)
    description: str | None = None
    parent_id: UUID | None = Field(default=None, alias="parentId")


class ClassifyRequest(BaseModel):
    text: str = Field(min_length=1)
    nodes: list[ClassificationNode]


class ClassificationScore(BaseModel):
    node_id: UUID = Field(alias="nodeId")
    score: float = Field(ge=0, le=1)


class ClassifyResponse(BaseModel):
    classifications: list[ClassificationScore]
    model: str
    model_version: str = Field(alias="modelVersion")


@dataclass(frozen=True)
class ClassifierRuntime:
    pipeline: ZeroShotClassificationPipeline


runtime: ClassifierRuntime | None = None


@asynccontextmanager
async def lifespan(_: FastAPI):
    global runtime
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    model = GLiClassModel.from_pretrained(MODEL_NAME)
    tokenizer = AutoTokenizer.from_pretrained(MODEL_NAME)
    runtime = ClassifierRuntime(
        ZeroShotClassificationPipeline(
            model,
            tokenizer,
            classification_type="multi-label",
            device=device,
        )
    )
    yield
    runtime = None


app = FastAPI(title="Knowledge Tracker Classification", lifespan=lifespan)


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ready" if runtime is not None else "starting", "model": MODEL_NAME}


@app.post("/classify", response_model=ClassifyResponse)
def classify(request: ClassifyRequest) -> ClassifyResponse:
    if runtime is None:
        raise RuntimeError("Classifier model is not ready.")
    if not request.nodes:
        return ClassifyResponse(
            classifications=[], model=MODEL_NAME, modelVersion=gliclass.__version__
        )

    labels_by_node = build_labels(request.nodes)
    node_by_label = {label: node_id for node_id, label in labels_by_node.items()}
    predictions = runtime.pipeline(request.text, list(node_by_label), threshold=0.0)[0]
    predicted_scores = {
        node_by_label[item["label"]]: float(item["score"])
        for item in predictions
        if item["label"] in node_by_label
    }
    scores = [
        ClassificationScore(nodeId=node.id, score=predicted_scores.get(node.id, 0.0))
        for node in request.nodes
    ]
    ranked_output = "\n".join(
        f"{rank:2}. {labels_by_node[score.node_id]} [{score.node_id}] = {score.score:.6f}"
        for rank, score in enumerate(
            sorted(scores, key=lambda item: item.score, reverse=True), start=1
        )
    )
    logger.info(
        "Classifier model output:\nModel: %s\nVersion: %s\nScores:\n%s",
        MODEL_NAME,
        gliclass.__version__,
        ranked_output,
    )
    return ClassifyResponse(
        classifications=scores,
        model=MODEL_NAME,
        modelVersion=gliclass.__version__,
    )


def build_labels(nodes: Sequence[ClassificationNode]) -> dict[UUID, str]:
    nodes_by_id = {node.id: node for node in nodes}
    labels: dict[UUID, str] = {}
    used: set[str] = set()

    for index, node in enumerate(nodes):
        lineage = node_lineage(node, nodes_by_id)
        label = " > ".join(lineage)
        if node.description and node.description.strip():
            label = f"{label}: {node.description.strip()}"
        if label in used:
            label = f"{label} (taxonomy node {index + 1})"
        used.add(label)
        labels[node.id] = label

    return labels


def node_lineage(
    node: ClassificationNode,
    nodes_by_id: dict[UUID, ClassificationNode],
) -> list[str]:
    lineage = [node.name.strip()]
    seen = {node.id}
    parent_id = node.parent_id
    while parent_id is not None and parent_id not in seen and parent_id in nodes_by_id:
        seen.add(parent_id)
        parent = nodes_by_id[parent_id]
        lineage.append(parent.name.strip())
        parent_id = parent.parent_id
    lineage.reverse()
    return lineage
