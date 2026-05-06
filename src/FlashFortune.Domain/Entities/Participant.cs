using FlashFortune.Domain.ValueObjects;

namespace FlashFortune.Domain.Entities;

/// <summary>
/// Represents one account holder participating in a raffle.
/// Balance is NOT stored here — it is used only to compute CouponRange and then discarded (BR-06).
/// PII fields (IdentityDoc, Phone, Name) are encrypted at rest in the DB.
/// </summary>
public class Participant
{
    public Guid Id { get; private set; }
    public Guid RaffleId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string IdentityDoc { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string AccountNumber { get; private set; } = string.Empty;
    public CouponRange CouponRange { get; private set; } = null!;

    private Participant() { }

    public static Participant Create(
        Guid raffleId,
        string fullName,
        string identityDoc,
        string phone,
        string accountNumber,
        CouponRange couponRange)
    {
        return new Participant
        {
            Id = Guid.NewGuid(),
            RaffleId = raffleId,
            FullName = fullName.Trim(),
            IdentityDoc = identityDoc.Trim(),
            Phone = phone.Trim(),
            AccountNumber = accountNumber.Trim(),
            CouponRange = couponRange
        };
    }
}
