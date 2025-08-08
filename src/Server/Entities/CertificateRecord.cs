namespace Server.Entities;

public class CertificateRecord
{
    public long Id { get; set; }
    public long HostId { get; set; }
    public Host Host { get; set; } = null!;
    public string SerialNumber { get; set; } = string.Empty;
    public DateTime ExpirationUtc { get; set; }
    public DateTime RetrievedAtUtc { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
