using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Infrastructure.Storage;

namespace Portfolio.UnitTests.Infrastructure;

public sealed class LocalFileStorageTests : IDisposable
{
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];

    private readonly string _root = Path.Combine(Path.GetTempPath(), "portfolio-tests", Guid.NewGuid().ToString("N"));
    private readonly LocalFileStorage _storage;

    public LocalFileStorageTests()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 17, 0, 0, 0, TimeSpan.Zero));
        _storage = new LocalFileStorage(
            Options.Create(new StorageOptions { UploadsPath = "uploads", MaxImageBytes = 1024 }),
            new TestHostEnvironment(_root),
            clock);
    }

    [Theory]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, ".jpg")]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, ".png")]
    [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }, ".gif")]
    [InlineData(new byte[] { 0x52, 0x49, 0x46, 0x46, 1, 2, 3, 4, 0x57, 0x45, 0x42, 0x50 }, ".webp")]
    public void DetectImageExtension_RecognizesImageSignatures(byte[] header, string expected)
    {
        Assert.Equal(expected, LocalFileStorage.DetectImageExtension(header));
    }

    [Fact]
    public void DetectImageExtension_RejectsHtmlDisguisedAsImage()
    {
        Assert.Null(LocalFileStorage.DetectImageExtension("<html><script>"u8));
    }

    [Fact]
    public async Task SaveImage_StoresUnderDatedFolder_WithContentBasedExtension()
    {
        using var content = new MemoryStream(PngHeader);

        var result = await _storage.SaveImageAsync(new ImageUpload(content, "kotu-isim.exe", content.Length), "projects");

        Assert.True(result.Succeeded);
        Assert.StartsWith("/uploads/projects/2026/10/", result.Value, StringComparison.Ordinal);
        Assert.EndsWith(".png", result.Value, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(_storage.RootPath, result.Value!["/uploads/".Length..])));
    }

    [Fact]
    public async Task SaveImage_RejectsTooLargeFile()
    {
        using var content = new MemoryStream(new byte[2048]);

        var result = await _storage.SaveImageAsync(new ImageUpload(content, "a.png", content.Length), "projects");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Delete_IgnoresPathsOutsideUploadRoot()
    {
        var outside = Path.Combine(_root, "secret.txt");
        Directory.CreateDirectory(_root);
        await File.WriteAllTextAsync(outside, "gizli");

        await _storage.DeleteAsync("/uploads/../secret.txt");

        Assert.True(File.Exists(outside));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private sealed class TestHostEnvironment(string root) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";

        public string ApplicationName { get; set; } = "Portfolio.Tests";

        public string ContentRootPath { get; set; } = root;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
