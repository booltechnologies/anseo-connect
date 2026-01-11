using System;
using System.Collections.Generic;
using System.Text;
namespace AnseoConnect.Data.Entities;

public sealed class Student : SchoolEntity
{
    public Guid StudentId { get; set; }

    public string ExternalStudentId { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? YearGroup { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<StudentGuardian> StudentGuardians { get; set; } = new List<StudentGuardian>();
    public ICollection<AttendanceMark> AttendanceMarks { get; set; } = new List<AttendanceMark>();
    public ICollection<Case> Cases { get; set; } = new List<Case>();
}
