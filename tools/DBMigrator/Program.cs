using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.AspNetCore.Identity;
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

// Seed identity roles for RBAC
var roleNames = Enum.GetNames(typeof(StaffRole));
var existingRoleNames = await db.Roles.Select(r => r.Name!).ToListAsync();

foreach (var roleName in roleNames)
{
    if (existingRoleNames.Contains(roleName))
    {
        continue;
    }

    db.Roles.Add(new IdentityRole<Guid>
    {
        Id = Guid.NewGuid(),
        Name = roleName,
        NormalizedName = roleName.ToUpperInvariant()
    });
}

if (db.ChangeTracker.HasChanges())
{
    await db.SaveChangesAsync();
    Console.WriteLine("Seeded staff roles.");
}
