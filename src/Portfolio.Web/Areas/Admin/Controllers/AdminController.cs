using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Common.Models;
using Portfolio.Application.Common.Security;

namespace Portfolio.Web.Areas.Admin.Controllers;

/// <summary>Yönetim paneli controller'larının ortak tabanı: yetki, CSRF koruması ve yardımcılar.</summary>
[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
[AutoValidateAntiforgeryToken]
public abstract class AdminController : Controller
{
    public const string FlashMessageKey = "FlashMessage";
    public const string FlashKindKey = "FlashKind";

    protected void Flash(string message, bool success = true)
    {
        TempData[FlashMessageKey] = message;
        TempData[FlashKindKey] = success ? "success" : "error";
    }

    /// <summary>
    /// Servis hatalarını forma taşır. Alanlar varsayılan olarak "Input." ön ekiyle eşleşir;
    /// <paramref name="rootFields"/> içindekiler (ör. dosya alanı) ön eksiz kalır.
    /// </summary>
    protected void AddErrors(Result result, params string[] rootFields)
    {
        foreach (var error in result.Errors)
        {
            var key = error.Field switch
            {
                null => string.Empty,
                var field when rootFields.Contains(field) => field,
                var field => $"Input.{field}"
            };
            ModelState.AddModelError(key, error.Message);
        }
    }

    /// <summary>Başarılı/başarısız bir silme veya durum değişikliğinden sonra listeye döner.</summary>
    protected IActionResult AfterAction(Result result, string successMessage, string action = "Index", object? routeValues = null)
    {
        if (result.IsNotFound)
        {
            return NotFound();
        }

        Flash(result.Succeeded ? successMessage : string.Join(" ", result.Errors.Select(e => e.Message)), result.Succeeded);
        return RedirectToAction(action, routeValues);
    }
}
