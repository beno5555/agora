using System.ComponentModel.DataAnnotations;
using colosseumbackend.Common;

namespace colosseumbackend.Dtos;

public class DealQuery : PageQuery
{
    /// <summary>"buyer", "seller", or empty for both.</summary>
    [RegularExpression("^(buyer|seller)$")]
    public string? Role { get; set; }

    public DealStatus? Status { get; set; }
}
