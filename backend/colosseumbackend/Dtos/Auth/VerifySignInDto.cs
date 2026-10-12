using System.ComponentModel.DataAnnotations;
using colosseumbackend.Common;

namespace colosseumbackend.Dtos;

public class VerifySignInDto
{
    [Required, MaxLength(Constants.WalletMaxLength)]
    public string Wallet { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>Base58 ed25519 signature of the UTF-8 message.</summary>
    [Required]
    public string Signature { get; set; } = string.Empty;
}
