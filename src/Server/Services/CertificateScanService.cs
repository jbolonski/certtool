using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Services;

public class CertificateScanService : BackgroundService
{
    private readonly IServiceProvider _provider;
    private readonly ILogger<CertificateScanService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(24);
    private readonly ScanScheduleState _schedule;

    public CertificateScanService(IServiceProvider provider, ILogger<CertificateScanService> logger, ScanScheduleState schedule)
    {
        _provider = provider;
        _logger = logger;
        _schedule = schedule;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
    _logger.LogInformation("Certificate scan service starting");
    await ScanOnce(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_interval, stoppingToken);
            await ScanOnce(stoppingToken);
        }
    }

    private async Task ScanOnce(CancellationToken ct)
    {
        var started = DateTime.UtcNow;
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var fetcher = scope.ServiceProvider.GetRequiredService<CertificateFetcher>();
        var hosts = await db.Hosts.ToListAsync(ct); // tracked to update reachability
        _logger.LogInformation("Scanning {Count} hosts for certificates", hosts.Count);
        foreach (var h in hosts)
        {
            var result = await fetcher.FetchAsync(h.HostName, 443, ct);
            h.LastCheckedUtc = DateTime.UtcNow;
            if (result is { } tuple)
            {
                h.IsReachable = true;
                h.LastReachableUtc = h.LastCheckedUtc;
                var (serial, notAfterUtc) = tuple;
                // Overwrite semantics only on successful fetch
                var existing = await db.Certificates.Where(c => c.HostId == h.Id).ToListAsync(ct);
                if (existing.Count > 0)
                    db.Certificates.RemoveRange(existing);
                db.Certificates.Add(new Entities.CertificateRecord
                {
                    HostId = h.Id,
                    SerialNumber = serial,
                    ExpirationUtc = notAfterUtc,
                    RetrievedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                h.IsReachable = false; // keep existing cert data if any; host just unreachable now
            }
        }
    await db.SaveChangesAsync(ct);
    _schedule.UpdateOnRun(started);
    }
}
