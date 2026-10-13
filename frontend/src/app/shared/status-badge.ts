import { Component, computed, input } from '@angular/core';
import { DealStatus } from '../core/models';
import { Icon, IconName } from './icon';

export interface StatusLook {
  tone: string;
  icon: IconName;
  label: string;
}

/** Colour, icon and wording per status (DESIGN.md §4). Anything not listed is grey with its own name. */
const LOOK: Partial<Record<DealStatus, StatusLook>> = {
  Draft: { tone: 'neutral', icon: 'circle', label: 'Not funded' },
  AwaitingSeller: { tone: 'neutral', icon: 'hourglass', label: 'Waiting for seller' },
  Active: { tone: 'blue', icon: 'circle-half', label: 'In progress' },
  Delivered: { tone: 'blue', icon: 'package', label: 'Delivered' },
  Disputed: { tone: 'amber', icon: 'alert-triangle', label: 'Disagreement' },
  Completed: { tone: 'green', icon: 'check', label: 'Completed' },
  Undelivered: { tone: 'red', icon: 'x-circle', label: 'Not delivered' },
  SellerRefunded: { tone: 'grey', icon: 'minus', label: 'Refunded by seller' },
};

/** Colour, icon and words for a status; also used by the deal timeline. */
export function statusLook(status: DealStatus): StatusLook {
  const known = LOOK[status];
  if (known) return known;
  const words = status.replace(/([a-z])([A-Z])/g, '$1 $2');
  return { tone: 'grey', icon: 'minus', label: words[0] + words.slice(1).toLowerCase() };
}

@Component({
  selector: 'app-status-badge',
  imports: [Icon],
  template: `<span class="badge" [class]="'badge--' + look().tone"
    ><app-icon [name]="look().icon" />{{ look().label }}</span
  >`,
})
export class StatusBadge {
  readonly status = input.required<DealStatus>();

  protected readonly look = computed(() => statusLook(this.status()));
}
