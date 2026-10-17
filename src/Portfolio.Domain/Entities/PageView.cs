using Portfolio.Domain.Common;

namespace Portfolio.Domain.Entities;

public enum DeviceType
{
    Desktop,
    Mobile,
    Tablet
}

/// <summary>
/// Gizlilik dostu sayfa görüntüleme kaydı. IP adresi ya da çerez saklanmaz;
/// <see cref="VisitorHash"/> her gün değişen bir anahtarla üretildiği için kişi günler arasında izlenemez.
/// </summary>
public sealed class PageView : BaseEntity
{
    public const int PathMaxLength = 300;
    public const int ReferrerMaxLength = 100;
    public const int VisitorHashLength = 16;

    private PageView()
    {
        Path = string.Empty;
        VisitorHash = string.Empty;
        Language = "tr";
    }

    public PageView(string path, string? referrerHost, string visitorHash, DeviceType device, string language, DateOnly day, DateTimeOffset occurredAt)
        : this()
    {
        Path = Guard.MaxLength(string.IsNullOrEmpty(path) ? "/" : path, nameof(Path), PathMaxLength);
        ReferrerHost = Guard.Optional(referrerHost, nameof(ReferrerHost), ReferrerMaxLength);
        VisitorHash = visitorHash.Length == VisitorHashLength
            ? visitorHash
            : throw new DomainException($"Ziyaretçi özeti {VisitorHashLength} karakter olmalı.");
        Device = device;
        Language = language;
        Day = day;
        OccurredAt = occurredAt;
    }

    public string Path { get; private set; }

    /// <summary>Ziyaretçinin geldiği sitenin yalnızca alan adı (ör. "linkedin.com"); tam adres saklanmaz.</summary>
    public string? ReferrerHost { get; private set; }

    public string VisitorHash { get; private set; }

    public DeviceType Device { get; private set; }

    public string Language { get; private set; }

    /// <summary>Raporlama için yerel saat dilimine göre gün.</summary>
    public DateOnly Day { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }
}
