namespace VolksbankTracker.Tests;

public sealed class SqliteApiTests(SqliteFixture fixture) : ApiBehaviorTests(fixture), IClassFixture<SqliteFixture>;
