# Agora architecture (alpha)

Technical design for the hackathon alpha. Product rules and the reasons behind them are in [`../PLAN.md`](../PLAN.md). These docs are Claude's proposal. Change them as the team decides.

| Doc | Covers |
|---|---|
| [program.md](program.md) | Solana program: accounts, instructions, deal states, fees |
| [auth.md](auth.md) | Sign in with Solana (wallet signature to session) |
| [api.md](api.md) | .NET REST API endpoints |
| [data-model.md](data-model.md) | Backend database tables |
| [frontend.md](frontend.md) | Angular pages, wallet and program client |

## Components

```
 Angular app ───REST (JSON)───▶ .NET API ───▶ SQLite (EF Core)
   │ Phantom Connect                │ builds unsigned txs, reads deal accounts
   │ signs + sends txs              │ (C#, Solnet + Solana JSON-RPC)
   ▼                                ▼
         Agora program (Anchor) on Solana devnet
         one account per deal; tokens held in a vault the program controls
```

## Key decisions

1. **All Solana logic outside the on-chain program is C#.** The backend builds each transaction *unsigned* (Solnet) and returns it base64-encoded. The frontend only asks the wallet to sign and send it. The backend holds no Solana keys and can't move funds: a transaction does nothing without the user's signature, and the wallet shows what it does before they approve. Rust is used only for the on-chain program, because Solana programs can't be written in C#.
2. **The chain is the source of truth for money and deal status.** The backend stores what doesn't belong on-chain (listings, descriptions, in-game names, notifications) and keeps a *copy* of each deal's on-chain state for fast pages and lists.
3. **Syncing:** after every successful transaction, the frontend calls `POST /api/deals/{id}/sync`. The backend then reads the deal account from the RPC and updates its copy. A background job re-syncs open deals every minute to catch anything missed.
4. **Time limits are checked by the program**, using the on-chain clock, whenever someone calls an instruction. Nothing on Solana runs by itself, so the overdue action ("refund if not delivered") is a button the buyer presses. The deal page shows them as soon as they're allowed. An automatic "crank" can come after the alpha.
5. **Identity = wallet address**, everywhere. See [auth.md](auth.md).
6. **Token:** one SPL stablecoin mint set in the program's config. For the alpha it's a test stablecoin we create on devnet with the Admin tool (`init`), so demo users can be funded freely (`fund`). The program uses Anchor's `token_interface`, so Token-2022 mints (possibly CASH; verify) also work.

## Demo scope

The alpha implements **services paid per match** (`milestones` = number of matches) end to end:
list a service → buyer opens the listing link and pays → seller accepts with their deposit → seller marks delivered → buyer confirms match by match, **or** disputes → split offer → accept.

Single-delivery items work through the same path with `milestones = 1`. Steam items and codes are out of scope (see PLAN.md).

## Environments

| | Value |
|---|---|
| Cluster | Solana devnet |
| Stablecoin | Agora test token (6 decimals), created by `colosseumbackend.Admin init`; its address goes in `Solana:Mint` |
| Backend | `http://localhost:5107` (dev) |
| Frontend | Angular dev server, proxying `/api` to the backend |
