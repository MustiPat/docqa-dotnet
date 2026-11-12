using System.Text.RegularExpressions;

namespace DocQA.Api.Services;

/// <summary>Keyword retrieval with BM25 scoring. No external services needed.</summary>
public class Retriever
{
    private const double K1 = 1.5, B = 0.75;
    private static readonly HashSet<string> Stop = new(StringComparer.OrdinalIgnoreCase)
    {
        "a","an","the","and","or","of","to","in","on","for","is","are","was","were","be","by","with","as","at","it","this","that","what","how","do","does","can","i","you","we"
    };

    public static IReadOnlyList<string> Tokenize(string text) =>
        Regex.Matches(text.ToLowerInvariant(), @"[a-z0-9]+")
             .Select(m => m.Value).Where(t => t.Length > 1 && !Stop.Contains(t)).ToList();

    public IReadOnlyList<RetrievedChunk> Search(IReadOnlyList<Chunk> chunks, string query, int topK)
    {
        var q = Tokenize(query).Distinct().ToList();
        if (q.Count == 0 || chunks.Count == 0) return Array.Empty<RetrievedChunk>();

        var docs = chunks.Select(c => Tokenize(c.Text)).ToList();
        var avgLen = docs.Average(d => d.Count);
        if (avgLen == 0) avgLen = 1;
        var df = q.ToDictionary(t => t, t => docs.Count(d => d.Contains(t)));

        var results = new List<RetrievedChunk>();
        for (int i = 0; i < chunks.Count; i++)
        {
            double score = 0;
            foreach (var term in q)
            {
                if (df[term] == 0) continue;
                var tf = docs[i].Count(t => t == term);
                if (tf == 0) continue;
                var idf = Math.Log(1 + (docs.Count - df[term] + 0.5) / (df[term] + 0.5));
                score += idf * tf * (K1 + 1) / (tf + K1 * (1 - B + B * docs[i].Count / avgLen));
            }
            if (score > 0) results.Add(new RetrievedChunk(chunks[i], Math.Round(score, 3)));
        }
        return results.OrderByDescending(r => r.Score).Take(Math.Clamp(topK, 1, 10)).ToList();
    }
}
