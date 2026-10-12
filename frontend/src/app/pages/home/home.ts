import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { GameDto, MinimalListingDto } from '../../core/models';
import { ToastService } from '../../core/toast.service';
import { ListingCard } from '../../shared/listing-card';

@Component({
  selector: 'app-home',
  imports: [FormsModule, RouterLink, ListingCard],
  template: `
    <h1>Gaming deals where nobody holds your money</h1>
    <p class="muted">Both sides lock a deposit on Solana. Funds only move when you both agree.</p>

    <div class="games">
      @for (game of games(); track game.id) {
        <a [routerLink]="['/g', game.slug]">
          <strong>{{ game.name }}</strong>
          <span class="muted small"
            >{{ game.activeListings }} listing{{ game.activeListings === 1 ? '' : 's' }}</span
          >
        </a>
      }
    </div>

    <h2>Newest listings</h2>
    <div class="inline-field" style="margin-bottom: 1rem">
      <input
        type="search"
        placeholder="Search listings"
        [(ngModel)]="query"
        (keyup.enter)="load()"
        style="width: 100%"
      />
      <button type="button" (click)="load()">Search</button>
    </div>
    @for (listing of listings(); track listing.id) {
      <app-listing-card [listing]="listing" />
    } @empty {
      <p class="muted">{{ loaded() ? 'No listings found.' : 'Loading…' }}</p>
    }
  `,
})
export class Home {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);

  protected readonly games = signal<GameDto[]>([]);
  protected readonly listings = signal<MinimalListingDto[]>([]);
  protected readonly loaded = signal(false);
  protected readonly query = signal('');

  constructor() {
    this.api.get<GameDto[]>('/api/games').then(
      (g) => this.games.set(g),
      (e) => this.toast.fromError(e),
    );
    void this.load();
  }

  protected async load(): Promise<void> {
    try {
      this.listings.set(
        await this.api.get<MinimalListingDto[]>('/api/listings', { q: this.query() }),
      );
    } catch (error) {
      this.toast.fromError(error);
    } finally {
      this.loaded.set(true);
    }
  }
}
