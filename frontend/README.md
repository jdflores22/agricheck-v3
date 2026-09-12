# Frontend — React 18 + Vite + TypeScript + MUI

> **Status:** Phase P0 complete ✅

## Structure

```
frontend/src/
├── app/           # store, router, providers
├── features/      # health, home, ...
├── layouts/       # PublicLayout (client/agency layouts in P1+)
└── theme/         # MUI forest/stone theme
```

## Run locally

```powershell
cd c:\xampp\htdocs\agricheck-v3\frontend
npm run dev
```

- App: http://localhost:5173
- API proxy: `/api` → http://localhost:5000

## Build

```powershell
npm run build
```

## Tech

- React 18 + Vite 5 + TypeScript
- MUI 9 (forest/stone theme)
- Redux Toolkit + RTK Query
- React Router 7

See [../docs/02-ARCHITECTURE.md](../docs/02-ARCHITECTURE.md).
