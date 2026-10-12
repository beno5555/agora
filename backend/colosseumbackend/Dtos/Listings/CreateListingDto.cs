using System.ComponentModel.DataAnnotations;
using colosseumbackend.Common;

namespace colosseumbackend.Dtos;

public class CreateListingDto
{
    [Range(1, int.MaxValue)]
    public int GameId { get; set; }

    public ListingType Type { get; set; }

    [Required, MaxLength(Constants.TitleMaxLength)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(Constants.DescriptionMaxLength)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Total price in token base units.</summary>
    [Required, RegularExpression(AmountFormat.Pattern)]
    public string Price { get; set; } = string.Empty;

    [Range(1, Constants.MaxMilestones)]
    public int Milestones { get; set; } = 1;

    [Required, MaxLength(Constants.UnitLabelMaxLength)]
    public string UnitLabel { get; set; } = string.Empty;

    /// <summary>One of the keys in <see cref="ConfigDto.DeliveryWindows"/>, e.g. "24h".</summary>
    public string DeliveryWindow { get; set; } = Constants.DefaultDeliveryWindow;
}
