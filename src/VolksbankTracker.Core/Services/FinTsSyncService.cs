using libfintx.FinTS;
using libfintx.FinTS.Camt;
using libfintx.FinTS.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using VolksbankTracker.Core.Data;
using Transaction = VolksbankTracker.Core.Data.Transaction;

namespace VolksbankTracker.Core.Services;

public class FinTsSyncService(
    AppDbContext db,
    CategorizationService categorization,
    ILogger<FinTsSyncService> logger)
{
    /// <summary>Security procedure 946 = decoupled pushTAN (SecureGo plus).</summary>
    private const string DecoupledPushTanMechanism = "946";

    private const string UnknownError = "Unknown FinTS error";

    public async Task<SyncResult> SyncAsync(FinTsConfig config, DateTime? from = null)
    {
        var log = new SyncLog { StartedAt = DateTime.UtcNow };
        await db.SyncLogs.AddAsync(log);
        await db.SaveChangesAsync();

        try
        {
            var (bankConnection, connectError) = await ConnectAsync(config);
            if (connectError is not null)
                return await FailSyncAsync(log, connectError);

            logger.LogInformation("SystemId: {SystemId} HITANS version: {Hitans}", bankConnection.SystemId, bankConnection.HITANS);

            var transactionsResult = await bankConnection.Transactions_camt(
                CreateTanDialog(),
                CamtVersion.Camt052,
                from ?? DateTime.UtcNow.AddDays(-90),
                DateTime.UtcNow);

            if (transactionsResult.HasError)
                return await FailSyncAsync(log, DescribeErrors(transactionsResult));

            var transactions = transactionsResult.Data?
                .SelectMany(s => s.Transactions)
                .ToList() ?? [];

            var existingHashes = await db.Transactions.Select(t => t.Hash).ToHashSetAsync();
            var newTransactions = new List<Transaction>();
            foreach (var raw in transactions)
            {
                var hash = ComputeHashCamt(raw);
                if (existingHashes.Add(hash))
                    newTransactions.Add(MapCamtTransaction(raw, hash));
            }

            await categorization.CategorizeAsync(newTransactions);
            await db.Transactions.AddRangeAsync(newTransactions);

            log.TransactionsFetched = transactions.Count;
            log.TransactionsNew = newTransactions.Count;
            log.Status = SyncStatus.Success;
            log.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            // Recurring flags depend only on stored transactions, so they can only change when new ones arrive.
            if (newTransactions.Count > 0)
                await MarkRecurringAsync();

            logger.LogInformation("Sync complete: {Fetched} fetched, {New} new", transactions.Count, newTransactions.Count);
            return new SyncResult { Fetched = transactions.Count, NewRecords = newTransactions.Count };
        }
        catch (Exception ex)
        {
            return await FailSyncAsync(log, ex.Message, ex);
        }
    }

    public async Task<BalanceResult> GetBalanceAsync(FinTsConfig config)
    {
        try
        {
            var (bankConnection, connectError) = await ConnectAsync(config);
            if (connectError is not null)
                return FailBalance(connectError);

            var balanceResult = await bankConnection.Balance(CreateTanDialog());
            if (balanceResult.HasError)
                return FailBalance(DescribeErrors(balanceResult));

            if (balanceResult.Data is not { Successful: true } balance)
                return FailBalance(balanceResult.Data?.Message is { Length: > 0 } message ? message : UnknownError);

            return new BalanceResult
            {
                Balance = balance.Balance,
                AvailableBalance = balance.AvailableBalance
            };
        }
        catch (Exception ex)
        {
            return FailBalance(ex.Message, ex);
        }
    }

    /// <summary>Opens a dialog with the bank to check the credentials. Stores nothing.</summary>
    public async Task<CredentialCheckResult> VerifyCredentialsAsync(FinTsConfig config)
    {
        try
        {
            var (_, connectError) = await ConnectAsync(config);
            if (connectError is null)
                return new CredentialCheckResult();

            logger.LogWarning("FinTS credential check rejected by bank: {Error}", connectError);
            return new CredentialCheckResult { Status = SyncStatus.Failed, Rejected = true, Error = connectError };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FinTS credential check failed: {Error}", ex.Message);
            return new CredentialCheckResult { Status = SyncStatus.Failed, Error = ex.Message };
        }
    }

    /// <summary>
    /// Creates a client and runs the FinTS synchronization. Returns the bank's error when it
    /// rejects the dialog; callers must stop then, since every further request with rejected
    /// credentials counts toward the bank's PIN lockout.
    /// </summary>
    private async Task<(FinTsClient Client, string? Error)> ConnectAsync(FinTsConfig config)
    {
        var client = CreateClient(config);
        var syncResult = await client.Synchronization();
        foreach (var msg in syncResult.Messages ?? [])
            logger.LogInformation("Sync message: {Code} | {Message}", msg.Code, msg.Message);

        return (client, syncResult.HasError ? DescribeErrors(syncResult) : null);
    }

    private FinTsClient CreateClient(FinTsConfig config)
    {
        if (!int.TryParse(config.BlZ, out var blz))
            throw new InvalidOperationException($"FinTs:BlZ is not a valid bank code: '{config.BlZ}'");

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
                logger.LogInformation("Push-TAN sent, waiting for approval in the banking app...");
                return Task.FromResult("");
            },
            approved =>
            {
                logger.LogInformation("Push-TAN approval: {Approved}", approved ? "granted" : "declined/failed");
                return Task.CompletedTask;
            });
        tanDialog.IsDecoupled = true;
        return tanDialog;
    }

    private async Task<SyncResult> FailSyncAsync(SyncLog log, string error, Exception? ex = null)
    {
        logger.LogError(ex, "FinTS sync failed: {Error}", error);

        // Drop half-applied changes (e.g. new transactions) so the failure itself can be saved.
        db.ChangeTracker.Clear();
        log.Status = SyncStatus.Failed;
        log.Error = error;
        log.CompletedAt = DateTime.UtcNow;
        db.SyncLogs.Update(log);
        await db.SaveChangesAsync();

        return new SyncResult { Status = SyncStatus.Failed, Error = error };
    }

    private BalanceResult FailBalance(string error, Exception? ex = null)
    {
        logger.LogError(ex, "FinTS balance request failed: {Error}", error);
        return new BalanceResult { Status = SyncStatus.Failed, Error = error };
    }

    private static string DescribeErrors(HBCIDialogResult result)
    {
        var errors = (result.Messages ?? [])
            .Where(m => m.IsError)
            .Select(m => $"{m.Code} {m.Message}")
            .ToList();
        return errors.Count > 0 ? string.Join(", ", errors) : UnknownError;
    }

    private static Transaction MapCamtTransaction(CamtTransaction raw, string hash)
    {
        bool isDebit = raw.Amount < 0;
        return new Transaction
        {
            Hash         = hash,
            BookingDate  = raw.InputDate == default ? DateTime.UtcNow : raw.InputDate,
            ValueDate    = raw.ValueDate == default ? raw.InputDate : raw.ValueDate,
            Amount       = raw.Amount,
            Purpose      = raw.Description ?? raw.Text ?? "",
            CreditorName = isDebit ? raw.PartnerName ?? "" : "",
            CreditorIban = isDebit ? raw.AccountCode ?? "" : "",
            DebtorName   = isDebit ? "" : raw.PartnerName ?? "",
            DebtorIban   = isDebit ? "" : raw.AccountCode ?? "",
        };
    }

    private static string ComputeHashCamt(CamtTransaction t)
    {
        var raw = $"{t.InputDate:yyyyMMdd}|{t.Amount.ToString(CultureInfo.InvariantCulture)}|{t.Description ?? t.Text}|{t.AccountCode}|{t.EndToEndId}";
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
