namespace colosseumbackend.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer   { get; set; } = "agora";
    public string Audience { get; set; } = "agora";
    /// <summary>Base64, at least 32 bytes. Set with `dotnet user-secrets`; never commit it.</summary>
    public string SigningKey { get; set; } = string.Empty;
}
