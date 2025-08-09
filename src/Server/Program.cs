using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connStr = BuildConnectionString();
// Explicit server version avoids early connection attempt for AutoDetect (which can fail if auth not finalized)
var serverVersion = new MySqlServerVersion(new Version(8, 0, 28));
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseMySql(connStr, serverVersion));

builder.Services.AddSingleton(new ScanScheduleState(24));
builder.Services.AddScoped<CertificateFetcher>();
builder.Services.AddHostedService<CertificateScanService>();

builder.Services.AddCors(o => o.AddPolicy("any", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Static files for Blazor Client
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseCors("any");

app.MapControllers();
app.MapFallbackToFile("index.html");

// Apply SQL migrations (simple runner) instead of EnsureCreated
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Migrations");
    var conn = db.Database.GetDbConnection();
    await conn.OpenAsync();
    // Load initial migration script
    var migrationPath = Path.Combine(AppContext.BaseDirectory, "db", "migrations", "202508081200__initial.sql");
    if (File.Exists(migrationPath))
    {
        var sql = await File.ReadAllTextAsync(migrationPath);
        // Split on "-- down" section if present; only run the part before it
        var upSql = sql.Split(new[] {"-- down"}, StringSplitOptions.RemoveEmptyEntries)[0];
        using var cmd = conn.CreateCommand();
        cmd.CommandText = upSql;
        try
        {
            await cmd.ExecuteNonQueryAsync();
            logger.LogInformation("Applied initial migration script");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Migration script execution encountered an error (may already be applied)");
        }
    }
}

app.Run();

static string BuildConnectionString()
{
    var host = Env("DB_HOST", "localhost");
    var port = Env("DB_PORT", "3306");
    var db = Env("DB_NAME", "cert_monitor");
    var user = Env("DB_USER", "root");
    var pwd = Env("DB_PASSWORD", "");
    if (string.IsNullOrWhiteSpace(pwd))
    {
        // Fallback to MYSQL_ROOT_PASSWORD if DB_PASSWORD not explicitly provided (common local mismatch)
        var rootPwd = Env("MYSQL_ROOT_PASSWORD", "");
        if (!string.IsNullOrEmpty(rootPwd) && user == "root") pwd = rootPwd;
    }
    var ssl = Env("DB_SSL_MODE", "Preferred");
    // Always include AllowPublicKeyRetrieval for dev when not enforcing TLS; production should disable when using VERIFY_* modes.
    var allowKey = Env("DB_ALLOW_PUBLIC_KEY_RETRIEVAL", "True");
    return $"Server={host};Port={port};Database={db};User={user};Password={pwd};SslMode={ssl};AllowPublicKeyRetrieval={allowKey};";
}

static string Env(string key, string def) => Environment.GetEnvironmentVariable(key) ?? def;
