# Yayına alma rehberi

Bu rehber, siteyi kendi alan adınla HTTPS üzerinden yayına almayı anlatır. Önerilen kurulum tek bir
Linux sunucu (VPS) üzerinde Docker Compose'dur:

```
İnternet ──443──► Caddy (otomatik HTTPS) ──► web (ASP.NET Core, :8080) ──► db (PostgreSQL)
```

Dışarıya yalnızca Caddy açılır. Uygulama ve veritabanı Docker'ın iç ağındadır.

## Neden bu kurulum?

| Seçenek | Artı | Eksi |
| --- | --- | --- |
| **VPS + Docker Compose** (önerilen) | Aylık birkaç dolar, tam kontrol, CI'ın ürettiği imaj olduğu gibi çalışır | Sunucu bakımı (güncellemeler, yedek) sende |
| Azure App Service + Azure PostgreSQL | Yönetilen servis; GitHub Öğrenci Paketi / Azure for Students kredisiyle ücretsiz başlanabilir | Daha fazla yapılandırma, kredi bitince maliyet |
| Render / Railway / Fly.io | Birkaç tıkla kurulum | Ücretsiz katmanlarda uyku modu ve kalıcı disk kısıtları (yüklenen görseller için önemli) |

## 1. Hazırlık

1. **Sunucu:** Ubuntu 24.04, en az 1 vCPU / 1 GB RAM (Hetzner, DigitalOcean, Linode vb.).
2. **Alan adı:** DNS'te `A` kaydı sunucunun IP adresini göstermeli (`ornek.dev` ve `www.ornek.dev`).
3. **GitHub deposu:** Kod GitHub'a gönderildiğinde CI, `main` dalındaki her değişiklikte imajı
   `ghcr.io/KULLANICI/DEPO` adresine yayımlar. Paket gizliyse sunucuda `docker login ghcr.io`
   yapılmalı (yalnızca `read:packages` yetkili bir jetonla).

## 2. Sunucuyu kur

```bash
# Docker
curl -fsSL https://get.docker.com | sh

# Güvenlik duvarı: yalnızca SSH ve web
sudo ufw allow OpenSSH && sudo ufw allow 80 && sudo ufw allow 443 && sudo ufw enable

# Dosyalar
sudo mkdir -p /opt/portfolio && cd /opt/portfolio
# deploy/ klasöründeki docker-compose.prod.yml, Caddyfile, .env.example, backup.sh dosyalarını buraya kopyala
cp .env.example .env && nano .env
chmod 600 .env && chmod +x backup.sh
```

`.env` içindeki gizli değerleri rastgele üret:

```bash
openssl rand -base64 32   # POSTGRES_PASSWORD
openssl rand -hex 32      # ANALYTICS_SECRET
```

## 3. Başlat

```bash
docker compose -f docker-compose.prod.yml up -d
docker compose -f docker-compose.prod.yml logs -f web
```

İlk açılışta uygulama veritabanı tablolarını oluşturur (migration) ve `.env`'deki admin hesabını açar.
Caddy birkaç saniye içinde Let's Encrypt sertifikasını alır.

Kontrol listesi:

- [ ] `https://ornek.dev/health` → `Healthy`
- [ ] `https://ornek.dev/admin` → giriş yap, **profilini doldur**, GitHub adresini gir
- [ ] Giriş yaptıktan sonra `.env`'den `ADMIN_PASSWORD` satırını sil (hesap zaten oluştu)
- [ ] İletişim formundan kendine test mesajı gönder (SMTP ayarlıysa e-posta gelmeli)
- [ ] `https://ornek.dev/sitemap.xml` adresini [Google Search Console](https://search.google.com/search-console)'a ekle

## 4. Güncelleme

CI yeni imajı yayımladıktan sonra:

```bash
cd /opt/portfolio
docker compose -f docker-compose.prod.yml pull web
docker compose -f docker-compose.prod.yml up -d web
```

Yeni migration'lar açılışta otomatik uygulanır. Geri dönmek için `.env`'de `IMAGE` değerini önceki
`sha-xxxxxxx` etiketine çevirip aynı komutları çalıştır.

## 5. Yedekleme ve geri yükleme

`backup.sh` veritabanının ve yüklenen görsellerin yedeğini `backups/` klasörüne alır, 14 günden
eskileri siler. Günlük çalıştırmak için `crontab -e`:

```
0 3 * * * cd /opt/portfolio && ./backup.sh >> backups/backup.log 2>&1
```

Yedekleri sunucu dışına da kopyala (ör. `rclone` ile bir bulut depolamaya). Geri yükleme:

```bash
gunzip -c backups/portfolio_TARIH.sql.gz | docker compose -f docker-compose.prod.yml exec -T db psql -U portfolio -d portfolio
docker compose -f docker-compose.prod.yml run --rm --no-deps --user root -v "$PWD/backups:/backups" \
  --entrypoint sh web -c "tar -xzf /backups/uploads_TARIH.tar.gz -C /app/wwwroot"
```

## 6. Güvenlik notları

- Uygulama; CSP (nonce'lu), `X-Frame-Options`, `X-Content-Type-Options`, `Referrer-Policy` ve HSTS başlıklarını kendisi gönderir.
- Admin girişinde 5 hatalı denemede hesap 15 dakika kilitlenir; iletişim formunda IP başına istek sınırı vardır.
- Veritabanı portu dışarıya açık değildir; `.env` dosyası `chmod 600` olmalı ve asla git'e eklenmemelidir.
- `DataProtection__KeysPath` kalıcı birimde tutulur; böylece yeniden başlatmalarda oturumlar ve form jetonları geçerliliğini korur.
  Açılışta görülen "No XML encryptor configured" uyarısı, bu anahtarların birimde şifrelenmeden durduğunu söyler; tek sunucuda,
  yalnızca Docker'ın erişebildiği bir birimde bu yaygın ve kabul edilebilir bir durumdur. Daha sıkı bir kurulumda anahtarlar
  bir sertifikayla (`ProtectKeysWithCertificate`) ya da Azure Key Vault ile şifrelenebilir.
- HTML yanıtları bilerek sıkıştırılmaz (HTTPS + CSRF jetonu + sıkıştırma = BREACH riski).
