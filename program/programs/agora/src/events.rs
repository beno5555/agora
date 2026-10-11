use anchor_lang::prelude::*;

use crate::state::Outcome;

#[event]
pub struct DealCreated {
    pub deal: Pubkey,
    pub buyer: Pubkey,
    pub seller: Pubkey,
    pub price: u64,
    pub buyer_deposit: u64,
    pub seller_deposit: u64,
}

#[event]
pub struct DealAccepted {
    pub deal: Pubkey,
}

#[event]
pub struct DealDelivered {
    pub deal: Pubkey,
}

#[event]
pub struct PartsConfirmed {
    pub deal: Pubkey,
    pub parts: u16,
    pub milestones_released: u16,
}

#[event]
pub struct DealDisputed {
    pub deal: Pubkey,
}

#[event]
pub struct SplitProposed {
    pub deal: Pubkey,
    pub by: Pubkey,
    pub buyer_amount: u64,
}

#[event]
pub struct DealClosed {
    pub deal: Pubkey,
    pub outcome: Outcome,
}
