using FlashFortune.Application.Common;
using FlashFortune.Application.Interfaces;
using FlashFortune.Domain.Entities;
using FlashFortune.Domain.Enums;
using MediatR;

namespace FlashFortune.Application.Features.BusinessUnits.Commands;

public record CreateBusinessUnitCommand(
    string Name,
    string CorporateId,
    string InstitutionType,
    string Address,
    Guid CreatedByUserId) : IRequest<Result<Guid>>;

public sealed class CreateBusinessUnitCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateBusinessUnitCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateBusinessUnitCommand request, CancellationToken cancellationToken)
    {
        var unit = BusinessUnit.Create(request.Name, request.CorporateId, request.InstitutionType, request.Address);
        db.BusinessUnits.Add(unit);

        var user = await db.Users.FindAsync([request.CreatedByUserId], cancellationToken);
        user?.AssignRole(unit.Id, UserRole.UnitAdmin);

        await db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(unit.Id);
    }
}
