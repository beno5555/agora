using Solnet.Wallet;

namespace colosseumbackend.Solana;

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
