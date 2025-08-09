using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Shared;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CertificatesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IServiceProvider _provider;
    public CertificatesController(AppDbContext db, IServiceProvider provider)
    {
        _db = db;
        _provider = provider;
    }

    [HttpGet]
       public async Task<ActionResult<IEnumerable<CertificateDto>>> Get()
       {
           var certs = await _db.Certificates
               .Include(c => c.Host)
               .OrderBy(c => c.ExpirationUtc)
               .ToListAsync();

           var now = DateTime.UtcNow;
           var list = certs.Select(c => new CertificateDto(
               c.Id,
               c.HostId,
               c.Host.HostName,
               c.SerialNumber,
               c.ExpirationUtc,
               (int)Math.Floor(c.ExpirationUtc > now ? (c.ExpirationUtc - now).TotalDays : 0),
               c.RetrievedAtUtc
           ));
           return Ok(list);
    }

    // FR007: Manual refresh endpoint
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        using var scope = _provider.CreateScope();
        var fetcher = scope.ServiceProvider.GetRequiredService<Server.Services.CertificateFetcher>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hosts = await db.Hosts.ToListAsync(); // tracked so we can update reachability
        var now = DateTime.UtcNow;
        foreach (var h in hosts)
        {
            var result = await fetcher.FetchAsync(h.HostName, 443);
            h.LastCheckedUtc = DateTime.UtcNow; // per-host precise timestamp
            if (result is { } tuple)
            {
                h.IsReachable = true;
                h.LastReachableUtc = h.LastCheckedUtc;
                var (serial, notAfterUtc) = tuple;
                var old = await db.Certificates.Where(c => c.HostId == h.Id).ToListAsync();
                if (old.Count > 0)
                    db.Certificates.RemoveRange(old);
                db.Certificates.Add(new Server.Entities.CertificateRecord
                {
                    HostId = h.Id,
                    SerialNumber = serial,
                    ExpirationUtc = notAfterUtc,
                    RetrievedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                h.IsReachable = false;
            }
        }
        await db.SaveChangesAsync();
        return Ok();
    }
}
