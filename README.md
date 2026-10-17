# Portfolyo

Projelerimi, deneyimlerimi ve yazılarımı sergileyen, içeriği kendi yönetim panelinden yönetilen kişisel portfolyo sitesi. Staj projesi olarak geliştirilmektedir.

## Teknolojiler

| Katman | Teknoloji |
| --- | --- |
| Web | ASP.NET Core 10 MVC, Razor, kendi CSS tasarım sistemi (açık/karanlık tema) |
| Veri | Entity Framework Core 10, PostgreSQL 17, snake_case adlandırma |
| Kimlik | ASP.NET Core Identity (çerez tabanlı, rol: `Admin`, hesap kilitleme) |
| Doğrulama | FluentValidation (Türkçe mesajlar) + domain seviyesinde koruma |
| İçerik | Markdig (Markdown), HtmlSanitizer (XSS koruması), IMemoryCache |
| Test | xUnit, EF Core InMemory, FakeTimeProvider |
| E-posta | MailKit (SMTP), `Channel<T>` tabanlı kuyruk + arka plan servisi, geliştirmede Mailpit |
| Çoklu dil | Türkçe/İngilizce, `/en` ön eki, JSON tabanlı yerelleştirici, içerik için isteğe bağlı İngilizce alanlar |
| SEO | Çok dilli `sitemap.xml` (hreflang), `robots.txt`, JSON-LD (Person, WebSite, BlogPosting, SoftwareSourceCode, BreadcrumbList), Open Graph |
| Entegrasyon | GitHub REST API (tipli `HttpClient` + `Microsoft.Extensions.Http.Resilience`, önbellek) |
| İstatistik | Çerezsiz, IP saklamayan ziyaret istatistikleri, sunucu tarafında çizilen SVG grafikler |
| Altyapı | Docker, Docker Compose, sağlık kontrolü (`/health`), ASP.NET Core Rate Limiting |

## Mimari

Proje **Clean Architecture** ilkesine göre dört katmana ayrılmıştır. Bağımlılıklar yalnızca içe doğru akar:

```
Web  ──►  Infrastructure  ──►  Application  ──►  Domain
 └───────────────────────────►─┘
```

| Proje | Sorumluluk |
| --- | --- |
| `Portfolio.Domain` | Varlıklar (`Project`, `Category`, `Skill`, `Experience`, `SiteProfile`) ve iş kuralları, `Slug` üretici. Hiçbir dış pakete bağımlı değildir. |
| `Portfolio.Application` | Özellik klasörleri (`Projects`, `Skills`, ...) altında servisler, giriş modelleri ve doğrulayıcılar. Veritabanına `IApplicationDbContext`, dosyalara `IFileStorage` soyutlaması üzerinden erişir. |
| `Portfolio.Infrastructure` | EF Core `DbContext`, migration'lar, Identity, başlangıç verileri, zaman damgası interceptor'ı, yerel dosya depolama. |
| `Portfolio.Web` | Controller'lar, Razor görünümleri, `Admin` alanı (area), statik dosyalar. |
| `tests/Portfolio.UnitTests` | Domain, doğrulayıcı, servis ve dosya depolama testleri. |

Öne çıkan tasarım kararları:

