using FlashFortune.Domain.Entities;
using FlashFortune.Domain.Enums;
using FlashFortune.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FlashFortune.Infrastructure.Tests.Persistence;

public sealed class UserUnitRoleConfigurationTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        var context = new AppDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public void UserUnitRole_HasCompositeKey_NoShadowKeyGenerated()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(UserUnitRole))!;
        var primaryKey = entityType.FindPrimaryKey()!;

        // Composite key must have exactly 3 columns: UserId, BusinessUnitId, Role
        primaryKey.Properties.Should().HaveCount(3);

        var keyPropertyNames = primaryKey.Properties.Select(p => p.Name).ToList();
        keyPropertyNames.Should().Contain("UserId");
        keyPropertyNames.Should().Contain("BusinessUnitId");
        keyPropertyNames.Should().Contain("Role");
    }

    [Fact]
    public void UserUnitRole_CompositeKey_DoesNotIncludeShadowProperty()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(UserUnitRole))!;
        var primaryKey = entityType.FindPrimaryKey()!;

        // No shadow properties in the key (shadow properties are not CLR-backed)
        var shadowProperties = primaryKey.Properties
            .Where(p => p.IsShadowProperty())
            .ToList();

        shadowProperties.Should().BeEmpty("the composite key must only contain CLR-backed properties");
    }

    [Fact]
    public void UserUnitRole_CanPersistAndRetrieve_WithCompositeKey()
    {
        using var context = CreateContext();

        var userId = Guid.NewGuid();
        var businessUnitId = Guid.NewGuid();

        // Seed required navigation entities
        var user = User.Create("test@example.com", "hash");
        var unit = BusinessUnit.Create("Test Unit", "CORP-001", "School", "123 Main St");

        // Use reflection to set fixed IDs for test predictability
        SetPrivateField(user, "Id", userId);
        SetPrivateField(unit, "Id", businessUnitId);

        context.Users.Add(user);
        context.BusinessUnits.Add(unit);

        var role = new UserUnitRole(userId, businessUnitId, UserRole.Operator);
        context.UserUnitRoles.Add(role);
        context.SaveChanges();

        context.ChangeTracker.Clear();

        var found = context.UserUnitRoles
            .FirstOrDefault(r => r.UserId == userId
                              && r.BusinessUnitId == businessUnitId
                              && r.Role == UserRole.Operator);

        found.Should().NotBeNull("the record should be retrievable by its composite key");
    }

    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var prop = obj.GetType().GetProperty(fieldName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (prop is not null)
        {
            // Use backing field setter for private set
            var field = obj.GetType().GetFields(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .FirstOrDefault(f => f.Name.Contains(fieldName, StringComparison.OrdinalIgnoreCase)
                                  && !f.Name.StartsWith("_"));
        }

        // Try direct property set via reflection on private setter
        var setter = obj.GetType().GetProperty(fieldName)?.GetSetMethod(nonPublic: true);
        setter?.Invoke(obj, [value]);
    }
}
