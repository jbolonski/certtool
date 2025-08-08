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
        var scanService = scope.ServiceProvider.GetRequiredService<Server.Services.CertificateFetcher>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hosts = await db.Hosts.AsNoTracking().ToListAsync();
        foreach (var h in hosts)
        {
            var result = await scanService.FetchAsync(h.HostName, 443);
            if (result is { } r)
            {
                var (serial, notAfterUtc) = r;
                // FR008: Remove any existing cert record for this host
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
        }
        await db.SaveChangesAsync();
        return Ok();
    }
}
