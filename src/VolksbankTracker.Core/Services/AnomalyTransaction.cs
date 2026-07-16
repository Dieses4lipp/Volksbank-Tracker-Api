namespace VolksbankTracker.Core.Services;

public record AnomalyTransaction(
    int Id,
    DateTime BookingDate,
    decimal Amount,
    string Purpose,
    string CreditorName,
    int? CategoryId,
    string CategoryName,
    decimal BaselineAverage,
    decimal BaselineStdDev,
    decimal DeviationFactor
);
