using System;
using System.Collections.Generic;
using System.Text;
namespace AnseoConnect.Data.Entities;

public sealed class Guardian : SchoolEntity
{
    public Guid GuardianId { get; set; }

    public string ExternalGuardianId { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? MobileE164 { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<StudentGuardian> StudentGuardians { get; set; } = new List<StudentGuardian>();
    public ICollection<ConsentState> ConsentStates { get; set; } = new List<ConsentState>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
