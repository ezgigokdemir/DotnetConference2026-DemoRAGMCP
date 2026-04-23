using System.Text.Json;
using Microsoft.SemanticKernel.Embeddings;
using Npgsql;
using Pgvector;

namespace PmRagChatbot.Services;

public class DbService
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ITextEmbeddingGenerationService _embeddingService;
    private readonly string _dataPath;

    public DbService(NpgsqlDataSource dataSource, ITextEmbeddingGenerationService embeddingService)
    {
        _dataSource = dataSource;
        _embeddingService = embeddingService;
        _dataPath = Path.Combine(AppContext.BaseDirectory, "data", "project_management_chunks.json");
    }

    public async Task SetupTableAsync()
    {
        await using var conn = await _dataSource.OpenConnectionAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE EXTENSION IF NOT EXISTS vector";
        await cmd.ExecuteNonQueryAsync();

        cmd.CommandText = """
                          CREATE TABLE IF NOT EXISTS pm_chunks (
                              id TEXT PRIMARY KEY,
                              chunk_text TEXT,
                              category TEXT,
                              embedding vector(1536)
                          )
                          """;
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SeedAsync()
    {
        await using var conn = await _dataSource.OpenConnectionAsync();

        await using var countCmd = conn.CreateCommand();
        countCmd.CommandText = "SELECT COUNT(*) FROM pm_chunks";
        var count = (long)(await countCmd.ExecuteScalarAsync() ?? 0);

        if (count > 0)
        {
            Console.WriteLine($"Already seeded ({count} records). Skipping.");
            return;
        }

        var json = await File.ReadAllTextAsync(_dataPath);
        var chunks = JsonSerializer.Deserialize<List<ChunkItem>>(json)!;

        Console.WriteLine($"Seeding {chunks.Count} chunks...");

        foreach (var chunk in chunks)
        {
            var chunkText = $"""
                             SORU: {chunk.Question}

                             CEVAP: {chunk.Answer}

                             BAĞLAM: {chunk.Context}
                             """;

            // ITextEmbeddingGenerationService — provider bağımsız
            var embeddingResult = await _embeddingService.GenerateEmbeddingAsync(chunkText);
            var vector = new Vector(embeddingResult.ToArray());

            await using var insertCmd = conn.CreateCommand();
            insertCmd.CommandText = """
                                    INSERT INTO pm_chunks (id, chunk_text, category, embedding)
                                    VALUES (@id, @chunk_text, @category, @embedding)
                                    ON CONFLICT (id) DO NOTHING
                                    """;
            insertCmd.Parameters.AddWithValue("id", chunk.Id);
            insertCmd.Parameters.AddWithValue("chunk_text", chunkText);
            insertCmd.Parameters.AddWithValue("category", chunk.Metadata.Category);
            insertCmd.Parameters.AddWithValue("embedding", vector);
            await insertCmd.ExecuteNonQueryAsync();
        }

        Console.WriteLine("Seeding complete.");
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        var result = await _embeddingService.GenerateEmbeddingAsync(text);
        return result.ToArray();
    }

    public async Task<List<SearchResult>> SearchByEmbeddingAsync(float[] embedding, int topK = 3)
    {
        var vector = new Vector(embedding);
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = """
                          SELECT chunk_text, category,
                                 1 - (embedding <=> @embedding) AS similarity
                          FROM pm_chunks
                          ORDER BY embedding <=> @embedding
                          LIMIT @topK
                          """;
        cmd.Parameters.AddWithValue("embedding", vector);
        cmd.Parameters.AddWithValue("topK", topK);

        var results = new List<SearchResult>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new SearchResult(
                reader.GetString(0),
                reader.GetString(1),
                Math.Round(reader.GetDouble(2), 4)
            ));
        }

        return results;
    }
}

public record SearchResult(string ChunkText, string Category, double Similarity);

public record ChunkItem(
    string Id,
    string Question,
    string Answer,
    string Context,
    ChunkMetadata Metadata
);

public record ChunkMetadata(string Category);