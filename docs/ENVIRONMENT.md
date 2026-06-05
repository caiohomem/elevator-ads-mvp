# Environment Variables

This document lists every environment variable read by the backend (`ElevatorAds.Api`) and the frontend (`elevator-ads-admin`). Placeholders and a working local override live in [`.env.example`](.env.example) at the repository root.

> **Never commit real credentials.** All secrets — database passwords, JWT signing keys, seed passwords — must be injected at deploy time by the WebService (Render, Railway, Fly.io, etc.). Rotate the JWT secret on every environment.

---

## Backend — `ElevatorAds.Api`

The backend is an ASP.NET Core 10 Minimal API. Configuration is read in this order (last wins):

1. `appsettings.json`
2. `appsettings.{ASPNETCORE_ENVIRONMENT}.json`
3. Environment variables (double-underscore `__` becomes a section separator, e.g. `ConnectionStrings__Default`)
4. Command-line arguments

| Variable | Required | Default | Purpose |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | no | `Production` | `Development` enables developer exception page and verbose logging |
| `ASPNETCORE_URLS` | no | `http://+:8080` (Dockerfile) | HTTP listen URLs. Set to your public URL behind a reverse proxy |
| `ConnectionStrings__Default` | **yes** | `appsettings.Development.json` | PostgreSQL connection string. Example: `Host=ep-xyz-pooler.region.aws.neon.tech;Port=5432;Database=neondb;Username=neondb_owner;Password=***;Ssl Mode=Require` |
| `Cors__AllowedOrigins` | yes (prod) | `appsettings.Development.json` | Comma-separated list of allowed frontend origins, no trailing slash |
| `Jwt__Issuer` | no | `ElevatorAds` | JWT `iss` claim |
| `Jwt__Audience` | no | `ElevatorAds.Clients` | JWT `aud` claim |
| `JWT__Secret` | **yes (prod)** | dev-only fallback (32+ bytes) | HS256 signing key. **Must be at least 32 bytes**; generate with `openssl rand -base64 48` |
| `SEED_ADMIN_USERNAME` | no | — | If set with `SEED_ADMIN_PASSWORD`, creates an admin on first startup |
| `SEED_ADMIN_PASSWORD` | no | — | Required alongside `SEED_ADMIN_USERNAME` |
| `SEED_OPERATOR_PASSWORD` | no | `Operator1!` | Demo operator password, used only by `DemoDataSeeder` |
| `SEED_VIEWER_PASSWORD` | no | `Viewer1!` | Demo viewer password, used only by `DemoDataSeeder` |

### Production checklist

- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__Default` set to the NEON pooled connection string (`-pooler` host with `Ssl Mode=Require`)
- `JWT__Secret` is a fresh 32+ byte random string, not the dev fallback
- `Cors__AllowedOrigins` lists the deployed frontend origin(s) only
- `SEED_ADMIN_USERNAME` and `SEED_ADMIN_PASSWORD` set to a real password, then optionally removed after the first run

---

## Frontend — `elevator-ads-admin`

The frontend is a Next.js 15 app. Only variables prefixed with `NEXT_PUBLIC_` are exposed to the browser and are baked into the build at **build time** (not runtime).

| Variable | Required | Default | Purpose |
|---|---|---|---|
| `NEXT_PUBLIC_API_BASE_URL` | yes (prod) | `http://localhost:5000` | Base URL of the backend (scheme + host + port, no trailing slash). Used by the API client (`lib/api/client.ts`) and by Next.js rewrites in `next.config.ts` |

> Set this at **build time**. Changing it after `docker build` requires a rebuild.

---

## NEON setup

When targeting NEON Postgres:

1. Create the project in the NEON console and copy the **pooled** connection string (the one containing `-pooler` in the host). Pooled connections are required because the API uses Npgsql with EF Core, which keeps short-lived connections; the pooler reuses server-side connections safely.
2. Store the connection string in your WebService's secret store (e.g. Render "Secret Files", Railway "Variables", Fly.io "Secrets") under the name `ConnectionStrings__Default`.
3. The API applies EF Core migrations automatically on startup (see PR #84 / issue #83). The first deploy will create all tables.
4. `DemoDataSeeder` is short-circuited against the InMemory provider used in tests, but it **will** run against a real database on the first startup. Set `SEED_ADMIN_USERNAME` / `SEED_ADMIN_PASSWORD` (and optionally `SEED_OPERATOR_PASSWORD` / `SEED_VIEWER_PASSWORD`) to control the demo data; the seeders are idempotent and skip when data already exists.

Example NEON connection string shape (do **not** commit a real one):

```text
Host=ep-polished-rice-aqc7no8h-pooler.c-8.us-east-1.aws.neon.tech;Port=5432;Database=neondb;Username=neondb_owner;Password=<from-neon-console>;Ssl Mode=Require
```

---

## Local override (`.env`)

For local Docker Compose runs, copy `.env.example` to `.env`, edit the values, then `docker compose up`. The compose file in [`docker-compose.yml`](../docker-compose.yml) reads the same variable names with the `__` → nested-key convention.
