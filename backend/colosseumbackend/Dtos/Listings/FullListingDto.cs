namespace colosseumbackend.Dtos;

/// <summary>The seller's own view of a listing, paused or not: everything the edit form needs. No chain reads.</summary>
public class FullListingDto : MinimalListingDto
{
    public int      GameId         { get; set; }
    public string   Description    { get; set; } = string.Empty;
    public string   DeliveryWindow { get; set; } = string.Empty;
    public bool     IsActive       { get; set; }
    public DateTime CreatedAt      { get; set; }
}
