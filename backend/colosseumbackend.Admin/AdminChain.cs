using System.Text.Json;
using colosseumbackend.Solana;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Models;
using Solnet.Rpc.Types;
using Solnet.Wallet;

namespace colosseumbackend.Admin;

/// <summary>
/// Admin-side chain operations signed with the deploy wallet: one-time program setup and
/// funding demo/test wallets. Never used by the API, which holds no keys.
/// </summary>
public class AdminChain
{
    public const int   MintDecimals    = 6;
    public const ulong LamportsPerSol  = 1_000_000_000;
    public const ulong TokenUnit       = 1_000_000; // 1 token at 6 decimals

    private static readonly TimeSpan ConfirmationTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan PollInterval        = TimeSpan.FromMilliseconds(500);

    public IRpcClient Rpc   { get; }
    public Account    Admin { get; }

    public AdminChain(string rpcUrl, Account admin)
    {
        Rpc   = ClientFactory.GetClient(rpcUrl);
        Admin = admin;
    }

    /// <summary>Loads a Solana CLI keypair file (JSON array of 64 bytes: secret seed + public key).</summary>
    public static Account LoadKeypair(string path)
    {
        // System.Text.Json reads byte[] as base64, but keypair files are arrays of numbers.
        byte[] bytes = JsonSerializer.Deserialize<int[]>(File.ReadAllText(path))?.Select(value => (byte)value).ToArray()
                       ?? throw new InvalidOperationException($"Empty keypair file: {path}");
        const int publicKeyOffset = 32;
        return new Account(bytes, bytes[publicKeyOffset..]);
    }

    public static string DefaultKeypairPath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "solana", "id.json");
    }

    /// <summary>Creates a new SPL token (6 decimals) whose mint authority is the admin.</summary>
    public async Task<PublicKey> CreateMintAsync()
    {
        var mint = new Account();
        var rent = await Rpc.GetMinimumBalanceForRentExemptionAsync(TokenProgram.MintAccountDataSize, Commitment.Confirmed);
        EnsureSuccess(rent.WasSuccessful, rent.Reason);

        await SendAsync(
        [
            SystemProgram.CreateAccount(Admin.PublicKey, mint.PublicKey, rent.Result, TokenProgram.MintAccountDataSize, TokenProgram.ProgramIdKey),
            TokenProgram.InitializeMint(mint.PublicKey, MintDecimals, Admin.PublicKey),
        ], Admin, mint);
        return mint.PublicKey;
    }

    /// <summary>Creates the treasury token account and the program's Config in one transaction.</summary>
    public async Task InitializeProgramAsync(AgoraProgram program, ushort feeBps, DepositParams deposit)
    {
        var treasury = program.TokenAccountAddress(Admin.PublicKey);
        await SendAsync(
        [
            program.CreateTokenAccountIfMissing(Admin.PublicKey, Admin.PublicKey),
            program.InitConfig(Admin.PublicKey, treasury, feeBps, deposit),
        ], Admin);
    }

    /// <summary>Sends SOL for fees and mints test tokens to `wallet` (creating its token account if needed).</summary>
    public async Task FundAsync(AgoraProgram program, PublicKey wallet, ulong lamports, ulong tokenAmount)
    {
        await SendAsync(
        [
            SystemProgram.Transfer(Admin.PublicKey, wallet, lamports),
            program.CreateTokenAccountIfMissing(Admin.PublicKey, wallet),
            TokenProgram.MintTo(program.Mint, program.TokenAccountAddress(wallet), tokenAmount, Admin.PublicKey),
        ], Admin);
    }

    public async Task<ulong> TokenBalanceAsync(AgoraProgram program, PublicKey owner)
    {
        var balance = await Rpc.GetTokenAccountBalanceAsync(program.TokenAccountAddress(owner), Commitment.Confirmed);
        EnsureSuccess(balance.WasSuccessful, balance.Reason);
        return balance.Result.Value.AmountUlong;
    }

    public async Task<ulong> SolBalanceAsync(PublicKey owner)
    {
        var balance = await Rpc.GetBalanceAsync(owner, Commitment.Confirmed);
        EnsureSuccess(balance.WasSuccessful, balance.Reason);
        return balance.Result.Value;
    }

    public async Task<string> SendAsync(List<TransactionInstruction> instructions, params Account[] signers)
    {
        var blockhash = await Rpc.GetLatestBlockHashAsync(Commitment.Confirmed);
        EnsureSuccess(blockhash.WasSuccessful, blockhash.Reason);

        var builder = new TransactionBuilder().SetRecentBlockHash(blockhash.Result.Value.Blockhash).SetFeePayer(signers[0].PublicKey);
        foreach (var instruction in instructions)
        {
            builder.AddInstruction(instruction);
        }

        return await SendRawAsync(builder.Build(signers.ToList()));
    }

    /// <summary>Sends a fully signed transaction and waits for confirmation; throws with the RPC's reason on failure.</summary>
    public async Task<string> SendRawAsync(byte[] signedTransaction)
    {
        var result = await Rpc.SendTransactionAsync(signedTransaction, false, Commitment.Confirmed);
        EnsureSuccess(result.WasSuccessful, $"{result.Reason} {result.RawRpcResponse}");

        await WaitForConfirmationAsync(result.Result);
        return result.Result;
    }

    private async Task WaitForConfirmationAsync(string signature)
    {
        var  deadline    = DateTime.UtcNow + ConfirmationTimeout;
        bool isConfirmed = false;

        while (!isConfirmed && DateTime.UtcNow < deadline)
        {
            var statuses = await Rpc.GetSignatureStatusesAsync([signature], true);
            var status   = statuses.WasSuccessful ? statuses.Result.Value.FirstOrDefault() : null;
            if (status?.Error is not null)
            {
                throw new InvalidOperationException($"Transaction {signature} failed on-chain: {JsonSerializer.Serialize(status.Error)}");
            }

            isConfirmed = status?.ConfirmationStatus is "confirmed" or "finalized";
            if (!isConfirmed)
            {
                await Task.Delay(PollInterval);
            }
        }

        if (!isConfirmed)
        {
            throw new TimeoutException($"Transaction {signature} not confirmed within {ConfirmationTimeout.TotalSeconds}s");
        }
    }

    private static void EnsureSuccess(bool wasSuccessful, string? reason)
    {
        if (!wasSuccessful)
        {
            throw new InvalidOperationException($"RPC request failed: {reason}");
        }
    }
}
