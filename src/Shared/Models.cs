namespace Shared;

public record HostDto(long Id, string HostName, bool IsReachable, DateTime? LastCheckedUtc, DateTime? LastReachableUtc);
public record CertificateDto(long Id, long HostId, string HostName, string SerialNumber, DateTime ExpirationUtc, int DaysUntilExpiration, DateTime RetrievedAtUtc);
public record StatsDto(
	int HostsMonitored,
	int CertificatesWithData,
	int ExpiringWithin30Days,
	int ExpiringWithin60Days,
	int UnreachableHosts,
	DateTime? LastScanUtc,
	int? DaysSinceLastScan);
public record ScanScheduleDto(DateTime? LastRunUtc, DateTime? NextRunUtc, int IntervalHours);

// Bulk import DTOs
public record BulkImportErrorDto(int LineNumber, string Value, string Reason);
public record BulkImportResultDto(
	int AddedCount,
	int SkippedCount,
	int ErrorCount,
	List<string> AddedHosts,
	List<string> SkippedHosts,
	List<BulkImportErrorDto> Errors);
