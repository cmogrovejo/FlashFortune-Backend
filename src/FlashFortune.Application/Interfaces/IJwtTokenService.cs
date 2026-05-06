using FlashFortune.Domain.Entities;
using FlashFortune.Domain.Enums;

namespace FlashFortune.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user, Guid businessUnitId, UserRole role);
}
