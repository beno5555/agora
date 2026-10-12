namespace colosseumbackend.Dtos;

public class StandardListingDto : MinimalListingDto
{
    public string             Description      { get; set; } = string.Empty;
    public string             DeliveryWindow   { get; set; } = string.Empty;
    public bool               IsActive         { get; set; }
    public DateTime           CreatedAt        { get; set; }
    public ReputationDto      SellerReputation { get; set; } = new();
    /// <summary>What each side would lock if the signed-in caller bought now; null when signed out or for the seller.</summary>
    public DepositPreviewDto? DepositPreview   { get; set; }
}
