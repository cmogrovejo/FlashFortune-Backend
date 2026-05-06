using FlashFortune.Application.Common;
using FlashFortune.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FlashFortune.Application.Features.Auth.Commands;

public record RecoverPasswordCommand(string Email) : IRequest<Result>;

public sealed class RecoverPasswordCommandHandler(
    IApplicationDbContext db,
    IEmailService email)
    : IRequestHandler<RecoverPasswordCommand, Result>
{
    public async Task<Result> Handle(RecoverPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        // Always return success to prevent email enumeration
        if (user is null)
            return Result.Success();

        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        await email.SendPasswordRecoveryAsync(user.Email, token, cancellationToken);

        return Result.Success();
    }
}
