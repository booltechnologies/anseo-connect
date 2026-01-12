using AnseoConnect.Data;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.EntityFrameworkCore;

var conn = Environment.GetEnvironmentVariable("ANSEO_SQL")
           ?? "Server=JHP;Database=AnseoConnectDev;User Id=sa;Password=F0urb4ll;TrustServerCertificate=True;";

// Create tenant context directly (migrations don't need real tenant scoping)
var tenantContext = new TenantContext();
tenantContext.Set(Guid.Parse("11111111-1111-1111-1111-111111111111"), null);

var options = new DbContextOptionsBuilder<AnseoConnectDbContext>()
    .UseSqlServer(conn)
    .Options;

using var db = new AnseoConnectDbContext(options, tenantContext);

Console.WriteLine("Applying migrations...");
await db.Database.MigrateAsync();
Console.WriteLine("Done.");
