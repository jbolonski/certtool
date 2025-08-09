using Microsoft.EntityFrameworkCore;
using Server.Entities;
using HostEntity = Server.Entities.Host;

namespace Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<HostEntity> Hosts => Set<HostEntity>();
    public DbSet<CertificateRecord> Certificates => Set<CertificateRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
    modelBuilder.Entity<HostEntity>(e =>
        {
            e.ToTable("hosts");
            e.HasKey(x => x.Id);
            e.Property(x => x.HostName).HasColumnName("host_name").IsRequired().HasMaxLength(255);
            e.HasIndex(x => x.HostName).IsUnique();
            e.Property(x => x.IsReachable).HasColumnName("is_reachable").HasColumnType("TINYINT(1)").HasDefaultValue(false);
            e.Property(x => x.LastCheckedUtc).HasColumnName("last_checked_utc").HasColumnType("DATETIME(6)").IsRequired(false);
            e.Property(x => x.LastReachableUtc).HasColumnName("last_reachable_utc").HasColumnType("DATETIME(6)").IsRequired(false);
            e.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("TIMESTAMP")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("TIMESTAMP")
                .HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<CertificateRecord>(e =>
        {
            e.ToTable("certificates");
            e.HasKey(x => x.Id);
            e.Property(x => x.HostId).HasColumnName("host_id").IsRequired();
            e.Property(x => x.SerialNumber).HasColumnName("serial_number").IsRequired().HasMaxLength(128);
            e.Property(x => x.ExpirationUtc).HasColumnName("expiration_utc").HasColumnType("DATETIME(6)").IsRequired();
            e.Property(x => x.RetrievedAtUtc).HasColumnName("retrieved_at_utc").HasColumnType("DATETIME(6)").IsRequired();
            e.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("TIMESTAMP")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("TIMESTAMP")
                .HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");
            e.HasIndex(x => x.HostId);
            e.HasOne(x => x.Host).WithMany(h => h.Certificates).HasForeignKey(x => x.HostId);
        });
    }
}
