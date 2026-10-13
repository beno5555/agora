import { DatePipe } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { FullListingDto, FullUserDto, MinimalDealDto } from '../../core/models';
import { NotificationService, notificationText } from '../../core/notification.service';
import { ToastService } from '../../core/toast.service';
import { AmountPipe } from '../../shared/amount.pipe';
import { ListingCard } from '../../shared/listing-card';
import { StatusBadge } from '../../shared/status-badge';
import { TabKeys } from '../../shared/tab-keys';

@Component({
  selector: 'app-me',
  imports: [DatePipe, FormsModule, RouterLink, AmountPipe, ListingCard, StatusBadge, TabKeys],
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
    @for (deal of deals(); track deal.id) {
      <a class="row-link" [routerLink]="['/d', deal.id]">
        <div class="grow">
          <strong>{{ deal.title }}</strong>
        </div>
        <app-status-badge [status]="deal.status" />
        <div class="price">\${{ deal.price | amount }}</div>
      </a>
    } @empty {
      <p class="muted">{{ loadedDeals() ? 'No deals yet.' : 'Loading…' }}</p>
    }

    <h2 style="margin-top: 2rem">Notifications</h2>
    @for (n of notifications.items(); track n.id) {
      <a
        class="row-link"
        [class.unread]="!n.isRead"
        [routerLink]="n.dealId ? ['/d', n.dealId] : null"
        (click)="notifications.markRead(n.id)"
      >
        <div class="grow">{{ text(n.kind) }}</div>
        <span class="muted small">{{ n.createdAt | date: 'short' }}</span>
      </a>
    } @empty {
      <p class="muted">Nothing new.</p>
    }

    <h2 style="margin-top: 2rem">My listings</h2>
    @for (listing of listings(); track listing.id) {
      <div class="with-action">
        <app-listing-card [listing]="listing" />
        @if (!listing.isActive) {
          <span class="badge badge--grey">Paused</span>
        }
        <button
          type="button"
          class="btn btn--secondary btn--sm"
          [disabled]="toggling() === listing.id"
          (click)="toggle(listing)"
        >
          {{ listing.isActive ? 'Pause' : 'Resume' }}
        </button>
      </div>
    } @empty {
      <p class="muted">
        {{ loadedListings() ? 'No listings yet.' : 'Loading…' }}
        <a routerLink="/sell">Sell something</a>
      </p>
    }

    <h2 style="margin-top: 2rem">Profile</h2>
    <section class="panel">
      <label>
        Display name <span class="hint">(shown instead of your wallet address)</span>
        <input maxlength="40" [(ngModel)]="displayName" />
      </label>
      <label>
        Discord <span class="hint">(so the other side of a deal can reach you)</span>
        <input maxlength="40" [(ngModel)]="discord" />
      </label>
      <button
        type="button"
        class="btn btn--primary"
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

  constructor() {
    effect(() => void this.loadDeals(this.role()));
    void this.loadListings();
  }

  protected async toggle(listing: FullListingDto): Promise<void> {
    const action = listing.isActive ? 'pause' : 'resume';
    this.toggling.set(listing.id);
    try {
      await this.api.post(`/api/listings/${listing.id}/${action}`);
      await this.loadListings();
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
      this.toast.show('Profile saved');
    } catch (error) {
      this.toast.fromError(error);
    } finally {
      this.savingProfile.set(false);
    }
  }

  private async loadDeals(role: 'buyer' | 'seller'): Promise<void> {
    this.loadedDeals.set(false);
    try {
      this.deals.set(await this.api.get<MinimalDealDto[]>('/api/me/deals', { role }));
    } catch (error) {
      this.toast.fromError(error);
    } finally {
      this.loadedDeals.set(true);
    }
  }

  /** The caller's own listings, paused ones included. */
  private async loadListings(): Promise<void> {
    try {
      this.listings.set(await this.api.get<FullListingDto[]>('/api/me/listings'));
    } catch (error) {
      this.toast.fromError(error);
    } finally {
      this.loadedListings.set(true);
    }
  }
}
