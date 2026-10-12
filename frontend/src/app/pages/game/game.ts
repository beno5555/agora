import { Component, effect, inject, input, signal } from '@angular/core';
import { ApiService } from '../../core/api.service';
import { ListingType, MinimalListingDto } from '../../core/models';
import { ToastService } from '../../core/toast.service';
import { ListingCard } from '../../shared/listing-card';

@Component({
  selector: 'app-game',
  imports: [ListingCard],
  template: `
    <h1>{{ listings()[0]?.gameName ?? slug() }}</h1>
    <div class="tabs">
      <button type="button" [class.on]="type() === ''" (click)="type.set('')">All</button>
      <button type="button" [class.on]="type() === 'Service'" (click)="type.set('Service')">Services</button>
      <button type="button" [class.on]="type() === 'Item'" (click)="type.set('Item')">Items</button>
    </div>
    @for (listing of listings(); track listing.id) {
      <app-listing-card [listing]="listing" />
    } @empty {
      <p class="muted">{{ loaded() ? 'No listings here yet.' : 'Loading…' }}</p>
    }
  `,
})
export class Game {
  readonly slug = input.required<string>();

  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);

  protected readonly type = signal<ListingType | ''>('');
  protected readonly listings = signal<MinimalListingDto[]>([]);
  protected readonly loaded = signal(false);

  constructor() {
    effect(() => void this.load(this.slug(), this.type()));
  }

  private async load(game: string, type: ListingType | ''): Promise<void> {
    this.loaded.set(false);
    try {
      this.listings.set(await this.api.get<MinimalListingDto[]>('/api/listings', { game, type }));
    } catch (error) {
      this.toast.fromError(error);
    } finally {
      this.loaded.set(true);
    }
  }
}
