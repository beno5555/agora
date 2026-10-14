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

/// How each party's deposit is worked out from their record and the price.
/// All values are basis points of the price. See `rate_bps`.
#[derive(AnchorSerialize, AnchorDeserialize, Clone, Copy, PartialEq, Eq, InitSpace, Debug)]
pub struct DepositParams {
    /// Rate for a wallet with no history.
    pub base_bps: u16,
    pub floor_bps: u16,
    pub cap_bps: u16,
    /// Discount per 1× this deal's price of completed volume.
    pub discount_per_volume_bps: u16,
    pub max_discount_bps: u16,
    /// Added per bad mark (see `RoleRecord::bad_marks`).
    pub penalty_bps: u16,
}

impl DepositParams {
    pub fn is_valid(&self) -> bool {
        self.floor_bps <= self.cap_bps
            && self.cap_bps <= MAX_BPS
            && self.base_bps <= MAX_BPS
            && self.max_discount_bps <= MAX_BPS
    }

    /// Deposit rate for a party with this record in their role on the deal:
    ///
    /// rate = base − discount + penalty, kept within [floor, cap]
    ///
    /// - discount: completed volume measured in multiples of *this* price, so a
    ///   record built on small deals barely helps on a large one. Capped at
    ///   `max_discount`.
    /// - penalty: `penalty_bps` per bad mark.
    pub fn rate_bps(&self, record: &RoleRecord, price: u64) -> u64 {
        let volume_in_prices =
            record.volume as u128 * self.discount_per_volume_bps as u128 / price as u128;
        let discount = volume_in_prices.min(self.max_discount_bps as u128) as u64;
        let penalty = record.bad_marks() * self.penalty_bps as u64;

        (self.base_bps as u64 + penalty)
            .saturating_sub(discount)
            .clamp(self.floor_bps as u64, self.cap_bps as u64)
    }

    pub fn amount(&self, record: &RoleRecord, price: u64) -> u64 {
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
    /// True once either party has opened a disagreement.
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

/// Seed of the `Reputation` account. The first version lived at `b"rep"`, with
/// one record for buying and selling together; those accounts are no longer read.
pub const REPUTATION_SEED: &[u8] = b"reputation";

/// A wallet's on-chain record, kept separately for each role, so buying a lot
/// doesn't lower a wallet's deposit as a seller (and the other way round).
#[account]
#[derive(InitSpace)]
pub struct Reputation {
    pub wallet: Pubkey,
    pub as_buyer: RoleRecord,
    pub as_seller: RoleRecord,
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

/// One wallet's history in one role.
#[derive(AnchorSerialize, AnchorDeserialize, Clone, Copy, Default, PartialEq, Eq, InitSpace, Debug)]
pub struct RoleRecord {
    /// Deals finished by full confirmation or an accepted split.
    pub completed: u32,
    /// How many of those had a disagreement along the way.
    pub disputed: u32,
    /// As seller only: deals accepted but not delivered by the deadline.
    pub undelivered: u32,
    /// Disagreements opened and not settled yet. Nobody can tell who is at
    /// fault, so each one counts against both parties until the deal closes.
    pub open_disputes: u32,
    /// Total price of completed deals, in token base units.
    pub volume: u64,
}

impl RoleRecord {
    /// Things that make this wallet riskier in this role. Each one raises its
    /// deposit by `DepositParams::penalty_bps`.
    pub fn bad_marks(&self) -> u64 {
        self.undelivered as u64 + self.open_disputes as u64
    }

    pub fn add_finished(&mut self, price: u64, disputed: bool) {
        self.completed = self.completed.saturating_add(1);
        self.volume = self.volume.saturating_add(price);
        if disputed {
            self.disputed = self.disputed.saturating_add(1);
        }
    }

    pub fn open_dispute(&mut self) {
        self.open_disputes = self.open_disputes.saturating_add(1);
    }

    pub fn close_dispute(&mut self) {
        self.open_disputes = self.open_disputes.saturating_sub(1);
    }
}

#[derive(AnchorSerialize, AnchorDeserialize, Clone)]
pub struct DealTerms {
    pub seller: Pubkey,
    pub price: u64,
    pub milestones: u16,
    pub delivery_window: i64,
}
