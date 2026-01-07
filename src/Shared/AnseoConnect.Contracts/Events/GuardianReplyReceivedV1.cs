using System;
using System.Collections.Generic;
using System.Text;

namespace AnseoConnect.Contracts.Events;

public record GuardianReplyReceivedV1(
    Guid MessageId,
    Guid GuardianId,
    string Channel,
    string Text,
    bool IsOptOutKeyword
);
