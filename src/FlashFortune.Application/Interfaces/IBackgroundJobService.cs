namespace FlashFortune.Application.Interfaces;

public interface IBackgroundJobService
{
    /// <summary>Enqueues file ingestion: validate, generate virtual ranges, purge balances.</summary>
    string EnqueueFileIngestion(Guid raffleId, string fileKey);
}
