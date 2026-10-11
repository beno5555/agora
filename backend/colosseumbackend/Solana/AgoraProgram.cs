using System.Security.Cryptography;
using System.Text;
using Solnet.Rpc.Models;
using Solnet.Wallet;

namespace colosseumbackend.Solana;

/// <summary>
/// Builds instructions for the Agora Anchor program and derives its addresses.
/// Account order in every builder must match the program's #[derive(Accounts)] structs (lib.rs).
/// </summary>
public class AgoraProgram
{
    public const int DiscriminatorLength = 8;

    public static readonly PublicKey TokenProgramId           = new("TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA");
    public static readonly PublicKey AssociatedTokenProgramId = new("ATokenGPvbdGVxr1b2hvZbsiqW5xWH25efTNsLJA8knL");
    public static readonly PublicKey SystemProgramId          = new("11111111111111111111111111111111");

    // Associated Token Account program instruction tag for "create, or do nothing if it exists".
    private const byte CreateIdempotentTag = 1;

    public PublicKey ProgramId { get; }
    public PublicKey Mint      { get; }

    public AgoraProgram(PublicKey programId, PublicKey mint)
    {
        ProgramId = programId;
        Mint      = mint;
    }

    #region Addresses
    public PublicKey ConfigAddress()
    {
        return FindAddress(ProgramId, Encoding.UTF8.GetBytes("config"));
    }

    public PublicKey DealAddress(PublicKey buyer, ulong dealId)
    {
        return FindAddress(ProgramId, Encoding.UTF8.GetBytes("deal"), buyer.KeyBytes, BitConverter.GetBytes(dealId));
    }

    public PublicKey VaultAddress(PublicKey deal)
    {
        return FindAddress(ProgramId, Encoding.UTF8.GetBytes("vault"), deal.KeyBytes);
    }

    public PublicKey ReputationAddress(PublicKey wallet)
    {
        return FindAddress(ProgramId, Encoding.UTF8.GetBytes("rep"), wallet.KeyBytes);
    }

    public PublicKey TokenAccountAddress(PublicKey owner)
    {
        return FindAddress(AssociatedTokenProgramId, owner.KeyBytes, TokenProgramId.KeyBytes, Mint.KeyBytes);
    }
    #endregion

    #region Instructions
    /// <summary>One-time setup by the program admin (see the Admin tool).</summary>
    public TransactionInstruction InitConfig(PublicKey admin, PublicKey treasury, ushort feeBps, DepositParams deposit)
    {
        var data = new BorshWriter()
            .Bytes(InstructionDiscriminator("init_config"))
            .U16(feeBps)
            .U16(deposit.BaseBps)
            .U16(deposit.FloorBps)
            .U16(deposit.CapBps)
            .U16(deposit.DiscountPerVolumeBps)
            .U16(deposit.MaxDiscountBps)
            .U16(deposit.UndeliveredPenaltyBps)
            .ToArray();

        return Instruction(data,
            AccountMeta.Writable(admin, true),
            AccountMeta.Writable(ConfigAddress(), false),
            AccountMeta.ReadOnly(Mint, false),
            AccountMeta.ReadOnly(treasury, false),
            AccountMeta.ReadOnly(SystemProgramId, false));
    }

    public TransactionInstruction CreateDeal(PublicKey buyer, ulong dealId, PublicKey seller, ulong price, ushort milestones, long deliveryWindowSeconds)
    {
        var deal = DealAddress(buyer, dealId);
        var data = new BorshWriter()
            .Bytes(InstructionDiscriminator("create_deal"))
            .U64(dealId)
            .Key(seller)
            .U64(price)
            .U16(milestones)
            .I64(deliveryWindowSeconds)
            .ToArray();

        return Instruction(data,
            AccountMeta.Writable(buyer, true),
            AccountMeta.ReadOnly(ConfigAddress(), false),
            AccountMeta.ReadOnly(Mint, false),
            AccountMeta.Writable(deal, false),
            AccountMeta.Writable(VaultAddress(deal), false),
            AccountMeta.Writable(TokenAccountAddress(buyer), false),
            AccountMeta.ReadOnly(ReputationAddress(buyer), false),
            AccountMeta.ReadOnly(ReputationAddress(seller), false),
            AccountMeta.ReadOnly(TokenProgramId, false),
            AccountMeta.ReadOnly(SystemProgramId, false));
    }

