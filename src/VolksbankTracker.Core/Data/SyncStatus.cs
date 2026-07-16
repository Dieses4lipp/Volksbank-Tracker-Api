namespace VolksbankTracker.Core.Data;

/// <summary>Values stored in <see cref="SyncLog.Status"/> and returned in sync results.</summary>
public static class SyncStatus
{
    public const string Running = "running";
    public const string Success = "success";
    public const string Failed = "failed";
}
