# Repository Guidelines

## Project Structure & Module Organization

Agora is a Solana marketplace with three application layers:

- `frontend/src/app/`: Angular 21; `core/` services, `shared/` components, and `pages/` routes. Global CSS lives in `src/styles/`; public assets in `frontend/public/`.
- `backend/colosseumbackend/`: .NET 10 API organized as controllers → services → repositories, with EF Core/SQLite and Solana integration. Sibling projects contain the Admin CLI and xUnit tests.
- `program/programs/agora/`: Anchor/Rust program in `src/`, LiteSVM integration tests in `tests/agora.rs`.
- `docs/`: architecture, product plan, design assets, and current status. `deploy/` contains hosting scripts.

Read `docs/currentstatus/STATUS.md`, `ISSUES.md`, and the relevant architecture document before changing behavior.

## Build, Test, and Development Commands

Use .NET 10 and Node.js 22+. Run commands from the indicated directory:

- `backend/`: `dotnet build`; `dotnet run --project colosseumbackend` starts the API on port 5107; `dotnet test` runs end-to-end tests.
- `frontend/`: `npm ci` installs locked dependencies; `npm start` serves port 4200 and proxies `/api`; `npm run build` creates the production bundle.
- `program/`: `cargo build-sbf --manifest-path programs/agora/Cargo.toml`, then `cargo test --manifest-path programs/agora/Cargo.toml`. Follow `program/README.md` for Windows/OpenSSL setup.

## Coding Style & Naming Conventions

Frontend: two-space indentation, strict TypeScript, single quotes, and Prettier's 100-column target. Follow `.editorconfig` and `.prettierrc`; use kebab-case filenames such as `wallet-address.ts` and `auth.service.ts`. No lint script is configured.

C#: four-space indentation, PascalCase types/methods, and one type per matching filename; preserve documented exceptions in `CLAUDE.md`. Rust: four-space indentation and snake_case functions/modules.

## Testing Guidelines

Backend xUnit tests use `*Tests.cs` and descriptive `Scenario_Action_Outcome` methods. They require an initialized devnet program and a funded mint-authority wallet (`AGORA_ADMIN_KEYPAIR`); they spend devnet SOL. Rust tests use descriptive snake_case names and load the compiled `agora.so`.

No coverage threshold is configured. Run `npm run test:wallet` for wallet-flow regressions. Frontend `npm test` lacks an Angular test target; also validate with a production build and relevant browser checks.

## Commit & Pull Request Guidelines

Follow history's `area: description` convention, e.g. `program: add review window`. The documented workflow uses `main` directly with `git pull --rebase` before changes and pushes. If opening a PR, describe behavior, validation, related issues, and UI screenshots.

Commit and push completed changes promptly, without waiting to be asked. Stage only task-related files; preserve unrelated local work.

Update API implementation, `docs/architecture/api.md`, and frontend consumers together.

## Security & Configuration

Keep keypairs, JWT secrets, and private `Solana__RpcUrl` values uncommitted and out of logs. The API builds unsigned transactions; user wallets authorize fund movements.
