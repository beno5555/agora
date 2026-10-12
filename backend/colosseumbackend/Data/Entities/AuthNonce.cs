namespace colosseumbackend.Data.Entities;

public class AuthNonce
{
    public string   Wallet    { get; set; } = string.Empty;
    public string   Message   { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
