using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.Common.Models;
using Portfolio.Application.Common.Validation;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.SiteProfiles;

internal sealed class SiteProfileService(
    IApplicationDbContext db,
    IValidator<SiteProfileInput> validator,
    ICurrentLanguage language) : ISiteProfileService
{
    public async Task<SiteProfileDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var english = language.IsEnglish;
        var profile = await db.SiteProfiles
            .AsNoTracking()
            .OrderBy(p => p.CreatedAt)
            .Select(p => new SiteProfileDto(
                p.FullName,
                english && p.HeadlineEn != null ? p.HeadlineEn : p.Headline,
                english && p.BioEn != null ? p.BioEn : p.Bio,
                p.Location,
                p.Email,
                p.GitHubUrl,
                p.LinkedInUrl,
                p.IsOpenToWork))
            .FirstOrDefaultAsync(cancellationToken);

        return profile ?? SiteProfileDto.Empty;
    }

    public async Task<SiteProfileInput> GetForEditAsync(CancellationToken cancellationToken = default)
    {
        // Düzenleme formu her iki dilin ham değerlerini görmeli; dile göre seçim yapan GetAsync kullanılmaz.
        var p = await db.SiteProfiles.AsNoTracking().OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (p is null)
        {
            var empty = SiteProfileDto.Empty;
            return new SiteProfileInput { FullName = empty.FullName, Headline = empty.Headline };
        }

        return new SiteProfileInput
        {
            FullName = p.FullName,
            Headline = p.Headline,
            Bio = p.Bio,
            Location = p.Location,
            Email = p.Email,
            GitHubUrl = p.GitHubUrl,
            LinkedInUrl = p.LinkedInUrl,
            IsOpenToWork = p.IsOpenToWork,
            HeadlineEn = p.HeadlineEn,
            BioEn = p.BioEn
        };
    }

    public async Task<Result> UpdateAsync(SiteProfileInput input, CancellationToken cancellationToken = default)
    {
        var errors = await validator.ValidateInputAsync(input, cancellationToken);
        if (errors is not null)
        {
            return Result.Invalid(errors);
        }

        var profile = await db.SiteProfiles.OrderBy(p => p.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        var isNew = profile is null;

        errors = ValidationExtensions.TryApply(() =>
        {
            profile ??= new SiteProfile(input.FullName, input.Headline, input.Bio ?? string.Empty);
            profile.UpdateIdentity(input.FullName, input.Headline, input.Bio);
            profile.UpdateContact(input.Location, input.Email, input.GitHubUrl, input.LinkedInUrl);
            profile.SetOpenToWork(input.IsOpenToWork);
            profile.UpdateEnglish(input.HeadlineEn, input.BioEn);
        });
        if (errors is not null)
        {
            return Result.Invalid(errors);
        }

        if (isNew)
        {
            db.SiteProfiles.Add(profile!);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
