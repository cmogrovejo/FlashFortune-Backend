using FlashFortune.Application.Common;
using FlashFortune.Application.Interfaces;
using FlashFortune.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FlashFortune.Application.Features.Auth.Commands;

public record LoginCommand(string Email, string Password, Guid BusinessUnitId) : IRequest<Result<LoginResult>>;

public record LoginResult(string Token, Guid UserId, UserRole Role);

public sealed class LoginCommandHandler(
    IApplicationDbContext db,
    IJwtTokenService jwt,
    IPasswordHasher hasher)
    : IRequestHandler<LoginCommand, Result<LoginResult>>
{
    public async Task<Result<LoginResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(u => u.UnitRoles)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        if (user is null)
            return Result<LoginResult>.Failure("Invalid credentials.");

        if (user.IsLocked)
            return Result<LoginResult>.Failure("Account is locked. Contact your administrator.");

        if (!hasher.Verify(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin();
            await db.SaveChangesAsync(cancellationToken);
            return Result<LoginResult>.Failure("Invalid credentials.");
        }

        var unitRole = user.UnitRoles.FirstOrDefault(r => r.BusinessUnitId == request.BusinessUnitId);
        if (unitRole is null)
            return Result<LoginResult>.Failure("You do not have access to this Business Unit.");

        user.ResetLoginAttempts();
        await db.SaveChangesAsync(cancellationToken);

        var token = jwt.GenerateToken(user, request.BusinessUnitId, unitRole.Role);
        return Result<LoginResult>.Success(new LoginResult(token, user.Id, unitRole.Role));
    }
}
