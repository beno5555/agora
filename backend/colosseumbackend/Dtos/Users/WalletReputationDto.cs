namespace colosseumbackend.Dtos;

public class WalletReputationDto
{
    public ReputationDto AsSeller { get; set; } = new();
    public ReputationDto AsBuyer  { get; set; } = new();
}
