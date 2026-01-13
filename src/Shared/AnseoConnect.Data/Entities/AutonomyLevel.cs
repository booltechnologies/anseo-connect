namespace AnseoConnect.Data.Entities;

/// <summary>
/// AI autonomy level per school/workflow.
/// </summary>
public enum AutonomyLevel
{
    A0_Advisory,     // Drafts/recommendations only
    A1_AutoMessage,  // Can send messages within policy constraints
    A2_AutoEscalate  // Can advance cases (never safeguarding)
}
