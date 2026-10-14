//! Agora: P2P deals where the payment and both parties' deposits are locked
//! in a vault until the buyer and seller agree. See docs/architecture/program.md.

use anchor_lang::prelude::*;
use anchor_lang::AccountsClose;
use anchor_spl::token_interface::{
    close_account, transfer_checked, CloseAccount, Mint, TokenAccount, TokenInterface,
    TransferChecked,
};

pub mod errors;
pub mod events;
pub mod state;

use errors::AgoraError;
use events::*;
use state::*;

declare_id!("J4fL4Q8nnndro2xmVdCayRt5apXhexkkVQoqqwf4AKi4");

#[program]
pub mod agora {
    use super::*;

    pub fn init_config(ctx: Context<InitConfig>, fee_bps: u16, deposit: DepositParams) -> Result<()> {
        require!(fee_bps <= MAX_BPS && deposit.is_valid(), AgoraError::InvalidConfig);
        ctx.accounts.config.set_inner(Config {
            admin: ctx.accounts.admin.key(),
            mint: ctx.accounts.mint.key(),
            treasury: ctx.accounts.treasury.key(),
            fee_bps,
            deposit,
            bump: ctx.bumps.config,
        });
        Ok(())
    }

    /// Admin tunes the fee and deposit formula. Applies to deals created afterwards.
    pub fn update_config(ctx: Context<UpdateConfig>, fee_bps: u16, deposit: DepositParams) -> Result<()> {
        require!(fee_bps <= MAX_BPS && deposit.is_valid(), AgoraError::InvalidConfig);
        let config = &mut ctx.accounts.config;
        config.fee_bps = fee_bps;
        config.deposit = deposit;
        Ok(())
    }

    /// Buyer opens a deal and locks the price plus their deposit. Both deposits
    /// are worked out here from each party's on-chain record and the price.
    pub fn create_deal(ctx: Context<CreateDeal>, deal_id: u64, terms: DealTerms) -> Result<()> {
        let buyer = ctx.accounts.buyer.key();
        require_keys_neq!(buyer, terms.seller, AgoraError::SameParty);
        require!(
            terms.price > 0 && terms.milestones > 0 && terms.price % terms.milestones as u64 == 0,
            AgoraError::InvalidPrice
        );
        require!(
            (MIN_WINDOW..=MAX_WINDOW).contains(&terms.delivery_window),
            AgoraError::InvalidDeliveryWindow
        );
        require!(
            (MIN_WINDOW..=MAX_WINDOW).contains(&terms.review_window),
            AgoraError::InvalidReviewWindow
        );
        // Each party's deposit depends on their own record, in the role they have here.
        // A wallet with no history yet has an empty record.
        let buyer_record = Reputation::load(&ctx.accounts.buyer_rep)
            .map(|r| r.as_buyer)
            .unwrap_or_default();
        let seller_record = Reputation::load(&ctx.accounts.seller_rep)
            .map(|r| r.as_seller)
            .unwrap_or_default();
        let params = ctx.accounts.config.deposit;
        let buyer_deposit = params.amount(&buyer_record, terms.price);
        let seller_deposit = params.amount(&seller_record, terms.price);

        let amount = terms
            .price
            .checked_add(buyer_deposit)
            .ok_or(AgoraError::Overflow)?;
        transfer_checked(
            CpiContext::new(
                ctx.accounts.token_program.to_account_info(),
                TransferChecked {
                    from: ctx.accounts.buyer_token.to_account_info(),
                    mint: ctx.accounts.mint.to_account_info(),
                    to: ctx.accounts.vault.to_account_info(),
                    authority: ctx.accounts.buyer.to_account_info(),
                },
            ),
            amount,
            ctx.accounts.mint.decimals,
        )?;

        ctx.accounts.deal.set_inner(Deal {
            deal_id,
            buyer,
            seller: terms.seller,
            mint: ctx.accounts.mint.key(),
            price: terms.price,
            milestones: terms.milestones,
            milestones_released: 0,
            buyer_deposit,
            seller_deposit,
            delivery_window: terms.delivery_window,
            review_window: terms.review_window,
            accepted_at: 0,
            delivered_at: 0,
            status: DealStatus::AwaitingSeller,
            disputed: false,
            split_offer: None,
            bump: ctx.bumps.deal,
            vault_bump: ctx.bumps.vault,
        });
        emit!(DealCreated {
            deal: ctx.accounts.deal.key(),
            buyer,
            seller: terms.seller,
            price: terms.price,
            buyer_deposit,
            seller_deposit,
        });
        Ok(())
    }

