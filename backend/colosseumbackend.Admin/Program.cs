using System.Globalization;
using colosseumbackend.Admin;
using colosseumbackend.Configuration;
using colosseumbackend.Solana;
using Microsoft.Extensions.Options;
using Solnet.Wallet;

// Agora admin tool. Signs with the deploy wallet (~/.config/solana/id.json by default).
//
//   init [rpcUrl]                       create a test stablecoin, the treasury, and the program config
//   fund <wallet> [tokens] [sol] [mint] [rpcUrl]   send devnet SOL for fees + test tokens to a demo user
//   status [rpcUrl]                     show the program config and the admin's balances

string defaultRpcUrl = SolanaOptions.RpcUrlFromEnvironment(); // Solana__RpcUrl, else public devnet
const ushort FeeBps             = 200;
const double DefaultFundTokens  = 100;
const double DefaultFundSol     = 0.05;

// Devnet values from docs/architecture/program.md#deposits.
var deposit = new DepositParams(BaseBps: 2000, FloorBps: 500, CapBps: 5000, DiscountPerVolumeBps: 150, MaxDiscountBps: 1500, PenaltyBps: 1000);

string command   = args.Length > 0 ? args[0] : "status";
string programId = new SolanaOptions().ProgramId;
var    admin     = AdminChain.LoadKeypair(Environment.GetEnvironmentVariable("AGORA_ADMIN_KEYPAIR") ?? AdminChain.DefaultKeypairPath());

switch (command)
{
    case "init":
    {
        var chain = new AdminChain(Arg(1) ?? defaultRpcUrl, admin);
        var mint  = await chain.CreateMintAsync();
        var program = new AgoraProgram(new PublicKey(programId), mint);
        await chain.InitializeProgramAsync(program, FeeBps, deposit);

        Console.WriteLine($"Program config initialized.");
        Console.WriteLine($"  Test stablecoin mint: {mint.Key}   <- put this in appsettings.json Solana:Mint");
        Console.WriteLine($"  Treasury:             {program.TokenAccountAddress(admin.PublicKey).Key}");
        break;
    }

    case "fund":
    {
        string wallet = Arg(1) ?? throw new ArgumentException("Usage: fund <wallet> [tokens] [sol] [mint] [rpcUrl]");
        double tokens = double.Parse(Arg(2) ?? DefaultFundTokens.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        double sol    = double.Parse(Arg(3) ?? DefaultFundSol.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        var    chain  = new AdminChain(Arg(5) ?? defaultRpcUrl, admin);
        var    mint   = Arg(4) is { } mintArg ? new PublicKey(mintArg) : await ReadConfigMintAsync(chain);
        var    program = new AgoraProgram(new PublicKey(programId), mint);

        await chain.FundAsync(program, new PublicKey(wallet), (ulong)(sol * AdminChain.LamportsPerSol), (ulong)(tokens * AdminChain.TokenUnit));
        Console.WriteLine($"Funded {wallet}: {sol} SOL, {tokens} test tokens.");
        break;
    }

    default:
    {
        var chain   = new AdminChain(Arg(1) ?? defaultRpcUrl, admin);
        var gateway = Gateway(chain, new PublicKey(new byte[32]));
        var config  = await gateway.GetConfigAsync();

        Console.WriteLine($"Admin wallet: {admin.PublicKey.Key}  ({(double)await chain.SolBalanceAsync(admin.PublicKey) / AdminChain.LamportsPerSol} SOL)");
        Console.WriteLine($"Program:      {programId}");
        Console.WriteLine(config is null
            ? "Config:       not initialized (run: init)"
            : $"Config:       mint {config.Mint.Key}, treasury {config.Treasury.Key}, fee {config.FeeBps} bps, deposit {config.Deposit}");
        break;
    }
}

string? Arg(int index) => args.Length > index ? args[index] : null;

async Task<PublicKey> ReadConfigMintAsync(AdminChain chain)
{
    var config = await Gateway(chain, new PublicKey(new byte[32])).GetConfigAsync()
                 ?? throw new InvalidOperationException("Program config not initialized; run init first");
    return config.Mint;
}

SolanaGateway Gateway(AdminChain chain, PublicKey mint)
{
    return new SolanaGateway(chain.Rpc, new AgoraProgram(new PublicKey(programId), mint), Options.Create(new SolanaOptions()));
}
