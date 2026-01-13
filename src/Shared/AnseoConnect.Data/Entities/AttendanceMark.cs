using System;
using System.Collections.Generic;
using System.Text;

namespace AnseoConnect.Data.Entities;

public sealed class AttendanceMark : SchoolEntity
{
    public Guid AttendanceMarkId { get; set; }
    public Guid StudentId { get; set; }

    public DateOnly Date { get; set; }
    public string Session { get; set; } = "AM"; // AM/PM
    public string Status { get; set; } = "UNKNOWN"; // PRESENT/ABSENT/etc.
    public string? ReasonCode { get; set; }
    public string Source { get; set; } = "WONDE"; // WONDE/SIS/ANSEO_RFID
    public string? RawPayloadJson { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Student? Student { get; set; }
}

