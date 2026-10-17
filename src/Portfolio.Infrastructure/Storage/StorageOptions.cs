namespace Portfolio.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Dosyaların diskte yazılacağı klasör. Göreli ise içerik köküne (content root) göre çözülür.</summary>
    public string UploadsPath { get; set; } = "wwwroot/uploads";

    /// <summary>Dosyaların sunulduğu URL ön eki.</summary>
    public string RequestPath { get; set; } = "/uploads";

    public long MaxImageBytes { get; set; } = 5 * 1024 * 1024;
}
