# Commerce Cart

A focused proof-of-concept e-commerce **Cart** vertical slice: a .NET 10 Minimal API backend backed by PostgreSQL, and a React + TypeScript frontend, demonstrating one complete, end-to-end shopping-cart flow with tests, validation, structured error handling, and health checks.

## Proof-of-concept scope

This repository implements exactly one vertical slice — Products and Cart — end to end:

- Product listing (active products only).
- Cart creation and persistence, restored across page reloads via a stored Cart ID.
- Add an item to the Cart.
- Update a Cart item's quantity.
- Remove a Cart item.
- PostgreSQL persistence via EF Core, with an automatically-applied migration and idempotent seed data on startup.
- A React frontend for the full Product/Cart flow, including loading, empty, error and retry states.
- Request validation, RFC 7807 `ProblemDetails` error responses (with a `traceId`), health check endpoints, and automated tests on both backend and frontend.

### Implemented PoC vs. target architecture

This repository intentionally implements a **small, complete slice**, not the full target system. A separate pair of architecture documents (outside this repository) describe a much larger target production architecture — a multi-channel platform with authentication, checkout, payments, messaging, search, and distributed deployment. **Those documents describe where the system could grow, not what is built here.** Anything not listed under "Proof-of-concept scope" above, or explicitly listed under "What is out of scope" below, is architecture-document-only and is **not** present in this codebase.

## Repository structure

```
.
├── compose.yaml                    # Local PostgreSQL only (see "What Docker Compose does" below)
├── ECommercePlatform.sln
├── global.json                     # Pins the .NET SDK version
├── src/
│   ├── Commerce.Api/                # ASP.NET Core Minimal API backend
│   │   ├── Domain/                  # Product, Cart, CartItem — entities and invariants
│   │   ├── Features/                # Minimal API endpoint groups (Products, Carts)
│   │   ├── Infrastructure/          # EF Core DbContext, configurations, migrations, error handling
│   │   └── Program.cs
│   └── Commerce.Web/                 # React + TypeScript frontend (Vite)
│       └── src/
│           ├── api/                  # Fetch client + RTK Query API slice
│           ├── app/                  # Redux store
│           ├── features/             # Cart and Product UI features
│           └── ...
└── tests/
    ├── Commerce.UnitTests/           # Domain-model unit tests (xUnit)
    └── Commerce.IntegrationTests/    # Endpoint integration tests (xUnit + Testcontainers)
```

## Prerequisites

