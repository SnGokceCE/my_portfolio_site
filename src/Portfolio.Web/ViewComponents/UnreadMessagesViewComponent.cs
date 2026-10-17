using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Contact;

namespace Portfolio.Web.ViewComponents;

/// <summary>Yönetim menüsündeki "Mesajlar" bağlantısının yanında okunmamış sayısını gösterir.</summary>
public class UnreadMessagesViewComponent(IContactService contact) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var count = await contact.CountUnreadAsync(HttpContext.RequestAborted);
        return count == 0
            ? Content(string.Empty)
            : Content(count > 99 ? "99+" : count.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}
