using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Portfolio.Application.Contact;

namespace Portfolio.Infrastructure.Security;

/// <summary>
/// Zaman damgasını ASP.NET Core Data Protection ile şifreleyip imzalar; bot jetonu
/// okuyamaz ya da tarihini değiştiremez.
/// </summary>
public sealed class DataProtectionFormTimeTrap(IDataProtectionProvider provider, TimeProvider clock) : IFormTimeTrap
{
    public static readonly TimeSpan MinimumFillTime = TimeSpan.FromSeconds(3);
    public static readonly TimeSpan MaximumAge = TimeSpan.FromHours(2);

    private readonly IDataProtector _protector = provider.CreateProtector("Portfolio.ContactForm.TimeTrap.v1");

    public string Issue() =>
        _protector.Protect(clock.GetUtcNow().UtcTicks.ToString(CultureInfo.InvariantCulture));

    public TimeTrapResult Check(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return TimeTrapResult.Invalid;
        }

        long ticks;
        try
        {
            if (!long.TryParse(_protector.Unprotect(token), NumberStyles.None, CultureInfo.InvariantCulture, out ticks))
            {
                return TimeTrapResult.Invalid;
            }
        }
        catch (CryptographicException)
        {
            return TimeTrapResult.Invalid;
        }

        var elapsed = clock.GetUtcNow() - new DateTimeOffset(ticks, TimeSpan.Zero);
        if (elapsed < MinimumFillTime)
        {
            return TimeTrapResult.TooFast;
        }

        return elapsed > MaximumAge ? TimeTrapResult.Expired : TimeTrapResult.Valid;
    }
}
