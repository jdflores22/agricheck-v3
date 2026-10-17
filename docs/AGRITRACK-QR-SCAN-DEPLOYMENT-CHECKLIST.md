# AgriTrack / AgriCheck — QR scan security & deployment checklist

Use this before enabling **operator (client transport)** and **doctor (driver handoff)** camera scanning in production. The web UI is not the security boundary; the API and signed QR payloads are.

Related: [P7-DEPLOYMENT-RUNBOOK.md](./P7-DEPLOYMENT-RUNBOOK.md), [P7-SECURITY-REVIEW.md](./P7-SECURITY-REVIEW.md).

---

## 1. QR roles (must stay separated)

| Scanner role | QR source | JSON `type` | API endpoint |
|--------------|-----------|-------------|--------------|
| **Operator** | Client / agency transport tag | `agricheck_transport` | `POST /api/v1/ops/operator/containers/scan` |
| **Doctor** | Driver AgriTrack live warehouse QR | `agricheck_delivery_handoff` | `POST /api/v1/ops/doctor/containers/scan-handoff` |

Server verifies signature, payload type, database token/tag match, status, and business rules. Frontend preset checks are UX only.

---

## 2. Pre-production checklist

### TLS & hosting

| # | Check | Done |
|---|--------|------|
| 1 | SPA served over **HTTPS** (browser camera requires secure context except localhost) | ☐ |
| 2 | API over **HTTPS**; no mixed-content calls from SPA | ☐ |
| 3 | `Cors__AllowedOrigins` lists only production SPA host(s) | ☐ |

### Secrets (Railway / server env)

| # | Variable | Requirement | Done |
|---|----------|-------------|------|
| 1 | `Jwt__SecretKey` | Strong random (≥ 32 chars); **not** default from `appsettings.json` | ☐ |
| 2 | `App__QrSigningKey` | Optional override; if unset, `Jwt__SecretKey` is used for QR HMAC | ☐ |
| 3 | QR signing key **never** in frontend, git, or client apps | ☐ |
| 4 | MySQL and other credentials only in env / secret store | ☐ |

### Roles & accounts

| # | Check | Done |
|---|--------|------|
| 1 | Only `ROLE_OPERATOR` (+ admin) can call operator scan API | ☐ |
| 2 | Only `ROLE_DOCTOR` / `ROLE_INSPECTOR` (+ admin) can call doctor handoff API | ☐ |
| 3 | Periodic audit: disable unused operator/doctor accounts | ☐ |
| 4 | Strong passwords / MFA policy per agency ICT standards | ☐ |

### QR operational security

| # | Check | Done |
|---|--------|------|
| 1 | Driver handoff QR **~90s** TTL — train doctors to scan live screen, not screenshots | ☐ |
| 2 | Handoff QR **one-time** — replays logged as compliance events | ☐ |
| 3 | Transport QR must match **issued tag** in DB; claim only when `AwaitingConfirmation` | ☐ |
| 4 | Handoff blocked when **compliance hold** or driver not **warehouse-eligible** | ☐ |

---

## 3. Post-deploy smoke test

| Step | Action | Expected |
|------|--------|----------|
| 1 | Operator opens **Container loads → Scan client transport QR** | Camera or paste works |
| 2 | Scan **wrong** type (driver handoff) as operator | Clear error before or from API |
| 3 | Valid client transport QR | Container claimed |
| 4 | Doctor opens **Scan delivery QR** | Camera works on HTTPS |
| 5 | Scan client transport QR as doctor | Rejected (wrong type) |
| 6 | Driver at warehouse shows live AgriTrack QR; doctor scans | Handoff accepted, trip ended |
| 7 | Repeat same handoff QR | `QR_ALREADY_USED` |
| 8 | Driver portal **Trip & DA NMIS** shows scan + inspection times after doctor completes | Timeline updates |

---

## 4. Key rotation (planned)

| Asset | When | Steps |
|-------|------|--------|
| `Jwt__SecretKey` | Compromise or annual policy | Issue new key → deploy API → force re-login; update QR key if shared |
| `App__QrSigningKey` | If dedicated from JWT | Rotate in env → deploy → **re-issue** active transport tags / driver must refresh handoff QR |

After rotation, old signed QRs fail verification until refreshed.

---

## 5. Deploy commands (reference)

**API (Railway):** push `main` — service rebuilds from repo (confirm Railway watches `main`).

**SPA (Hostinger — SSH/SFTP, recommended):**

1. hPanel → **SSH Access** (host `82.25.100.95`, port **65002**, user + password).
2. Copy `scripts/hostinger.deploy.example.json` → `scripts/hostinger.deploy.local.json` (gitignored).
3. Fill `sshHost`, `sshPort`, `sshUser`, `sshPassword`, and `remotePath` (see example).
4. Run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/deploy-frontend-hostinger.ps1
```

Or env-only: `HOSTINGER_SSH_HOST`, `HOSTINGER_SSH_PORT`, `HOSTINGER_SSH_USER`, `HOSTINGER_SSH_PASSWORD`, then `node scripts/deploy-hostinger-sftp.mjs` (after `npm run build` in `frontend/`).

Without local config, the script still builds; upload `frontend/dist/` manually to `public_html`.

**Verify production API** (example):

```text
https://agricheck-v3-production.up.railway.app/health
```

---

## Sign-off

| Item | Owner | Date | Done |
|------|-------|------|------|
| Checklist reviewed | | | ☐ |
| Secrets verified on Railway / host | | | ☐ |
| HTTPS smoke test (operator + doctor QR) | | | ☐ |
| Document shared with NMIS / ops trainers | | | ☐ |
