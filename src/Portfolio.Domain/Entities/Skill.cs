using Portfolio.Domain.Common;

namespace Portfolio.Domain.Entities;

/// <summary>
/// Bir yetenek veya teknoloji (ör. "C#", "Figma", "PostgreSQL").
/// Projelere "kullanılan teknolojiler" olarak bağlanabilir.
/// </summary>
public sealed class Skill : BaseEntity
{
    public const int NameMaxLength = 50;
    public const int GroupMaxLength = 50;
    public const int MinLevel = 1;
    public const int MaxLevel = 5;

    private readonly List<Project> _projects = [];

    private Skill()
    {
        Name = string.Empty;
    }

    public Skill(string name, string? group = null, int level = 3, int sortOrder = 0) : this()
    {
        Update(name, group, level, sortOrder);
    }

    public string Name { get; private set; }

    /// <summary>Yetenekleri gruplamak için serbest metin (ör. "Backend", "Tasarım").</summary>
    public string? Group { get; private set; }

    /// <summary>1 (başlangıç) ile 5 (uzman) arası öz değerlendirme.</summary>
    public int Level { get; private set; }

    public int SortOrder { get; private set; }

    public IReadOnlyCollection<Project> Projects => _projects;

    /// <summary>İngilizce sayfada gösterilen ad (ör. "Makine öğrenmesi" → "Machine learning"). Boşsa Türkçe ad kullanılır.</summary>
    public string? NameEn { get; private set; }

    public string? GroupEn { get; private set; }

    public void UpdateEnglish(string? nameEn, string? groupEn)
    {
        NameEn = Guard.Optional(nameEn, nameof(NameEn), NameMaxLength);
        GroupEn = Guard.Optional(groupEn, nameof(GroupEn), GroupMaxLength);
    }

    public void Update(string name, string? group, int level, int sortOrder)
    {
        Name = Guard.Required(name, nameof(Name), NameMaxLength);
        Group = Guard.Optional(group, nameof(Group), GroupMaxLength);
        Level = Guard.Range(level, nameof(Level), MinLevel, MaxLevel);
        SortOrder = sortOrder;
    }
}
