from __future__ import annotations

import sys
import unittest
from dataclasses import dataclass
from pathlib import Path
from uuid import NAMESPACE_URL, uuid5

import torch
from transformers import AutoTokenizer


SERVICE_ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(SERVICE_ROOT))

import app as classification_app


@dataclass(frozen=True)
class ClassificationCase:
    name: str
    expected_subject: str | None
    content: str


LEAF_SUBJECTS = (
    "Dev > Apps",
    "Mathematics > Linear Algebra",
    "Mathematics > Vector Calculus",
    "Dev > Cloud",
    "Dev > Data > Python for Analysis",
    "Humanas > Literatura",
    "Dev > AI Pair Programming",
    "Dev > Data > SQL Server",
)

CASES = (
    ClassificationCase(
        "apps",
        "Dev > Apps",
        "Desenvolvi um aplicativo web com telas, formulários, rotas, autenticação de usuários e publicação da interface.",
    ),
    ClassificationCase(
        "data",
        None,
        "Analisei um conjunto de dados, tratei valores ausentes, agrupei registros e calculei estatísticas para identificar padrões.",
    ),
    ClassificationCase(
        "linear_algebra",
        "Mathematics > Linear Algebra",
        "Estudei matrizes, vetores, espaços vetoriais, transformações lineares, autovalores e autovetores.",
    ),
    ClassificationCase(
        "vector_calculus",
        "Mathematics > Vector Calculus",
        "Calculei gradiente, divergente e rotacional de campos vetoriais e revisei integrais de linha e o teorema de Stokes.",
    ),
    ClassificationCase(
        "cloud",
        "Dev > Cloud",
        "Configurei recursos no Azure com máquinas virtuais, armazenamento, rede virtual, escalabilidade e alta disponibilidade na nuvem.",
    ),
    ClassificationCase(
        "python_for_analysis",
        "Dev > Data > Python for Analysis",
        "Usei Python com pandas e NumPy para carregar um CSV em um DataFrame, executar groupby e criar gráficos com Matplotlib.",
    ),
    ClassificationCase(
        "literatura",
        "Humanas > Literatura",
        "Analisei o narrador, as personagens, as metáforas e a estrutura de um romance, observando seu estilo literário.",
    ),
    ClassificationCase(
        "ai_pair_programming",
        "Dev > AI Pair Programming",
        "Programei em parceria com um assistente de inteligência artificial, escrevendo prompts, delegando tarefas a subagentes e revisando o código gerado.",
    ),
    ClassificationCase(
        "sql_server",
        "Dev > Data > SQL Server",
        "No SQL Server, escrevi consultas T-SQL com SELECT e JOIN, criei índices e analisei o plano de execução.",
    ),
)


class PortugueseSubjectClassificationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        model = classification_app.GLiClassModel.from_pretrained(classification_app.MODEL_NAME)
        tokenizer = AutoTokenizer.from_pretrained(classification_app.MODEL_NAME)
        pipeline = classification_app.ZeroShotClassificationPipeline(
            model,
            tokenizer,
            classification_type="multi-label",
            device=device,
            progress_bar=False,
        )
        nodes = [
            classification_app.ClassificationNode(
                id=uuid5(NAMESPACE_URL, subject),
                name=subject,
                description=None,
                parentId=None,
            )
            for subject in LEAF_SUBJECTS
        ]
        labels_by_node = classification_app.build_labels(nodes)
        cls.labels = list(labels_by_node.values())
        predictions = pipeline(
            [case.content for case in CASES],
            cls.labels,
            threshold=0.0,
        )
        cls.scores = {
            case.name: {item["label"]: float(item["score"]) for item in result}
            for case, result in zip(CASES, predictions, strict=True)
        }

    def assert_expected_subject_first(self, case_name: str) -> None:
        case = next(item for item in CASES if item.name == case_name)
        scores = self.scores[case_name]
        ranked = sorted(scores.items(), key=lambda item: item[1], reverse=True)
        self.assertIsNotNone(case.expected_subject)
        self.assertEqual(
            case.expected_subject,
            ranked[0][0],
            msg=f"Classificação incorreta para {case_name}: {ranked}",
        )

    def test_apps_content_ranks_apps_first(self) -> None:
        self.assert_expected_subject_first("apps")

    def test_data_content_routes_to_a_data_leaf(self) -> None:
        scores = self.scores["data"]
        winner = max(scores, key=scores.__getitem__)

        self.assertNotIn("Dev > Data", self.labels)
        self.assertIn(
            winner,
            {
                "Dev > Data > Python for Analysis",
                "Dev > Data > SQL Server",
            },
            msg=f"Conteúdo de Data foi classificado em um assunto não relacionado: {scores}",
        )

    def test_linear_algebra_content_ranks_linear_algebra_first(self) -> None:
        self.assert_expected_subject_first("linear_algebra")

    def test_vector_calculus_content_ranks_vector_calculus_first(self) -> None:
        self.assert_expected_subject_first("vector_calculus")

    def test_cloud_content_ranks_cloud_first(self) -> None:
        self.assert_expected_subject_first("cloud")

    def test_python_for_analysis_content_ranks_python_for_analysis_first(self) -> None:
        self.assert_expected_subject_first("python_for_analysis")

    def test_literatura_content_ranks_literatura_first(self) -> None:
        self.assert_expected_subject_first("literatura")

    def test_ai_pair_programming_content_ranks_ai_pair_programming_first(self) -> None:
        self.assert_expected_subject_first("ai_pair_programming")

    def test_sql_server_content_ranks_sql_server_first(self) -> None:
        self.assert_expected_subject_first("sql_server")


if __name__ == "__main__":
    unittest.main()
