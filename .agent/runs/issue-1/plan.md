Repo is essentially empty (only `.git` and `.agent/runs/`). I have enough to write the plan.

---

# Elevator Ads MVP — Bootstrap Implementation Plan

**Goal:** Bootstrap the repository with a clean full-stack scaffold (.NET solution + Next.js app + docs). No business features, no DB, no auth.

**Architecture:** Backend is a Clean-Architecture-style .NET solution with 5 projects (Api, Application, Domain, Infrastructure, Tests). Frontend is a fresh Next.js + TypeScript app. Both stand alone — no cross-calls in this issue. Docs live at the repo root and under `docs/`.

**Tech stack:** .NET 8 (LTS) / ASP.NET Core Minimal API, xUnit, Next.js (App Router) + TypeScript.

---

## 1. Summary

This issue creates only the *skeleton*: a buildable .NET solution exposing `GET /health` → `{"status":"ok"}`, a runnable Next.js homepage showing the product name + tagline, and the docs (`README.md`, `AGENTS.md`, `docs/architecture.md`, `docs/roadmap.md`) plus a `.gitignore`. Nothing else.

The repo currently contains only `.git/` and `.agent/runs/`. The work is purely additive.

---

## 2. File Structure / Files Likely to Change

All files are **new** (the repo is empty).

```
elevator-ads-mvp/
├── .gitignore                                           (new)
├── README.md                                            (new)
├── AGENTS.md                                            (new)
├── docs/
│   ├── architecture.md                                  (new)
│   └── roadmap.md                                       (new)
├── backend/
│   ├── ElevatorAds.sln                                  (new)
│   ├── ElevatorAds.Api/
│   │   ├── ElevatorAds.Api.csproj                       (new)
│   │   ├── Program.cs                                   (new — minimal API + /health)
│   │   ├── appsettings.json                             (new — default scaffold)
│   │   └── Properties/launchSettings.json               (new — default scaffold)
│   ├── ElevatorAds.Domain/
│   │   └── ElevatorAds.Domain.csproj                    (new — empty classlib)
│   ├── ElevatorAds.Application/
│   │   └── ElevatorAds.Application.csproj               (new — refs Domain)
│   ├── ElevatorAds.Infrastructure/
│   │   └── ElevatorAds.Infrastructure.csproj            (new — refs Application)
│   └── ElevatorAds.Tests/
│       ├── ElevatorAds.Tests.csproj                     (new — xUnit, refs Api/App/Domain)
│       └── HealthEndpointTests.cs                       (new — smoke test for /health)
└── frontend/
    └── elevator-ads-admin/                              (new — `create-next-app` output)
        ├── package.json
        ├── tsconfig.json
        ├── next.config.* / next-env.d.ts
        ├── app/page.tsx                                 (overwritten — landing copy)
        ├── app/layout.tsx                               (default — title updated)
        └── ...standard Next.js files
```

**Responsibilities:**
- `ElevatorAds.Api` — HTTP layer only. Hosts `/health`. Composition root.
- `ElevatorAds.Application` — future use-cases. Empty class library now.
- `ElevatorAds.Domain` — future entities/value objects. Empty class library now.
- `ElevatorAds.Infrastructure` — future DB/external integrations. Empty class library now.
- `ElevatorAds.Tests` — xUnit project; one smoke test that hits `/health`.
- `frontend/elevator-ads-admin` — Next.js admin shell. Landing page only.

---

## 3. Implementation Steps (small, sequential, each testable)

### Task 1 — Repo-level docs and `.gitignore`

**Files:** `.gitignore`, `README.md`, `AGENTS.md`, `docs/architecture.md`, `docs/roadmap.md`

- [ ] Create `.gitignore` combining GitHub's standard **VisualStudio** and **Node** templates (covers `bin/`, `obj/`, `*.user`, `.vs/`, `node_modules/`, `.next/`, `out/`, `.env*`, `*.log`). Add an `.idea/` line for JetBrains users.
- [ ] Create `README.md` with the sections required by the spec: project description, tech stack, local dev instructions, backend run command (`cd backend && dotnet build && dotnet run --project ElevatorAds.Api`), frontend run command (`cd frontend/elevator-ads-admin && npm install && npm run dev`), current scope (bootstrap only), and a 1-line-per-phase roadmap summary pointing at `docs/roadmap.md`.
- [ ] Create `AGENTS.md` with the exact rules from the spec:
  - Work only on the requested issue
  - Do not implement unrelated features
  - Do not commit secrets
  - Do not add authentication unless requested
  - Do not add OpenRTB yet
  - Do not add SSP/DSP logic yet
  - Keep PRs small and focused
  - Run tests/build before finishing
- [ ] Create `docs/architecture.md` describing the intended layered architecture (Api → Application → Domain; Infrastructure → Application; tests cross-cut). Note that PostgreSQL/EF Core will land in Infrastructure later.
- [ ] Create `docs/roadmap.md` listing the 13 phases from the spec, in order, with a one-line description each.

**Verify:** `ls` shows all five files. No build/test required for this task.

