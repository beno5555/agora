# Submission texts

The Colosseum form's missing fields, written by Claude on Oct 13 from the repo and the live site. The two text fields below are the **final versions** (Oct 13, ~06:00 UTC), in plain text because the form may not render formatting: read them through before pasting. Progress and next steps are in [`STATUS.md`](STATUS.md).

| Field | State |
|---|---|
| Access instructions (`liveProductAccessInstructions`) | Final text below (297 of the field's 300 characters), with Sandro's Telegram (@sandrobenashvili) |
| Important context about the repo | Final text below (468 of the field's 500 characters). It says Claude Code wrote the Solana program; delete those lines if the team decides otherwise |
| Demo video | **Planned** ([below](#technical-demo-plan-oct-13-0545-utc)); recorded with Loom on the hosted site |
| Repo `README.md` | Claude's older draft; rewrite it. Stale points listed at the bottom |

## Access instructions

**The field takes 300 characters at most**; this is 297. It keeps the steps a judge needs and that our team accepts deals; the 2-minute refund listing is shown in the demo video instead.

```
Free on Solana devnet. 1) Install Phantom; in Settings > Developer Settings turn on Testnet Mode, Solana Devnet. 2) Send your address to @sandrobenashvili on Telegram for test USDC and SOL. 3) Open https://46-101-154-81.sslip.io, click Connect, buy a listing. Our team accepts deals as the seller.
```

## Important context about the repo

The form asks: "Please share any important context about your repo. For example, if it only represents part of the product, or includes elements of unrelated products." **The field takes 500 characters at most**; this is 468.

```
The whole product, built during the hackathon (from Oct 11). program/: Anchor program that enforces every rule; a program-owned vault holds each payment and both deposits. backend/: .NET API that builds unsigned transactions and holds no keys. frontend/: Angular app with Phantom. deploy/, docs/: hosting and design. Not product code: .claude/skills/ (third-party dev tools) and CLAUDE.md; Claude Code wrote the Rust program. Devnet only, test stablecoin, not audited.
```

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
