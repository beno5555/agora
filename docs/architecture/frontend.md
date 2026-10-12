# Frontend (Angular)

The brief for building the web app. The API contract, with TypeScript shapes for every request and response, is in [api.md](api.md). Sign-in details are in [auth.md](auth.md). Product context is in [../PLAN.md](../PLAN.md).

The app talks to the .NET API for everything off-chain. For anything involving money, it hands a transaction the **backend built** to the user's wallet to sign and send. **The frontend never builds Solana transactions and never holds keys.**

**Status:** not started. The backend and program are deployed on devnet and tested end to end, so every endpoint below works today.

## Stack and layout

- Angular (latest), standalone components, signals, `HttpClient` with `withFetch()` and an interceptor. Put it in `frontend/` at the repo root.
- Solana in the browser: `@solana/web3.js` (only to deserialize transactions and for `PublicKey`), `bs58` (signatures), and the Phantom SDK (see below).
- Keep it small. Suggested structure:

```
frontend/src/app/
  core/       api.service.ts, wallet.service.ts, auth.service.ts, deal-action.service.ts,
              config.service.ts, notification.service.ts, auth.interceptor.ts, models.ts (types from api.md)
  shared/     amount.pipe.ts (base units → "15.00"), countdown, wallet-address (short + copy), explorer-link
  pages/      home, game, listing, deal, sell, me, profile
```

## Pages

| Route | Page | Data | Main actions |
|---|---|---|---|
| `/` | Home: games grid, newest listings | `GET /api/games`, `GET /api/listings` | browse, search (`q`) |
| `/g/:slug` | Game storefront, filter Service/Item | `GET /api/listings?game=:slug&type=` | open a listing |
| `/l/:id` | Listing, **the deal link sellers share** | `GET /api/listings/:id` | terms, seller's on-chain record, deposit preview; **Buy** (with an optional note such as an in-game name) |
| `/d/:id` | **Deal page** | `GET /api/deals/:id` | status timeline, countdown, vault breakdown, buttons from `allowedActions` |
| `/sell`, `/sell/:id` | Create or edit a listing | `GET /api/games`, `POST`/`PATCH /api/listings` | |
| `/me` | Dashboard: my deals (tabs: buying / selling), my listings (pause/resume), notifications, profile (display name, Discord) | `GET /api/me/deals?role=`, `GET /api/users/{me}`, `GET /api/me/notifications`, `PATCH /api/me` | |
| `/u/:wallet` | Public profile: on-chain reputation + active listings | `GET /api/users/:wallet` | |

Every page except `/d/:id`, `/sell` and `/me` works signed out. For those three, the guard asks the user to connect and sign in.

There's no "my listings" endpoint. Use `activeListings` from `GET /api/users/{myWallet}`. It only shows active listings, so keep the IDs of listings paused in this session to show them as paused.

## The deal page (the heart of the demo)

Make "the money is locked, and nobody outside decides" visible:

1. **Vault breakdown:** price, buyer deposit and seller deposit (from `FullDealDto`), and the total locked.
2. **What happens under each outcome.** Remaining price = `price - price/milestones × milestonesReleased`; fee = `feeBps` (from `/api/config`) of whatever the seller is paid.

| Outcome | Buyer gets | Seller gets |
|---|---|---|
| Confirm (each part) | n/a | `price/milestones` per part, minus fee |
| Completed (all parts confirmed) | own deposit back | own deposit back |
| Buyer cancels before acceptance | price + deposit | n/a |
| Seller declines or refunds (`SellerRefund`) | remaining price + deposit | own deposit |
| Seller misses the deadline (`RefundUndelivered`) | remaining price + own deposit **+ seller's deposit** | nothing |
| Split accepted | `buyerAmount` + deposit | remaining − `buyerAmount` − fee, + deposit |

3. **Timeline:** created → accepted (`acceptedAt`) → delivered (`deliveredAt`) → final status, with parts released `milestonesReleased / milestones` shown as "2 of 3 matches" (use `unitLabel`).
4. **Countdown** to `deliveryDeadline` while `Active`. When it hits zero, refetch the deal: the buyer's `RefundUndelivered` button appears and the seller's `MarkDelivered` disappears.
5. **Buttons come only from `allowedActions`.** Label map: Accept → "Accept & lock deposit", Cancel → "Cancel & refund", MarkDelivered → "Mark delivered", Confirm → "Confirm N {unitLabel}(s)" (stepper, 1..remaining parts), RefundUndelivered → "Take refund + seller's deposit", RaiseDispute → "Disagree", ProposeSplit → "Offer a split" (amount input: how much goes back to the buyer, 0..remaining), AcceptSplit → "Accept their offer: you get X", SellerRefund → "Refund buyer".
6. **Disputed:** show the current `splitOffer` and who made it (`by` = buyer or seller wallet), and explain that the funds stay locked until both agree.
7. After every action, show the transaction signature as a link: `https://explorer.solana.com/tx/{sig}?cluster=devnet`. Also link the deal account: `https://explorer.solana.com/address/{dealPda}?cluster=devnet`. Judges like seeing it on-chain.
8. While on the page, poll `GET /api/deals/:id` every 10–15 s for open deals, so a change the other side makes shows up (the backend picks it up within a minute, or immediately if they used the app).

