import { Injectable, inject, signal } from '@angular/core';
import { Transaction } from '@solana/web3.js';
import { ApiError, ApiService } from './api.service';
import { AuthService } from './auth.service';
import { DealActionDto, DealTransactionDto, FullDealDto } from './models';
import { ToastService } from './toast.service';
import { WalletRejectedError, WalletService } from './wallet.service';

/** Where the current action is: asking the wallet, or waiting for Solana to confirm. */
export type ActionPhase = 'idle' | 'wallet' | 'confirming';

/**
 * Runs every on-chain action: get the unsigned transaction from the backend,
 * have the wallet sign and send it, then sync the deal from the chain.
 */
@Injectable({ providedIn: 'root' })
export class DealActionService {
  private readonly api = inject(ApiService);
  private readonly wallet = inject(WalletService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);

  readonly phase = signal<ActionPhase>('idle');

  /** Creates the deal for a listing and funds it. Returns the synced deal, or null if cancelled or failed. */
  buy(listingId: string, buyerNote?: string): Promise<FullDealDto | null> {
    return this.run(
      () => this.api.post<DealTransactionDto>('/api/deals', { listingId, buyerNote: buyerNote || undefined }),
    );
  }

  /** Any other action on an existing deal. Returns the synced deal, or null if cancelled or failed. */
  act(dealId: string, dto: DealActionDto): Promise<FullDealDto | null> {
    return this.run(() => this.api.post<DealTransactionDto>(`/api/deals/${dealId}/tx`, dto));
  }

  private async run(getTransaction: () => Promise<DealTransactionDto>): Promise<FullDealDto | null> {
    if (this.phase() !== 'idle') return null;
    this.phase.set('wallet');
    try {
      await this.ensureWallet();
      // Requested right before the wallet opens: the blockhash expires in about a minute.
      const { deal, transaction } = await getTransaction();
      const tx = Transaction.from(Uint8Array.from(atob(transaction), c => c.charCodeAt(0)));
      const signature = await this.wallet.signAndSendTransaction(tx);

      this.phase.set('confirming');
      return await this.sync(deal.id, signature);
    } catch (error) {
      if (!(error instanceof WalletRejectedError)) this.toast.fromError(error);
      return null;
    } finally {
      this.phase.set('idle');
    }
  }

  private async ensureWallet(): Promise<void> {
    const user = this.auth.user();
    const key = this.wallet.publicKey() ?? (await this.wallet.connect());
    if (user && key !== user.wallet) {
      await this.auth.logout();
      throw new Error('The wallet account changed. Sign in again with this account.');
    }
  }

  private async sync(dealId: string, signature: string): Promise<FullDealDto> {
    const path = `/api/deals/${dealId}/sync`;
    try {
      return await this.api.post<FullDealDto>(path, { signature });
    } catch (error) {
      if (!(error instanceof ApiError) || error.message !== 'Transaction Not Confirmed') throw error;
    }
    // Slow or failed: retry sync once, then fall back to reading the deal.
    try {
      return await this.api.post<FullDealDto>(path, { signature });
    } catch {
      return this.api.get<FullDealDto>(`/api/deals/${dealId}`);
    }
  }
}
