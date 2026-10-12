namespace colosseumbackend.Solana;

/// <summary>Deposit formula parameters, in basis points. Mirrors `DepositParams` in state.rs.</summary>
public record DepositParams(
    ushort BaseBps,
    ushort FloorBps,
    ushort CapBps,
    ushort DiscountPerVolumeBps,
    ushort MaxDiscountBps,
    ushort UndeliveredPenaltyBps)
{
    private const ulong MaxBps = 10_000;

    /// <summary>Same arithmetic as the program's DepositParams::amount, so previews match exactly.</summary>
    public ulong Amount(ReputationAccount record, ulong price)
    {
        ulong discount = Math.Min(
            (ulong)((UInt128)record.Volume * DiscountPerVolumeBps / price),
            MaxDiscountBps);
        ulong penalty  = (ulong)record.DealsUndelivered * UndeliveredPenaltyBps;
        ulong raw      = BaseBps + penalty;
        ulong rate     = Math.Clamp(raw > discount ? raw - discount : 0, FloorBps, CapBps);

        return (ulong)((UInt128)price * rate / MaxBps);
    }
}
