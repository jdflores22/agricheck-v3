# P7 — Cutover Playbook

**Objective:** Switch production traffic from AgriCheck V2 (Symfony/PHP) to V3 (.NET/React) with minimal downtime.

**Estimated maintenance window:** 2–4 hours

---

## Timeline Overview

| Phase | Duration | Tasks |
|-------|----------|-------|
| T-7 days | — | Staging UAT sign-off, security review |
| T-1 day | 1 hr | Final regression, backup V2 DB + files |
| T-0 (cutover) | 2–4 hr | V2 read-only → deploy V3 → DNS switch → smoke |
| T+1 day | — | Hypercare monitoring |
| T+14 days | — | V2 decommission review |

---

## Phase A — Pre-Cutover (T-7 to T-1)

- [ ] [P7-UAT-SIGNOFF.md](./P7-UAT-SIGNOFF.md) approved
- [ ] [P7-SECURITY-REVIEW.md](./P7-SECURITY-REVIEW.md) actions completed
- [ ] `.\scripts\p7\run-regression.ps1` passes on staging
- [ ] Production runbook rehearsed on staging
- [ ] Stakeholder comms sent (maintenance window notice)
- [ ] Rollback DNS TTL lowered to 300s (24h before)

### Optional: V2 → V3 data import

Only if business requires historical data (not default fresh DB):

```powershell
# See scripts/etl/README.md
.\scripts\etl\export-v2-users.ps1   # read-only export from V2 MySQL
.\scripts\etl\import-v3-staging.ps1 # transform + load to agricheck_v3
```

---

## Phase B — V2 Read-Only Mode (P7-08)

Enable V2 maintenance mode so users cannot mutate data during cutover.

### Via V2 Admin (recommended)

1. Login as V2 admin
2. System Settings → `maintenance_mode` = `1`
3. Set `maintenance_message` = "AgriCheck is being upgraded. Read-only access until [time]."

### Via database (emergency)

```sql
-- V2 database (agricheck or your V2 schema)
UPDATE system_settings SET value = '1' WHERE name = 'maintenance_mode';
UPDATE system_settings SET value = 'AgriCheck is under maintenance for system upgrade.' WHERE name = 'maintenance_message';
```

### Verify

- [ ] Non-admin users cannot log in to V2 (or see maintenance page)
- [ ] Admin can still access for read-only verification
- [ ] V2 audit log records maintenance enable event

---

## Phase C — Deploy V3 (P7-06)

Follow [P7-DEPLOYMENT-RUNBOOK.md](./P7-DEPLOYMENT-RUNBOOK.md):

1. Apply EF migrations to production `agricheck_v3`
2. Deploy API (without demo seeder)
3. Deploy SPA static build
4. Run smoke test against production URLs (before DNS switch, via hosts file)

---

## Phase D — DNS / Routing Switch (P7-07)

| Record | Before | After |
|--------|--------|-------|
| `app.agricheck.gov.ph` | V2 web server | V3 SPA (CDN/nginx) |
| `api.agricheck.gov.ph` | V2 PHP API (if separate) | V3 Kestrel behind proxy |

### Steps

1. [ ] Confirm V3 smoke test green via hosts override
2. [ ] Update DNS A/CNAME records (or load balancer target groups)
3. [ ] Wait for TTL propagation (monitor from multiple regions)
4. [ ] Verify public login → V3 SPA loads
5. [ ] Disable V2 maintenance for rollback path only if needed

### IIS / Apache redirect (optional interim)

Redirect all V2 URLs to V3 landing with announcement page for 24h.

---

## Phase E — Post-Launch Monitoring (P7-10)

**Duration:** 14 days hypercare, then standard ops

### Hour 0–24 (critical)

| Check | Frequency | Alert if |
|-------|-----------|----------|
| `/health` + `/api/v1/health` | 1 min | non-200 |
| Error rate (5xx) | 5 min | > 1% |
| Login success rate | 15 min | < 95% |
| MySQL connections | 15 min | pool exhausted |

### Daily (days 2–14)

- [ ] Review Serilog files / centralized logs
- [ ] Check failed login spikes
- [ ] Monitor disk (uploads + logs)
- [ ] Triage UAT feedback tickets

### Metrics to track

| Metric | Target |
|--------|--------|
| API uptime | 99.5% |
| p95 login latency | < 1s |
| Support tickets P1 | 0 open > 4h |

---

## Phase F — V2 Decommission (P7-09)

**Earliest:** T+14 days after stable V3 operation

| Step | Action |
|------|--------|
| 1 | Confirm no V2 traffic (access logs zero for 7 days) |
| 2 | Final archive of V2 database (`mysqldump`) |
| 3 | Archive V2 `public/uploads` to cold storage |
| 4 | Stop V2 PHP-FPM / Apache vhost |
| 5 | Retain V2 codebase tag `v2-final` in git for 1 year |
| 6 | Document decommission date in change log |

---

## Rollback Procedure

If critical failure within 4h of cutover:

1. Revert DNS to V2 endpoints
2. Disable V2 maintenance mode (`maintenance_mode` = `0`)
3. Communicate rollback to users
4. Root-cause analysis before retry

---

## Communication Templates

**Pre-cutover (T-1):**

> AgriCheck will undergo scheduled maintenance on [DATE] [TIME]. The system will be read-only during the upgrade. Plan submissions accordingly.

**Go-live:**

> AgriCheck has been upgraded. Please use [URL] and clear browser cache if you see errors.

**Rollback:**

> We have temporarily restored the previous system while we resolve an issue. We will notify you when the upgrade resumes.

---

## Sign-off

| Gate | Approver | Date |
|------|----------|------|
| UAT complete | PO | |
| Security actions done | Tech Lead | |
| Cutover executed | Ops | |
| Hypercare complete (day 14) | PO + Tech Lead | |
