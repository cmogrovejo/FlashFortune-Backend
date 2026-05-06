using FlashFortune.Application.Interfaces;
using Hangfire;

namespace FlashFortune.Infrastructure.BackgroundJobs;

public sealed class HangfireJobService(IBackgroundJobClient hangfire) : IBackgroundJobService
{
    public string EnqueueFileIngestion(Guid raffleId, string fileKey)
    {
        return hangfire.Enqueue<FileIngestionJob>(job => job.ExecuteAsync(raffleId, fileKey));
    }
}
