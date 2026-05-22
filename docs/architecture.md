# Architecture

Elevator Ads MVP is intended to use a layered backend architecture and a standalone frontend application.

## Backend

The backend is organized as a .NET solution with the following project responsibilities:

- `ElevatorAds.Api`: HTTP API layer and application composition root.
- `ElevatorAds.Application`: future use cases and application services.
- `ElevatorAds.Domain`: future domain entities, value objects, and business rules.
- `ElevatorAds.Infrastructure`: future persistence and external integrations.
- `ElevatorAds.Tests`: automated tests for the backend.

The intended dependency direction is:

```text
Api -> Application -> Domain
Infrastructure -> Application
Tests -> Api / Application / Domain
```

PostgreSQL and Entity Framework Core will be added later in `Infrastructure`. This bootstrap phase does not include database integration, authentication, or business entities.

## Frontend

The frontend is a Next.js TypeScript app under `frontend/elevator-ads-admin`.

For this bootstrap phase, the frontend is a basic admin shell only. It does not call the backend and does not include dashboard features.
