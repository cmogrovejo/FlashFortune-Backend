using FlashFortune.Application.Common;
using FlashFortune.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FlashFortune.Application.Features.Raffles.Commands;

/// <summary>
/// Resets the current prize draw without touching previously awarded prizes.
/// Removes the last winner record and its exclusion from the cache.
/// </summary>
public record PanicResetCommand(Guid RaffleId, Guid PrizeId) : IRequest<Result>;

public sealed class PanicResetCommandHandler(IApplicationDbContext db, ICacheService cache)
    : IRequestHandler<PanicResetCommand, Result>
{
    public async Task<Result> Handle(PanicResetCommand request, CancellationToken cancellationToken)
    {
        var raffle = await db.Raffles
            .Include(r => r.Winners)
            .ThenInclude(w => w.Participant)
            .FirstOrDefaultAsync(r => r.Id == request.RaffleId, cancellationToken);

        if (raffle is null) return Result.Failure("Raffle not found.");

        var lastWinner = raffle.Winners
            .Where(w => w.PrizeId == request.PrizeId)
            .OrderByDescending(w => w.DrawnAt)
            .FirstOrDefault();

        if (lastWinner is null) return Result.Success();

        db.Winners.Remove(lastWinner);
        await cache.RemoveFromExclusionSetAsync(raffle.Id, lastWinner.Participant.IdentityDoc, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
