using DocQA.Api;
using DocQA.Api.Services;
using Xunit;

namespace DocQA.Tests;

public class ChunkerTests
{
    [Fact]
    public void EmptyText_ReturnsNoChunks() => Assert.Empty(Chunker.Split("   "));

    [Fact]
    public void ShortParagraphs_AreMergedUpToTheLimit()
    {
        var chunks = Chunker.Split("One.\n\nTwo.\n\nThree.", 100);
        Assert.Single(chunks);
        Assert.Equal("One. Two. Three.", chunks[0]);
    }

    [Fact]
    public void LongText_NeverExceedsTheLimit()
    {
        var text = string.Join(" ", Enumerable.Repeat("This is a sentence.", 100));
        var chunks = Chunker.Split(text, 200);
        Assert.True(chunks.Count > 1);
        Assert.All(chunks, c => Assert.True(c.Length <= 200));
    }
}

public class RetrieverTests
{
    private static Chunk C(string title, string text) => new(Guid.NewGuid(), title, 0, text);

    [Fact]
    public void RanksTheRelevantChunkFirst()
    {
        var chunks = new[] { C("Shipping", "Standard shipping takes 3 to 5 days."), C("Refunds", "Refunds are issued within 5 business days.") };
        var hits = new Retriever().Search(chunks, "how long do refunds take", 3);
        Assert.Equal("Refunds", hits[0].Chunk.DocumentTitle);
    }

    [Fact]
    public void NoMatchingTerms_ReturnsNothing()
    {
        var hits = new Retriever().Search(new[] { C("A", "Apples and pears.") }, "guitars", 3);
        Assert.Empty(hits);
    }

    [Fact]
    public void TopK_IsRespected()
    {
        var chunks = Enumerable.Range(0, 5).Select(i => C($"D{i}", "refund policy text")).ToList();
        Assert.Equal(2, new Retriever().Search(chunks, "refund", 2).Count);
    }
}

public class GeneratorTests
{
    [Fact]
    public async Task Extractive_WithNoContext_SaysItCannotAnswer()
    {
        var answer = await new ExtractiveGenerator().AnswerAsync("anything", Array.Empty<RetrievedChunk>(), default);
        Assert.Contains("could not find", answer);
    }

    [Fact]
    public async Task Extractive_CitesTheSource()
    {
        var ctx = new[] { new RetrievedChunk(new Chunk(Guid.NewGuid(), "Refunds", 0, "Refunds take 5 business days. Unrelated text."), 1.0) };
        var answer = await new ExtractiveGenerator().AnswerAsync("how long do refunds take", ctx, default);
        Assert.Contains("[1]", answer);
        Assert.Contains("5 business days", answer);
    }
}
