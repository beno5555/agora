# Agora

**A peer-to-peer marketplace on Solana for gaming items, in-game currency and game services, where both buyer and seller put money on the line.**

**Live on Solana devnet: https://agora-market.duckdns.org**

Built for Colosseum's **Crypto World's Fair** (Build Station Tbilisi).

## Try it

It's free: everything runs on devnet with a test stablecoin.

1. Install the [Phantom](https://phantom.com) browser extension. In Settings → Developer Settings, turn on **Testnet Mode** and pick **Solana Devnet**.
2. Send your wallet address to **@sandrobenashvili** on Telegram to get test USDC and devnet SOL.
3. Open https://agora-market.duckdns.org, click **Connect**, and buy a listing. Our team accepts deals as the seller. The listing "Demo: 2-minute deadline (try the refund)" shows what happens when a seller misses the deadline.

On a phone, Connect reopens the site in the Phantom app's browser.

## The problem

Buying a $5 Fortnite carry on today's marketplaces costs the buyer $5.70 while the seller receives $4.50, and the platform decides who is right when something goes wrong. Agora replaces the middleman with a Solana program:

- **Both buyer and seller lock a deposit** with every deal, next to the payment.
- **If they disagree, the money stays locked** until they agree, or until one side accepts the other's offer to split. Nobody outside decides, so lying costs the liar as much as the victim.
- **If the seller misses the delivery deadline**, the buyer takes a one-click refund *plus the seller's deposit*.
- **Services are paid in parts:** a 3-match carry releases a third of the price after each match the buyer confirms.
- **Deposits set themselves** from each wallet's on-chain record: 20% of the price for a new wallet, less as its completed volume grows, more after missed deliveries.
- **2% fee**, taken by the program only from what the seller is paid. Refunds pay no fee.

Agora never holds user funds. Payments and deposits sit in a vault owned by the program, and only the rules below can move them, with the signature of the right party. Our backend builds unsigned transactions and holds no keys.

## How a deal works

```
buyer pays price + deposit ──▶ seller accepts with their deposit ──▶ seller delivers
        │                                   │                              │
   (buyer can cancel                 deadline missed?               buyer confirms, part by part
    until accepted)             buyer refunded + seller's deposit   ──▶ seller paid, deposits returned
                                                                           │
                                                          either side disagrees ──▶ split offer ──▶ accepted
                                                          (funds stay locked until the two agree)
```

Every finished deal updates both wallets' public on-chain record (completed deals, volume, missed deliveries), which sets their deposits in future deals. Each step on the deal page links to its transaction on Solana Explorer.

## Why Solana

The rules only work if neither the parties nor the platform can change them after the money is in. A Solana program enforces them, holds the funds in a vault no one controls, and keeps each wallet's track record public. Low fees make small deals like a $5 carry viable, and stablecoin payments settle in seconds for buyers and sellers anywhere in the world.

## What's in this repo

All of it is the product, built during the hackathon.

| Folder | What it is |
|---|---|
| [`program/`](program/) | The Solana program (Rust, Anchor). It enforces every rule above and holds the funds. |
| [`backend/`](backend/) | The .NET API: listings, deal pages, notifications, sign-in. It builds unsigned transactions and reads deal accounts from Solana. Also an Admin tool and the end-to-end tests. |
| [`frontend/`](frontend/) | The Angular web app, with Phantom for signing. |
| [`deploy/`](deploy/) | How the live site is hosted and deployed. |
| [`docs/`](docs/) | Product plan, technical design, visual design, and the team's working notes. |
| `CLAUDE.md`, `.claude/` | Development tooling. Not product code. |

**Where to read more:**
- **Technical design:** [`docs/architecture/`](docs/architecture/README.md), with the on-chain rules in [`program.md`](docs/architecture/program.md).
- **Product plan:** [`docs/PLAN.md`](docs/PLAN.md), including the research behind it.
- **Current state:** [`docs/currentstatus/STATUS.md`](docs/currentstatus/STATUS.md), the team's working notes.

## Architecture

```
 Angular app ───REST───▶ .NET API ───▶ SQLite
   │ Phantom                │ builds unsigned transactions,
   │ signs + sends          │ reads deal accounts
   ▼                        ▼
        Agora program (Anchor) on Solana devnet
```

| Part | Tech |
|---|---|
| On-chain program | Rust, Anchor 0.32 |
| API | C#, ASP.NET Core (.NET 10), EF Core + SQLite, Solnet |
| Web app | Angular 21, Phantom browser extension (or the Phantom app's browser on phones) |
| Hosting | One DigitalOcean server: Caddy for HTTPS in front of the API, which also serves the web app |

- **The chain is the source of truth** for money and deal status. The API stores what doesn't belong on-chain (listings, descriptions, notifications) and a cached copy of each deal for fast pages.
- **Sign in with Solana:** a wallet address is the account. Users sign in by signing a one-time message. No passwords or emails are stored.
- **Nothing runs by itself on Solana:** the program checks deadlines whenever someone acts, so an overdue refund is a button the buyer presses.

## Devnet deployment

| | Address |
|---|---|
| Program | [`J4fL4Q8nnndro2xmVdCayRt5apXhexkkVQoqqwf4AKi4`](https://explorer.solana.com/address/J4fL4Q8nnndro2xmVdCayRt5apXhexkkVQoqqwf4AKi4?cluster=devnet) |
| Test stablecoin (6 decimals) | [`7FutDfvw3Whjv6PcwCYdYb3FV25EHc3TRiUA4zw8pj21`](https://explorer.solana.com/address/7FutDfvw3Whjv6PcwCYdYb3FV25EHc3TRiUA4zw8pj21?cluster=devnet) |

## Run it locally

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) and Node.js 22+. The API uses the public devnet RPC unless the `Solana__RpcUrl` environment variable points to a private one.

From the repo root, in two terminals:
```bash
cd backend && dotnet run --project colosseumbackend    # API on http://localhost:5107, Swagger UI at /swagger
cd frontend && npm ci && npm start                     # app on http://localhost:4200, proxies /api to the API
```

Building the program: [`program/README.md`](program/README.md).

### Tests

- **Program:** 8 tests in [LiteSVM](https://github.com/LiteSVM/litesvm), an in-process Solana runtime. Setup in [`program/README.md`](program/README.md).
- **API:** 15 end-to-end tests against the deployed program on devnet (`dotnet test` from `backend/`): a full deal paid match by match, a dispute settled by a split, a missed deadline refunded with the seller's deposit, access checks, and sign-in replay and impostor signatures. They need a funded devnet wallet that owns the test stablecoin.

## Status and limits

A hackathon alpha on devnet. The program has not been audited. Known limits:

- A deal where buyer and seller never agree stays locked. Dispute resolution beyond split offers is future work.
- A seller with a bad record can start over with a new wallet.
- Steam items (CS2 skins) settle like any other item for now: the buyer confirms. Waiting out Steam's 7-day trade-reversal window before paying the seller is future work. Gift codes need their own rules and are out of scope.
- Phantom shows the test stablecoin as an unknown token, because it has no on-chain name yet.

## Team

| | Role |
|---|---|
| Sandro | Backend and frontend |
| Saba | Frontend |
| Mate | Product |
| Dato | Design and social media |
