using Solnet.Wallet;

namespace colosseumbackend.Solana;

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
