import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject, linkedSignal, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { DealAction, FullListingDto, FullUserDto, MinimalDealDto } from '../../core/models';
import { NotificationService, notificationText } from '../../core/notification.service';
import { ToastService } from '../../core/toast.service';
import { AmountPipe } from '../../shared/amount.pipe';
import { Icon } from '../../shared/icon';
import { ListingCard } from '../../shared/listing-card';
import { Pager, fetchPage } from '../../shared/pager';
import { StatusBadge } from '../../shared/status-badge';
import { TabKeys } from '../../shared/tab-keys';
import { TimeAgoPipe } from '../../shared/time-ago.pipe';
import { primaryAction } from '../deal/deal-money';

/** What "Your move" means on a deal row, by the action waiting on the viewer. */
const MOVE: Partial<Record<DealAction, string>> = {
  Accept: 'accept',
  Confirm: 'confirm',
  MarkDelivered: 'mark delivered',
  AcceptSplit: 'answer their offer',
  RefundUndelivered: 'take your refund',
};

const PAGE_SIZE = 10;
const NOTES_COLLAPSED = 5;

interface DealRow {
  deal: MinimalDealDto;
  move: string | null;
}

@Component({
  selector: 'app-me',
  imports: [
    DatePipe,
    FormsModule,
    RouterLink,
    AmountPipe,
    Icon,
    ListingCard,
    Pager,
    StatusBadge,
    TabKeys,
    TimeAgoPipe,
  ],
  styleUrl: './me.css',
  template: `
    <h1>My deals</h1>

    <div class="tabs" role="tablist" appTabKeys>
      <button
        type="button"
        role="tab"
        [attr.aria-selected]="role() === 'buyer'"
        [tabindex]="role() === 'buyer' ? 0 : -1"
        (click)="role.set('buyer')"
      >
        Buying
      </button>
      <button
        type="button"
        role="tab"
        [attr.aria-selected]="role() === 'seller'"
        [tabindex]="role() === 'seller' ? 0 : -1"
        (click)="role.set('seller')"
      >
        Selling
      </button>
    </div>

    <div class="deals">
      @for (row of rows(); track row.deal.id) {
        <a class="deal-row" [routerLink]="['/d', row.deal.id]">
          <span class="title">{{ row.deal.title }}</span>
          <app-status-badge class="status" [status]="row.deal.status" />
          @if (row.move) {
            <span class="move"><app-icon name="arrow-right" />Your move: {{ row.move }}</span>
          }
          <span class="price num">\${{ row.deal.price | amount }}</span>
        </a>
      } @empty {
        @if (!loadedDeals()) {
          <p class="muted">Loading…</p>
        } @else if (role() === 'buyer') {
          <div class="empty-state">
            <p>You haven't bought anything yet. Every deal you start shows up here.</p>
            <a class="btn btn--secondary" routerLink="/">Browse listings</a>
          </div>
        } @else {
          <div class="empty-state">
            <p>
              No one has bought from you yet. Share your listing link where your buyers are:
              Discord, X or Reddit.
            </p>
            <a class="btn btn--secondary" href="#my-listings" (click)="toListings($event)"
              >Go to my listings</a
            >
          </div>
        }
      }
      <app-pager
        label="Deal pages"
        [page]="dealPage()"
        [hasNext]="moreDeals()"
        (go)="dealPage.set($event)"
      />
    </div>

    <div class="columns">
      <section aria-labelledby="notes-title">
        <h2 id="notes-title">Notifications</h2>
        <div id="note-list">
          @for (n of shownNotes(); track n.id) {
            <a
              class="note-row"
              [class.unread]="!n.isRead"
              [routerLink]="n.dealId ? ['/d', n.dealId] : null"
              (click)="notifications.markRead(n.id)"
            >
              <span class="grow"
                >{{ text(n.kind) }}
                @if (!n.isRead) {
                  <span class="visually-hidden">(unread)</span>
                }
              </span>
              <time
                class="when"
                [attr.datetime]="n.createdAt"
                [title]="n.createdAt | date: 'medium'"
                >{{ n.createdAt | timeAgo }}</time
              >
            </a>
          } @empty {
            <p class="empty-state">
              Nothing new. You'll get a note here when the other side of a deal does something.
            </p>
          }
        </div>
        @if (notifications.items().length > NOTES_COLLAPSED) {
          <button
            type="button"
            class="btn btn--ghost btn--sm see-more"
            aria-controls="note-list"
            [attr.aria-expanded]="notesOpen()"
            (click)="notesOpen.set(!notesOpen())"
          >
            {{
              notesOpen()
                ? 'See less'
                : 'See ' + (notifications.items().length - NOTES_COLLAPSED) + ' more'
            }}
            <app-icon name="chevron-down" class="chevron" [class.open]="notesOpen()" />
          </button>
        }
      </section>

      <section id="my-listings" aria-labelledby="listings-title" tabindex="-1">
        <h2 id="listings-title">My listings</h2>
        @for (listing of listings(); track listing.id) {
          <div class="listing">
            <app-listing-card class="is-flush" [listing]="listing" />
            <div class="listing-actions">
              @if (!listing.isActive) {
                <span class="badge badge--grey">Paused</span>
              }
              <button
                type="button"
                class="btn btn--secondary btn--sm"
                [class.is-loading]="toggling() === listing.id"
                [disabled]="toggling() === listing.id"
                (click)="toggle(listing)"
              >
                {{ listing.isActive ? 'Pause' : 'Resume' }}
              </button>
            </div>
          </div>
        } @empty {
          @if (!loadedListings()) {
            <p class="muted">Loading…</p>
          } @else {
            <div class="empty-state">
              <p>You have no listings.</p>
              <a class="btn btn--secondary" routerLink="/sell">Create a listing</a>
            </div>
          }
        }
        <app-pager
          label="Listing pages"
          [page]="listingPage()"
          [hasNext]="moreListings()"
          (go)="listingPage.set($event)"
        />
      </section>
    </div>

    <section class="panel profile" aria-labelledby="profile-title">
      <h2 id="profile-title">Profile</h2>
      <label>
        Display name
        <input maxlength="40" aria-describedby="name-hint" [(ngModel)]="displayName" />
        <span class="hint" id="name-hint">Shown instead of your wallet address.</span>
      </label>
      <label>
        Discord
        <input maxlength="40" aria-describedby="discord-hint" [(ngModel)]="discord" />
        <span class="hint" id="discord-hint">So the other side of a deal can reach you.</span>
      </label>
      <button
        type="button"
        class="btn btn--primary"
        [class.is-loading]="savingProfile()"
        [disabled]="savingProfile()"
        (click)="saveProfile()"
      >
        {{ savingProfile() ? 'Saving…' : 'Save profile' }}
      </button>
    </section>
  `,
})
export class Me {
  protected readonly notifications = inject(NotificationService);
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);

  protected readonly role = signal<'buyer' | 'seller'>('buyer');
  protected readonly deals = signal<MinimalDealDto[]>([]);
  protected readonly loadedDeals = signal(false);
  protected readonly listings = signal<FullListingDto[]>([]);
  protected readonly loadedListings = signal(false);
  /** Id of the listing being paused or resumed. */
  protected readonly toggling = signal<string | null>(null);
  protected readonly displayName = signal(this.auth.user()?.displayName ?? '');
  protected readonly discord = signal(this.auth.user()?.discord ?? '');
  protected readonly savingProfile = signal(false);
  protected readonly text = notificationText;
  /** Deals start over at page 1 when the Buying/Selling tab changes. */
  protected readonly dealPage = linkedSignal({ source: this.role, computation: () => 1 });
  protected readonly moreDeals = signal(false);
  private latestDeals = 0;
  protected readonly listingPage = signal(1);
  protected readonly moreListings = signal(false);
  /** Notifications show the newest few until "See more" opens the rest. */
  protected readonly NOTES_COLLAPSED = NOTES_COLLAPSED;
  protected readonly notesOpen = signal(false);
  protected readonly shownNotes = computed(() =>
    this.notesOpen()
      ? this.notifications.items()
      : this.notifications.items().slice(0, NOTES_COLLAPSED),
  );

  /** Deals waiting on the viewer first; otherwise the API's order (newest first). */
  protected readonly rows = computed<DealRow[]>(() =>
    this.deals()
      .map((deal) => {
        // A backend from before rows carried allowedActions sends none; one row that throws here
        // would stop the whole page from updating.
        const action = primaryAction(deal.allowedActions ?? []);
        return { deal, move: action ? (MOVE[action] ?? null) : null };
      })
      .sort((a, b) => Number(!!b.move) - Number(!!a.move)),
  );

  constructor() {
    effect(() => void this.loadDeals(this.role(), this.dealPage()));
    effect(() => void this.loadListings(this.listingPage()));
  }

  protected toListings(event: Event): void {
    event.preventDefault();
    const section = document.getElementById('my-listings');
    section?.scrollIntoView({ behavior: 'smooth' });
    section?.focus({ preventScroll: true });
  }

  protected async toggle(listing: FullListingDto): Promise<void> {
    const action = listing.isActive ? 'pause' : 'resume';
    this.toggling.set(listing.id);
    try {
      await this.api.post(`/api/listings/${listing.id}/${action}`);
      await this.loadListings(this.listingPage());
    } catch (error) {
      this.toast.fromError(error);
    } finally {
      this.toggling.set(null);
    }
  }

  protected async saveProfile(): Promise<void> {
    this.savingProfile.set(true);
    try {
      // An empty value clears the field.
      const user = await this.api.patch<FullUserDto>('/api/me', {
        displayName: this.displayName().trim(),
        discord: this.discord().trim(),
      });
      this.auth.user.set(user);
      this.toast.show('Profile saved', 'success');
    } catch (error) {
      this.toast.fromError(error);
    } finally {
      this.savingProfile.set(false);
    }
  }

  private async loadDeals(role: 'buyer' | 'seller', page: number): Promise<void> {
    this.loadedDeals.set(false);
    const request = ++this.latestDeals;
    try {
      const result = await fetchPage<MinimalDealDto>(
        this.api,
        '/api/me/deals',
        { role },
        page,
        PAGE_SIZE,
      );
      // Switching tabs quickly must not show the other tab's answer.
      if (request !== this.latestDeals) return;
      this.deals.set(result.items);
      this.moreDeals.set(result.hasNext);
    } catch (error) {
      if (request === this.latestDeals) this.toast.fromError(error);
    } finally {
      if (request === this.latestDeals) this.loadedDeals.set(true);
    }
  }

  /** The caller's own listings, paused ones included. */
  private async loadListings(page: number): Promise<void> {
    try {
      const result = await fetchPage<FullListingDto>(
        this.api,
        '/api/me/listings',
        {},
        page,
        PAGE_SIZE,
      );
      this.listings.set(result.items);
      this.moreListings.set(result.hasNext);
    } catch (error) {
      this.toast.fromError(error);
    } finally {
      this.loadedListings.set(true);
    }
  }
}
