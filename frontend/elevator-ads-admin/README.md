# Elevator Ads Admin

Admin frontend shell for the Elevator Ads MVP.

## Frontend Path

`frontend/elevator-ads-admin`

## Scope

- Responsive admin shell for dashboard operations
- Pages for buildings, screens, advertisers, creatives, campaigns, playlists, reports, and settings
- English and Portuguese language structure
- Light and dark mode toggle with local persistence
- API-backed resource pages with loading, error, empty, and data states

The current delivery model is scheduled DOOH playlist publishing and download. No authentication or real-time ad serving is included here.

## API Configuration

Copy `.env.example` to `.env.local` for local development and set the backend base URL:

```bash
NEXT_PUBLIC_API_BASE_URL=http://localhost:5000
```

The Next.js app proxies `/api/*` requests to this backend URL so browser requests do not require backend CORS configuration.

## Run

Start the backend API from the repository root:

```bash
dotnet run --project backend/ElevatorAds.Api
```

In a second terminal, install dependencies and start the frontend development server:

```bash
npm install
npm run dev
```

Open `http://localhost:3000`.

## Build

```bash
npm run build
```

## Lint

```bash
npm run lint
```

## Notes

- Supported languages: `en`, `pt`
- Theme support: light, dark
- API client: `lib/api/client.ts`
- API-backed pages: buildings, screens, advertisers, creatives, campaigns, daily playlists
- Mock-only: dashboard summary, recent activity, and placeholder reports until backend endpoints exist
- When the API is unavailable, resource pages show a translated error state instead of silently falling back to mock data
