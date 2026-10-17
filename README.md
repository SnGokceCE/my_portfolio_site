# Portfolyo

Projelerimi, deneyimlerimi ve yazılarımı sergileyen, içeriği kendi yönetim panelinden yönetilen kişisel portfolyo sitesi. Staj projesi olarak geliştirilmektedir.

## Teknolojiler

| Katman | Teknoloji |
| --- | --- |
| Web | ASP.NET Core 10 MVC, Razor, kendi CSS tasarım sistemi (açık/karanlık tema) |
| Veri | Entity Framework Core 10, PostgreSQL 17, snake_case adlandırma |
| Kimlik | ASP.NET Core Identity (çerez tabanlı, rol: `Admin`, hesap kilitleme) |
| Test | xUnit |
| Altyapı | Docker, Docker Compose, sağlık kontrolü (`/health`) |

## Mimari

Proje **Clean Architecture** ilkesine göre dört katmana ayrılmıştır. Bağımlılıklar yalnızca içe doğru akar:

```
Web  ──►  Infrastructure  ──►  Application  ──►  Domain
 └───────────────────────────►─┘
```

| Proje | Sorumluluk |
| --- | --- |
| `Portfolio.Domain` | Varlıklar ve iş kuralları (`SiteProfile`, `BaseEntity`). Hiçbir dış pakete bağımlı değildir. |
| `Portfolio.Application` | Kullanım senaryoları, servis arayüzleri, DTO'lar. Veritabanına `IApplicationDbContext` soyutlaması üzerinden erişir. |
| `Portfolio.Infrastructure` | EF Core `DbContext`, migration'lar, Identity, başlangıç verileri, zaman damgası interceptor'ı. |
| `Portfolio.Web` | Controller'lar, Razor görünümleri, `Admin` alanı (area), statik dosyalar. |
| `tests/Portfolio.UnitTests` | Domain ve Application katmanı birim testleri. |

Öne çıkan tasarım kararları:

- **Zengin domain modeli:** Varlıkların setter'ları `private`; değişiklikler doğrulama yapan metotlar (`UpdateIdentity`, `UpdateContact`) üzerinden yapılır. Geçersiz URL (ör. `javascript:`) domain seviyesinde reddedilir.
- **Otomatik denetim alanları:** `AuditableEntityInterceptor`, `CreatedAt` / `UpdatedAt` alanlarını `TimeProvider` ile doldurur; bu sayede testte saat sabitlenebilir.
- **Gizli bilgiler koda gömülmez:** Bağlantı dizesi ve admin bilgileri yapılandırmadan (geliştirmede `appsettings.Development.json`, canlıda ortam değişkenleri) okunur.
- **Güvenlik:** CSRF koruması (`ValidateAntiForgeryToken`), açık yönlendirme koruması, 5 hatalı denemede 15 dk hesap kilidi, `HttpOnly` çerez.

## Çalıştırma

### Gereksinimler

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Geliştirme ortamı

```bash
# 1. Veritabanını başlat
docker compose up -d db

# 2. EF Core aracını geri yükle (ilk seferde)
dotnet tool restore

# 3. Uygulamayı çalıştır (migration'lar ve başlangıç verisi otomatik uygulanır)
dotnet run --project src/Portfolio.Web
```

Site: <http://localhost:5011> · Yönetim paneli: <http://localhost:5011/admin>

Geliştirme ortamındaki admin giriş bilgileri `src/Portfolio.Web/appsettings.Development.json` dosyasındaki `Seed` bölümündedir.

### Tüm yığını Docker ile çalıştırma

```bash
ADMIN_EMAIL=sen@ornek.com ADMIN_PASSWORD='GucluBirSifre!123' docker compose --profile full up --build
```

Site: <http://localhost:8080>

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
- [ ] **2. İçerik modeli ve admin paneli:** Projeler, deneyimler, yetenekler için CRUD, görsel yükleme
- [ ] **3. Ziyaretçi sitesi:** Proje listesi ve filtreleme, detay sayfaları, zaman çizelgesi
- [ ] **4. Blog ve iletişim:** Markdown blog, iletişim formu, spam koruması, e-posta bildirimi
- [ ] **5. Ekstralar:** Türkçe/İngilizce, SEO (sitemap, Open Graph), GitHub API entegrasyonu, ziyaret istatistikleri
- [ ] **6. Kalite ve yayın:** Entegrasyon testleri, GitHub Actions CI, yayına alma
