# Frontend (Angular)

The brief for building the web app. **The API contract is [api.md](api.md)**: endpoints, TypeScript types, `allowedActions` and the transaction flow. This file covers only what the UI does with it. Product context: [../PLAN.md](../PLAN.md).

The app talks to the .NET API for everything off-chain. For anything involving money, it hands a transaction the **backend built** to the user's wallet to sign and send. **The frontend never builds Solana transactions and never holds keys.**

## Stack and layout

- Angular 21 in `frontend/`: standalone components with inline templates, signals, route params bound to inputs (`withComponentInputBinding`), `HttpClient` with `withFetch()` and an interceptor. Plain CSS, no framework: shared styles in `src/styles/` (tokens, base, controls, surfaces, utilities), the header and toasts in `app/app.css`, and a `.css` next to each component that has its own look. The visual design is [../design/DESIGN.md](../design/DESIGN.md).
- Solana in the browser: `@solana/web3.js` (only to deserialize transactions), `bs58` (signatures), and Phantom's injected provider (`window.phantom.solana`, the browser extension).

```
frontend/src/app/
  core/       api.service.ts (unwraps the envelope, throws ApiError), wallet.service.ts, auth.service.ts,
              auth.guard.ts, auth.interceptor.ts, deal-action.service.ts, config.service.ts,
              notification.service.ts, toast.service.ts, models.ts (types from api.md)
  shared/     amount.pipe.ts (base units ↔ "15.00"), countdown + clock.service (one shared tick),
              wallet-address (short + copy), explorer-link, listing-card, reputation, status-badge,
              delivery-window ("24h" → "24 hours"), unit (ListingUnit → "3 matches", no unit → "3 milestones")
  pages/      home, game, listing, deal, sell, me, profile
```

## Pages

| Route | Page | Data | Main actions |
|---|---|---|---|
| `/` | Home: hero with an example vault, games as a text index (games with listings first), newest listings, 10 per page | `GET /api/games`, `GET /api/listings` | browse, search (`q`): runs 0.5s after typing pauses, or at once on Enter/Search |
| `/g/:slug` | Game storefront, filter Service/Item (the tabs set `?type=`), 10 per page | `GET /api/games`, `GET /api/listings?game=:slug&type=` | open a listing; empty state links to `/sell?game=:slug` |
| `/l/:id` | Listing, **the deal link sellers share** | `GET /api/listings/:id` | terms, seller's on-chain record, what you lock (the vault preview, signed in); **Buy · lock $X** ("Connect Phantom to buy" when signed out; an optional note such as an in-game name, kept in sessionStorage). The seller sees Copy deal link, Edit and Pause instead |
| `/d/:id` | **Deal page** | `GET /api/deals/:id` | status timeline, countdown, vault breakdown, buttons from `allowedActions` |
| `/sell`, `/sell/:id` | Create or edit a listing | `GET /api/games`, `GET /api/config`, `POST`/`PATCH /api/listings` | deadline dropdown from `deliveryWindows` (label `2m` "2 minutes (demo)"), preselect `defaultDeliveryWindow`; `?game=:slug` preselects the game; price and parts are checked inline (at most 6 decimals, the price splits evenly into the parts, with two nearby prices that do) |
| `/me` | Dashboard: my deals (buying / selling tabs; "Your move" on rows whose `allowedActions` include a primary action, sorted first within the page), my listings (pause/resume), both 10 per page; notifications (the newest 5, "See more" opens the rest), profile | `GET /api/me/deals?role=`, `GET /api/me/listings`, `GET /api/me/notifications`, `PATCH /api/me` | |
| `/u/:wallet` | Public profile: record on Solana (completed, not delivered, disagreements) + active listings | `GET /api/users/:wallet` | |

`/d/:id`, `/sell` and `/me` need a session; their guard asks the user to connect and sign in. Everything else works signed out.

If sign-in is declined, the guard redirects home. Its dependencies are injected before awaiting sign-in, while Angular's injection context is still available; both outcomes are covered by `npm run test:wallet`.

## The deal page (the heart of the demo)

