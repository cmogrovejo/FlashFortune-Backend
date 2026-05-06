using FlashFortune.Application.Interfaces;
using FlashFortune.Domain.Entities;
using FlashFortune.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace FlashFortune.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire background job. Processes the uploaded CSV file:
/// 1. Validates structure and data types.
/// 2. Calculates each account's coupon count (floor(balance / factor)).
/// 3. Assigns virtual coupon ranges (no DB rows per coupon).
/// 4. Purges balance data from all records (BR-06).
/// 5. Locks the raffle (sets status to Ready + stores AuditSeed).
/// </summary>
public sealed class FileIngestionJob(
    IApplicationDbContext db,
    IFileStorageService storage,
    ILogger<FileIngestionJob> logger)
{
    public async Task ExecuteAsync(Guid raffleId, string fileKey)
    {
        logger.LogInformation("[Ingestion] Starting for Raffle {RaffleId}, file {FileKey}", raffleId, fileKey);

        var raffle = await db.Raffles.FindAsync(raffleId);
        if (raffle is null)
        {
            logger.LogError("[Ingestion] Raffle {RaffleId} not found", raffleId);
            return;
        }

        // Download file from S3/MinIO
        var downloadUrl = await storage.GetDownloadUrlAsync(fileKey, TimeSpan.FromMinutes(5));
        using var http = new HttpClient();
        await using var stream = await http.GetStreamAsync(downloadUrl);
        using var reader = new StreamReader(stream);

        var fileHasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var participants = new List<Participant>();
        var seenAccounts = new HashSet<string>();
        long virtualStart = 0;
        long totalCoupons = 0;
        int lineNumber = 0;
        string? line;

        // Expected header: nombre_completo|doc_identidad|telefono|numero_cuenta|saldo_moneda
        var header = await reader.ReadLineAsync();
        logger.LogInformation("[Ingestion] Header: {Header}", header);

        while ((line = await reader.ReadLineAsync()) is not null)
        {
            lineNumber++;
            fileHasher.AppendData(System.Text.Encoding.UTF8.GetBytes(line));

            var parts = line.Split('|');
            if (parts.Length < 5)
            {
                logger.LogWarning("[Ingestion] Line {Line}: malformed ({Parts} fields)", lineNumber, parts.Length);
                continue;
            }

            var accountNumber = parts[3].Trim();
            if (!seenAccounts.Add(accountNumber))
            {
                logger.LogWarning("[Ingestion] Line {Line}: duplicate account {Account} — skipped", lineNumber, accountNumber);
                continue;
            }

            if (!decimal.TryParse(parts[4].Trim(), out var balance) || balance <= 0)
            {
                logger.LogWarning("[Ingestion] Line {Line}: invalid balance — skipped", lineNumber);
                continue;
            }

            var couponCount = (long)Math.Floor(balance / raffle.ConversionFactor);
            if (couponCount == 0) continue;

            var participant = Participant.Create(
                raffleId,
                parts[0].Trim(),
                parts[1].Trim(),
                parts[2].Trim(),
                accountNumber,
                new CouponRange(virtualStart, couponCount));

            participants.Add(participant);
            virtualStart += couponCount;
            totalCoupons += couponCount;
        }

        db.Participants.AddRange(participants);

        // Build audit seed: CSPRNG + file hash
        var fileHashBytes = fileHasher.GetHashAndReset();
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var seed = new AuditSeed(
            Convert.ToHexString(randomBytes),
            Convert.ToHexString(fileHashBytes));

        raffle.Lock(fileKey, seed, totalCoupons);
        await db.SaveChangesAsync();

        logger.LogInformation("[Ingestion] Done. {Participants} participants, {Coupons} total coupons. Seed: {Seed}",
            participants.Count, totalCoupons, seed.Combined);
    }
}
