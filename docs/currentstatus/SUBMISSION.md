# Submission texts

The texts for the Colosseum form fields that needed writing, final since Oct 13 (~06:00 UTC), in plain text because the form may not render formatting, and how the demo video was set up. Progress and next steps are in [`STATUS.md`](STATUS.md).

| Field | State |
|---|---|
| Access instructions (`liveProductAccessInstructions`) | Final text below (297 of the field's 300 characters), with Sandro's Telegram (@sandrobenashvili). Its sslip.io link now redirects to https://agora-market.duckdns.org |
| Important context about the repo | Final text below (468 of the field's 500 characters). It says Claude Code wrote the Solana program |
| Demo video | Recorded on the hosted site (2:52). [Setup below](#technical-demo-setup) |
| Repo `README.md` | Rewritten for judges on Oct 13: live link, how to try it, the repo map, tests and limits |

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

**What it shows:** one deal on the live site from buying to getting paid and what happens on Solana at each step, then the missed-deadline refund and how it is built. Under 3 minutes.

**Setup:**
1. **Hide test listings.** Signed in as Dealer1 (`CwjS…9HYJ`), pause "aa" and "service test 1" on `/me` if they are still public.
2. **Two browser windows side by side**, both on exactly `https://agora-market.duckdns.org` (a localhost tab makes Phantom refuse sign-in): left = buyer (Dealer1: 99.3 test USDC, 0.046 SOL on Oct 13), right = seller **NovaCarry** (100 test USDC, 0.05 SOL; key in `sellers.json`). Easiest: a second Chrome profile with Phantom and only NovaCarry imported.
3. Both Phantoms on **Devnet**, both signed in. Browser zoom 110–125%. Close other tabs and notifications.
4. **Start the 2-minute deal a few minutes before recording:** buyer buys "Demo: 2-minute deadline (try the refund)" ($2), seller accepts.
5. Buyer window on the home page; the listing "Ranked boost, Gold IV to Platinum IV" (Apex, NovaCarry, $24, 4 ranks of $6) one click away. A tab with the GitHub repo open at `program/programs/agora/src/lib.rs`.

**After recording:** trim long pauses, upload it, make the link viewable by anyone with it, check it plays in a private window.

## Demo: inputs for planning

What the setup above was built from:
- **Live demo data:** 11 listings from three demo sellers across all six games, each seller funded with 100 test tokens + 0.05 SOL. Their keys are in `C:\Users\admin\.agora-demo\sellers.json` on Sandro's PC only (import a seller into Phantom: Add account → Import private key with `phantomPrivateKey`). A demo needs a buyer wallet and a seller wallet in Phantom, both funded.
- **What shows the product best:** the vault locking both deposits after Buy and Accept (it animates), confirming part by part, a disagreement with a split offer, and the 2-minute missed-deadline refund. Each step links to the transaction on Solana Explorer.
- **Run end to end on the hosted site** for the demo video (Oct 13): a deal from buying to getting paid, and the `2m` missed-deadline refund.
