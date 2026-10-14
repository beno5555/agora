namespace colosseumbackend.Solana;

/// <summary>Deposit formula parameters, in basis points. Mirrors `DepositParams` in state.rs.</summary>
public record DepositParams(
    ushort BaseBps,
    ushort FloorBps,
    ushort CapBps,
    ushort DiscountPerVolumeBps,
    ushort MaxDiscountBps,
    ushort PenaltyBps)
{
    private const ulong MaxBps = 10_000;

    /// <summary>
    /// Same arithmetic as the program's DepositParams::amount, so previews match exactly:
    /// rate = base − discount + penalty, kept within [floor, cap].
    /// </summary>
    public ulong Amount(RoleRecord record, ulong price)
    {
        ulong volumeInPrices = (ulong)((UInt128)record.Volume * DiscountPerVolumeBps / price);
        ulong discount       = Math.Min(volumeInPrices, MaxDiscountBps);
        ulong penalty        = record.BadMarks * PenaltyBps;
        ulong raw            = BaseBps + penalty;
        ulong rate           = Math.Clamp(raw > discount ? raw - discount : 0, FloorBps, CapBps);

        return (ulong)((UInt128)price * rate / MaxBps);
    }
}
