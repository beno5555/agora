# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

It is also meant to be shared with regular Claude chats (for example as claude.ai Project knowledge, alongside `docs/PLAN.md`).

## Project

**Agora** is a global P2P marketplace on Solana for gaming items, in-game currencies and game services (such as paid carries). With each deal, both buyer and seller lock a deposit. If they disagree, the funds stay locked until they agree or one accepts the other's offer to split. Nobody outside decides.

**The full product plan is in [`docs/PLAN.md`](docs/PLAN.md):** settlement rules, fees, the alpha build plan, positioning and research evidence. Read it before working on product or architecture. Keep plan details there, not here. **The technical design is in [`docs/architecture/`](docs/architecture/README.md):** read the relevant file before writing code. **Latest progress, blockers and next steps:** [`docs/currentstatus/STATUS.md`](docs/currentstatus/STATUS.md). Read it at the start of a session. **Cross-side issues** (a problem found in the other person's area; see Team workflow below): [`docs/currentstatus/ISSUES.md`](docs/currentstatus/ISSUES.md) — check it at the start of a session too.

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

### Team workflow
- **Ownership:** Sandro owns `backend/` and `program/`; Saba owns `frontend/`. Work only in your owner's area, even if a fix for the other side seems obvious and small.
- **`docs/architecture/api.md` is the contract between frontend and backend.** The frontend codes only against it. If something there looks wrong or missing while working on the frontend, don't change the backend to fix it — add a row to [`docs/currentstatus/ISSUES.md`](docs/currentstatus/ISSUES.md) and keep working on your own side; the owner fixes it and updates `api.md` in the same commit.
- **Shared docs:** `CLAUDE.md`, `docs/PLAN.md`, `docs/architecture/*` (except `frontend.md`) and `STATUS.md` are Sandro's to change; `frontend.md` is Saba's. Either side can ask the other for a change instead of making it directly. `ISSUES.md` is written by whoever hits the problem, cleared by whoever owns the area.
- **Git:** both work on `main` directly for this hackathon (no feature branches yet, since the two areas don't overlap). `git pull --rebase` before starting each change (not just once per session) and again before pushing, so no change is built on stale code; commit and push small and often so neither side drifts far behind. Never push a change inside the other person's owned folder.
- **Push promptly:** commit and push finished doc or code changes right away, without waiting to be asked each time, so the other side always has the latest `api.md`/`frontend.md` on GitHub.

### Rules
- The app never holds user funds. Money moves only through the on-chain program and users' own wallets.
- The product must depend on Solana; it should not work without it.

## Repository layout

```
agora/
├── backend/                  .NET solution (net10.0): API, Admin tool, end-to-end tests
├── frontend/                 Angular 21 app (Saba): core/ services, shared/ helpers, pages/
├── program/                  Solana program (Anchor workspace)
├── docs/PLAN.md              detailed product plan
├── docs/architecture/        technical design: program, auth, API, data model, frontend
├── docs/currentstatus/       STATUS.md (current state, next steps), ISSUES.md (cross-side problems)
├── README.md                 public project overview (draft, for the submission)
├── LICENSE                   MIT
├── .claude/skills/colosseum-copilot/   project-scoped Claude skill
└── skills-lock.json          source and hash of installed skills
```

**Backend** (`backend/colosseumbackend`): ASP.NET Core controllers → services → repositories, EF Core on SQLite, wallet sign-in with a JWT in an HttpOnly cookie. All off-chain Solana code is in `Solana/`: `AgoraProgram` builds instructions (account order must match the program's `#[derive(Accounts)]` structs), `SolanaGateway` reads accounts and builds *unsigned* transactions, `DealAccount`, `ReputationAccount` and `ConfigAccount` decode them (field order must match `state.rs`). The backend holds no keys. Code follows the team's coding-conventions skill (result wrappers, single exit outside controllers, constants, logging in services). **One type per file, named after the type** (`UserRepository` → `UserRepository.cs`); never group a layer's types into one file. Exceptions: nested types, and `ApplicationResponse`/`ApplicationResponse<T>`/`ListResponse<T>`, which share `ApplicationResponse.cs`.

**Admin tool** (`backend/colosseumbackend.Admin`): signs with the deploy wallet; `init` (one-time program setup + test stablecoin), `fund <wallet>` (devnet SOL + test tokens for demo users), `status`.

## Commands

Backend (run from `backend/`):
```bash
dotnet build
dotnet run --project colosseumbackend                          # http://localhost:5107
dotnet run --project colosseumbackend --launch-profile https   # https://localhost:7005
```
In Development, Swagger UI is at `/swagger` (the OpenAPI document at `/openapi/v1.json`), and a missing `Jwt:SigningKey` is replaced by a random one (sessions end on restart). Elsewhere set it with `dotnet user-secrets set Jwt:SigningKey <base64, 32+ bytes>`.

```bash
dotnet ef migrations add <Name> --project colosseumbackend --output-dir Data/Migrations   # after `dotnet tool restore`
dotnet run --project colosseumbackend.Admin -- status        # or: init | fund <wallet> [tokens] [sol]
dotnet test                                                  # end-to-end on devnet; needs the program deployed + initialized
```

**RPC:** the API, the Admin tool and the tests use the `Solana__RpcUrl` environment variable when it's set (a private devnet RPC such as Helius), else the public devnet RPC. It contains an API key: keep it in the environment, never in a committed file, and never return or log it.

The end-to-end tests run against devnet, not a local validator: `solana-test-validator` (Agave 4.3) can't start on Windows. They fund throwaway wallets from the deploy wallet (~0.02 SOL each).

Frontend (run from `frontend/`, with the backend running): `npm ci`, then `npm start` (`ng serve` on http://localhost:4200, proxying `/api` to `localhost:5107` via `proxy.conf.json`); `npx ng build` writes the production build to `dist/frontend/browser/`.

Solana program (run from `program/`, native Windows, no WSL): `cargo build-sbf --manifest-path programs/agora/Cargo.toml`, then `cargo test --manifest-path programs/agora/Cargo.toml` (needs the OpenSSL env vars from [program/README.md](program/README.md)).

## Claude tooling

The `colosseum-copilot` skill is installed in `.claude/skills/` so teammates get it through git. Each person signs in to Colosseum once on their own machine (`npx @colosseum-org/copilot-connect login`). Use it to check ideas against past Colosseum projects and to find event sponsors and tools.
