# Current status: 2026-10-12

A snapshot of where things stand, kept short; history is in `git log`. Plan: [`../PLAN.md`](../PLAN.md). Design: [`../architecture/`](../architecture/README.md).

**Deadline:** submission closes 2026-10-13 06:59 UTC (10:59 Tbilisi). The team targets Oct 12.

## Where we are

| Part | State |
|---|---|
| Solana program | **Deployed to devnet** (`J4fL4Q8nnndro2xmVdCayRt5apXhexkkVQoqqwf4AKi4`) and initialized. Test stablecoin `7FutDfvw3Whjv6PcwCYdYb3FV25EHc3TRiUA4zw8pj21`. 8 program tests pass. No security review yet: devnet only. |
| Backend API | Complete for the alpha. **11 end-to-end tests pass on devnet**: a full deal paid match by match, a dispute settled by a split, a missed 2-minute deadline refunded with the seller's deposit, access checks, validation, sign-in replay and impostor signatures. |
| Admin tool | `init` done. `fund <wallet>` gives a demo wallet 100 test tokens + 0.05 devnet SOL. |
| Frontend | **Not started; the critical path.** Everything it needs is in [`api.md`](../architecture/api.md) and [`frontend.md`](../architecture/frontend.md). |
| Demo, pitch, submission | Not started. |

Deploy wallet (`Hw4hT8bo2zsvsTm4W1PGTVmheBZgeGvBU5njNuSrf9a9`, keypair on Sandro's PC only): ~1.65 SOL. A full test run uses ~0.3 SOL (the faucet at https://faucet.solana.com tops it up), and each funded demo wallet 0.05.

## Next steps
1. **Blocked on Sandro, in progress:** set `Solana__RpcUrl` to his Helius devnet URL (`setx Solana__RpcUrl "..."`, then restart the terminal/IDE so the new session sees it) to stop the public devnet RPC throttling tests. Once he confirms it's set, **Claude: run the full `dotnet test` suite** (from `backend/`) to confirm everything still passes through the new RPC, including the new `GetMyListings` test.
2. **Saba:** the Angular app, starting with the listing page, the deal page and wallet sign-in (the demo path).
3. **Sandro/Claude:** `Admin fund` demo wallets on request. Support the frontend: fix whatever the real UI uncovers.
4. **Mate and Dato:** the demo script (a listing with the `2m` deadline shows the missed-deadline refund live), the pitch, the video, test carriers and coaches.
5. **Everyone, by Oct 12:** repo README, demo video, Colosseum form, in the team's own words.

## Things to know
- **Devnet RPC:** the public devnet RPC throttles us (one test run had 2 failures that passed on rerun). Set the `Solana__RpcUrl` environment variable to a private devnet RPC (Helius free tier); the API, the Admin tool and the tests all use it. It contains an API key: never commit it.
- **Phantom sign-in:** the message's domain is `localhost:4200` in Development (matches `ng serve`). If Phantom still warns, `Auth:Domain` must match the page host.
- **Windows Defender** sometimes locks freshly built files, so a build can fail once and pass on retry.
- **Program tests** need OpenSSL environment variables; see `program/README.md`.
- **Production JWT key:** `dotnet user-secrets set Jwt:SigningKey <base64>`. In Development, a random key is generated when it's missing.
