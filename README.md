# Elevator Ads MVP

Elevator Ads MVP is a programmatic DOOH advertising platform for elevator screens.

This repository currently contains the initial full-stack scaffold only. Business features, database integration, authentication, SSP/DSP logic, auctioning, and OpenRTB support are intentionally out of scope for this phase.

## Tech Stack

- Backend: C# / .NET, ASP.NET Core Web API
- Frontend: Next.js, TypeScript
- Future database: PostgreSQL with Entity Framework Core
- Future deployment targets: Render, Vercel, Neon PostgreSQL

## Local Development

### Backend

```bash
cd backend
dotnet build
dotnet test
dotnet run --project ElevatorAds.Api
```

The API exposes a health endpoint at:

```text
GET /health
```

Expected response:

```json
{
  "status": "ok"
}
```

### Frontend

```bash
cd frontend/elevator-ads-admin
npm install
npm run dev
```

The admin app starts a basic landing page for the product.

## Current Scope

- Clean repository structure
- Buildable .NET solution
- Minimal API health endpoint
- Backend test project with a health smoke test
- Basic Next.js admin app
- Initial documentation

## Future Roadmap Summary

Future phases include building management, screen management, advertiser and campaign management, creative management, next-ad decisions, proof-of-play tracking, reports, SSP/DSP models, auctioning, and an OpenRTB adapter.

See [docs/roadmap.md](docs/roadmap.md) for the full roadmap.
