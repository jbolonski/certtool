namespace Shared;

public record HostDto(long Id, string HostName);
public record CertificateDto(long Id, long HostId, string HostName, string SerialNumber, DateTime ExpirationUtc, int DaysUntilExpiration, DateTime RetrievedAtUtc);