    /// Seller accepts the deal and locks their deposit. The delivery deadline starts now.
    pub fn accept_deal(ctx: Context<AcceptDeal>) -> Result<()> {
        let deal = &ctx.accounts.deal;
        require!(deal.status == DealStatus::AwaitingSeller, AgoraError::InvalidState);
        transfer_checked(
            CpiContext::new(
                ctx.accounts.token_program.to_account_info(),
                TransferChecked {
                    from: ctx.accounts.seller_token.to_account_info(),
                    mint: ctx.accounts.mint.to_account_info(),
                    to: ctx.accounts.vault.to_account_info(),
                    authority: ctx.accounts.seller.to_account_info(),
                },
            ),
            deal.seller_deposit,
            ctx.accounts.mint.decimals,
        )?;

        let deal = &mut ctx.accounts.deal;
        deal.accepted_at = Clock::get()?.unix_timestamp;
        deal.status = DealStatus::Active;
        emit!(DealAccepted { deal: deal.key() });
        Ok(())
    }

    /// Seller marks the deal delivered, before the delivery deadline.
    pub fn mark_delivered(ctx: Context<DealAction>) -> Result<()> {
        let deal = &mut ctx.accounts.deal;
        require_keys_eq!(ctx.accounts.actor.key(), deal.seller, AgoraError::NotSeller);
        require!(deal.status == DealStatus::Active, AgoraError::InvalidState);
        let now = Clock::get()?.unix_timestamp;
        require!(now <= deal.delivery_deadline(), AgoraError::DeliveryDeadlinePassed);
        deal.delivered_at = now;
        deal.status = DealStatus::Delivered;
        emit!(DealDelivered { deal: deal.key() });
        Ok(())
    }

    /// Either party opens a disagreement once the deal is under way. Everything
    /// stays locked until they agree (confirm, split, or seller refund), and the
    /// open disagreement counts against both parties' records until then.
    pub fn raise_dispute(ctx: Context<RaiseDispute>) -> Result<()> {
        let actor = ctx.accounts.actor.key();
        let deal = &mut ctx.accounts.deal;
        require!(actor == deal.buyer || actor == deal.seller, AgoraError::NotParty);
        require!(
            matches!(deal.status, DealStatus::Active | DealStatus::Delivered),
            AgoraError::InvalidState
        );
        deal.status = DealStatus::Disputed;
        deal.disputed = true;

        ctx.accounts.buyer_rep.wallet = deal.buyer;
        ctx.accounts.buyer_rep.as_buyer.open_dispute();
        ctx.accounts.seller_rep.wallet = deal.seller;
        ctx.accounts.seller_rep.as_seller.open_dispute();
        emit!(DealDisputed { deal: deal.key() });
        Ok(())
    }

    /// Either party offers how to split the remaining price. Replaces any earlier offer.
    pub fn propose_split(ctx: Context<DealAction>, buyer_amount: u64) -> Result<()> {
        let actor = ctx.accounts.actor.key();
        let deal = &mut ctx.accounts.deal;
        require!(actor == deal.buyer || actor == deal.seller, AgoraError::NotParty);
        require!(deal.status == DealStatus::Disputed, AgoraError::InvalidState);
        require!(buyer_amount <= deal.remaining(), AgoraError::InvalidSplit);
        deal.split_offer = Some(SplitOffer { by: actor, buyer_amount });
        emit!(SplitProposed { deal: deal.key(), by: actor, buyer_amount });
        Ok(())
    }

