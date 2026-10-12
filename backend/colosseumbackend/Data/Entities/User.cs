namespace colosseumbackend.Data.Entities;

public class User
{
    /// <summary>Base58 wallet address; the account identity everywhere.</summary>
    public string         Wallet      { get; set; } = string.Empty;
    public string?        DisplayName { get; set; }
    public string?        Discord     { get; set; }
    public DateTime       CreatedAt   { get; set; } = DateTime.UtcNow;
}
