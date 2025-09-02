# Application Requirements

## Requirements

The functional requirements are grouped by domain area for clarity.

### A. Core Monitoring & Data Collection

| ID | Description | User Story | Expected Behavior / Outcome |
|----|-------------|------------|-----------------------------|
|FR001| Report of certificate expirations | As a user, I want to be able to track certificate expirations | Provide a list of certificates and their expiration dates |
|FR003| Check the certificates daily | As a user, I want the certificates to be checked automatically on a daily basis | A scheduled background job iterates all hosts and refreshes certificate + reachability data |
|FR007| Manual Refresh | As a user, I want to manually check certs | User can trigger a refresh; updates cert data AND host reachability / timestamps |
|FR008| Keep only latest certificate check | As a user, I only want to see the most recent certificate scan information | Overwrite prior certificate record per host with newest result |
|FR016| Auto fetch cert on host add | As a user, I want immediate data for a newly added host | On host creation attempt immediate fetch (port 443), storing cert if successful |
|FR019| Scans update reachability fields | As a user, I want host status current after any scan | All scan types set LastCheckedUtc, update IsReachable, set LastReachableUtc on success |

### B. Host Management

| ID | Description | User Story | Expected Behavior / Outcome |
|----|-------------|------------|-----------------------------|
|FR002| Manage list of hosts to monitor | As a user, I want to manage the list of hosts | Provide page to add and delete hosts |
|FR015| Alphabetical host sorting | As a user, I want hosts listed alphabetically | Host list sorted by name (case-insensitive) |
|FR017| Host reachability status | As a user, I want to know if a host was reachable | Maintain IsReachable, LastCheckedUtc, LastReachableUtc; show status pill |
|FR018| Filter unreachable hosts | As a user, I want to focus on unreachable hosts | Toggle filters host list to only unreachable ones |
|FR022| Show next scheduled scan | As a user, I want to know when the next automatic scan will run | Hosts page displays next scheduled scan UTC timestamp (and last run) sourced from /api/schedule |
|FR023| Bulk import hosts from file | As a user, I want to import hosts from a text file (one host per line) | Hosts page supports uploading a .txt file with one host per line; trims whitespace; skips blank lines and duplicates; validates hostnames; imports new hosts; shows an import summary (added/skipped/errors) |

### C. Certificate Reporting & Presentation

| ID | Description | User Story | Expected Behavior / Outcome |
|----|-------------|------------|-----------------------------|
|FR004| Certificate details should be shown | As a user, I want to see key certificate details | Display host, serial number, expiration date |
|FR005| Reporting page should be tabular | As a user, I want tabular scan results | Present results in a table with clear headings |
|FR006| Reporting table sorted by expiration | As a user, I want results sorted by days until expiration | Default sort ascending by days remaining |
|FR009| Color code the results table | As a user, I want quick criticality cues | Apply color highlighting by days to expiration (≤30 red, 31–60 yellow) |
|FR010| Show the dates in a nicer format | As a user, I want readable dates | Format dates MM/dd/yyyy (and times where applicable) |
|FR014| Clickable host links | As a user, I want to open the host site quickly | Host names link to the host over HTTPS in a new tab |

### D. User Interface / UX Enhancements

| ID | Description | User Story | Expected Behavior / Outcome |
|----|-------------|------------|-----------------------------|
|FR011| Dark mode support | As a user, I want a dark mode | Provide toggle / detection switching theme |
|FR012| Remember dark/light mode | As a user, I want my preference remembered | Persist selection (e.g., localStorage) and restore on load |
|FR013| Dense tables | As a user, I want information-dense tables | Reduced padding for higher data density |
|FR021| Sidebar dashboard navigation | As a user, I want quick return to dashboard | Sidebar includes Dashboard link highlighting at root |

### E. Dashboard & Analytics

| ID | Description | User Story | Expected Behavior / Outcome |
|----|-------------|------------|-----------------------------|
|FR020| Dashboard overview statistics | As a user, I want a summary dashboard | Root page shows: total hosts, certs tracked, expiring ≤30, expiring ≤60, unreachable hosts, last scan UTC, days since last scan (/api/stats) |

### F. Status & Reachability (Cross-cutting)

| ID | Description | User Story | Expected Behavior / Outcome |
|----|-------------|------------|-----------------------------|
|FR017| Host reachability status | As a user, I want to know if a host was reachable | (Duplicate reference) status fields maintained and displayed |
|FR019| Scans update reachability fields | As a user, host status should reflect latest probe | (Duplicate reference) all scan entry points keep status fresh |
|FR022| Show next scheduled scan | As a user, I want visibility of upcoming work | (Duplicate reference) expose last/next run via schedule service & /api/schedule, surface on Hosts page |

> Note: FR017 and FR019 appear in both their primary sections and the cross‑cutting section for traceability.
