using FlashFortune.Application.Interfaces;
using FlashFortune.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FlashFortune.Application.Features.Raffles.Queries;

public record GetRafflesByUnitQuery(Guid BusinessUnitId, int Page = 1, int PageSize = 20)
    : IRequest<PaginatedResult<RaffleSummaryDto>>;

public record RaffleSummaryDto(
    Guid Id,
    string Name,
    RaffleStatus Status,
    int PrizeCount,
    long TotalCoupons,
    DateTimeOffset CreatedAt);

public record PaginatedResult<T>(List<T> Data, int Page, int PageSize, int TotalItems);

public sealed class GetRafflesByUnitQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetRafflesByUnitQuery, PaginatedResult<RaffleSummaryDto>>
{
    public async Task<PaginatedResult<RaffleSummaryDto>> Handle(
        GetRafflesByUnitQuery request,
        CancellationToken cancellationToken)
    {
        var query = db.Raffles
            .Where(r => r.BusinessUnitId == request.BusinessUnitId);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new RaffleSummaryDto(
                r.Id,
                r.Name,
                r.Status,
                r.Prizes.Count,
                r.TotalCoupons,
                r.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<RaffleSummaryDto>(items, request.Page, request.PageSize, total);
    }
}
