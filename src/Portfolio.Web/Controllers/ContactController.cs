using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using Portfolio.Application.Contact;
using Portfolio.Application.SiteProfiles;
using Portfolio.Web.Infrastructure;
using Portfolio.Web.Localization;
using Portfolio.Web.Models;

namespace Portfolio.Web.Controllers;

/// <summary>
/// İletişim formu. Spam'e karşı katmanlı savunma:
/// 1) CSRF jetonu, 2) bal küpü alanı, 3) şifreli zaman tuzağı, 4) IP başına istek sınırı,
/// 5) içerik sezgileri (Application katmanında; şüpheliler "Spam" klasörüne düşer).
/// </summary>
[Route("iletisim")]
public partial class ContactController(
    IContactService contact,
    IFormTimeTrap timeTrap,
    ISiteProfileService profiles,
    IStringLocalizer<SharedResource> localizer,
    ILogger<ContactController> logger) : Controller
{
    private const string SentKey = "ContactSent";

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new ContactFormViewModel
        {
            FormToken = timeTrap.Issue(),
            Sent = TempData[SentKey] is true,
            Profile = await profiles.GetAsync(cancellationToken)
        };
        return View(model);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(RateLimitPolicies.Contact)]
    public async Task<IActionResult> Index(ContactFormViewModel form, CancellationToken cancellationToken)
    {
        // Botlara "başarılı" görünür; böylece hangi kuralın onları yakaladığını öğrenemezler.
        if (!string.IsNullOrEmpty(form.Website))
        {
            LogSpamBlocked(logger, "honeypot");
            return SentRedirect();
        }

        switch (timeTrap.Check(form.FormToken))
        {
            case TimeTrapResult.TooFast:
            case TimeTrapResult.Invalid:
                LogSpamBlocked(logger, "time-trap");
                return SentRedirect();
            case TimeTrapResult.Expired:
                ModelState.AddModelError(string.Empty, localizer["Form uzun süre açık kaldığı için süresi doldu. Lütfen tekrar gönder."]);
                return await Redisplay(form, cancellationToken);
        }

        if (!ModelState.IsValid)
        {
            return await Redisplay(form, cancellationToken);
        }

        var result = await contact.SubmitAsync(form.Input, cancellationToken);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Field is null ? string.Empty : $"Input.{error.Field}", error.Message);
            }

            return await Redisplay(form, cancellationToken);
        }

        return SentRedirect();
    }

    // Post/Redirect/Get: sayfa yenilenince form tekrar gönderilmez.
    private RedirectToActionResult SentRedirect()
    {
        TempData[SentKey] = true;
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> Redisplay(ContactFormViewModel form, CancellationToken cancellationToken)
    {
        form.FormToken = timeTrap.Issue();
        form.Profile = await profiles.GetAsync(cancellationToken);
        return View(form);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "İletişim formu spam koruması tarafından engellendi: {Rule}")]
    private static partial void LogSpamBlocked(ILogger logger, string rule);
}
