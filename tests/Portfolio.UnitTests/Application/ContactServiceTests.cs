using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Application.Contact;
using Portfolio.Domain.Entities;

namespace Portfolio.UnitTests.Application;

public class SpamHeuristicsTests
{
    [Theory]
    [InlineData("Ada", "Merhaba, staj ilanınız hakkında yazıyorum.", false)]
    [InlineData("Ada", "Projen çok güzel: https://github.com/ada/proje", false)]
    [InlineData("Ada", "http://a.com http://b.com http://c.com", true)]
    [InlineData("http://spam.example", "Merhaba, harika bir site!", true)]
    [InlineData("Ada", "Best CASINO bonus for you", true)]
    public void LooksLikeSpam(string name, string message, bool expected)
    {
        var input = new ContactInput { Name = name, Email = "a@example.com", Message = message };

        Assert.Equal(expected, SpamHeuristics.LooksLikeSpam(input, maxLinks: 2));
    }
}

public sealed class ContactServiceTests : IAsyncLifetime
{
    private TestServices _services = null!;

    private IContactService Service => _services.Get<IContactService>();

    public Task InitializeAsync()
    {
        _services = new TestServices(s => s.Configure<ContactOptions>(o => o.NotifyEmail = "ben@example.com"));
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Submit_Valid_SavesMessage_AndQueuesNotificationWithReplyTo()
    {
        var result = await Service.SubmitAsync(new ContactInput
        {
            Name = "Ada Lovelace",
            Email = " ada@example.com ",
            Subject = "Staj",
            Message = "Merhaba, staj programınız hakkında bilgi alabilir miyim?"
        });

        Assert.True(result.Succeeded);
        var saved = await _services.Db.ContactMessages.SingleAsync();
        Assert.False(saved.IsSpam);
        Assert.Equal("ada@example.com", saved.Email);

        var email = Assert.Single(_services.Outbox.Sent);
        Assert.Equal("ben@example.com", email.To);
        Assert.Equal("ada@example.com", email.ReplyTo);
        Assert.Contains("Staj", email.Subject, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Submit_Spam_IsStoredAsSpam_WithoutNotification()
    {
        await Service.SubmitAsync(new ContactInput
        {
            Name = "Bot",
            Email = "bot@example.com",
            Message = "cheap crypto http://a.io http://b.io http://c.io"
        });

        Assert.True((await _services.Db.ContactMessages.SingleAsync()).IsSpam);
        Assert.Empty(_services.Outbox.Sent);
    }

    [Fact]
    public async Task Submit_Invalid_ReturnsFieldErrors_AndSavesNothing()
    {
        var result = await Service.SubmitAsync(new ContactInput { Name = "", Email = "gecersiz", Message = "kısa" });

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Field == nameof(ContactInput.Email));
        Assert.Contains(result.Errors, e => e.Field == nameof(ContactInput.Message));
        Assert.Empty(_services.Db.ContactMessages);
    }

    [Fact]
    public async Task Submit_WithoutNotifyEmail_FallsBackToProfileEmail()
    {
        await using var services = new TestServices();
        var profile = new SiteProfile("Ad Soyad", "Unvan", "");
        profile.UpdateContact(null, "profil@example.com", null, null);
        services.Db.SiteProfiles.Add(profile);
        await services.Db.SaveChangesAsync();

        await services.Get<IContactService>().SubmitAsync(new ContactInput
        {
            Name = "Ada", Email = "ada@example.com", Message = "Merhaba, nasılsın?"
        });

        Assert.Equal("profil@example.com", Assert.Single(services.Outbox.Sent).To);
    }

    [Fact]
    public async Task Open_MarksRead_AndUnreadCountDrops()
    {
        await Service.SubmitAsync(new ContactInput { Name = "Ada", Email = "ada@example.com", Message = "Merhaba, nasılsın?" });
        var id = (await _services.Db.ContactMessages.SingleAsync()).Id;
        Assert.Equal(1, await Service.CountUnreadAsync());

        var detail = await Service.OpenAsync(id);

        Assert.NotNull(detail?.ReadAt);
        Assert.Equal(0, await Service.CountUnreadAsync());
    }

    [Fact]
    public async Task List_SeparatesInboxAndSpam()
    {
        await Service.SubmitAsync(new ContactInput { Name = "Ada", Email = "ada@example.com", Message = "Merhaba, nasılsın?" });
        await Service.SubmitAsync(new ContactInput { Name = "Bot", Email = "b@example.com", Message = "casino casino casino!!" });

        var inbox = await Service.ListAsync(MessageFilter.Inbox, 1);
        var spam = await Service.ListAsync(MessageFilter.Spam, 1);

        Assert.Equal("Ada", Assert.Single(inbox.Items).Name);
        Assert.Equal("Bot", Assert.Single(spam.Items).Name);
    }

    public async Task DisposeAsync() => await _services.DisposeAsync();
}
