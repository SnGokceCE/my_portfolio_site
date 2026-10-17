using Portfolio.Application.Blog;
using Portfolio.Application.Common.Models;
using Portfolio.Application.Contact;

namespace Portfolio.Web.Areas.Admin.Models;

public sealed class PostFormViewModel : FormViewModel<BlogPostInput>
{
    public string? CoverImageUrl { get; set; }

    public IFormFile? Cover { get; set; }

    public bool RemoveCover { get; set; }
}

public sealed record PostIndexViewModel(PagedList<BlogPostListItem> Posts, BlogPostQuery Query);

public sealed record MessageIndexViewModel(PagedList<MessageListItem> Messages, MessageFilter Filter);
