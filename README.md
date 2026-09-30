# Volksbank-Tracker-Api

A REST API wrapper for accessing Volksbank online banking data via the FinTS protocol using standard Onlinebanking login credentials. Syncs transactions locally (SQLite), auto-categorizes them, and exposes stats/anomaly endpoints.

## Stack

- .NET 10, ASP.NET Core Web API
- EF Core + SQLite (`AppDbContext`, code-first migrations)
- Swagger/OpenAPI (Development only)

## Project layout

- `src/VolksbankTracker.API` — controllers, DTOs, middleware, `Program.cs`
- `src/VolksbankTracker.Core` — EF Core data layer, migrations, services (FinTS sync, categorization, stats, anomaly detection)

## Configuration

Only the host-level settings below live in configuration (`appsettings.json`, environment variables, or `dotnet user-secrets`). Everything else — FinTS credentials and the classification lists — is set exclusively through the API and stored in the database; there is no configuration fallback for them.

### `Api:Key`

Static API key required in the `X-Api-Key` header on every request. Must be set outside Development (startup throws otherwise). Requests without a matching key get `401`. This one cannot move into the database: it guards the endpoints that configure everything else.

### `DataProtection:KeysPath`

Directory for the ASP.NET Core Data Protection key ring that encrypts the stored FinTS credentials. Optional — defaults to the framework location (Windows: `%LOCALAPPDATA%\ASP.NET\DataProtection-Keys`, DPAPI-protected). Keep it outside the database folder, so a copy of the database alone does not expose the credentials. In Docker, mount it as a volume: if the key ring is lost, stored credentials become unreadable (`503`) and must be submitted again.

### `ConnectionStrings:Default`

SQLite connection string. Defaults to `Data Source=tracker.db`.

```
dotnet user-secrets set "Api:Key" "..."
```

## FinTS credentials

Submitted via `PUT /api/settings/fints` (`BankUrl`, `BlZ`, `Iban`, `UserId`, `Pin` required; `Bic`, `Account` optional). They are verified against the bank first and only stored — encrypted — if the bank accepts them. Endpoints under `/api/sync` return `503` until they are stored.

## Running

```
dotnet run --project src/VolksbankTracker.API
```

Migrations apply automatically on startup. In Development, Swagger UI is available at `/swagger`.

## API

All endpoints except `GET /health` require `X-Api-Key` header (if `Api:Key` is configured).

### Sync — `/api/sync`
- `POST /` — sync transactions from the bank (`{ "fromDate": "..." }` optional body)
- `GET /balance` — current account balance
- `GET /logs` — last 20 sync log entries

`POST /` and `GET /balance` share a rate limit of 1 request per 30 seconds.

### Transactions — `/api/transactions`
- `GET /` — paged list; query: `page`, `pageSize` (max 200), `categoryId`, `search`, `type` (`income`/`expense`), `sortBy` (`date`/`amount`/`category`), `sortDir` (`asc`/`desc`)
- `PATCH /{id}/category` — assign a category; the IBAN (or name) → category mapping is learned for future syncs

### Categories — `/api/categories`
- `GET /` — list categories
- `POST /` — create a category (`{ "name", "color", "icon" }`); `409` if the name exists (case-insensitive)
- `PUT /{id}` — update name/color/icon
- `POST /recategorize` — re-run auto-categorization (learned mappings, else fallback category) on all transactions

### Stats — `/api/stats`
- `GET /summary` — averages over the last 12 completed months, current month totals, top expense categories, last sync
- `GET /monthly?months=24` — monthly breakdown (max 120)
- `GET /anomalies?months=12&threshold=2.5` — anomalous transactions

### Settings — `/api/settings`
- `GET /classification` — current classification settings
- `PUT /classification` — update classification settings
- `GET /fints` — credential status (`source`: `None`/`Database`/`Unreadable`); identifiers masked, PIN never returned
- `PUT /fints` — verify credentials against the bank and store them encrypted (`{ "bankUrl", "blZ", "iban", "userId", "pin", "bic", "account" }`); `422` if the bank rejects them. Rate-limited to 3 requests per 10 minutes — wrong PINs count toward the bank's lockout
- `DELETE /fints` — remove stored credentials

Errors follow RFC 7807 `ProblemDetails` (`502` bank communication failure, `503` FinTS not configured, `401` bad/missing API key).
