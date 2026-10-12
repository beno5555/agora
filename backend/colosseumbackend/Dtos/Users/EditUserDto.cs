using System.ComponentModel.DataAnnotations;
using colosseumbackend.Common;

namespace colosseumbackend.Dtos;

public class EditUserDto
{
    [MaxLength(Constants.DisplayNameMaxLength)]
    public string? DisplayName { get; set; }

    [MaxLength(Constants.DiscordMaxLength)]
    public string? Discord { get; set; }
}
