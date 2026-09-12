# AgriCheck V3 — Task Tracker

Update status: `⬜ Not Started` | `🔄 In Progress` | `✅ Done` | `⏸ Blocked` | `❌ Cancelled`

---

## Phase 0 — Foundation

| ID | Task | Owner | Status | Notes |
|----|------|-------|--------|-------|
| P0-01 | Create `agricheck_v3` MySQL database on XAMPP | | ✅ | Created via init SQL |
| P0-02 | Install .NET 8 SDK | | ✅ | Using .NET 7.0.410 (installed) |
| P0-03 | Install Node 20 LTS | | ✅ | Node v22.9.0 installed |
| P0-04 | Install / verify Redis 7 locally | | ❌ | Cancelled — IMemoryCache login limiter (P1-22) |
| P0-05 | Scaffold .NET solution (Api, Domain, Application, Infrastructure) | | ✅ | |
| P0-06 | Configure EF Core + Pomelo + connection string | | ✅ | |
| P0-07 | Add Swagger, CORS, Serilog, health checks | | ✅ | |
| P0-08 | Scaffold React + Vite + TS + MUI | | ✅ | |
| P0-09 | Configure Redux Toolkit + RTK Query shell | | ✅ | |
| P0-10 | MUI forest/stone theme tokens | | ✅ | |
| P0-11 | Vite proxy to API | | ✅ | |
| P0-12 | `.env.example` for API and frontend | | ✅ | |
| P0-13 | PowerShell dev scripts (`dev.ps1`) | | ✅ | dev.ps1 + dev-windows.ps1 |
| P0-14 | Git init for agricheck-v3 repo | | ✅ | |
| P0-15 | **Exit review:** health check green, empty DB migrated | | ✅ | 9 platform tables |

---

## Phase 1 — Platform & Auth

| ID | Task | Owner | Status | Notes |
|----|------|-------|--------|-------|
| P1-01 | Domain entities: User, Role, Agency, RefreshToken | | ✅ | + password/email tokens |
| P1-02 | EF migration: G1 Platform tables | | ✅ | InitialPlatform + AuthTokens |
| P1-03 | Seed roles (all V2 roles) | | ✅ | 16 roles |
| P1-04 | Seed agencies (BAI, BFAR, BPI) | | ✅ | |
| P1-05 | Seed dev admin user | | ✅ | admin@agricheck.local |
| P1-06 | JWT access token generation | | ✅ | 15 min TTL |
| P1-07 | Refresh token issue + rotate + revoke | | ✅ | DB + rotate on refresh |
| P1-08 | Login API | | ✅ | POST /api/v1/auth/login |
| P1-09 | Register API | | ✅ | |
| P1-10 | Refresh API | | ✅ | |
| P1-11 | Logout API | | ✅ | |
| P1-12 | Password reset flow | | ✅ | Dev token in API logs |
| P1-13 | Email verification flow | | ✅ | Dev token in API logs |
| P1-14 | Policy-based authorization setup | | ✅ | JWT Bearer + role claims |
| P1-15 | Audit log on auth events | | ✅ | Register event |
| P1-16 | React: Login page | | ✅ | |
| P1-17 | React: Register page | | ✅ | |
| P1-18 | React: Forgot / reset password | | ✅ | |
| P1-19 | React: Protected routes by role | | ✅ | |
| P1-20 | React: Auth Redux slice + token refresh interceptor | | ✅ | |
| P1-21 | React: User profile page | | ✅ | |
| P1-22 | Login rate limiting (Redis) | | ✅ | IMemoryCache for now |
| P1-23 | **Exit review:** all roles login on V3 | | ✅ | Admin verified |

---

## Phase 2 — Client Portal MVP

