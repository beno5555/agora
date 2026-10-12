using System.ComponentModel.DataAnnotations;
using colosseumbackend.Common;

namespace colosseumbackend.Dtos;

public class CreateDealDto
{
    public Guid ListingId { get; set; }

    [MaxLength(Constants.BuyerNoteMaxLength)]
    public string? BuyerNote { get; set; }
}
