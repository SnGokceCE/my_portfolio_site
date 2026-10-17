using FluentValidation;
using Portfolio.Application.Common.Validation;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Experiences;

public sealed record ExperienceListItem(
    Guid Id,
    ExperienceType Type,
    string Title,
    string Organization,
    string? Location,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Description,
    string? Url);

public sealed class ExperienceInput
{
    public ExperienceType Type { get; set; } = ExperienceType.Work;

    public string Title { get; set; } = string.Empty;

    public string Organization { get; set; } = string.Empty;

    public string? Location { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Description { get; set; }

    public string? Url { get; set; }

    public int SortOrder { get; set; }
}

public sealed class ExperienceInputValidator : AbstractValidator<ExperienceInput>
{
    public ExperienceInputValidator()
    {
        RuleFor(x => x.Type).IsInEnum().WithName("Tür");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(Experience.TitleMaxLength).WithName("Başlık");
        RuleFor(x => x.Organization).NotEmpty().MaximumLength(Experience.OrganizationMaxLength).WithName("Kurum");
        RuleFor(x => x.Location).MaximumLength(Experience.LocationMaxLength).WithName("Konum");
        RuleFor(x => x.StartDate).NotNull().WithName("Başlangıç tarihi");
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.StartDate is not null && x.EndDate is not null)
            .WithMessage("Bitiş tarihi başlangıç tarihinden önce olamaz.");
        RuleFor(x => x.Description).MaximumLength(Experience.DescriptionMaxLength).WithName("Açıklama");
        RuleFor(x => x.Url).HttpUrl().WithName("Bağlantı");
    }
}