**Commit:** `chore: add repo docs, AGENTS guide, and .gitignore`

---

### Task 2 — Backend solution + empty class library projects

**Files:** `backend/ElevatorAds.sln`, `backend/ElevatorAds.Domain/`, `backend/ElevatorAds.Application/`, `backend/ElevatorAds.Infrastructure/`

Use the .NET CLI rather than hand-rolling `.csproj` files so versions match the installed SDK.

- [ ] `cd backend && dotnet new sln -n ElevatorAds`
- [ ] `dotnet new classlib -n ElevatorAds.Domain -o ElevatorAds.Domain`
- [ ] `dotnet new classlib -n ElevatorAds.Application -o ElevatorAds.Application`
- [ ] `dotnet new classlib -n ElevatorAds.Infrastructure -o ElevatorAds.Infrastructure`
- [ ] Delete the default `Class1.cs` from each new project (keep them empty).
- [ ] Add project references:
  - `dotnet add ElevatorAds.Application reference ElevatorAds.Domain`
  - `dotnet add ElevatorAds.Infrastructure reference ElevatorAds.Application`
- [ ] Add all three projects to the solution: `dotnet sln add ElevatorAds.Domain ElevatorAds.Application ElevatorAds.Infrastructure`

**Verify:** `dotnet build` from `backend/` succeeds (3 projects, no errors).

**Commit:** `feat(backend): scaffold Domain/Application/Infrastructure projects`

---

### Task 3 — Backend Api project with `/health`

**Files:** `backend/ElevatorAds.Api/`

- [ ] `cd backend && dotnet new webapi --use-minimal-apis -n ElevatorAds.Api -o ElevatorAds.Api` (use `--use-minimal-apis` if the SDK template supports it; otherwise the default minimal-API template is fine).
- [ ] Remove the sample `WeatherForecast.cs` / weather endpoint that the template generates — we keep the Api lean.
- [ ] Open `ElevatorAds.Api/Program.cs` and replace its contents with a minimal API that maps `GET /health` to return `Results.Ok(new { status = "ok" })`. Keep the existing `WebApplication.CreateBuilder(args)` / `app.Run()` shell. Remove Swagger if it complicates the build; otherwise leave the default scaffold.
- [ ] Add the Application reference: `dotnet add ElevatorAds.Api reference ElevatorAds.Application` (Api transitively depends on Domain via Application — do **not** add a direct Domain reference).
- [ ] Add to solution: `dotnet sln add ElevatorAds.Api`

**Verify:**
- `dotnet build` succeeds.
- `dotnet run --project ElevatorAds.Api` starts the server; `curl http://localhost:<port>/health` returns `{"status":"ok"}` with HTTP 200.

**Commit:** `feat(api): add minimal API project with /health endpoint`

---

### Task 4 — Backend test project + `/health` smoke test

**Files:** `backend/ElevatorAds.Tests/ElevatorAds.Tests.csproj`, `backend/ElevatorAds.Tests/HealthEndpointTests.cs`

- [ ] `cd backend && dotnet new xunit -n ElevatorAds.Tests -o ElevatorAds.Tests`
- [ ] Delete the default `UnitTest1.cs`.
- [ ] Add the test-side dependency on the Api: `dotnet add ElevatorAds.Tests package Microsoft.AspNetCore.Mvc.Testing` (matches the chosen .NET version).
- [ ] Add project references: `dotnet add ElevatorAds.Tests reference ElevatorAds.Api ElevatorAds.Application ElevatorAds.Domain`
- [ ] Add to solution: `dotnet sln add ElevatorAds.Tests`
- [ ] Ensure `Program.cs` is reachable for `WebApplicationFactory` — either declare `public partial class Program {}` at the bottom of `Program.cs`, or add `<InternalsVisibleTo Include="ElevatorAds.Tests" />` if you prefer the attribute-style approach. The partial-class approach is the conventional one for minimal APIs.
- [ ] Create `HealthEndpointTests.cs` with a single xUnit test using `WebApplicationFactory<Program>`:

```csharp
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ElevatorAds.Tests;

public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task GetHealth_ReturnsOkStatus()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(body);
        Assert.Equal("ok", body!.Status);
    }

    private record HealthResponse(string Status);
}
```

**Verify:** `dotnet test` from `backend/` passes (1 test, 0 failures).

**Commit:** `test(api): add xUnit project and /health smoke test`

---

### Task 5 — Frontend Next.js app

**Files:** `frontend/elevator-ads-admin/` (full scaffold from `create-next-app`)

- [ ] From repo root: `mkdir -p frontend && cd frontend && npx --yes create-next-app@latest elevator-ads-admin --ts --eslint --app --src-dir=false --no-tailwind --no-import-alias --use-npm` (Tailwind is *later scope* per spec — explicitly opt out now).
- [ ] In `frontend/elevator-ads-admin/app/page.tsx`, replace the boilerplate content with a minimal landing page that renders exactly:

```tsx
export default function Home() {
  return (
    <main>
      <h1>Elevator Ads MVP</h1>
      <p>Programmatic DOOH platform for elevator screens.</p>
    </main>
  );
}
```

