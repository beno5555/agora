namespace colosseumbackend.Solana;

/// <summary>Mirrors the program's DealStatus enum; the byte order must match state.rs.</summary>
public enum OnchainDealStatus : byte
{
    AwaitingSeller = 0,
    Active         = 1,
    Delivered      = 2,
    Disputed       = 3,
}
