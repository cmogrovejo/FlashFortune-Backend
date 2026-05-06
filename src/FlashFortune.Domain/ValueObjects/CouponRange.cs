namespace FlashFortune.Domain.ValueObjects;

/// <summary>
/// Represents a participant's virtual coupon allocation.
/// No physical rows are stored per coupon — only this range.
/// The actual coupon numbers are computed on-the-fly via the permutation algorithm.
/// </summary>
public sealed record CouponRange(long VirtualStart, long Count)
{
    public long VirtualEnd => VirtualStart + Count - 1;

    public bool Contains(long virtualIndex) =>
        virtualIndex >= VirtualStart && virtualIndex <= VirtualEnd;
}
