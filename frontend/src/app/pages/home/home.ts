import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { ConfigService } from '../../core/config.service';
import { GameDto, MinimalListingDto } from '../../core/models';
import { ToastService } from '../../core/toast.service';
import { Icon } from '../../shared/icon';
import { ListingCard } from '../../shared/listing-card';
import { Vault } from '../../shared/vault';

/** The example deal in the hero: a $10.00 service in 8 parts, with $2.00 deposits on both sides. */
const EXAMPLE = { price: 10, deposit: 2, parts: 8 };

@Component({
  selector: 'app-home',
  imports: [FormsModule, RouterLink, Icon, ListingCard, Vault],
  styleUrl: './home.css',
  template: `
    <section class="hero ink" aria-labelledby="hero-title">
      <div class="copy">
        <h1 id="hero-title">Gaming deals where nobody holds your money.</h1>
        <p class="lead">
          You and the seller each lock a deposit on Solana. Money moves when you both agree, or
          when the seller's deadline runs out. Agora never holds it.
        </p>
      </div>

      <figure class="example">
        <app-vault
          [buyerDeposit]="example().deposit"
          [price]="example().price"
          [sellerDeposit]="example().deposit"
          priceLabel="Price, 8 ranks"
          [priceNote]="example().each"
        >
          <p class="no-key">Nobody else. Agora has no key.</p>
          <ul class="keys">
            <li><app-icon name="key" />Buyer: confirms each rank, or disagrees</li>
            <li><app-icon name="key" />Seller: refunds, or agrees to a split</li>
            <li>
              <app-icon name="clock" />The deadline: if nothing arrives, the buyer takes a refund
              plus the seller's deposit
            </li>
          </ul>
        </app-vault>
        <figcaption>An example deal: a rank boost in 8 parts, paid one rank at a time.</figcaption>
      </figure>

      <div class="actions">
        <a class="btn btn--primary" href="#listings" (click)="toListings($event)"
          >Browse listings</a
        >
        <a class="btn btn--secondary" routerLink="/sell">Sell something</a>
      </div>
    </section>

    <section class="games-section" aria-labelledby="games-title">
      <h2 id="games-title">Games</h2>
      <ul class="games">
        @for (game of sortedGames(); track game.id) {
          <li>
            <a [routerLink]="['/g', game.slug]" [class.empty]="game.activeListings === 0">
              <span class="name">{{ game.name }}</span>
              <span class="count num"
                >{{ game.activeListings
                }}<span class="visually-hidden">
                  listing{{ game.activeListings === 1 ? '' : 's' }}</span
                ></span
              >
              <app-icon name="arrow-right" class="arrow" />
            </a>
          </li>
        }
      </ul>
    </section>

    <section id="listings" class="listings" aria-labelledby="listings-title">
      <h2 id="listings-title">Newest listings</h2>
      <form class="inline-field search" role="search" (ngSubmit)="search()">
        <input
          type="search"
          name="q"
          placeholder="Search listings…"
          aria-label="Search listings"
          [(ngModel)]="query"
        />
        <button type="submit" class="btn btn--secondary">Search</button>
      </form>
      @for (listing of listings(); track listing.id) {
        <app-listing-card [listing]="listing" />
      } @empty {
        @if (!loaded()) {
          <p class="muted">Loading…</p>
        } @else if (searched()) {
          <p class="empty-state">
            Nothing matches “{{ searched() }}”. Try a game name or a shorter word.
          </p>
        } @else {
          <div class="empty-state">
            <p>No listings yet. Selling a carry, coaching or items? Create the first listing.</p>
            <a class="btn btn--secondary" routerLink="/sell">Sell something</a>
          </div>
        }
      }
    </section>
  `,
})
export class Home {
  private readonly api = inject(ApiService);
  private readonly config = inject(ConfigService);
  private readonly toast = inject(ToastService);

  protected readonly games = signal<GameDto[]>([]);
  protected readonly listings = signal<MinimalListingDto[]>([]);
  protected readonly loaded = signal(false);
  protected readonly query = signal('');
  /** The query the shown listings were searched with, for the empty state. */
  protected readonly searched = signal('');

  /** Games with listings first; the order within each group stays the API's. */
  protected readonly sortedGames = computed(() =>
    [...this.games()].sort(
      (a, b) => Number(b.activeListings > 0) - Number(a.activeListings > 0),
    ),
  );

  protected readonly example = computed(() => {
    const unit = 10n ** BigInt(this.config.decimals());
    return {
      price: BigInt(EXAMPLE.price) * unit,
      deposit: BigInt(EXAMPLE.deposit) * unit,
      each: `$${(EXAMPLE.price / EXAMPLE.parts).toFixed(2)} each`,
    };
  });

  constructor() {
    this.api.get<GameDto[]>('/api/games').then(
      (g) => this.games.set(g),
      (e) => this.toast.fromError(e),
    );
    void this.load();
  }

  protected toListings(event: Event): void {
    event.preventDefault();
    document.getElementById('listings')?.scrollIntoView({ behavior: 'smooth' });
  }

  protected search(): void {
    void this.load();
  }

  private async load(): Promise<void> {
    const q = this.query().trim();
    try {
      this.listings.set(await this.api.get<MinimalListingDto[]>('/api/listings', { q }));
      this.searched.set(q);
    } catch (error) {
      this.toast.fromError(error);
    } finally {
      this.loaded.set(true);
    }
  }
}
