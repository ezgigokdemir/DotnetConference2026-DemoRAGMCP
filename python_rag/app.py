import os
import streamlit as st
from dotenv import load_dotenv
from openai import AzureOpenAI
from services.db_service import DbService
from services.rag_service import RagService

load_dotenv()

st.set_page_config(page_title="PM RAG Chatbot", page_icon="📋")
st.title("📋 Project Management RAG Chatbot")
st.caption("Powered by Azure OpenAI + pgvector | Python Implementation")


@st.cache_resource
def init():
    client = AzureOpenAI(
        azure_endpoint=os.getenv("AZURE_OPENAI_ENDPOINT"),
        api_key=os.getenv("AZURE_OPENAI_API_KEY"),
        api_version=os.getenv("AZURE_OPENAI_API_VERSION", "2024-02-01")
    )
    db_service = DbService(
        connection_string=os.getenv("DATABASE_URL"),
        client=client,
        embedding_deployment=os.getenv("EMBEDDING_DEPLOYMENT", "text-embedding-3-small")
    )
    db_service.setup_table()
    db_service.seed()

    rag_service = RagService(
        db_service=db_service,
        client=client,
        llm_deployment=os.getenv("LLM_DEPLOYMENT", "gpt-4o-mini")
    )
    return rag_service


rag = init()

if "messages" not in st.session_state:
    st.session_state.messages = []

for msg in st.session_state.messages:
    with st.chat_message(msg["role"]):
        st.markdown(msg["content"])

if question := st.chat_input("Scrum, Kanban, CI/CD hakkında soru sorun..."):
    st.session_state.messages.append({"role": "user", "content": question})
    with st.chat_message("user"):
        st.markdown(question)

    with st.chat_message("assistant"):
        with st.spinner("Yanıt hazırlanıyor..."):
            result = rag.ask(question)

        st.markdown(result["answer"])

        with st.expander("📊 Benchmark"):
            b = result["benchmark"]
            col1, col2, col3, col4 = st.columns(4)
            col1.metric("Embedding", f"{b['embedding_ms']} ms")
            col2.metric("Retrieval", f"{b['retrieval_ms']} ms")
            col3.metric("LLM", f"{b['llm_ms']} ms")
            col4.metric("Total", f"{b['total_ms']} ms")

        with st.expander("🔍 Kaynaklar"):
            for s in result["sources"]:
                st.markdown(f"**{s['chunk_text'][:80]}...** *(similarity: {s['similarity']})*")
                st.caption(s["category"])

    st.session_state.messages.append({"role": "assistant", "content": result["answer"]})
