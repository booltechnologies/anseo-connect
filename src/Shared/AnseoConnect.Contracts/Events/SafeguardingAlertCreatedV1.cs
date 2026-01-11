using System;
using System.Collections.Generic;
using System.Text;

namespace AnseoConnect.Contracts.Events;

public record SafeguardingAlertCreatedV1(
    Guid AlertId,
    Guid CaseId,
    Guid StudentId,
    string Severity,     // "HIGH", "MEDIUM", "LOW", "CRITICAL"
    string? ChecklistId, // e.g. "SBK_DEFAULT_HIGH"
    bool RequiresHumanReview
);

