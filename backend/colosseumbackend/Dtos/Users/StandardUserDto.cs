namespace colosseumbackend.Dtos;

public class StandardUserDto
{
    public string                  Wallet         { get; set; } = string.Empty;
    public string?                 DisplayName    { get; set; }
    public string?                 Discord        { get; set; }
    public WalletReputationDto     Reputation     { get; set; } = new();
    public List<MinimalListingDto> ActiveListings { get; set; } = [];
}
