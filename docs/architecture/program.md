# Solana program (Anchor)

Anchor program, written in Rust, deployed to devnet. It holds each deal's payment and both deposits in a vault it controls, and releases them only by the rules below. Nobody has admin power to move deal funds, including us.

## Accounts

| Account | Seeds | Fields |
|---|---|---|
| `Config` | `["config"]` | `admin`, `mint`, `treasury` (token account for fees), `fee_bps` (200 = 2%), `min_deposit_bps`, `bump` |
| `Deal` | `["deal", buyer, deal_id: u64 LE]` | see below |
| Vault | `["vault", deal]` | token account for `mint`, authority = `Deal` PDA |
| `Reputation` | `["rep", wallet]` | `wallet`, `deals_completed`, `deals_disputed`, `deals_refunded`, `volume`, `bump` |

The buyer is part of the `Deal` seed so that nobody else can claim a `deal_id` first and block a buyer's deal. The backend picks `deal_id` (a random u64).

### `Deal` fields
| Field | Type | Meaning |
|---|---|---|
| `deal_id` | u64 | backend-chosen id |
| `buyer`, `seller` | Pubkey | parties |
| `price` | u64 | total price in token base units |
| `milestones` | u16 | number of parts (matches/hours); `price % milestones == 0` |
| `milestones_released` | u16 | parts already paid to the seller |
| `buyer_deposit`, `seller_deposit` | u64 | each ≥ `price * min_deposit_bps / 10_000` |
| `delivery_window` | i64 | seconds the seller has after accepting to mark it delivered |
| `review_period` | i64 | seconds the buyer has after delivery to confirm or dispute |
| `accepted_at`, `delivered_at` | i64 | timestamps (0 = not yet) |
| `status` | enum | see below |
| `split_offer` | Option<SplitOffer> | `{ by: Pubkey, buyer_amount: u64 }` |
| `bump`, `vault_bump` | u8 | |

## Deal states

```
            create_deal (buyer pays price + deposit)
                     │
                     ▼
  cancel ◀──── AwaitingSeller
 (buyer)             │ accept_deal (seller pays deposit)
                     ▼
               Active ───────── deadline passed ──▶ refund_undelivered ──▶ Refunded
                     │ mark_delivered (seller)
                     ▼
               Delivered ── confirm (all parts) ─────────────▶ Completed
                │    │
                │    └── review period passed ──▶ claim_after_review ──▶ Completed
                │ raise_dispute (buyer, during review)
                ▼
               Disputed ── accept_split / confirm / seller_refund ──▶ Completed / Refunded
                (stays locked until the two agree)
```

`seller_refund` (seller gives everything back) is allowed from any open state. Closed deals return the rent to the buyer by closing the `Deal` and vault accounts.

## Instructions

| Instruction | Signer | Allowed when | Effect |
|---|---|---|---|
| `init_config` | admin | once | creates `Config` |
| `create_deal(deal_id, seller, price, milestones, buyer_deposit, seller_deposit, delivery_window, review_period)` | buyer | n/a | creates `Deal` and vault; moves `price + buyer_deposit` in; → `AwaitingSeller` |
| `cancel_deal` | buyer | `AwaitingSeller` | refunds the buyer; closes |
| `accept_deal` | seller | `AwaitingSeller` | moves `seller_deposit` in; sets `accepted_at`; → `Active` |
| `mark_delivered` | seller | `Active`, before deadline | sets `delivered_at`; → `Delivered` |
| `confirm(parts)` | buyer | `Active`, `Delivered` or `Disputed` | pays the seller `parts × price/milestones` minus the fee; after the last part → `Completed` |
| `claim_after_review` | seller | `Delivered`, `now ≥ delivered_at + review_period` | pays the seller the rest minus the fee; → `Completed` |
| `refund_undelivered` | buyer | `Active`, `now > accepted_at + delivery_window` | buyer gets the remaining price, their deposit **and the seller's deposit**; → `Refunded` |
| `raise_dispute` | buyer | `Delivered`, within the review period | → `Disputed` (stops `claim_after_review`) |
| `propose_split(buyer_amount)` | buyer or seller | `Disputed` | stores/replaces `split_offer` |
| `accept_split` | the other party | `Disputed` with an offer | buyer gets `buyer_amount`; seller gets the rest minus the fee; → `Completed` |
| `seller_refund` | seller | any open state | buyer gets the remaining price; everyone's deposit returned; → `Refunded` |

Allowing `confirm` in `Active` lets service buyers confirm match by match before the seller marks everything delivered.

**On every close (`Completed`/`Refunded`):** deposits go back to their owners, except in `refund_undelivered`. Then `Reputation` for both wallets is updated (created on first use with `init_if_needed`) and the `Deal` and vault accounts are closed.

**Fee:** `fee_bps` of every amount paid to the seller goes to `Config.treasury`. Buyers' refunds pay no fee.

**Events:** each instruction emits an Anchor event (`DealCreated`, `DealAccepted`, `Delivered`, `Confirmed`, `Disputed`, `SplitProposed`, `DealClosed`) for indexing later.

## Open (from PLAN.md)
- **Disputes that never end:** a `Disputed` deal currently stays locked forever.
- **Seller's deposit on non-delivery:** this design gives it to the buyer as compensation. Confirm with the team.
- Deposit sizes and review periods per listing type.

## Build and test
Anchor CLI with Rust and the Solana CLI. On Windows, use WSL. TypeScript tests with `anchor test` on a local validator cover each path in the state diagram. The generated IDL (`target/idl/agora.json`) is copied to the frontend for its client.
