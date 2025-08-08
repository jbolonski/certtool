# Certificate Monitor

**Monitoring and Reporting application for SSL/TLS certificates**

**Always check the .env file for database connection information**

## Architecture Overview

**Core Pattern**: Blazor WebAssembly Single Page Application (ASP.NET Core hosted) + backend service layer (API endpoints) + bash scripts + cron jobs + openssl + MySQL.

Frontend must be implemented as a .NET Blazor WebAssembly (WASM) application (ASP.NET Core hosted) targeting the current LTS (.NET 8 unless superseded). No alternative SPA frameworks (React/Vue/Angular) should be introduced without an approved Architecture Decision Record (ADR).

## Database (MySQL Required)

Use **MySQL** as the only supported relational database (no SQLite/Postgres fallbacks).

Required version:
- Minimum: 8.0.28
- SQL mode must include: `STRICT_TRANS_TABLES,NO_ENGINE_SUBSTITUTION`
- Charset: `utf8mb4`
- Collation: `utf8mb4_unicode_ci`
- Storage engine: `InnoDB` only

Environment variables (all required unless marked optional):
```
DB_HOST=            # hostname or IP
DB_PORT=3306        # override if non‑default
DB_NAME=cert_monitor
DB_USER=            # least-privileged app user
DB_PASSWORD=        # never hardcode; store in .env (not committed)
DB_POOL_MIN=1       # optional (defaults internally)
DB_POOL_MAX=10      # optional (cap connections)
DB_SSL_MODE=VERIFY_CA   # one of: DISABLED|PREFERRED|REQUIRED|VERIFY_CA|VERIFY_IDENTITY
DB_SSL_CA_PATH=         # path to CA bundle if VERIFY_* modes used
```

Connection string (when a DSN form is needed):
```
mysql://DB_USER:DB_PASSWORD@DB_HOST:DB_PORT/DB_NAME?ssl-mode=REQUIRED
```

Table / schema conventions:
- snake_case table and column names
- Primary keys: `id` BIGINT UNSIGNED AUTO_INCREMENT
- Timestamps: `created_at`, `updated_at` (UTC, `TIMESTAMP` with DEFAULT CURRENT_TIMESTAMP and ON UPDATE)
- Do not use `ON DELETE CASCADE` unless explicitly justified
- Index names: `idx_<table>_<col>[_<col2>]`
- Unique constraints: `uq_<table>_<col>`

Migrations:
- All schema changes must be expressed as idempotent migration scripts in `db/migrations/` named `YYYYMMDDHHMM__description.sql`
- Never edit a committed migration; add a new one for changes
- Each migration must be reversible (include a `-- down` section or paired rollback file if using a tool that supports it)

Data access:
- Prefer parameterized queries (no string concatenation)
- Validate and bound input lengths before persistence
- Wrap multi-statement modifications in a transaction; rollback on any error

Connection handling:
- Use a pooled client; never open ad‑hoc one-off connections per query
- Retry transient errors (deadlocks, lock wait timeout) up to 3 times with exponential backoff (base 100ms, cap 2s)
- Treat the following as non-retriable: syntax errors, permission errors, unknown table/column

Security:
- Application DB user must not have `ALTER USER`, `SUPER`, `FILE`, or `SHUTDOWN`
- Enforce least privilege: CRUD only on owned schema
- Use TLS when DB is off‑host (verify CA)

Backups / recovery (operational note):
- Logical dumps (mysqldump or equivalent) daily; retain 7 days
- Test restore procedure before altering critical schema elements

Do NOT:
- Embed credentials in scripts or HTML reports
- Assume implicit UTC conversion—always store and compare in UTC
- Use `TEXT` when a bounded `VARCHAR` suffices
- Disable `ONLY_FULL_GROUP_BY`

If the database is unreachable:
1. Log a single critical error with context tag `DB_CONNECT_FAIL`
2. Skip certificate querying steps dependent on persistence (fail fast)
3. Emit a degraded run report marking persistence layer as `UNAVAILABLE`

