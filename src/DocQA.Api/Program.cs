using DocQA.Api;
using DocQA.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DocumentStore>();
builder.Services.AddSingleton<Retriever>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IAnswerGenerator>(sp =>
{
    var key = Environment.GetEnvironmentVariable("LLM_API_KEY");
    if (string.IsNullOrWhiteSpace(key)) return new ExtractiveGenerator();
    var baseUrl = Environment.GetEnvironmentVariable("LLM_BASE_URL") ?? "https://api.openai.com/v1";
    var model = Environment.GetEnvironmentVariable("LLM_MODEL") ?? "gpt-4o-mini";
    return new ChatCompletionsGenerator(sp.GetRequiredService<IHttpClientFactory>().CreateClient(), baseUrl, key, model,
        sp.GetRequiredService<ILogger<ChatCompletionsGenerator>>());
});

var app = builder.Build();

// Optional shared-secret protection: set DEMO_API_KEY to require an X-API-Key header.
var demoKey = Environment.GetEnvironmentVariable("DEMO_API_KEY");
app.Use(async (ctx, next) =>
{
    if (!string.IsNullOrEmpty(demoKey) && ctx.Request.Path != "/health" &&
        ctx.Request.Headers["X-API-Key"] != demoKey)
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await ctx.Response.WriteAsJsonAsync(new { error = "Missing or invalid X-API-Key." });
        return;
    }
    await next();
});

const int MaxTextChars = 200_000;

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/documents", (DocumentStore store) => Results.Ok(store.Documents()));

app.MapPost("/documents", (AddDocumentRequest req, DocumentStore store) =>
{
    if (string.IsNullOrWhiteSpace(req.Title) || string.IsNullOrWhiteSpace(req.Text))
        return Results.BadRequest(new { error = "Both title and text are required." });
    if (req.Text.Length > MaxTextChars)
        return Results.BadRequest(new { error = $"Text is limited to {MaxTextChars} characters." });
    var doc = store.Add(req.Title.Trim(), req.Text);
    return Results.Created($"/documents/{doc.Id}", doc);
});

app.MapDelete("/documents/{id:guid}", (Guid id, DocumentStore store) =>
    store.Remove(id) ? Results.NoContent() : Results.NotFound());

app.MapPost("/ask", async (AskRequest req, DocumentStore store, Retriever retriever, IAnswerGenerator generator, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(req.Question))
        return Results.BadRequest(new { error = "A question is required." });
    if (req.Question.Length > 1000)
        return Results.BadRequest(new { error = "Questions are limited to 1000 characters." });

    var hits = retriever.Search(store.Chunks(), req.Question, req.TopK ?? 3);
    var answer = await generator.AnswerAsync(req.Question, hits, ct);
    var citations = hits.Select((h, i) => new Citation(i + 1, h.Chunk.DocumentTitle, h.Chunk.Index, h.Score,
        h.Chunk.Text.Length > 160 ? h.Chunk.Text[..160] + "..." : h.Chunk.Text)).ToList();
    return Results.Ok(new AskResponse(answer, generator.Mode, citations));
});

app.Run();

public partial class Program { }
