# Certificate Monitor

Modern Blazor WebAssembly (ASP.NET Core hosted) application for monitoring SSL/TLS certificate expirations across a managed inventory of hosts.

Implements functional requirements FR001–FR016 (see `certtool-prd.md`).

## Current Feature Set

### Core

* FR001 Report of certificate expirations (tabular view)
* FR002 Host management (add / delete)
* FR003 Daily automated scan (background hosted service)
* FR004 Certificate details (host, serial, expiration)
* FR005 Tabular reporting layout
* FR006 Sorting by days until expiration (client ordering)
* FR007 Manual refresh trigger (POST /api/certificates/refresh)
* FR008 Only latest certificate per host retained
* FR009 Color-coded severity rows (<=30 days red, 31–60 yellow, else default)
* FR010 Friendly date formatting (MM/dd/yyyy)
* FR011 Dark mode toggle
* FR012 Dark/light preference persisted (localStorage)
* FR013 Dense table styling
* FR014 Host names clickable (open in new tab with https:// prefix)
* FR015 Alphabetical host listing
* FR016 Auto fetch certificate immediately when a host is added

### Architecture / UX

* 2025 design system: sidebar navigation, glass panels, responsive layout, CSS variables
* Accessible focus states & reduced motion friendly transitions
* Separation of concerns via `Shared` DTOs

## Tech Stack

* .NET 8 LTS (Blazor WebAssembly Hosted)
* ASP.NET Core minimal controllers
* Entity Framework Core (Pomelo MySQL provider)
* MySQL 8+ (STRICT mode; schema migrations in `db/migrations`)

## Data Model (summary)

Tables: `hosts`, `certificates` (one current cert row per host). Schema created via SQL migration scripts under `db/migrations`.

## Background Scanning

`CertificateScanService` (Hosted Service) executes once at startup and then every 24 hours (interval constant). It iterates hosts, fetches the leaf certificate via TLS handshake (`SslStream` on port 443), and upserts (replace strategy) the record.

## Immediate Fetch on Host Add

When a host is added (`POST /api/hosts`), the API attempts an immediate fetch. Failure is non-fatal (host still created; later daily or manual refresh will populate certificates).

## Manual Refresh

Client button invokes `POST /api/certificates/refresh` to re-scan all hosts on demand.

## REST Endpoints (public surface)

* GET `/api/hosts` → list hosts
* POST `/api/hosts` → add host (triggers immediate fetch)
* DELETE `/api/hosts/{id}` → remove host
* GET `/api/certificates` → list latest cert per host
* POST `/api/certificates/refresh` → manual full scan

## Local Development Quick Start

1. Install .NET 8 SDK and Docker.
2. Create `.env` with required DB vars (see `copilot-instructions.md` / forthcoming sample).
3. Start services:

```bash
docker compose up --build
```

4. Apply migrations (if not auto-applied) by running the SQL files in `db/migrations` against the MySQL container.
5. Browse: <http://localhost:5000> (adjust port mapping if changed).

## Environment & Configuration

Environment variables (DB_*) configure MySQL connectivity (TLS recommended for remote DB). See architecture instructions in `.github/copilot-instructions.md` for required modes and constraints.

## Styling / Theming

Design uses CSS custom properties and a dark-mode class applied to `<html>`. Extend palette via `app.css` variables. Light/dark preference stored in `certtool-darkmode` localStorage key.

## Security Notes

* Certificate validation disabled during fetch (trust-agnostic). Future hardening: validate chain & expiry window.
* No secrets stored client-side. DB credentials loaded server-side via environment variables.

## Testing (Planned)

* Component tests (bUnit) for table rendering & severity classification
* Service tests for `CertificateFetcher` (mock TCP)

## Roadmap / Future Enhancements

* Edit host names (PUT)
* Pagination / filtering / search
* Export (CSV / JSON)
* Telemetry & correlation IDs (client → server)
* Badge components & status legend
* Retry logic & transient error classification
* Chain / issuer inspection & display

## License

Internal / TBD (add appropriate license file before distribution).

## Contributing
Open an issue or PR describing change rationale. Observe migration policy: never modify committed migrations—add a new one.