Make "the money is locked, and nobody outside decides" visible. The layout, the status line under the title (whose move it is), the vault with "who can move this money" and the copy are in [DESIGN.md](../design/DESIGN.md) §4, §5.4 and §7; the amounts and sentences are worked out in `pages/deal/deal-money.ts`.

1. **Vault breakdown:** price, buyer deposit and seller deposit, and the total locked.
2. **What happens under each outcome.** Remaining = `price - price/milestones × milestonesReleased`; fee = `feeBps` of whatever the seller is paid.

| Outcome | Buyer gets | Seller gets |
|---|---|---|
| Confirm (each part) | n/a | `price/milestones` per part, minus fee |
| Completed (all parts confirmed) | own deposit back | own deposit back |
| Buyer cancels before acceptance | price + deposit | n/a |
| Seller declines or refunds (`SellerRefund`) | remaining price + deposit | own deposit |
| Seller misses the deadline (`RefundUndelivered`) | remaining price + own deposit **+ seller's deposit** | nothing |
| Buyer silent past the review time (`ClaimPayment`) | own deposit back | remaining price minus fee + own deposit |
| Split accepted | `buyerAmount` + deposit | remaining − `buyerAmount` − fee, + deposit |

3. **Timeline:** created → accepted → delivered → final status (the buyer can confirm straight from `Active`, so when `deliveredAt` is empty but `milestonesReleased` > 0, show "Delivered" as done, "confirmed by the buyer"), with parts shown as "2 of 3 matches" (`unit`; "milestones" when it's null).
4. **Countdown** to `deliveryDeadline` while `Active`. At zero, show "Refund available in a few seconds…" and refetch at deadline + 16 s; that's when the buyer's `RefundUndelivered` appears. The same for `reviewDeadline` while `Delivered`: the seller's `ClaimPayment` appears at review deadline + 16 s.
5. **Buttons come only from `allowedActions`.** Labels: Accept → "Accept & lock deposit", Cancel → "Cancel & refund", MarkDelivered → "Mark delivered", Confirm → "Confirm N {unit}(s)" (stepper), RefundUndelivered → "Take refund + seller's deposit", RaiseDispute → "Disagree", ProposeSplit → an "Offer a split" amount field (what goes back to the buyer) and an "Offer this split" button, AcceptSplit → "Accept their offer: you get X", SellerRefund → "Refund buyer", ClaimPayment → "Claim payment". One primary button (the first of Accept, Confirm, MarkDelivered, AcceptSplit, RefundUndelivered, ClaimPayment); the rest are secondary and Disagree is the only danger button. Each button has one line under it saying what it does in dollars. Disagree and Refund buyer ask once more inline before they run. On a phone the primary also sits in a bar at the bottom of the screen.
6. **Disputed:** show the current `splitOffer`, who made it, and that the funds stay locked until both agree.
7. After every action, link the signature (`https://explorer.solana.com/tx/{sig}?cluster=devnet`), and always link the deal account (`…/address/{dealPda}?cluster=devnet`). Judges like seeing it on-chain.
8. Poll `GET /api/deals/:id` every 10–15 s while the deal is open, so the other side's actions show up.

Status badges (icon + text, never colour alone): Draft "Not funded" and AwaitingSeller "Waiting for seller" neutral, Active "In progress" and Delivered blue, Disputed "Disagreement" amber, Completed green, Undelivered "Not delivered" red, the rest grey.

## Wallet and sign-in

- **Phantom.** `WalletService` uses the injected `window.phantom.solana` provider (the browser extension, or the Phantom app's own browser). Without it, Connect opens a sheet instead of a sign-in: on a phone (no extensions there) "Open Agora in Phantom" with a link that reopens the page in the Phantom app's browser (`https://phantom.app/ul/browse/<page URL>`) and a Copy page link fallback; on a computer "Install Phantom to continue" with a reload button. Connecting from the phone's own browser would need Phantom Connect or Phantom's deeplink protocol (backlog). Phantom Connect (email sign-in with an embedded wallet, https://docs.phantom.com/phantom-connect) can replace it later behind the same `WalletService` interface.
- **Phantom must be set to devnet** (Settings → Developer settings → Testnet mode), or it simulates against mainnet and shows errors.
- `WalletService`: `connect()`, `disconnect()`, `publicKey` (signal), `signMessage(bytes)`, `signAndSendTransaction(tx)`. If the wallet's account changes, log out: the session belongs to the old wallet.
- `AuthService`: nonce → `signMessage(new TextEncoder().encode(message))` → `bs58.encode(signature)` → verify ([api.md](api.md#auth)). On app start, `GET /api/me` tells whether the cookie is still valid.
- **Interceptor:** `withCredentials: true`. On 401 (except from `/api/me` and `/api/auth/*`), clear the user and open sign-in. On 429/503, toast what to do. `core/error-text.ts` distinguishes insufficient funds, blockhash failures, simulation failures, slow confirmation and offline errors. Generic wallet errors do **not** identify the selected network; preserve the original error in the browser console for diagnosis.
- The sign-in message names the domain `localhost:4200` in Development, matching `ng serve`, so Phantom shouldn't warn. If you serve on another port and it does, set the backend's `Auth:Domain` to the page host.

## Transactions

A `DealActionService` runs the flow in [api.md](api.md#the-transaction-flow-every-on-chain-action) for every on-chain action. The UI-specific parts:

```ts
const tx = Transaction.from(Uint8Array.from(atob(transaction), c => c.charCodeAt(0)));   // legacy tx, @solana/web3.js
const signature = await this.wallet.signAndSendTransaction(tx);                          // base58
```
The injected Phantom provider takes the `Transaction` object; `WalletService.signAndSendTransaction` is the only place that changes if a different wallet SDK wants raw bytes. Decoding with `atob` avoids needing a `Buffer` polyfill.

The provider receives `preflightCommitment: 'confirmed'`, matching the backend's blockhash commitment. Preflight remains enabled. A wallet using a different RPC can still be slow or unavailable; a successful server-side simulation does not prove Phantom's own simulation service is healthy.

- Show "Preparing transaction…" while connecting/building, then "Approve in Phantom…" when calling the wallet.
- Show "Confirming on Solana…" while sync runs (up to 30 s), then replace the page state with the returned deal.
- If the user rejects in the wallet, do nothing.
- Buy: `/l/:id` → Buy → (sign in if needed) → `POST /api/deals` → wallet → sync → navigate to `/d/{deal.id}`.

## Money and notifications

- Format amounts with `BigInt` and `mintDecimals`. Parse user input (`"15.5"` → `"15500000"`), reject more than 6 decimals, and check `price % milestones === 0n` before submitting.
- Show the token as "USDC (test)" or `$`; on devnet it's our own test stablecoin.
- Notifications: badge = unread count on the first page; text per `kind`; click → mark read and open `/d/{dealId}`.

## Dev setup

1. Backend: from `backend/`, `dotnet run --project colosseumbackend` (`http://localhost:5107`).
2. Frontend: from `frontend/`, `npm ci` once, then `npm start` (`ng serve` on `http://localhost:4200`). `angular.json` points `ng serve` at `proxy.conf.json` (the API has no CORS; the proxy keeps the cookie same-origin):
   ```json
   { "/api": { "target": "http://localhost:5107", "secure": false, "changeOrigin": false } }
   ```
   Production build: `npx ng build` → `dist/frontend/browser/`, which the backend serves from its `wwwroot` when hosted (see [`deploy/README.md`](../../deploy/README.md)).
3. Develop in Chrome, Edge or Firefox: they accept the `Secure` session cookie on `http://localhost`; Safari doesn't.
4. **Two wallets per deal** (buyer ≠ seller): two Phantom accounts or two browser profiles. Each needs test tokens and devnet SOL: send the addresses to Sandro, who runs `Admin fund <wallet>` (100 tokens + 0.05 SOL).
5. **Demo:** a listing with the `2m` deadline lets you show buy → accept → missed deadline → refund plus the seller's deposit live (covered by an end-to-end test).

## Verification

Wallet-flow regression tests: run `npm run test:wallet` from `frontend/`. These exercise the real services with a stub provider; they do not replace a check with the actual Phantom extension. `npm run build` checks Angular templates and production compilation. The older `npm test` script has no configured Angular test target.

## Out of scope for the alpha

Chat (the buyer note plus Discord handles cover contact), listing images, search ranking, and dispute resolution beyond splits.
