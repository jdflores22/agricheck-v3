# P3 — Agency Portal UAT Checklist

**Module:** Agency Portal (BAI workflow)  
**Environment:** `http://localhost:5173` + API `http://localhost:5000`  
**Date:** 2026-09-09

---

## Prerequisites

- [ ] Run `.\dev-windows.ps1` (API + frontend)
- [ ] Database migrated with `AgencyPortalMvp` migration
- [ ] Demo accounts seeded (see below)

---

## Test Accounts

| Role | Email | Password |
|------|-------|----------|
| Importer | `importer@agricheck.local` | `Importer@12345` |
| BAI Evaluator | `evaluator@agricheck.local` | `Evaluator@12345` |
| BAI Inspector | `inspector@agricheck.local` | `Inspector@12345` |
| Billing Agent | `billing@agricheck.local` | `Billing@12345` |
| Accreditation Officer | `accred@agricheck.local` | `Accred@12345` |
| BFAR Evaluator (isolation) | `evaluator.bfar@agricheck.local` | `Evaluator@12345` |

---

## Workflow 1 — Entry Evaluation (BAI)

| # | Step | Expected | Pass |
|---|------|----------|------|
| 1 | Login as importer, create BAI entry, submit | Entry status = Submitted, bill created | ☐ |
| 2 | Pay processing fee on entry | Payment status = Paid | ☐ |
| 3 | Login as BAI evaluator, open Evaluation Queue | Entry appears in queue | ☐ |
| 4 | Assign entry to self | Status → Under Review, checklist populated | ☐ |
| 5 | Evaluate files, update compliance checklist | Decisions saved | ☐ |
| 6 | Add internal note | Note visible on entry view | ☐ |
| 7 | Complete evaluation → Approved | Entry status = Approved | ☐ |

---

## Workflow 2 — Inspection

| # | Step | Expected | Pass |
|---|------|----------|------|
| 1 | Login as inspector | Redirect to `/inspector` | ☐ |
| 2 | Open Inspections, see approved entry | Entry listed under "ready for inspection" | ☐ |
| 3 | Schedule inspection | Inspection created with Scheduled status | ☐ |
| 4 | Pass or fail inspection | Status = Completed or Failed | ☐ |

---

## Workflow 3 — Agency Billing

| # | Step | Expected | Pass |
|---|------|----------|------|
| 1 | Login as billing agent | Agency dashboard loads | ☐ |
| 2 | Select approved entry, create draft billing | Bill status = Draft | ☐ |
| 3 | Issue billing | Status = Issued | ☐ |
| 4 | Mark paid | Status = Paid | ☐ |

---

## Workflow 4 — Accreditation Review

| # | Step | Expected | Pass |
|---|------|----------|------|
| 1 | Login as importer, submit accreditation | Status = Submitted | ☐ |
| 2 | Login as accreditation officer, open list | Submission visible | ☐ |
| 3 | Open detail, review files | File decisions saved | ☐ |
| 4 | Complete review (approve/reject) | Status updated, history recorded | ☐ |

---

## Workflow 5 — Secretary Reports

| # | Step | Expected | Pass |
|---|------|----------|------|
| 1 | Login as evaluator or billing agent | Reports page accessible | ☐ |
| 2 | Open `/agency/reports` | Summary counts displayed | ☐ |

---

## Workflow 6 — Agency Data Isolation

| # | Step | Expected | Pass |
|---|------|----------|------|
| 1 | Create BAI entry (importer), pay fee | Entry in BAI queue only | ☐ |
| 2 | Login as BFAR evaluator | BAI entry NOT in queue | ☐ |
| 3 | Run automated tests: `dotnet test tests/AgriCheck.IntegrationTests` | All isolation tests pass | ☐ |

---

## Sign-off

| Role | Name | Date | Signature |
|------|------|------|-----------|
| QA | | | |
| Product Owner | | | |
| BAI Representative | | | |

**Status:** Ready for UAT — automated isolation tests included in CI path.
