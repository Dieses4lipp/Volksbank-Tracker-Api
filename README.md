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

Set via `appsettings.json`, environment variables, or (recommended for secrets) `dotnet user-secrets`.

### `FinTs` section

| Key | Description |
|---|---|
| `BankUrl` | FinTS endpoint URL of your bank |
| `BlZ` | Bank sort code (Bankleitzahl) |
| `Iban` | Account IBAN |
| `Bic` | Account BIC |
| `Account` | Account number |
| `UserId` | Online banking user ID |
| `Pin` | Online banking PIN |

`BankUrl`, `BlZ`, `Iban`, `UserId`, `Pin` are required — endpoints under `/api/sync` return `503` until all are set.

Instead of configuring this section, the credentials can be submitted via `PUT /api/settings/fints`. They are verified against the bank and stored encrypted in the database; stored credentials take precedence over the `FinTs` section, which remains the fallback.

### `DataProtection:KeysPath`

Directory for the ASP.NET Core Data Protection key ring that encrypts stored FinTS credentials. Optional — defaults to the framework location (Windows: `%LOCALAPPDATA%\ASP.NET\DataProtection-Keys`, DPAPI-protected). Keep it outside the database folder, so a copy of the database alone does not expose the credentials. In Docker, mount it as a volume: if the key ring is lost, stored credentials become unreadable (`503`) and must be submitted again.

### `Api:Key`

Static API key required in the `X-Api-Key` header on every request. Must be set outside Development (startup throws otherwise). Requests without a matching key get `401`.

### `ConnectionStrings:Default`

SQLite connection string. Defaults to `Data Source=tracker.db`.

Example `dotnet user-secrets`:

```
dotnet user-secrets set "FinTs:BankUrl" "https://hbci-pintan.gad.de/..."
dotnet user-secrets set "FinTs:BlZ" "..."
dotnet user-secrets set "FinTs:Iban" "..."
dotnet user-secrets set "FinTs:Bic" "..."
dotnet user-secrets set "FinTs:Account" "..."
dotnet user-secrets set "FinTs:UserId" "..."
dotnet user-secrets set "FinTs:Pin" "..."
dotnet user-secrets set "Api:Key" "..."
```

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
- `PUT /{id}` — update name/color/icon
- `POST /recategorize` — re-run auto-categorization (learned mappings, else fallback category) on all transactions

### Stats — `/api/stats`
- `GET /dashboard` — summary stats
- `GET /monthly?months=24` — monthly breakdown (max 120)
- `GET /anomalies?months=12&threshold=2.5` — anomalous transactions

### Settings — `/api/settings`
- `GET /classification` — current classification settings
- `PUT /classification` — update classification settings
- `GET /fints` — credential status (`source`: `None`/`Configuration`/`Database`/`Unreadable`); identifiers masked, PIN never returned
- `PUT /fints` — verify credentials against the bank and store them encrypted (`{ "bankUrl", "blZ", "iban", "userId", "pin", "bic", "account" }`); `422` if the bank rejects them. Rate-limited to 3 requests per 10 minutes — wrong PINs count toward the bank's lockout
- `DELETE /fints` — remove stored credentials (falls back to the `FinTs` section)

Errors follow RFC 7807 `ProblemDetails` (`502` bank communication failure, `503` FinTS not configured, `401` bad/missing API key).
