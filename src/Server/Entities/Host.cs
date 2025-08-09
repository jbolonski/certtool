namespace Server.Entities;

public class Host
{
    public long Id { get; set; }
    public string HostName { get; set; } = string.Empty;
    public bool IsReachable { get; set; }
    public DateTime? LastCheckedUtc { get; set; }
    public DateTime? LastReachableUtc { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<CertificateRecord> Certificates { get; set; } = new();
}
