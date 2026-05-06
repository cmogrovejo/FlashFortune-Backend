namespace FlashFortune.Domain.Entities;

public class Winner
{
    public Guid Id { get; private set; }
    public Guid RaffleId { get; private set; }
    public Guid PrizeId { get; private set; }
    public Guid ParticipantId { get; private set; }
    public long CouponNumber { get; private set; }
    public DateTimeOffset DrawnAt { get; private set; }
    public string ResultHash { get; private set; } = string.Empty;

    public Participant Participant { get; private set; } = null!;
    public Prize Prize { get; private set; } = null!;

    private Winner() { }

    public static Winner Create(Guid raffleId, Guid prizeId, Participant participant, long couponNumber, string resultHash)
    {
        return new Winner
        {
            Id = Guid.NewGuid(),
            RaffleId = raffleId,
            PrizeId = prizeId,
            ParticipantId = participant.Id,
            Participant = participant,
            CouponNumber = couponNumber,
            DrawnAt = DateTimeOffset.UtcNow,
            ResultHash = resultHash
        };
    }
}
