#!/usr/bin/env sh
# Veritabanı yedeği: sıkıştırılmış pg_dump alır, 14 günden eski yedekleri siler.
# Sunucuda günlük çalıştırmak için (crontab -e):
#   0 3 * * * cd /opt/portfolio && ./backup.sh >> backups/backup.log 2>&1
set -eu

KEEP_DAYS=14
STAMP=$(date +%Y-%m-%d_%H%M)
FILE="/backups/portfolio_${STAMP}.sql.gz"

docker compose -f docker-compose.prod.yml exec -T db \
  sh -c "pg_dump -U portfolio -d portfolio --no-owner | gzip > ${FILE}"

# Yüklenen görseller de yedeklenmeli (veritabanında yalnızca adresleri var).
docker compose -f docker-compose.prod.yml run --rm --no-deps --user root -v "$(pwd)/backups:/backups" \
  --entrypoint sh web -c "tar -czf /backups/uploads_${STAMP}.tar.gz -C /app/wwwroot uploads"

find ./backups -name 'portfolio_*.sql.gz' -mtime +${KEEP_DAYS} -delete
find ./backups -name 'uploads_*.tar.gz' -mtime +${KEEP_DAYS} -delete

echo "$(date -Iseconds) yedek alındı: ${FILE}"
