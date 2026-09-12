# Database

Fresh MySQL 8 schema for AgriCheck V3.

## Setup

1. Start MySQL via XAMPP
2. Run `init/001_create_database.sql` in phpMyAdmin or CLI:

```powershell
mysql -u root -p < init/001_create_database.sql
```

3. EF Core migrations (after Phase 0 scaffold) will create all tables

## Structure

```
database/
├── init/           # Manual SQL scripts (database creation, seeds)
├── seeds/          # Dev seed data (future)
└── diagrams/       # ER diagrams per domain group (future)
```

## Connection

```
Database: agricheck_v3
Host:     localhost
Port:     3306
User:     agricheck_v3
Password: (see .env — not committed)
```

See [../docs/03-DATABASE.md](../docs/03-DATABASE.md) for full schema design.
