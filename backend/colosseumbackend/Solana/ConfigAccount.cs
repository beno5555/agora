using Solnet.Wallet;

namespace colosseumbackend.Solana;

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
