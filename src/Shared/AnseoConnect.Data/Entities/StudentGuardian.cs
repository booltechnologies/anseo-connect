using System;
using System.Collections.Generic;
using System.Text;

namespace AnseoConnect.Data.Entities;

public sealed class StudentGuardian : SchoolEntity
{
    public Guid StudentId { get; set; }
    public Guid GuardianId { get; set; }

    public string? Relationship { get; set; }

    public Student? Student { get; set; }
    public Guardian? Guardian { get; set; }
}

