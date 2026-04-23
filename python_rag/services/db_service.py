import json
from pathlib import Path
from sqlalchemy import create_engine, text
from openai import AzureOpenAI


class DbService:
    def __init__(self, connection_string: str, client: AzureOpenAI, embedding_deployment: str):
        self.engine = create_engine(connection_string)
        self.client = client
        self.embedding_deployment = embedding_deployment

    def setup_table(self):
        with self.engine.connect() as conn:
            conn.execute(text("CREATE EXTENSION IF NOT EXISTS vector"))
            conn.execute(text("""
                CREATE TABLE IF NOT EXISTS pm_chunks (
                    id TEXT PRIMARY KEY,
                    chunk_text TEXT,
                    category TEXT,
                    embedding vector(1536)
                )
            """))
            conn.commit()

    def get_embedding(self, text_input: str) -> list[float]:
        response = self.client.embeddings.create(
            input=text_input,
            model=self.embedding_deployment
        )
        return response.data[0].embedding

    def seed(self):
        data_path = Path(__file__).parent.parent / "data" / "project_management_chunks.json"
        chunks = json.loads(data_path.read_text(encoding="utf-8"))

        with self.engine.connect() as conn:
            count = conn.execute(text("SELECT COUNT(*) FROM pm_chunks")).scalar()
            if count > 0:
                print(f"Already seeded ({count} records). Skipping.")
                return

        print(f"Seeding {len(chunks)} chunks...")
        for chunk in chunks:
            chunk_text = f"""SORU: {chunk['question']}

CEVAP: {chunk['answer']}

BAĞLAM: {chunk['context']}"""

            embedding = self.get_embedding(chunk_text)

            with self.engine.connect() as conn:
                conn.execute(text("""
                    INSERT INTO pm_chunks (id, chunk_text, category, embedding)
                    VALUES (:id, :chunk_text, :category, CAST(:embedding AS vector))
                    ON CONFLICT (id) DO NOTHING
                """), {
                    "id": chunk["id"],
                    "chunk_text": chunk_text,
                    "category": chunk["metadata"]["category"],
                    "embedding": str(embedding)
                })
                conn.commit()

        print("Seeding complete.")

    def search_by_embedding(self, query_embedding: list[float], top_k: int = 3) -> list[dict]:
        embedding_str = str(query_embedding)
        with self.engine.connect() as conn:
            rows = conn.execute(text("""
                SELECT chunk_text, category,
                    1 - (embedding <=> CAST(:query_embedding AS vector)) AS similarity
                FROM pm_chunks
                ORDER BY embedding <=> CAST(:query_embedding AS vector)
                LIMIT :top_k
            """), {
                "query_embedding": embedding_str,
                "top_k": top_k
            }).fetchall()

        return [
            {
                "chunk_text": row.chunk_text,
                "category": row.category,
                "similarity": round(float(row.similarity), 4)
            }
            for row in rows
        ]