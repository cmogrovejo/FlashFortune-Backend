namespace FlashFortune.Domain.Entities;

public class BusinessUnit
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string CorporateId { get; private set; } = string.Empty;
    public string InstitutionType { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public IReadOnlyCollection<Raffle> Raffles => _raffles.AsReadOnly();
    private readonly List<Raffle> _raffles = [];

    private BusinessUnit() { }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
    }

    public static BusinessUnit Create(string name, string corporateId, string institutionType, string address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(corporateId);

        return new BusinessUnit
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            CorporateId = corporateId.Trim(),
            InstitutionType = institutionType.Trim(),
            Address = address.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
