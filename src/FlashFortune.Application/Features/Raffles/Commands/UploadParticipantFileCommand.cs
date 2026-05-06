using FlashFortune.Application.Common;
using FlashFortune.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FlashFortune.Application.Features.Raffles.Commands;

public record UploadParticipantFileCommand(
    Guid RaffleId,
    Stream FileContent,
    string FileName) : IRequest<Result<string>>;

public sealed class UploadParticipantFileCommandHandler(
    IApplicationDbContext db,
    IFileStorageService storage)
    : IRequestHandler<UploadParticipantFileCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UploadParticipantFileCommand request, CancellationToken cancellationToken)
    {
        var raffle = await db.Raffles.FirstOrDefaultAsync(r => r.Id == request.RaffleId, cancellationToken);
        if (raffle is null) return Result<string>.Failure("Raffle not found.");

        var fileKey = await storage.UploadAsync(
            request.FileContent,
            request.FileName,
            "text/csv",
            cancellationToken);

        // Background job enqueued via ConfirmUploadCommand after user reviews validation
        return Result<string>.Success(fileKey);
    }
}
