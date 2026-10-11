use anchor_lang::prelude::*;

pub const MAX_BPS: u16 = 10_000;

/// Shortest and longest delivery window a deal may have.
pub const MIN_DELIVERY_WINDOW: i64 = 60;
pub const MAX_DELIVERY_WINDOW: i64 = 30 * 24 * 3600;

#[account]
#[derive(InitSpace)]
pub struct Config {
    pub admin: Pubkey,
    /// The only stablecoin deals can use.
    pub mint: Pubkey,
    /// Token account (for `mint`) that receives fees.
    pub treasury: Pubkey,
    /// Fee on every amount paid to a seller, in basis points (200 = 2%).
    pub fee_bps: u16,
    pub deposit: DepositParams,
    pub bump: u8,
}

/// How each party's deposit is worked out from their on-chain record and the price.
/// All values are basis points of the price.
///
/// rate = base - min(max_discount, discount_per_volume × completed_volume / price)
///             + undelivered_penalty × deals_undelivered, clamped to [floor, cap]
///
/// A new wallet pays `base`. The discount grows with completed volume *relative
/// to this deal's price*, so a record built on small deals barely helps on a
/// large one.
#[derive(AnchorSerialize, AnchorDeserialize, Clone, Copy, PartialEq, Eq, InitSpace, Debug)]
pub struct DepositParams {
    pub base_bps: u16,
    pub floor_bps: u16,
    pub cap_bps: u16,
    /// Discount per 1× this price of completed volume.
    pub discount_per_volume_bps: u16,
    pub max_discount_bps: u16,
    /// Added per deal the wallet accepted as seller and never delivered.
    pub undelivered_penalty_bps: u16,
}

impl DepositParams {
    pub fn is_valid(&self) -> bool {
        self.floor_bps <= self.cap_bps
            && self.cap_bps <= MAX_BPS
            && self.base_bps <= MAX_BPS
            && self.max_discount_bps <= MAX_BPS
    }

    pub fn rate_bps(&self, record: Option<&Reputation>, price: u64) -> u64 {
        let (volume, undelivered) = record.map_or((0, 0), |r| (r.volume, r.deals_undelivered));
        let discount = (volume as u128 * self.discount_per_volume_bps as u128 / price as u128)
            .min(self.max_discount_bps as u128) as u64;
        let penalty = undelivered as u64 * self.undelivered_penalty_bps as u64;
        (self.base_bps as u64 + penalty)
            .saturating_sub(discount)
            .clamp(self.floor_bps as u64, self.cap_bps as u64)
    }

    pub fn amount(&self, record: Option<&Reputation>, price: u64) -> u64 {
        (price as u128 * self.rate_bps(record, price) as u128 / MAX_BPS as u128) as u64
    }
}

#[account]
#[derive(InitSpace)]
pub struct Deal {
    pub deal_id: u64,
    pub buyer: Pubkey,
    pub seller: Pubkey,
    pub mint: Pubkey,
    pub price: u64,
    pub milestones: u16,
    pub milestones_released: u16,
    /// Worked out by the program at creation; see `DepositParams`.
    pub buyer_deposit: u64,
    pub seller_deposit: u64,
    pub delivery_window: i64,
    pub accepted_at: i64,
    pub delivered_at: i64,
    pub status: DealStatus,
    /// True once either party has opened a disagreement; counted in both parties' reputation.
    pub disputed: bool,
    pub split_offer: Option<SplitOffer>,
    pub bump: u8,
    pub vault_bump: u8,
}

impl Deal {
    pub fn unit_price(&self) -> u64 {
        self.price / self.milestones as u64
    }

    /// Part of the price still in the vault.
    pub fn remaining(&self) -> u64 {
        self.price - self.unit_price() * self.milestones_released as u64
    }
}

/// Only open states exist on-chain; a finished deal's account is closed and
/// its outcome is reported in the `DealClosed` event.
#[derive(AnchorSerialize, AnchorDeserialize, Clone, Copy, PartialEq, Eq, InitSpace, Debug)]
pub enum DealStatus {
    AwaitingSeller,
    Active,
    Delivered,
    Disputed,
}

#[derive(AnchorSerialize, AnchorDeserialize, Clone, Copy, PartialEq, Eq, InitSpace, Debug)]
pub struct SplitOffer {
    pub by: Pubkey,
    /// What the buyer gets back out of the remaining price; the seller gets the rest.
    pub buyer_amount: u64,
}

#[derive(AnchorSerialize, AnchorDeserialize, Clone, Copy, PartialEq, Eq, Debug)]
pub enum Outcome {
    /// Buyer confirmed every part.
    Completed,
    /// Disagreement settled by an accepted split offer.
    Split,
    /// Seller missed the delivery deadline; buyer took the refund and the seller's deposit.
    Undelivered,
    /// Seller voluntarily refunded the buyer.
    SellerRefunded,
    /// Closed before the seller accepted (buyer cancelled or seller declined).
    Cancelled,
}

#[account]
#[derive(InitSpace)]
pub struct Reputation {
    pub wallet: Pubkey,
    pub deals_completed: u32,
    pub deals_disputed: u32,
    /// Deals this wallet accepted as seller but never delivered.
    pub deals_undelivered: u32,
    /// Total price of completed deals, in token base units.
    pub volume: u64,
}

impl Reputation {
    /// Reads a wallet's record, or `None` for a wallet with no history yet.
    /// The caller must have checked the account's address (seeds).
    pub fn load(info: &AccountInfo) -> Option<Reputation> {
        if info.owner != &crate::ID || info.data_is_empty() {
            return None;
        }
        Reputation::try_deserialize(&mut &info.data.borrow()[..]).ok()
    }
}

#[derive(AnchorSerialize, AnchorDeserialize, Clone)]
pub struct DealTerms {
    pub seller: Pubkey,
    pub price: u64,
    pub milestones: u16,
    pub delivery_window: i64,
}
