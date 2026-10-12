using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace VolksbankTracker.Core.Data;

/// <summary>
/// Pins DateTime.Kind per column so both providers behave the same. Npgsql rejects
/// Kind=Utc for timestamp and anything but Utc for timestamptz; SQLite stores text and
/// ignores Kind, so on SQLite these only change the Kind of values read back.
/// </summary>
public static class DateTimeKindConverters
{
    /// <summary>Points in time, always set from DateTime.UtcNow.</summary>
    public static readonly ValueConverter<DateTime, DateTime> Utc = new(
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    /// <summary>Bank calendar dates without a time zone; query bounds derived from UtcNow compare by wall clock.</summary>
    public static readonly ValueConverter<DateTime, DateTime> Unspecified = new(
        v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified),
        v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified));
}
