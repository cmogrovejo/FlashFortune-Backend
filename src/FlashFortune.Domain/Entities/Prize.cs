namespace FlashFortune.Domain.Entities;

public class Prize
{
    public Guid Id { get; private set; }
    public Guid RaffleId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public int Order { get; private set; }

    private Prize() { }

    public static Prize Create(Guid raffleId, string name, string description, int quantity, int order)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));

        return new Prize
        {
            Id = Guid.NewGuid(),
            RaffleId = raffleId,
            Name = name.Trim(),
            Description = description.Trim(),
            Quantity = quantity,
            Order = order
        };
    }

    public void UpdateOrder(int newOrder) => Order = newOrder;
}