    /// Buyer releases `parts` of the price to the seller (e.g. one per finished match).
    /// Releasing the last part completes the deal.
    pub fn confirm(ctx: Context<Settle>, parts: u16) -> Result<()> {
        let s = ctx.accounts;
        s.require_buyer()?;
        require!(
            matches!(
                s.deal.status,
                DealStatus::Active | DealStatus::Delivered | DealStatus::Disputed
            ),
            AgoraError::InvalidState
        );
        let released = s
            .deal
            .milestones_released
            .checked_add(parts)
            .ok_or(AgoraError::Overflow)?;
        require!(parts > 0 && released <= s.deal.milestones, AgoraError::InvalidParts);

        s.pay_seller(s.deal.unit_price() * parts as u64)?;
        s.deal.milestones_released = released;
        emit!(PartsConfirmed {
            deal: s.deal.key(),
            parts,
            milestones_released: released,
        });

        if released == s.deal.milestones {
            s.finalize(Outcome::Completed)?;
        }
        Ok(())
    }

    /// Buyer takes back the rest, plus the seller's deposit, if the seller
    /// missed the delivery deadline.
    pub fn refund_undelivered(ctx: Context<Settle>) -> Result<()> {
        let s = ctx.accounts;
        s.require_buyer()?;
        require!(s.deal.status == DealStatus::Active, AgoraError::InvalidState);
        let now = Clock::get()?.unix_timestamp;
        require!(now > s.deal.delivery_deadline(), AgoraError::DeliveryDeadlineNotPassed);
        s.vault_transfer(s.buyer_token.to_account_info(), s.deal.remaining())?;
        s.finalize(Outcome::Undelivered)
    }

    /// Seller takes the remaining price once the buyer's review time after delivery
    /// is over and the buyer neither confirmed nor raised a disagreement.
    pub fn claim_payment(ctx: Context<Settle>) -> Result<()> {
        let s = ctx.accounts;
        s.require_seller()?;
        require!(s.deal.status == DealStatus::Delivered, AgoraError::InvalidState);
        let now = Clock::get()?.unix_timestamp;
        require!(now > s.deal.review_deadline(), AgoraError::ReviewNotOver);
        s.pay_seller(s.deal.remaining())?;
        s.finalize(Outcome::Completed)
    }

    /// The party who didn't make the current split offer accepts it.
    pub fn accept_split(ctx: Context<Settle>) -> Result<()> {
        let s = ctx.accounts;
        let actor = s.actor.key();
        require!(actor == s.deal.buyer || actor == s.deal.seller, AgoraError::NotParty);
        require!(s.deal.status == DealStatus::Disputed, AgoraError::InvalidState);
        let offer = s.deal.split_offer.ok_or(AgoraError::NoSplitOffer)?;
        require_keys_neq!(offer.by, actor, AgoraError::OwnSplitOffer);
        let remaining = s.deal.remaining();
        require!(offer.buyer_amount <= remaining, AgoraError::InvalidSplit);

        s.vault_transfer(s.buyer_token.to_account_info(), offer.buyer_amount)?;
        s.pay_seller(remaining - offer.buyer_amount)?;
        s.finalize(Outcome::Split)
    }

    /// Buyer cancels before the seller has accepted.
    pub fn cancel_deal(ctx: Context<Settle>) -> Result<()> {
        let s = ctx.accounts;
        s.require_buyer()?;
        require!(s.deal.status == DealStatus::AwaitingSeller, AgoraError::InvalidState);
        s.vault_transfer(s.buyer_token.to_account_info(), s.deal.remaining())?;
        s.finalize(Outcome::Cancelled)
    }

