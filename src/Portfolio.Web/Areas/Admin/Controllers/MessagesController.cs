using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Contact;
using Portfolio.Web.Areas.Admin.Models;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class MessagesController(IContactService contact) : AdminController
{
    public async Task<IActionResult> Index(MessageFilter filter = MessageFilter.Inbox, int page = 1, CancellationToken cancellationToken = default) =>
        View(new MessageIndexViewModel(await contact.ListAsync(filter, page, cancellationToken), filter));

    public async Task<IActionResult> Detail(Guid id, CancellationToken cancellationToken)
    {
        var message = await contact.OpenAsync(id, cancellationToken);
        return message is null ? NotFound() : View(message);
    }

    [HttpPost]
    public async Task<IActionResult> MarkUnread(Guid id, CancellationToken cancellationToken) =>
        AfterAction(await contact.MarkUnreadAsync(id, cancellationToken), "Okunmadı olarak işaretlendi.");

    [HttpPost]
    public async Task<IActionResult> Spam(Guid id, bool isSpam, CancellationToken cancellationToken) =>
        AfterAction(
            await contact.SetSpamAsync(id, isSpam, cancellationToken),
            isSpam ? "Mesaj spam klasörüne taşındı." : "Mesaj gelen kutusuna taşındı.",
            routeValues: new { filter = isSpam ? MessageFilter.Inbox : MessageFilter.Spam });

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        AfterAction(await contact.DeleteAsync(id, cancellationToken), "Mesaj silindi.");
}