Status badge colors: Draft/AwaitingSeller neutral, Active/Delivered blue, Disputed amber, Completed green, Split/SellerRefunded/Cancelled grey, Undelivered red, Expired grey.

## Wallet and sign-in

- **Phantom.** Phantom Connect also lets email users sign in and creates a wallet for them: https://docs.phantom.com/phantom-connect. If Phantom Connect setup costs time, start with the injected `window.phantom.solana` provider (`connect()`, `signMessage()`, `signAndSendTransaction()`), which covers the extension, then swap it in behind the same `WalletService` interface.
- **Phantom must be set to devnet** (Settings → Developer settings → Testnet mode → Solana Devnet). Otherwise it simulates against mainnet and shows errors.
- `WalletService`: `connect()`, `disconnect()`, `publicKey` (signal), `signMessage(bytes)`, `signAndSendTransaction(tx)`. Watch the wallet's account-change event: if the account changes, log out (the session belongs to the old wallet).
- `AuthService` (flow in [auth.md](auth.md)):
  1. `GET /api/auth/nonce?wallet=` → `message`
  2. `wallet.signMessage(new TextEncoder().encode(message))` → `bs58.encode(signature)`
  3. `POST /api/auth/verify { wallet, message, signature }` (message unchanged) → the cookie is set; store the returned user in a signal.
  - On app start, call `GET /api/me`: 200 means already signed in (the cookie lasts 7 days); 401 means signed out.
- **Interceptor:** `withCredentials: true` on every request. On 401 from anything other than `/api/me` and `/api/auth/*`, clear the user and open the connect/sign-in dialog. On 429/503, show the problem title in a toast.
- The sign-in message starts with `localhost wants you to sign in…` (backend `Auth:Domain`). Phantom recognizes this "Sign in with Solana" format and may warn if the domain doesn't match the page. If it does, tell Sandro to set `Auth:Domain` to the page's host (e.g. `localhost:4200`).

## Transactions

A `DealActionService` handles every on-chain action:

1. Ask the backend for the transaction: `POST /api/deals { listingId, buyerNote }` (Buy) or `POST /api/deals/{id}/tx { action, parts?, buyerAmount? }`.
2. Turn it into bytes and let the wallet sign and send it:
   ```ts
   const tx = Transaction.from(Buffer.from(res.data.transaction, 'base64'));   // legacy tx from @solana/web3.js
   // or: Uint8Array.from(atob(b64), c => c.charCodeAt(0)) if the SDK takes raw bytes
   const { signature } = await wallet.signAndSendTransaction(tx);
   ```
   Check which form the Phantom SDK in use expects; this is the only place that changes. (`Buffer` needs the `buffer` polyfill in Angular, or use the `Uint8Array` route with `VersionedTransaction.deserialize`.)
3. `POST /api/deals/{id}/sync { signature }` → updated `FullDealDto`. Replace the page state with it.
   - Show a "Confirming on Solana…" state; sync can take up to 30 s.
   - `"Transaction Not Confirmed"` → retry sync once, then refetch the deal and show the error.
   - If the user rejects in the wallet, do nothing (for Buy, the draft expires on its own).
4. Request the transaction **right before** opening the wallet. The blockhash expires in about a minute.

Buy flow: on `/l/:id`, Buy → (sign in if needed) → `POST /api/deals` → wallet → sync → navigate to `/d/{deal.id}`.

## Money formatting

- All amounts are strings of base units. Convert with `BigInt` and `mintDecimals` (6): `15000000` → `15.00`. Never use `Number` math on amounts.
- Inputs (price, split amount): parse the decimal string the user typed into base units (`"15.5"` → `"15500000"`), and reject more than 6 decimals.
- Show the token as **"USDC (test)"** or just `$`. On devnet it's our own test stablecoin.
- Before submitting, check `price % milestones === 0n` in base units and say "price must split evenly into N parts".

## Notifications

Poll `GET /api/me/notifications` every 30–60 s while signed in; badge = unread count on the first page. Map `kind` to text ([api.md](api.md#notifications)) and link to `/d/{dealId}`. Mark read on click.

## Dev setup

1. Backend: from `backend/`, `dotnet run --project colosseumbackend` (http profile, `http://localhost:5107`).
2. `frontend/proxy.conf.json`:
   ```json
   { "/api": { "target": "http://localhost:5107", "secure": false, "changeOrigin": false } }
   ```
   and `ng serve --proxy-config proxy.conf.json` (or set `proxyConfig` in `angular.json`). The API has no CORS, and the proxy keeps the cookie same-origin.
3. The session cookie is `Secure` + `SameSite=Lax`. Chrome, Edge and Firefox accept it on `http://localhost`; Safari doesn't, so develop in Chrome.
4. **Two wallets to test a deal:** a buyer and a seller must be different wallets. Create two accounts in Phantom (or two browser profiles). Each needs test tokens and devnet SOL: send the addresses to Sandro, who runs `Admin fund <wallet>`.
5. For a fast demo, set a short delivery window. The listing presets are 1h/6h/24h/3d; the program accepts down to 60 s, but the API only takes the presets.

## Out of scope for the alpha

Chat (the buyer note plus Discord handles cover contact), images on listings, search ranking, and dispute resolution beyond splits.
