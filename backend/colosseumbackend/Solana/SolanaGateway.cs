using colosseumbackend.Configuration;
using Microsoft.Extensions.Options;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Models;
using Solnet.Rpc.Types;
using Solnet.Wallet;

namespace colosseumbackend.Solana;

/// <summary>
/// Everything the backend asks of the Solana RPC: reading program accounts, building unsigned
/// transactions and checking submitted ones. Holds no keys and never signs.
/// </summary>
public class SolanaGateway
{
    private const string ProgramDataLogPrefix = "Program data: ";
    private const string ConfirmedStatus      = "confirmed";
    private const string FinalizedStatus      = "finalized";
    private const int    SignatureLength      = 64;

    private static readonly byte[] DealDiscriminator       = AgoraProgram.AccountDiscriminator("Deal");
    private static readonly byte[] ReputationDiscriminator = AgoraProgram.AccountDiscriminator("Reputation");
    private static readonly byte[] ConfigDiscriminator     = AgoraProgram.AccountDiscriminator("Config");
    private static readonly byte[] DealClosedDiscriminator = AgoraProgram.EventDiscriminator("DealClosed");

    private readonly IRpcClient    _rpc;
    private readonly SolanaOptions _options;

    public AgoraProgram Program { get; }

    public SolanaGateway(IRpcClient rpc, AgoraProgram program, IOptions<SolanaOptions> options)
    {
        _rpc     = rpc;
        Program  = program;
        _options = options.Value;
    }

    #region Reading accounts
    public async Task<ConfigAccount?> GetConfigAsync()
    {
        var data = await GetAccountDataAsync(Program.ConfigAddress());
        return HasDiscriminator(data, ConfigDiscriminator) ? ConfigAccount.Decode(data) : null;
    }

    /// <summary>A wallet with no history has no account yet; that reads as an empty record.</summary>
    public async Task<ReputationAccount> GetReputationAsync(PublicKey wallet)
    {
        var data = await GetAccountDataAsync(Program.ReputationAddress(wallet));
        return HasDiscriminator(data, ReputationDiscriminator) ? ReputationAccount.Decode(data) : ReputationAccount.Empty;
    }

    /// <summary>Returns null when the deal account doesn't exist (not created yet, or closed).</summary>
    public async Task<DealAccount?> GetDealAsync(PublicKey deal)
    {
        var data = await GetAccountDataAsync(deal);
        return HasDiscriminator(data, DealDiscriminator) ? DealAccount.Decode(data) : null;
    }

    /// <summary>Reads many deals in one call (at most 100). Missing accounts come back as null, in order.</summary>
    public async Task<List<DealAccount?>> GetDealsAsync(IList<string> dealAddresses)
    {
        var result = await _rpc.GetMultipleAccountsAsync(dealAddresses, Commitment.Confirmed);
        EnsureSuccess(result.WasSuccessful, result.Reason);

        return result.Result.Value
            .Select(account => DecodeData(account) is { } data && HasDiscriminator(data, DealDiscriminator) ? DealAccount.Decode(data) : null)
            .ToList();
    }
    #endregion

    #region Transactions
    /// <summary>
    /// Compiles the instructions into a transaction paid and signed by `feePayer`, with an
    /// empty signature slot for the wallet to fill. Returns it base64-encoded.
    /// </summary>
    public async Task<string> BuildUnsignedTransactionAsync(PublicKey feePayer, IEnumerable<TransactionInstruction> instructions)
    {
        var blockhash = await _rpc.GetLatestBlockHashAsync(Commitment.Confirmed);
        EnsureSuccess(blockhash.WasSuccessful, blockhash.Reason);

        var builder = new TransactionBuilder()
            .SetRecentBlockHash(blockhash.Result.Value.Blockhash)
            .SetFeePayer(feePayer);
        foreach (var instruction in instructions)
        {
            builder.AddInstruction(instruction);
        }

        byte[] message            = builder.CompileMessage();
        int    requiredSignatures = message[0]; // first header byte: number of required signatures

        // Wire format: compact-u16 signature count (< 128 here, so one byte), zeroed signatures, message.
        var transaction = new List<byte>(1 + requiredSignatures * SignatureLength + message.Length) { (byte)requiredSignatures };
        transaction.AddRange(new byte[requiredSignatures * SignatureLength]);
        transaction.AddRange(message);

        return Convert.ToBase64String(transaction.ToArray());
    }

