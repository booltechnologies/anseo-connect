using AnseoConnect.Data;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var conn = Environment.GetEnvironmentVariable("ANSEO_SQL")
           ?? "Server=JHP;Database=AnseoConnectDev;User Id=sa;Password=F0urb4ll;TrustServerCertificate=True;";

var services = new ServiceCollection();

services.AddScoped<TenantContext>();
services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

services.AddDbContext<AnseoConnectDbContext>((sp, opt) =>
{
    // tenant values are irrelevant for migrations, but must be non-empty for SaveChanges enforcement
    var tc = sp.GetRequiredService<TenantContext>();
    tc.Set(Guid.Parse("11111111-1111-1111-1111-111111111111"), null);

    opt.UseSqlServer(conn);
});

var sp = services.BuildServiceProvider();

using var scope = sp.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AnseoConnectDbContext>();

Console.WriteLine("Applying migrations...");
await db.Database.MigrateAsync();
Console.WriteLine("Done.");
