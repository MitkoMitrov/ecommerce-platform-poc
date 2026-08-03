# Reviewer Guide — Running and Verifying the E-Commerce PoC

Minimal commands to clone, run, and verify this repository. For prerequisite details, endpoint list, a manual smoke-test checklist, troubleshooting, and scope notes, see [README.md](README.md).

## Prerequisites

Git, .NET SDK `10.0.1xx`, Node.js (`^20.19.0` or `>=22.12.0`), Docker with Compose — running.

## 1. Clone

```powershell
git clone <repository-url>
cd <repository-folder>
```

## 2. Start PostgreSQL

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

## 3. Start the API

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

Migrations and seed data apply automatically. Leave this terminal running.

## 4. Start the frontend (second terminal)

```powershell
cd src/Commerce.Web
npm ci
npm run dev
```

## 5. Verify

- Frontend: http://127.0.0.1:5173
- API: `curl http://localhost:5003/health/live`, `/health/ready`, `/api/products`

## 6. Run automated tests

```powershell
dotnet restore ECommercePlatform.sln
dotnet build ECommercePlatform.sln --configuration Release --no-restore
dotnet test ECommercePlatform.sln --configuration Release --no-build
```

```powershell
cd src/Commerce.Web
npm ci
npm run lint
npm test -- --run
npm run build
```

## 7. Stop everything

Stop the frontend and API terminals with `Ctrl+C`, then:

```powershell
docker compose down
```

(`docker compose down -v` also deletes the PostgreSQL data volume, including any Purchase History created during review.)
