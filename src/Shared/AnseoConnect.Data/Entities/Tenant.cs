using System;
using System.Collections.Generic;
using System.Text;
using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

public sealed class Tenant : ITenantScoped
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = "";
    public string CountryCode { get; set; } = "IE";
    public string DefaultPolicyPackId { get; set; } = "IE-ANSEO-DEFAULT";
    public string DefaultPolicyPackVersion { get; set; } = "1.3.0";
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<School> Schools { get; set; } = new List<School>();
}
