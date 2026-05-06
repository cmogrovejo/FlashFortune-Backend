using FlashFortune.Domain.Entities;
using FluentAssertions;

namespace FlashFortune.Domain.Tests.Entities;

public sealed class BusinessUnitTests
{
    [Fact]
    public void Create_NewBusinessUnit_IsDeletedDefaultsFalse()
    {
        var unit = BusinessUnit.Create("Test Unit", "CORP-001", "School", "123 Main St");

        unit.IsDeleted.Should().BeFalse("a newly created BusinessUnit must not be soft-deleted");
    }

    [Fact]
    public void Create_NewBusinessUnit_DeletedAtDefaultsNull()
    {
        var unit = BusinessUnit.Create("Test Unit", "CORP-001", "School", "123 Main St");

        unit.DeletedAt.Should().BeNull("a newly created BusinessUnit must have no deletion timestamp");
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedTrue()
    {
        var unit = BusinessUnit.Create("Test Unit", "CORP-001", "School", "123 Main St");

        unit.SoftDelete();

        unit.IsDeleted.Should().BeTrue("SoftDelete() must mark the entity as deleted");
    }

    [Fact]
    public void SoftDelete_SetsDeletedAtToUtcNow()
    {
        var before = DateTimeOffset.UtcNow;
        var unit = BusinessUnit.Create("Test Unit", "CORP-001", "School", "123 Main St");

        unit.SoftDelete();

        unit.DeletedAt.Should().NotBeNull();
        unit.DeletedAt!.Value.Should().BeOnOrAfter(before);
        unit.DeletedAt!.Value.Should().BeOnOrBefore(DateTimeOffset.UtcNow.AddSeconds(1));
    }
}
