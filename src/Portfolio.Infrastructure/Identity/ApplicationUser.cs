using Microsoft.AspNetCore.Identity;

namespace Portfolio.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string? DisplayName { get; set; }
}
