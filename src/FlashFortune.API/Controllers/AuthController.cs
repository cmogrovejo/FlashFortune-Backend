using FlashFortune.Application.Features.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlashFortune.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new LoginCommand(request.Email, request.Password, request.BusinessUnitId), ct);

        if (!result.IsSuccess) return Unauthorized(new { message = result.Error });
        return Ok(result.Value);
    }

    [HttpPost("recover-password")]
    public async Task<IActionResult> RecoverPassword([FromBody] RecoverPasswordRequest request, CancellationToken ct)
    {
        await mediator.Send(new RecoverPasswordCommand(request.Email), ct);
        return Ok(new { message = "If the email exists, a recovery link has been sent." });
    }
}

public record LoginRequest(string Email, string Password, Guid BusinessUnitId);
public record RecoverPasswordRequest(string Email);
