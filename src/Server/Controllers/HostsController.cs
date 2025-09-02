using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Entities;
using HostEntity = Server.Entities.Host;
using Shared;
using System.Text.RegularExpressions;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HostsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly Services.CertificateFetcher _fetcher;
    public HostsController(AppDbContext db, Services.CertificateFetcher fetcher)
    {
        _db = db;
        _fetcher = fetcher;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<HostDto>>> Get()
    {
        var hosts = await _db.Hosts.OrderBy(h => h.HostName).Select(h =>
            new HostDto(h.Id, h.HostName, h.IsReachable, h.LastCheckedUtc, h.LastReachableUtc)).ToListAsync();
        return Ok(hosts);
    }

    [HttpPost]
    public async Task<ActionResult> Post([FromBody] HostDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.HostName)) return BadRequest("HostName required");
        var hostName = dto.HostName.Trim();
        if (await _db.Hosts.AnyAsync(h => h.HostName == hostName, ct)) return Conflict("Host already exists");

        var host = new HostEntity { HostName = hostName };
    _db.Hosts.Add(host);
    await _db.SaveChangesAsync(ct);

        // Auto-fetch certificate upon host creation (best-effort, non-fatal on failure)
        try
        {
            var result = await _fetcher.FetchAsync(hostName, 443, ct);
            host.LastCheckedUtc = DateTime.UtcNow;
            if (result is { } tuple)
            {
                host.IsReachable = true;
                host.LastReachableUtc = host.LastCheckedUtc;
                var (serial, notAfterUtc) = tuple;
                var existing = await _db.Certificates.Where(c => c.HostId == host.Id).ToListAsync(ct);
                if (existing.Count > 0) _db.Certificates.RemoveRange(existing);
                _db.Certificates.Add(new CertificateRecord
                {
                    HostId = host.Id,
                    SerialNumber = serial,
                    ExpirationUtc = notAfterUtc,
                    RetrievedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                host.IsReachable = false;
            }
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Swallow network/SSL issues; host is still created.
            Console.Error.WriteLine($"CERT_FETCH_FAIL host={hostName} err={ex.Message}");
        }
        return CreatedAtAction(nameof(Get), new { id = host.Id },
            new HostDto(host.Id, host.HostName, host.IsReachable, host.LastCheckedUtc, host.LastReachableUtc));
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult> Delete(long id)
    {
        // Resolve FK constraint by explicitly removing certificates first (no ON DELETE CASCADE per policy)
        var host = await _db.Hosts.FirstOrDefaultAsync(h => h.Id == id);
        if (host == null) return NotFound();

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var certs = await _db.Certificates.Where(c => c.HostId == id).ToListAsync();
            if (certs.Count > 0)
            {
                _db.Certificates.RemoveRange(certs);
                await _db.SaveChangesAsync();
            }

            _db.Hosts.Remove(host);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            Console.Error.WriteLine($"HOST_DELETE_FAIL hostId={id} err={ex.Message}");
            return StatusCode(500, "Failed to delete host");
        }
    }

    // POST api/hosts/import
    // Accepts text/plain where each line contains a single host. Trims whitespace, ignores blanks and '#'-style comments.
    // Skips duplicates and already-existing hosts. Performs simple hostname validation.
    [HttpPost("import")]
    public async Task<ActionResult<BulkImportResultDto>> Import(CancellationToken ct)
    {
        string content;
        using (var reader = new StreamReader(Request.Body))
        {
            content = await reader.ReadToEndAsync();
        }
        if (content == null) return BadRequest("No content provided");

        var added = new List<string>();
        var skipped = new List<string>();
        var errors = new List<BulkImportErrorDto>();

        // Simple RFC 1123-ish hostname validation (letters, digits, hyphens, dots), max 253 chars
        var hostRegex = new Regex("^(?=.{1,253}$)([a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)(?:\\.([a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?))*$", RegexOptions.Compiled);

    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var addedEntities = new List<HostEntity>();
        var lines = content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var raw = lines[i];
            var lineNo = i + 1;
            var trimmed = raw?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmed)) { continue; }
            if (trimmed.StartsWith("#")) { continue; }

            // Normalize to lowercase for storage/compare but keep original for reporting
            var host = trimmed;

            if (!hostRegex.IsMatch(host))
            {
                errors.Add(new BulkImportErrorDto(lineNo, trimmed, "Invalid hostname format"));
                continue;
            }

            if (!seen.Add(host))
            {
                skipped.Add(host);
                continue;
            }

            // Skip if exists
            if (await _db.Hosts.AnyAsync(h => h.HostName == host, ct))
            {
                skipped.Add(host);
                continue;
            }

            var entity = new HostEntity { HostName = host };
            _db.Hosts.Add(entity);
            addedEntities.Add(entity);
            added.Add(host);
        }

        // Persist new hosts to generate IDs
        if (addedEntities.Count > 0) await _db.SaveChangesAsync(ct);

        // FR024: Immediately scan newly imported hosts (best-effort; failures do not abort import)
        foreach (var host in addedEntities)
        {
            try
            {
                var fetchRes = await _fetcher.FetchAsync(host.HostName, 443, ct);
                host.LastCheckedUtc = DateTime.UtcNow;
                if (fetchRes is { } tuple)
                {
                    host.IsReachable = true;
                    host.LastReachableUtc = host.LastCheckedUtc;
                    var (serial, notAfterUtc) = tuple;
                    var existing = await _db.Certificates.Where(c => c.HostId == host.Id).ToListAsync(ct);
                    if (existing.Count > 0) _db.Certificates.RemoveRange(existing);
                    _db.Certificates.Add(new CertificateRecord
                    {
                        HostId = host.Id,
                        SerialNumber = serial,
                        ExpirationUtc = notAfterUtc,
                        RetrievedAtUtc = DateTime.UtcNow
                    });
                }
                else
                {
                    host.IsReachable = false;
                }
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"BULK_CERT_FETCH_FAIL host={host.HostName} err={ex.Message}");
            }
        }

        var result = new BulkImportResultDto(
            AddedCount: added.Count,
            SkippedCount: skipped.Count,
            ErrorCount: errors.Count,
            AddedHosts: added,
            SkippedHosts: skipped,
            Errors: errors);

        return Ok(result);
    }
}
