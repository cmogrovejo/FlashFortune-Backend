using FlashFortune.Application.Features.Raffles.Commands;
using FlashFortune.Application.Features.Raffles.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using FlashFortune.API.Hubs;

namespace FlashFortune.API.Controllers;

[ApiController]
[Route("api/raffles")]
[Authorize]
public sealed class RafflesController(IMediator mediator, IHubContext<RaffleHub> hub) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid businessUnitId, [FromQuery] int page = 1, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetRafflesByUnitQuery(businessUnitId, page), ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Configurator,UnitAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateRaffleRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new CreateRaffleCommand(request.BusinessUnitId, request.Name, request.Description, request.ConversionFactor), ct);

        if (!result.IsSuccess) return BadRequest(new { message = result.Error });
        return CreatedAtAction(nameof(GetAll), new { id = result.Value }, result.Value);
    }

    [HttpPost("{raffleId}/upload")]
    [Authorize(Roles = "Configurator,UnitAdmin")]
    public async Task<IActionResult> Upload(Guid raffleId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        await using var stream = file.OpenReadStream();
        var result = await mediator.Send(new UploadParticipantFileCommand(raffleId, stream, file.FileName), ct);

        if (!result.IsSuccess) return BadRequest(new { message = result.Error });
        return Ok(new { fileKey = result.Value });
    }

    [HttpPost("{raffleId}/confirm-upload")]
    [Authorize(Roles = "Configurator,UnitAdmin")]
    public async Task<IActionResult> ConfirmUpload(Guid raffleId, [FromBody] ConfirmUploadRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new ConfirmUploadCommand(raffleId, request.FileKey), ct);
        if (!result.IsSuccess) return BadRequest(new { message = result.Error });
        return Accepted();
    }

    [HttpPost("{raffleId}/draw")]
    [Authorize(Roles = "Operator,UnitAdmin")]
    public async Task<IActionResult> Draw(Guid raffleId, [FromBody] DrawRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new DrawNextWinnerCommand(raffleId, request.PrizeId), ct);

        if (!result.IsSuccess) return BadRequest(new { message = result.Error });

        // Push winner to all clients watching this raffle
        await hub.Clients.Group($"raffle:{raffleId}")
            .SendAsync("WinnerFound", result.Value, ct);

        return Ok(result.Value);
    }

    [HttpPost("{raffleId}/panic-reset")]
    [Authorize(Roles = "Operator,UnitAdmin")]
    public async Task<IActionResult> PanicReset(Guid raffleId, [FromBody] DrawRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new PanicResetCommand(raffleId, request.PrizeId), ct);
        if (!result.IsSuccess) return BadRequest(new { message = result.Error });

        await hub.Clients.Group($"raffle:{raffleId}")
            .SendAsync("PanicReset", new { raffleId, request.PrizeId }, ct);

        return Ok();
    }
}

public record CreateRaffleRequest(Guid BusinessUnitId, string Name, string Description, decimal ConversionFactor);
public record ConfirmUploadRequest(string FileKey);
public record DrawRequest(Guid PrizeId);
