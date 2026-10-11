use anchor_lang::prelude::*;

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
    /// Minimum deposit per side, as basis points of the price.
    pub min_deposit_bps: u16,
    pub bump: u8,
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
    pub buyer_deposit: u64,
    pub seller_deposit: u64,
    pub delivery_window: i64,
    pub review_period: i64,
    pub accepted_at: i64,
    pub delivered_at: i64,
    pub status: DealStatus,
    /// True once the buyer has raised a disagreement; counted in both parties' reputation.
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
    /// All parts confirmed, or claimed after the review period.
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

#[derive(AnchorSerialize, AnchorDeserialize, Clone)]
pub struct DealTerms {
    pub seller: Pubkey,
    pub price: u64,
    pub milestones: u16,
    pub buyer_deposit: u64,
    pub seller_deposit: u64,
    pub delivery_window: i64,
    pub review_period: i64,
}