- **Zengin domain modeli:** Varlıkların setter'ları `private`; değişiklikler doğrulama yapan metotlar (`UpdateIdentity`, `UpdateContact`) üzerinden yapılır. Geçersiz URL (ör. `javascript:`) domain seviyesinde reddedilir.
- **Otomatik denetim alanları:** `AuditableEntityInterceptor`, `CreatedAt` / `UpdatedAt` alanlarını `TimeProvider` ile doldurur; bu sayede testte saat sabitlenebilir.
- **Gizli bilgiler koda gömülmez:** Bağlantı dizesi ve admin bilgileri yapılandırmadan (geliştirmede `appsettings.Development.json`, canlıda ortam değişkenleri) okunur.
- **Result deseni:** Servisler beklenen hataları (geçersiz giriş, bulunamayan kayıt) istisna fırlatmak yerine `Result` ile döndürür; alan bazlı hatalar doğrudan formdaki ilgili alanın altında gösterilir.
- **Çift katmanlı doğrulama:** FluentValidation kullanıcı dostu mesajlar üretir; domain varlıkları yine de kendi kurallarını korur, veritabanında da `CHECK` kısıtları ve benzersiz indeksler bulunur.
- **Güvenli dosya yükleme:** Görsel türü dosya adından değil içeriğin ilk baytlarından (magic number) tespit edilir, dosya adı rastgele üretilir, `X-Content-Type-Options: nosniff` ile sunulur ve silmede klasör dışına çıkma (path traversal) engellenir.
- **Güvenli Markdown:** Proje içerikleri Markdown ile yazılır. Ham HTML kapalıdır, üretilen HTML ayrıca HtmlSanitizer'dan geçer (`javascript:` bağlantıları ve `onclick` gibi öznitelikler temizlenir). Başlıklardan otomatik içindekiler listesi ve okuma süresi çıkarılır.
- **Taslak önizleme:** Yayımlanmamış projeler ziyaretçiye 404 döner; admin aynı adreste "Taslak önizleme" bandıyla görür.
- **Aşamalı geliştirme:** Animasyonlar JavaScript ile eklenir ama içerik hiçbir koşulda betiğe bağımlı olarak gizli kalmaz; `prefers-reduced-motion` desteklenir.
- **Katmanlı spam koruması (iletişim formu):** CSRF jetonu → bal küpü (honeypot) alanı → Data Protection ile şifrelenmiş zaman tuzağı (3 sn'den hızlı ya da 2 saatten eski formlar) → IP başına istek sınırı (10 dk'da 5) → içerik sezgileri (çok bağlantı, şüpheli kelimeler). Botlara her durumda "başarılı" yanıtı verilir; hangi kurala takıldıklarını öğrenemezler. Şüpheli mesajlar silinmez, "Spam" klasörüne düşer.
- **Asenkron e-posta:** Form isteği SMTP'yi beklemez; mesaj kuyruğa yazılır, `EmailDispatcher` arka planda üstel geri çekilmeyle (2 sn, 4 sn…) yeniden deneyerek gönderir. SMTP çökse bile mesaj veritabanında kalır.
- **Blog:** Etiketler PostgreSQL `text[]` dizisinde, GIN indeksiyle saklanır. Okuma süresi kayıt sırasında hesaplanır, RSS 2.0 akışı (`/blog/rss.xml`) sunulur. Editörde sitedeki işleyiciyle birebir aynı çıktıyı veren canlı Markdown önizleme vardır.
- **Çoklu dil:** `/en` ile başlayan istekler bir ara katmanda `PathBase`'e taşınır; böylece `Url.Action`, tag helper'lar ve statik dosyalar ek kod gerekmeden `/en` önekli bağlantı üretir. Arayüz metinleri kaynak dili Türkçe olan bir JSON yerelleştiricide (`Localization/Translations/en.json`) tutulur. Bir birim testi, görünümlerdeki her `L["..."]` metninin çevirisi olduğunu ve `{0}` yer tutucularının korunduğunu denetler. İçerikte İngilizce alan boşsa Türkçe aslı gösterilir ve sayfada not düşülür.
- **SEO:** Her sayfa iki dilde site haritasına girer ve `hreflang` ile birbirine bağlanır. JSON-LD verisi `System.Text.Json` ile üretilir; varsayılan kodlayıcı `<` karakterini kaçırdığı için içerikteki `</script>` betik bloğunu kapatamaz.
- **GitHub entegrasyonu:** Profildeki GitHub adresinden kullanıcı adı çıkarılır; ana sayfada son güncellenen depolar ve dil dağılımı, proje detayında depo istatistikleri gösterilir. Yanıtlar 1 saat önbelleklenir. GitHub'a ulaşılamazsa son başarılı veri, o da yoksa hiçbir şey gösterilmez; sayfa asla hata vermez.
- **Gizlilik dostu istatistikler (KVKK/GDPR):** Çerez kullanılmaz, IP adresi saklanmaz. Tekil ziyaretçi `HMAC-SHA256(gizli anahtar, gün | IP | tarayıcı)` özetiyle sayılır; gün anahtara dahil olduğu için kişi günler arasında izlenemez. Botlar, admin'in kendi gezintisi, ön yüklemeler ve DNT/GPC gönderen tarayıcılar sayılmaz. Kayıtlar kuyruktan toplu yazılır, 400 günden eskiler otomatik silinir.
- **Güvenlik:** CSRF koruması (`ValidateAntiForgeryToken`), açık yönlendirme koruması, 5 hatalı denemede 15 dk hesap kilidi, `HttpOnly` çerez.

