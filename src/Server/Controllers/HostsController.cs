using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Entities;
using HostEntity = Server.Entities.Host;
using Shared;

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
}
