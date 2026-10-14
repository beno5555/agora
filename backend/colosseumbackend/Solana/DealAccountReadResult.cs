namespace colosseumbackend.Solana;

/// <summary>Distinguishes a missing/closed account from an incompatible account still on-chain.</summary>
public record DealAccountReadResult(DealAccount? Account, bool IsCompatible)
{
    private static readonly byte[] Discriminator = AgoraProgram.AccountDiscriminator("Deal");

    public static DealAccountReadResult FromData(byte[]? data)
    {
        DealAccount? account = null;
        bool isCompatible = data is null;

        if (data is not null)
        {
            isCompatible = data.Length == DealAccount.AccountSize
                && data.AsSpan(0, AgoraProgram.DiscriminatorLength).SequenceEqual(Discriminator);
            if (isCompatible)
            {
                account = DealAccount.Decode(data);
            }
        }

        return new DealAccountReadResult(account, isCompatible);
    }
}
