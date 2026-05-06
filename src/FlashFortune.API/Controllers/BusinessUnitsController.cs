using FlashFortune.Application.Features.BusinessUnits.Commands;
using FlashFortune.Application.Features.BusinessUnits.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FlashFortune.API.Controllers;

[ApiController]
[Route("api/business-units")]
[Authorize]
public sealed class BusinessUnitsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await mediator.Send(new GetBusinessUnitsQuery(userId), ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateBusinessUnitRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await mediator.Send(
            new CreateBusinessUnitCommand(request.Name, request.CorporateId, request.InstitutionType, request.Address, userId), ct);

        if (!result.IsSuccess) return BadRequest(new { message = result.Error });
        return CreatedAtAction(nameof(GetAll), new { id = result.Value }, result.Value);
    }
}

public record CreateBusinessUnitRequest(string Name, string CorporateId, string InstitutionType, string Address);