    /// Seller gives the remaining price back to the buyer, from any open state.
    /// Before accepting, this is how a seller declines a deal.
    pub fn seller_refund(ctx: Context<Settle>) -> Result<()> {
        let s = ctx.accounts;
        s.require_seller()?;
        let outcome = if s.deal.status == DealStatus::AwaitingSeller {
            Outcome::Cancelled
        } else {
            Outcome::SellerRefunded
        };
        s.vault_transfer(s.buyer_token.to_account_info(), s.deal.remaining())?;
        s.finalize(outcome)
    }
}

#[derive(Accounts)]
pub struct InitConfig<'info> {
    #[account(mut)]
    pub admin: Signer<'info>,
    #[account(init, payer = admin, space = 8 + Config::INIT_SPACE, seeds = [b"config"], bump)]
    pub config: Account<'info, Config>,
    pub mint: InterfaceAccount<'info, Mint>,
    #[account(constraint = treasury.mint == mint.key() @ AgoraError::WrongTokenAccount)]
    pub treasury: InterfaceAccount<'info, TokenAccount>,
    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
pub struct UpdateConfig<'info> {
    pub admin: Signer<'info>,
    #[account(mut, seeds = [b"config"], bump = config.bump, has_one = admin)]
    pub config: Account<'info, Config>,
}

#[derive(Accounts)]
#[instruction(deal_id: u64, terms: DealTerms)]
pub struct CreateDeal<'info> {
    #[account(mut)]
    pub buyer: Signer<'info>,
    #[account(seeds = [b"config"], bump = config.bump, has_one = mint)]
    pub config: Account<'info, Config>,
    pub mint: InterfaceAccount<'info, Mint>,
    #[account(
        init,
        payer = buyer,
        space = 8 + Deal::INIT_SPACE,
        seeds = [b"deal", buyer.key().as_ref(), &deal_id.to_le_bytes()],
        bump
    )]
    pub deal: Account<'info, Deal>,
    #[account(
        init,
        payer = buyer,
        seeds = [b"vault", deal.key().as_ref()],
        bump,
        token::mint = mint,
        token::authority = deal,
        token::token_program = token_program
    )]
    pub vault: InterfaceAccount<'info, TokenAccount>,
    #[account(
        mut,
        constraint = buyer_token.owner == buyer.key() @ AgoraError::WrongTokenAccount,
        constraint = buyer_token.mint == mint.key() @ AgoraError::WrongTokenAccount
    )]
    pub buyer_token: InterfaceAccount<'info, TokenAccount>,
    /// CHECK: address fixed by the seeds; empty for a wallet with no history.
    #[account(seeds = [REPUTATION_SEED, buyer.key().as_ref()], bump)]
    pub buyer_rep: UncheckedAccount<'info>,
    /// CHECK: address fixed by the seeds; empty for a wallet with no history.
    #[account(seeds = [REPUTATION_SEED, terms.seller.as_ref()], bump)]
    pub seller_rep: UncheckedAccount<'info>,
    pub token_program: Interface<'info, TokenInterface>,
    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
pub struct AcceptDeal<'info> {
    pub seller: Signer<'info>,
    #[account(
        mut,
        seeds = [b"deal", deal.buyer.as_ref(), &deal.deal_id.to_le_bytes()],
        bump = deal.bump,
        has_one = seller @ AgoraError::NotSeller,
        has_one = mint
    )]
    pub deal: Account<'info, Deal>,
    pub mint: InterfaceAccount<'info, Mint>,
    #[account(mut, seeds = [b"vault", deal.key().as_ref()], bump = deal.vault_bump)]
    pub vault: InterfaceAccount<'info, TokenAccount>,
    #[account(
        mut,
        constraint = seller_token.owner == seller.key() @ AgoraError::WrongTokenAccount,
        constraint = seller_token.mint == mint.key() @ AgoraError::WrongTokenAccount
    )]
    pub seller_token: InterfaceAccount<'info, TokenAccount>,
    pub token_program: Interface<'info, TokenInterface>,
}

