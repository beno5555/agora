using Solnet.Wallet;

namespace colosseumbackend.Solana;

/// <summary>Decoded `Reputation` account: a wallet's record, kept separately for each role.</summary>
public record ReputationAccount(PublicKey Wallet, RoleRecord AsBuyer, RoleRecord AsSeller)
{
    public static readonly ReputationAccount Empty = new(new PublicKey(new byte[32]), RoleRecord.Empty, RoleRecord.Empty);

    public static ReputationAccount Decode(ReadOnlySpan<byte> data)
    {
        var reader   = new BorshReader(data, AgoraProgram.DiscriminatorLength);
        var wallet   = reader.Key();
        var asBuyer  = RoleRecord.Read(ref reader);
        var asSeller = RoleRecord.Read(ref reader);

        return new ReputationAccount(wallet, asBuyer, asSeller);
    }
}
