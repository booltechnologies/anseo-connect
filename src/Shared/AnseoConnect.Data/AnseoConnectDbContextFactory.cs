using System;
using System.Collections.Generic;
using System.Text;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AnseoConnect.Data;

public sealed class AnseoConnectDbContextFactory : IDesignTimeDbContextFactory<AnseoConnectDbContext>
{
    public AnseoConnectDbContext CreateDbContext(string[] args)
    {
        var conn =
            Environment.GetEnvironmentVariable("ANSEO_SQL")
            ?? "Server=JHP;Database=AnseoConnectDev;User Id=sa;Password=F0urb4ll;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<AnseoConnectDbContext>()
            .UseSqlServer(conn)
            .Options;

        // Design-time: we just need non-empty TenantId so SaveChanges enforcement doesn't throw.
        var tenant = new TenantContext();
        tenant.Set(Guid.Parse("11111111-1111-1111-1111-111111111111"), null);

        return new AnseoConnectDbContext(options, tenant);
    }
}
