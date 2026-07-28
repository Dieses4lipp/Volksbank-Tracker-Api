namespace VolksbankTracker.Core.Services;

public record CategoryBreakdown(
    string CategoryName,
    string Icon,
    string Color,
    decimal Total,
    int Count
);
