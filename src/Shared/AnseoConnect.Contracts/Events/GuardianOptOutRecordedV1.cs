using System;
using System.Collections.Generic;
using System.Text;

namespace AnseoConnect.Contracts.Events;

public record GuardianOptOutRecordedV1(
    Guid GuardianId,
    string Channel,
    string Source // "GUARDIAN_REPLY","PROVIDER_WEBHOOK","STAFF_ACTION"
);
