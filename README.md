# Certificate Monitor

Blazor WebAssembly (ASP.NET Core hosted) app for monitoring SSL/TLS certificate expirations and host reachability across a managed list of hosts.

## Requirements

See the full functional requirements: [certtool-prd.md](./certtool-prd.md).

## Tech Stack

- .NET 8 LTS (Blazor WebAssembly Hosted)
- ASP.NET Core controllers
- Entity Framework Core (Pomelo MySQL provider)
- MySQL 8+ (STRICT mode; SQL migrations in `db/migrations`)

## Data Model (summary)

Tables: `hosts`, `certificates` (one current cert row per host). Schema via SQL scripts in `db/migrations`.

`hosts`

- id (PK)
- host_name (unique)
- is_reachable (TINYINT 0/1)
- last_checked_utc (nullable)
- last_reachable_utc (nullable)
- created_at / updated_at

`certificates`

- id (PK)
- host_id (FK → hosts.id)
- serial_number
- expiration_utc
- retrieved_at_utc
- created_at / updated_at

Policy: keep only the latest successful certificate per host (replace previous on success).

## Scanning

Background: `CertificateScanService` runs at startup and every 24 hours. It performs TLS handshakes on port 443, updates reachability, and replaces the certificate row on success.

On host add: `POST /api/hosts` best-effort immediate fetch; failure does not block creation.

Manual refresh: Client button calls `POST /api/certificates/refresh` to re-scan all hosts.

Bulk import: `POST /api/hosts/import` accepts text/plain with one host per line. Blank lines and lines starting with `#` are ignored. Duplicates and already-existing hosts are skipped. Newly-added hosts are scanned immediately.

## Certificate CSV Export

The Certificates page provides an Export CSV link. The server endpoint is `GET /api/certificates/export` and returns a CSV with header:

- Host
- Serial
- ExpirationUtc (ISO 8601)
- DaysUntilExpiration
- RetrievedAtUtc (ISO 8601)

## REST Endpoints (public surface)

- GET `/api/hosts` → list hosts (with reachability fields)
- POST `/api/hosts` → add host (immediate fetch & reachability update)
- DELETE `/api/hosts/{id}` → remove host (deletes associated cert rows first)
- POST `/api/hosts/import` (text/plain) → bulk import hosts; returns summary (added, skipped, errors)
- GET `/api/certificates` → list latest certificate per host (with computed days until expiration)
- POST `/api/certificates/refresh` → manual full scan
- GET `/api/certificates/export` → download CSV report
- GET `/api/stats` → dashboard summary statistics

## Local Development Quick Start

1. Install .NET 8 SDK and Docker.
1. Create `.env` with DB variables (see `.github/copilot-instructions.md`).
1. Start services:

```powershell
docker compose up --build
```

1. Apply migrations by running SQL files in `db/migrations`.
1. Browse: [http://localhost:5000](http://localhost:5000).

## Environment & Configuration

Environment variables (DB_*) configure MySQL. Use TLS for remote DBs. See `.github/copilot-instructions.md` for modes and constraints.

## Styling / Theming

CSS variables + dark mode class on the root element. Preference is stored in `certtool-darkmode`.

## Security Notes

- Certificate trust chain not yet validated (future: chain/issuer validation and alert thresholds).
- No secrets in WASM; DB credentials are server-side via environment variables.

## Testing (Planned)

- Component tests (bUnit) for rendering and severity classification
- Service tests for `CertificateFetcher` (mock TCP)
- Snapshot tests with deterministic clock

## Roadmap / Future Enhancements

- Pagination, filtering, and search
- Webhook / email notifications for impending expiration
- Alert threshold configuration (custom day windows)

## License

Internal / TBD (add license before distribution).

## Contributing

Open an issue or PR with change rationale. Migration policy: never modify committed migrations—add a new one (exception only before first production deployment).
