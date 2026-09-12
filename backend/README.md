# Backend — ASP.NET Core 7 Web API

> **Status:** Phase P0 complete ✅

## Structure

```
backend/
├── AgriCheck.sln
└── src/
    ├── AgriCheck.Api/              # Controllers, Program.cs, Swagger
    ├── AgriCheck.Application/      # Use cases (Phase P1+)
    ├── AgriCheck.Domain/           # Entities, enums
    └── AgriCheck.Infrastructure/     # EF Core, MySQL, migrations
```

## Run locally

```powershell
cd c:\xampp\htdocs\agricheck-v3\backend\src\AgriCheck.Api
dotnet run --launch-profile http
```

- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- Health: http://localhost:5000/api/v1/health

## Database

```powershell
cd c:\xampp\htdocs\agricheck-v3\backend
dotnet ef database update -p src/AgriCheck.Infrastructure -s src/AgriCheck.Api
```

Connection string in `src/AgriCheck.Api/appsettings.json` (see `.env.example` for overrides).

## Migrations

| Migration | Tables |
|-----------|--------|
| InitialPlatform | users, roles, agencies, refresh_tokens, audit_logs, system_settings, ... |

See [../docs/03-DATABASE.md](../docs/03-DATABASE.md).
