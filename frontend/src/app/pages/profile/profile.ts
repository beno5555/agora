import { Component, effect, inject, input, signal } from '@angular/core';
import { ApiService } from '../../core/api.service';
import { StandardUserDto } from '../../core/models';
import { ExplorerLink } from '../../shared/explorer-link';
import { ListingCard } from '../../shared/listing-card';
import { Reputation } from '../../shared/reputation';
import { WalletAddress } from '../../shared/wallet-address';

@Component({
  selector: 'app-profile',
  imports: [ExplorerLink, ListingCard, Reputation, WalletAddress],
  template: `
    @if (user(); as u) {
      <h1>{{ u.displayName || 'Trader' }}</h1>
      <p class="muted">
        <app-wallet-address [wallet]="u.wallet" />
        · <app-explorer-link kind="address" [value]="u.wallet" />
        @if (u.discord) {
          · Discord: {{ u.discord }}
        }
      </p>

      <section class="panel">
        <h2>Track record</h2>
        <app-reputation [reputation]="u.reputation" />
      </section>

      <h2>Active listings</h2>
      @for (listing of u.activeListings; track listing.id) {
        <app-listing-card [listing]="listing" />
      } @empty {
        <p class="muted">No active listings.</p>
      }
    } @else {
      <p class="muted">{{ error() ?? 'Loading…' }}</p>
    }
  `,
})
export class Profile {
  readonly wallet = input.required<string>();

  private readonly api = inject(ApiService);

  protected readonly user = signal<StandardUserDto | null>(null);
  protected readonly error = signal<string | null>(null);

  constructor() {
    effect(() => void this.load(this.wallet()));
  }

  private async load(wallet: string): Promise<void> {
    this.user.set(null);
    this.error.set(null);
    try {
      this.user.set(await this.api.get<StandardUserDto>(`/api/users/${wallet}`));
    } catch (error) {
      // 404 when this wallet has never signed in.
      this.error.set(error instanceof Error ? error.message : 'User not found');
    }
  }
}
