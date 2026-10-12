using colosseumbackend.Common;

namespace colosseumbackend.Data.Entities;

public class Listing
{
    public Guid         Id                    { get; set; } = Guid.NewGuid();
    public string       SellerWallet          { get; set; } = string.Empty;
    public int          GameId                { get; set; }
    public ListingType  Type                  { get; set; }
    public string       Title                 { get; set; } = string.Empty;
    public string       Description           { get; set; } = string.Empty;
    /// <summary>Total price in token base units.</summary>
    public long         Price                 { get; set; }
    public int          Milestones            { get; set; }
    public ListingUnit? Unit                  { get; set; }
    public long         DeliveryWindowSeconds { get; set; }
    public bool         IsActive              { get; set; } = true;
    public DateTime     CreatedAt             { get; set; } = DateTime.UtcNow;

    public User? Seller { get; set; }
    public Game? Game   { get; set; }
}
