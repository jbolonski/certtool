using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Shared;
using System.Text;

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

    // FR025: Export certificate report as CSV
    [HttpGet("export")]
    public async Task<IActionResult> ExportCsv()
    {
        var certs = await _db.Certificates
            .Include(c => c.Host)
            .OrderBy(c => c.ExpirationUtc)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var sb = new StringBuilder();
        // Header
        sb.AppendLine("Host,Serial,ExpirationUtc,DaysUntilExpiration,RetrievedAtUtc");

        foreach (var c in certs)
        {
            var days = (int)Math.Floor(c.ExpirationUtc > now ? (c.ExpirationUtc - now).TotalDays : 0);
            // Use ISO 8601 UTC for CSV stability
            string Esc(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
            var line = string.Join(",",
                new[]
                {
                    Esc(c.Host.HostName),
                    Esc(c.SerialNumber ?? string.Empty),
                    Esc(c.ExpirationUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")),
                    days.ToString(),
                    Esc(c.RetrievedAtUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"))
                });
            sb.AppendLine(line);
        }

        var csv = sb.ToString();
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(csv); // include BOM for Excel
        var fileName = $"certificates-{DateTime.UtcNow:yyyyMMdd}.csv";
        return File(bytes, "text/csv", fileName);
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
