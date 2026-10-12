using System.ComponentModel.DataAnnotations;
using colosseumbackend.Common;

namespace colosseumbackend.Dtos;

public class SyncDealDto
{
    [Required, MaxLength(Constants.SignatureMaxLength)]
    public string Signature { get; set; } = string.Empty;
}
