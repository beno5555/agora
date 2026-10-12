namespace colosseumbackend.Dtos;

public class GameDto
{
    public int     Id             { get; set; }
    public string  Slug           { get; set; } = string.Empty;
    public string  Name           { get; set; } = string.Empty;
    public string? IconUrl        { get; set; }
    public int     ActiveListings { get; set; }
}
