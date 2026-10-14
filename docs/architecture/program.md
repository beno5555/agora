# Solana program (Anchor)

Anchor program, written in Rust, deployed to devnet. It holds each deal's payment and both deposits in a vault it controls, and releases them only by the rules below. Nobody has admin power to move deal funds, including us.

## Accounts

| Account | Seeds | Fields |
|---|---|---|
| `Config` | `["config"]` | `admin`, `mint`, `treasury` (token account for fees), `fee_bps` (200 = 2%), `deposit` (formula parameters, see below), `bump` |
| `Deal` | `["deal", buyer, deal_id: u64 LE]` | see below |
| Vault | `["vault", deal]` | token account for `mint`, authority = `Deal` PDA |
| `Reputation` | `["reputation", wallet]` | `wallet`, `as_buyer`, `as_seller`: one `RoleRecord` per role (below) |

**`RoleRecord`**, kept separately for buying and selling, so buying a lot doesn't lower a wallet's deposit as a seller: `completed` (deals finished by full confirmation or an accepted split), `disputed` (how many of those had a disagreement), `undelivered` (as seller: deals accepted but not delivered in time), `open_disputes` (disagreements not settled yet), `volume` (total price of completed deals).

The first version kept one combined record at `["rep", wallet]`. Those accounts are no longer read, so every wallet's record restarted from zero with the per-role version (Oct 14).

The buyer is part of the `Deal` seed so that nobody else can claim a `deal_id` first and block a buyer's deal. The backend picks `deal_id` (a random u64).

### `Deal` fields
| Field | Type | Meaning |
|---|---|---|
| `deal_id` | u64 | backend-chosen id |
| `buyer`, `seller` | Pubkey | parties |
| `price` | u64 | total price in token base units |
| `milestones` | u16 | number of parts (matches/hours); `price % milestones == 0` |
| `milestones_released` | u16 | parts already paid to the seller |
| `buyer_deposit`, `seller_deposit` | u64 | worked out by the program at creation (see Deposits) |
| `delivery_window` | i64 | seconds the seller has after accepting to mark it delivered; 60 s to 30 days |
| `accepted_at`, `delivered_at` | i64 | timestamps (0 = not yet) |
| `status` | enum | see below |
| `disputed` | bool | a disagreement was opened at some point |
| `split_offer` | Option<SplitOffer> | `{ by: Pubkey, buyer_amount: u64 }` |
| `bump`, `vault_bump` | u8 | |

## Deposits

Nobody enters deposits. `create_deal` works out each party's deposit from **their own** record **in their role on this deal** (the buyer's `as_buyer`, the seller's `as_seller`) and the price, using the parameters in `Config.deposit` (all basis points of the price). The code is `DepositParams::rate_bps` in `state.rs`:

```
rate = base − discount + penalty, kept within [floor, cap]
  discount = min(max_discount, discount_per_volume × completed volume ÷ price)
  penalty  = penalty × bad marks
  bad marks = undelivered deals + disagreements still open
deposit = price × rate
```

| Parameter | Devnet value | Meaning |
|---|---|---|
| `base_bps` | 2000 (20%) | a wallet with no history |
| `discount_per_volume_bps` | 150 (1.5%) | per 1× *this deal's price* of completed volume |
| `max_discount_bps` | 1500 (15%) | |
| `penalty_bps` | 1000 (10%) | per bad mark |
| `floor_bps` / `cap_bps` | 500 (5%) / 5000 (50%) | |

Examples for a $15 deal: new wallet $3.00; after $15 completed $2.78; after $150 completed $0.75 (floor). The same $150 record on a $1,500 deal only takes 1.5% off. Since the discount is measured against the price, a record built on small deals barely helps on a big one.

**Open disagreements count against both sides.** Nobody can tell who is at fault, so `raise_dispute` adds an open mark to the buyer's `as_buyer` and the seller's `as_seller`, and closing the deal (confirm, split or seller refund) removes it. Until then both pay 10% more on their next deals in that role, so leaving a disagreement unsettled costs both sides. This is also the only buyer-side penalty: `undelivered` can only happen to sellers.

`update_config` (admin only) can retune the numbers. That affects only deals created afterwards; existing deals keep their deposits.

**Known limits (alpha):**
- **New wallet = fresh start.** A seller with penalties can abandon the wallet and start again at the base rate. Penalties only bite while the wallet's discount is worth keeping. Fixing this needs identity, which is out of scope.
- **Volume can be farmed** by trading with yourself. It costs the 2% fee on the farmed volume, and the impact is small: deposits only cover non-delivery and wasted time, because the price itself is always locked in the vault and can't be taken.
- **Finished disputes aren't penalised,** because the program can't tell who was right. Only *open* ones count (against both sides). Revisit once dispute resolution exists.
- **A buyer who never confirms** after delivery isn't penalised: the deal is just locked. That needs a review time limit (see PLAN.md).

