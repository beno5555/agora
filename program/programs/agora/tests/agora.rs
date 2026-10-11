//! Runs the compiled program (target/deploy/agora.so) in LiteSVM, an in-process
//! Solana runtime. Build first with `cargo build-sbf`, then `cargo test`.

use agora::state::{DealTerms, Reputation};
use anchor_lang::{AccountDeserialize, InstructionData, ToAccountMetas};
use anchor_spl::associated_token::{
    get_associated_token_address, spl_associated_token_account::instruction::create_associated_token_account,
};
use anchor_spl::token::spl_token;
use litesvm::LiteSVM;
use solana_sdk::{
    clock::Clock,
    instruction::Instruction,
    pubkey::Pubkey,
    signature::{Keypair, Signer},
    system_instruction, system_program,
    transaction::Transaction,
};

const FEE_BPS: u16 = 200; // 2%
const MIN_DEPOSIT_BPS: u16 = 1000; // 10%
const TOKEN: u64 = 1_000_000; // 6 decimals

struct Env {
    svm: LiteSVM,
    admin: Keypair,
    buyer: Keypair,
    seller: Keypair,
    mint: Pubkey,
    treasury: Pubkey,
    buyer_token: Pubkey,
    seller_token: Pubkey,
    next_id: u64,
}

fn pda(seeds: &[&[u8]]) -> Pubkey {
    Pubkey::find_program_address(seeds, &agora::ID).0
}

fn config_pda() -> Pubkey {
    pda(&[b"config"])
}

fn vault_pda(deal: &Pubkey) -> Pubkey {
    pda(&[b"vault", deal.as_ref()])
}

fn rep_pda(wallet: &Pubkey) -> Pubkey {
    pda(&[b"rep", wallet.as_ref()])
}

/// 3 matches at 5 tokens each; deposits of 1.5 tokens per side.
fn terms(seller: Pubkey) -> DealTerms {
    DealTerms {
        seller,
        price: 15 * TOKEN,
        milestones: 3,
        buyer_deposit: 3 * TOKEN / 2,
        seller_deposit: 3 * TOKEN / 2,
        delivery_window: 3600,
        review_period: 3600,
    }
}

impl Env {
    fn new() -> Self {
        let mut svm = LiteSVM::new();
        let so = concat!(env!("CARGO_MANIFEST_DIR"), "/../../target/deploy/agora.so");
        svm.add_program_from_file(agora::ID, so)
            .expect("build the program first: cargo build-sbf");

        let (admin, buyer, seller) = (Keypair::new(), Keypair::new(), Keypair::new());
        for kp in [&admin, &buyer, &seller] {
            svm.airdrop(&kp.pubkey(), 10_000_000_000).unwrap();
        }

        let mint = Keypair::new();
        let rent = svm.minimum_balance_for_rent_exemption(82);
        let mut env = Env {
            svm,
            treasury: get_associated_token_address(&admin.pubkey(), &mint.pubkey()),
            buyer_token: get_associated_token_address(&buyer.pubkey(), &mint.pubkey()),
            seller_token: get_associated_token_address(&seller.pubkey(), &mint.pubkey()),
            mint: mint.pubkey(),
            admin,
            buyer,
            seller,
            next_id: 1,
        };
        let a = env.admin.pubkey();
        let mut ixs = vec![
            system_instruction::create_account(&a, &env.mint, rent, 82, &spl_token::ID),
            spl_token::instruction::initialize_mint2(&spl_token::ID, &env.mint, &a, None, 6)
                .unwrap(),
        ];
        for owner in [a, env.buyer.pubkey(), env.seller.pubkey()] {
            ixs.push(create_associated_token_account(&a, &owner, &env.mint, &spl_token::ID));
        }
        for account in [env.buyer_token, env.seller_token] {
            ixs.push(
                spl_token::instruction::mint_to(&spl_token::ID, &env.mint, &account, &a, &[], 1000 * TOKEN)
                    .unwrap(),
            );
        }
        let admin = env.admin.insecure_clone();
        env.send(ixs, &[&admin, &mint]).unwrap();

        let init = Instruction {
            program_id: agora::ID,
            accounts: agora::accounts::InitConfig {
                admin: a,
                config: config_pda(),
                mint: env.mint,
                treasury: env.treasury,
                system_program: system_program::ID,
            }
            .to_account_metas(None),
            data: agora::instruction::InitConfig { fee_bps: FEE_BPS, min_deposit_bps: MIN_DEPOSIT_BPS }.data(),
        };
        env.send(vec![init], &[&admin]).unwrap();
        env
    }

    /// Sends a transaction paid by the first signer. On failure, returns the program logs.
    fn send(&mut self, ixs: Vec<Instruction>, signers: &[&Keypair]) -> Result<(), String> {
        let tx = Transaction::new_signed_with_payer(
            &ixs,
            Some(&signers[0].pubkey()),
            signers,
            self.svm.latest_blockhash(),
        );
        let result = self
            .svm
            .send_transaction(tx)
            .map(|_| ())
            .map_err(|e| e.meta.logs.join("\n"));
        self.svm.expire_blockhash();
        result
    }

