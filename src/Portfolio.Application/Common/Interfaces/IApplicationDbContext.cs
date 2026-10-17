using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Common.Interfaces;

/// <summary>
/// Application katmanının veritabanına eriştiği soyutlama. Somut uygulaması
/// Infrastructure katmanındaki ApplicationDbContext'tir; böylece iş mantığı
/// PostgreSQL'e veya Identity'ye doğrudan bağımlı olmaz.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<SiteProfile> SiteProfiles { get; }

    DbSet<Project> Projects { get; }

    DbSet<Category> Categories { get; }

    DbSet<Skill> Skills { get; }

    DbSet<Experience> Experiences { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
