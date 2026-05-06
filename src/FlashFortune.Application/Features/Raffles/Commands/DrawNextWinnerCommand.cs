using FlashFortune.Application.Common;
using FlashFortune.Application.Interfaces;
using FlashFortune.Domain.Entities;
using FlashFortune.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FlashFortune.Application.Features.Raffles.Commands;

public record DrawNextWinnerCommand(Guid RaffleId, Guid PrizeId) : IRequest<Result<DrawResult>>;

public record DrawResult(
    Guid WinnerId,
    string FullName,
    string IdentityDocPartial,
    string AccountNumber,
    long CouponNumber,
    string ResultHash);

public sealed class DrawNextWinnerCommandHandler(
    IApplicationDbContext db,
    ICacheService cache,
    IPermutationAlgorithm permutation)
    : IRequestHandler<DrawNextWinnerCommand, Result<DrawResult>>
{
    private const int MaxReRolls = 1000;

    public async Task<Result<DrawResult>> Handle(DrawNextWinnerCommand request, CancellationToken cancellationToken)
    {
        var raffle = await db.Raffles
            .Include(r => r.Participants)
            .Include(r => r.Winners)
            .FirstOrDefaultAsync(r => r.Id == request.RaffleId, cancellationToken);

        if (raffle is null) return Result<DrawResult>.Failure("Raffle not found.");
        if (raffle.Seed is null) return Result<DrawResult>.Failure("Raffle has no seed — not locked.");

        Participant? winner = null;
        long couponNumber = 0;
        int attempts = 0;

        var rng = System.Security.Cryptography.RandomNumberGenerator.Create();

        while (attempts < MaxReRolls)
        {
            var position = NextSecurePosition(rng, raffle.TotalCoupons);
            couponNumber = permutation.Map(position, raffle.TotalCoupons, raffle.Seed.Combined);

            var candidate = raffle.Participants
                .FirstOrDefault(p => p.CouponRange.Contains(position));

            if (candidate is null) { attempts++; continue; }

            var excluded = await cache.IsExcludedAsync(raffle.Id, candidate.IdentityDoc, cancellationToken);
            if (excluded) { attempts++; continue; }

            winner = candidate;
            break;
        }

        if (winner is null)
            return Result<DrawResult>.Failure("Could not find a valid winner after maximum re-rolls.");

        var resultHash = ComputeHash(raffle.Id, request.PrizeId, winner.Id, couponNumber);
        var winnerEntity = Winner.Create(raffle.Id, request.PrizeId, winner, couponNumber, resultHash);

        raffle.RecordWinner(winnerEntity);
        await cache.AddToExclusionSetAsync(raffle.Id, winner.IdentityDoc, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return Result<DrawResult>.Success(new DrawResult(
            winnerEntity.Id,
            winner.FullName,
            ObfuscateId(winner.IdentityDoc),
            winner.AccountNumber,
            couponNumber,
            resultHash));
    }

    private static long NextSecurePosition(System.Security.Cryptography.RandomNumberGenerator rng, long max)
    {
        var bytes = new byte[8];
        rng.GetBytes(bytes);
        return (long)(Math.Abs(BitConverter.ToInt64(bytes, 0)) % max);
    }

    private static string ObfuscateId(string id) =>
        id.Length <= 4 ? "***" : id[..3] + new string('x', id.Length - 6) + id[^3..];

    private static string ComputeHash(Guid raffleId, Guid prizeId, Guid participantId, long coupon)
    {
        var input = $"{raffleId}:{prizeId}:{participantId}:{coupon}:{DateTimeOffset.UtcNow.Ticks}";
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16];
    }
}
