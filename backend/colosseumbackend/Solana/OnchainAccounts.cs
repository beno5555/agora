using Solnet.Wallet;

namespace colosseumbackend.Solana;

/// <summary>Mirrors the program's DealStatus enum; the byte order must match state.rs.</summary>
public enum OnchainDealStatus : byte
{
    AwaitingSeller = 0,
    Active         = 1,
    Delivered      = 2,
    Disputed       = 3,
}

/// <summary>Mirrors the program's Outcome enum; the byte order must match state.rs.</summary>
public enum OnchainOutcome : byte
{
    Completed      = 0,
    Split          = 1,
    Undelivered    = 2,
    SellerRefunded = 3,
    Cancelled      = 4,
}

public record SplitOfferAccount(PublicKey By, ulong BuyerAmount);

/// <summary>Decoded `Deal` account. Field order must match `state.rs`.</summary>
public record DealAccount(
    ulong              DealId,
    PublicKey          Buyer,
    PublicKey          Seller,
    PublicKey          Mint,
    ulong              Price,
    ushort             Milestones,
    ushort             MilestonesReleased,
    ulong              BuyerDeposit,
    ulong              SellerDeposit,
    long               DeliveryWindow,
    long               AcceptedAt,
    long               DeliveredAt,
    OnchainDealStatus  Status,
    bool               Disputed,
    SplitOfferAccount? SplitOffer)
{
    public static DealAccount Decode(ReadOnlySpan<byte> data)
    {
        var reader = new BorshReader(data, AgoraProgram.DiscriminatorLength);

        ulong     dealId             = reader.U64();
        var       buyer              = reader.Key();
        var       seller             = reader.Key();
        var       mint               = reader.Key();
        ulong     price              = reader.U64();
        ushort    milestones         = reader.U16();
        ushort    milestonesReleased = reader.U16();
        ulong     buyerDeposit       = reader.U64();
        ulong     sellerDeposit      = reader.U64();
        long      deliveryWindow     = reader.I64();
        long      acceptedAt         = reader.I64();
        long      deliveredAt        = reader.I64();
        var       status             = (OnchainDealStatus)reader.U8();
        bool      disputed           = reader.Bool();
        bool      hasSplitOffer      = reader.Bool();
        SplitOfferAccount? splitOffer = hasSplitOffer ? new SplitOfferAccount(reader.Key(), reader.U64()) : null;

        return new DealAccount(dealId, buyer, seller, mint, price, milestones, milestonesReleased, buyerDeposit,
            sellerDeposit, deliveryWindow, acceptedAt, deliveredAt, status, disputed, splitOffer);
    }
}

/// <summary>Decoded `Reputation` account.</summary>
public record ReputationAccount(PublicKey Wallet, uint DealsCompleted, uint DealsDisputed, uint DealsUndelivered, ulong Volume)
{
    public static readonly ReputationAccount Empty = new(new PublicKey(new byte[32]), 0, 0, 0, 0);

    public static ReputationAccount Decode(ReadOnlySpan<byte> data)
    {
        var reader = new BorshReader(data, AgoraProgram.DiscriminatorLength);
        return new ReputationAccount(reader.Key(), reader.U32(), reader.U32(), reader.U32(), reader.U64());
    }
}

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

/// <summary>Decoded `Config` account.</summary>
public record ConfigAccount(PublicKey Admin, PublicKey Mint, PublicKey Treasury, ushort FeeBps, DepositParams Deposit)
{
    public static ConfigAccount Decode(ReadOnlySpan<byte> data)
    {
        var reader   = new BorshReader(data, AgoraProgram.DiscriminatorLength);
        var admin    = reader.Key();
        var mint     = reader.Key();
        var treasury = reader.Key();
        ushort feeBps = reader.U16();
        var deposit  = new DepositParams(reader.U16(), reader.U16(), reader.U16(), reader.U16(), reader.U16(), reader.U16());

        return new ConfigAccount(admin, mint, treasury, feeBps, deposit);
    }
}
