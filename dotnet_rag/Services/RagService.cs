using System.Diagnostics;
using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace PmRagChatbot.Services;

public class RagService
{
    private readonly DbService _dbService;
    private readonly ChatClient _chatClient;

    private const string SystemPrompt = """
                                        Sen bir proje yönetimi asistanısın.
                                        Yalnızca sağlanan bağlam bilgisini kullanarak soruları yanıtla.
                                        Bağlamda cevap yoksa bunu açıkça belirt.
                                        """;

    public RagService(DbService dbService, IConfiguration config)
    {
        _dbService = dbService;

        var endpoint = config["AzureOpenAI:Endpoint"]!;
        var apiKey = config["AzureOpenAI:ApiKey"]!;
        var chatDeployment = config["AzureOpenAI:ChatDeployment"]!;

        var azureClient = new AzureOpenAIClient(new Uri(endpoint), new System.ClientModel.ApiKeyCredential(apiKey));
        _chatClient = azureClient.GetChatClient(chatDeployment);
    }

    public async Task<RagResponse> AskAsync(string question, int topK = 3)
    {
        // Step 1: Embedding
        var swEmbedding = Stopwatch.StartNew();
        var embedding = await _dbService.GetEmbeddingAsync(question);
        swEmbedding.Stop();

        // Step 2: Retrieval
        var swRetrieval = Stopwatch.StartNew();
        var results = await _dbService.SearchByEmbeddingAsync(embedding, topK);
        swRetrieval.Stop();

        // Step 3: LLM
        var context = string.Join("\n\n", results.Select(r => r.ChunkText));
        var prompt = $"""
                      ## İLGİLİ DOKÜMANTASYON
                      {context}

                      ## SORU
                      {question}
                      """;

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(SystemPrompt),
            new UserChatMessage(prompt)
        };

        var swLlm = Stopwatch.StartNew();
        var response = await _chatClient.CompleteChatAsync(messages, new ChatCompletionOptions
        {
            Temperature = 0.3f,
            MaxOutputTokenCount = 1000
        });
        swLlm.Stop();
        
        var benchmark = new BenchmarkResult(
            EmbeddingMs: Math.Round(swEmbedding.Elapsed.TotalMilliseconds, 2),
            RetrievalMs: Math.Round(swRetrieval.Elapsed.TotalMilliseconds, 2),
            LlmMs: Math.Round(swLlm.Elapsed.TotalMilliseconds, 2)
        );

        LogResult(question, benchmark);

        return new RagResponse(
            Answer: response.Value.Content[0].Text,
            Sources: results,
            Benchmark: new BenchmarkResult(
                EmbeddingMs: Math.Round(swEmbedding.Elapsed.TotalMilliseconds, 2),
                RetrievalMs: Math.Round(swRetrieval.Elapsed.TotalMilliseconds, 2),
                LlmMs: Math.Round(swLlm.Elapsed.TotalMilliseconds, 2)
            )
        );
    }
    
    private static readonly string LogFile = Path.Combine(AppContext.BaseDirectory, "benchmark_results.txt");

    private void LogResult(string question, BenchmarkResult benchmark)
    {
        var separator = new string('=', 60);
        var line = $"\n{separator}\n" +
                   $"[{DateTime.Now:HH:mm:ss}] {question}\n" +
                   $"  Embedding : {benchmark.EmbeddingMs} ms\n" +
                   $"  Retrieval : {benchmark.RetrievalMs} ms\n" +
                   $"  LLM       : {benchmark.LlmMs} ms\n" +
                   $"  Total     : {benchmark.TotalMs} ms\n";

        File.AppendAllText(LogFile, line);
    }
}

public record RagResponse(string Answer, List<SearchResult> Sources, BenchmarkResult Benchmark);

public record BenchmarkResult(double EmbeddingMs, double RetrievalMs, double LlmMs)
{
    public double TotalMs => Math.Round(EmbeddingMs + RetrievalMs + LlmMs, 2);
}