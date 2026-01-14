using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Default tenant GUID (matches DBMigrator)
const string DefaultTenantId = "11111111-1111-1111-1111-111111111111";

var conn = Environment.GetEnvironmentVariable("ANSEO_SQL")
           ?? "Server=JHP;Database=AnseoConnectDev;User Id=sa;Password=F0urb4ll;TrustServerCertificate=True;";

var defaultTenantGuid = Guid.Parse(DefaultTenantId);

// Configuration from environment variables
var tenantName = Environment.GetEnvironmentVariable("ANSEO_SEED_TENANT_NAME") ?? "Development Tenant";
var schoolName = Environment.GetEnvironmentVariable("ANSEO_SEED_SCHOOL_NAME") ?? "Development School";
var wondeSchoolId = Environment.GetEnvironmentVariable("ANSEO_SEED_WONDE_SCHOOL_ID");
var seedUsers = Environment.GetEnvironmentVariable("ANSEO_SEED_USERS")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
var adminUsername = Environment.GetEnvironmentVariable("ANSEO_SEED_ADMIN_USERNAME") ?? "admin";
var adminPassword = Environment.GetEnvironmentVariable("ANSEO_SEED_ADMIN_PASSWORD") ?? "ChangeMe123!";
var adminEmail = Environment.GetEnvironmentVariable("ANSEO_SEED_ADMIN_EMAIL");
var adminRole = Environment.GetEnvironmentVariable("ANSEO_SEED_ADMIN_ROLE") ?? "Principal";

try
{
    Console.WriteLine("Starting data seeding...");

    // Create tenant context for School/User operations
    var tenantContext = new TenantContext();
    tenantContext.Set(defaultTenantGuid, null);

    var options = new DbContextOptionsBuilder<AnseoConnectDbContext>()
        .UseSqlServer(conn)
        .Options;

    using var db = new AnseoConnectDbContext(options, tenantContext);

    // Seed Tenant (not tenant-scoped, so no tenant context needed)
    var tenant = await SeedTenantAsync(db, defaultTenantGuid, tenantName);
    if (tenant == null)
    {
        Console.WriteLine("Failed to seed tenant. Exiting.");
        return 1;
    }

    // Update tenant context with the actual tenant
    tenantContext.Set(tenant.TenantId, null);

    // Seed School (tenant-scoped, needs tenant context)
    var school = await SeedSchoolAsync(db, tenant.TenantId, schoolName, wondeSchoolId);
    if (school == null)
    {
        Console.WriteLine("Failed to seed school. Exiting.");
        return 1;
    }

    // Update tenant context with tenant and school
    tenantContext.Set(tenant.TenantId, school.SchoolId);

    // Optionally seed users
    if (seedUsers)
    {
        await SeedAdminUserAsync(db, tenant.TenantId, school.SchoolId, adminUsername, adminPassword, adminEmail, adminRole);
    }
    else
    {
        Console.WriteLine("User seeding is disabled. Set ANSEO_SEED_USERS=true to enable.");
    }

    Console.WriteLine("Data seeding completed successfully.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error during seeding: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    return 1;
}

static async Task<Tenant?> SeedTenantAsync(AnseoConnectDbContext db, Guid tenantId, string name)
{
    try
    {
        // Check if tenant already exists
        var existingTenant = await db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TenantId == tenantId);

        if (existingTenant != null)
        {
            Console.WriteLine($"Tenant '{existingTenant.Name}' already exists (ID: {tenantId}).");
            return existingTenant;
        }

        // Create new tenant
        var tenant = new Tenant
        {
            TenantId = tenantId,
            Name = name,
            CountryCode = "IE",
            DefaultPolicyPackId = "IE-ANSEO-DEFAULT",
            DefaultPolicyPackVersion = "1.3.0",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        Console.WriteLine($"Created tenant '{name}' (ID: {tenantId}).");
        return tenant;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error seeding tenant: {ex.Message}");
        return null;
    }
}

static async Task<School?> SeedSchoolAsync(AnseoConnectDbContext db, Guid tenantId, string name, string? wondeSchoolId)
{
    try
    {
        // Check if school already exists for this tenant
        var existingSchool = await db.Schools
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);

        if (existingSchool != null)
        {
            Console.WriteLine($"School '{existingSchool.Name}' already exists for tenant (ID: {existingSchool.SchoolId}).");
            return existingSchool;
        }

        // Create new school
        var school = new School
        {
            SchoolId = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            SISProvider = "WONDE",
            WondeSchoolId = wondeSchoolId,
            Timezone = "Europe/Dublin",
            SyncStatus = SyncStatus.Healthy,
            SyncErrorCount = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        db.Schools.Add(school);
        await db.SaveChangesAsync();

        Console.WriteLine($"Created school '{name}' (ID: {school.SchoolId}).");
        if (!string.IsNullOrEmpty(wondeSchoolId))
        {
            Console.WriteLine($"  Wonde School ID: {wondeSchoolId}");
        }

        return school;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error seeding school: {ex.Message}");
        return null;
    }
}

static async Task SeedAdminUserAsync(
    AnseoConnectDbContext db,
    Guid tenantId,
    Guid schoolId,
    string username,
    string password,
    string? email,
    string roleName)
{
    try
    {
        // Check if user already exists
        var existingUser = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.SchoolId == schoolId && u.UserName == username);

        if (existingUser != null)
        {
            Console.WriteLine($"User '{username}' already exists (ID: {existingUser.Id}).");
            return;
        }

        // Create password hasher
        var passwordHasher = new PasswordHasher<AppUser>();

        // Create new user
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SchoolId = schoolId,
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email?.ToUpperInvariant(),
            EmailConfirmed = !string.IsNullOrEmpty(email),
            FirstName = "Admin",
            LastName = "User",
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        // Hash password
        user.PasswordHash = passwordHasher.HashPassword(user, password);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        Console.WriteLine($"Created user '{username}' (ID: {user.Id}).");

        // Assign role
        var role = await db.Roles
            .FirstOrDefaultAsync(r => r.Name == roleName);

        if (role != null)
        {
            var userRole = new IdentityUserRole<Guid>
            {
                UserId = user.Id,
                RoleId = role.Id
            };

            db.Set<IdentityUserRole<Guid>>().Add(userRole);
            await db.SaveChangesAsync();

            Console.WriteLine($"  Assigned role '{roleName}' to user '{username}'.");
        }
        else
        {
            Console.WriteLine($"  Warning: Role '{roleName}' not found. User created without role assignment.");
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error seeding admin user: {ex.Message}");
        // Don't throw - allow other operations to continue
    }
}
