# Current status: 2026-10-11 (evening)

Snapshot of work since the team decided on dynamic deposits, no review time, and the delivery-deadline choice. For the overall plan see [`../PLAN.md`](../PLAN.md); for the technical design see [`../architecture/`](../architecture/README.md).

**Deadline:** submission closes 2026-10-13 06:59 UTC (10:59 Tbilisi). The team targets Oct 12.

## Update: 2026-10-12

- **Program deployed to devnet:** `J4fL4Q8nnndro2xmVdCayRt5apXhexkkVQoqqwf4AKi4`, initialized with `Admin init`.
- **Test stablecoin (mint):** `7FutDfvw3Whjv6PcwCYdYb3FV25EHc3TRiUA4zw8pj21` (6 decimals); treasury `32gaRtk7owPbBndHgpNiskFPQEFMmMWsbrtJnnbXkXqZ`. Set in `appsettings.json`.
- **End-to-end tests: 9/9 passed** on devnet on the first run, so the C#-built transactions work with the program.
- **Frontend docs rewritten** against the actual backend: [`architecture/api.md`](../architecture/api.md) (full contract with TypeScript types) and [`architecture/frontend.md`](../architecture/frontend.md) (pages, deal page, wallet, transactions, dev setup). The frontend can start from those two files.
- **In progress (Claude):** the API sent times without the UTC `Z` (SQLite drops it), so browsers would read deadlines as local time. Fix written; checking a test failure before committing.
- Funding: the deploy wallet has ~3 SOL. Demo wallets get tokens with `Admin fund <wallet>` when someone needs them.

## Done (all committed and pushed)

| Commit | What |
|---|---|
| `76816a6` docs | Recorded team decisions: a seller who misses the delivery deadline forfeits their deposit to the buyer; disputes that never end stay locked forever (alpha). |
| `7839bc8` program | **Dynamic deposits** and **no review time** (details below). 8 program tests pass. |
| `8dddb45` backend | Full API: wallet sign-in, profiles, games, listings, deals, notifications, background sync. |
| `6beff4f` backend | Admin tool: `init`, `fund`, `status`. |
| `a7471d1` backend | 9 end-to-end tests (written, **not run yet**). |

### 1. Dynamic deposits (team decision)
- Nobody enters deposits. The **on-chain program** calculates both when a deal is created, from each party's on-chain record and the deal's price, so nobody can fake them, including us.
- Formula (all values adjustable by the admin via `update_config`, for new deals only):
  - New wallet: **20%** of the price.
  - **−1.5%** for each 1× *this deal's price* of completed volume, up to −15%. A record built on small deals barely helps on a big deal.
  - **+10%** for each deal the wallet accepted as seller and never delivered.
  - Kept between **5% and 50%**.
  - Example, $15 deal: new wallet $3.00; after $15 completed $2.78; after $150 completed $0.75.
- **Weak spots** (documented in `architecture/program.md#deposits`):
  1. A penalized seller can start over with a new wallet. Penalties only matter while the earned discount is worth keeping; fixing this needs identity checks.
  2. The record can be faked by trading with yourself. It costs the 2% fee, and the impact is small: the price is always locked, so deposits only cover non-delivery and wasted time.
  3. Disputes aren't in the formula, since either side can open one. Revisit when dispute resolution is designed.
  4. Buying and selling count toward the same record.
- More variables can be added later (requires a program update).

### 2. Deadlines and disputes
- **Delivery deadline presets:** 1h, 6h, **24h (default)**, 3d. The program accepts 60 seconds to 30 days, so the demo can use **2 minutes**.
- **Review time removed.** After delivery, the seller is paid only when the buyer confirms (or a split is accepted, or the seller refunds).
- **Either side** can open a disagreement once the deal is under way. Dispute resolution will be designed later.

### 3. Backend (`backend/colosseumbackend`)
Follows the team's coding conventions: controllers → services → repositories, result wrappers, single exit outside controllers, constants, logging in services, rate limits.
- **Sign-in with wallet:** the backend issues a one-time message, the wallet signs it, the backend checks the signature and sets a session cookie. Phantom email users get a wallet automatically.
- **Endpoints:**
  - `/api/config`
  - `/api/auth/*`
  - `/api/me`, `/api/users/{wallet}` (with on-chain reputation)
  - `/api/games`
  - `/api/listings` (search, create, edit, pause/resume; the listing page shows a deposit preview)
  - `/api/deals` (create, view, list mine, `/tx` builds the transaction for any allowed action, `/sync` after the wallet sends)
  - `/api/me/notifications`
- **Solana in C#:** the backend builds every transaction **unsigned**, the user's wallet signs and sends it, then the backend reads the result back from the chain. **The backend holds no keys.**
- A background job re-checks open deals every minute and sends deadline reminders.
- SQLite database with EF Core migrations, applied at startup.
- **Verified:** builds with 0 warnings. Ran it locally: endpoints respond, a bad signature is rejected, a request without a session gets 401.

### 4. Admin tool (`backend/colosseumbackend.Admin`)
Signs with the deploy wallet. Run from `backend/`:
```powershell
dotnet run --project colosseumbackend.Admin -- status
dotnet run --project colosseumbackend.Admin -- init                  # one-time: test stablecoin + treasury + program config
dotnet run --project colosseumbackend.Admin -- fund <wallet> 100 0.05 # 100 test tokens + 0.05 devnet SOL for a demo user
```
We'll use **our own test stablecoin** on devnet instead of Circle's devnet USDC, so we can fund demo users freely.

### 5. End-to-end tests (`backend/colosseumbackend.Tests`)
9 tests run the real API against devnet. Test wallets sign the backend's transactions exactly like a user's wallet would. Coverage:
- a full deal paid match by match;
- a dispute settled by a split;
- access checks;
- validation;
- sign-in replay and impostor signatures.

**Not run yet**, because they need the program on devnet. They run on devnet because Solana's local test validator doesn't start on Windows.

## Next steps (in order)
1. ~~Fund the deploy wallet.~~ Done.
2. ~~Deploy, `Admin init`, set the mint.~~ Done.
3. ~~Run `dotnet test` on devnet.~~ Done (9/9). UTC time fix pending.
4. **On demand:** `Admin fund <wallet>` for each demo wallet (a deal needs two: buyer and seller).
5. **Saba:** Angular app per `architecture/api.md` and `architecture/frontend.md`. Can start now.
6. **Mate and Dato:** demo listing with a 2-minute deadline, the pitch, the demo video, test carriers and coaches.
7. **Submit by Oct 12:** README, video, Colosseum form, in the team's own words.

## Things to know
- **No security review** of the program yet. Fine for the devnet demo; don't use real money.
- **Windows Defender** sometimes locks freshly built files, so a build can fail once and pass on retry. Program tests use no debug symbols for this reason.
- **Running program tests** needs OpenSSL environment variables; see `program/README.md`.
- **Production JWT key:** set with `dotnet user-secrets set Jwt:SigningKey <base64>`. In Development, a random key is generated when it's missing.