## Web Frontend (Blazor WebAssembly)

Baseline:
- Use .NET SDK LTS (8.x currently). File a task to review on new LTS release; do not jump to non-LTS without ADR.
- Project layout: `src/Client` (Blazor WASM), `src/Server` (ASP.NET Core host + API), `src/Shared` (DTOs / validation). Place database access only in `Server` project (no direct DB in WASM).
- Use the ASP.NET Core hosted template as starting point (enables shared models and API).
- All network/data operations from WASM go through typed `HttpClient` calling `/api/*` endpoints.

Build & Deploy:
- Release build command: `dotnet publish src/Server -c Release -o build/publish` (serves WASM assets via Server project).
- Static assets for the Blazor client output under `wwwroot`. Do not place secrets or environment-only values inside WASM; expose runtime config via a `/api/config` endpoint if needed.
- Enable response compression (Brotli + gzip) for `.wasm`, `.dll`, `.json`, `.js`, `.css`.

Performance & Size:
- Trim assemblies (`<PublishTrimmed>true</PublishTrimmed>`) but validate no required reflection paths are removed (add `rd.xml` or `DynamicDependency` attributes if needed).
- Use lazy loading for large, infrequently visited feature modules.
- Avoid JS interop unless necessary (prefer pure Blazor components); consolidate interop calls to thin wrappers.

State Management:
- Prefer cascading parameters for small shared state; for broader app state use a scoped DI state container service (no global static singletons).
- State must be serializable if future prerendering is introduced.

Components & Naming:
- Component files in `Components/` or feature folders; suffix components with `*Component.razor` only when clarity needed—otherwise just `CertificateList.razor`.
- Keep component code-behind in partial class (`.razor.cs`) when logic > ~40 lines.

Styling:
- Use scoped CSS (`Component.razor.css`). Avoid global selectors leakage; prefer CSS variables for theme tokens.
- Do not embed base64 images > 2KB directly in CSS.

Accessibility:
- All interactive elements must have discernible text / `aria-label`.
- Table listings must include `<caption>` summarizing certificate set.
- Color alone cannot convey certificate status (include icon + text).

Error Handling & UX:
- Provide a top-level `ErrorBoundary` wrapping `Router` with a user-friendly fallback and a telemetry log.
- Show certificate expiration severity badges: OK (green), WARNING (amber), CRITICAL (red) matching backend thresholds.
- Network failures: use exponential backoff (200ms, 400ms, 800ms, 1600ms; max 4 tries) then surface toast + retry button.

Security:
- No secrets in WASM (cannot be protected). All sensitive operations server-side.
- Enforce HTTPS redirection on Server host; use HSTS in production.
- Validate all input again on server (client validation is convenience only).

Internationalization (future-ready):
- Wrap user-visible strings with a resource lookup (resx) in Shared if localization is planned; otherwise keep them centralized for future extraction.

Testing:
- Unit test component logic (render fragments) with bUnit; target key components (list, detail, status badge).
- Snapshot tests must be stable—avoid volatile timestamps (inject clock abstraction).

Telemetry:
- Emit custom events for: page navigation, certificate fetch duration, number of expiring certificates.
- Correlate client requests with server logs via a `Correlation-Id` header (GUID generated client-side if absent).

Build Quality Gates:
- Fail build if published WASM size (compressed) exceeds baseline + 15%. Maintain a `SIZE_BASELINE.md` with current figures.
- Lint (analyzers) warnings elevated to errors for security & reliability IDs (CAxxxx, SYSLIB, ASP0000 set as errors).

Do NOT:
- Add alternative client frameworks or bundlers.
- Access `window` directly except through a vetted JS interop service.
- Store large (>1MB) response payloads in localStorage/sessionStorage.

Future Options (requires ADR before adoption):
- Pre-rendering on server.
- Offline/PWA mode (service workers) for reporting cache.
- WebAssembly AOT compilation (evaluate size vs perf trade-offs first).
