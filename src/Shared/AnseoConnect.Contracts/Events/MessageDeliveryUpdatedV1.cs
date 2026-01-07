using System;
using System.Collections.Generic;
using System.Text;

namespace AnseoConnect.Contracts.Events;

public record MessageDeliveryUpdatedV1(
    Guid MessageId,
    string Provider,           // "TWILIO", "ZOHO", "ACS"
    string Status,             // "SENT","DELIVERED","FAILED"
    string? ProviderMessageId,
    string? ErrorCode,
    string? ErrorMessage
);

