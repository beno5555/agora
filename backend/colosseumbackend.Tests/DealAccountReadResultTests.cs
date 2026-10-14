using colosseumbackend.Solana;
using Solnet.Wallet;

namespace colosseumbackend.Tests;

public class DealAccountReadResultTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FromData_CurrentDeal_DecodesReviewWindowAndSplit(bool hasSplit)
    {
        byte[] data = EncodeDeal(hasReviewWindow: true, hasSplit);

        var result = DealAccountReadResult.FromData(data);

        Assert.True(result.IsCompatible);
        Assert.NotNull(result.Account);
        Assert.Equal(259200, result.Account.ReviewWindow);
        Assert.Equal(1700000000, result.Account.AcceptedAt);
        Assert.Equal(OnchainDealStatus.Disputed, result.Account.Status);
        Assert.Equal(hasSplit, result.Account.SplitOffer is not null);
        if (hasSplit)
        {
            Assert.Equal(500000UL, result.Account.SplitOffer!.BuyerAmount);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FromData_PreUpgradeDeal_IsIncompatibleRatherThanMissing(bool hasSplit)
    {
        byte[] data = EncodeDeal(hasReviewWindow: false, hasSplit);

        var result = DealAccountReadResult.FromData(data);

        Assert.False(result.IsCompatible);
        Assert.Null(result.Account);
    }

    [Fact]
    public void FromData_MixedBatch_PreservesCurrentAndMissingAccountsAfterLegacyAccount()
    {
        byte[]?[] data = [EncodeDeal(false, true), EncodeDeal(true, true), null];

        var results = data.Select(DealAccountReadResult.FromData).ToList();

        Assert.False(results[0].IsCompatible);
        Assert.NotNull(results[1].Account);
        Assert.True(results[2].IsCompatible);
        Assert.Null(results[2].Account);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(216)]
    [InlineData(218)]
    public void FromData_UnexpectedSize_RejectsWithoutDecoding(int length)
    {
        var result = DealAccountReadResult.FromData(new byte[length]);

        Assert.False(result.IsCompatible);
        Assert.Null(result.Account);
    }

    [Fact]
    public void FromData_WrongDiscriminator_RejectsWithoutDecoding()
    {
        byte[] data = EncodeDeal(true, false);
        data[0] ^= byte.MaxValue;

        Assert.False(DealAccountReadResult.FromData(data).IsCompatible);
    }

    private static byte[] EncodeDeal(bool hasReviewWindow, bool hasSplit)
    {
        var key = new PublicKey(new byte[32]);
        var writer = new BorshWriter()
            .Bytes(AgoraProgram.AccountDiscriminator("Deal"))
            .U64(7).Key(key).Key(key).Key(key)
            .U64(1000000).U16(1).U16(0).U64(200000).U64(200000).I64(86400);
        if (hasReviewWindow)
        {
            writer.I64(259200);
        }

        writer.I64(1700000000).I64(1700000100)
            .Bytes([(byte)OnchainDealStatus.Disputed, 1, (byte)(hasSplit ? 1 : 0)]);
        if (hasSplit)
        {
            writer.Key(key).U64(500000);
        }

        writer.Bytes([254, 253]);
        // Account allocations are independent fixtures for the two deployed Anchor layouts.
        var data = new byte[hasReviewWindow ? 217 : 209];
        writer.ToArray().CopyTo(data, 0);
        return data;
    }
}
