# AgriCheck V3 — Master Plan

## 1. Vision

Rebuild AgriCheck as **AgriCheck V3**: a clean, API-first platform with a React SPA frontend and ASP.NET Core backend, running from a **new folder** and **new MySQL database** — completely separate from the legacy Symfony app.

**Legacy (V2):** `c:\xampp\htdocs\agricheck` — reference for business rules, workflows, and UX only.  
**New (V3):** `c:\xampp\htdocs\agricheck-v3` — all new code and schema.

---

## 2. Target Tech Stack

| Layer | Technology | Notes |
|-------|------------|-------|
| Backend | ASP.NET Core **8** Web API | LTS; screenshot said .NET 7 — we use 8 for support |
| Frontend | React 18 + Vite + TypeScript | SPA only, no server-rendered pages |
| UI | MUI (Material UI) | Forest/stone theme inspired by V2 client portal |
| State | Redux Toolkit + RTK Query | Server state via RTK Query where possible |
| Database | **MySQL 8** (fresh) | Database name: `agricheck_v3` |
| Cache | Redis 7 | Sessions blacklist, rate limit, hot reads |
| ORM | EF Core + Pomelo.MySql | Code-first migrations |
| Auth | JWT access + refresh token | Stateless API; role-based access |
| QR | QRCoder | Certificates, verification links |
| PDF | QuestPDF or similar | Certificate generation (TBD in Phase 4) |
| Payments | PayMongo + Xendit SDKs (.NET) | Webhook handlers in API |
| Email | SMTP / SendGrid | Transactional notifications |

---

## 3. Core Principles

1. **Greenfield database** — design schema for V3 needs; do not auto-import V2 tables.
2. **Reference V2, don't copy V2** — simplify where legacy had tech debt.
3. **Module-by-module delivery** — shippable increments, not big-bang.
4. **API contract first** — OpenAPI/Swagger for every module before UI.
5. **Role parity** — match V2 role model (see Architecture doc).
6. **Parallel run** — V2 stays live until V3 module is UAT-approved.

---

## 4. Phases Overview

| Phase | Name | Duration | Goal |
|-------|------|----------|------|
| **P0** | Foundation | Weeks 1–4 | Repo structure, API shell, React shell, CI, fresh DB |
| **P1** | Platform & Auth | Weeks 5–8 | Users, roles, JWT, login UI, Redis |
| **P2** | Client Portal MVP | Weeks 9–16 | Entries, accreditation, certificates, warehouse bookings |
| **P3** | Agency Portal | Weeks 17–28 | Evaluation, billing, inspector, secretary |
| **P4** | Admin & Certificates | Weeks 29–38 | Admin, form builder, cert builder, QR/PDF |
| **P5** | MAV Module | Weeks 39–48 | Applications, licenses, MIC, compliance |
| **P6** | Ops & Mobile | Weeks 49–56 | Warehouse ops, driver, mobile API |
| **P7** | Cutover | Weeks 57–60 | UAT, data import tools (optional), V2 retirement |

**MVP milestone:** End of P2 — client users can register, submit entries, track accreditation, view certificates, book warehouse.  
**Full parity target:** End of P6 — all V2 portals covered.

---

## 5. Phase Details

### P0 — Foundation (Weeks 1–4)

**Backend**
- Create `AgriCheck.sln` with projects: `Api`, `Domain`, `Application`, `Infrastructure`
- EF Core code-first + Pomelo; database `agricheck_v3`
- Health endpoint, Swagger, global exception handling, CORS
- Serilog logging, environment config

**Frontend**
- Vite + React 18 + TypeScript
- MUI theme (forest/stone palette from V2 redesign)
- Redux store shell, React Router, axios/fetch client
- Layout shells: public, client, agency, admin

**DevOps**
- `.env.example` files for API and frontend
- Local run scripts (PowerShell)
- Optional: Docker Compose (MySQL + Redis + API)

**Exit criteria:** `dotnet run` + `npm run dev` both work; health check green; empty DB migrated.

---

### P1 — Platform & Auth (Weeks 5–8)

**Database (fresh tables)**
- `users`, `user_profiles`, `roles`, `user_roles`
- `agencies`, `agency_leadership`
- `refresh_tokens`, `audit_logs`
- `system_settings`

**API**
- Register, login, refresh, logout, password reset, email verification
- Role hierarchy (Admin, Evaluator, Inspector, Doctor, Importer, etc.)
- Permission middleware / policy-based authorization

**Frontend**
- Login, register, forgot password, force password change
- Protected routes by role
- User profile page

**Exit criteria:** All V2 role types can authenticate; JWT refresh flow works; Redis stores revoked tokens.

