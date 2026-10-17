using Portfolio.Application.Common.Models;

namespace Portfolio.Application.Common.Interfaces;

/// <summary>Formdan yüklenen bir görsel. Akış (stream) controller tarafından açılır ve kapatılır.</summary>
public sealed record ImageUpload(Stream Content, string FileName, long Length);

/// <summary>
/// Yüklenen dosyaların saklandığı yer. Şu an yerel disk; ileride bulut depolama
/// (S3, Azure Blob) ile değiştirilebilir, Application katmanı etkilenmez.
/// </summary>
public interface IFileStorage
{
    /// <summary>Görseli doğrular ve kaydeder; başarılıysa herkese açık URL'ini döndürür.</summary>
    Task<Result<string>> SaveImageAsync(ImageUpload image, string folder, CancellationToken cancellationToken = default);

    Task DeleteAsync(string? url, CancellationToken cancellationToken = default);
}