- [ ] In `frontend/elevator-ads-admin/app/layout.tsx`, update the `metadata.title` to `"Elevator Ads MVP"` and `metadata.description` to `"Programmatic DOOH platform for elevator screens."` Leave the rest of the layout as the template provides.
- [ ] Confirm `frontend/elevator-ads-admin/.gitignore` (added by create-next-app) covers `.next/`, `node_modules/`, etc. — no extra root-level entries needed if Task 1's root `.gitignore` already covers them.

**Verify:**
- `cd frontend/elevator-ads-admin && npm install` finishes clean.
- `npm run build` succeeds.
- `npm run dev` serves `http://localhost:3000` showing the heading + tagline.

**Commit:** `feat(frontend): scaffold Next.js admin app with landing page`

---

### Task 6 — Final verification + housekeeping

- [ ] From repo root: `cd backend && dotnet build && dotnet test` → both pass.
- [ ] From repo root: `cd frontend/elevator-ads-admin && npm run build` → succeeds.
- [ ] `git status` shows only the intended files; no `bin/`, `obj/`, `node_modules/`, `.next/` are tracked.
- [ ] Walk through the README's "run commands" yourself to confirm they work as written.

**Commit (if anything tweaked):** `chore: verify bootstrap commands`

---

## 4. Acceptance Criteria (mapped to spec)

| # | Criterion | Verified by |
|---|---|---|
| 1 | Repository structure created | `tree -L 3` matches the spec layout |
| 2 | Backend solution builds | `cd backend && dotnet build` exits 0 |
| 3 | `GET /health` exists | `curl localhost:<port>/health` → `{"status":"ok"}` |
| 4 | Backend tests project exists | `ElevatorAds.Tests` in `.sln`; `dotnet test` runs ≥1 test |
| 5 | Frontend Next.js app starts | `npm run build` succeeds; `npm run dev` serves the home page |
| 6 | `README.md` exists | File present with all required sections |
| 7 | `AGENTS.md` exists | File present with all listed rules |
| 8 | `docs/architecture.md` exists | File present |
| 9 | `docs/roadmap.md` exists | File present with all 13 phases |
| 10 | No business features | Grep for `Building`, `Screen`, `Advertiser`, `Campaign`, `Creative` returns no source classes |
| 11 | No database integration | No `Npgsql`, `Microsoft.EntityFrameworkCore`, or connection-string packages in any `.csproj` |
| 12 | No authentication | No `Microsoft.AspNetCore.Authentication*` packages; no `[Authorize]` attributes |

---

## 5. Build / Test Commands

**Backend:**
```bash
cd backend
dotnet build
dotnet test
dotnet run --project ElevatorAds.Api
# then in another shell:
curl -s http://localhost:5000/health   # or whichever port launchSettings.json assigns
```

**Frontend:**
```bash
cd frontend/elevator-ads-admin
npm install
npm run build
npm run dev   # http://localhost:3000
```

---

## 6. Risks & Mitigations

| Risk | Mitigation |
|---|---|
| `dotnet` SDK version mismatch on contributor machines | Don't hand-pin a TFM in this issue — let `dotnet new` choose the installed SDK's default. A `global.json` can be added in a later issue once the team agrees on a target. |
| `WebApplicationFactory<Program>` can't find `Program` in minimal-API style | Add `public partial class Program {}` at the bottom of `Program.cs` — standard Microsoft-documented fix. |
| `create-next-app` defaults drift between versions (e.g., Tailwind on by default) | Pass explicit flags (`--ts --eslint --app --no-tailwind --use-npm`) so the scaffold is deterministic. |
| Root `.gitignore` doesn't cover everything Next.js / .NET emit | Use community-standard templates (VisualStudio + Node) and confirm `git status` is clean after a full build. |
| Solution-file path drift causing `dotnet build` to miss a project | Always run `dotnet sln add` for each new project; verify final list with `dotnet sln list`. |
| `npx create-next-app` requires network — may fail offline / in sandbox | Run in an environment with internet; this is a one-time bootstrap. |
| Adding `Swashbuckle`/Swagger by default could be seen as a "feature" | Spec doesn't forbid Swagger, but keeping the Api lean (no Swagger) avoids scope creep. If the template adds it, strip it. |

---

## 7. Out of Scope (explicit)

Per the spec, **do not** add any of the following in this issue:
- Building / Screen / Advertiser / Campaign / Creative entities or endpoints
- PostgreSQL, Neon, Entity Framework Core, migrations, repositories
- Authentication, JWT, identity, `[Authorize]`
- Deployment configuration (Render, Vercel, Dockerfiles, CI/CD)
- Tailwind, UI component libraries, dashboards, API clients on the frontend
- SSP / DSP logic, auction engine, OpenRTB adapter
- Proof-of-play, reporting, analytics
- Any HTTP call from the frontend to the backend

If any of the above feels tempting while implementing, stop — it belongs to a later roadmap phase.

---

**End of plan.** Plan is read-only output as requested; no files were created or modified. Hand this to an implementer or to the `superpowers:executing-plans` skill when you're ready to ship.
