# Elevator Ads Admin

Admin frontend shell for the Elevator Ads MVP.

## Frontend Path

`frontend/elevator-ads-admin`

## Scope

- Responsive admin shell for dashboard operations
- Pages for buildings, screens, advertisers, creatives, campaigns, playlists, reports, and settings
- English and Portuguese language structure
- Light and dark mode toggle with local persistence
- Mocked data only for now

The current delivery model is scheduled DOOH playlist publishing and download. No authentication, real API integration, or real-time ad serving is included here.

## Run

Install dependencies and start the development server:

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
- Data source: centralized mock data in `lib/mockData.ts`
- Future backend integration can replace the async helpers in `lib/api.ts`
