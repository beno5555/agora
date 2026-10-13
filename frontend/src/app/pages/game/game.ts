import { Component, effect, inject, input, signal } from '@angular/core';
import { ApiService } from '../../core/api.service';
import { ListingType, MinimalListingDto } from '../../core/models';
import { ToastService } from '../../core/toast.service';
import { ListingCard } from '../../shared/listing-card';
import { TabKeys } from '../../shared/tab-keys';

@Component({
  selector: 'app-game',
  imports: [ListingCard, TabKeys],
  template: `
    <h1>{{ listings()[0]?.gameName ?? slug() }}</h1>
    <div class="tabs" role="tablist" appTabKeys>
      @for (tab of tabs; track tab.value) {
        <button
          type="button"
          role="tab"
          [attr.aria-selected]="type() === tab.value"
          [tabindex]="type() === tab.value ? 0 : -1"
          (click)="type.set(tab.value)"
        >
          {{ tab.label }}
        </button>
      }
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

  protected readonly tabs: { value: ListingType | ''; label: string }[] = [
    { value: '', label: 'All' },
    { value: 'Service', label: 'Services' },
    { value: 'Item', label: 'Items' },
  ];
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