    fn balance(&self, token_account: &Pubkey) -> u64 {
        let data = self.svm.get_account(token_account).unwrap().data;
        u64::from_le_bytes(data[64..72].try_into().unwrap())
    }

    fn reputation(&self, wallet: &Pubkey) -> Reputation {
        let data = self.svm.get_account(&rep_pda(wallet)).unwrap().data;
        Reputation::try_deserialize(&mut data.as_slice()).unwrap()
    }

    fn advance_clock(&mut self, seconds: i64) {
        let mut clock = self.svm.get_sysvar::<Clock>();
        clock.unix_timestamp += seconds;
        self.svm.set_sysvar(&clock);
    }

    fn deal_pda(&self, id: u64) -> Pubkey {
        pda(&[b"deal", self.buyer.pubkey().as_ref(), &id.to_le_bytes()])
    }

    /// Buyer creates and funds a deal, seller accepts it.
    fn open_deal(&mut self, terms: DealTerms) -> Pubkey {
        let id = self.next_id;
        self.next_id += 1;
        let deal = self.deal_pda(id);
        let buyer = self.buyer.insecure_clone();
        let seller = self.seller.insecure_clone();

        let create = Instruction {
            program_id: agora::ID,
            accounts: agora::accounts::CreateDeal {
                buyer: buyer.pubkey(),
                config: config_pda(),
                mint: self.mint,
                deal,
                vault: vault_pda(&deal),
                buyer_token: self.buyer_token,
                token_program: spl_token::ID,
                system_program: system_program::ID,
            }
            .to_account_metas(None),
            data: agora::instruction::CreateDeal { deal_id: id, terms }.data(),
        };
        self.send(vec![create], &[&buyer]).unwrap();

        let accept = Instruction {
            program_id: agora::ID,
            accounts: agora::accounts::AcceptDeal {
                seller: seller.pubkey(),
                deal,
                mint: self.mint,
                vault: vault_pda(&deal),
                seller_token: self.seller_token,
                token_program: spl_token::ID,
            }
            .to_account_metas(None),
            data: agora::instruction::AcceptDeal {}.data(),
        };
        self.send(vec![accept], &[&seller]).unwrap();
        deal
    }

    fn action(&mut self, data: Vec<u8>, deal: Pubkey, actor: &Keypair) -> Result<(), String> {
        let ix = Instruction {
            program_id: agora::ID,
            accounts: agora::accounts::DealAction { actor: actor.pubkey(), deal }.to_account_metas(None),
            data,
        };
        self.send(vec![ix], &[actor])
    }

    fn settle(&mut self, data: Vec<u8>, deal: Pubkey, actor: &Keypair) -> Result<(), String> {
        let ix = Instruction {
            program_id: agora::ID,
            accounts: agora::accounts::Settle {
                actor: actor.pubkey(),
                config: config_pda(),
                deal,
                buyer: self.buyer.pubkey(),
                mint: self.mint,
                vault: vault_pda(&deal),
                buyer_token: self.buyer_token,
                seller_token: self.seller_token,
                treasury: self.treasury,
                buyer_rep: rep_pda(&self.buyer.pubkey()),
                seller_rep: rep_pda(&self.seller.pubkey()),
                token_program: spl_token::ID,
                system_program: system_program::ID,
            }
            .to_account_metas(None),
            data,
        };
        self.send(vec![ix], &[actor])
    }
}

fn assert_err(result: Result<(), String>, code: &str) {
    let logs = result.expect_err("expected the transaction to fail");
    assert!(logs.contains(code), "expected {code}, got logs:\n{logs}");
}

#[test]
fn pays_per_match_and_completes_after_the_last_one() {
    let mut env = Env::new();
    let (buyer, seller) = (env.buyer.insecure_clone(), env.seller.insecure_clone());
    let seller_before = env.balance(&env.seller_token);
    let buyer_before = env.balance(&env.buyer_token);

    let deal = env.open_deal(terms(seller.pubkey()));
    assert_eq!(env.balance(&vault_pda(&deal)), 18 * TOKEN);

    env.settle(agora::instruction::Confirm { parts: 1 }.data(), deal, &buyer).unwrap();
    // 5 tokens minus the 2% fee; the seller's 1.5 deposit is still locked.
    assert_eq!(env.balance(&env.seller_token), seller_before - 3 * TOKEN / 2 + 49 * TOKEN / 10);

    env.settle(agora::instruction::Confirm { parts: 2 }.data(), deal, &buyer).unwrap();
    assert!(env.svm.get_account(&deal).map_or(true, |a| a.lamports == 0), "deal closed");
    assert_eq!(env.balance(&env.seller_token), seller_before + 147 * TOKEN / 10);
    assert_eq!(env.balance(&env.buyer_token), buyer_before - 15 * TOKEN);
    assert_eq!(env.balance(&env.treasury), 3 * TOKEN / 10);
    assert_eq!(env.reputation(&seller.pubkey()).deals_completed, 1);
}

