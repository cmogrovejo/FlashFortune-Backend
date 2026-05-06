using FlashFortune.Application.Common;
using FlashFortune.Application.Interfaces;
using FlashFortune.Domain.Entities;
using MediatR;

namespace FlashFortune.Application.Features.Raffles.Commands;

public record CreateRaffleCommand(
    Guid BusinessUnitId,
    string Name,
    string Description,
    decimal ConversionFactor) : IRequest<Result<Guid>>;

public sealed class CreateRaffleCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateRaffleCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateRaffleCommand request, CancellationToken cancellationToken)
    {
        var raffle = Raffle.Create(request.BusinessUnitId, request.Name, request.Description, request.ConversionFactor);
        db.Raffles.Add(raffle);
        await db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(raffle.Id);
    }
}
