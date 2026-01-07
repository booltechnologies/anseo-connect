using System;
using System.Collections.Generic;
using System.Text;

namespace AnseoConnect.Contracts.Events;

public record AttendanceMarksIngestedV1(
    DateOnly Date,
    int StudentCount,
    int MarkCount,
    string Source // "WONDE"
);