#[test]
fn seller_claims_after_the_review_period() {
    let mut env = Env::new();
    let seller = env.seller.insecure_clone();
    let seller_before = env.balance(&env.seller_token);
    let deal = env.open_deal(terms(seller.pubkey()));
    env.action(agora::instruction::MarkDelivered {}.data(), deal, &seller).unwrap();

    assert_err(
        env.settle(agora::instruction::ClaimAfterReview {}.data(), deal, &seller),
        "ReviewPeriodNotEnded",
    );
    env.advance_clock(3601);
    env.settle(agora::instruction::ClaimAfterReview {}.data(), deal, &seller).unwrap();
    assert_eq!(env.balance(&env.seller_token), seller_before + 147 * TOKEN / 10);
}

#[test]
fn disagreement_keeps_funds_locked_until_a_split_is_accepted() {
    let mut env = Env::new();
    let (buyer, seller) = (env.buyer.insecure_clone(), env.seller.insecure_clone());
    let buyer_before = env.balance(&env.buyer_token);
    let deal = env.open_deal(terms(seller.pubkey()));

    env.action(agora::instruction::MarkDelivered {}.data(), deal, &seller).unwrap();
    env.action(agora::instruction::RaiseDispute {}.data(), deal, &buyer).unwrap();

    env.advance_clock(3601);
    assert_err(
        env.settle(agora::instruction::ClaimAfterReview {}.data(), deal, &seller),
        "InvalidState",
    );

    env.action(agora::instruction::ProposeSplit { buyer_amount: 10 * TOKEN }.data(), deal, &seller)
        .unwrap();
    assert_err(
        env.settle(agora::instruction::AcceptSplit {}.data(), deal, &seller),
        "OwnSplitOffer",
    );
    env.settle(agora::instruction::AcceptSplit {}.data(), deal, &buyer).unwrap();

    // Buyer paid 15 and got 10 back; both deposits returned.
    assert_eq!(env.balance(&env.buyer_token), buyer_before - 5 * TOKEN);
    assert_eq!(env.reputation(&buyer.pubkey()).deals_disputed, 1);
}

#[test]
fn missed_deadline_refunds_buyer_plus_sellers_deposit() {
    let mut env = Env::new();
    let (buyer, seller) = (env.buyer.insecure_clone(), env.seller.insecure_clone());
    let buyer_before = env.balance(&env.buyer_token);
    let deal = env.open_deal(terms(seller.pubkey()));

    assert_err(
        env.settle(agora::instruction::RefundUndelivered {}.data(), deal, &buyer),
        "DeliveryDeadlineNotPassed",
    );
    env.advance_clock(3601);
    env.settle(agora::instruction::RefundUndelivered {}.data(), deal, &buyer).unwrap();

    assert_eq!(env.balance(&env.buyer_token), buyer_before + 3 * TOKEN / 2);
    assert_eq!(env.reputation(&seller.pubkey()).deals_undelivered, 1);
}

#[test]
fn rejects_actions_from_the_wrong_party() {
    let mut env = Env::new();
    let (buyer, seller) = (env.buyer.insecure_clone(), env.seller.insecure_clone());
    let deal = env.open_deal(terms(seller.pubkey()));

    assert_err(env.settle(agora::instruction::Confirm { parts: 1 }.data(), deal, &seller), "NotBuyer");
    assert_err(env.action(agora::instruction::MarkDelivered {}.data(), deal, &buyer), "NotSeller");
    env.settle(agora::instruction::SellerRefund {}.data(), deal, &seller).unwrap();
}

#[test]
fn buyer_can_cancel_before_the_seller_accepts() {
    let mut env = Env::new();
    let buyer = env.buyer.insecure_clone();
    let buyer_before = env.balance(&env.buyer_token);
    let deal = env.deal_pda(99);
    let create = Instruction {
        program_id: agora::ID,
        accounts: agora::accounts::CreateDeal {
            buyer: buyer.pubkey(),
            config: config_pda(),
            mint: env.mint,
            deal,
            vault: vault_pda(&deal),
            buyer_token: env.buyer_token,
            token_program: spl_token::ID,
            system_program: system_program::ID,
        }
        .to_account_metas(None),
        data: agora::instruction::CreateDeal { deal_id: 99, terms: terms(env.seller.pubkey()) }.data(),
    };
    env.send(vec![create], &[&buyer]).unwrap();
    env.settle(agora::instruction::CancelDeal {}.data(), deal, &buyer).unwrap();
    assert_eq!(env.balance(&env.buyer_token), buyer_before);
}
