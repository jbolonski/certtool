# Certificate Monitor

Blazor WebAssembly (ASP.NET Core hosted) application that tracks SSL/TLS certificate expirations for a managed list of hosts. Implements requirements FR001–FR006.

## Features
- Manage hosts to monitor (add/remove)
- Daily automatic certificate retrieval (serial number, expiration)
- Report table sorted by days until expiration (ascending)
- REST API (`/api/hosts`, `/api/certificates`)

## Tech Stack
- .NET 8 (Blazor WASM hosted)
- ASP.NET Core Web API
- EF Core + MySQL (Pomelo provider)

## Quick Start (Dev)
1. Ensure Docker & .NET 8 SDK installed.
2. Populate `.env` (see `.env.example`).
3. `docker compose up --build` (starts MySQL + app)
4. Navigate to http://localhost:5000 (adjust if mapped).

## Environment Variables (excerpt)
See `.env.example` for required DB settings.

## Migrations
SQL migrations in `db/migrations`. Apply manually or via EF in future enhancement.

## Background Scan
`CertificateScanService` runs on startup then every 24h (configurable). It uses `SslStream` to fetch the certificate for each host on port 443.

## Roadmap
- Edit/update host names
- Pagination & filtering
- Severity badges & telemetry

## License
Internal use only (add proper license).
