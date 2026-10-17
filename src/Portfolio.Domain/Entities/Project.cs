using Portfolio.Domain.Common;

namespace Portfolio.Domain.Entities;

/// <summary>
/// Portfolyoda sergilenen bir çalışma. Taslak olarak oluşturulur,
/// yayımlanana kadar ziyaretçilere görünmez.
/// </summary>
public sealed class Project : BaseEntity
{
    public const int TitleMaxLength = 120;
    public const int SummaryMaxLength = 300;
    public const int BodyMaxLength = 50_000;

    private readonly List<Skill> _skills = [];

    private Project()
    {
        Title = string.Empty;
        Slug = string.Empty;
        Summary = string.Empty;
    }

    public Project(string title, string summary, string? slug = null) : this()
    {
        UpdateDetails(title, slug, summary, null);
    }

    public string Title { get; private set; }

    public string Slug { get; private set; }

    /// <summary>Kartlarda ve arama sonuçlarında gösterilen kısa açıklama.</summary>
    public string Summary { get; private set; }

    /// <summary>Detay sayfasının Markdown içeriği.</summary>
    public string? Body { get; private set; }

    public string? CoverImageUrl { get; private set; }

    public string? RepositoryUrl { get; private set; }

    public string? LiveUrl { get; private set; }

    public DateOnly? StartedOn { get; private set; }

    public DateOnly? CompletedOn { get; private set; }

    public bool IsFeatured { get; private set; }

    public bool IsPublished { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public int SortOrder { get; private set; }

    public Guid? CategoryId { get; private set; }

    public Category? Category { get; private set; }

    public IReadOnlyCollection<Skill> Skills => _skills;

    public void UpdateDetails(string title, string? slug, string summary, string? body)
    {
        Title = Guard.Required(title, nameof(Title), TitleMaxLength);
        Slug = Common.Slug.FromInputOrText(slug, Title);
        Summary = Guard.Required(summary, nameof(Summary), SummaryMaxLength);
        Body = Guard.Optional(body, nameof(Body), BodyMaxLength);
    }

    public void UpdateLinks(string? repositoryUrl, string? liveUrl)
    {
        RepositoryUrl = Guard.OptionalUrl(repositoryUrl, nameof(RepositoryUrl));
        LiveUrl = Guard.OptionalUrl(liveUrl, nameof(LiveUrl));
    }

    public void UpdateTimeline(DateOnly? startedOn, DateOnly? completedOn)
    {
        Guard.DateRange(startedOn, completedOn, nameof(CompletedOn));
        StartedOn = startedOn;
        CompletedOn = completedOn;
    }

    public void UpdateDisplay(Guid? categoryId, bool isFeatured, int sortOrder)
    {
        CategoryId = categoryId;
        IsFeatured = isFeatured;
        SortOrder = sortOrder;
    }

    public void SetCoverImage(string? url) => CoverImageUrl = url;

    public void SetSkills(IEnumerable<Skill> skills)
    {
        _skills.Clear();
        _skills.AddRange(skills.DistinctBy(s => s.Id));
    }

    /// <summary>İlk yayın tarihini korur; tekrar yayımlamak tarihi değiştirmez.</summary>
    public void Publish(DateTimeOffset now)
    {
        IsPublished = true;
        PublishedAt ??= now;
    }

    public void Unpublish() => IsPublished = false;
}
