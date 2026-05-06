using FlashFortune.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FlashFortune.Application.Features.BusinessUnits.Queries;

public record GetBusinessUnitsQuery(Guid UserId) : IRequest<List<BusinessUnitDto>>;

public record BusinessUnitDto(Guid Id, string Name, string CorporateId, string InstitutionType);

public sealed class GetBusinessUnitsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetBusinessUnitsQuery, List<BusinessUnitDto>>
{
    public async Task<List<BusinessUnitDto>> Handle(GetBusinessUnitsQuery request, CancellationToken cancellationToken)
    {
        return await db.UserUnitRoles
            .Where(r => r.UserId == request.UserId)
            .Join(db.BusinessUnits,
                role => role.BusinessUnitId,
                bu => bu.Id,
                (role, bu) => new BusinessUnitDto(bu.Id, bu.Name, bu.CorporateId, bu.InstitutionType))
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
