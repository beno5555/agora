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
| POST | `/api/deals` 🔒 | `{ listingId, buyerNote }` (e.g. in-game name). Creates a draft deal with a fresh `dealId` and returns the exact `create_deal` arguments plus the Deal PDA. The frontend then builds and signs the transaction |
| GET | `/api/deals/{id}` 🔒 | buyer or seller only; off-chain details + mirrored on-chain state + `allowedActions` for the caller |
| GET | `/api/me/deals` 🔒 | `?role=buyer\|seller&status=` |
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

## Reading the chain from .NET
Plain JSON-RPC over `HttpClient` (`getAccountInfo` / `getMultipleAccounts` with base64 encoding), then decode the `Deal` layout by hand: 8-byte Anchor discriminator, then fields in order, little-endian. That avoids depending on a C# Solana SDK. PDAs are worked out in the frontend and returned by `POST /api/deals`, so the backend doesn't need to derive them.

## Errors
RFC 7807 problem details (`Results.Problem` / `Results.ValidationProblem`).
