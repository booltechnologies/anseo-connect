using System.Linq.Expressions;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using AnseoConnect.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace AnseoConnect.Data;

public sealed class AnseoConnectDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    private readonly ITenantContext _tenant;

    public AnseoConnectDbContext(DbContextOptions<AnseoConnectDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<School> Schools => Set<School>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Guardian> Guardians => Set<Guardian>();
    public DbSet<StudentGuardian> StudentGuardians => Set<StudentGuardian>();
    public DbSet<AttendanceMark> AttendanceMarks => Set<AttendanceMark>();
    public DbSet<ConsentState> ConsentStates => Set<ConsentState>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<CaseTimelineEvent> CaseTimelineEvents => Set<CaseTimelineEvent>();
    public DbSet<SafeguardingAlert> SafeguardingAlerts => Set<SafeguardingAlert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Identity tables
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("AppUsers");
            entity.HasIndex(x => new { x.TenantId, x.SchoolId, x.NormalizedUserName })
                .IsUnique()
                .HasDatabaseName("IX_AppUsers_Tenant_School_UserName");
            entity.HasIndex(x => new { x.TenantId, x.SchoolId, x.NormalizedEmail })
                .HasDatabaseName("IX_AppUsers_Tenant_School_Email");
        });

        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("AppRoles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("AppUserRoles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("AppUserClaims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("AppUserLogins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("AppUserTokens");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("AppRoleClaims");

        ApplyTenantAndSchoolFilters(modelBuilder);
        
        modelBuilder.Entity<Tenant>().HasKey(x => x.TenantId);

        modelBuilder.Entity<School>().HasKey(x => x.SchoolId);
        modelBuilder.Entity<School>()
            .HasIndex(x => new { x.TenantId, x.WondeSchoolId })
            .HasDatabaseName("IX_Schools_Tenant_WondeSchoolId");

        modelBuilder.Entity<Student>().HasKey(x => x.StudentId);
        modelBuilder.Entity<Student>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.ExternalStudentId })
            .IsUnique();

        modelBuilder.Entity<Guardian>().HasKey(x => x.GuardianId);
        modelBuilder.Entity<Guardian>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.ExternalGuardianId })
            .IsUnique();

        modelBuilder.Entity<StudentGuardian>().HasKey(x => new { x.StudentId, x.GuardianId });
        modelBuilder.Entity<StudentGuardian>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.StudentId });

        modelBuilder.Entity<AttendanceMark>().HasKey(x => x.AttendanceMarkId);
        modelBuilder.Entity<AttendanceMark>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.StudentId, x.Date, x.Session })
            .IsUnique();

        modelBuilder.Entity<StudentGuardian>()
            .HasOne(x => x.Student)
            .WithMany(x => x.StudentGuardians)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Cascade); // keep

        modelBuilder.Entity<StudentGuardian>()
            .HasOne(x => x.Guardian)
            .WithMany(x => x.StudentGuardians)
            .HasForeignKey(x => x.GuardianId)
            .OnDelete(DeleteBehavior.NoAction); // IMPORTANT

        modelBuilder.Entity<ConsentState>().HasKey(x => x.ConsentStateId);
        modelBuilder.Entity<ConsentState>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.GuardianId, x.Channel })
            .IsUnique()
            .HasDatabaseName("IX_ConsentStates_Tenant_School_Guardian_Channel");

        modelBuilder.Entity<Message>().HasKey(x => x.MessageId);
        modelBuilder.Entity<Message>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.CaseId, x.CreatedAtUtc })
            .HasDatabaseName("IX_Messages_Tenant_School_Case_Created");
        modelBuilder.Entity<Message>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.GuardianId, x.CreatedAtUtc })
            .HasDatabaseName("IX_Messages_Tenant_School_Guardian_Created");

        modelBuilder.Entity<Case>().HasKey(x => x.CaseId);
        modelBuilder.Entity<Case>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.StudentId, x.Status })
            .HasDatabaseName("IX_Cases_Tenant_School_Student_Status");
        modelBuilder.Entity<Case>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.CaseType, x.Status })
            .HasDatabaseName("IX_Cases_Tenant_School_Type_Status");

        modelBuilder.Entity<CaseTimelineEvent>().HasKey(x => x.EventId);
        modelBuilder.Entity<CaseTimelineEvent>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.CaseId, x.CreatedAtUtc })
            .HasDatabaseName("IX_CaseTimelineEvents_Tenant_School_Case_Created");

        modelBuilder.Entity<SafeguardingAlert>().HasKey(x => x.AlertId);
        modelBuilder.Entity<SafeguardingAlert>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.CaseId, x.RequiresHumanReview })
            .HasDatabaseName("IX_SafeguardingAlerts_Tenant_School_Case_Review");

        // Relationships
        modelBuilder.Entity<CaseTimelineEvent>()
            .HasOne(x => x.Case)
            .WithMany(x => x.TimelineEvents)
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SafeguardingAlert>()
            .HasOne(x => x.Case)
            .WithMany(x => x.SafeguardingAlerts)
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public override int SaveChanges()
    {
        EnforceTenancyOnWrites();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EnforceTenancyOnWrites();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTenantAndSchoolFilters(ModelBuilder modelBuilder)
    {
        // These values come from the scoped TenantContext. They must be set before querying.
        var tenantId = _tenant.TenantId;
        var schoolId = _tenant.SchoolId;

        // Skip tenant filtering for AppUser - Identity operations need global access
        // AppUser access is controlled by unique index on (TenantId, SchoolId, NormalizedUserName)
        
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            // Skip AppUser for tenant filtering (handled by unique constraint)
            if (clrType == typeof(AppUser))
            {
                continue;
            }

            // Tenant filter
            if (typeof(ITenantScoped).IsAssignableFrom(clrType))
            {
                var parameter = Expression.Parameter(clrType, "e");
                var tenantProp = Expression.Property(parameter, nameof(ITenantScoped.TenantId));
                var tenantValue = Expression.Constant(tenantId);
                var tenantEq = Expression.Equal(tenantProp, tenantValue);
                var lambda = Expression.Lambda(tenantEq, parameter);

                modelBuilder.Entity(clrType).HasQueryFilter(lambda);
            }

            // Optional school filter if the entity is school-scoped and SchoolId is set
            if (schoolId.HasValue && typeof(ISchoolScoped).IsAssignableFrom(clrType))
            {
                var parameter = Expression.Parameter(clrType, "e");
                var schoolProp = Expression.Property(parameter, nameof(ISchoolScoped.SchoolId));
                var schoolValue = Expression.Constant(schoolId.Value);
                var schoolEq = Expression.Equal(schoolProp, schoolValue);
                var lambda = Expression.Lambda(schoolEq, parameter);

                modelBuilder.Entity(clrType).HasQueryFilter(lambda);
            }
        }
    }

    private void EnforceTenancyOnWrites()
    {
        var tenantId = _tenant.TenantId;
        var schoolId = _tenant.SchoolId;
        var appUserEntries = ChangeTracker.Entries<AppUser>().ToList();
        var hasNonAppUserChanges = ChangeTracker.Entries<ITenantScoped>()
            .Any(e => e.Entity is not AppUser);

        // Require TenantContext for non-AppUser changes
        if (hasNonAppUserChanges && tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("TenantContext.TenantId not set before SaveChanges.");
        }

        // Handle AppUser - allow explicit TenantId/SchoolId or use TenantContext
        foreach (var entry in appUserEntries)
        {
            if (entry.State == EntityState.Added)
            {
                // Use TenantContext if available, otherwise require explicit TenantId
                if (entry.Entity.TenantId == Guid.Empty)
                {
                    if (tenantId == Guid.Empty)
                    {
                        throw new InvalidOperationException("AppUser must have TenantId set explicitly when TenantContext is not available.");
                    }
                    entry.Entity.TenantId = tenantId;
                    if (schoolId.HasValue && entry.Entity.SchoolId == Guid.Empty)
                    {
                        entry.Entity.SchoolId = schoolId.Value;
                    }
                }
                else if (tenantId != Guid.Empty && entry.Entity.TenantId != tenantId)
                {
                    throw new InvalidOperationException($"AppUser TenantId {entry.Entity.TenantId} does not match TenantContext {tenantId}.");
                }
            }
            else if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                if (tenantId == Guid.Empty)
                {
                    throw new InvalidOperationException("TenantContext must be set to modify or delete AppUser.");
                }
                if (entry.Entity.TenantId != tenantId)
                {
                    throw new InvalidOperationException("AppUser TenantId mismatch on write.");
                }
            }
        }

        // Handle other tenant-scoped entities (not AppUser)
        foreach (var entry in ChangeTracker.Entries<ITenantScoped>())
        {
            if (entry.Entity is AppUser)
            {
                continue; // Already handled above
            }

            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = tenantId;
            }
            else if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                if (entry.Entity.TenantId != tenantId)
                    throw new InvalidOperationException("TenantId mismatch on write.");
            }
        }

        // Handle school-scoped entities (not AppUser)
        if (schoolId.HasValue)
        {
            foreach (var entry in ChangeTracker.Entries<ISchoolScoped>())
            {
                if (entry.Entity is AppUser)
                {
                    continue; // Already handled above
                }

                if (entry.State == EntityState.Added)
                {
                    entry.Entity.SchoolId = schoolId.Value;
                }
                else if (entry.State is EntityState.Modified or EntityState.Deleted)
                {
                    if (entry.Entity.SchoolId != schoolId.Value)
                        throw new InvalidOperationException("SchoolId mismatch on write.");
                }
            }
        }
    }
}
