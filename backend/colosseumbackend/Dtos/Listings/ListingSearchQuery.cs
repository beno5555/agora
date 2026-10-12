using System.ComponentModel.DataAnnotations;
using colosseumbackend.Common;

namespace colosseumbackend.Dtos;

public class ListingSearchQuery : PageQuery
{
    [MaxLength(Constants.GameSlugMaxLength)]
    public string? Game { get; set; }

    public ListingType? Type { get; set; }

    [MaxLength(Constants.TitleMaxLength)]
    public string? Q { get; set; }
}
