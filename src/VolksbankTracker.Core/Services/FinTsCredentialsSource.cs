namespace VolksbankTracker.Core.Services;

/// <summary>Where the FinTS credentials returned by <see cref="FinTsCredentialsService"/> come from.</summary>
public enum FinTsCredentialsSource
{
    /// <summary>Nothing stored and the FinTs configuration section is incomplete.</summary>
    None,
    /// <summary>FinTs configuration section (appsettings / user secrets / environment).</summary>
    Configuration,
    /// <summary>Encrypted AppSettings row, set via the API.</summary>
    Database,
    /// <summary>A stored row exists but cannot be decrypted or parsed (e.g. Data Protection key ring lost).</summary>
    Unreadable
}
