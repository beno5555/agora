import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { DealActionService } from '../../core/deal-action.service';
import { StandardListingDto } from '../../core/models';
import { AmountPipe } from '../../shared/amount.pipe';
import { windowLabel } from '../../shared/delivery-window';
import { plural } from '../../shared/plural';
import { Reputation } from '../../shared/reputation';
import { WalletAddress } from '../../shared/wallet-address';

@Component({
  selector: 'app-listing',
  imports: [FormsModule, RouterLink, AmountPipe, Reputation, WalletAddress],
  template: `
    @if (listing(); as l) {
      <p class="muted small">
        <a [routerLink]="['/g', l.gameSlug]">{{ l.gameName }}</a> · {{ l.type }}
      </p>
      <h1>{{ l.title }}</h1>
      @if (!l.isActive) {
        <p class="notice">This listing is paused and not taking new deals.</p>
      }

      <section class="panel">
        <p style="white-space: pre-line">{{ l.description }}</p>
        <dl class="facts">
          <dt>Price</dt>
          <dd>\${{ l.price | amount }}</dd>
          <dt>Released in</dt>
          <dd>{{ plural(l.unitLabel, l.milestones) }}</dd>
          <dt>Delivery deadline</dt>
          <dd>{{ label(l.deliveryWindow) }}</dd>
        </dl>
      </section>

      <section class="panel">
        <h2>Seller</h2>
        <p>
          <a [routerLink]="['/u', l.seller.wallet]">{{
            l.seller.displayName || 'Seller profile'
          }}</a>
          <app-wallet-address [wallet]="l.seller.wallet" />
        </p>
        <app-reputation [reputation]="l.sellerReputation" />
      </section>

      @if (isOwner()) {
        <a [routerLink]="['/sell', l.id]"
          ><button type="button" class="secondary">Edit listing</button></a
        >
      } @else {
        <section class="panel">
          <h2>Buy</h2>
          @if (l.depositPreview; as d) {
            <dl class="facts">
              <dt>You pay now</dt>
              <dd>\${{ l.price | amount }}</dd>
              <dt>Your deposit</dt>
              <dd>\${{ d.buyerDeposit | amount }}</dd>
              <dt>Seller locks</dt>
              <dd>\${{ d.sellerDeposit | amount }}</dd>
            </dl>
            <p class="muted small">Deposits come back when the deal completes normally.</p>
          } @else if (!auth.user()) {
            <p class="muted">Sign in to see your deposit.</p>
          }
          <label>
            Note for the seller <span class="hint">(optional, e.g. your in-game name)</span>
            <input maxlength="300" [(ngModel)]="note" />
          </label>
          @if (actions.phase() === 'confirming') {
            <p class="notice">Confirming on Solana…</p>
          }
          <button
            type="button"
            [disabled]="actions.phase() !== 'idle' || !l.isActive"
            (click)="buy()"
          >
            {{ actions.phase() === 'wallet' ? 'Waiting for wallet…' : 'Buy' }}
          </button>
        </section>
      }
    } @else {
      <p class="muted">{{ error() ?? 'Loading…' }}</p>
    }
  `,
})
export class Listing {
  readonly id = input.required<string>();

  protected readonly auth = inject(AuthService);
  protected readonly actions = inject(DealActionService);
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  protected readonly listing = signal<StandardListingDto | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly note = signal('');
  protected readonly label = windowLabel;
  protected readonly plural = plural;
  protected readonly isOwner = computed(
    () => this.listing()?.seller.wallet === this.auth.user()?.wallet,
  );

  constructor() {
    // Reload when the listing changes or the user signs in (depositPreview is null when signed out).
    effect(() => {
      this.auth.user();
      void this.load(this.id());
    });
  }

  private async load(id: string): Promise<void> {
    try {
      this.listing.set(await this.api.get<StandardListingDto>(`/api/listings/${id}`));
    } catch (error) {
      this.error.set(error instanceof Error ? error.message : 'Listing not found');
    }
  }

  protected async buy(): Promise<void> {
    if (!(await this.auth.requestSignIn())) return;
    const deal = await this.actions.buy(this.id(), this.note().trim());
    if (deal) await this.router.navigate(['/d', deal.id]);
  }
}
