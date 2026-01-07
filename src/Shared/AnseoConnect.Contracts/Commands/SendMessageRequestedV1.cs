using System;
using System.Collections.Generic;
using System.Text;

namespace AnseoConnect.Contracts.Commands;

public record SendMessageRequestedV1(
    Guid CaseId,
    Guid StudentId,
    Guid GuardianId,
    string Channel,       // "SMS", "EMAIL", "WHATSAPP", "VOICE_AUTODIAL"
    string MessageType,   // "SERVICE_ATTENDANCE"
    string TemplateId,
    Dictionary<string, string> TemplateData
);

