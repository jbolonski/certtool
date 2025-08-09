using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Shared;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    private readonly AppDbContext _db;
    public StatsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<StatsDto>> Get()
    {
        var hosts = await _db.Hosts.ToListAsync();
        var certs = await _db.Certificates.Include(c => c.Host).ToListAsync();
        var now = DateTime.UtcNow;
        var exp30 = certs.Count(c => c.ExpirationUtc <= now.AddDays(30));
        var exp60 = certs.Count(c => c.ExpirationUtc <= now.AddDays(60));
        var lastScan = certs.Count > 0 ? certs.Max(c => c.RetrievedAtUtc) : (DateTime?)null;
        int? daysSince = lastScan.HasValue ? (int)Math.Floor((now - lastScan.Value).TotalDays) : null;

        var dto = new StatsDto(
            HostsMonitored: hosts.Count,
            CertificatesWithData: certs.Count,
            ExpiringWithin30Days: exp30,
            ExpiringWithin60Days: exp60,
            UnreachableHosts: hosts.Count(h => !h.IsReachable),
            LastScanUtc: lastScan,
            DaysSinceLastScan: daysSince
        );
        return Ok(dto);
    }
}
