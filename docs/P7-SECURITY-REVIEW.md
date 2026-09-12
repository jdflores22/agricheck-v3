# P7 — Security Review

**Review date:** 2026-09-09  
**Scope:** AgriCheck V3 API + React SPA (pre-production)  
**Reviewer:** _______________

---

## Summary

| Area | Status | Notes |
|------|--------|-------|
| Authentication (JWT) | ⚠️ Pass with actions | Rotate production secret |
| Authorization (RBAC) | ✅ Pass | Role attributes on controllers |
| Agency data isolation | ✅ Pass | 5 automated tests |
| Ops data isolation | ✅ Pass | 3 automated tests |
| Input validation | ✅ Pass | DTOs + EF parameterized queries |
| File uploads | ⚠️ Review | Local storage; scan in prod |
| Rate limiting | ✅ Pass | Login attempt service |
| CORS | ⚠️ Pass with actions | Restrict origins in prod |
| Secrets management | ❌ Action required | Default JWT key in appsettings |
| Logging / audit | ✅ Pass | Serilog + admin/MAV audit logs |
| HTTPS | ⚠️ Action required | Terminate TLS at reverse proxy |

**Overall:** Ready for staging. **Block production** until JWT secret and HTTPS are configured.

---

## 1. Authentication

| Check | Finding | Risk | Remediation |
|-------|---------|------|-------------|
| JWT signing key | `appsettings.json` contains placeholder `CHANGE_ME_USE_ENV...` | High | Set via env var / Azure Key Vault / user secrets |
| Token lifetime | Access 15 min, refresh 7 days | Low | Acceptable; consider shorter refresh in prod |
| Password hashing | BCrypt via `IPasswordService` | Low | OK |
| Refresh token rotation | Implemented in `AuthService` | Low | OK |
| Mobile auth | Reuses same `IAuthService` | Low | OK |

---

## 2. Authorization

| Check | Finding | Risk |
|-------|---------|------|
| `[Authorize(Roles=...)]` on admin/agency/ops endpoints | Present | Low |
| Client data scoped to `UserId` | `EntryService`, `WarehouseBookingService` filter by current user | Low |
| Agency scoped to membership | `AgencyPortalServices` filter by agency | Low |
| Driver containers scoped to `AssignedDriverUserId` | Enforced in `DriverOpsService` | Low |

**Action:** Re-run smoke test after any new endpoints are added.

---

## 3. Injection & Data Integrity

| Check | Finding |
|-------|---------|
| SQL injection | EF Core parameterized queries — no raw SQL in services |
| XSS in SPA | React escapes by default; verify rich text areas if added later |
| Mass assignment | Update DTOs are explicit records, not entity binding |

---

## 4. File Storage

| Check | Finding | Action |
|-------|---------|--------|
| Upload path | `LocalFileStorageService` under app data folder | Move to blob/S3 in prod |
| Path traversal | Stored filenames generated server-side | OK |
| Content validation | Content-type stored; no AV scan | Add clamav or cloud scan in prod |

---

## 5. Network & Transport

| Check | Finding | Action |
|-------|---------|--------|
| HTTPS | Not enforced in dev | Enable HSTS + redirect in production |
| CORS | `Cors:AllowedOrigins` in config | Set to production domain only |
| Swagger | Enabled in Development only | Confirm `ASPNETCORE_ENVIRONMENT=Production` |
| Health endpoints | Public (`/health`, `/api/v1/health`) | OK for load balancers |

---

## 6. Rate Limiting & Abuse

| Check | Finding |
|-------|---------|
| Login brute force | `ILoginAttemptService` in-memory (5 attempts / 15 min window) |
| Distributed deployment | In-memory limiter resets per instance | **Use Redis for multi-node prod** |

---

## 7. Sensitive Data

| Check | Finding | Action |
|-------|---------|--------|
| Passwords in logs | Serilog does not log request bodies | OK |
| PII in audit logs | User emails in admin audit | Retention policy needed |
| Demo accounts | Seeded in `DatabaseSeeder` | **Disable seeder in production** |

---

## 8. Pre-Production Checklist

- [ ] `Jwt:SecretKey` from environment (min 32 chars random)
- [ ] `ConnectionStrings:DefaultConnection` from secure store
- [ ] `Cors:AllowedOrigins` = production SPA URL only
- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Swagger disabled
- [ ] Database seeder disabled or production-safe seed only
- [ ] TLS certificate on reverse proxy
- [ ] File storage on secured volume or cloud bucket
- [ ] Backup schedule for `agricheck_v3` database
- [ ] Login rate limiter backed by Redis (if multi-instance)

---

## Sign-off

| Role | Name | Date | Approved |
|------|------|------|----------|
| Security / Tech Lead | | | ☐ |
| Product Owner | | | ☐ |
