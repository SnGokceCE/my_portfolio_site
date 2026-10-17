using Portfolio.Domain.Common;

namespace Portfolio.Domain.Entities;

/// <summary>
/// Projeleri gruplayan, kullanıcının kendi tanımladığı kategori
/// (ör. "Web", "Mobil", "Tasarım", "Veri Bilimi").
/// </summary>
public sealed class Category : BaseEntity
{
    public const int NameMaxLength = 60;

    private readonly List<Project> _projects = [];

    private Category()
    {
        Name = string.Empty;
        Slug = string.Empty;
    }

    public Category(string name, string? slug = null, int sortOrder = 0) : this()
    {
        Update(name, slug, sortOrder);
    }

    public string Name { get; private set; }

    public string Slug { get; private set; }

    public int SortOrder { get; private set; }

    public IReadOnlyCollection<Project> Projects => _projects;

    public string? NameEn { get; private set; }

    public void SetEnglishName(string? nameEn) => NameEn = Guard.Optional(nameEn, nameof(NameEn), NameMaxLength);

    public void Update(string name, string? slug, int sortOrder)
    {
        Name = Guard.Required(name, nameof(Name), NameMaxLength);
        Slug = Common.Slug.FromInputOrText(slug, Name);
        SortOrder = sortOrder;
    }
}
