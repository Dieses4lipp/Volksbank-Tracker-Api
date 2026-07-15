using libfintx.FinTS;
using libfintx.FinTS.Data;
using libfintx.Swift;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using VolksbankTracker.Core.Data;

namespace VolksbankTracker.Core.Services;

public class FinTsConfig
{
    public string BankUrl { get; set; } = "";
    public string BlZ { get; set; } = "";
    public string Iban { get; set; } = "";
    public string Bic { get; set; } = "";
    public string Account { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Pin { get; set; } = "";
}

public class SyncResult
{
    public int Fetched { get; set; }
    public int NewRecords { get; set; }
    public string Status { get; set; } = "success";
    public string? Error { get; set; }
    public string Bic { get; set; } = "";
    public string Account { get; set; } = "";
}

public class BalanceResult
{
    public string Status { get; set; } = "success";
    public string? Error { get; set; }
    public decimal? Balance { get; set; }
    public decimal? AvailableBalance { get; set; }
}

internal class NoBpdStore : libfintx.FinTS.BankParameterData.IBpdStore
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
public class FinTsSyncService(
    AppDbContext db,
    CategorizationService categorization,
    ILogger<FinTsSyncService> logger)
{
    /// <summary>Sicherheitsverfahren 946 = Decoupled pushTAN (SecureGo plus).</summary>
    private const string DecoupledPushTanMechanism = "946";

    public async Task<SyncResult> SyncAsync(FinTsConfig config, DateTime? from = null)
    {
        var log = new SyncLog { StartedAt = DateTime.UtcNow };
        await db.SyncLogs.AddAsync(log);
        await db.SaveChangesAsync();

        try
        {
            var startDate = from ?? DateTime.UtcNow.AddDays(-90);

            var bankConnection = CreateClient(config);

            var syncResult = await bankConnection.Synchronization();
            foreach (var msg in syncResult.Messages)
                logger.LogInformation("Sync message: {Code} | {Message}", msg.Code, msg.Message);

            logger.LogInformation("SystemId: {SystemId} HITANS version: {Hitans}", bankConnection.SystemId, bankConnection.HITANS);

            var transactionsResult = await bankConnection.Transactions_camt(
                CreateTanDialog(),
                libfintx.FinTS.Camt.CamtVersion.Camt052,
                startDate,
                DateTime.UtcNow
            );

            if (transactionsResult.HasError)
            {
                log.Status = "failed";
                var errorMsg = transactionsResult.Messages != null
                    ? string.Join(", ", transactionsResult.Messages.Select(m => m.ToString()))
                    : "Unbekannter FinTS Fehler";

                log.Error = errorMsg;
                logger.LogError("FINTS FEHLER: {Error}", errorMsg);

                log.CompletedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
                return new SyncResult { Status = "failed", Error = errorMsg };
            }

            var transactionsStatements = transactionsResult.Data;


            if (transactionsStatements == null || !transactionsStatements.Any())
            {
                log.Status = "success";
                log.CompletedAt = DateTime.UtcNow;
                log.TransactionsFetched = 0;
                log.TransactionsNew = 0;
                await db.SaveChangesAsync();
                return new SyncResult { Fetched = 0, NewRecords = 0 };
            }

            var transactions = transactionsResult.Data
            ?.SelectMany(s => s.Transactions)
            .ToList() ?? [];

            log.TransactionsFetched = transactions.Count;

            var existingHashes = await db.Transactions.Select(t => t.Hash).ToHashSetAsync();
            var categories = await db.Categories.ToListAsync();

            int newCount = 0;
            foreach (var raw in transactions)
            {
                var hash = ComputeHashCamt(raw);
                if (!existingHashes.Add(hash))
                    continue;

                var transaction = MapCamtTransaction(raw, hash);
                await categorization.CategorizeAsync(transaction, categories);
                await db.Transactions.AddAsync(transaction);
                newCount++;
            }

            log.TransactionsNew = newCount;
            log.Status = "success";
            log.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            await MarkRecurringAsync();

            logger.LogInformation("Sync complete: {Fetched} fetched, {New} new", transactions.Count, newCount);
            return new SyncResult { Fetched = transactions.Count, NewRecords = newCount };
        }
        catch (Exception ex)
        {
            log.Status = "failed";
            log.Error = ex.Message;
            log.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            logger.LogError(ex, "FINTS FEHLER: {Error}", ex.Message);

            return new SyncResult { Status = "failed", Error = ex.Message };
        }
    }

    public async Task<BalanceResult> GetBalanceAsync(FinTsConfig config)
    {
        try
        {
            var bankConnection = CreateClient(config);
            await bankConnection.Synchronization();

            var balanceResult = await bankConnection.Balance(CreateTanDialog());

            if (balanceResult.HasError || !balanceResult.Data.Successful)
            {
                var errorMsg = balanceResult.Messages != null
                    ? string.Join(", ", balanceResult.Messages.Select(m => m.ToString()))
                    : balanceResult.Data.Message;

                logger.LogError("FINTS BALANCE FEHLER: {Error}", errorMsg);
                return new BalanceResult { Status = "failed", Error = errorMsg };
            }

            return new BalanceResult
            {
                Balance = balanceResult.Data.Balance,
                AvailableBalance = balanceResult.Data.AvailableBalance
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FINTS BALANCE FEHLER: {Error}", ex.Message);
            return new BalanceResult { Status = "failed", Error = ex.Message };
        }
    }

    private FinTsClient CreateClient(FinTsConfig config)
    {
        if (!int.TryParse(config.BlZ, out var blz))
            throw new InvalidOperationException($"FinTs:BlZ ist keine gültige Bankleitzahl: '{config.BlZ}'");

        var client = new FinTsClient(new ConnectionDetails
        {
            Url = config.BankUrl,
            Blz = blz,
            Iban = config.Iban,
            Bic = config.Bic,
            Account = config.Account,
            UserId = config.UserId,
            Pin = config.Pin,
            CustomerSystemId = "0"
        }, bpdDataStore: new NoBpdStore());

        client.HIRMS = DecoupledPushTanMechanism;
        return client;
    }

    private TANDialog CreateTanDialog()
    {
        var tanDialog = new TANDialog(
            dialog =>
            {
                logger.LogInformation("Push-TAN gesendet, warte auf Freigabe in der App...");
                return Task.FromResult("");
            },
            approved =>
            {
                logger.LogInformation("Push-TAN Freigabe: {Approved}", approved ? "erteilt" : "abgelehnt/fehlgeschlagen");
                return Task.CompletedTask;
            });
        tanDialog.IsDecoupled = true;
        return tanDialog;
    }

    private static VolksbankTracker.Core.Data.Transaction MapCamtTransaction(
    libfintx.FinTS.Camt.CamtTransaction raw, string hash)
    {
        bool isDebit = raw.Amount < 0;
        return new VolksbankTracker.Core.Data.Transaction
        {
            Hash         = hash,
            BookingDate  = raw.InputDate == default ? DateTime.UtcNow : raw.InputDate,
            ValueDate = raw.ValueDate == default ? raw.InputDate : raw.ValueDate,
            Amount       = raw.Amount,
            Currency     = "EUR",
            Purpose      = raw.Description ?? raw.Text ?? "",
            CreditorName = isDebit ? raw.PartnerName ?? "" : "",
            CreditorIban = isDebit ? raw.AccountCode ?? "" : "",
            DebtorName   = isDebit ? "" : raw.PartnerName ?? "",
            DebtorIban   = isDebit ? "" : raw.AccountCode ?? "",
        };
    }

    private static string ComputeHashCamt(libfintx.FinTS.Camt.CamtTransaction t)
    {
        var raw = $"{t.InputDate:yyyyMMdd}|{t.Amount}|{t.Description}|{t.AccountCode}|{t.EndToEndId}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }

    public async Task MarkRecurringAsync()
    {
        var candidates = await db.Transactions
            .Where(t => t.Amount < 0 && t.CreditorIban != "")
            .GroupBy(t => new { t.CreditorIban, t.Amount })
            .Where(g => g.Count() >= 3)
            .Select(g => new { g.Key.CreditorIban, g.Key.Amount })
            .ToListAsync();

        await db.Transactions
            .Where(t => t.IsRecurring)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsRecurring, false));

        foreach (var c in candidates)
        {
            await db.Transactions
                .Where(t => t.CreditorIban == c.CreditorIban && t.Amount == c.Amount)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsRecurring, true));
        }
    }
}
