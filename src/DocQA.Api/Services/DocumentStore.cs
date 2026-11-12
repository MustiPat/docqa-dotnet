namespace DocQA.Api.Services;

/// <summary>Thread-safe in-memory store. Swap for a database or vector store in production.</summary>
public class DocumentStore
{
    private readonly object _lock = new();
    private readonly List<Document> _documents = new();
    private readonly List<Chunk> _chunks = new();

    public Document Add(string title, string text)
    {
        var doc = new Document(Guid.NewGuid(), title, DateTime.UtcNow);
        var pieces = Chunker.Split(text);
        lock (_lock)
        {
            _documents.Add(doc);
            for (int i = 0; i < pieces.Count; i++) _chunks.Add(new Chunk(doc.Id, title, i, pieces[i]));
        }
        return doc;
    }

    public IReadOnlyList<Document> Documents() { lock (_lock) return _documents.ToList(); }
    public IReadOnlyList<Chunk> Chunks() { lock (_lock) return _chunks.ToList(); }

    public bool Remove(Guid id)
    {
        lock (_lock)
        {
            var removed = _documents.RemoveAll(d => d.Id == id) > 0;
            _chunks.RemoveAll(c => c.DocumentId == id);
            return removed;
        }
    }
}
