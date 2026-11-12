namespace DocQA.Api;

public record Document(Guid Id, string Title, DateTime AddedUtc);
public record Chunk(Guid DocumentId, string DocumentTitle, int Index, string Text);
public record AddDocumentRequest(string? Title, string? Text);
public record AskRequest(string? Question, int? TopK);
public record Citation(int Number, string DocumentTitle, int ChunkIndex, double Score, string Excerpt);
public record AskResponse(string Answer, string Mode, IReadOnlyList<Citation> Citations);
public record RetrievedChunk(Chunk Chunk, double Score);
