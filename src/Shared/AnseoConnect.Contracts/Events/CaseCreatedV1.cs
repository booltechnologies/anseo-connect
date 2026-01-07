using System;
using System.Collections.Generic;
using System.Text;

namespace AnseoConnect.Contracts.Events;

public record CaseCreatedV1(
    Guid CaseId,
    Guid StudentId,
    int Tier,
    string CaseType // "ATTENDANCE","SAFEGUARDING"
);

