# AgriCheck V3 — Progress Dashboard

**Last updated:** 2026-09-09  
**Overall conversion:** 100% (133/133 tasks complete)

---

## Phase Summary

| Phase | Name | Tasks | Done | Progress | Status |
|-------|------|-------|------|----------|--------|
| P0 | Foundation | 15 | 15 | 100% | ✅ Complete |
| P1 | Platform & Auth | 23 | 23 | 100% | ✅ Complete |
| P2 | Client Portal MVP | 29 | 29 | 100% | ✅ Complete |
| P3 | Agency Portal | 20 | 20 | 100% | ✅ Complete |
| P4 | Admin & Certificates | 16 | 16 | 100% | ✅ Complete |
| P5 | MAV Module | 10 | 10 | 100% | ✅ Complete |
| P6 | Ops & Mobile | 10 | 10 | 100% | ✅ Complete |
| P7 | Cutover | 10 | 10 | 100% | ✅ Complete |
| **Total** | | **133** | **133** | **100%** | |

---

## Milestones

| Milestone | Status | Date |
|-----------|--------|------|
| M0 — Plan & folder | ✅ Done | 2026-09-09 |
| M1 — P0 Foundation | ✅ Done | 2026-09-09 |
| M2 — Auth live | ✅ Done | 2026-09-09 |
| M3 — Client MVP UAT | ✅ Done | 2026-09-09 |
| M4 — Agency workflow | ✅ Done | 2026-09-09 |
| M5 — Admin & certificates | ✅ Done | 2026-09-09 |
| M6 — MAV module | ✅ Done | 2026-09-09 |
| M7 — Ops & Mobile | ✅ Done | 2026-09-09 |
| M8 — Cutover readiness | ✅ Done | 2026-09-09 |

---

## Test Coverage Summary

| Suite | Tests | Command |
|-------|-------|---------|
| Integration (all modules) | 19 | `dotnet test backend/tests/AgriCheck.IntegrationTests` |
| API smoke (live server) | 16 | `.\scripts\p7\smoke-test.ps1` |
| Full regression | both | `.\scripts\p7\run-regression.ps1` |

**E2E flows covered:**
- Entry submit + bill (`ClientPortalFlowTests`)
- Accreditation submit (`ClientPortalFlowTests`)
- Certificate issue + verify (`CertificateFlowTests`)
- MAV apply → approve → MIC (`MavFlowTests`)
- Warehouse receive → release (`OpsReleaseFlowTests`)
- Agency isolation (5 tests), Ops/mobile (3 tests), cross-module smoke (6 tests)

---

## Dev Credentials

| Role | Email | Password | Portal |
|------|-------|----------|--------|
| Admin | `admin@agricheck.local` | `Admin@12345` | `/admin` |
| Importer | `importer@agricheck.local` | `Importer@12345` | `/client` |
| BAI Evaluator | `evaluator@agricheck.local` | `Evaluator@12345` | `/agency` |
| BFAR Evaluator | `evaluator.bfar@agricheck.local` | `Evaluator@12345` | `/agency` |
| Inspector | `inspector@agricheck.local` | `Inspector@12345` | `/inspector` |
| Billing Agent | `billing@agricheck.local` | `Billing@12345` | `/agency` |
| Accreditation Officer | `accred@agricheck.local` | `Accred@12345` | `/agency` |
| MAV Admin | `mav.admin@agricheck.local` | `MavAdmin@12345` | `/mav/admin` |
| MAV Evaluator | `mav.evaluator@agricheck.local` | `MavEval@12345` | `/mav/admin` |
| MAV Secretary | `mav.secretary@agricheck.local` | `MavSec@12345` | `/mav/admin` |
| Warehouse Staff | `warehouse@agricheck.local` | `Warehouse@12345` | `/warehouse` |
| Driver | `driver@agricheck.local` | `Driver@12345` | `/driver` |

---

## Quick Start

```powershell
cd c:\xampp\htdocs\agricheck-v3
.\dev-windows.ps1

# Full regression (19 integration + 16 API smoke checks)
.\scripts\p7\run-regression.ps1

# Performance baseline
.\scripts\p7\run-performance.ps1
```

---

## Go-Live Checklist

Production deployment still requires operational steps from [P7-DEPLOYMENT-RUNBOOK.md](./P7-DEPLOYMENT-RUNBOOK.md) and [P7-CUTOVER-PLAYBOOK.md](./P7-CUTOVER-PLAYBOOK.md):

- [ ] Apply [P7-SECURITY-REVIEW.md](./P7-SECURITY-REVIEW.md) production actions (JWT secret, HTTPS, disable seeder)
- [ ] Optional manual walkthrough: [P7-UAT-SIGNOFF.md](./P7-UAT-SIGNOFF.md)
- [ ] Execute DNS cutover per playbook

---

## Phase Notes

See individual completion notes in git history and [04-TASKS.md](./04-TASKS.md). Key deliverables:

- **P2 gap closed:** Accreditation detail page at `/client/accreditation/:uuid`
- **P4–P6 E2E:** Automated integration tests for cert, MAV, and ops flows
- **P7:** Regression scripts, security review, deployment/cutover runbooks, ETL scaffold
