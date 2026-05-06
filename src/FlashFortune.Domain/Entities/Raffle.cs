using FlashFortune.Domain.Enums;
using FlashFortune.Domain.Exceptions;
using FlashFortune.Domain.ValueObjects;

namespace FlashFortune.Domain.Entities;

public class Raffle
{
    public Guid Id { get; private set; }
    public Guid BusinessUnitId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal ConversionFactor { get; private set; }
    public RaffleStatus Status { get; private set; }
    public long TotalCoupons { get; private set; }
    public string? SourceFileKey { get; private set; }
    public AuditSeed? Seed { get; private set; }
    public DateTimeOffset? LockedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<Prize> Prizes => _prizes.AsReadOnly();
    private readonly List<Prize> _prizes = [];

    public IReadOnlyCollection<Participant> Participants => _participants.AsReadOnly();
    private readonly List<Participant> _participants = [];

    public IReadOnlyCollection<Winner> Winners => _winners.AsReadOnly();
    private readonly List<Winner> _winners = [];

    private Raffle() { }

    public static Raffle Create(Guid businessUnitId, string name, string description, decimal conversionFactor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (conversionFactor <= 0)
            throw new DomainException("Conversion factor must be greater than zero.");

        return new Raffle
        {
            Id = Guid.NewGuid(),
            BusinessUnitId = businessUnitId,
            Name = name.Trim(),
            Description = description.Trim(),
            ConversionFactor = conversionFactor,
            Status = RaffleStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Lock(string fileKey, AuditSeed seed, long totalCoupons)
    {
        EnsureNotLocked();
        SourceFileKey = fileKey;
        Seed = seed;
        TotalCoupons = totalCoupons;
        Status = RaffleStatus.Ready;
        LockedAt = DateTimeOffset.UtcNow;
    }

    public void StartDraw()
    {
        if (Status != RaffleStatus.Ready)
            throw new DomainException("Raffle must be in Ready status to start a draw.");

        Status = RaffleStatus.Live;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void End()
    {
        if (Status != RaffleStatus.Live)
            throw new DomainException("Cannot end a raffle that is not Live.");

        Status = RaffleStatus.Ended;
        EndedAt = DateTimeOffset.UtcNow;
    }

    public void AddPrize(Prize prize)
    {
        EnsureNotLocked();
        _prizes.Add(prize);
    }

    public void RecordWinner(Winner winner) => _winners.Add(winner);

    public bool IsParticipantAlreadyWinner(string identityDoc) =>
        _winners.Any(w => w.Participant.IdentityDoc == identityDoc);

    private void EnsureNotLocked()
    {
        if (Status != RaffleStatus.Draft)
            throw new RaffleLockedException(Id);
    }
}
