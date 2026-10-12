namespace colosseumbackend.Dtos;

public class FullUserDto
{
    public string   Wallet      { get; set; } = string.Empty;
    public string?  DisplayName { get; set; }
    public string?  Discord     { get; set; }
    public DateTime CreatedAt   { get; set; }
}
