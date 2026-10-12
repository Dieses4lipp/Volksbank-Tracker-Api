namespace VolksbankTracker.Tests;

public sealed class PostgresApiTests(PostgresFixture fixture) : ApiBehaviorTests(fixture), IClassFixture<PostgresFixture>;
