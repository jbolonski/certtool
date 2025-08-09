# Certificate Monitor

Modern Blazor WebAssembly (ASP.NET Core hosted) application for monitoring SSL/TLS certificate expirations and host reachability across a managed inventory of hosts.

Implements functional requirements FR001–FR021 (see `certtool-prd.md`).

## Current Feature Set

### Core Monitoring & Data

* FR001 Certificate expiration reporting (tabular)
* FR002 Host management (add / delete)
* FR003 Daily automated scan (background hosted service on 24h interval + run at startup)
* FR004 Certificate details (host, serial, expiration)
* FR005 Tabular reporting layout
* FR006 Sorting by days until expiration (client ordering)
* FR007 Manual refresh trigger (POST /api/certificates/refresh) – updates cert data & reachability
* FR008 Only latest certificate per host retained (overwrite on success)
* FR009 Color-coded severity rows (≤30 days red, 31–60 yellow, else default)
* FR010 Friendly date formatting (MM/dd/yyyy); dashboard uses compact MM/dd/yy HH:mm
* FR016 Auto fetch certificate immediately when a host is added (best-effort)
* FR017 Host reachability status (IsReachable, LastCheckedUtc, LastReachableUtc)
* FR018 Unreachable host filtering (checkbox toggle)
* FR019 All scan paths (daily, manual, host add) update reachability fields; only overwrite cert on success
* FR020 Dashboard statistics (root) summarizing environment
* FR021 Sidebar dashboard navigation (🏠 link)

### User Interface / UX

* Dark mode toggle (FR011) with preference persistence (FR012)
* Dense table styling (FR013)
* Clickable host links (FR014) open HTTPS in new tab
* Alphabetical host listing (FR015)
* 2025 design system: sidebar navigation, glass panels, responsive layout, CSS variables
* Accessible focus states & reduced motion friendly transitions
* Dashboard stat cards: total hosts, certificates tracked, expiring ≤30, expiring ≤60, unreachable hosts, last scan timestamp, days since last scan

### Status & Reachability

* Per-host boolean reachability (green / red pill)
* LastCheckedUtc set on every attempt (success or failure)
* LastReachableUtc updated only on successful fetch
* Unreachable hosts retain prior cert record (no destructive overwrite on failure)

## Tech Stack

* .NET 8 LTS (Blazor WebAssembly Hosted)
* ASP.NET Core controllers
* Entity Framework Core (Pomelo MySQL provider)
* MySQL 8+ (STRICT mode; schema migrations in `db/migrations`)

## Data Model (summary)

Tables: `hosts`, `certificates` (one current cert row per host). Schema created via SQL migration scripts under `db/migrations`.

`hosts`

* id (PK)
* host_name (unique)
* is_reachable (TINYINT 0/1)
* last_checked_utc (nullable)
* last_reachable_utc (nullable)
* created_at / updated_at

`certificates`

* id (PK)
* host_id (FK → hosts.id)
* serial_number
* expiration_utc
* retrieved_at_utc (timestamp certificate was fetched)
* created_at / updated_at

Policy: Only one certificate row per host retained (latest successful fetch). Previous row deleted before insert on success.

## Background Scanning

`CertificateScanService` runs once at startup and then every 24 hours. It iterates hosts, performs a TLS handshake (`SslStream` port 443), updates reachability, and overwrites the certificate row only on success.

## Immediate Fetch on Host Add

`POST /api/hosts` attempts immediate fetch (best-effort). Failure does not block host creation.

## Manual Refresh

Client button invokes `POST /api/certificates/refresh` to re-scan all hosts. For each: set LastCheckedUtc; on success set IsReachable & LastReachableUtc and overwrite cert; on failure set IsReachable=false and keep prior cert.

## Dashboard

Root page (/) displays statistics sourced from `/api/stats`: total hosts, certificates tracked, expiring ≤30 days, expiring ≤60 days, unreachable host count, last scan UTC timestamp (compact format), days since last scan.

## REST Endpoints (public surface)

* GET `/api/hosts` → list hosts (with reachability fields)
* POST `/api/hosts` → add host (immediate fetch & reachability update)
* DELETE `/api/hosts/{id}` → remove host (first deletes associated certificate row(s))
* GET `/api/certificates` → list latest certificate per host (includes days until expiration)
* POST `/api/certificates/refresh` → manual full scan (updates certs + reachability)
* GET `/api/stats` → dashboard summary statistics

## Local Development Quick Start

1. Install .NET 8 SDK and Docker.
1. Create `.env` with required DB vars (see `.github/copilot-instructions.md`).
1. Start services:

```bash
docker compose up --build
```

1. Apply migrations by running SQL files in `db/migrations` (initial script now includes reachability columns).
1. Browse: <http://localhost:5000> (adjust port mapping if changed).

## Environment & Configuration

Environment variables (DB_*) configure MySQL (TLS recommended for remote DB). See `.github/copilot-instructions.md` for required modes and constraints.

## Styling / Theming

CSS custom properties + dark-mode class on `<html>`. Preference stored in `certtool-darkmode` localStorage key.

## Security Notes

* Certificate trust chain currently not validated (future enhancement: chain/issuer validation + alert thresholds)
* No secrets in WASM; DB credentials server-side via environment variables

## Testing (Planned)

* Component tests (bUnit) for table rendering & severity classification
* Service tests for `CertificateFetcher` (mock TCP)
* Potential snapshot tests with deterministic clock

## Roadmap / Future Enhancements

* Edit host names (PUT)
* Pagination / filtering / search
* Export (CSV / JSON)
* Telemetry (correlation IDs, scan timing metrics)
* Status legend & accessibility semantics for color indicators
* Retry/backoff policy surfaced in UI (currently implicit)
* Certificate chain / issuer inspection & display
* Per-host on-demand recheck button
* Webhook / email notifications for impending expiration
* Alert threshold configuration (custom day windows)

## License

Internal / TBD (add license before distribution).

## Contributing

Open an issue or PR describing change rationale. Observe migration policy: never modify committed migrations—add a new one (exception: early consolidation before first production deployment, already applied here for reachability columns).
