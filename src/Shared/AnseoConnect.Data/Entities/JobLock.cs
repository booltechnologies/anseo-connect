namespace AnseoConnect.Data.Entities;

/// <summary>
/// Simple table-based distributed lock for background jobs.
/// </summary>
public sealed class JobLock
{
    public string LockName { get; set; } = string.Empty;
    public string HolderInstanceId { get; set; } = string.Empty;
    public DateTimeOffset AcquiredAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
