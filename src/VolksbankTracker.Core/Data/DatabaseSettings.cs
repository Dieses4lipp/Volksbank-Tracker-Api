namespace VolksbankTracker.Core.Data;

public sealed record DatabaseSettings(DatabaseProvider Provider, string ConnectionString);
