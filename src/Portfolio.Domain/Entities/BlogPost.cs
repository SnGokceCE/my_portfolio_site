using Portfolio.Domain.Common;

namespace Portfolio.Domain.Entities;

/// <summary>Markdown ile yazılan blog yazısı. Taslak olarak oluşturulur.</summary>
public sealed class BlogPost : BaseEntity
{
    public const int TitleMaxLength = 150;
    public const int ExcerptMaxLength = 300;
    public const int BodyMaxLength = 100_000;
    public const int MaxTags = 8;
    public const int TagMaxLength = 30;
    public const int WordsPerMinute = 200;

    private BlogPost()
    {
        Title = string.Empty;
        Slug = string.Empty;
        Excerpt = string.Empty;
        Body = string.Empty;
    }

    public BlogPost(string title, string excerpt, string body, string? slug = null) : this()
    {
        Update(title, slug, excerpt, body);
    }

    public string Title { get; private set; }

    public string Slug { get; private set; }

    /// <summary>Listelerde ve paylaşım önizlemelerinde görünen kısa giriş.</summary>
    public string Excerpt { get; private set; }

    public string Body { get; private set; }

    public string? CoverImageUrl { get; private set; }

    public string[] Tags { get; private set; } = [];

    /// <summary>Listelerde gövdeyi yüklemeden gösterebilmek için içerik değiştikçe hesaplanır.</summary>
    public int ReadingMinutes { get; private set; } = 1;

    public bool IsPublished { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public void Update(string title, string? slug, string excerpt, string body)
    {
        Title = Guard.Required(title, nameof(Title), TitleMaxLength);
        Slug = Common.Slug.FromInputOrText(slug, Title);
        Excerpt = Guard.Required(excerpt, nameof(Excerpt), ExcerptMaxLength);
        Body = Guard.Required(body, nameof(Body), BodyMaxLength);
        ReadingMinutes = EstimateReadingMinutes(Body);
    }

    public static int EstimateReadingMinutes(string text)
    {
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, (int)Math.Ceiling(words / (double)WordsPerMinute));
    }

    /// <summary>Etiketleri sadeleştirir: boşlukları kırpar, tekrarları (büyük/küçük harf duyarsız) atar.</summary>
    public void SetTags(IEnumerable<string> tags)
    {
        var normalized = NormalizeTags(tags);
        if (normalized.Length > MaxTags)
        {
            throw new DomainException($"En fazla {MaxTags} etiket eklenebilir.");
        }

        foreach (var tag in normalized)
        {
            Guard.MaxLength(tag, "Etiket", TagMaxLength);
        }

        Tags = normalized;
    }

    public void SetCoverImage(string? url) => CoverImageUrl = url;

    public void Publish(DateTimeOffset now)
    {
        IsPublished = true;
        PublishedAt ??= now;
    }

    public void Unpublish() => IsPublished = false;

    /// <summary>"C#, Web API ,c#" gibi virgülle ayrılmış girişi de kabul eden yardımcı.</summary>
    public static string[] NormalizeTags(IEnumerable<string> tags) =>
        tags
            .SelectMany(t => t.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(t => t.Length > 0)
            .DistinctBy(t => t.ToUpperInvariant())
            .ToArray();
}
