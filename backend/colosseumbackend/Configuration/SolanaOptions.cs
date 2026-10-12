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
