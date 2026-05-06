using FlashFortune.Domain.Enums;
using FlashFortune.Domain.Exceptions;

namespace FlashFortune.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public int FailedLoginAttempts { get; private set; }
    public DateTimeOffset? LockedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<UserUnitRole> UnitRoles => _unitRoles.AsReadOnly();
    private readonly List<UserUnitRole> _unitRoles = [];

    private const int MaxFailedAttempts = 5;

    private User() { }

    public static User Create(string email, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant().Trim(),
            PasswordHash = passwordHash,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= MaxFailedAttempts)
            LockedAt = DateTimeOffset.UtcNow;
    }

    public void ResetLoginAttempts()
    {
        FailedLoginAttempts = 0;
        LockedAt = null;
    }

    public bool IsLocked => LockedAt.HasValue;

    public void AssignRole(Guid businessUnitId, UserRole role)
    {
        if (_unitRoles.Any(r => r.BusinessUnitId == businessUnitId && r.Role == role))
            return;

        _unitRoles.Add(new UserUnitRole(Id, businessUnitId, role));
    }
}
