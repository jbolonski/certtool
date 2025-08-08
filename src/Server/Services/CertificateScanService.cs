using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Services;

public class CertificateScanService : BackgroundService
{
    private readonly IServiceProvider _provider;
    private readonly ILogger<CertificateScanService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(24);

    public CertificateScanService(IServiceProvider provider, ILogger<CertificateScanService> logger)
    {
        _provider = provider;
        _logger = logger;
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
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var fetcher = scope.ServiceProvider.GetRequiredService<CertificateFetcher>();
        var hosts = await db.Hosts.AsNoTracking().ToListAsync(ct);
        _logger.LogInformation("Scanning {Count} hosts for certificates", hosts.Count);
        foreach (var h in hosts)
        {
            var result = await fetcher.FetchAsync(h.HostName, 443, ct);
            if (result is { } r)
            {
                var (serial, notAfterUtc) = r;
                db.Certificates.Add(new Entities.CertificateRecord
                {
                    HostId = h.Id,
                    SerialNumber = serial,
                    ExpirationUtc = notAfterUtc,
                    RetrievedAtUtc = DateTime.UtcNow
                });
            }
        }
        await db.SaveChangesAsync(ct);
    }
}