## Deal states

```
            create_deal (buyer pays price + deposit)
                     │
                     ▼
  cancel ◀──── AwaitingSeller
 (buyer)             │ accept_deal (seller pays deposit)
                     ▼
               Active ───────── deadline passed ──▶ refund_undelivered ──▶ Undelivered
                │    │ mark_delivered (seller, before the deadline)
                │    ▼
                │  Delivered ── confirm (all parts) ──▶ Completed
                │    │
                │    │ raise_dispute (either party)
                ▼    ▼
               Disputed ── accept_split / confirm / seller_refund ──▶ Split / Completed / SellerRefunded
                (stays locked until the two agree)
```

There's no review time limit. After delivery, the money moves only when the buyer confirms, a split is accepted, or the seller refunds. A silent buyer leaves it locked. Dispute handling will be designed later.

`seller_refund` (seller gives everything back) is allowed from any open state. Finished deals close their `Deal` and vault accounts (rent goes back to the buyer). Only open states exist on-chain; the outcome (`Completed`, `Split`, `Undelivered`, `SellerRefunded`, `Cancelled`) is reported in the `DealClosed` event.

## Instructions

| Instruction | Signer | Allowed when | Effect |
|---|---|---|---|
| `init_config(fee_bps, deposit)` | admin | once | creates `Config` |
| `update_config(fee_bps, deposit)` | admin | any time | retunes the fee and deposit formula for new deals |
| `create_deal(deal_id, {seller, price, milestones, delivery_window})` | buyer | n/a | works out both deposits; creates `Deal` and vault; moves `price + buyer_deposit` in; → `AwaitingSeller` |
| `cancel_deal` | buyer | `AwaitingSeller` | refunds the buyer; closes |
| `accept_deal` | seller | `AwaitingSeller` | moves `seller_deposit` in; sets `accepted_at`; → `Active` |
| `mark_delivered` | seller | `Active`, before the deadline | sets `delivered_at`; → `Delivered` |
| `confirm(parts)` | buyer | `Active`, `Delivered` or `Disputed` | pays the seller `parts × price/milestones` minus the fee; after the last part → `Completed` |
| `refund_undelivered` | buyer | `Active`, `now > accepted_at + delivery_window` | buyer gets the remaining price, their deposit **and the seller's deposit**; → `Undelivered` |
| `raise_dispute` | buyer or seller | `Active` or `Delivered` | → `Disputed`; adds an open disagreement to both parties' records (created if missing; the caller pays the rent) |
| `propose_split(buyer_amount)` | buyer or seller | `Disputed` | stores/replaces `split_offer` |
| `accept_split` | the other party | `Disputed` with an offer | buyer gets `buyer_amount`; seller gets the rest minus the fee; → `Split` |
| `seller_refund` | seller | any open state | buyer gets the remaining price; everyone's deposit returned; → `SellerRefunded` |

`create_deal` also takes both parties' `Reputation` addresses (they may not exist yet for new wallets). Allowing `confirm` in `Active` lets service buyers confirm match by match before the seller marks everything delivered.

**On every close:** deposits go back to their owners, except in `refund_undelivered`. Then the buyer's `as_buyer` and the seller's `as_seller` are updated (accounts created on first use with `init_if_needed`; a disagreement on the deal stops counting as open) and the `Deal` and vault accounts are closed.

**Fee:** `fee_bps` of every amount paid to the seller goes to `Config.treasury`. Buyers' refunds pay no fee.

**Events:** `DealCreated` (with both deposits), `DealAccepted`, `DealDelivered`, `PartsConfirmed`, `DealDisputed`, `SplitProposed`, `DealClosed`, for the backend to index.

## Delivery deadlines
The program accepts any `delivery_window` from 60 seconds to 30 days. The backend limits sellers to presets (15m to 7d, plus 2m on devnet for demos; see [api.md](api.md#config)).

## Decided
- **Disputes that never end:** a `Disputed` deal stays locked forever (team decision for the alpha).
- **Seller's deposit on non-delivery:** goes to the buyer as compensation (team decision).
- **Deposits are dynamic** (team decision): set by the program from reputation and price; never entered by users.
- **No review time** for now (team decision).

## Build and test
Code is in `program/`. Setup and commands: [program/README.md](../../program/README.md).
