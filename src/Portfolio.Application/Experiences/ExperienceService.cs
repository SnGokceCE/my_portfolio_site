using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.Common.Models;
using Portfolio.Application.Common.Validation;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Experiences;

public interface IExperienceService
{
    Task<IReadOnlyList<ExperienceListItem>> ListAsync(CancellationToken cancellationToken = default);

    Task<ExperienceInput?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateAsync(ExperienceInput input, CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(Guid id, ExperienceInput input, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

internal sealed class ExperienceService(IApplicationDbContext db, IValidator<ExperienceInput> validator) : IExperienceService
{
    public async Task<IReadOnlyList<ExperienceListItem>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Experiences
            .AsNoTracking()
            // Devam edenler en üstte, sonra en yeniden en eskiye.
            .OrderBy(e => e.EndDate != null)
            .ThenByDescending(e => e.StartDate)
            .ThenBy(e => e.SortOrder)
            .Select(e => new ExperienceListItem(
                e.Id, e.Type, e.Title, e.Organization, e.Location, e.StartDate, e.EndDate, e.Description, e.Url))
            .ToListAsync(cancellationToken);

    public async Task<ExperienceInput?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default) =>
        await db.Experiences
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new ExperienceInput
            {
                Type = e.Type,
                Title = e.Title,
                Organization = e.Organization,
                Location = e.Location,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Description = e.Description,
                Url = e.Url,
                SortOrder = e.SortOrder
            })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<Result<Guid>> CreateAsync(ExperienceInput input, CancellationToken cancellationToken = default)
    {
        var errors = await validator.ValidateInputAsync(input, cancellationToken);
        Experience? experience = null;
        errors ??= ValidationExtensions.TryApply(() =>
        {
            experience = new Experience(input.Type, input.Title, input.Organization, input.StartDate!.Value);
            Apply(experience, input);
        });
        if (errors is not null)
        {
            return Result.Invalid<Guid>(errors);
        }

        db.Experiences.Add(experience!);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(experience!.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, ExperienceInput input, CancellationToken cancellationToken = default)
    {
        var experience = await db.Experiences.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (experience is null)
        {
            return Result.NotFound();
        }

        var errors = await validator.ValidateInputAsync(input, cancellationToken)
                     ?? ValidationExtensions.TryApply(() => Apply(experience, input));
        if (errors is not null)
        {
            return Result.Invalid(errors);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var experience = await db.Experiences.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (experience is null)
        {
            return Result.NotFound();
        }

        db.Experiences.Remove(experience);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static void Apply(Experience experience, ExperienceInput input) =>
        experience.Update(
            input.Type,
            input.Title,
            input.Organization,
            input.Location,
            input.StartDate!.Value,
            input.EndDate,
            input.Description,
            input.Url,
            input.SortOrder);
}
