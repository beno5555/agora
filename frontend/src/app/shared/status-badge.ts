import { Component, computed, input } from '@angular/core';
import { DealStatus } from '../core/models';

const TONE: Partial<Record<DealStatus, string>> = {
  Draft: 'neutral',
  AwaitingSeller: 'neutral',
  Active: 'blue',
  Delivered: 'blue',
  Disputed: 'amber',
  Completed: 'green',
  Undelivered: 'red',
};

@Component({
  selector: 'app-status-badge',
  template: `<span class="badge" [class]="tone()">{{ label() }}</span>`,
})
export class StatusBadge {
  readonly status = input.required<DealStatus>();
  protected readonly tone = computed(() => TONE[this.status()] ?? 'grey');
  protected readonly label = computed(() =>
    this.status().replace(/([a-z])([A-Z])/g, '$1 $2').replace('Seller Refunded', 'Refunded by seller'),
  );
}
