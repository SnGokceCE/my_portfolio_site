using Portfolio.Domain.Common;

namespace Portfolio.Domain.Entities;

/// <summary>
/// Zaman çizelgesindeki bir kayıt: iş, staj, eğitim, gönüllülük veya sertifika.
/// </summary>
public sealed class Experience : BaseEntity
{
    public const int TitleMaxLength = 120;
    public const int OrganizationMaxLength = 120;
    public const int LocationMaxLength = 100;
    public const int DescriptionMaxLength = 4000;

    private Experience()
    {
        Title = string.Empty;
        Organization = string.Empty;
    }

    public Experience(ExperienceType type, string title, string organization, DateOnly startDate) : this()
    {
        Update(type, title, organization, null, startDate, null, null, null, 0);
    }

    public ExperienceType Type { get; private set; }

    /// <summary>Pozisyon, bölüm ya da sertifika adı.</summary>
    public string Title { get; private set; }

    /// <summary>Şirket, okul ya da kurum adı.</summary>
    public string Organization { get; private set; }

    public string? Location { get; private set; }

    public DateOnly StartDate { get; private set; }

    /// <summary>Boşsa deneyim hâlâ devam ediyor demektir.</summary>
    public DateOnly? EndDate { get; private set; }

    public string? Description { get; private set; }

    public string? Url { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsCurrent => EndDate is null;

    public void Update(
        ExperienceType type,
        string title,
        string organization,
        string? location,
        DateOnly startDate,
        DateOnly? endDate,
        string? description,
        string? url,
        int sortOrder)
    {
        if (!Enum.IsDefined(type))
        {
            throw new DomainException("Geçersiz deneyim türü.");
        }

        Guard.DateRange(startDate, endDate, nameof(EndDate));

        Type = type;
        Title = Guard.Required(title, nameof(Title), TitleMaxLength);
        Organization = Guard.Required(organization, nameof(Organization), OrganizationMaxLength);
        Location = Guard.Optional(location, nameof(Location), LocationMaxLength);
        StartDate = startDate;
        EndDate = endDate;
        Description = Guard.Optional(description, nameof(Description), DescriptionMaxLength);
        Url = Guard.OptionalUrl(url, nameof(Url));
        SortOrder = sortOrder;
    }
}
