using System;
using System.Collections.Generic;
using System.Text;

namespace AnseoConnect.Contracts.Common;

public record MessageEnvelope<T>(
    string MessageType,
    string Version,
    Guid TenantId,
    Guid SchoolId,
    string CorrelationId,
    DateTimeOffset OccurredAtUtc,
    T Payload
);