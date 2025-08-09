using Microsoft.AspNetCore.Mvc;
using Server.Services;
using Shared;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScheduleController : ControllerBase
{
    private readonly ScanScheduleState _state;
    public ScheduleController(ScanScheduleState state)
    {
        _state = state;
    }

    [HttpGet]
    public ActionResult<ScanScheduleDto> Get()
    {
        var snap = _state.Snapshot();
        var dto = new ScanScheduleDto(snap.lastRunUtc, snap.nextRunUtc, snap.intervalHours);
        return Ok(dto);
    }
}
