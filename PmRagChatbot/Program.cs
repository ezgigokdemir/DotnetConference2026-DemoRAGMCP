using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Npgsql;
using PmRagChatbot.Services;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;
var endpoint = config["AzureOpenAI:Endpoint"]!;
var apiKey = config["AzureOpenAI:ApiKey"]!;
var embedDeployment = config["AzureOpenAI:EmbeddingDeployment"]!;
var chatDeployment = config["AzureOpenAI:ChatDeployment"]!;
var connectionString = config["Database:ConnectionString"]!;

// Semantic Kernel — ITextEmbeddingGenerationService
var skKernel = Kernel.CreateBuilder()
    .AddAzureOpenAITextEmbeddingGeneration(embedDeployment, endpoint, apiKey)
    .Build();

var embeddingService = skKernel.GetRequiredService<ITextEmbeddingGenerationService>();

// Npgsql — pgvector binary protocol
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.UseVector();
var dataSource = dataSourceBuilder.Build();

// DI registrations
builder.Services.AddSingleton(embeddingService);
builder.Services.AddSingleton(dataSource);
builder.Services.AddSingleton<DbService>();
builder.Services.AddSingleton<RagService>();
builder.Services.AddControllers();

var app = builder.Build();

// Setup table and seed data on startup
var dbService = app.Services.GetRequiredService<DbService>();
await dbService.SetupTableAsync();
await dbService.SeedAsync();

app.MapControllers();
app.Run();