/// State changes that move no tokens and touch no records (mark_delivered,
/// propose_split). The handler checks who `actor` must be.
#[derive(Accounts)]
pub struct DealAction<'info> {
    pub actor: Signer<'info>,
    #[account(
        mut,
        seeds = [b"deal", deal.buyer.as_ref(), &deal.deal_id.to_le_bytes()],
        bump = deal.bump
    )]
    pub deal: Account<'info, Deal>,
}

/// Opens a disagreement and records it in both parties' reputation.
#[derive(Accounts)]
pub struct RaiseDispute<'info> {
    #[account(mut)]
    pub actor: Signer<'info>,
    #[account(
        mut,
        seeds = [b"deal", deal.buyer.as_ref(), &deal.deal_id.to_le_bytes()],
        bump = deal.bump
    )]
    pub deal: Account<'info, Deal>,
    #[account(
        init_if_needed,
        payer = actor,
        space = 8 + Reputation::INIT_SPACE,
        seeds = [REPUTATION_SEED, deal.buyer.as_ref()],
        bump
    )]
    pub buyer_rep: Account<'info, Reputation>,
    #[account(
        init_if_needed,
        payer = actor,
        space = 8 + Reputation::INIT_SPACE,
        seeds = [REPUTATION_SEED, deal.seller.as_ref()],
        bump
    )]
    pub seller_rep: Account<'info, Reputation>,
    pub system_program: Program<'info, System>,
}

/// Every instruction that pays out of the vault. The handler checks who `actor`
/// must be; payouts can only go to the deal's buyer, seller and the treasury.
#[derive(Accounts)]
pub struct Settle<'info> {
    #[account(mut)]
    pub actor: Signer<'info>,
    #[account(seeds = [b"config"], bump = config.bump)]
    pub config: Account<'info, Config>,
    #[account(
        mut,
        seeds = [b"deal", deal.buyer.as_ref(), &deal.deal_id.to_le_bytes()],
        bump = deal.bump,
        has_one = buyer,
        has_one = mint
    )]
    pub deal: Account<'info, Deal>,
    /// Receives the rent of the closed deal and vault accounts (the buyer paid it).
    #[account(mut)]
    pub buyer: SystemAccount<'info>,
    pub mint: InterfaceAccount<'info, Mint>,
    #[account(mut, seeds = [b"vault", deal.key().as_ref()], bump = deal.vault_bump)]
    pub vault: InterfaceAccount<'info, TokenAccount>,
    #[account(
        mut,
        constraint = buyer_token.owner == deal.buyer @ AgoraError::WrongTokenAccount,
        constraint = buyer_token.mint == mint.key() @ AgoraError::WrongTokenAccount
    )]
    pub buyer_token: InterfaceAccount<'info, TokenAccount>,
    #[account(
        mut,
        constraint = seller_token.owner == deal.seller @ AgoraError::WrongTokenAccount,
        constraint = seller_token.mint == mint.key() @ AgoraError::WrongTokenAccount
    )]
    pub seller_token: InterfaceAccount<'info, TokenAccount>,
    #[account(mut, address = config.treasury @ AgoraError::WrongTokenAccount)]
    pub treasury: InterfaceAccount<'info, TokenAccount>,
    #[account(
        init_if_needed,
        payer = actor,
        space = 8 + Reputation::INIT_SPACE,
        seeds = [REPUTATION_SEED, deal.buyer.as_ref()],
        bump
    )]
    pub buyer_rep: Account<'info, Reputation>,
    #[account(
        init_if_needed,
        payer = actor,
        space = 8 + Reputation::INIT_SPACE,
        seeds = [REPUTATION_SEED, deal.seller.as_ref()],
        bump
    )]
    pub seller_rep: Account<'info, Reputation>,
    pub token_program: Interface<'info, TokenInterface>,
    pub system_program: Program<'info, System>,
}

impl<'info> Settle<'info> {
    fn require_buyer(&self) -> Result<()> {
        require_keys_eq!(self.actor.key(), self.deal.buyer, AgoraError::NotBuyer);
        Ok(())
    }

