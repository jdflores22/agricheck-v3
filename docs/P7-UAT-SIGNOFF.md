# P7 — Master UAT Sign-off (All Modules)

**Environment:** Staging or local  
**API:** `http://localhost:5000`  
**SPA:** `http://localhost:5173`  
**Date:** _______________

---

## Prerequisites

- [ ] `.\dev-windows.ps1` or staging deployment running
- [ ] Database fully migrated (through `OpsMobileMvp`)
- [ ] Automated regression passed: `.\scripts\p7\run-regression.ps1` (19 integration + 16 smoke)

---

## Test Accounts

See [PROGRESS.md](./PROGRESS.md) for full credential table.

---

## §1 — Auth & Profile

| # | Step | Expected | Pass |
|---|------|----------|------|
| 1 | Register new user | Account created, verification email flow | ☐ |
| 2 | Login with valid credentials | JWT issued, redirect to role portal | ☐ |
| 3 | Login with wrong password (6×) | Rate limit / lockout message | ☐ |
| 4 | Refresh token | New access token without re-login | ☐ |
| 5 | Change password on `/profile` | Password updated, can re-login | ☐ |
| 6 | Logout | Token invalidated | ☐ |

---

## §2 — Client Portal

| # | Step | Expected | Pass |
|---|------|----------|------|
| 1 | Dashboard loads | Counts displayed | ☐ |
| 2 | Create import entry (BAI) | Draft saved | ☐ |
| 3 | Upload supporting file | File attached | ☐ |
| 4 | Submit entry | Status = Submitted, bill created | ☐ |
| 5 | Mark payment paid | Payment status = Paid | ☐ |
| 6 | Submit accreditation | Status = Submitted | ☐ |
| 7 | Book warehouse slot | Booking created | ☐ |
| 8 | Verify certificate page (`/verify?code=`) | Certificate details or not-found | ☐ |

---

## §3 — Agency Portal

Use [P3-UAT-CHECKLIST.md](./P3-UAT-CHECKLIST.md) workflows 1–6.

| Workflow | Pass |
|----------|------|
| Entry evaluation | ☐ |
| Inspection | ☐ |
| Agency billing | ☐ |
| Accreditation review | ☐ |
| Secretary reports | ☐ |
| Agency data isolation | ☐ |

---

## §4 — Admin & Certificates

| # | Step | Expected | Pass |
|---|------|----------|------|
| 1 | Admin dashboard | Stats load | ☐ |
| 2 | Manage users / roles | CRUD works | ☐ |
| 3 | Payment config | Fees saved per agency | ☐ |
| 4 | Form builder | Template published | ☐ |
| 5 | Certificate template | Elements saved | ☐ |
| 6 | Issue certificate on approved entry | PDF generated with QR | ☐ |
| 7 | Revoke certificate | Status = Revoked | ☐ |
| 8 | Audit log | Actions recorded | ☐ |

---

## §5 — MAV Module

| # | Step | Expected | Pass |
|---|------|----------|------|
| 1 | Importer: view open period | 2026 BYP visible | ☐ |
| 2 | Submit MAV application | Reference number assigned | ☐ |
| 3 | Admin: approve application | License + account created | ☐ |
| 4 | Importer: issue MIC | MIC active, balance deducted | ☐ |
| 5 | Admin: compliance dashboard | Alerts visible | ☐ |
| 6 | Admin: reports summary | Counts match | ☐ |

---

## §6 — Warehouse Ops

| # | Step | Expected | Pass |
|---|------|----------|------|
| 1 | Warehouse dashboard | Stored/pending counts | ☐ |
| 2 | Receive container `CONT-OPS-001` | Inventory status = Stored | ☐ |
| 3 | Create release authorization | Auth linked to entry | ☐ |
| 4 | Execute release | Container + inventory released | ☐ |

---

## §7 — Driver & Mobile

| # | Step | Expected | Pass |
|---|------|----------|------|
| 1 | Driver dashboard | Assigned container count | ☐ |
| 2 | Update container → In Transit | Status saved | ☐ |
| 3 | Record GPS location | Coordinates stored | ☐ |
| 4 | Update driver profile | Completion % increases | ☐ |
| 5 | Mobile login (`POST /api/mobile/auth/login`) | JWT returned | ☐ |
| 6 | Mobile sync push (container_status) | Queue item processed | ☐ |

---

## §8 — Cross-cutting

| # | Check | Pass |
|---|-------|------|
| 1 | Each role lands on correct portal after login | ☐ |
| 2 | Unauthorized API calls return 401/403 | ☐ |
| 3 | CORS works from SPA origin only | ☐ |
| 4 | No secrets in client bundle | ☐ |
| 5 | Swagger disabled in production config | ☐ |

---

## Final Sign-off

| Module | QA | PO | Date |
|--------|----|----|------|
| Auth & Client | | | |
| Agency | | | |
| Admin & Certificates | | | |
| MAV | | | |
| Ops & Mobile | | | |
| **Overall V3 UAT** | | | |

**Approved for production cutover:** ☐ Yes ☐ No (blockers: _______________)
