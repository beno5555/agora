# Agora product notes (started 2026-10-11, updated 2026-10-13)

The detailed product plan. Team decisions and Claude proposals are marked; proposals are not approved yet. What the hackathon alpha actually built is under [Alpha](#alpha-hackathon-plan-and-what-was-built). General project context (team, deadline, stack, commands) lives in `CLAUDE.md`. Don't repeat it here.

## Direction (stated by the team)
- Global product, not tied to any country. Built entirely on Solana; it must not work without it.
- P2P marketplace mainly for gaming items and in-game currencies, plus game-related services (e.g. paid Fortnite carries).

## Disagreements in the alpha (team decision)
- Both sides lock a deposit with each deal.
- If buyer and seller disagree, the payment and both deposits stay locked until they agree, or until one side accepts the other's offer to split. Nobody outside decides.
- Trade-off: an honest person's money can stay stuck if the other side refuses.

## Proposed settlement rules by listing type (Claude proposal)
| Type | Examples | Seller paid when |
|---|---|---|
| Services | Play-along carries, coaching, raid help | Buyer confirms per match/hour |
| In-game handovers | Currency, items traded in game | Buyer confirms |
| Steam items | CS2 skins | After the 7-day trade-reversal window, if item still in buyer's inventory |
| Codes (later) | Gift cards, keys | After a short complaint window |

- If the seller doesn't mark the deal delivered by its deadline, the buyer can take a refund (one click; nothing on Solana runs by itself).
- Either side can open a disagreement once the deal is under way.
- Deposits are set automatically from each party's on-chain record and the price: smaller as the record grows relative to the deal size, larger after missed deliveries. Nobody enters them. Formula: [architecture/program.md](architecture/program.md#deposits).

Not allowed: account sales; services that require logging into the buyer's account; currency a game bans selling or that can't be transferred (Fortnite V-Bucks can't be transferred or gifted between accounts).

Team decisions (2026-10-11):
- If the seller accepts but misses the delivery deadline, the buyer gets the refund **plus the seller's deposit**.
- If buyer and seller never agree, the funds stay locked forever (alpha).
- Deposits are dynamic (reputation + price); sellers don't enter them. More variables can be added later.
- ~~No review time limit for now.~~ Replaced on 2026-10-14 (Sandro): after delivery, the buyer has a review time (3 days) to confirm or raise a disagreement; if they do neither, the seller can claim the payment. A disagreement stops the clock. Dispute resolution will be designed later.

Team decision (2026-10-13, Sandro): **Steam items (CS2 skins) are in scope.** The alpha settles them like any other item: the buyer confirms. The 7-day trade-reversal rule proposed above is not built yet.

Open decisions: how disagreements get resolved; the deposit formula's exact numbers (devnet values in program.md).

## Fee
- About 2% per deal. Compare Eldorado.gg, where a $5 carry costs the buyer $5.70 and the seller gets $4.50 (see Evidence).

## Alpha (hackathon): plan and what was built
Built and live on devnet at https://agora-market.duckdns.org (Oct 13). State and next steps: [currentstatus/STATUS.md](currentstatus/STATUS.md).
- **Solana program (Anchor): built as planned.** Create and fund a deal (payment plus both deposits), mark delivered, confirm (fully or per milestone), refund if not delivered by the deadline, raise a disagreement, offer and accept a split, reputation per wallet. Devnet, with our own test stablecoin (CASH or USDC on mainnet later).
- **Backend (.NET): built.** Listings, game storefronts, deal pages, notifications, deadline reminders.
- **Frontend (Angular): built,** with one change: sign-in uses the **Phantom browser extension** (on phones, the Phantom app's browser), not Phantom Connect's email sign-in. Phantom Connect is in [BACKLOG.md](currentstatus/BACKLOG.md). Storefronts, the deal-link page, split offers and countdowns on the deal page are in.
- **Mate and Dato (proposed):** recruit 10–20 real carriers and coaches, run devnet test deals, collect quotes.

## Positioning (Claude's opinion)
- Traction should come from sellers first: fast payment, ~2% fee, no chargebacks, public record, deal links for their existing Discord/X/Reddit customers.
- Pitch order: problem with the $5 carry example -> demo -> "what if someone lies?" (the money stays locked, so lying costs the liar too) -> why only on Solana -> traction -> business model -> team of four with roles.

## Evidence collected
- Eldorado.gg fees: seller 10% standard on items/accounts/boosting (up to 30% for some games); buyer 8% + $0.30. A $5 carry: buyer pays $5.70, seller gets $4.50. https://intercom.help/eldoradogg/en/articles/8409025-fees
- Eldorado TradeShield: the buyer has up to 3 days to confirm; for some item types a warranty period follows (e.g. 10 days for Fortnite custom items, 14 for GTA5/Roblox limiteds custom items). The seller is paid when the order completes. https://intercom.help/eldoradogg/en/articles/8408994
- Steam trade protection (CS2, since 16 July 2025): a trade can be reversed for 7 days, so a seller could take a sold skin back. https://csdb.gg/cs2-trade-protection-update/ and https://www.pcworld.com/article/2851576/steam-thwarts-counter-strike-scammers-with-new-trade-reversal-system.html
- Epic disables bought or shared Fortnite accounts. https://www.fortnite.com/news/it-is-a-competitive-violation-to-buy-a-fortnite-account-for-sale-or-to-share-an-account
- V-Bucks can't be transferred between Epic accounts or gifted. https://www.epicgames.com/help/c-202300000001645/c-202300000001754/a202300000014473 and https://www.epicgames.com/help/c-34254770/c-39884448/a26118196
- Some CS2 skin sites charge sellers only about 2% (CSFloat 2%; DMarket 2% plus a 2.5% buyer fee; Waxpeer about 2-3%), so Agora's fee advantage is in services and currency, not skins.
- Colosseum projects in this space (from their project pages):
  - P2P Loot (Frontier): Solana escrow for MMO trades + guild tools; 1 listed member; says 250 active users, 4 paying guilds; no prize listed.
  - MMO Nexus (Frontier): escrow released when both confirm; 1 listed member; no traction stated; no prize listed.
  - Skinana (Breakout): Steam skins traded via zkTLS web proofs + AI analytics; 1 listed member; no prize listed.
  - Huch (Breakout): borrow against CS2 skins; 4 members; University Prize, Gaming track.
  - Scrim (Breakout): skill-based wagers on matches; 3 members; 3rd place, Gaming track.
  - Other escrow projects, no prize listed: Nector, OPINSPACE.IO, Vigent Escrow, Solia Network.
- Crypto World's Fair has no gaming track (tracks are by blockchain), so Agora competes against all categories. Rules judging criteria: functionality/code quality, potential impact, novelty, UX, open source/composability, business plan and team execution.
- Sponsor recommendations: Phantom Connect for wallets, CASH as the stablecoin. https://colosseum.com/worldsfair/resources

## Set aside
- A "digital accountant" for expenses and taxes: hard to justify a blockchain, and many similar projects already exist.