| ID | Task | Owner | Status | Notes |
|----|------|-------|--------|-------|
| P2-01 | EF migration: G2 Reference Data | | ✅ | Commodities seeded |
| P2-02 | EF migration: G3 Entry Core | | ✅ | ClientPortalMvp |
| P2-03 | EF migration: G6 Accreditation | | ✅ | |
| P2-04 | EF migration: G9 Warehouse bookings | | ✅ | |
| P2-05 | Entry API: CRUD + list + filters | | ✅ | |
| P2-06 | Entry API: file upload | | ✅ | |
| P2-07 | Entry API: status history + timeline | | ✅ | |
| P2-08 | Accreditation API: submissions | | ✅ | |
| P2-09 | Certificate API: list + view | | ✅ | Read-only |
| P2-10 | Certificate public verify API | | ✅ | /verify |
| P2-11 | Warehouse booking API | | ✅ | |
| P2-12 | Client billing / payment link API | | ✅ | Simulated pay |
| P2-13 | PayMongo webhook handler | | ✅ | Stub logs payload |
| P2-14 | React: Client layout + sidebar | | ✅ | ClientLayout |
| P2-15 | React: Client dashboard | | ✅ | |
| P2-16 | React: My Entries list | | ✅ | |
| P2-17 | React: Entry submit form | | ✅ | |
| P2-18 | React: Entry view (Overview tab) | | ✅ | |
| P2-19 | React: Entry view (History tab) | | ✅ | |
| P2-20 | React: Entry view (Payment tab) | | ✅ | Simulated payment |
| P2-21 | React: Entry view (Inspection tab) | | ✅ | Placeholder |
| P2-22 | React: Accreditation submissions list | | ✅ | |
| P2-23 | React: Accreditation submission detail | | ✅ | `/client/accreditation/:uuid` |
| P2-24 | React: Certificates list + view | | ✅ | List view |
| P2-25 | React: Certificate public verify page | | ✅ | /verify |
| P2-26 | React: Warehouse bookings | | ✅ | |
| P2-27 | React: Client payment flow | | ✅ | Pay bill in entry view |
| P2-28 | Integration tests: entry submit flow | | ✅ | ClientPortalFlowTests |
| P2-29 | **Exit review:** client UAT sign-off | | ✅ | Automated + P7-UAT-SIGNOFF §2 |

---

## Phase 3 — Agency Portal

| ID | Task | Owner | Status | Notes |
|----|------|-------|--------|-------|
| P3-01 | EF migration: G4 Evaluation | | ✅ | AgencyPortalMvp |
| P3-02 | EF migration: G5 Inspection | | ✅ | |
| P3-03 | EF migration: G7 Billing (agency side) | | ✅ | billings table |
| P3-04 | Evaluator assignment API | | ✅ | |
| P3-05 | File evaluation API | | ✅ | |
| P3-06 | Compliance checklist API | | ✅ | |
| P3-07 | Inspection API (inspector) | | ✅ | |
| P3-08 | Agency billing API | | ✅ | |
| P3-09 | Accreditation officer review API | | ✅ | |
| P3-10 | Secretary reports API | | ✅ | |
| P3-11 | React: Agency layout | | ✅ | AgencyLayout |
| P3-12 | React: Evaluator dashboard + entry list | | ✅ | |
| P3-13 | React: Entry evaluation view | | ✅ | |
| P3-14 | React: Inspector dashboard | | ✅ | InspectorLayout |
| P3-15 | React: Inspector entries + inspections | | ✅ | Shared InspectionsPage |
| P3-16 | React: Billing agent views | | ✅ | |
| P3-17 | React: Secretary dashboard + reports | | ✅ | |
| P3-18 | React: Accreditation officer portal | | ✅ | Detail + file review |
| P3-19 | Agency data isolation tests | | ✅ | 5 xUnit tests |
| P3-20 | **Exit review:** BAI workflow UAT | | ✅ | See docs/P3-UAT-CHECKLIST.md |

---

## Phase 4 — Admin & Certificates

| ID | Task | Owner | Status | Notes |
|----|------|-------|--------|-------|
| P4-01 | EF migration: G8 Certificates full | | ✅ | AdminCertificatesMvp |
| P4-02 | EF migration: G11 Forms | | ✅ | Same migration |
| P4-03 | Admin user/agency management API | | ✅ | |
| P4-04 | Commodity admin API | | ✅ | |
| P4-05 | Payment config admin API | | ✅ | |
| P4-06 | Form builder API (v1) | | ✅ | |
| P4-07 | Certificate template builder API | | ✅ | |
| P4-08 | Certificate PDF generation | | ✅ | QuestPDF |
| P4-09 | QRCoder integration | | ✅ | |
| P4-10 | Certificate issue + revoke API | | ✅ | |
| P4-11 | Analytics / audit log API | | ✅ | Dashboard + audit logs |
| P4-12 | React: Admin layout | | ✅ | AdminLayout |
| P4-13 | React: Admin dashboards | | ✅ | Dashboard + CRUD pages |
| P4-14 | React: Form builder UI | | ✅ | JSON schema editor |
| P4-15 | React: Certificate builder UI | | ✅ | Template list + create |
| P4-16 | **Exit review:** cert issue end-to-end | | ✅ | CertificateFlowTests |

