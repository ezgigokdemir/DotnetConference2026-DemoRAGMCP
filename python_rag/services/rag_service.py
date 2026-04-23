import time
import json
from datetime import datetime
from pathlib import Path
from openai import AzureOpenAI
from services.db_service import DbService

SYSTEM_PROMPT = """Sen bir proje yönetimi asistanısın. 
Yalnızca sağlanan bağlam bilgisini kullanarak soruları yanıtla.
Bağlamda cevap yoksa bunu açıkça belirt."""

LOG_FILE = Path(__file__).parent.parent / "benchmark_results.txt"


def log_result(question: str, benchmark: dict):
    with open(LOG_FILE, "a", encoding="utf-8") as f:
        f.write(f"\n{'='*60}\n")
        f.write(f"[{datetime.now().strftime('%H:%M:%S')}] {question}\n")
        f.write(f"  Embedding : {benchmark['embedding_ms']} ms\n")
        f.write(f"  Retrieval : {benchmark['retrieval_ms']} ms\n")
        f.write(f"  LLM       : {benchmark['llm_ms']} ms\n")
        f.write(f"  Total     : {benchmark['total_ms']} ms\n")


class RagService:
    def __init__(self, db_service: DbService, client: AzureOpenAI, llm_deployment: str):
        self.db_service = db_service
        self.client = client
        self.llm_deployment = llm_deployment

    def ask(self, question: str, top_k: int = 3) -> dict:
        # Step 1: Embedding
        t0 = time.perf_counter()
        query_embedding = self.db_service.get_embedding(question)
        embedding_ms = round((time.perf_counter() - t0) * 1000, 2)

        # Step 2: Retrieval
        t1 = time.perf_counter()
        results = self.db_service.search_by_embedding(query_embedding, top_k)
        retrieval_ms = round((time.perf_counter() - t1) * 1000, 2)

        # Step 3: LLM
        context = "\n\n".join([r['chunk_text'] for r in results])
        prompt = f"""## İLGİLİ DOKÜMANTASYON
{context}

## SORU
{question}"""

        messages = [
            {"role": "system", "content": SYSTEM_PROMPT},
            {"role": "user", "content": prompt}
        ]

        t2 = time.perf_counter()
        response = self.client.chat.completions.create(
            model=self.llm_deployment,
            messages=messages,
            temperature=0.3,
            max_tokens=1000
        )
        llm_ms = round((time.perf_counter() - t2) * 1000, 2)

        benchmark = {
            "embedding_ms": embedding_ms,
            "retrieval_ms": retrieval_ms,
            "llm_ms": llm_ms,
            "total_ms": round(embedding_ms + retrieval_ms + llm_ms, 2)
        }

        log_result(question, benchmark)

        return {
            "answer": response.choices[0].message.content,
            "sources": results,
            "benchmark": benchmark
        }