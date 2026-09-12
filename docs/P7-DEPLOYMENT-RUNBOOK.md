# P7 — Production Deployment Runbook

**Target stack:** ASP.NET Core 7 API + React SPA + MySQL 8  
**Database:** `agricheck_v3` (utf8mb4_unicode_ci)

---

## 1. Prerequisites

| Item | Requirement |
|------|-------------|
| Server | Windows Server 2019+ or Linux (Ubuntu 22.04+) |
| Runtime | .NET 7 ASP.NET Core Runtime |
| Database | MySQL 8.0+ |
| Reverse proxy | IIS, nginx, or Caddy with TLS |
| Node (build only) | Node 18+ for SPA build |

---

## 2. Database Setup

```sql
CREATE DATABASE agricheck_v3 CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER 'agricheck_v3'@'%' IDENTIFIED BY '<strong-password>';
GRANT ALL PRIVILEGES ON agricheck_v3.* TO 'agricheck_v3'@'%';
FLUSH PRIVILEGES;
```

Apply migrations (from build server or API host):

```powershell
cd backend\src\AgriCheck.Infrastructure
$env:ConnectionStrings__DefaultConnection = "Server=<db-host>;Port=3306;Database=agricheck_v3;User=agricheck_v3;Password=<pwd>;"
dotnet ef database update --startup-project ..\AgriCheck.Api\AgriCheck.Api.csproj
```

> **Production:** Disable demo seeder. Remove or guard `DatabaseSeeder` hosted service for production builds.

---

## 3. API Deployment

### 3.1 Publish

```powershell
cd backend\src\AgriCheck.Api
dotnet publish -c Release -o C:\inetpub\agricheck-v3-api
```

### 3.2 Environment variables

| Variable | Example |
|----------|---------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_URLS` | `http://127.0.0.1:5000` |
| `ConnectionStrings__DefaultConnection` | MySQL connection string |
| `Jwt__SecretKey` | `<64-char-random-secret>` |
| `Jwt__Issuer` | `AgriCheckV3` |
| `Jwt__Audience` | `AgriCheckV3` |
| `Cors__AllowedOrigins__0` | `https://app.agricheck.gov.ph` |
| `App__PublicBaseUrl` | `https://app.agricheck.gov.ph` |

### 3.3 IIS (Windows)

1. Install ASP.NET Core Hosting Bundle 7.x
2. Create Application Pool (No Managed Code)
3. Create site pointing to publish folder
4. Ensure `logs/` folder is writable
5. Configure URL Rewrite + HTTPS binding

### 3.4 nginx (Linux) — snippet

```nginx
server {
    listen 443 ssl;
    server_name api.agricheck.gov.ph;

    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

---

## 4. Frontend Deployment

```powershell
cd frontend
npm ci
npm run build
# Deploy dist/ to static host (IIS, S3+CloudFront, nginx)
```

### SPA routing

Configure server to fallback to `index.html` for client-side routes (`/client/*`, `/agency/*`, etc.).

### API URL

Set Vite env at build time:

```powershell
$env:VITE_API_BASE_URL = "https://api.agricheck.gov.ph/api/v1"
npm run build
```

(Verify `baseQuery` reads this env var in your frontend config.)

---

## 5. Post-Deploy Verification

```powershell
# Health
curl https://api.agricheck.gov.ph/health
curl https://api.agricheck.gov.ph/api/v1/health

# Full smoke (against staging/prod URL)
.\scripts\p7\smoke-test.ps1 -ApiBaseUrl "https://api.agricheck.gov.ph"
```

---

## 6. Rollback Plan

| Step | Action |
|------|--------|
| 1 | Revert DNS to V2 (see [P7-CUTOVER-PLAYBOOK.md](./P7-CUTOVER-PLAYBOOK.md)) |
| 2 | Disable V3 maintenance banner if shown |
| 3 | Restore previous API publish folder from backup |
| 4 | Roll back DB migration only if schema incompatible (prefer forward-fix) |

Keep last known-good API + SPA artifacts tagged in CI for 30 days.

---

## 7. Backup Schedule

| Asset | Frequency | Retention |
|-------|-----------|-----------|
| MySQL full dump | Daily | 30 days |
| Uploaded files | Daily sync | 30 days |
| Application logs | Continuous | 90 days |

---

## Sign-off

| Step | Owner | Date | Done |
|------|-------|------|------|
| DB migrated | | | ☐ |
| API deployed | | | ☐ |
| SPA deployed | | | ☐ |
| Smoke test pass | | | ☐ |
| Monitoring alerts on | | | ☐ |