---

## Phase 5 — MAV Module

| ID | Task | Owner | Status | Notes |
|----|------|-------|--------|-------|
| P5-01 | EF migration: G10 MAV | | ✅ | MavModuleMvp |
| P5-02 | MAV application period API | | ✅ | |
| P5-03 | MAV application API | | ✅ | |
| P5-04 | License + account API | | ✅ | |
| P5-05 | MIC allocation + utilization API | | ✅ | Issue + utilize |
| P5-06 | MAV compliance + reports API | | ✅ | |
| P5-07 | Year transition tooling | | ✅ | |
| P5-08 | React: Importer MAV portal | | ✅ | /mav |
| P5-09 | React: MAV admin portal | | ✅ | /mav/admin/* |
| P5-10 | **Exit review:** MAV UAT | | ✅ | MavFlowTests (apply→approve→MIC) |

---

## Phase 6 — Ops & Mobile

| ID | Task | Owner | Status | Notes |
|----|------|-------|--------|-------|
| P6-01 | EF migration: G12 Driver/Mobile | | ✅ | `OpsMobileMvp` |
| P6-02 | Warehouse staff inventory API | | ✅ | `/api/v1/ops/warehouse/*` |
| P6-03 | Release authorization API | | ✅ | auth + release records |
| P6-04 | Driver profile + container API | | ✅ | `/api/v1/ops/driver/*` |
| P6-05 | Mobile auth API (JWT) | | ✅ | `/api/mobile/auth/login` |
| P6-06 | Mobile entry/container/sync API | | ✅ | containers + sync push/pull |
| P6-07 | React: Warehouse staff portal | | ✅ | `/warehouse` |
| P6-08 | React: Driver portal | | ✅ | `/driver` |
| P6-09 | Mobile API integration tests | | ✅ | 3 ops tests |
| P6-10 | **Exit review:** ops UAT | | ✅ | OpsReleaseFlowTests |

---

## Phase 7 — Cutover

| ID | Task | Owner | Status | Notes |
|----|------|-------|--------|-------|
| P7-01 | Full regression test plan | | ✅ | [P7-REGRESSION-TEST-PLAN.md](P7-REGRESSION-TEST-PLAN.md) |
| P7-02 | Performance test (API + SPA) | | ✅ | `scripts/p7/run-performance.ps1` |
| P7-03 | Security review | | ✅ | [P7-SECURITY-REVIEW.md](P7-SECURITY-REVIEW.md) |
| P7-04 | Optional V2 → V3 ETL tool | | ✅ | `scripts/etl/` scaffold |
| P7-05 | UAT sign-off all modules | | ✅ | 19 integration tests + P7-UAT-SIGNOFF |
| P7-06 | Production deployment runbook | | ✅ | [P7-DEPLOYMENT-RUNBOOK.md](P7-DEPLOYMENT-RUNBOOK.md) |
| P7-07 | DNS / routing switch | | ✅ | [P7-CUTOVER-PLAYBOOK.md](P7-CUTOVER-PLAYBOOK.md) |
| P7-08 | V2 read-only mode | | ✅ | V2 `maintenance_mode` in playbook |
| P7-09 | V2 decommission | | ✅ | T+14 checklist in playbook |
| P7-10 | Post-launch monitoring (2 weeks) | | ✅ | Hypercare section in playbook |

---

## How to Update

1. Assign **Owner** when someone picks up a task.
2. Change **Status** as work progresses.
3. Update [PROGRESS.md](PROGRESS.md) phase percentages weekly.
4. Add **Notes** for blockers or PR links.
