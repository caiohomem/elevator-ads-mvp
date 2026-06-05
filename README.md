# Elevator Ads MVP

Elevator Ads MVP is a programmatic DOOH advertising platform for elevator screens.

This repository contains the full-stack scaffold and the initial feature set: Building management, Screen management, Advertisers, Creatives, Campaigns, Campaign Booking Requests, Campaign Delivery Constraints, Daily Playlist generation, Playlist Download, Proof-of-Play reporting, Delivery Reports, and an External Buyer Simulator for forecast requests. Database integration uses PostgreSQL with Entity Framework Core. Authentication, SSP/DSP logic, auctioning, and OpenRTB support are intentionally out of scope for this phase.

The MVP delivery model is scheduled DOOH playlist delivery. Screens and players download a daily playlist, execute the same programmed sequence throughout the day, and report playback after execution rather than requesting ads in real time.

The External Buyer Simulator is a DSP/SSP-style forecasting surface adapted to that scheduled delivery model. It allows a simulated external buyer to estimate eligible inventory, plays, audience, cost, and rough capacity before API keys, external buyer authentication, or any OpenRTB adapter exist.

## Playlist Simulation

Playlist simulation is an internal planning tool for the scheduled elevator delivery model. It helps operators estimate what a daily loop would look like for a booking request, campaign, or inventory package before any real playlist is published.

The simulator does not publish or persist a `DailyPlaylist`. It estimates loop duration, repeats per day, plays per creative, total plays, audience, and capacity warnings using a simple deterministic MVP algorithm aligned with the once-per-day scheduled playlist workflow.

## Campaign Booking Requests

A campaign booking request captures commercial demand before it becomes a real campaign. It is the structured handoff between an advertiser or operator expressing intent and the later operational steps such as forecast review, approval, campaign creation, and daily playlist allocation.

This workflow is intentionally not OpenRTB and not real-time bidding. Elevator screens do not request ads live. A booking request describes a planned scheduled DOOH buy, such as a 15-second campaign in selected cities and building types for a fixed date range and budget, which can later be reviewed and converted by operators in a later issue.

## Inventory Packages

An inventory package is a sellable grouping of elevator media inventory. It is the SSP-lite commercial layer between raw buildings/screens and later workflow steps such as booking requests, forecasts, and external buyer-facing APIs.

Packages can represent operator-friendly bundles such as "Lisbon Corporate Buildings", "Premium Residential Buildings", or "All Screens Network". A package may explicitly include specific buildings or screens, or it may use reusable filters such as city, building type, and screen orientation. Empty filter fields mean the package applies to all relevant inventory on that dimension.

Screen matching is intentionally simple and deterministic in the MVP. A screen belongs to a package when it is explicitly listed in `ScreenIds`, when its building is listed in `BuildingIds`, or when it matches the package filters. This keeps inventory packaging aligned with the current scheduled-playlist delivery model and avoids introducing availability calculation, automated allocation, OpenRTB, or real-time bidding behavior in this issue.

## Campaign Forecasts

A campaign forecast is the estimate generated for a booking request before approval or campaign conversion. It helps sales and operations judge whether the requested date range and targeting can be supported by the currently known inventory.

The MVP forecast is an estimate only, not proof-of-play. It uses the booking request filters, and later inventory packages, to reason about cities, building types, and screen orientations; matches those rules against known buildings and active screens; estimates plays using a simple 480-second scheduled playlist loop assumption; estimates audience from building-level daily audience values when available; and estimates cost with a placeholder base CPM of 10. Warnings are returned whenever the forecast depends on incomplete data or placeholder assumptions.

In the commercial flow, forecast sits between booking request capture and any later approval or manual campaign conversion. It does not create campaigns automatically, allocate playlist slots automatically, or introduce OpenRTB or real-time bidding behavior.

## Tech Stack

- Backend: C# / .NET, ASP.NET Core Web API
- Frontend: Next.js, TypeScript
- Database: PostgreSQL with Entity Framework Core
- Future deployment targets: Render, Vercel, Neon PostgreSQL

## Local Development

### Backend

The backend uses PostgreSQL via Entity Framework Core. Before running the API for the first time, provision a local database and apply the migrations.

