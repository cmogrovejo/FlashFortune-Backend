namespace FlashFortune.Application.Interfaces;

public interface ICacheService
{
    /// <summary>Adds a winner's identity doc to the exclusion set for a raffle.</summary>
    Task AddToExclusionSetAsync(Guid raffleId, string identityDoc, CancellationToken ct = default);

    /// <summary>Returns true if the identity doc is already in the winner exclusion set.</summary>
    Task<bool> IsExcludedAsync(Guid raffleId, string identityDoc, CancellationToken ct = default);

    /// <summary>Removes a single identity doc from the exclusion set (used by panic reset).</summary>
    Task RemoveFromExclusionSetAsync(Guid raffleId, string identityDoc, CancellationToken ct = default);

    /// <summary>Clears the entire exclusion set for a raffle.</summary>
    Task ClearExclusionSetAsync(Guid raffleId, CancellationToken ct = default);
}
