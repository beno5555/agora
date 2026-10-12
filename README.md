<!-- DRAFT written by Claude for the team to rewrite in its own words before submission. -->

# Agora

**A peer-to-peer marketplace on Solana for gaming items, in-game currency and game services, where both sides put money on the line.**

Buying a $5 Fortnite carry on today's marketplaces costs the buyer $5.70 while the seller receives $4.50, and the platform decides who is right when something goes wrong. Agora replaces the middleman with a Solana program:

- **Both buyer and seller lock a deposit** with every deal, next to the payment.
- **If they disagree, the money stays locked** until they agree, or until one side accepts the other's offer to split. Nobody outside decides, so lying costs the liar as much as the victim.
- **If the seller misses the delivery deadline**, the buyer takes a one-click refund *plus the seller's deposit*.
- **Services are paid in parts:** a 3-match carry releases a third of the price after each match the buyer confirms.
- **Deposits set themselves** from each wallet's on-chain record: a seller with a history of completed deals locks less, one who missed deliveries locks more.
- **~2% fee**, taken by the program only from what the seller is paid.

Agora never holds user funds. Payments and deposits sit in a vault owned by the program, and only the rules below can move them, with the signature of the right party. Our backend builds unsigned transactions and holds no keys.

Built for Colosseum's **Crypto World's Fair** (Build Station Tbilisi).

## How a deal works

```
buyer pays price + deposit ──▶ seller accepts with their deposit ──▶ seller delivers
        │                                   │                              │
   (buyer can cancel                 deadline missed?               buyer confirms, part by part
    until accepted)             buyer refunded + seller's deposit   ──▶ seller paid, deposits returned
                                                                           │
                                                          either side disputes ──▶ split offer ──▶ accepted
                                                          (funds stay locked until the two agree)
```

Every finished deal updates both wallets' public on-chain reputation (completed deals, volume, missed deliveries).

## Why Solana

The rules only work if neither the parties nor the platform can change them after the money is in. A Solana program enforces them, holds the funds in a vault no one controls, and keeps each wallet's track record public. Low fees make small deals like a $5 carry viable, and stablecoin payments settle in seconds for buyers and sellers anywhere in the world.

## Architecture

```
 Angular app ───REST───▶ .NET API ───▶ SQLite
   │ Phantom                │ builds unsigned transactions,
   │ signs + sends          │ reads deal accounts (Solnet)
   ▼                        ▼
        Agora program (Anchor) on Solana devnet
```

| Part | Tech | Folder |
|---|---|---|
| On-chain program | Rust, Anchor 0.32 | [`program/`](program/) |
| API | C#, ASP.NET Core (.NET 10), EF Core + SQLite, Solnet | [`backend/`](backend/) |
| Web app | Angular, Phantom Connect | [`frontend/`](frontend/) (in progress) |

- **The chain is the source of truth** for money and deal status. The API stores what doesn't belong on-chain (listings, descriptions, notifications) and a cached copy of each deal for fast pages.
- **Sign-in with Solana:** a user's wallet address is their account; they sign in by signing a one-time message. No passwords or emails are stored.
- **Nothing runs by itself on Solana:** deadlines are checked by the program whenever someone acts, so an overdue refund is a button the buyer presses.

Full design: [`docs/architecture/`](docs/architecture/README.md).

## Devnet deployment

| | Address |
|---|---|
| Program | [`J4fL4Q8nnndro2xmVdCayRt5apXhexkkVQoqqwf4AKi4`](https://explorer.solana.com/address/J4fL4Q8nnndro2xmVdCayRt5apXhexkkVQoqqwf4AKi4?cluster=devnet) |
| Test stablecoin (6 decimals) | [`7FutDfvw3Whjv6PcwCYdYb3FV25EHc3TRiUA4zw8pj21`](https://explorer.solana.com/address/7FutDfvw3Whjv6PcwCYdYb3FV25EHc3TRiUA4zw8pj21?cluster=devnet) |

## Run it locally

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download). Set your Phantom wallet to devnet (Settings → Developer settings → Testnet mode).

```bash
cd backend
dotnet run --project colosseumbackend          # API on http://localhost:5107, Swagger UI at /swagger
```

The API uses the public devnet RPC unless the `Solana__RpcUrl` environment variable points to a private one.

Frontend: see [`docs/architecture/frontend.md`](docs/architecture/frontend.md#dev-setup) (Angular dev server with a proxy to the API).

### Tests

- **Program:** 8 tests in [LiteSVM](https://github.com/LiteSVM/litesvm); setup in [`program/README.md`](program/README.md).
- **API:** 12 end-to-end tests against the deployed program on devnet (`dotnet test` from `backend/`): a full deal paid match by match, a dispute settled by a split, a missed deadline refunded with the seller's deposit, access checks and sign-in attacks. They need a funded devnet wallet that owns the test stablecoin.

## Status and limits

This is a hackathon alpha on devnet. The program has not been audited. Known limits:

- A deal where buyer and seller never agree stays locked; dispute resolution beyond split offers is future work.
- A seller with a bad record can start over with a new wallet.
- Steam items (7-day trade reversal) and gift codes need their own settlement rules and are out of scope for the alpha.

## Team

| | Role |
|---|---|
| Sandro | Backend |
| Saba | Frontend |
| Mate | Product |
| Dato | Designer |