    /// <summary>
    /// Waits until the signature is confirmed. Returns false if it failed on-chain or didn't
    /// confirm within the configured timeout.
    /// </summary>
    public async Task<bool> WaitForConfirmationAsync(string signature)
    {
        bool isConfirmed = false;
        bool isSettled   = false;
        var  deadline    = DateTime.UtcNow + _options.ConfirmationTimeout;

        while (!isSettled && DateTime.UtcNow < deadline)
        {
            var statuses = await _rpc.GetSignatureStatusesAsync([signature], true);
            var status   = statuses.WasSuccessful ? statuses.Result.Value.FirstOrDefault() : null;

            if (status is not null)
            {
                bool reachedConfirmation = status.ConfirmationStatus is ConfirmedStatus or FinalizedStatus;
                if (status.Error is not null)
                {
                    isSettled = true;
                }
                else if (reachedConfirmation)
                {
                    isConfirmed = true;
                    isSettled   = true;
                }
            }

            if (!isSettled)
            {
                await Task.Delay(_options.ConfirmationPollInterval);
            }
        }

        return isConfirmed;
    }

    /// <summary>
    /// Finds the outcome of a closed deal from the DealClosed event in the given transaction's
    /// logs. Returns null if the transaction didn't close this deal.
    /// </summary>
    public async Task<OnchainOutcome?> GetDealClosedOutcomeAsync(string signature, PublicKey deal)
    {
        OnchainOutcome? outcome = null;
        var transaction = await _rpc.GetTransactionAsync(signature, Commitment.Confirmed);

        if (transaction.WasSuccessful && transaction.Result?.Meta?.LogMessages is { } logs)
        {
            foreach (string log in logs.Where(line => line.StartsWith(ProgramDataLogPrefix)))
            {
                byte[] payload = Convert.FromBase64String(log[ProgramDataLogPrefix.Length..]);
                bool isDealClosed = HasDiscriminator(payload, DealClosedDiscriminator);
                if (isDealClosed)
                {
                    var reader     = new BorshReader(payload, AgoraProgram.DiscriminatorLength);
                    var closedDeal = reader.Key();
                    if (closedDeal == deal)
                    {
                        outcome = (OnchainOutcome)reader.U8();
                    }
                }
            }
        }

        return outcome;
    }

    /// <summary>Most recent transaction signatures that touched the address, newest first.</summary>
    public async Task<List<string>> GetRecentSignaturesAsync(PublicKey address, ulong limit)
    {
        var result = await _rpc.GetSignaturesForAddressAsync(address, limit, null, null, Commitment.Confirmed);
        EnsureSuccess(result.WasSuccessful, result.Reason);

        return result.Result.Select(info => info.Signature).ToList();
    }
    #endregion

    #region Helpers
    private async Task<byte[]?> GetAccountDataAsync(PublicKey address)
    {
        var result = await _rpc.GetAccountInfoAsync(address, Commitment.Confirmed, BinaryEncoding.Base64);
        EnsureSuccess(result.WasSuccessful, result.Reason);

        return DecodeData(result.Result.Value);
    }

    private static byte[]? DecodeData(AccountInfo? account)
    {
        byte[]? data = null;
        if (account?.Data is { Count: > 0 } encoded)
        {
            data = Convert.FromBase64String(encoded[0]);
        }

        return data;
    }

    private static bool HasDiscriminator(byte[]? data, byte[] discriminator)
    {
        return data is not null
            && data.Length >= AgoraProgram.DiscriminatorLength
            && data.AsSpan(0, AgoraProgram.DiscriminatorLength).SequenceEqual(discriminator);
    }

    private static void EnsureSuccess(bool wasSuccessful, string? reason)
    {
        if (!wasSuccessful)
        {
            throw new SolanaUnavailableException(reason);
        }
    }
    #endregion
}
