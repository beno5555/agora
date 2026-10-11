namespace colosseumbackend;

public class Wallet
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string OwnerName { get; set; } = string.Empty;

    public decimal Balance { get; set; }

    public string Currency { get; set; } = "USD";

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
