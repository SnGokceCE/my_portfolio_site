using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
using Portfolio.Application;
using Portfolio.Infrastructure;
using Portfolio.Infrastructure.Persistence.Seeding;
using Portfolio.Infrastructure.Storage;
using Portfolio.Web.Infrastructure;
using Portfolio.Web.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.LogoutPath = "/account/logout";
    options.AccessDeniedPath = "/account/login";
    options.Cookie.Name = "portfolio.auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

// Türkçe karakterler HTML'de "&#x131;" gibi kodlanmasın (okunabilir kaynak, daha küçük sayfa).
// <, >, & ve tırnaklar yine her zaman kaçırılır; XSS koruması değişmez.
builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(options =>
    options.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All));
builder.Services.AddSiteLocalization();
builder.Services.AddPortfolioRateLimiting();
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddControllersWithViews(options =>
{
    // Doğrulamayı FluentValidation yapıyor; MVC'nin "nullable olmayan = zorunlu" kuralı
    // İngilizce ve tekrarlı hata üretmesin.
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    TurkishModelBindingMessages.Apply(options.ModelBindingMessageProvider);
});

// Reverse proxy (Docker, bulut sağlayıcı) arkasında gerçek şema/IP bilgisini almak için.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

if (app.Configuration.GetValue("Database:MigrateOnStartup", defaultValue: app.Environment.IsDevelopment()))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
}

app.UseForwardedHeaders();
app.UseSecurityHeaders();

app.UseCulturePrefix();
app.UseSiteLocalization();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/home/error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/home/status/{0}");
app.UseHttpsRedirection();

// Yüklenen dosyalar derleme zamanında bilinmediği için MapStaticAssets yerine UseStaticFiles ile sunulur.
var storage = app.Services.GetRequiredService<LocalFileStorage>();
var storageOptions = app.Configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>() ?? new StorageOptions();
Directory.CreateDirectory(storage.RootPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(storage.RootPath),
    RequestPath = storageOptions.RequestPath,
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.XContentTypeOptions = "nosniff"
});

app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<PageViewTrackingMiddleware>();

app.MapStaticAssets();
app.MapHealthChecks("/health");

app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

// Entegrasyon testlerinde WebApplicationFactory<Program> kullanabilmek için.
public partial class Program;