- **.NET 10 SDK** (this repository's `global.json` pins SDK `10.0.100` with `rollForward: latestFeature` — any installed `10.0.1xx` SDK will work).
- **Node.js** and **npm** (developed and verified against Node 24.x / npm 11.x; any current Node LTS satisfying Vite's requirements — Node 20.19+ or 22.12+ — will work).
- **Docker** with **Docker Compose** (used to run PostgreSQL locally, and required by the backend integration test suite — see below).

## Local setup

All commands below assume the repository root as the working directory unless otherwise noted. Both a Windows PowerShell version and a platform-neutral (bash / macOS / Linux) version are given for the steps involving environment variables.

### 1. Start PostgreSQL

`compose.yaml` requires a `COMMERCE_POSTGRES_PASSWORD` environment variable — it is not defaulted, and Compose will refuse to start without it. Choose any value for local development; it never leaves your machine and is not stored anywhere in the repository.

**PowerShell**

```powershell
$env:COMMERCE_POSTGRES_PASSWORD = "local-dev-only"
docker compose up -d postgres
```

**bash / macOS / Linux**

```bash
export COMMERCE_POSTGRES_PASSWORD=local-dev-only
docker compose up -d postgres
```

Wait for the container to report healthy (`docker ps` shows `healthy` for the `postgres` service) before continuing. PostgreSQL will be reachable on `localhost:5433` (mapped from the container's `5432`).

### 2. Configure and start the API

The API reads its database connection string from the `ConnectionStrings__CommerceDatabase` environment variable and **fails fast at startup** if it is missing — there is no default and no connection string is committed to any `appsettings*.json` file.

**PowerShell**

```powershell
$env:ConnectionStrings__CommerceDatabase = "Host=127.0.0.1;Port=5433;Database=commerce;Username=commerce;Password=local-dev-only"
dotnet restore ECommercePlatform.sln
dotnet run --project src/Commerce.Api --launch-profile http
```

**bash / macOS / Linux**

```bash
export ConnectionStrings__CommerceDatabase="Host=127.0.0.1;Port=5433;Database=commerce;Username=commerce;Password=local-dev-only"
dotnet restore ECommercePlatform.sln
dotnet run --project src/Commerce.Api --launch-profile http
```

Use the same password you set for `COMMERCE_POSTGRES_PASSWORD` in step 1. On startup the API automatically applies the EF Core migration and seeds demo products — no separate `dotnet ef database update` step is needed.

Leave this running in its own terminal.

### 3. Install and start the React frontend

In a second terminal:

```powershell
cd src/Commerce.Web
npm install
npm run dev
```

The dev server proxies `/api` and `/health` to the backend on `http://127.0.0.1:5003`, so no CORS configuration is required.

### 4. Open the app

Visit **http://127.0.0.1:5173**. Adding, updating, and removing Cart items should work end to end against the real backend and database.

## Expected local URLs

| Service | URL |
|---|---|
| Frontend (Vite dev server) | http://127.0.0.1:5173 |
| Backend API | http://localhost:5003 |
| OpenAPI document | http://localhost:5003/openapi/v1.json *(Development environment only — see note below)* |
| Liveness health check | http://localhost:5003/health/live |
| Readiness health check | http://localhost:5003/health/ready |
| PostgreSQL (host) | localhost:5433 |

The `http` launch profile used above sets `ASPNETCORE_ENVIRONMENT=Development`, which is what exposes `/openapi/v1.json`; the endpoint is not mapped in other environments.

## Backend verification

From the repository root:

```powershell
dotnet restore ECommercePlatform.sln
dotnet build ECommercePlatform.sln --configuration Release
dotnet test ECommercePlatform.sln --configuration Release
```

Expected: a clean build with 0 warnings and 0 errors, and all backend tests passing (49 unit tests + 24 integration tests).

**The integration test project uses [Testcontainers](https://testcontainers.com/) to start a real, disposable PostgreSQL container for each test run — Docker must be running for `dotnet test` to succeed.** No manually-started database is required for the tests themselves; Testcontainers manages the container's full lifecycle automatically.

## Frontend verification

From `src/Commerce.Web`:

```powershell
npm ci
npm run lint
npm test -- --run
npm run build
```

Expected: a clean install, 0 lint errors/warnings, all frontend tests passing (26 tests), and a successful production build under `dist/`.

The frontend uses **Redux Toolkit and RTK Query** (`@reduxjs/toolkit`, `react-redux`) as the single owner of server/remote state — product and cart data are fetched, cached, and mutated through one RTK Query API slice (`src/api/commerceApiSlice.ts`); there is no separate client-side business-state store.

## What Docker Compose does (and does not do)

`compose.yaml` currently defines and starts **only the PostgreSQL database service**. It does not build or run the API or the frontend, and there is no single command that starts the entire application — the API and the frontend are each started directly (steps 2 and 3 above).

## What is out of scope

The following are deliberately **not** implemented in this proof of concept, regardless of how they may be described in the separate architecture documents:

- Authentication or authorization of any kind.
- Checkout, payment processing, or order management.
- Inventory reservation.
- A message broker or any asynchronous/event-driven processing (e.g. RabbitMQ, outbox/inbox).
- Distributed deployment, multi-region infrastructure, or Kubernetes.
- Search infrastructure (e.g. Elasticsearch), caching infrastructure (e.g. Redis), or a secrets manager.
- Optimistic concurrency control — concurrent writes to the same Cart are resolved last-write-wins; no version/conflict check exists.
- CI/CD — there is currently no automated pipeline in this repository; the commands above must be run locally.

## Troubleshooting

**Docker is not running.**
`docker compose up -d postgres` will fail to connect to the Docker daemon. Start Docker Desktop (or your Docker engine) and retry. `dotnet test` on the integration project will fail the same way, since Testcontainers also needs a running daemon.

**Port 5433 is already in use.**
Something else on your machine is bound to 5433 (often a previous, still-running `postgres` container, or a locally-installed PostgreSQL instance). Find and stop whatever is listening on that port, or stop the conflicting container with `docker ps` / `docker stop <container>`, then retry `docker compose up -d postgres`.

**"Connection string 'CommerceDatabase' is required" on API startup.**
`ConnectionStrings__CommerceDatabase` was not set in the shell the API was launched from — this is a fail-fast check, by design. Re-run the `$env:...` / `export ...` command from step 2 in the **same terminal** you then run `dotnet run` from (environment variables set in one terminal are not visible to another).

**Stale PostgreSQL data / seed data looks wrong after changing something.**
The Compose volume (`commerce-postgres-data`) persists data across restarts. To start from a completely clean database:
```powershell
docker compose down -v
docker compose up -d postgres
```
`-v` removes the named volume along with the container.

**`npm install` / `npm ci` fails or behaves unexpectedly.**
Confirm your Node version satisfies Vite's requirement (Node 20.19+ or 22.12+ — `node --version`). Delete `node_modules` and `package-lock.json`-derived caches if a previous partial install left the tree inconsistent, then re-run `npm install`. Use `npm ci` (not `npm install`) when you want an exact, reproducible install matching the committed `package-lock.json`.
