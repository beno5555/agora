# Backend REST API

The contract the Angular app codes against. Source of truth: `backend/colosseumbackend` (`Controllers/`, `Dtos/`, and the enums in `Common/`, one type per file). In Development, Swagger UI is at `/swagger` and the OpenAPI document at `GET /openapi/v1.json`.

## Conventions

- **Base URL:** `/api`. Locally the API runs on `http://localhost:5107` (`dotnet run --project colosseumbackend` from `backend/`). There is **no CORS**: the frontend reaches it through the Angular dev-server proxy (see [frontend.md](frontend.md#dev-setup)). When hosted, the backend serves the Angular build itself from the same origin, so keep `/api` a relative path.
- **JSON** is camelCase.
- **Enums** travel as their C# names in PascalCase: `"Service"`, `"AwaitingSeller"`, `"MarkDelivered"`. Send them the same way. Exception: `role` is a plain lowercase string, `"buyer"` or `"seller"`.
- **Amounts** are strings of token base units, e.g. `"15000000"` = 15 tokens at 6 decimals. u64 doesn't fit safely in a JS number. Use `mintDecimals` from `/api/config` to format, and keep them as `bigint` or string in code.
- **Times** are ISO 8601 UTC, e.g. `"2026-10-12T08:01:09.480Z"`.
- **IDs:** listings and deals are GUIDs; notifications are numbers; users are wallet addresses (base58).
- 🔒 means a session is required. The session is an HttpOnly cookie, `agora_session` (see [auth.md](auth.md)). Without it, the API returns `401` with an empty body.
- **Paging:** list endpoints take `?pageNumber=1&pageSize=20` (max 100). They return no total count: if fewer than `pageSize` items come back, it's the last page.

### Response envelope

Every successful response, and every *expected* failure, uses one envelope:

```jsonc
{ "succeeded": true,  "message": "Listing Retrieved Successfully", "data": { ... } }   // data is an array on list endpoints
{ "succeeded": false, "message": "That Action Is Not Allowed Right Now", "data": null }
```

`message` is short, human-readable text and can be shown as-is in a toast. Endpoints with no payload (pause, resume, mark read) have no `data`.

### Errors

| Status | Body | When |
|---|---|---|
| 400 / 401 / 404 | envelope with `succeeded: false` | business failure: not found, not allowed, bad sign-in, price not divisible… |
| 400 | RFC 7807 `ValidationProblemDetails` (`{ title, status, errors: { field: [msg] } }`) | request failed model validation (missing field, too long, bad amount format) |
| 401 | empty | no or expired session on a 🔒 endpoint |
| 429 | empty | rate limit hit (per wallet when signed in, otherwise per IP) |
| 503 | problem details, `title: "Solana Is Unreachable Right Now; Try Again"` | the Solana RPC failed |
| 500 / 409 | problem details | unexpected |

Rate limits per minute: sign-in 20, reads 300, writes 60, overall 600.

## Config

| Method | Path | Returns |
|---|---|---|
| GET | `/api/config` | `ConfigDto` |

```ts
interface ConfigDto {
  cluster: string; programId: string; mint: string; mintDecimals: number; feeBps: number | null;
  deliveryWindows: { key: string; seconds: number }[];   // the deadline options a seller can pick, shortest first
  defaultDeliveryWindow: string;                         // "24h"
}
```
Current devnet values: cluster `devnet`, program `J4fL4Q8nnndro2xmVdCayRt5apXhexkkVQoqqwf4AKi4`, mint `7FutDfvw3Whjv6PcwCYdYb3FV25EHc3TRiUA4zw8pj21` (our own test stablecoin), `mintDecimals` 6, `feeBps` 200 (2%, taken from what the seller is paid). `feeBps` is null only if the program isn't initialized.

`deliveryWindows` on devnet: `2m`, `15m`, `30m`, `1h`, `3h`, `6h`, `12h`, `24h`, `3d`, `7d`. **`2m` exists only on devnet**, for demos; it disappears on any other cluster. Build the deadline dropdown from this list; don't hard-code it.

## Auth

| Method | Path | Body / query | Returns |
|---|---|---|---|
| GET | `/api/auth/nonce` | `?wallet=<base58>` | `{ message }`: the exact text to sign. 400 for an invalid address |
| POST | `/api/auth/verify` | `{ wallet, message, signature }` | sets the session cookie; `data: FullUserDto`. Any failure → 401 envelope `"Invalid Or Expired Sign-In"` |
| POST | `/api/auth/logout` | n/a | `204`, clears the cookie |

- `message` must be sent back **byte for byte** as received. It is valid for 5 minutes and can be used once.
- `signature` is the wallet's 64-byte signature over the UTF-8 bytes of `message`, **base58-encoded** (the wallet returns a `Uint8Array`; encode it with `bs58`).
- Sessions last 7 days, with no refresh: on 401, sign in again. The first sign-in creates the user. Design: [auth.md](auth.md).

## Users

| Method | Path | Body | Returns |
|---|---|---|---|
| GET | `/api/me` 🔒 | | `FullUserDto` |
| PATCH | `/api/me` 🔒 | `{ displayName?, discord? }` (max 40 chars each; empty or null clears it) | `FullUserDto` |
| GET | `/api/users/{wallet}` | | `StandardUserDto`; 404 if that wallet has never signed in |

```ts
interface MinimalUserDto  { wallet: string; displayName: string | null }
interface FullUserDto     { wallet: string; displayName: string | null; discord: string | null; createdAt: string }
interface ReputationDto   { dealsCompleted: number; dealsDisputed: number; dealsUndelivered: number; volume: string }  // read from the chain
interface StandardUserDto { wallet: string; displayName: string | null; discord: string | null; reputation: ReputationDto; activeListings: MinimalListingDto[] }
```

## Games

| Method | Path | Returns |
|---|---|---|
| GET | `/api/games` | `GameDto[]` |

```ts
interface GameDto { id: number; slug: string; name: string; iconUrl: string | null; activeListings: number }
```
Seeded: 1 `fortnite`, 2 `valorant`, 3 `league-of-legends`, 4 `cs2`, 5 `apex-legends`, 6 `world-of-warcraft`. `iconUrl` is null for all of them for now, so ship local icons keyed by `slug`.

## Listings

| Method | Path | Body / query | Returns |
|---|---|---|---|
| GET | `/api/listings` | `?game=<slug>&type=Service\|Item&q=<text>&pageNumber&pageSize` | `MinimalListingDto[]`, active listings only. An unknown `game` returns `[]` |
| GET | `/api/listings/{id}` | | `StandardListingDto`. **This is the "deal link" sellers share.** It works signed out |
| GET | `/api/me/listings` 🔒 | `?pageNumber&pageSize` | `FullListingDto[]`: the caller's own listings, **paused ones included**, newest first (seller dashboard, edit form) |
| POST | `/api/listings` 🔒 | `ListingTermsDto` | `201`, `StandardListingDto`; the caller is the seller |
| PATCH | `/api/listings/{id}` 🔒 | `ListingTermsDto` (full object, not a partial) | `StandardListingDto`. Seller only (anyone else gets "Listing Not Found"). Applies to future deals only |
| POST | `/api/listings/{id}/pause` 🔒 | | envelope, no data. Hides it from search and blocks new deals |
| POST | `/api/listings/{id}/resume` 🔒 | | envelope, no data |

```ts
type ListingType = 'Service' | 'Item';

// What one milestone of the price pays for ("3 matches"). Optional: null when none fits,
// and the description says what a milestone is; show it as "milestones".
type ListingUnit =
  | 'Match' | 'Game' | 'Round' | 'Win' | 'Run' | 'Level' | 'Rank'
  | 'Hour' | 'Day' | 'Session' | 'Item';

interface ListingTermsDto {
  gameId: number;
  type: ListingType;
  title: string;            // required, max 100
  description: string;      // max 2000
  price: string;            // total, base units, > 0
  milestones: number;       // 1..100 parts the price is released in (e.g. 3 matches)
  unit: ListingUnit | null; // optional; anything not in ListingUnit → 400
  deliveryWindow: string;   // a key from /api/config deliveryWindows, default '24h'
}

interface MinimalListingDto {
  id: string; title: string; gameSlug: string; gameName: string; type: ListingType;
  price: string; milestones: number; unit: ListingUnit | null; seller: MinimalUserDto;
}

interface StandardListingDto extends MinimalListingDto {
  description: string;
  deliveryWindow: string;            // a deliveryWindows key, e.g. '24h'
  isActive: boolean;
  createdAt: string;
  sellerReputation: ReputationDto;   // from the chain
  depositPreview: { buyerDeposit: string; sellerDeposit: string } | null;
}

interface FullListingDto extends MinimalListingDto {   // the seller's own view
  gameId: number; description: string; deliveryWindow: string; isActive: boolean; createdAt: string;
}
```

- **Nobody enters deposits.** The program sets both when the deal is created, from each side's on-chain record and the price (see [program.md](program.md#deposits)). A new wallet pays 20% of the price; the range is 5%–50%.
- `depositPreview` shows what each side would lock if the caller bought now. It is `null` when signed out, when the caller is the seller, or if the chain couldn't be read. When signed out, show "sign in to see your deposit".
- An unknown `unit` (a name not in `ListingUnit`, or a number) fails model validation: `400` with problem details. Only add members to `ListingUnit`, never rename one: listings store the name.
- Validation errors come back as envelope messages: `"Game Not Found"`, `"Delivery Window Is Not One Of The Available Options"`, `"Price Must Divide Evenly Into Milestones"`. Check `price % milestones == 0` in the form, too.

## Deals

| Method | Path | Body / query | Returns |
|---|---|---|---|
| POST | `/api/deals` 🔒 | `{ listingId, buyerNote? }` (note max 300, e.g. in-game name) | `201`, `DealTransactionDto`: a `Draft` deal plus the unsigned `create_deal` transaction for the buyer to sign |
| GET | `/api/deals/{id}` 🔒 | | `FullDealDto`. Buyer or seller only; anyone else gets 404 |
| GET | `/api/me/deals` 🔒 | `?role=buyer\|seller&status=<DealStatus>&pageNumber&pageSize` | `MinimalDealDto[]`, newest first. Drafts and expired drafts are left out |
| POST | `/api/deals/{id}/tx` 🔒 | `DealActionDto` | `DealTransactionDto`: the unsigned transaction for that action |
| POST | `/api/deals/{id}/sync` 🔒 | `{ signature }` | `FullDealDto` re-read from the chain |

```ts
type DealStatus =
  | 'Draft'            // created here, create_deal not confirmed yet (off-chain only)
  | 'AwaitingSeller'   // funded by the buyer: price + buyer deposit locked
  | 'Active'           // seller accepted and locked their deposit; the delivery deadline is running
  | 'Delivered'        // seller marked delivered; waiting for the buyer to confirm
  | 'Disputed'         // either side raised a disagreement; funds stay locked until a split is accepted
  // final:
  | 'Completed' | 'Split' | 'Undelivered' | 'SellerRefunded' | 'Cancelled'
  | 'Expired';         // a Draft never funded within 30 minutes

type DealAction =
  | 'Accept' | 'Cancel' | 'MarkDelivered' | 'Confirm' | 'RefundUndelivered'
  | 'RaiseDispute' | 'ProposeSplit' | 'AcceptSplit' | 'SellerRefund';

interface MinimalDealDto { id: string; title: string; status: DealStatus; price: string; role: 'buyer' | 'seller'; createdAt: string }

interface FullDealDto extends MinimalDealDto {
  dealPda: string;                // the on-chain Deal account (link it on Solana Explorer)
  onchainDealId: string;
  listingId: string;
  gameName: string;
  unit: ListingUnit | null;       // from the listing
  buyerWallet: string;
  sellerWallet: string;
  buyerNote: string | null;
  milestones: number;
  milestonesReleased: number;
  buyerDeposit: string;           // "0" until the first sync after funding
  sellerDeposit: string;
  deliveryWindowSeconds: number;
  acceptedAt: string | null;
  deliveredAt: string | null;
  deliveryDeadline: string | null;  // acceptedAt + window; null before Accept
  splitOffer: { by: string; buyerAmount: string } | null;   // by = wallet that offered
  lastSignature: string | null;     // last transaction seen for this deal
  allowedActions: DealAction[];     // what the CALLER can do right now
}

interface DealActionDto { action: DealAction; parts?: number; buyerAmount?: string }
interface DealTransactionDto { deal: FullDealDto; transaction: string }   // base64
```

### Who can do what (`allowedActions`)

The backend works this out for the caller from status, role and the clock; **render buttons from it** rather than re-deriving the rules. The program enforces the same rules on-chain.

| Status | Buyer | Seller |
|---|---|---|
| AwaitingSeller | `Cancel` (full refund) | `Accept`, `SellerRefund` (decline) |
| Active | `Confirm`, `RaiseDispute`, + `RefundUndelivered` from 15 s after the deadline (margin for Solana's clock) | `MarkDelivered` (before the deadline only), `RaiseDispute`, `SellerRefund` |
| Delivered | `Confirm`, `RaiseDispute` | `RaiseDispute`, `SellerRefund` |
| Disputed | `ProposeSplit`, `AcceptSplit` (only if the *seller* made the current offer), `Confirm` | `ProposeSplit`, `AcceptSplit` (only if the *buyer* made the current offer), `SellerRefund` |
| Draft and final states | n/a | n/a |

Action inputs:
- `Confirm` needs `parts`: how many more parts to release, from 1 up to `milestones - milestonesReleased`. Releasing the last part completes the deal.
- `ProposeSplit` needs `buyerAmount`: how much of the **remaining** price goes back to the buyer (base units, 0…remaining, where remaining = `price - price / milestones * milestonesReleased`). The seller gets the rest minus the fee. A new offer replaces the old one.
- Anything else → `400 "Invalid Parts Or Split Amount"`. An action not in `allowedActions` → `400 "That Action Is Not Allowed Right Now"`.

`allowedActions` is a snapshot: refetch the deal when the delivery deadline passes, since `RefundUndelivered` only appears after it.

### The transaction flow (every on-chain action)

1. `POST /api/deals` (buy) or `POST /api/deals/{id}/tx` (any other action) → `transaction`.
2. Base64-decode it and have the wallet **sign and send** it. It's a **legacy** (not versioned) transaction with the caller as the only signer and fee payer; its signature slot is zero-filled. Its blockhash expires after about a minute, so request it right before opening the wallet. If the user takes longer, request a new one.
3. `POST /api/deals/{id}/sync` with the transaction signature (base58). The backend waits up to 30 s for confirmation, then copies the on-chain state and creates notifications. If it answers `400 "Transaction Not Confirmed"`, the transaction failed or is slow: retry sync once, then refetch the deal.

If the buyer rejects the `create_deal` signature, the draft simply expires; the next Buy creates a new one. Settlement actions (`Confirm`, `Cancel`, `RefundUndelivered`, `AcceptSplit`, `SellerRefund`) may also create a missing token account first, which costs the caller a little devnet SOL. Wallets need test tokens and devnet SOL to act at all (see [frontend.md](frontend.md#dev-setup)).

## Notifications

| Method | Path | Returns |
|---|---|---|
| GET | `/api/me/notifications` 🔒 | `?pageNumber&pageSize` → `NotificationDto[]`, newest first |
| POST | `/api/me/notifications/{id}/read` 🔒 | envelope, no data |

```ts
type NotificationKind =
  | 'DealCreated'      // seller: a buyer funded a deal on your listing
  | 'DealAccepted'     // buyer
  | 'DealDelivered'    // buyer: confirm or dispute
  | 'PartsConfirmed'   // seller: parts were released to you
  | 'DealDisputed'     // both
  | 'SplitProposed'    // the other side
  | 'DealClosed'       // both, any final outcome
  | 'DeadlineSoon'     // seller: delivery deadline within 1 hour
  | 'RefundAvailable'; // buyer: the deadline passed without delivery

interface NotificationDto { id: number; dealId: string | null; kind: NotificationKind; createdAt: string; isRead: boolean }
```
Notifications carry **no text**: the frontend maps `kind` to a sentence and links to `/d/{dealId}`. There's no push or websocket; poll every 30–60 s while signed in. There's no unread-count endpoint; count `isRead === false` on the first page. A background job re-syncs open deals every minute, so changes made outside the app also produce notifications.
