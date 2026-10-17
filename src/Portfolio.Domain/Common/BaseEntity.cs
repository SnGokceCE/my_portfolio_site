namespace Portfolio.Domain.Common;

/// <summary>
/// Tüm varlıkların ortak tabanı. Zaman damgaları Infrastructure katmanındaki
/// AuditableEntityInterceptor tarafından otomatik doldurulur.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
