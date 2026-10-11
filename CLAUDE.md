# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

It is also meant to be pasted into regular Claude chats, so teammates outside Claude Code get the same context.

## Project context

**Agora** is our entry for Colosseum's **Crypto World's Fair**. We're at the Build Station in Tbilisi, Georgia, with Colosseum, Superteam Georgia and Solana as partners. We're building on Solana.

- **Submission deadline:** 2026-10-13 06:59 UTC (10:59 Tbilisi time). The team aims to submit on **Oct 12**.
- **Presentation:** Oct 14.
- **Event resources:** https://colosseum.com/worldsfair/resources

Event sponsors recommend Phantom Connect as the embedded wallet (it supports email sign-in) and CASH as the stablecoin.

### Team
| Member | Role |
|---|---|
| Sandro | Backend (C#/.NET) |
| Saba | Frontend (Angular) |
| Mate | Came up with the product ideas |
| Dato | Not set yet |

Nobody on the team writes Rust, so Claude writes any on-chain Solana program, using Anchor.

### Idea: not settled yet
**The leading candidate is a P2P trade agreement with deposits.** Two people agree on a trade, a sale or an exchange of items, and lock funds on Solana so that nobody loses out if the other person backs out. It should not be Steam-only.

Claude has suggested a two-sided deposit. Both sides lock a deposit. If both confirm the deal, the deposits are returned and the seller is paid. If either one backs out, both deposits stay locked. Because cheating costs the cheater too, nobody has to rule on disputes. The team hasn't approved this yet.

Open questions:
- Two-sided deposit, or a referee or other way to settle disputes?
- Should the demo story be general goods (for example Georgian classifieds) or in-game items?

**Why not plain escrow:** the blockchain can lock money, but it can't see whether an item was actually delivered. Plain escrow on Solana has also been built many times in earlier Colosseum hackathons (Nector, OPINSPACE.IO, Vigent Escrow, Solia, MMO Nexus, P2PLOOT and others), so the project needs a sharper angle.

**Set aside:** a "digital accountant" for allocating expenses and taxes, without handling money. It's hard to explain why it needs a blockchain, many similar projects already exist, and it may not fit Georgian small-business tax, which may be charged on turnover rather than profit (unverified).

### Rough plan for the leading idea
- **Solana program (Anchor/Rust, written by Claude):** create agreement → both deposit → both confirm → release funds or keep them locked. Devnet, test USDC.
- **Backend (.NET):** listings, deal pages and agreement details stored off-chain.
- **Frontend (Angular):** create a deal, share a link, deposit, confirm. Wallet via Phantom Connect.
- **Rule:** the app must never hold user funds itself. Money only moves through the on-chain program and users' own wallets.

## Repository layout

```
agora/
├── backend/                  .NET solution
│   ├── colosseumbackend.slnx
│   └── colosseumbackend/     ASP.NET Core minimal API, net10.0
├── frontend/                 empty: Angular app still to be created
├── .claude/skills/colosseum-copilot/   project-scoped Claude skill (commit it)
└── skills-lock.json          source and hash of installed skills
```

The backend is still the default template: `Program.cs` sets up OpenAPI and authorization but maps no endpoints yet. `colosseumbackend.http` still points at the template's `/weatherforecast`. `Wallet.cs` is a placeholder model with an off-chain `Balance`. That doesn't fit the no-custody rule above, so rethink it before building on it.

## Commands

Backend (run from `backend/`):
```bash
dotnet build
dotnet run --project colosseumbackend                  # http://localhost:5107
dotnet run --project colosseumbackend --launch-profile https   # https://localhost:7005
```
In Development, the OpenAPI document is served at `/openapi/v1.json`. There are no tests or lint setup yet.

## Claude tooling

The `colosseum-copilot` skill is installed in this project at `.claude/skills/`, not globally, so teammates get it through git. Each person signs in to Colosseum once on their own machine (`npx @colosseum-org/copilot-connect login`). Use it to check ideas against past Colosseum hackathon projects and to find event sponsors and tools.
