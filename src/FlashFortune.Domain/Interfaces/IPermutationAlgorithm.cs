namespace FlashFortune.Domain.Interfaces;

/// <summary>
/// Maps a virtual index to a non-contiguous coupon number using
/// a deterministic Format-Preserving Encryption (Feistel cipher).
/// f(index, seed) -> couponNumber
/// Pure function — no side effects, no DB access. Fully unit-testable.
/// </summary>
public interface IPermutationAlgorithm
{
    /// <summary>
    /// Maps a 0-based virtual index to a coupon number in [1, universeSize].
    /// </summary>
    long Map(long index, long universeSize, string seed);

    /// <summary>
    /// Inverse: given a coupon number, returns its virtual index.
    /// </summary>
    long Reverse(long couponNumber, long universeSize, string seed);
}
