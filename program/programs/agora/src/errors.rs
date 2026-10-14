use anchor_lang::prelude::*;

#[error_code]
pub enum AgoraError {
    #[msg("Fee or deposit basis points out of range")]
    InvalidConfig,
    #[msg("Buyer and seller must be different wallets")]
    SameParty,
    #[msg("Price must be positive and divisible by the number of milestones")]
    InvalidPrice,
    #[msg("Delivery window must be between 1 minute and 30 days")]
    InvalidDeliveryWindow,
    #[msg("Review window must be between 1 minute and 30 days")]
    InvalidReviewWindow,
    #[msg("Only the buyer can do this")]
    NotBuyer,
    #[msg("Only the seller can do this")]
    NotSeller,
    #[msg("Only the buyer or seller can do this")]
    NotParty,
    #[msg("Not allowed in the deal's current state")]
    InvalidState,
    #[msg("Invalid number of parts")]
    InvalidParts,
    #[msg("The delivery deadline has passed")]
    DeliveryDeadlinePassed,
    #[msg("The delivery deadline has not passed yet")]
    DeliveryDeadlineNotPassed,
    #[msg("The buyer's review time is not over yet")]
    ReviewNotOver,
    #[msg("Split amount exceeds what remains")]
    InvalidSplit,
    #[msg("There is no split offer to accept")]
    NoSplitOffer,
    #[msg("You cannot accept your own split offer")]
    OwnSplitOffer,
    #[msg("Token account has the wrong owner or mint")]
    WrongTokenAccount,
    #[msg("Arithmetic overflow")]
    Overflow,
}
