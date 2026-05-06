using FlashFortune.Infrastructure.Engine;
using FluentAssertions;

namespace FlashFortune.Infrastructure.Tests.Engine;

public sealed class FeistelPermutationTests
{
    private readonly FeistelPermutation _sut = new();
    private const string Seed = "test-audit-seed-abc123";

    [Fact]
    public void Map_SameInputs_AlwaysReturnsSameOutput()
    {
        var first = _sut.Map(0, 1_000_000, Seed);
        var second = _sut.Map(0, 1_000_000, Seed);
        first.Should().Be(second);
    }

    [Fact]
    public void Map_OutputIsWithinUniverse()
    {
        const long universeSize = 1_000;
        for (long i = 0; i < 200; i++)
        {
            var coupon = _sut.Map(i, universeSize, Seed);
            coupon.Should().BeGreaterThanOrEqualTo(1);
            coupon.Should().BeLessThanOrEqualTo(universeSize);
        }
    }

    [Fact]
    public void Map_IsBijective_NoTwoDifferentIndexesMapToSameCoupon()
    {
        // Keep universe small — the bijection proof holds at any size; we test correctness not scale
        const long universeSize = 500;
        var results = new HashSet<long>();

        for (long i = 0; i < universeSize; i++)
        {
            var coupon = _sut.Map(i, universeSize, Seed);
            results.Add(coupon).Should().BeTrue($"index {i} produced duplicate coupon {coupon}");
        }

        results.Should().HaveCount((int)universeSize);
    }

    [Fact]
    public void Map_DifferentSeeds_ProduceDifferentDistributions()
    {
        const long universeSize = 1000;
        var seedA = Enumerable.Range(0, 100).Select(i => _sut.Map(i, universeSize, "seed-A")).ToList();
        var seedB = Enumerable.Range(0, 100).Select(i => _sut.Map(i, universeSize, "seed-B")).ToList();

        seedA.Should().NotBeEquivalentTo(seedB);
    }

    [Fact]
    public void Reverse_IsInverseOfMap()
    {
        const long universeSize = 500;
        for (long i = 0; i < 50; i++)
        {
            var coupon = _sut.Map(i, universeSize, Seed);
            var reversed = _sut.Reverse(coupon, universeSize, Seed);
            reversed.Should().Be(i, $"Reverse(Map({i})) should equal {i}");
        }
    }

    [Fact]
    public void Map_DistributionIsNotContiguous_ConsecutiveIndexesAreScattered()
    {
        const long universeSize = 10_000;
        var coupons = Enumerable.Range(0, 5)
            .Select(i => _sut.Map(i, universeSize, Seed))
            .OrderBy(x => x)
            .ToList();

        for (int i = 0; i < coupons.Count - 1; i++)
            (coupons[i + 1] - coupons[i]).Should().BeGreaterThan(1,
                "consecutive indexes should NOT produce adjacent coupon numbers");
    }
}
