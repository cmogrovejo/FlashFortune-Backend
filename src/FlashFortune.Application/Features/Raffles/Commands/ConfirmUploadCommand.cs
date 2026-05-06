using FlashFortune.Application.Common;
using FlashFortune.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FlashFortune.Application.Features.Raffles.Commands;

/// <summary>
/// Triggers the point of no return: enqueues the Hangfire ingestion job,
/// which validates, generates virtual coupon ranges, and purges balances.
/// </summary>
public record ConfirmUploadCommand(Guid RaffleId, string FileKey) : IRequest<Result>;

public sealed class ConfirmUploadCommandHandler(
    IApplicationDbContext db,
    IBackgroundJobService jobs)
    : IRequestHandler<ConfirmUploadCommand, Result>
{
    public async Task<Result> Handle(ConfirmUploadCommand request, CancellationToken cancellationToken)
    {
        var raffle = await db.Raffles.FirstOrDefaultAsync(r => r.Id == request.RaffleId, cancellationToken);
        if (raffle is null) return Result.Failure("Raffle not found.");

        var jobId = jobs.EnqueueFileIngestion(request.RaffleId, request.FileKey);
        return Result.Success();
    }
}
