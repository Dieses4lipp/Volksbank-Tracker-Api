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

`BankUrl`, `BlZ`, `Iban` are required — endpoints under `/api/sync` return `503` until all three are set.

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

All endpoints require `X-Api-Key` header (if `Api:Key` is configured).

### Sync — `/api/sync`
- `POST /` — sync transactions from the bank (`{ "fromDate": "..." }` optional body)
- `GET /balance` — current account balance
- `GET /logs` — last 20 sync log entries

### Transactions — `/api/transactions`
- `GET /` — paged list; query: `page`, `pageSize` (max 200), `categoryId`, `search`, `type` (`income`/`expense`)
- `PATCH /{id}/category` — assign a category

### Categories — `/api/categories`
- `GET /` — list categories
- `PUT /{id}` — update name/keywords/color/icon
- `POST /recategorize` — re-run auto-categorization on all transactions

### Stats — `/api/stats`
- `GET /dashboard` — summary stats
- `GET /monthly?months=24` — monthly breakdown (max 120)
- `GET /anomalies?months=12&threshold=2.5` — anomalous transactions

### Settings — `/api/settings`
- `GET /classification` — current classification settings
- `PUT /classification` — update classification settings

Errors follow RFC 7807 `ProblemDetails` (`502` bank communication failure, `503` FinTS not configured, `401` bad/missing API key).
