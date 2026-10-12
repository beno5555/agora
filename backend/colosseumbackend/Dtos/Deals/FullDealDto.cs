using colosseumbackend.Common;

namespace colosseumbackend.Dtos;

public class FullDealDto : MinimalDealDto
{
    public string             DealPda            { get; set; } = string.Empty;
    public string             OnchainDealId      { get; set; } = string.Empty;
    public Guid               ListingId          { get; set; }
    public string             GameName           { get; set; } = string.Empty;
    public ListingUnit?       Unit               { get; set; }
    public string             BuyerWallet        { get; set; } = string.Empty;
    public string             SellerWallet       { get; set; } = string.Empty;
    public string?            BuyerNote          { get; set; }
    public int                Milestones         { get; set; }
    public int                MilestonesReleased { get; set; }
    public string             BuyerDeposit       { get; set; } = "0";
    public string             SellerDeposit      { get; set; } = "0";
    public long               DeliveryWindowSeconds { get; set; }
    public DateTime?          AcceptedAt         { get; set; }
    public DateTime?          DeliveredAt        { get; set; }
    public DateTime?          DeliveryDeadline   { get; set; }
    public SplitOfferDto?     SplitOffer         { get; set; }
    public string?            LastSignature      { get; set; }
    public List<DealAction>   AllowedActions     { get; set; } = [];
}
