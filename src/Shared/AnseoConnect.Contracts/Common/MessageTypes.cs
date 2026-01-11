using System;
using System.Collections.Generic;
using System.Text;

namespace AnseoConnect.Contracts.Common;

public static class MessageTypes
{
    public const string AttendanceMarksIngestedV1 = nameof(Events.AttendanceMarksIngestedV1);
    public const string SendMessageRequestedV1 = nameof(Commands.SendMessageRequestedV1);
    public const string MessageDeliveryUpdatedV1 = nameof(Events.MessageDeliveryUpdatedV1);
    public const string GuardianReplyReceivedV1 = nameof(Events.GuardianReplyReceivedV1);
    public const string GuardianOptOutRecordedV1 = nameof(Events.GuardianOptOutRecordedV1);
    public const string CaseCreatedV1 = nameof(Events.CaseCreatedV1);
    public const string SafeguardingAlertCreatedV1 = nameof(Events.SafeguardingAlertCreatedV1);
}
