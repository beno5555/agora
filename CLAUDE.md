# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

It is also meant to be shared with regular Claude chats (for example as claude.ai Project knowledge, alongside `docs/PLAN.md`).

## Project

**Agora** is a global P2P marketplace on Solana for gaming items, in-game currencies and game services (such as paid carries). With each deal, both buyer and seller lock a deposit. If they disagree, the funds stay locked until they agree or one accepts the other's offer to split. Nobody outside decides.

**The full product plan is in [`docs/PLAN.md`](docs/PLAN.md):** settlement rules, fees, the alpha build plan, positioning and research evidence. Read it before working on product or architecture. Keep plan details there, not here. **The technical design is in [`docs/architecture/`](docs/architecture/README.md):** read the relevant file before writing code.

- **Event:** Colosseum's Crypto World's Fair (Build Station Tbilisi; partners Colosseum, Superteam Georgia, Solana). https://colosseum.com/worldsfair/resources
- **Submission deadline:** 2026-10-13 06:59 UTC. The team aims to submit on Oct 12. **Presentation:** Oct 14.

### Team
| Member | Role |
|---|---|
| Sandro | Backend (C#/.NET) |
| Saba | Frontend (Angular) |
| Mate | Product ideas |
| Dato | Not set yet |

Nobody on the team writes Rust, so Claude writes the Solana program, using Anchor.

### Rules
- The app never holds user funds. Money moves only through the on-chain program and users' own wallets.
- The product must depend on Solana; it should not work without it.

## Repository layout

```
agora/
├── backend/                  .NET solution: ASP.NET Core minimal API, net10.0
├── frontend/                 Angular app (not created yet)
├── program/                  Solana program (Anchor workspace)
├── docs/PLAN.md              detailed product plan
├── docs/architecture/        technical design: program, auth, API, data model, frontend
├── .claude/skills/colosseum-copilot/   project-scoped Claude skill
└── skills-lock.json          source and hash of installed skills
```

The backend is still the default template: `Program.cs` maps no endpoints, and `colosseumbackend.http` points at the template's `/weatherforecast`. `Wallet.cs` is a placeholder with an off-chain `Balance`, which breaks the no-custody rule. Rethink it before building on it.

## Commands

Backend (run from `backend/`):
```bash
dotnet build
dotnet run --project colosseumbackend                          # http://localhost:5107
dotnet run --project colosseumbackend --launch-profile https   # https://localhost:7005
```
In Development, the OpenAPI document is at `/openapi/v1.json`.

Solana program (run from `program/`, native Windows, no WSL): `cargo build-sbf --manifest-path programs/agora/Cargo.toml`, then `cargo test --manifest-path programs/agora/Cargo.toml` (needs the OpenSSL env vars from [program/README.md](program/README.md)).

## Claude tooling

The `colosseum-copilot` skill is installed in `.claude/skills/` so teammates get it through git. Each person signs in to Colosseum once on their own machine (`npx @colosseum-org/copilot-connect login`). Use it to check ideas against past Colosseum projects and to find event sponsors and tools.
