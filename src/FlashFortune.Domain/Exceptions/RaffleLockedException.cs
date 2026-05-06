namespace FlashFortune.Domain.Exceptions;

public class RaffleLockedException : DomainException
{
    public RaffleLockedException(Guid raffleId)
        : base($"Raffle '{raffleId}' is locked and cannot be modified.") { }
}
