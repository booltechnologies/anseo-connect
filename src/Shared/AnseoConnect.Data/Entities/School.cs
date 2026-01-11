using System;
using System.Collections.Generic;
using System.Text;
namespace AnseoConnect.Data.Entities;

public sealed class School
{
    public Guid SchoolId { get; set; }
    public Guid TenantId { get; set; }

    public string Name { get; set; } = "";
    public string SISProvider { get; set; } = "UNKNOWN";
    public string? WondeSchoolId { get; set; }
    public string? WondeDomain { get; set; } // Regional domain (optional, fetched from API if not set)
    public string Timezone { get; set; } = "Europe/Dublin";
    public DateTimeOffset? LastSyncUtc { get; set; } // Last successful sync timestamp for incremental sync
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Tenant? Tenant { get; set; }

    public ICollection<Student> Students { get; set; } = new List<Student>();
    public ICollection<Guardian> Guardians { get; set; } = new List<Guardian>();
}

