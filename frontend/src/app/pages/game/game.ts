import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { GameDto, ListingType, MinimalListingDto } from '../../core/models';
import { PageTitle } from '../../core/page-title';
import { ToastService } from '../../core/toast.service';
import { ListingCard } from '../../shared/listing-card';
import { TabKeys } from '../../shared/tab-keys';

const TABS: { value: ListingType | ''; label: string; plural: string }[] = [
  { value: '', label: 'All', plural: 'listings' },
  { value: 'Service', label: 'Services', plural: 'services' },
  { value: 'Item', label: 'Items', plural: 'items' },
];

@Component({
  selector: 'app-game',
  imports: [RouterLink, ListingCard, TabKeys],
  styleUrl: './game.css',
  template: `
    <h1>{{ name() }}</h1>
    @if (game(); as g) {
      <p class="count">{{ g.activeListings }} listing{{ g.activeListings === 1 ? '' : 's' }}</p>
    }

    <div class="tabs" role="tablist" appTabKeys>
      @for (tab of tabs; track tab.value) {
        <button
          type="button"
          role="tab"
          [attr.aria-selected]="selected() === tab.value"
          [tabindex]="selected() === tab.value ? 0 : -1"
          (click)="select(tab.value)"
        >
          {{ tab.label }}
        </button>
      }
    </div>

    @for (listing of listings(); track listing.id) {
      <app-listing-card [listing]="listing" />
    } @empty {
      @if (!loaded()) {
        <p class="muted">Loading…</p>
      } @else if (selected()) {
        <div class="empty-state">
          <p>No {{ plural() }} for {{ name() }} yet.</p>
          <button type="button" class="btn btn--secondary" (click)="select('')">Show all</button>
        </div>
      } @else {
        <div class="empty-state">
          <p>No {{ name() }} listings yet.</p>
          <a class="btn btn--secondary" routerLink="/sell" [queryParams]="{ game: slug() }"
            >Sell something for {{ name() }}</a
          >
        </div>
      }
    }
  `,
})
export class Game {
  readonly slug = input.required<string>();
  /** The `?type=` filter; the tabs change it in the URL. */
  readonly type = input<string>();

  private readonly api = inject(ApiService);
  private readonly pageTitle = inject(PageTitle);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  protected readonly tabs = TABS;
  protected readonly games = signal<GameDto[]>([]);
  protected readonly listings = signal<MinimalListingDto[]>([]);
  protected readonly loaded = signal(false);

  protected readonly selected = computed<ListingType | ''>(
    () => TABS.find((t) => t.value === this.type())?.value ?? '',
  );
  protected readonly plural = computed(
    () => TABS.find((t) => t.value === this.selected())?.plural ?? 'listings',
  );
  protected readonly game = computed(() => this.games().find((g) => g.slug === this.slug()));
  protected readonly name = computed(
    () => this.game()?.name ?? this.listings()[0]?.gameName ?? this.slug(),
  );

  constructor() {
    this.api.get<GameDto[]>('/api/games').then(
      (g) => this.games.set(g),
      (e) => this.toast.fromError(e),
    );
    effect(() => void this.load(this.slug(), this.selected()));
    effect(() => this.pageTitle.set(this.name()));
  }

  protected select(type: ListingType | ''): void {
    void this.router.navigate([], {
      queryParams: { type: type || null },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
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
