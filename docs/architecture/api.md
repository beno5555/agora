# Backend REST API

ASP.NET Core minimal API under `/api`, JSON only. 🔒 = needs a session (see [auth.md](auth.md)); the caller's wallet is `User.sub`. Amounts are strings of token base units (u64 doesn't fit safely in a JavaScript number). Times are ISO 8601 UTC.

## Config
| Method | Path | Returns |
|---|---|---|
| GET | `/api/config` | `{ cluster, rpcUrl, programId, mint, mintDecimals, feeBps }` for the frontend |

## Auth
| Method | Path | Body / query | Returns |
|---|---|---|---|
| GET | `/api/auth/nonce` | `?wallet=` | `{ message }` |
| POST | `/api/auth/verify` | `{ wallet, message, signature }` | sets the session cookie; `User` |
| POST | `/api/auth/logout` 🔒 | n/a | clears the cookie |

## Users
| Method | Path | Notes |
|---|---|---|
| GET | `/api/me` 🔒 | own profile |
| PATCH | `/api/me` 🔒 | `{ displayName, discord }` |
| GET | `/api/users/{wallet}` | public profile, on-chain reputation (read from the `Reputation` account), active listings |

## Games and listings
| Method | Path | Notes |
|---|---|---|
| GET | `/api/games` | games with listing counts |
| GET | `/api/listings` | `?game=&type=&q=&page=`; active listings only |
| GET | `/api/listings/{id}` | listing + seller's public profile. This is the "deal link" sellers share |
| POST | `/api/listings` 🔒 | create; caller is the seller |
| PATCH | `/api/listings/{id}` 🔒 | seller only; edits apply to future deals only |
| POST | `/api/listings/{id}/pause` 🔒 / `/resume` 🔒 | seller only |

Listing body: `{ gameId, type: "service" | "item", title, description, price, milestones, unitLabel ("match", "hour", …), buyerDeposit, sellerDeposit, deliveryWindowHours, reviewPeriodHours }`. The backend validates the program's rules ahead of time (`price % milestones == 0`, minimum deposits), so users don't sign transactions that will fail.

## Deals
| Method | Path | Notes |
|---|---|---|
| POST | `/api/deals` 🔒 | `{ listingId, buyerNote }` (e.g. in-game name). Creates a draft deal with a fresh `dealId` and returns `{ deal, transaction }`: the unsigned `create_deal` transaction (base64) for the buyer's wallet to sign and send |
| GET | `/api/deals/{id}` 🔒 | buyer or seller only; off-chain details + mirrored on-chain state + `allowedActions` for the caller |
| GET | `/api/me/deals` 🔒 | `?role=buyer\|seller&status=` |
| POST | `/api/deals/{id}/tx` 🔒 | `{ action, parts?, buyerAmount? }` where `action` is one of `allowedActions`. Returns the unsigned transaction (base64), with the caller as fee payer and signer |
| POST | `/api/deals/{id}/sync` 🔒 | `{ signature }`; backend re-reads the Deal account (or confirms it was closed) and updates the copy and notifications |

`allowedActions` (e.g. `["confirm", "raise_dispute"]`) is worked out from status, role and the on-chain clock. The frontend uses it to decide which buttons to show. The program still enforces the rules itself.

Drafts that aren't funded within 30 minutes are discarded.

## Notifications
| Method | Path | Notes |
|---|---|---|
| GET | `/api/me/notifications` 🔒 | newest first |
| POST | `/api/me/notifications/{id}/read` 🔒 | n/a |

Notifications are created when a sync sees a state change: a new deal, accepted, delivered, a confirmed part, disputed, a split offered, closed, and "time limit ends soon".

## Background job
A hosted service re-syncs every open deal once a minute (one `getMultipleAccounts` RPC call per 100 deals) and creates "time limit ends soon" or "you can now claim/refund" notifications.

## Solana from .NET
- **Library:** [Solnet](https://github.com/bmresearch/Solnet) (`Solnet.Rpc`, `Solnet.Wallet`, `Solnet.Programs`) for RPC calls, PDA derivation and building transactions. Its last NuGet release is from 2022 (targets net6.0, which runs on net10). The repo is still active, and Solana's transaction format hasn't changed. If it causes trouble, the few pieces we need are small enough to write ourselves.
- **Building instructions:** an Anchor instruction is `sha256("global:<instruction_name>")[0..8]` followed by the Borsh-encoded arguments, plus the account list in the order of the program's `#[derive(Accounts)]` struct. Keep one C# class (`AgoraInstructions`) with a method per instruction. If the user has no token account yet, add a "create associated token account (idempotent)" instruction first.
- **Reading deals:** `getAccountInfo` / `getMultipleAccounts` (base64), then decode the `Deal` layout: 8-byte Anchor discriminator, then fields in order, little-endian (Borsh; `Option` is a 1-byte tag).

## Errors
RFC 7807 problem details (`Results.Problem` / `Results.ValidationProblem`).