## Çalıştırma

### Gereksinimler

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Geliştirme ortamı

```bash
# 1. Veritabanını ve e-posta test sunucusunu başlat
docker compose up -d db mail

# 2. EF Core aracını geri yükle (ilk seferde)
dotnet tool restore

# 3. Uygulamayı çalıştır (migration'lar ve başlangıç verisi otomatik uygulanır)
dotnet run --project src/Portfolio.Web
```

Site: <http://localhost:5011> (İngilizce: `/en`) · Projeler: `/projeler` · Blog: `/blog` · İletişim: `/iletisim` · Yönetim paneli: `/admin` · Site haritası: `/sitemap.xml`

Geliştirmede iletişim formundan giden bildirim e-postaları gerçek bir adrese gitmez; Mailpit arayüzünde görünür: <http://localhost:8025>

Geliştirme ortamındaki admin giriş bilgileri `src/Portfolio.Web/appsettings.Development.json` dosyasındaki `Seed` bölümündedir.

### Tüm yığını Docker ile çalıştırma

```bash
ADMIN_EMAIL=sen@ornek.com ADMIN_PASSWORD='GucluBirSifre!123' docker compose --profile full up --build
```

Site: <http://localhost:8080>

### Yapılandırma

Canlı ortamda ortam değişkenleriyle verilir (`__` bölüm ayırıcıdır):

| Ayar | Açıklama |
| --- | --- |
| `ConnectionStrings__Default` | PostgreSQL bağlantısı |
| `Seed__AdminEmail`, `Seed__AdminPassword` | İlk açılışta oluşturulacak admin hesabı |
| `Email__SmtpHost`, `Email__SmtpPort`, `Email__Username`, `Email__Password`, `Email__FromAddress` | SMTP; boşsa e-postalar yalnızca loglanır |
| `Contact__NotifyEmail` | İletişim formu bildirimlerinin gideceği adres (boşsa profildeki e-posta) |
| `Analytics__Secret` | Ziyaretçi özeti anahtarı; boşsa her açılışta yeniden üretilir |
| `GitHub__Token` | İsteğe bağlı; GitHub API sınırını saatte 60'tan 5000 isteğe çıkarır |
| `DataProtection__KeysPath` | Şifreleme anahtarlarının kalıcı klasörü (Docker'da birim) |

### Testler

```bash
dotnet test
```

### Yeni migration ekleme

```bash
dotnet ef migrations add <Ad> -p src/Portfolio.Infrastructure -s src/Portfolio.Web -o Persistence/Migrations
```

## Yol haritası

- [x] **1. İskelet:** Katmanlı mimari, PostgreSQL, Identity, Docker, tema ve temel düzen
- [x] **2. İçerik modeli ve admin paneli:** Projeler, deneyimler, yetenekler için CRUD, görsel yükleme
- [x] **3. Ziyaretçi sitesi:** Proje listesi ve filtreleme, detay sayfaları, zaman çizelgesi
- [x] **4. Blog ve iletişim:** Markdown blog, iletişim formu, spam koruması, e-posta bildirimi
- [x] **5. Ekstralar:** Türkçe/İngilizce, SEO (sitemap, Open Graph), GitHub API entegrasyonu, ziyaret istatistikleri
- [ ] **6. Kalite ve yayın:** Entegrasyon testleri, GitHub Actions CI, yayına alma