---

### P2 — Client Portal MVP (Weeks 9–16)

**Modules:** Entry, Accreditation, Certificate (read/verify), Warehouse Booking, Client Billing

**Priority flows**
1. Dashboard (client)
2. My Entries — list, submit, view (tabs: overview, history, payment, inspection)
3. Accreditation — submissions list + detail
4. Certificates — list, view, public verify
5. Warehouse bookings — list, create, cancel
6. Client payments — pay link, history

**Exit criteria:** Importer/Exporter/Broker end-to-end on V3; no dependency on V2 DB.

---

### P3 — Agency Portal (Weeks 17–28)

**Modules:** Agency Dashboard, Entry Evaluation, Document Review, Billing, Inspector

**Portals**
- Evaluator — assigned entries, file review, notes
- Secretary — entry list, reports
- Billing agent — client bills, payment confirmation
- Inspector / Doctor — inspections, containers
- Accreditation officer — submission review

**Exit criteria:** At least one agency (e.g. BAI) full workflow on V3.

---

### P4 — Admin & Certificates (Weeks 29–38)

- User/agency management
- Commodity categories, fee schedules, payment config
- Dynamic form builder (simplified v1)
- Certificate template builder + PDF + QRCoder
- Analytics dashboards, audit logs
- Canned responses, terms of agreement

---

### P5 — MAV Module (Weeks 39–48)

- Application periods, MAV applications
- Licenses, accounts, MIC allocation
- Compliance dashboards, year-end reports
- MAV admin + secretary roles

---

### P6 — Ops & Mobile (Weeks 49–56)

- Warehouse staff portal (inventory, releases)
- Driver/operator container tracking
- Face verification (if retained — evaluate scope)
- Mobile API parity (`/api/mobile/*` equivalents)

---

### P7 — Cutover (Weeks 57–60)

- UAT sign-off per module
- Optional: one-time **data import** tool V2 → V3 (separate from fresh DB decision — only when going live)
- DNS / routing switch
- V2 read-only → decommission

---

## 6. Fresh Database Strategy

V3 does **not** start from V2 migrations.

| Approach | Description |
|----------|-------------|
| **Code-first EF Core** | Entities defined in C#; migrations generate MySQL schema |
| **Domain-driven groups** | Schema built module-by-module (Auth → Entry → Agency → …) |
| **Seed data** | Dev seeds for agencies, roles, test users, commodities |
| **V2 reference** | Business analysts + devs read V2 entities for field names/workflows |
| **Production cutover** | Optional ETL scripts in P7 only — not part of initial build |

Database name: **`agricheck_v3`**  
Charset: `utf8mb4_unicode_ci`

See [03-DATABASE.md](03-DATABASE.md) for domain table groups.

---

## 7. Team & Roles (Suggested)

| Role | Responsibility |
|------|----------------|
| Tech Lead | Architecture, code review, API contracts |
| Backend Dev (.NET) | API, EF Core, integrations |
| Frontend Dev (React) | MUI pages, Redux, forms |
| QA | Test plans per phase, UAT |
| BA / Domain | V2 workflow documentation, acceptance criteria |

**Minimum viable team:** 1 full-stack + 1 frontend (or 2 full-stack)  
**Comfortable pace:** 2 backend + 2 frontend + 1 QA

---

## 8. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Form builder complexity | High delay | Ship simplified static forms in P2; dynamic builder in P4 |
| Payment webhooks | Production outage | Sandbox testing; idempotent handlers; replay logs |
| Scope creep from V2 | Never finishes | Module map with Must/Should/Could; defer Could |
| Fresh DB vs user expectation | No historical data at launch | Plan optional ETL in P7; communicate early |
| Certificate PDF parity | Visual differences | Template migration as separate sub-project |

---

## 9. Success Metrics

| Metric | Target |
|--------|--------|
| P0 complete | API + SPA run locally against empty `agricheck_v3` |
| P2 complete | 5 core client flows pass UAT |
| P3 complete | 1 agency full workflow on V3 |
| API test coverage | ≥ 70% on critical paths by P4 |
| Page load (SPA) | LCP < 2.5s on client dashboard |

---

## 10. Next Actions

1. ✅ Create V3 folder and plan docs (this document)
2. ⬜ Approve phase order and MVP scope (P2 modules)
3. ⬜ Phase 0: Scaffold .NET solution + React app
4. ⬜ Create MySQL database `agricheck_v3` on local XAMPP
5. ⬜ First EF migration: Auth domain tables

Track detailed tasks in [04-TASKS.md](04-TASKS.md).
