using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Portfolio.Web.Infrastructure;

public static class RateLimitPolicies
{
    public const string Contact = "contact";

    /// <summary>Yapılandırma anahtarı; varsayılan: aynı IP'den 10 dakikada 5 gönderim.</summary>
    public const string ContactPermitLimitKey = "RateLimit:ContactPermitLimit";

    public static IServiceCollection AddPortfolioRateLimiting(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(Contact, context =>
            {
                var limit = context.RequestServices.GetRequiredService<IConfiguration>().GetValue(ContactPermitLimitKey, 5);
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limit,
                        Window = TimeSpan.FromMinutes(10),
                        QueueLimit = 0
                    });
            });
        });
}
