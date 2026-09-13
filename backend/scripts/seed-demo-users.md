# Demo users (local + production)

Demo accounts are created idempotently by `DatabaseSeeder.SeedDemoAccountsAsync` on API startup and via:

```bash
cd backend/tools/DemoUserSeederRunner
$env:ConnectionStrings__DefaultConnection = "Server=<host>;Port=3306;Database=<db>;User=<user>;Password=<pwd>;"
dotnet run
```

## Key accounts

| Email | Password | Role |
|-------|----------|------|
| admin@agricheck.local | Admin@12345 | Admin |
| operator@agricheck.local | Operator@12345 | Operator |
| driver@agricheck.local | Driver@12345 | Driver |
| importer@agricheck.local | Importer@12345 | Importer |
| warehouse@agricheck.local | Warehouse@12345 | Warehouse |
| doctor@agricheck.local | Doctor@12345 | Doctor |

AgriTrack invite code: `AGRITRACK-DEMO`
