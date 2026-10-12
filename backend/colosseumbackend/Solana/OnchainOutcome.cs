namespace colosseumbackend.Solana;

/// <summary>Mirrors the program's Outcome enum; the byte order must match state.rs.</summary>
public enum OnchainOutcome : byte
{
    Completed      = 0,
    Split          = 1,
    Undelivered    = 2,
    SellerRefunded = 3,
    Cancelled      = 4,
}
