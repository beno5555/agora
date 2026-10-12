using colosseumbackend.Common;

namespace colosseumbackend.Dtos;

public class MinimalListingDto
{
    public Guid           Id         { get; set; }
    public string         Title      { get; set; } = string.Empty;
    public string         GameSlug   { get; set; } = string.Empty;
    public string         GameName   { get; set; } = string.Empty;
    public ListingType    Type       { get; set; }
    public string         Price      { get; set; } = "0";
    public int            Milestones { get; set; }
    public string         UnitLabel  { get; set; } = string.Empty;
    public MinimalUserDto Seller     { get; set; } = new();
}
