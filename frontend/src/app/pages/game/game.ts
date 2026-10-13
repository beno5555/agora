import { Component, computed, effect, inject, input, linkedSignal, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { GameDto, ListingType, MinimalListingDto } from '../../core/models';
import { PageTitle } from '../../core/page-title';
import { ToastService } from '../../core/toast.service';
import { ListingCard } from '../../shared/listing-card';
import { Pager, fetchPage } from '../../shared/pager';
import { TabKeys } from '../../shared/tab-keys';

const PAGE_SIZE = 10;

const TABS: { value: ListingType | ''; label: string; plural: string }[] = [
  { value: '', label: 'All', plural: 'listings' },
  { value: 'Service', label: 'Services', plural: 'services' },
  { value: 'Item', label: 'Items', plural: 'items' },
];

@Component({
  selector: 'app-game',
  imports: [RouterLink, ListingCard, Pager, TabKeys],
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
    <app-pager label="Listing pages" [page]="page()" [hasNext]="hasNext()" (go)="goTo($event)" />
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
  protected readonly hasNext = signal(false);
  /** Number of the newest request; answers to older ones are dropped. */
  private latest = 0;

  protected readonly selected = computed<ListingType | ''>(
    () => TABS.find((t) => t.value === this.type())?.value ?? '',
  );
  protected readonly plural = computed(
    () => TABS.find((t) => t.value === this.selected())?.plural ?? 'listings',
  );
  /** Back to the first page whenever the game or the tab changes. */
  protected readonly page = linkedSignal({
    source: () => [this.slug(), this.selected()],
    computation: () => 1,
  });
  protected readonly game = computed(() => this.games().find((g) => g.slug === this.slug()));
  protected readonly name = computed(
    () => this.game()?.name ?? this.listings()[0]?.gameName ?? this.slug(),
  );

  constructor() {
    this.api.get<GameDto[]>('/api/games').then(
      (g) => this.games.set(g),
      (e) => this.toast.fromError(e),
    );
    effect(() => void this.load(this.slug(), this.selected(), this.page()));
    effect(() => this.pageTitle.set(this.name()));
  }

  protected goTo(page: number): void {
    this.page.set(page);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  protected select(type: ListingType | ''): void {
    void this.router.navigate([], {
      queryParams: { type: type || null },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  private async load(game: string, type: ListingType | '', page: number): Promise<void> {
    this.loaded.set(false);
    const request = ++this.latest;
    try {
      const result = await fetchPage<MinimalListingDto>(
        this.api,
        '/api/listings',
        { game, type },
        page,
        PAGE_SIZE,
      );
      if (request !== this.latest) return;
      this.listings.set(result.items);
      this.hasNext.set(result.hasNext);
    } catch (error) {
      if (request === this.latest) this.toast.fromError(error);
    } finally {
      if (request === this.latest) this.loaded.set(true);
    }
  }
}
