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
    public HostsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<HostDto>>> Get()
    {
        var hosts = await _db.Hosts.OrderBy(h => h.HostName).Select(h => new HostDto(h.Id, h.HostName)).ToListAsync();
        return Ok(hosts);
    }

    [HttpPost]
    public async Task<ActionResult> Post([FromBody] HostDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.HostName)) return BadRequest("HostName required");
        if (await _db.Hosts.AnyAsync(h => h.HostName == dto.HostName)) return Conflict("Host already exists");
    var host = new HostEntity { HostName = dto.HostName.Trim() };
        _db.Hosts.Add(host);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = host.Id }, new HostDto(host.Id, host.HostName));
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult> Delete(long id)
    {
        var host = await _db.Hosts.FindAsync(id);
        if (host == null) return NotFound();
        _db.Hosts.Remove(host);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
