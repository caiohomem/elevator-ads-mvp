# Elevator Ads MVP

Elevator Ads MVP is a programmatic DOOH advertising platform for elevator screens.

This repository currently contains the initial full-stack scaffold and the first inventory feature: Building management. Database integration, authentication, SSP/DSP logic, auctioning, and OpenRTB support are intentionally out of scope for this phase.

The MVP delivery model is scheduled DOOH playlist delivery. Screens and players download a daily playlist, execute the same programmed sequence throughout the day, and report playback after execution rather than requesting ads in real time.

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

The API also exposes Building management endpoints:

```text
GET    /api/buildings
GET    /api/buildings/{id}
POST   /api/buildings
PUT    /api/buildings/{id}
DELETE /api/buildings/{id}
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
- Building management CRUD with in-memory persistence
- Backend test project with health and building endpoint coverage
- Basic Next.js admin app
- Initial documentation

## Delivery Model

The first delivery model for Elevator Ads MVP is not real-time ad serving. Each screen or player is expected to download a `DailyPlaylist` once per day, repeat that same ordered sequence throughout the day, and send playback or proof-of-play data later. Real-time `next-ad` decisions, auction logic, DSP/SSP bidding, and OpenRTB are intentionally out of scope for the MVP.

## Future Roadmap Summary

Future phases include screen management, advertiser and campaign management, creative management, campaign delivery constraints, daily playlist generation, playlist download, proof-of-play tracking, reports, and only later SSP/DSP models and an OpenRTB adapter.

See [docs/roadmap.md](docs/roadmap.md) for the full roadmap.
