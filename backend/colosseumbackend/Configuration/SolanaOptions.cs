namespace colosseumbackend.Configuration;

public class SolanaOptions
{
    public const string SectionName = "Solana";

    /// <summary>
    /// Set this environment variable to a private RPC URL (e.g. Helius devnet). The API reads it
    /// through ASP.NET configuration; the Admin tool and the tests read it through
    /// <see cref="RpcUrlFromEnvironment"/>. It usually contains an API key, so never commit it.
    /// </summary>
    public const string RpcUrlEnvironmentVariable = "Solana__RpcUrl";

    public const string PublicDevnetRpcUrl = "https://api.devnet.solana.com";

    public string Cluster      { get; set; } = "devnet";
    public string RpcUrl       { get; set; } = PublicDevnetRpcUrl;
    public string ProgramId    { get; set; } = "J4fL4Q8nnndro2xmVdCayRt5apXhexkkVQoqqwf4AKi4";
    /// <summary>Our devnet test stablecoin (created by the Admin tool's init); must match the program's Config.mint.</summary>
    public string Mint         { get; set; } = "7FutDfvw3Whjv6PcwCYdYb3FV25EHc3TRiUA4zw8pj21";
    public int    MintDecimals { get; set; } = 6;

    /// <summary>How long sync waits for a submitted transaction to confirm.</summary>
    public TimeSpan ConfirmationTimeout      { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan ConfirmationPollInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>For programs outside the API: the private RPC if configured, else public devnet.</summary>
    public static string RpcUrlFromEnvironment()
    {
        return Environment.GetEnvironmentVariable(RpcUrlEnvironmentVariable) ?? PublicDevnetRpcUrl;
    }
}
