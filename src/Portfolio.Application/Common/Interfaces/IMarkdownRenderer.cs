namespace Portfolio.Application.Common.Interfaces;

public sealed record MarkdownHeading(int Level, string Text, string Id);

/// <summary>Markdown'dan üretilmiş, güvenli (sanitize edilmiş) HTML ve içindekiler listesi.</summary>
public sealed record RenderedMarkdown(string Html, IReadOnlyList<MarkdownHeading> Headings, int WordCount)
{
    public static RenderedMarkdown Empty { get; } = new(string.Empty, [], 0);

    public bool IsEmpty => Html.Length == 0;

    /// <summary>Dakikada ~200 kelime okuma hızına göre tahmini süre.</summary>
    public int ReadingMinutes => Math.Max(1, (int)Math.Ceiling(WordCount / 200d));
}

public interface IMarkdownRenderer
{
    RenderedMarkdown Render(string? markdown);
}
