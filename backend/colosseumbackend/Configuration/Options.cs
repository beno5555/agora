namespace colosseumbackend.Configuration;

public class SolanaOptions
{
    public const string SectionName = "Solana";

    public string Cluster      { get; set; } = "devnet";
    public string RpcUrl       { get; set; } = "https://api.devnet.solana.com";
    public string ProgramId    { get; set; } = "J4fL4Q8nnndro2xmVdCayRt5apXhexkkVQoqqwf4AKi4";
    /// <summary>Devnet USDC; must match the program's Config.mint.</summary>
    public string Mint         { get; set; } = "4zMMC9srt5Ri5X14GAgXhaHii3GnPAEERYPJgZJDncDU";
    public int    MintDecimals { get; set; } = 6;

    /// <summary>How long sync waits for a submitted transaction to confirm.</summary>
    public TimeSpan ConfirmationTimeout      { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan ConfirmationPollInterval { get; set; } = TimeSpan.FromSeconds(1);
}

public class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Shown in the sign-in message; should be the site's host.</summary>
    public string   Domain          { get; set; } = "localhost";
    public TimeSpan NonceLifetime   { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan SessionLifetime { get; set; } = TimeSpan.FromDays(7);
}

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer   { get; set; } = "agora";
    public string Audience { get; set; } = "agora";
    /// <summary>Base64, at least 32 bytes. Set with `dotnet user-secrets`; never commit it.</summary>
    public string SigningKey { get; set; } = string.Empty;
}

public class DealSyncOptions
{
    public const string SectionName = "DealSync";

    public TimeSpan Interval  { get; set; } = TimeSpan.FromMinutes(1);
    /// <summary>getMultipleAccounts accepts at most 100 keys per call.</summary>
    public int      BatchSize { get; set; } = 100;
}
