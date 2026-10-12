namespace colosseumbackend.Solana;

/// <summary>The RPC node couldn't be reached or rejected the request. Mapped to 503 by the global handler.</summary>
public class SolanaUnavailableException : Exception
{
    public SolanaUnavailableException(string? reason) : base($"Solana RPC request failed: {reason}")
    {
    }
}
