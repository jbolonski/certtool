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
    public CertificatesController(AppDbContext db) => _db = db;

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
}
