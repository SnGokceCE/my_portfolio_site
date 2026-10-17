using System.Globalization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.Common.Models;

namespace Portfolio.Infrastructure.Storage;

/// <summary>
/// Görselleri yerel diske kaydeder. Dosya adı ve uzantısı kullanıcının gönderdiği isimden değil,
/// dosyanın gerçek içeriğinden (ilk baytlarındaki "magic number") belirlenir; böylece
/// görsel kılığına sokulmuş bir betik ya da HTML dosyası sunucuya yazılamaz.
/// </summary>
public sealed class LocalFileStorage(
    IOptions<StorageOptions> options,
    IHostEnvironment environment,
    TimeProvider clock) : IFileStorage
{
    private readonly StorageOptions _options = options.Value;

    public string RootPath => Path.GetFullPath(Path.Combine(environment.ContentRootPath, _options.UploadsPath));

    public async Task<Result<string>> SaveImageAsync(
        ImageUpload image, string folder, CancellationToken cancellationToken = default)
    {
        if (image.Length == 0)
        {
            return Invalid("Seçilen dosya boş.");
        }

        if (image.Length > _options.MaxImageBytes)
        {
            return Invalid($"Görsel en fazla {_options.MaxImageBytes / 1024 / 1024} MB olabilir.");
        }

        var header = new byte[12];
        var read = await image.Content.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        var extension = DetectImageExtension(header.AsSpan(0, read));
        if (extension is null)
        {
            return Invalid("Yalnızca JPG, PNG, WEBP veya GIF görselleri yüklenebilir.");
        }

        var now = clock.GetUtcNow();
        var relativeDir = Path.Combine(
            SafeSegment(folder),
            now.ToString("yyyy", CultureInfo.InvariantCulture),
            now.ToString("MM", CultureInfo.InvariantCulture));
        var fileName = $"{Guid.CreateVersion7():N}{extension}";
        var directory = Path.Combine(RootPath, relativeDir);
        Directory.CreateDirectory(directory);

        var fullPath = Path.Combine(directory, fileName);
        await using (var target = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await target.WriteAsync(header.AsMemory(0, read), cancellationToken);
            await image.Content.CopyToAsync(target, cancellationToken);
        }

        var url = $"{_options.RequestPath.TrimEnd('/')}/{relativeDir.Replace('\\', '/')}/{fileName}";
        return Result.Success(url);
    }

    public Task DeleteAsync(string? url, CancellationToken cancellationToken = default)
    {
        var prefix = _options.RequestPath.TrimEnd('/') + "/";
        if (string.IsNullOrEmpty(url) || !url.StartsWith(prefix, StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        var fullPath = Path.GetFullPath(Path.Combine(RootPath, url[prefix.Length..]));

        // "../" ile kök klasörün dışına çıkılmasını (path traversal) engelle.
        if (fullPath.StartsWith(RootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
            File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    internal static string? DetectImageExtension(ReadOnlySpan<byte> header) => header switch
    {
        [0xFF, 0xD8, 0xFF, ..] => ".jpg",
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, ..] => ".png",
        [0x47, 0x49, 0x46, 0x38, ..] => ".gif",
        [0x52, 0x49, 0x46, 0x46, _, _, _, _, 0x57, 0x45, 0x42, 0x50, ..] => ".webp",
        _ => null
    };

    private static string SafeSegment(string folder) =>
        string.Concat(folder.Where(char.IsAsciiLetterOrDigit)) is { Length: > 0 } safe ? safe : "misc";

    private static Result<string> Invalid(string message) => Result.Invalid<string>([new Error(message)]);
}
