namespace VolksbankTracker.Core.Services;

/// <summary>No-op bank parameter data store: BPD is re-fetched on every connection.</summary>
internal sealed class NoBpdStore : libfintx.FinTS.BankParameterData.IBpdStore
{
    public Task<int?> GetBPDVersion(int bankCountry, int bankCode)
        => Task.FromResult<int?>(null);

    public Task<string?> GetBPD(int bankCountry, int bankCode)
        => Task.FromResult<string?>(null);

    public Task SaveBPD(int bankCountry, int bankCode, string bpd)
        => Task.CompletedTask;

    public Task DeleteBPD(int bankCountry, int bankCode)
        => Task.CompletedTask;
}