1. Install PostgreSQL 15+ and ensure `psql` is available.
2. Create a development database (default name: `elevator_ads_dev`):

   ```bash
   createdb elevator_ads_dev
   ```

   The `appsettings.Development.json` file in `backend/ElevatorAds.Api` includes a placeholder connection string that points at `Host=localhost;Port=5432;Database=elevator_ads_dev;Username=postgres;Password=postgres`. Override it for your local environment using one of the following:

   - Environment variable (highest priority, recommended for local secrets):

     ```bash
     export ConnectionStrings__Default="Host=localhost;Port=5432;Database=elevator_ads_dev;Username=postgres;Password=YOUR_PASSWORD"
     ```

   - .NET user-secrets for the API project:

     ```bash
     cd backend/ElevatorAds.Api
     dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=elevator_ads_dev;Username=postgres;Password=YOUR_PASSWORD"
     ```

   Do not commit real credentials.

3. Apply the EF Core migrations:

   ```bash
   cd backend
   dotnet ef database update --project ElevatorAds.Infrastructure --startup-project ElevatorAds.Api
   ```

4. Build, test, and run:

   ```bash
   cd backend
   dotnet build
   dotnet test
   dotnet run --project ElevatorAds.Api
   ```

The API listens on `http://localhost:5000` in local development.

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

For local frontend integration, the API applies a config-driven CORS policy. In `Development`, local admin origins are allowed by default:

- `http://localhost:3000`
- `http://127.0.0.1:3000`

Other environments do not allow cross-origin browser access unless `Cors:AllowedOrigins` is explicitly configured.

### Frontend

```bash
cd frontend/elevator-ads-admin
npm install
npm run dev
```

The admin app runs on `http://localhost:3000`.

Create a local frontend env file from the example and point it to the backend API:

```bash
cd frontend/elevator-ads-admin
cp .env.example .env.local
```

```text
NEXT_PUBLIC_API_BASE_URL=http://localhost:5000
```

The admin app includes both a Next.js rewrite proxy and direct browser requests to `NEXT_PUBLIC_API_BASE_URL`, so the backend CORS settings above are required for local cross-origin API calls.

## Docker Compose

Run the stack with its own PostgreSQL container:

```bash
docker compose up -d --build
```

Published ports:

```text
Frontend: http://localhost:3001
Backend:  http://localhost:8081
Postgres: localhost:5434
```

This Compose stack uses its own named volume, `elevator_ads_postgres_data`. The current application code still uses in-memory repositories, so the Postgres service is provisioned and isolated for this project, but it is not yet consumed by the running API.

## Current Scope

- Clean repository structure
- Buildable .NET solution
- Minimal API health endpoint
- Building management CRUD with in-memory persistence
- Campaign booking request workflow before campaign creation
- Inventory package CRUD and matching for sellable DOOH groupings
- Backend test project with health and building endpoint coverage
- Basic Next.js admin app
- Initial documentation

## Delivery Model

The first delivery model for Elevator Ads MVP is not real-time ad serving. Each screen or player is expected to download a `DailyPlaylist` once per day, repeat that same ordered sequence throughout the day, and send playback or proof-of-play data later. Real-time `next-ad` decisions, auction logic, DSP/SSP bidding, and OpenRTB are intentionally out of scope for the MVP.

## Estimated Proof-of-Play Report

The MVP also exposes an estimated proof-of-play report for advertiser-facing delivery evidence. This report separates three different concepts:

- Actual reported plays: proof-of-play events sent back by screens after playback.
- Scheduled plays: playlist rows stored for a given screen and day, used as fallback when proof-of-play is missing.
- Estimated audience and impressions: values derived from each building's `EstimatedDailyAudience`, not from direct passenger counting.

This distinction matters. The platform can know what was scheduled and what was later reported as played, but the number of people inside an elevator is still an estimate in this MVP. The report does not claim measured people counts, unique reach, or computer-vision verification.

The current assumptions are intentionally simple and deterministic:

- Proof-of-play events are the primary source for reported playback.
- The latest stored daily playlist for a screen and date is used to estimate scheduled delivery when playback data is absent or incomplete.
- Building-level daily audience is apportioned across that screen's effective plays for the day.
- Estimated audience and estimated impressions are the same numeric value in the MVP report because the system does not yet measure rider-level exposure or deduplicated reach.

Warnings are returned whenever the report depends on fallback playlist data or when buildings are missing audience inputs. This keeps the report useful for advertiser communication without overstating what the system currently measures.

## Future Roadmap Summary

Future phases include screen management, advertiser and campaign management, creative management, campaign delivery constraints, daily playlist generation, playlist download, proof-of-play tracking, reports, richer external buyer APIs, and only later SSP/DSP models with API keys and an OpenRTB adapter.

See [docs/roadmap.md](docs/roadmap.md) for the full roadmap.
