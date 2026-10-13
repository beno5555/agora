# Submission drafts

Drafts for the Colosseum form's three missing fields, written by Claude on Oct 13 from the repo and the live site. **Rewrite them in the team's own words before pasting.** Placeholders are in `<angle brackets>`. Progress and next steps are in [`STATUS.md`](STATUS.md).

| Field | State |
|---|---|
| Access instructions (`liveProductAccessInstructions`) | Draft below, with Sandro's Telegram (@sandrobenashvili; his Discord may be `.ycuber`, unconfirmed) |
| Important context about the repo | Draft below |
| Demo video | **Planned** ([below](#technical-demo-plan-oct-13-0545-utc)); record on the hosted site |
| Repo `README.md` | Claude's older draft; rewrite it. Stale points listed at the bottom |

## Access instructions (draft)

> Agora runs on Solana devnet with our own test stablecoin, so trying it costs nothing.
>
> 1. Install Phantom: the browser extension (Chrome, Brave, Firefox) from https://phantom.com/download, or the Phantom app on a phone.
> 2. In Phantom, open Settings → Developer Settings, turn on **Testnet Mode** and choose **Solana Devnet**.
> 3. Send your Phantom wallet address to Sandro on Telegram: **@sandrobenashvili**. He sends you 100 test USDC and 0.05 devnet SOL for network fees. The app can't fund wallets by itself: our backend holds no keys.
> 4. Open https://46-101-154-81.sslip.io and click **Connect**. Signing in is a message signature, not a transaction, and costs nothing. On a phone, Connect reopens the site inside the Phantom app's browser.
> 5. Buy any listing. Phantom shows our test token as an unknown token; that's expected, it's our own devnet stablecoin.
>
> The demo sellers (NovaCarry, LootSmith, RiftCoach) are run by our team, so a new deal waits until one of us accepts it. Message @sandrobenashvili on Telegram if it waits too long. To see the missed-deadline rule, buy **"Demo: 2-minute deadline (try the refund)"**: once the seller accepts, they have 2 minutes to deliver; if they don't, the deal page lets you take back your payment plus the seller's deposit, without asking anyone.
>
> You can also sell: create a listing on **Sell**, then buy it from a second Phantom wallet.

## Important context about the repo (draft)

> **What it is:** Agora is a peer-to-peer marketplace for gaming services, items and in-game currency where buyer and seller both lock a deposit with every deal. If they disagree, the money stays locked until they agree or one accepts the other's split offer. Nobody outside decides, and Agora never holds the funds.
>
> **Where to look:**
> - `program/`: the Solana program (Anchor). It holds each deal's payment and both deposits in a program-owned vault and enforces every rule: create and fund a deal, accept with the seller's deposit, mark delivered, confirm part by part (a 4-rank boost pays out per rank), raise a disagreement, offer and accept a split, refund plus the seller's deposit after a missed deadline, cancel before acceptance, seller refund. Deposits are set by the program from each wallet's on-chain record, and every closed deal updates that record. A 2% fee is taken only from what the seller is paid.
> - `backend/`: a .NET 10 API. It builds **unsigned** transactions for the user's wallet to sign and holds no keys. It stores what doesn't belong on-chain (listings, profiles, notifications) in SQLite and reads deal state back from the chain. Sign-in is a signed message (Sign-In With Solana style). `colosseumbackend.Admin` is the deploy-wallet tool that set up the program and funds demo wallets.
> - `frontend/`: the Angular 21 web app, with Phantom.
> - `docs/`: the product plan (`PLAN.md`) and the technical design (`architecture/`).
>
> **Tested:** 8 program tests (LiteSVM) and 15 end-to-end API tests against the deployed program on devnet: a deal paid match by match, a dispute settled by a split, a missed deadline refunded with the seller's deposit, access checks, and sign-in replay and impostor signatures.
>
> **Deployed:** program `J4fL4Q8nnndro2xmVdCayRt5apXhexkkVQoqqwf4AKi4` on devnet; live app at https://46-101-154-81.sslip.io.
>
> **Limits:** devnet only, with a test stablecoin; the program has not been audited. A deal whose two sides never agree stays locked: resolving disagreements beyond split offers is future work.
>
> **Built during the hackathon:** the repo starts on Oct 11, 2026. <Team decision: say that the team built it with Claude Code, which wrote the Solana program since nobody on the team writes Rust. The repo's `CLAUDE.md` already shows this.>

## Technical demo plan (Oct 13, 05:45 UTC)

**Time is the constraint:** the form closes at 06:59 UTC (10:59 Tbilisi). Upload the video by **06:40 UTC** at the latest. While Sandro records, a teammate pastes the two text fields above into the form.

**Goal:** about 3 minutes (check the form's own limit), recorded on https://46-101-154-81.sslip.io. It shows one real deal going through every rule, then the missed-deadline refund, then what runs on Solana. Your first take is your rehearsal: if it works, keep it.

**Setup (10 min):**
1. **Hide test listings.** Signed in as Dealer1 (`CwjS…9HYJ`), pause "aa" and "service test 1" on `/me`: they show on the public home page right now.
2. **Two browser windows side by side:** left = buyer (Dealer1: 99.3 test USDC, 0.046 SOL), right = seller **NovaCarry** (100 test USDC, 0.05 SOL; key in `sellers.json`). Easiest: a second Chrome profile with Phantom and only NovaCarry imported. Fallback: one Phantom, switch account and Connect again (slower on video).
3. Both Phantoms on **Devnet**, both signed in to the site. Browser zoom 110–125% so text reads on video. Close other tabs and notifications.
4. **Start the 2-minute deal now, before recording:** buyer buys "Demo: 2-minute deadline (try the refund)" ($2), seller accepts. Its deadline will have passed by the time you reach that part of the video.
5. Recorder: **Loom** (Sandro's choice), **full screen** mode so the Phantom popups and both windows are captured; camera bubble off or in a corner away from the buttons. The free plan stops at 5 minutes.
6. Both windows on exactly `https://46-101-154-81.sslip.io`: a localhost tab makes Phantom refuse sign-in ("domain in the sign-in message does not match").

**Script** (the main deal is "Ranked boost, Gold IV to Platinum IV": Apex, NovaCarry, $24, paid in 4 ranks of $6):

| Time | Screen | What to say |
|---|---|---|
| 0:00–0:15 | Home page | "Agora is a P2P marketplace for game services, items and currency on Solana. Buyer and seller both lock a deposit, and nobody outside the deal decides who's right." |
| 0:15–0:40 | Buyer: listing page → **Buy · lock $…** → Phantom → deal page | "The deposit isn't typed in by anyone: the program sets it from my on-chain record and the price, about 20% for a new wallet. One transaction puts my payment and deposit into a vault the program owns." The vault animates. |
| 0:40–1:00 | Seller: deal page → **Accept & lock deposit** → Phantom | "The seller accepts and locks their own deposit. The delivery deadline starts now." Both deposits now show in the vault. |
| 1:00–1:20 | Buyer: stepper to 2 ranks → **Confirm 2 ranks** → Phantom | "I pay part by part: two ranks done, so $12 goes to the seller, minus a 2% fee taken only from what the seller is paid." |
| 1:20–1:30 | Click **Last transaction** (On Solana panel) → Explorer | "Every step is a real devnet transaction; here are the token transfers out of the vault." |
| 1:30–2:00 | Buyer: **Disagree** → **Yes, disagree**. Seller: **Offer a split** $6 → **Offer this split**. Buyer: **Accept their offer** | "Say we disagree about the rest. The $12 left stays locked: no support team, no moderator. The seller offers $6 back, I accept, and the program pays it out." Timeline: "Closed with an agreed split". |
| 2:00–2:25 | Buyer: the 2-minute deal from setup step 4 → **Take refund + seller's deposit** | "If a seller accepts and misses the deadline, the buyer takes back the payment plus the seller's deposit, without asking anyone." |
| 2:25–2:55 | GitHub repo: `program/programs/agora/src/lib.rs`, then `backend/` | "The rules live in an Anchor program: deals, deposits from on-chain reputation, part payments, splits, deadline refunds. The .NET backend only builds unsigned transactions and holds no keys, so Agora never touches user funds. 8 program tests and 15 end-to-end tests on devnet." |
| 2:55–3:00 | Live link | "Try it at 46-101-154-81.sslip.io: steps are in our submission." |

**Waits:** each action is a Phantom approval plus up to ~30 s for confirmation. Pause Loom after approving in Phantom and resume once the page has updated.

**After recording:** trim the start and end in Loom, set it to "Anyone with the link can view", check the link plays in a private window, paste it into the form.

**If something breaks while recording:** a 400 "Transaction Not Confirmed" usually means slow devnet: wait, reload the deal page and carry on. If Buy fails, check that Phantom is on Devnet. Don't debug on camera: stop, fix, re-record that part.

## Demo: inputs for planning

What the plan above was built from:
- **Live demo data:** 11 listings from three demo sellers across all six games, each seller funded with 100 test tokens + 0.05 SOL. Their keys are in `C:\Users\admin\.agora-demo\sellers.json` on Sandro's PC only (import a seller into Phantom: Add account → Import private key with `phantomPrivateKey`). A demo needs a buyer wallet and a seller wallet in Phantom, both funded.
- **What shows the product best:** the vault locking both deposits after Buy and Accept (it animates), confirming part by part, a disagreement with a split offer, and the 2-minute missed-deadline refund. Each step links to the transaction on Solana Explorer.
- **Not yet run end to end on the hosted site:** the full path (list → buy → accept → deliver → confirm) and the `2m` refund. Do it once before recording.

## README: stale points to fix in the rewrite

- Steam items are **in scope** (Sandro, Oct 13). The "Status and limits" line was corrected on Oct 13: they settle like any other item for now, and waiting out Steam's 7-day reversal window is future work. Keep that in the rewrite.

- The web app table row says "(in progress)" and "Phantom Connect": the app is finished and uses the Phantom browser extension (on phones, the Phantom app's browser).
- The Team table says Dato is the "Designer"; `CLAUDE.md` says Dato's role isn't set yet.
- There is no live link yet: add https://46-101-154-81.sslip.io and the access steps above.
- "Run it locally" sends readers to `frontend.md` for the frontend; one line is enough: `cd frontend && npm ci && npm start` (with the API running).
