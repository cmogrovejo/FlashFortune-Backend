using FlashFortune.Domain.Enums;

namespace FlashFortune.Domain.Entities;

public class UserUnitRole
{
    public Guid UserId { get; private set; }
    public Guid BusinessUnitId { get; private set; }
    public UserRole Role { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }

    public UserUnitRole(Guid userId, Guid businessUnitId, UserRole role)
    {
        UserId = userId;
        BusinessUnitId = businessUnitId;
        Role = role;
        AssignedAt = DateTimeOffset.UtcNow;
    }
}
