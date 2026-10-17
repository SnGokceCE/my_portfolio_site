using Portfolio.Domain.Common;
using Portfolio.Domain.Entities;

namespace Portfolio.UnitTests.Domain;

public class BlogPostTests
{
    [Fact]
    public void SetTags_SplitsCommas_TrimsAndRemovesCaseInsensitiveDuplicates()
    {
        var post = new BlogPost("Başlık", "Giriş", "Metin");

        post.SetTags(["C#, Web API ,c#", "  Staj  ", ""]);

        Assert.Equal(["C#", "Web API", "Staj"], post.Tags);
    }

    [Fact]
    public void SetTags_RejectsTooMany()
    {
        var post = new BlogPost("Başlık", "Giriş", "Metin");
        var tags = Enumerable.Range(1, BlogPost.MaxTags + 1).Select(i => $"etiket{i}");

        Assert.Throws<DomainException>(() => post.SetTags(tags));
    }

    [Fact]
    public void ReadingMinutes_IsCalculatedFromBody()
    {
        var post = new BlogPost("Başlık", "Giriş", string.Join(' ', Enumerable.Repeat("kelime", 450)));

        Assert.Equal(3, post.ReadingMinutes);
    }

    [Fact]
    public void Publish_KeepsFirstDate()
    {
        var post = new BlogPost("Başlık", "Giriş", "Metin");
        var first = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        post.Publish(first);
        post.Unpublish();
        post.Publish(first.AddDays(10));

        Assert.Equal(first, post.PublishedAt);
    }
}

public class ContactMessageTests
{
    [Fact]
    public void MarkRead_KeepsFirstReadTime_AndUnreadClearsIt()
    {
        var message = new ContactMessage("Ada", "ada@example.com", null, "Merhaba dünya!", isSpam: false);
        var first = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        message.MarkRead(first);
        message.MarkRead(first.AddHours(1));
        Assert.Equal(first, message.ReadAt);

        message.MarkUnread();
        Assert.False(message.IsRead);
    }
}
