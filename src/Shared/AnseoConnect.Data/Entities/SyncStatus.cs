namespace AnseoConnect.Data.Entities;

/// <summary>
/// Indicates the health of external data synchronization for a school.
/// </summary>
public enum SyncStatus
{
    Healthy,
    Warning,
    Failed,
    Paused
}
