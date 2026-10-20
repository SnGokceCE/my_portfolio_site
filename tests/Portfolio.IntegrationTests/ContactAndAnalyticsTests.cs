using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Portfolio.IntegrationTests;

[Collection(WebTestGroup.Name)]
public class ContactAndAnalyticsTests(PortfolioFactory factory)
{
    private static Dictionary<string, string> Message(string token, string formToken, string name, string website = "") => new()
    {
        ["__RequestVerificationToken"] = token,
        ["FormToken"] = formToken,
        ["Website"] = website,
        ["Input.Name"] = name,
        ["Input.Email"] = "ada@example.com",
        ["Input.Subject"] = "Staj",
        ["Input.Message"] = "Merhaba, portfolyonu çok beğendim; staj için görüşebilir miyiz?"
    };

    private async Task<(HttpClient Client, string Token, string FormToken)> OpenFormAsync()
    {
        var client = factory.CreateBrowser();
        var page = await client.GetDocumentAsync("/iletisim");
        return (client, page.AntiforgeryToken(), page.QuerySelector("input[name=FormToken]")!.GetAttribute("value")!);
    }

    [Fact]
    public async Task ValidMessage_IsSaved_AndOwnerIsNotifiedInBackground()
    {
        var name = $"Ada {Guid.NewGuid():N}"[..14];
        var (client, token, formToken) = await OpenFormAsync();
        factory.Clock.Advance(TimeSpan.FromSeconds(10)); // bir insanın formu doldurma süresi

        using var response = await client.PostFormAsync("/iletisim", Message(token, formToken, name));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.True(await factory.WithDatabaseAsync(db => db.ContactMessages.AnyAsync(m => m.Name == name && !m.IsSpam)));
        await Eventually(() => factory.Emails.Sent.Any(e => e.TextBody.Contains(name, StringComparison.Ordinal)));
        Assert.Contains(factory.Emails.Sent, e => e.To == "owner@integration.test" && e.ReplyTo == "ada@example.com");
    }

    [Fact]
    public async Task BotFilledHoneypot_LooksSuccessful_ButIsNotSaved()
    {
        var name = $"Bot {Guid.NewGuid():N}"[..14];
        var (client, token, formToken) = await OpenFormAsync();
        factory.Clock.Advance(TimeSpan.FromSeconds(10));

        using var response = await client.PostFormAsync("/iletisim", Message(token, formToken, name, website: "http://spam.example"));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.False(await factory.WithDatabaseAsync(db => db.ContactMessages.AnyAsync(m => m.Name == name)));
    }

    [Fact]
    public async Task InstantSubmission_IsTreatedAsBot()
    {
        var name = $"Hızlı {Guid.NewGuid():N}"[..14];
        var (client, token, formToken) = await OpenFormAsync();

        using var response = await client.PostFormAsync("/iletisim", Message(token, formToken, name));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.False(await factory.WithDatabaseAsync(db => db.ContactMessages.AnyAsync(m => m.Name == name)));
    }

    [Fact]
    public async Task TooManySubmissions_AreRateLimited()
    {
        // Aynı veritabanını kullanan, sınırı 2'ye düşürülmüş ayrı bir uygulama örneği.
        using var limited = factory.WithWebHostBuilder(b => b.UseSetting("RateLimit:ContactPermitLimit", "2"));
        var client = limited.CreateClient(new() { AllowAutoRedirect = false });
        var page = await client.GetDocumentAsync("/iletisim");
        var fields = Message(page.AntiforgeryToken(), "gecersiz", "Tekrar");

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 3; i++)
        {
            using var response = await client.PostFormAsync("/iletisim", fields);
            statuses.Add(response.StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[^1]);
        Assert.DoesNotContain(HttpStatusCode.TooManyRequests, statuses.Take(2));
    }

    [Fact]
    public async Task VisitorPageViews_AreRecordedAnonymously_ButAdminVisitsAreNot()
    {
        var marker = $"/projeler?ref={Guid.NewGuid():N}";
        var visitor = factory.CreateBrowser();
        visitor.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) Mobile Safari/604.1");
        visitor.DefaultRequestHeaders.Referrer = new Uri("https://www.linkedin.com/in/someone");

        var before = await factory.WithDatabaseAsync(db => db.PageViews.CountAsync(v => v.Path == "/projeler"));
        await visitor.GetAsync(marker);
        var admin = await factory.LoginAsAdminAsync();
        await admin.GetAsync("/projeler");

        // Kayıtlar arka planda toplu yazılır (en geç 5 sn).
        await Eventually(async () =>
            await factory.WithDatabaseAsync(db => db.PageViews.CountAsync(v => v.Path == "/projeler")) > before);
        await Task.Delay(TimeSpan.FromSeconds(6));

        var views = await factory.WithDatabaseAsync(db =>
            db.PageViews.Where(v => v.Path == "/projeler").OrderByDescending(v => v.OccurredAt).Take(5).ToListAsync());
        Assert.Equal(before + 1, await factory.WithDatabaseAsync(db => db.PageViews.CountAsync(v => v.Path == "/projeler")));
        Assert.Equal("linkedin.com", views[0].ReferrerHost);
        Assert.Equal(Portfolio.Domain.Entities.DeviceType.Mobile, views[0].Device);
    }

    private static Task Eventually(Func<bool> condition) => Eventually(() => Task.FromResult(condition()));

    private static async Task Eventually(Func<Task<bool>> condition, int timeoutSeconds = 15)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(200);
        }

        Assert.Fail($"Koşul {timeoutSeconds} saniye içinde sağlanmadı.");
    }
}