    public TransactionInstruction AcceptDeal(PublicKey seller, PublicKey deal)
    {
        return Instruction(InstructionDiscriminator("accept_deal"),
            AccountMeta.Writable(seller, true),
            AccountMeta.Writable(deal, false),
            AccountMeta.ReadOnly(Mint, false),
            AccountMeta.Writable(VaultAddress(deal), false),
            AccountMeta.Writable(TokenAccountAddress(seller), false),
            AccountMeta.ReadOnly(TokenProgramId, false));
    }

    /// <summary>mark_delivered, raise_dispute and propose_split share the DealAction accounts.</summary>
    public TransactionInstruction DealAction(string instructionName, PublicKey actor, PublicKey deal, byte[]? args = null)
    {
        var data = new BorshWriter().Bytes(InstructionDiscriminator(instructionName)).Bytes(args ?? []).ToArray();
        return Instruction(data,
            AccountMeta.Writable(actor, true),
            AccountMeta.Writable(deal, false));
    }

    /// <summary>Every instruction that pays out of the vault shares the Settle accounts.</summary>
    public TransactionInstruction Settle(string instructionName, PublicKey actor, PublicKey deal, PublicKey buyer, PublicKey seller, PublicKey treasury, byte[]? args = null)
    {
        var data = new BorshWriter().Bytes(InstructionDiscriminator(instructionName)).Bytes(args ?? []).ToArray();
        return Instruction(data,
            AccountMeta.Writable(actor, true),
            AccountMeta.ReadOnly(ConfigAddress(), false),
            AccountMeta.Writable(deal, false),
            AccountMeta.Writable(buyer, false),
            AccountMeta.ReadOnly(Mint, false),
            AccountMeta.Writable(VaultAddress(deal), false),
            AccountMeta.Writable(TokenAccountAddress(buyer), false),
            AccountMeta.Writable(TokenAccountAddress(seller), false),
            AccountMeta.Writable(treasury, false),
            AccountMeta.Writable(ReputationAddress(buyer), false),
            AccountMeta.Writable(ReputationAddress(seller), false),
            AccountMeta.ReadOnly(TokenProgramId, false),
            AccountMeta.ReadOnly(SystemProgramId, false));
    }

    /// <summary>Creates `owner`'s token account for the mint if it doesn't exist yet; no-op otherwise.</summary>
    public TransactionInstruction CreateTokenAccountIfMissing(PublicKey payer, PublicKey owner)
    {
        return new TransactionInstruction
        {
            ProgramId = AssociatedTokenProgramId.KeyBytes,
            Data      = [CreateIdempotentTag],
            Keys      =
            [
                AccountMeta.Writable(payer, true),
                AccountMeta.Writable(TokenAccountAddress(owner), false),
                AccountMeta.ReadOnly(owner, false),
                AccountMeta.ReadOnly(Mint, false),
                AccountMeta.ReadOnly(SystemProgramId, false),
                AccountMeta.ReadOnly(TokenProgramId, false),
            ],
        };
    }
    #endregion

    #region Encoding helpers
    public static byte[] InstructionDiscriminator(string instructionName)
    {
        return Discriminator($"global:{instructionName}");
    }

    public static byte[] AccountDiscriminator(string accountName)
    {
        return Discriminator($"account:{accountName}");
    }

    public static byte[] EventDiscriminator(string eventName)
    {
        return Discriminator($"event:{eventName}");
    }

    private static byte[] Discriminator(string preimage)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(preimage))[..DiscriminatorLength];
    }

    private TransactionInstruction Instruction(byte[] data, params AccountMeta[] keys)
    {
        return new TransactionInstruction { ProgramId = ProgramId.KeyBytes, Data = data, Keys = keys.ToList() };
    }

    private static PublicKey FindAddress(PublicKey programId, params byte[][] seeds)
    {
        bool found = PublicKey.TryFindProgramAddress(seeds, programId, out var address, out _);
        if (!found)
        {
            // Practically unreachable: a bump in 0..=255 that is off-curve always exists.
            throw new InvalidOperationException("No program address found for the given seeds");
        }

        return address;
    }
    #endregion
}
