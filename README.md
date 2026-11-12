# DocQA: document question-answering API (.NET 8)

[![CI](https://github.com/MustiPat/docqa-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/MustiPat/docqa-dotnet/actions/workflows/ci.yml)

A small, production-minded REST API that answers questions from your own documents and cites its sources. It shows the retrieval-augmented generation (RAG) pattern end to end: ingest, chunk, retrieve, answer.

It runs with **no API key** in an offline extractive mode, and switches to any OpenAI-compatible chat model when you set three environment variables.

## What it demonstrates

- **Retrieval:** paragraph-aware chunking and BM25 keyword ranking, written from scratch with no external services.
- **Pluggable answering:** `IAnswerGenerator` has an offline extractive implementation and an LLM implementation (any OpenAI-compatible `/chat/completions` endpoint). If the LLM call fails, it falls back to the extractive answer instead of returning an error.
- **Citations:** every answer returns the source chunks that supported it.
- **Prompt-injection awareness:** the model is told that document text is untrusted data and must not be treated as instructions.
- **API hygiene:** input validation and size limits, optional `X-API-Key` protection, health endpoint, 400/404 responses with clear messages.
- **Tests:** xUnit tests for the chunker, retriever and generators in `tests/DocQA.Tests`.

## Run it

Requires the .NET 8 SDK.

```bash
cd src/DocQA.Api
dotnet run
```

Add a document and ask a question:

```bash
curl -X POST http://localhost:5000/documents -H "Content-Type: application/json" \
  -d '{"title":"Refund policy","text":"Customers can request a refund within 30 days. Refunds are issued within 5 business days."}'

curl -X POST http://localhost:5000/ask -H "Content-Type: application/json" \
  -d '{"question":"How long do refunds take?"}'
```

Example response:

```json
{
  "answer": "Refunds are issued within 5 business days. [1]",
  "mode": "extractive",
  "citations": [{ "number": 1, "documentTitle": "Refund policy", "chunkIndex": 0, "score": 0.591, "excerpt": "..." }]
}
```

The port is printed on start-up; set `ASPNETCORE_URLS=http://localhost:5000` to fix it.

## Use a real language model

```bash
export LLM_API_KEY=your-key
export LLM_MODEL=gpt-4o-mini                       # optional
export LLM_BASE_URL=https://api.openai.com/v1      # optional; any compatible endpoint
dotnet run
```

The response `mode` becomes `llm`.

## Endpoints

| Method | Path | Purpose |
|---|---|---|
| GET | `/health` | Liveness check |
| GET | `/documents` | List documents |
| POST | `/documents` | Add `{ "title", "text" }` (max 200,000 characters) |
| DELETE | `/documents/{id}` | Remove a document |
| POST | `/ask` | Ask `{ "question", "topK" }` (topK 1 to 10, default 3) |

Set `DEMO_API_KEY` to require an `X-API-Key` header on every route except `/health`.

## Tests

```bash
dotnet test tests/DocQA.Tests
```

## Limits and next steps

This is a demo. The store is in memory, so documents vanish on restart; retrieval is keyword-based, so it will not match synonyms. For production, replace `DocumentStore` with a database, add embeddings and a vector index (for example pgvector or Azure AI Search), add authentication and rate limiting, and stream responses.

## Layout

```
src/DocQA.Api/            API, models and services
src/DocQA.Api/Services/   Chunker, Retriever, DocumentStore, answer generators
tests/DocQA.Tests/        xUnit tests
samples/                  sample document to try
```
