using System.Text.RegularExpressions;

namespace DocQA.Api.Services;

/// <summary>Splits text into paragraph-based chunks of roughly maxChars characters.</summary>
public static class Chunker
{
    public static IReadOnlyList<string> Split(string text, int maxChars = 600)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
        var paragraphs = Regex.Split(text.Replace("\r\n", "\n"), @"\n\s*\n")
            .Select(p => p.Trim()).Where(p => p.Length > 0);

        var chunks = new List<string>();
        var current = new System.Text.StringBuilder();

        void Flush()
        {
            if (current.Length > 0) { chunks.Add(current.ToString().Trim()); current.Clear(); }
        }

        foreach (var paragraph in paragraphs)
        {
            foreach (var piece in BreakLong(paragraph, maxChars))
            {
                if (current.Length + piece.Length + 1 > maxChars) Flush();
                if (current.Length > 0) current.Append(' ');
                current.Append(piece);
            }
        }
        Flush();
        return chunks;
    }

    private static IEnumerable<string> BreakLong(string paragraph, int maxChars)
    {
        if (paragraph.Length <= maxChars) { yield return paragraph; yield break; }
        var sentences = Regex.Split(paragraph, @"(?<=[.!?])\s+");
        var buffer = new System.Text.StringBuilder();
        foreach (var s in sentences)
        {
            if (buffer.Length + s.Length + 1 > maxChars && buffer.Length > 0)
            {
                yield return buffer.ToString();
                buffer.Clear();
            }
            // A single sentence longer than the limit is hard-cut.
            var rest = s;
            while (rest.Length > maxChars)
            {
                yield return rest[..maxChars];
                rest = rest[maxChars..];
            }
            if (buffer.Length > 0) buffer.Append(' ');
            buffer.Append(rest);
        }
        if (buffer.Length > 0) yield return buffer.ToString();
    }
}
