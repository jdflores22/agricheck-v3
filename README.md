# AgriCheck V3

Greenfield rebuild of AgriCheck using a modern API-first stack, separate from the legacy Symfony app at `../agricheck`.

## Quick Facts

| Item | Value |
|------|-------|
| Project name | **AgriCheck V3** |
| Legacy reference | `c:\xampp\htdocs\agricheck` (Symfony 6.4 — read-only reference) |
| Location | `c:\xampp\htdocs\agricheck-v3` |
| Database | **Fresh** MySQL 8 schema — `agricheck_v3` (no V2 data migration in Phase 1) |
| Backend | ASP.NET Core **7** Web API | LTS upgrade to .NET 8 optional later |
| Frontend | React 18 + Vite + TypeScript + MUI |
| State | Redux Toolkit |
| Cache | Redis 7 |
| ORM | Entity Framework Core + Pomelo (MySQL) |
| Auth | JWT + Refresh Token |
| QR | QRCoder (.NET) |

## Folder Layout

```
agricheck-v3/
├── backend/          # .NET Web API (to be scaffolded in Phase 0)
├── frontend/         # React SPA (to be scaffolded in Phase 0)
├── database/         # SQL seeds, migration notes, ER diagrams
└── docs/             # Plan, architecture, tasks, progress
```

## Documentation

| Doc | Purpose |
|-----|---------|
| [docs/01-PLAN.md](docs/01-PLAN.md) | Master plan, phases, timeline, risks |
| [docs/02-ARCHITECTURE.md](docs/02-ARCHITECTURE.md) | System design, modules, API conventions |
| [docs/03-DATABASE.md](docs/03-DATABASE.md) | Fresh schema strategy and domain groups |
| [docs/04-TASKS.md](docs/04-TASKS.md) | Detailed task checklist with status |
| [docs/05-MODULE-MAP.md](docs/05-MODULE-MAP.md) | V2 feature → V3 module mapping |
| [docs/PROGRESS.md](docs/PROGRESS.md) | High-level progress dashboard |

## Local URLs (planned)

| Service | URL |
|---------|-----|
| React dev | `http://localhost:5173` |
| API | `http://localhost:5000` or `http://127.0.0.1:5000` |
| Legacy V2 (reference) | `http://127.0.0.1:8000` |

## Getting Started

1. Read [docs/01-PLAN.md](docs/01-PLAN.md) for phase order.
2. Track work in [docs/04-TASKS.md](docs/04-TASKS.md) and [docs/PROGRESS.md](docs/PROGRESS.md).
3. Phase 0 ✅ — scaffold `backend/` and `frontend/` complete.

## Important Decisions

- **Fresh database** — V3 owns its schema from day one; V2 is reference only for business rules and UX.
- **Separate folder** — no shared code with Symfony; parallel deployment during transition.
- **.NET 8 LTS** — recommended over .NET 7 (EOL); same stack as your target screenshot.
