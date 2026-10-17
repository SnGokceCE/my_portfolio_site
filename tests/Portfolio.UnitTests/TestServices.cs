using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Portfolio.Application;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.Common.Models;
using Portfolio.Infrastructure.Content;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.UnitTests;

/// <summary>
/// Gerçek Application DI kurulumunu, bellek içi veritabanı ve sahte dış bağımlılıklarla
/// (dosya, e-posta, saat) ayağa kaldırır.
/// </summary>
public sealed class TestServices : IAsyncDisposable
{
    public static readonly DateTimeOffset Now = new(2026, 10, 17, 9, 0, 0, TimeSpan.Zero);

    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    /// <param name="start">Saatin başlangıcı; FakeTimeProvider yalnızca ileri sarılabilir.</param>
    public TestServices(Action<IServiceCollection>? configure = null, DateTimeOffset? start = null)
    {
        Clock = new FakeTimeProvider(start ?? Now);
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();

        services.AddApplication();
        services.AddMemoryCache();
        services.AddSingleton<IMarkdownRenderer, MarkdownRenderer>();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(databaseName));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddSingleton<IFileStorage>(Storage);
        services.AddSingleton<IEmailOutbox>(Outbox);
        services.AddSingleton<TimeProvider>(Clock);
        configure?.Invoke(services);

        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
    }

    public FakeTimeProvider Clock { get; }

    public FakeStorage Storage { get; } = new();

    public FakeOutbox Outbox { get; } = new();

    public ApplicationDbContext Db => Get<ApplicationDbContext>();

    public T Get<T>() where T : notnull => _scope.ServiceProvider.GetRequiredService<T>();

    public async ValueTask DisposeAsync()
    {
        _scope.Dispose();
        await _provider.DisposeAsync();
    }

    public sealed class FakeStorage : IFileStorage
    {
        private int _counter;

        public List<string?> Deleted { get; } = [];

        public static ImageUpload Image() => new(new MemoryStream([1, 2, 3]), "kapak.png", 3);

        public Task<Result<string>> SaveImageAsync(ImageUpload image, string folder, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success($"/uploads/{folder}/{++_counter}.png"));

        public Task DeleteAsync(string? url, CancellationToken cancellationToken = default)
        {
            Deleted.Add(url);
            return Task.CompletedTask;
        }
    }

    public sealed class FakeOutbox : IEmailOutbox
    {
        public List<EmailMessage> Sent { get; } = [];

        public ValueTask<bool> EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            return ValueTask.FromResult(true);
        }
    }
}
