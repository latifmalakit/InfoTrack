# InfoTrack Solicitor Intelligence

ASP.NET Core Web API and Vue 3 SPA for extracting conveyancing solicitor listings from Solicitors.com by location and turning the results into a compact sales report.

The application uses in-memory storage only. Search history and "new since previous run" comparisons are available while the API process is running and reset when it restarts. The API keeps the latest 25 runs to avoid unbounded memory growth.

## Features

- Searches Solicitors.com conveyancing results for adjustable locations.
- Uses `did=192` for Conveyancing and submits to `/prepare-search.asp`.
- Parses result pages without third-party scraping libraries.
- Extracts firm name, location, address, phone, website, contact form, profile URL, ratings, review counts, quality marks, and description.
- Reports totals, new listings, listings by location, missing contact details, top-rated firms, and duplicate firm names.
- Keeps recent runs in memory for comparison.
- Shows recent in-memory runs in the UI and opens saved reports by run id.
- Keeps solicitor listing construction inside the domain model, including rating range, contact URL, required identity fields, and quality mark normalization.
- Validates location input, returns RFC 7807 ProblemDetails for invalid requests, and applies a small per-IP rate limit.
- Adds basic security headers for API and SPA responses.
- Emits structured backend logs for search runs, per-location scraping, upstream failures, and in-memory pruning.

## Architecture

```text
backend/InfoTrack.Api                     Minimal API endpoints, DI, CORS, ProblemDetails
backend/InfoTrack.Application             Use cases, ports, report aggregation
backend/InfoTrack.Domain                  Domain records, construction invariants, stable listing keys
backend/InfoTrack.Infrastructure          Solicitors.com HTTP client, manual parser, in-memory repository
backend/tests/InfoTrack.UnitTests         Parser, domain, report, repository, key tests
backend/tests/InfoTrack.IntegrationTests  API and application integration tests
frontend                                  Vue 3 + Vite + TypeScript SPA
```

Dependencies flow inward:

```text
Api -> Application -> Domain
Api -> Infrastructure -> Application/Domain
```

The scraper is behind `ISolicitorSearchClient`, and history storage is behind `ISearchRunRepository`.
Search creation and search history reads are handled in the Application layer, so API endpoints stay thin and do not assemble reports directly.
The in-memory repository stores domain search runs only; history read models are assembled in the Application layer.

## Docker

Run the full app:

```bash
docker compose up --build
```

Then open:

- UI: `http://localhost:8080`
- API: `http://localhost:5080`
- Swagger: `http://localhost:5080/swagger`
- Health: `http://localhost:5080/health`

Docker services:

- `api`: ASP.NET Core API on port `8080` inside the container.
- `frontend`: Nginx serving the Vue build and proxying `/api` to the API container.

## Local Development

Prerequisites:

- .NET 10 SDK
- Node.js 20+

Run the API:

```bash
dotnet run --project backend/InfoTrack.Api --urls http://localhost:5080
```

Rider/IDE project launch uses the same local API port from `backend/InfoTrack.Api/Properties/launchSettings.json`.
If Docker Compose is already running, stop the `api` container before starting the API locally because both use host port `5080`.

Run the frontend:

```bash
cd frontend
npm install
npm run dev
```

The Vite dev server proxies `/api` to `http://localhost:5080`.

## Configuration

Runtime settings live in `backend/InfoTrack.Api/appsettings.json`.
When `ASPNETCORE_ENVIRONMENT=Development`, ASP.NET Core also loads `backend/InfoTrack.Api/appsettings.Development.json` and applies those overrides.

- `Cors:ClientOrigins`: allowed Vue client origins. Development adds the Vite origin `http://localhost:5173`.
- `RateLimiting`: fixed-window request limit policy.
- `SolicitorsClient`: Solicitors.com base address, user agent, and timeout.
- `SearchRunStorage:MaxStoredRuns`: number of in-memory search runs retained by the API process.

## API

Swagger UI is available at:

```text
http://localhost:5080/swagger
```

```http
GET /api/locations/defaults
```

```http
POST /api/search-runs
Content-Type: application/json

{
  "locations": ["London", "Birmingham"],
  "compareWithPreviousRun": true
}
```

```http
GET /api/search-runs/recent
GET /api/search-runs/{runId}
```

## Quality Checks

```bash
dotnet build backend/InfoTrack.sln
dotnet test backend/InfoTrack.sln

cd frontend
npm run typecheck
npm run build
npm audit
```

## Assumptions

- No external database is used, so no SQL script is required.
- Search history is in memory and resets after API restart.
- Solicitors.com exposes contact actions mostly as contact form URLs, not direct email addresses.
- "Solicitor name" is treated as the listed firm/display name.
- The source site returns 404 for non-browser-like clients, so the scraper sends a browser-style `User-Agent`.
