namespace VolksbankTracker.API;

/// <summary>
/// Rate limiter policy names, registered in Program.cs.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>
    /// Endpoints that talk to the bank (sync, balance).
    /// </summary>
    public const string Sync = "sync";

    /// <summary>
    /// Credential verification; wrong PINs count toward the bank's lockout.
    /// </summary>
    public const string Credentials = "credentials";
}
