# Submission texts

The Colosseum form's missing fields, written by Claude on Oct 13 from the repo and the live site. The two text fields below are the **final versions** (Oct 13, ~06:00 UTC), in plain text because the form may not render formatting: read them through before pasting. Progress and next steps are in [`STATUS.md`](STATUS.md).

| Field | State |
|---|---|
| Access instructions (`liveProductAccessInstructions`) | Final text below (297 of the field's 300 characters), with Sandro's Telegram (@sandrobenashvili) |
| Important context about the repo | Final text below (468 of the field's 500 characters). It says Claude Code wrote the Solana program; delete those lines if the team decides otherwise |
| Demo video | Not in the submission (Loom failed). Script rewritten as a natural workflow walkthrough ([below](#technical-demo-plan-rewritten-oct-13-after-the-deadline)) |
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

## Technical demo plan (rewritten Oct 13, after the deadline)

**What it is:** a walkthrough of one deal's workflow and what happens on Solana at each step. It explains how things work; benefits come up only in passing, since the pitch video covers them. About 2:30 of speech (358 words at a normal pace), under 3 minutes with clicks. Recorded on https://46-101-154-81.sslip.io. The first version, recorded with Loom by the deadline, failed: Loom's desktop app wouldn't record.

**Setup:**
1. **Hide test listings.** Signed in as Dealer1 (`CwjS…9HYJ`), pause "aa" and "service test 1" on `/me` if they are still public.
2. **Two browser windows side by side**, both on exactly `https://46-101-154-81.sslip.io` (a localhost tab makes Phantom refuse sign-in): left = buyer (Dealer1: 99.3 test USDC, 0.046 SOL on Oct 13), right = seller **NovaCarry** (100 test USDC, 0.05 SOL; key in `sellers.json`). Easiest: a second Chrome profile with Phantom and only NovaCarry imported.
3. Both Phantoms on **Devnet**, both signed in. Browser zoom 110–125%. Close other tabs and notifications.
4. **Start the 2-minute deal a few minutes before recording:** buyer buys "Demo: 2-minute deadline (try the refund)" ($2), seller accepts.
5. Buyer window on the home page; the listing "Ranked boost, Gold IV to Platinum IV" (Apex, NovaCarry, $24, 4 ranks of $6) one click away. A tab with the GitHub repo open at `program/programs/agora/src/lib.rs`.
6. Recorder: the Loom website (full-screen mode) or OBS. Read the script from a phone. Camera bubble off or in a corner away from the buttons.

**Script** (plain text, to read from a phone; ⏸ = pause the recording after approving in Phantom, resume once the page has updated):

```
1. INTRO
DO: Home page.
SAY: Hi, this is Agora. I'll walk you through one deal, from buying to getting paid, and show what happens on Solana at each step. On the left I'm the buyer, on the right the seller, both using Phantom on devnet.

2. BUY
DO: Buyer: open the Apex "Ranked boost" listing > Buy > approve in Phantom ⏸
SAY: Here's a listing: an Apex Legends rank boost, 24 dollars, paid in four ranks. When I click Buy, our backend builds a transaction and I sign it in Phantom. That creates the deal on-chain and moves my payment, plus a deposit, into a vault the program controls. The deposit is worked out from my on-chain history, so nobody types it in.

3. SELLER ACCEPTS
DO: Seller: open the deal (notification or My deals) > Accept & lock deposit > Phantom ⏸
SAY: Over on the seller's side, the deal shows up, and the seller accepts it. That locks the seller's deposit in the same vault and starts the delivery timer.

4. PAY PART BY PART
DO: Buyer: stepper to 2 > Confirm 2 ranks > Phantom ⏸
SAY: Let's say two of the four ranks are done. I confirm two ranks, and the program releases those two parts to the seller, minus a 2 percent fee. The rest stays locked.

5. ON SOLANA
DO: "Last transaction" link in the On Solana panel > Solana Explorer.
SAY: Each of these steps is a normal Solana transaction. This link opens it in the explorer, where you can see the tokens leaving the vault.

6. DISAGREE AND SPLIT
DO: Buyer: Disagree > Yes, disagree > Phantom ⏸
DO: Seller: type 6 under "Offer a split" > Offer this split > Phantom ⏸
DO: Buyer: Accept their offer > Phantom ⏸
SAY: Now say we don't agree about the last two ranks. I press Disagree, and the remaining money just stays where it is. Either side can offer a split. The seller offers 6 dollars back, I accept, and the program pays both of us and closes the deal.

7. MISSED DEADLINE
DO: Buyer: open the 2-minute deal > Take refund + seller's deposit > Phantom ⏸
SAY: One more case. I started this deal earlier, on a listing with a two-minute deadline. The seller accepted but never delivered. Once the deadline passed, a new button appeared for me: take the refund plus the seller's deposit. One signature, and it's back in my wallet.

8. HOW IT'S BUILT
DO: GitHub tab: program/programs/agora/src/lib.rs > then the backend folder.
SAY: Quickly, how it's built. All the rules live in this Anchor program: creating deals, deposits, part payments, splits and the deadline refund. The .NET backend stores listings and profiles, builds unsigned transactions for the wallet to sign, and reads the deal back from the chain after each step. It never holds any keys. The frontend is Angular with Phantom.

9. CLOSE
DO: Back to the live site.
SAY: That's the whole flow. You can try it yourself on devnet with the link in our submission. Thanks for watching.

IF SOMETHING BREAKS
- "Transaction Not Confirmed": stay paused, reload the deal page, resume.
- Buy fails: check that Phantom is on Devnet.
- Anything else: stay paused, fix it, resume.
```

**After recording:** trim the start and end, make the link viewable by anyone with it, check it plays in a private window.

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
