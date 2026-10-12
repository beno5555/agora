namespace colosseumbackend.Configuration;

public class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Shown in the sign-in message; should be the site's host.</summary>
    public string   Domain          { get; set; } = "localhost";
    public TimeSpan NonceLifetime   { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan SessionLifetime { get; set; } = TimeSpan.FromDays(7);
}
