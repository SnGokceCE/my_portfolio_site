using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.Common.Models;
using Portfolio.Application.Common.Validation;
using Portfolio.Domain.Common;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Projects;

public interface IProjectService
{
    Task<PagedList<ProjectListItem>> ListAsync(ProjectQuery query, CancellationToken cancellationToken = default);

    Task<ProjectEditModel?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateAsync(ProjectInput input, ImageUpload? cover, CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(
        Guid id, ProjectInput input, ImageUpload? cover, bool removeCover, CancellationToken cancellationToken = default);

    Task<Result> SetPublishedAsync(Guid id, bool published, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

internal sealed class ProjectService(
    IApplicationDbContext db,
    IValidator<ProjectInput> validator,
    IFileStorage storage,
    TimeProvider clock) : IProjectService
{
    private const string CoverFolder = "projects";

    public async Task<PagedList<ProjectListItem>> ListAsync(ProjectQuery query, CancellationToken cancellationToken = default)
    {
        var projects = db.Projects.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Bu ifade SQL'e (lower(...) LIKE ...) çevrilir; kültür/StringComparison uyarıları burada geçersiz.
#pragma warning disable CA1304, CA1311, CA1862
            var term = query.Search.Trim().ToLower();
            projects = projects.Where(p => p.Title.ToLower().Contains(term) || p.Summary.ToLower().Contains(term));
#pragma warning restore CA1304, CA1311, CA1862
        }

        if (query.CategoryId is not null)
        {
            projects = projects.Where(p => p.CategoryId == query.CategoryId);
        }

        projects = query.Status switch
        {
            ProjectStatusFilter.Published => projects.Where(p => p.IsPublished),
            ProjectStatusFilter.Draft => projects.Where(p => !p.IsPublished),
            _ => projects
        };

        return await projects
            .OrderBy(p => p.SortOrder)
            .ThenByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .Select(p => new ProjectListItem(
                p.Id,
                p.Title,
                p.Slug,
                p.Category != null ? p.Category.Name : null,
                p.CoverImageUrl,
                p.IsPublished,
                p.IsFeatured,
                p.Skills.Count,
                p.UpdatedAt ?? p.CreatedAt))
            .ToPagedListAsync(query.Page, query.PageSize, cancellationToken);
    }

    public async Task<ProjectEditModel?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await db.Projects
            .AsNoTracking()
            .Include(p => p.Skills)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (project is null)
        {
            return null;
        }

        var input = new ProjectInput
        {
            Title = project.Title,
            Slug = project.Slug,
            Summary = project.Summary,
            Body = project.Body,
            CategoryId = project.CategoryId,
            SkillIds = project.Skills.Select(s => s.Id).ToList(),
            RepositoryUrl = project.RepositoryUrl,
            LiveUrl = project.LiveUrl,
            StartedOn = project.StartedOn,
            CompletedOn = project.CompletedOn,
            IsFeatured = project.IsFeatured,
            IsPublished = project.IsPublished,
            SortOrder = project.SortOrder
        };

        return new ProjectEditModel(input, project.CoverImageUrl, project.PublishedAt);
    }

    public async Task<Result<Guid>> CreateAsync(ProjectInput input, ImageUpload? cover, CancellationToken cancellationToken = default)
    {
        var errors = await ValidateAsync(null, input, cancellationToken);
        if (errors is not null)
        {
            return Result.Invalid<Guid>(errors);
        }

        var skills = await LoadSkillsAsync(input.SkillIds, cancellationToken);
        Project? project = null;
        errors = ValidationExtensions.TryApply(() =>
        {
            project = new Project(input.Title, input.Summary, input.Slug);
            Apply(project, input, skills);
        });
        if (errors is not null)
        {
            return Result.Invalid<Guid>(errors);
        }

        if (cover is not null)
        {
            var saved = await storage.SaveImageAsync(cover, CoverFolder, cancellationToken);
            if (!saved.Succeeded)
            {
                return Result.Invalid<Guid>(AsCoverErrors(saved));
            }

            project!.SetCoverImage(saved.Value);
        }

        db.Projects.Add(project!);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(project!.Id);
    }

    public async Task<Result> UpdateAsync(
        Guid id, ProjectInput input, ImageUpload? cover, bool removeCover, CancellationToken cancellationToken = default)
    {
        var project = await db.Projects
            .Include(p => p.Skills)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (project is null)
        {
            return Result.NotFound();
        }

        var errors = await ValidateAsync(id, input, cancellationToken);
        if (errors is not null)
        {
            return Result.Invalid(errors);
        }

        var skills = await LoadSkillsAsync(input.SkillIds, cancellationToken);
        errors = ValidationExtensions.TryApply(() => Apply(project, input, skills));
        if (errors is not null)
        {
            return Result.Invalid(errors);
        }

        var previousCover = project.CoverImageUrl;
        if (cover is not null)
        {
            var saved = await storage.SaveImageAsync(cover, CoverFolder, cancellationToken);
            if (!saved.Succeeded)
            {
                return Result.Invalid(AsCoverErrors(saved));
            }

            project.SetCoverImage(saved.Value);
        }
        else if (removeCover)
        {
            project.SetCoverImage(null);
        }

        await db.SaveChangesAsync(cancellationToken);

        // Eski dosya ancak veritabanı başarıyla güncellendikten sonra silinir.
        if (previousCover is not null && previousCover != project.CoverImageUrl)
        {
            await storage.DeleteAsync(previousCover, cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result> SetPublishedAsync(Guid id, bool published, CancellationToken cancellationToken = default)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (project is null)
        {
            return Result.NotFound();
        }

        if (published)
        {
            project.Publish(clock.GetUtcNow());
        }
        else
        {
            project.Unpublish();
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (project is null)
        {
            return Result.NotFound();
        }

        var cover = project.CoverImageUrl;
        db.Projects.Remove(project);
        await db.SaveChangesAsync(cancellationToken);
        await storage.DeleteAsync(cover, cancellationToken);
        return Result.Success();
    }

    private static List<Error> AsCoverErrors(Result result) =>
        result.Errors.Select(e => e with { Field = ProjectFormFields.Cover }).ToList();

    private void Apply(Project project, ProjectInput input, IReadOnlyList<Skill> skills)
    {
        project.UpdateDetails(input.Title, input.Slug, input.Summary, input.Body);
        project.UpdateLinks(input.RepositoryUrl, input.LiveUrl);
        project.UpdateTimeline(input.StartedOn, input.CompletedOn);
        project.UpdateDisplay(input.CategoryId, input.IsFeatured, input.SortOrder);
        project.SetSkills(skills);

        if (input.IsPublished)
        {
            project.Publish(clock.GetUtcNow());
        }
        else
        {
            project.Unpublish();
        }
    }

    private async Task<IReadOnlyList<Skill>> LoadSkillsAsync(IList<Guid> ids, CancellationToken cancellationToken) =>
        ids.Count == 0
            ? []
            : await db.Skills.Where(s => ids.Contains(s.Id)).ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<Error>?> ValidateAsync(Guid? id, ProjectInput input, CancellationToken cancellationToken)
    {
        var errors = await validator.ValidateInputAsync(input, cancellationToken);
        if (errors is not null)
        {
            return errors;
        }

        var problems = new List<Error>();

        var slug = Slug.Generate(string.IsNullOrWhiteSpace(input.Slug) ? input.Title : input.Slug);
        if (await db.Projects.AnyAsync(p => p.Slug == slug && p.Id != id, cancellationToken))
        {
            problems.Add(new Error($"'{slug}' adresi başka bir projede kullanılıyor.", nameof(ProjectInput.Slug)));
        }

        if (input.CategoryId is { } categoryId &&
            !await db.Categories.AnyAsync(c => c.Id == categoryId, cancellationToken))
        {
            problems.Add(new Error("Seçilen kategori bulunamadı.", nameof(ProjectInput.CategoryId)));
        }

        return problems.Count == 0 ? null : problems;
    }
}
