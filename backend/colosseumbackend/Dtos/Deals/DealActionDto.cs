using System.ComponentModel.DataAnnotations;
using colosseumbackend.Common;

namespace colosseumbackend.Dtos;

public class DealActionDto
{
    public DealAction Action { get; set; }

    /// <summary>For Confirm: how many parts to release.</summary>
    [Range(1, Constants.MaxMilestones)]
    public int? Parts { get; set; }

    /// <summary>For ProposeSplit: what the buyer gets back, in base units.</summary>
    [RegularExpression(AmountFormat.Pattern)]
    public string? BuyerAmount { get; set; }
}
