# V2 → V3 Optional ETL (Production Cutover Only)

**Status:** Scaffold / documentation — run only when migrating live V2 data into V3.

V3 was built with a **fresh database** (`agricheck_v3`). Historical V2 data is **not** required for MVP launch. Use this ETL only if stakeholders require continuity of users, entries, or certificates.

---

## Strategy

| Approach | When to use |
|----------|-------------|
| **Fresh start (default)** | New installs, pilot agencies, dev/staging |
| **Selective ETL** | Migrate users + open entries only |
| **Full ETL** | Full production cutover with history |

Recommended: **selective ETL** — users, agencies, commodities, open (non-terminal) entries.

---

## Table Mapping (V2 → V3)

| V2 (Symfony) | V3 (EF Core) | Notes |
|--------------|--------------|-------|
| `user` | `users` + `user_profiles` | Re-hash passwords cannot migrate BCrypt→BCrypt as-is if algorithms differ; force password reset |
| `role` / user roles | `roles`, `user_roles` | Map `ROLE_*` codes directly |
| `agency` | `agencies` | Code alignment (BAI, BFAR, BPI) |
| `entry` | `entries` + `entry_details` | Status enum mapping required |
| `certificate` | `certificates` | Re-issue PDFs via V3 templates preferred |
| `mav_*` | `mav_*` | Complex; migrate after core entry flow validated |
| `warehouse_*` | `warehouse_*`, `containers` | Migrate only active inventory |

See [03-DATABASE.md](../docs/03-DATABASE.md) for full V3 schema.

---

## Status Enum Mapping (Entry)

| V2 status | V3 `EntryStatus` |
|-----------|------------------|
| draft | Draft |
| submitted | Submitted |
| under_review | UnderReview |
| approved | Approved |
| rejected | Rejected |
| cancelled | Cancelled |

---

## Export Scripts (read-only on V2)

```powershell
# Requires MySQL client + read-only V2 DB credentials
.\scripts\etl\export-v2-users.ps1 -V2ConnectionString "Server=localhost;Database=agricheck;..."
```

Outputs CSV/JSON to `scripts/etl/output/` (gitignored).

---

## Import Scripts (V3 staging only)

```powershell
.\scripts\etl\import-v3-staging.ps1 `
  -V3ConnectionString "Server=localhost;Database=agricheck_v3;..." `
  -InputDir ".\scripts\etl\output"
```

**Never run against production without staging dry-run.**

---

## Dry-Run Checklist

- [ ] V2 in maintenance mode (read-only)
- [ ] V3 staging database empty or disposable
- [ ] Export row counts match V2 source queries
- [ ] Import validation queries (user count, entry count)
- [ ] Sample login with migrated user (or forced reset email)
- [ ] Spot-check 10 random entries in V3 UI
- [ ] Rollback plan: drop staging DB and re-import

---

## Password Handling

V2 and V3 both use BCrypt. If cost/parameters match, hashes **may** be copied directly into `users.password_hash`. Otherwise:

1. Import users with `status = PendingVerification`
2. Send password reset email on first V3 login campaign

---

## Files

| Script | Purpose |
|--------|---------|
| `export-v2-users.ps1` | Export users, roles, agencies (read-only) |
| `import-v3-staging.ps1` | Load CSV into V3 staging tables |
| `validate-etl.ps1` | Row count + FK integrity checks |

---

## Support

For full production ETL engagement, allocate 1 backend dev + 1 DBA for 1–2 weeks beyond MVP timeline (per [01-PLAN.md](../docs/01-PLAN.md) §6).
