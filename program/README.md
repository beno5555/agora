# Agora Solana program

Anchor program (Rust) for deals with two-sided deposits. Design: [docs/architecture/program.md](../docs/architecture/program.md).

Built and tested natively on Windows, with no WSL and no Anchor CLI: `cargo build-sbf` compiles the program, and the tests run it in [LiteSVM](https://github.com/LiteSVM/litesvm), an in-process Solana runtime.

| | Version |
|---|---|
| anchor-lang / anchor-spl | 0.32.2 |
| Solana CLI (Agave) | 4.3.0 |
| LiteSVM (tests) | 0.7.1 |

**Devnet program ID:** `J4fL4Q8nnndro2xmVdCayRt5apXhexkkVQoqqwf4AKi4` (in `declare_id!`).

## One-time setup (Windows)

1. **C++ build tools** (admin PowerShell):
   `winget install Microsoft.VisualStudio.2022.BuildTools --override "--quiet --wait --add Microsoft.VisualStudio.Workload.VCTools --includeRecommended"`
2. **Developer Mode** on: Settings → System → For developers.
3. **Rust:** https://rustup.rs (`rustup-init.exe -y`).
4. **Solana CLI:** download `agave-install-init-x86_64-pc-windows-msvc.exe` from the [Agave v4.3.0 release](https://github.com/anza-xyz/agave/releases/tag/v4.3.0) and run it with `v4.3.0`.
5. **OpenSSL** (only for tests; LiteSVM needs it): install "Win64 OpenSSL" from https://slproweb.com/products/Win32OpenSSL.html. The installer works without admin when run as `<installer>.exe /VERYSILENT /CURRENTUSER /DIR=%LOCALAPPDATA%\agora-tools\openssl`.

## Commands (from `program/`)

```powershell
cargo build-sbf --manifest-path programs/agora/Cargo.toml   # → target/deploy/agora.so

# tests: point openssl-sys at the installed OpenSSL instead of building it from source
$o = "$env:LOCALAPPDATA\agora-tools\openssl"
$env:OPENSSL_NO_VENDOR = "1"; $env:OPENSSL_LIB_DIR = "$o\lib\VC\x64\MD"; $env:OPENSSL_INCLUDE_DIR = "$o\include"; $env:PATH += ";$o\bin"
cargo test --manifest-path programs/agora/Cargo.toml        # run build-sbf first; tests load agora.so
```

## Deploy to devnet

```powershell
solana config set --url devnet
solana-keygen new                     # first time: your deploy wallet (~/.config/solana/id.json, never commit)
solana airdrop 2                      # or https://faucet.solana.com
solana program deploy target/deploy/agora.so --program-id target/deploy/agora-keypair.json
```
Then initialize it once with the backend's Admin tool, from `backend/`:

```powershell
dotnet run --project colosseumbackend.Admin -- init     # creates the test stablecoin, treasury and Config; prints the mint
```

Put the printed mint in `backend/colosseumbackend/appsettings.json` → `Solana:Mint`.

`target/deploy/agora-keypair.json` decides the program ID and is gitignored. Only the person who deploys needs it; keep a backup. If it's lost, a fresh build creates a new ID, and `declare_id!` plus the backend config must be updated.