    fn require_seller(&self) -> Result<()> {
        require_keys_eq!(self.actor.key(), self.deal.seller, AgoraError::NotSeller);
        Ok(())
    }

    /// Moves tokens out of the vault, signed by the deal PDA.
    fn vault_transfer(&self, to: AccountInfo<'info>, amount: u64) -> Result<()> {
        if amount == 0 {
            return Ok(());
        }
        let buyer = self.deal.buyer;
        let id = self.deal.deal_id.to_le_bytes();
        let bump = [self.deal.bump];
        let seeds: &[&[u8]] = &[b"deal", buyer.as_ref(), &id, &bump];
        transfer_checked(
            CpiContext::new_with_signer(
                self.token_program.to_account_info(),
                TransferChecked {
                    from: self.vault.to_account_info(),
                    mint: self.mint.to_account_info(),
                    to,
                    authority: self.deal.to_account_info(),
                },
                &[seeds],
            ),
            amount,
            self.mint.decimals,
        )
    }

    /// Pays the seller `amount`, minus the fee, which goes to the treasury.
    fn pay_seller(&self, amount: u64) -> Result<()> {
        let fee = (amount as u128 * self.config.fee_bps as u128 / MAX_BPS as u128) as u64;
        self.vault_transfer(self.treasury.to_account_info(), fee)?;
        self.vault_transfer(self.seller_token.to_account_info(), amount - fee)
    }

    /// Returns deposits, updates reputation, and closes the deal and vault.
    /// Call after the price itself has been paid out.
    fn finalize(&mut self, outcome: Outcome) -> Result<()> {
        // The seller's deposit is only in the vault once they've accepted.
        let seller_deposit = if self.deal.status == DealStatus::AwaitingSeller {
            0
        } else {
            self.deal.seller_deposit
        };
        if outcome == Outcome::Undelivered {
            self.vault_transfer(
                self.buyer_token.to_account_info(),
                self.deal.buyer_deposit + seller_deposit,
            )?;
        } else {
            self.vault_transfer(self.buyer_token.to_account_info(), self.deal.buyer_deposit)?;
            self.vault_transfer(self.seller_token.to_account_info(), seller_deposit)?;
        }

        // Anything left (e.g. tokens sent to the vault directly) goes to the buyer,
        // so the vault can be closed.
        self.vault.reload()?;
        self.vault_transfer(self.buyer_token.to_account_info(), self.vault.amount)?;

        let buyer = self.deal.buyer;
        let id = self.deal.deal_id.to_le_bytes();
        let bump = [self.deal.bump];
        let seeds: &[&[u8]] = &[b"deal", buyer.as_ref(), &id, &bump];
        close_account(CpiContext::new_with_signer(
            self.token_program.to_account_info(),
            CloseAccount {
                account: self.vault.to_account_info(),
                destination: self.buyer.to_account_info(),
                authority: self.deal.to_account_info(),
            },
            &[seeds],
        ))?;

        self.update_reputation(outcome);
        emit!(DealClosed { deal: self.deal.key(), outcome });
        self.deal.close(self.buyer.to_account_info())
    }

    /// Records the outcome in the buyer's record as buyer and the seller's record as seller.
    fn update_reputation(&mut self, outcome: Outcome) {
        let (price, disputed) = (self.deal.price, self.deal.disputed);
        self.buyer_rep.wallet = self.deal.buyer;
        self.seller_rep.wallet = self.deal.seller;
        let buyer = &mut self.buyer_rep.as_buyer;
        let seller = &mut self.seller_rep.as_seller;

        // The deal is closing, so its disagreement (if any) is no longer open.
        if disputed {
            buyer.close_dispute();
            seller.close_dispute();
        }
        match outcome {
            Outcome::Completed | Outcome::Split => {
                buyer.add_finished(price, disputed);
                seller.add_finished(price, disputed);
            }
            Outcome::Undelivered => seller.undelivered = seller.undelivered.saturating_add(1),
            Outcome::SellerRefunded | Outcome::Cancelled => {}
        }
    }
}
