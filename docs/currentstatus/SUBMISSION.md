# Submission texts

The Colosseum form's missing fields, written by Claude on Oct 13 from the repo and the live site. The two text fields below are the **final versions** (Oct 13, ~06:00 UTC), in plain text because the form may not render formatting: read them through before pasting. Progress and next steps are in [`STATUS.md`](STATUS.md).

| Field | State |
|---|---|
| Access instructions (`liveProductAccessInstructions`) | Final text below (297 of the field's 300 characters), with Sandro's Telegram (@sandrobenashvili) |
| Important context about the repo | Final text below (468 of the field's 500 characters). It says Claude Code wrote the Solana program; delete those lines if the team decides otherwise |
| Demo video | Not in the Colosseum submission. Recorded afterwards on the hosted site (2:52). [Setup below](#technical-demo-setup) |
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

## Technical demo setup

**What it shows:** one deal on https://46-101-154-81.sslip.io from buying to getting paid and what happens on Solana at each step, then the missed-deadline refund and how it is built. Under 3 minutes.

**Setup:**
1. **Hide test listings.** Signed in as Dealer1 (`CwjS…9HYJ`), pause "aa" and "service test 1" on `/me` if they are still public.
2. **Two browser windows side by side**, both on exactly `https://46-101-154-81.sslip.io` (a localhost tab makes Phantom refuse sign-in): left = buyer (Dealer1: 99.3 test USDC, 0.046 SOL on Oct 13), right = seller **NovaCarry** (100 test USDC, 0.05 SOL; key in `sellers.json`). Easiest: a second Chrome profile with Phantom and only NovaCarry imported.
3. Both Phantoms on **Devnet**, both signed in. Browser zoom 110–125%. Close other tabs and notifications.
4. **Start the 2-minute deal a few minutes before recording:** buyer buys "Demo: 2-minute deadline (try the refund)" ($2), seller accepts.
5. Buyer window on the home page; the listing "Ranked boost, Gold IV to Platinum IV" (Apex, NovaCarry, $24, 4 ranks of $6) one click away. A tab with the GitHub repo open at `program/programs/agora/src/lib.rs`.

**After recording:** trim long pauses, upload it, make the link viewable by anyone with it, check it plays in a private window.

## Demo: inputs for planning

What the setup above was built from:
- **Live demo data:** 11 listings from three demo sellers across all six games, each seller funded with 100 test tokens + 0.05 SOL. Their keys are in `C:\Users\admin\.agora-demo\sellers.json` on Sandro's PC only (import a seller into Phantom: Add account → Import private key with `phantomPrivateKey`). A demo needs a buyer wallet and a seller wallet in Phantom, both funded.
- **What shows the product best:** the vault locking both deposits after Buy and Accept (it animates), confirming part by part, a disagreement with a split offer, and the 2-minute missed-deadline refund. Each step links to the transaction on Solana Explorer.
- **Not yet run end to end on the hosted site:** the full path (list → buy → accept → deliver → confirm) and the `2m` refund. Do it once before recording.

## README: stale points to fix in the rewrite

- Steam items are **in scope** (Sandro, Oct 13). The "Status and limits" line was corrected on Oct 13: they settle like any other item for now, and waiting out Steam's 7-day reversal window is future work. Keep that in the rewrite.

- The web app table row says "(in progress)" and "Phantom Connect": the app is finished and uses the Phantom browser extension (on phones, the Phantom app's browser).
- The Team table says Dato is the "Designer"; `CLAUDE.md` says Dato's role isn't set yet.
- There is no live link yet: add https://46-101-154-81.sslip.io and the access steps above.
- "Run it locally" sends readers to `frontend.md` for the frontend; one line is enough: `cd frontend && npm ci && npm start` (with the API running).
