namespace colosseumbackend.Dtos;

public class DealTransactionDto
{
    public FullDealDto Deal        { get; set; } = new();
    /// <summary>Unsigned transaction, base64; the caller's wallet signs and sends it.</summary>
    public string      Transaction { get; set; } = string.Empty;
}
