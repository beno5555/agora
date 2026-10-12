namespace colosseumbackend.Data.Entities;

public class Game
{
    public int     Id      { get; set; }
    public string  Slug    { get; set; } = string.Empty;
    public string  Name    { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
}
