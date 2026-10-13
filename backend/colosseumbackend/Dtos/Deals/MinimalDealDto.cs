using colosseumbackend.Common;

namespace colosseumbackend.Dtos;

public class MinimalDealDto
{
    public Guid       Id        { get; set; }
    public string     Title     { get; set; } = string.Empty;
    public DealStatus Status    { get; set; }
    public string     Price     { get; set; } = "0";
    public string     Role      { get; set; } = string.Empty;
    public DateTime   CreatedAt { get; set; }
    /// <summary>What the caller can do right now (DealRules); lets a list show whose move it is.</summary>
    public List<DealAction> AllowedActions { get; set; } = [];
}
