using FlashFortune.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace FlashFortune.Infrastructure.Engine;

/// <summary>
/// Format-Preserving Encryption using a Feistel network with cycle-walking.
///
/// The Feistel network is a bijection on [0, 2^(2b) - 1] where b = ceil(log2(N)/2).
/// Cycle-walking restricts it to [0, N-1]: if the output >= N, re-apply until in range.
/// Since f is a bijection on the full range, cycle-walking produces a bijection on [0, N-1].
///
/// Forward:  if f(x) >= N, compute f(f(x)), f(f(f(x))), ... until < N.
/// Inverse:  same cycle-walk using f_inv.
/// </summary>
public sealed class FeistelPermutation : IPermutationAlgorithm
{
    private const int Rounds = 8;

    public long Map(long index, long universeSize, string seed)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(universeSize, 0);

        int halfBits = HalfBits(universeSize);

        long current = index;
        do
        {
            current = Encrypt(current, halfBits, seed);
        }
        while (current >= universeSize);

        return current + 1; // 1-indexed
    }

    public long Reverse(long couponNumber, long universeSize, string seed)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(couponNumber, 0);

        int halfBits = HalfBits(universeSize);

        long current = couponNumber - 1; // back to 0-indexed
        do
        {
            current = Decrypt(current, halfBits, seed);
        }
        while (current >= universeSize);

        return current;
    }

    private long Encrypt(long value, int halfBits, string seed)
    {
        long halfMask = (1L << halfBits) - 1;
        long left = value >> halfBits;
        long right = value & halfMask;

        for (int round = 0; round < Rounds; round++)
        {
            long temp = left ^ RoundFunction(right, round, seed, halfBits);
            left = right;
            right = temp;
        }

        return (left << halfBits) | right;
    }

    private long Decrypt(long value, int halfBits, string seed)
    {
        long halfMask = (1L << halfBits) - 1;
        long left = value >> halfBits;
        long right = value & halfMask;

        for (int round = Rounds - 1; round >= 0; round--)
        {
            long temp = right ^ RoundFunction(left, round, seed, halfBits);
            right = left;
            left = temp;
        }

        return (left << halfBits) | right;
    }

    private static int HalfBits(long universeSize)
    {
        int totalBits = (int)Math.Ceiling(Math.Log2(universeSize));
        // Ensure even total bits for a balanced split
        if (totalBits % 2 != 0) totalBits++;
        return totalBits / 2;
    }

    private static long RoundFunction(long value, int round, string seed, int halfBits)
    {
        var input = Encoding.UTF8.GetBytes($"{value}:{round}:{seed}");
        var hash = SHA256.HashData(input);
        var result = Math.Abs(BitConverter.ToInt64(hash, 0));
        return result & ((1L << halfBits) - 1);
    }
}
