import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { MinimalDealDto, StandardUserDto } from '../../core/models';
import { NotificationService, notificationText } from '../../core/notification.service';
import { ToastService } from '../../core/toast.service';
import { AmountPipe } from '../../shared/amount.pipe';
import { ListingCard } from '../../shared/listing-card';
import { StatusBadge } from '../../shared/status-badge';

@Component({
  selector: 'app-me',
  imports: [DatePipe, RouterLink, AmountPipe, ListingCard, StatusBadge],
  template: `
    <h1>My deals</h1>

    <div class="tabs">
      <button type="button" [class.on]="role() === 'buyer'" (click)="role.set('buyer')">Buying</button>
      <button type="button" [class.on]="role() === 'seller'" (click)="role.set('seller')">Selling</button>
    </div>
    @for (deal of deals(); track deal.id) {
      <a class="row-link" [routerLink]="['/d', deal.id]">
        <div class="grow"><strong>{{ deal.title }}</strong></div>
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
      <app-listing-card [listing]="listing" />
    } @empty {
      <p class="muted">No active listings. <a routerLink="/sell">Sell something</a></p>
    }
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
  protected readonly listings = signal<StandardUserDto['activeListings']>([]);
  protected readonly text = notificationText;

  constructor() {
    effect(() => void this.loadDeals(this.role()));

    const wallet = computed(() => this.auth.user()?.wallet);
    effect(() => {
      const w = wallet();
      if (w) {
        this.api.get<StandardUserDto>(`/api/users/${w}`).then(u => this.listings.set(u.activeListings), e => this.toast.fromError(e));
      }
    });
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
}
