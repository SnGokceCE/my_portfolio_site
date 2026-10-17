using FluentValidation;
using Portfolio.Application.Common.Validation;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.SiteProfiles;

public sealed class SiteProfileInput
{
    public string FullName { get; set; } = string.Empty;

    public string Headline { get; set; } = string.Empty;

    public string? Bio { get; set; }

    public string? Location { get; set; }

    public string? Email { get; set; }

    public string? GitHubUrl { get; set; }

    public string? LinkedInUrl { get; set; }

    public bool IsOpenToWork { get; set; }

    public string? HeadlineEn { get; set; }

    public string? BioEn { get; set; }
}

public sealed class SiteProfileInputValidator : AbstractValidator<SiteProfileInput>
{
    public SiteProfileInputValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(SiteProfile.FullNameMaxLength).WithName("Ad soyad");
        RuleFor(x => x.Headline).NotEmpty().MaximumLength(SiteProfile.HeadlineMaxLength).WithName("Unvan");
        RuleFor(x => x.Bio).MaximumLength(SiteProfile.BioMaxLength).WithName("Biyografi");
        RuleFor(x => x.Location).MaximumLength(SiteProfile.FullNameMaxLength).WithName("Konum");
        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Geçerli bir e-posta adresi gir.")
            .MaximumLength(SiteProfile.UrlMaxLength)
            .WithName("E-posta");
        RuleFor(x => x.HeadlineEn).MaximumLength(SiteProfile.HeadlineMaxLength).WithName("Unvan (EN)");
        RuleFor(x => x.BioEn).MaximumLength(SiteProfile.BioMaxLength).WithName("Biyografi (EN)");
        RuleFor(x => x.GitHubUrl).HttpUrl().WithName("GitHub adresi");
        RuleFor(x => x.LinkedInUrl).HttpUrl().WithName("LinkedIn adresi");
    }
}
