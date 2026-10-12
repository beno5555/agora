using System.Net.Http.Json;
using System.Text;
using colosseumbackend.Admin;
using colosseumbackend.Common;
using colosseumbackend.Configuration;
using colosseumbackend.Dtos;
using colosseumbackend.Solana;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;

namespace colosseumbackend.Tests;

/// <summary>
/// Runs the API against a cluster where the program is deployed and initialized (devnet by
/// default; override with AGORA_TEST_RPC). The admin keypair (AGORA_ADMIN_KEYPAIR, default
/// ~/.config/solana/id.json) must be the test stablecoin's mint authority: it funds every test
/// wallet with a little SOL and test tokens. solana-test-validator can't be used on Windows
/// (Agave 4.3 fails to unpack its genesis archive there).
/// </summary>
public class SolanaTestFixture : IAsyncLifetime
{
    public const ulong Token = AdminChain.TokenUnit;

    private const ulong  LamportsPerTest = AdminChain.LamportsPerSol / 50; // 0.02 SOL: fees + rent for reputation accounts
    private const ulong  TokensPerWallet = 1_000 * Token;
    private const int    SignatureOffset = 1;  // after the one-byte signature count
    private const int    SignatureLength = 64;

    private string                          _databasePath = string.Empty;
    private WebApplicationFactory<Program>? _factory;

    public AdminChain   Chain   { get; private set; } = null!;
    public AgoraProgram Program { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        string rpcUrl    = SolanaOptions.RpcUrlFromEnvironment(); // Solana__RpcUrl, else public devnet
        string programId = new SolanaOptions().ProgramId;
        var    admin     = AdminChain.LoadKeypair(Environment.GetEnvironmentVariable("AGORA_ADMIN_KEYPAIR") ?? AdminChain.DefaultKeypairPath());

        Chain = new AdminChain(rpcUrl, admin);
        var gateway = new SolanaGateway(Chain.Rpc, new AgoraProgram(new PublicKey(programId), new PublicKey(new byte[32])), Options.Create(new SolanaOptions()));
        var config  = await gateway.GetConfigAsync()
                      ?? throw new InvalidOperationException($"Program config not initialized on the configured RPC; deploy and run the Admin tool's init first");
        Program = new AgoraProgram(new PublicKey(programId), config.Mint);

        _databasePath = Path.Combine(Path.GetTempPath(), $"agora-test-{Guid.NewGuid():N}.db");
        _factory      = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Agora", $"Data Source={_databasePath}");
            builder.UseSetting("Solana:RpcUrl", rpcUrl);
            builder.UseSetting("Solana:Mint", config.Mint.Key);
            builder.UseSetting("DealSync:Interval", "01:00:00");
        });
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        try
        {
            File.Delete(_databasePath);
        }
        catch (IOException)
        {
            // SQLite may still hold the file briefly; leftovers live in %TEMP%.
        }
    }

    /// <summary>An HTTP client with its own cookie jar, served over https so the Secure session cookie is kept.</summary>
    public HttpClient CreateClient()
    {
        return _factory!.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress       = new Uri("https://localhost"),
            HandleCookies     = true,
            AllowAutoRedirect = false,
        });
    }

    /// <summary>A new wallet with SOL for fees and test tokens.</summary>
    public async Task<Account> CreateFundedWalletAsync()
    {
        var wallet = new Account();
        await Chain.FundAsync(Program, wallet.PublicKey, LamportsPerTest, TokensPerWallet);
        return wallet;
    }

    public async Task SignInAsync(HttpClient client, Account wallet)
    {
        var nonce = await client.GetFromJsonAsync<ApplicationResponse<NonceResponseDto>>($"api/auth/nonce?wallet={wallet.PublicKey.Key}", TestJson.Options);
        string message   = nonce!.Data!.Message;
        string signature = Encoders.Base58.EncodeData(wallet.Sign(Encoding.UTF8.GetBytes(message)));

        var response = await client.PostAsJsonAsync("api/auth/verify",
            new VerifySignInDto { Wallet = wallet.PublicKey.Key, Message = message, Signature = signature }, TestJson.Options);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Does what the user's wallet does: signs the backend's unsigned transaction and sends it.</summary>
    public async Task<string> SignAndSendAsync(string unsignedTransactionBase64, Account wallet)
    {
        byte[] transaction = Convert.FromBase64String(unsignedTransactionBase64);
        byte[] message     = transaction[(SignatureOffset + SignatureLength)..];
        Array.Copy(wallet.Sign(message), 0, transaction, SignatureOffset, SignatureLength);

        return await Chain.SendRawAsync(transaction);
    }

    public async Task<ulong> TokenBalanceAsync(PublicKey owner)
    {
        return await Chain.TokenBalanceAsync(Program, owner);
    }
}
