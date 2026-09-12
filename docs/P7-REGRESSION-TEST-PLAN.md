# P7 — Full Regression Test Plan

**Project:** AgriCheck V3  
**Scope:** All modules P0–P6  
**Environment:** Local (`http://localhost:5173` + `http://localhost:5000`) or staging  
**Date:** 2026-09-09

---

## 1. Objectives

Verify that V3 core workflows work end-to-end before production cutover, with no regressions in auth, portal isolation, or data integrity.

---

## 2. Test Layers

| Layer | Tool | Command | Pass Criteria |
|-------|------|---------|---------------|
| Unit / service | xUnit | `dotnet test backend/tests/AgriCheck.IntegrationTests` | 0 failures |
| API smoke | PowerShell | `.\scripts\p7\smoke-test.ps1` | All checks green |
| Manual UAT | Checklist | [P7-UAT-SIGNOFF.md](./P7-UAT-SIGNOFF.md) | All modules signed |
| Performance baseline | PowerShell | `.\scripts\p7\run-performance.ps1` | p95 within targets |

---

## 3. Automated Regression Suite

### 3.1 Integration tests (in-memory)

| Test class | Coverage |
|------------|----------|
| `AgencyDataIsolationTests` | BAI/BFAR entry isolation (5 tests) |
| `OpsPortalTests` | Driver isolation, warehouse receive, mobile sync (3 tests) |
| `CrossModuleSmokeTests` | Dashboard/list endpoints per portal (6 tests) |
| `ClientPortalFlowTests` | Entry submit + accreditation submit (2 tests) |
| `CertificateFlowTests` | Issue certificate + public verify (1 test) |
| `MavFlowTests` | Apply → approve → MIC (1 test) |
| `OpsReleaseFlowTests` | Receive → authorize → release (1 test) |

Run:

```powershell
cd c:\xampp\htdocs\agricheck-v3\backend
dotnet test tests/AgriCheck.IntegrationTests --logger "console;verbosity=normal"
```

### 3.2 API smoke (live server required)

Covers health, auth, and authenticated reads for every portal:

```powershell
cd c:\xampp\htdocs\agricheck-v3
.\scripts\p7\smoke-test.ps1
```

Optional base URL override:

```powershell
.\scripts\p7\smoke-test.ps1 -ApiBaseUrl "https://staging-api.agricheck.local"
```

### 3.3 Full regression runner

Runs integration tests + smoke test in sequence:

```powershell
.\scripts\p7\run-regression.ps1
```

---

## 4. Manual Regression Matrix

| Module | Portal | Critical paths | UAT doc |
|--------|--------|----------------|---------|
| Auth | `/login` | Register, login, refresh, logout, password reset | P7-UAT-SIGNOFF §1 |
| Client | `/client` | Entry CRUD, submit, pay, accreditation, warehouse booking | P7-UAT-SIGNOFF §2 |
| Agency | `/agency` | Evaluate, inspect, bill, accredit, reports | [P3-UAT-CHECKLIST.md](./P3-UAT-CHECKLIST.md) |
| Admin | `/admin` | Users, forms, cert templates, issue/revoke PDF | P7-UAT-SIGNOFF §4 |
| MAV | `/mav` | Apply, approve, license, MIC issue/utilize | P7-UAT-SIGNOFF §5 |
| Warehouse | `/warehouse` | Receive container, release authorization, execute release | P7-UAT-SIGNOFF §6 |
| Driver / Mobile | `/driver`, `/api/mobile` | Profile, container status, GPS, offline sync | P7-UAT-SIGNOFF §7 |

---

## 5. Entry / Exit Criteria

### Entry

- [ ] All EF migrations applied on target database
- [ ] API and frontend deployed to test environment
- [ ] Demo/staging accounts available (see [PROGRESS.md](./PROGRESS.md))

### Exit (regression pass)

- [ ] All automated tests pass
- [ ] Smoke test script passes against staging
- [ ] No P1/P2 defects open
- [ ] UAT sign-off completed ([P7-UAT-SIGNOFF.md](./P7-UAT-SIGNOFF.md))

---

## 6. Performance Targets (baseline)

| Endpoint | p50 | p95 | Max error rate |
|----------|-----|-----|----------------|
| `GET /health` | < 50 ms | < 200 ms | 0% |
| `POST /api/v1/auth/login` | < 300 ms | < 800 ms | 0% |
| `GET /api/v1/client/dashboard` | < 400 ms | < 1200 ms | 0% |
| SPA first load (prod build) | < 2 s | < 4 s | — |

Measured via `.\scripts\p7\run-performance.ps1`.

---

## 7. Defect Severity

| Severity | Definition | Cutover blocker? |
|----------|------------|------------------|
| P1 | Data loss, auth bypass, cross-tenant leak | Yes |
| P2 | Core workflow broken (submit, approve, pay) | Yes |
| P3 | UI glitch, non-critical feature | No |
| P4 | Cosmetic | No |

---

## 8. Sign-off

| Role | Name | Date |
|------|------|------|
| QA Lead | | |
| Tech Lead | | |
| Product Owner | | |
