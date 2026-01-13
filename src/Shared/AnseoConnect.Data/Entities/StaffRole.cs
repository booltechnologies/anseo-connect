namespace AnseoConnect.Data.Entities;

/// <summary>
/// Staff roles used for RBAC across the platform.
/// </summary>
public enum StaffRole
{
    AttendanceAdmin,   // Daily lists, contact, logging
    Teacher,           // Basic read access
    YearHead,          // Targeted interventions, meetings
    Principal,         // Oversight, reporting
    DeputyPrincipal,
    DLP,               // Safeguarding alerts
    ETBTrustAdmin      // Multi-school roll-ups
}
