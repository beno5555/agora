import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { ApiService } from './api.service';
import { AuthService } from './auth.service';
import { NotificationDto, NotificationKind } from './models';

const TEXT: Record<NotificationKind, string> = {
  DealCreated: 'A buyer funded a deal on your listing',
  DealAccepted: 'The seller accepted your deal',
  DealDelivered: 'The seller marked your order delivered. Confirm it or disagree.',
  PartsConfirmed: 'Parts of your deal were released to you',
  DealDisputed: 'A disagreement was raised on your deal',
  SplitProposed: 'The other side offered a split',
  DealClosed: 'Your deal is closed',
  DeadlineSoon: 'Your delivery deadline is within an hour',
  RefundAvailable: 'The deadline passed without delivery. You can take your refund.',
  ReviewEndingSoon: 'Your time to check a delivery ends soon. Confirm it or disagree.',
  PaymentClaimable: "The buyer's time to check is over. You can claim your payment.",
};

export const notificationText = (kind: NotificationKind): string => TEXT[kind];

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);

  readonly items = signal<NotificationDto[]>([]);
  readonly unread = computed(() => this.items().filter((n) => !n.isRead).length);

  constructor() {
    // Poll while signed in.
    effect((onCleanup) => {
      if (!this.auth.user()) {
        this.items.set([]);
        return;
      }
      void this.refresh();
      const timer = setInterval(() => void this.refresh(), 45_000);
      onCleanup(() => clearInterval(timer));
    });
  }

  async refresh(): Promise<void> {
    try {
      this.items.set(await this.api.get<NotificationDto[]>('/api/me/notifications'));
    } catch {
      // polling: ignore a failed round
    }
  }

  async markRead(id: number): Promise<void> {
    this.items.update((list) => list.map((n) => (n.id === id ? { ...n, isRead: true } : n)));
    try {
      await this.api.post(`/api/me/notifications/${id}/read`);
    } catch {
      // the next poll restores the real state
    }
  }
}
