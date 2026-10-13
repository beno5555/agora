import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiError, ApiService } from '../../core/api.service';
import { StandardUserDto } from '../../core/models';
import { PageTitle } from '../../core/page-title';
import { Icon } from '../../shared/icon';
import { ListingCard } from '../../shared/listing-card';
import { Reputation } from '../../shared/reputation';
import { WalletAddress } from '../../shared/wallet-address';

@Component({
  selector: 'app-profile',
  imports: [RouterLink, Icon, ListingCard, Reputation, WalletAddress],
  styleUrl: './profile.css',
  template: `
    @if (user(); as u) {
      <h1>{{ u.displayName || short(u.wallet) }}</h1>
      <p class="who">
        <app-wallet-address [wallet]="u.wallet" />
        <a [href]="explorer()" target="_blank" rel="noopener"
          >View on Solana<app-icon name="external-link" /><span class="visually-hidden">
            Explorer</span
          ></a
        >
        @if (u.discord) {
          <span class="discord">Discord: {{ u.discord }}</span>
        }
      </p>

      <section class="panel" aria-labelledby="record-title">
        <h2 id="record-title">Record on Solana</h2>
        <app-reputation [reputation]="u.reputation" />
      </section>

      <h2 class="listings-title">Active listings</h2>
      @for (listing of u.activeListings; track listing.id) {
        <app-listing-card [listing]="listing" />
      } @empty {
        <p class="empty-state">No active listings right now.</p>
      }
    } @else if (notFound()) {
      <div class="missing">
        <h1>No profile here</h1>
        <p>This wallet hasn't used Agora yet, or the link is wrong.</p>
        <a routerLink="/" class="btn btn--secondary">Browse listings</a>
      </div>
    } @else {
      <p class="muted">{{ error() ?? 'Loading…' }}</p>
    }
  `,
})
export class Profile {
  readonly wallet = input.required<string>();

  private readonly api = inject(ApiService);
  private readonly pageTitle = inject(PageTitle);

  protected readonly user = signal<StandardUserDto | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly notFound = signal(false);
  protected readonly explorer = computed(
    () => `https://explorer.solana.com/address/${this.wallet()}?cluster=devnet`,
  );

  constructor() {
    effect(() => void this.load(this.wallet()));
    effect(() => {
      const user = this.user();
      if (user) this.pageTitle.set(user.displayName || this.short(user.wallet));
    });
  }

  protected short(wallet: string): string {
    return `${wallet.slice(0, 4)}…${wallet.slice(-4)}`;
  }

  private async load(wallet: string): Promise<void> {
    this.user.set(null);
    this.error.set(null);
    this.notFound.set(false);
    try {
      this.user.set(await this.api.get<StandardUserDto>(`/api/users/${wallet}`));
    } catch (error) {
      // 404 when this wallet has never signed in.
      if (error instanceof ApiError && error.status === 404) this.notFound.set(true);
      else this.error.set(error instanceof Error ? error.message : 'Could not load this profile.');
    }
  }
}
