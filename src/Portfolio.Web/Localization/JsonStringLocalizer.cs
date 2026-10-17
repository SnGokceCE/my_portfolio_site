using System.Collections.Frozen;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Localization;

namespace Portfolio.Web.Localization;

/// <summary>Görünümlerde <c>@L["..."]</c> için işaretçi tip.</summary>
public sealed class SharedResource;

/// <summary>
/// Kaynak dili Türkçe olan basit JSON yerelleştirici. Anahtar Türkçe metnin kendisidir:
/// <c>L["Projeler"]</c> Türkçe sayfada "Projeler", İngilizce sayfada en.json'daki karşılığı döner.
/// Çevirisi eksik metin Türkçe görünür ve geliştirme ortamında loglanır; sayfa asla bozulmaz.
/// .resx'e göre avantajı: çeviriler tek, okunabilir bir dosyada durur.
/// </summary>
public sealed partial class JsonStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly FrozenDictionary<string, FrozenDictionary<string, string>> _translations;
    private readonly ILogger<JsonStringLocalizerFactory> _logger;
    private readonly bool _logMissing;

    public JsonStringLocalizerFactory(IWebHostEnvironment environment, ILogger<JsonStringLocalizerFactory> logger)
    {
        _logger = logger;
        _logMissing = environment.IsDevelopment();

        var folder = Path.Combine(environment.ContentRootPath, "Localization", "Translations");
        _translations = Directory.Exists(folder)
            ? Directory.GetFiles(folder, "*.json").ToFrozenDictionary(
                file => Path.GetFileNameWithoutExtension(file),
                file => JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file))!.ToFrozenDictionary(),
                StringComparer.OrdinalIgnoreCase)
            : FrozenDictionary<string, FrozenDictionary<string, string>>.Empty;
    }

    public IStringLocalizer Create(Type resourceSource) => new JsonStringLocalizer(this);

    public IStringLocalizer Create(string baseName, string location) => new JsonStringLocalizer(this);

    internal string? Find(string key)
    {
        var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        if (language == "tr")
        {
            return null;
        }

        if (_translations.TryGetValue(language, out var table) && table.TryGetValue(key, out var value))
        {
            return value;
        }

        if (_logMissing)
        {
            LogMissing(_logger, language, key);
        }

        return null;
    }

    internal IEnumerable<LocalizedString> All()
    {
        var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return _translations.TryGetValue(language, out var table)
            ? table.Select(kv => new LocalizedString(kv.Key, kv.Value))
            : [];
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Çeviri eksik ({Language}): \"{Key}\"")]
    private static partial void LogMissing(ILogger logger, string language, string key);

    private sealed class JsonStringLocalizer(JsonStringLocalizerFactory factory) : IStringLocalizer
    {
        public LocalizedString this[string name] =>
            factory.Find(name) is { } value
                ? new LocalizedString(name, value)
                : new LocalizedString(name, name, resourceNotFound: true);

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                var format = this[name];
                return new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, format.Value, arguments), format.ResourceNotFound);
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => factory.All();
    }
}
