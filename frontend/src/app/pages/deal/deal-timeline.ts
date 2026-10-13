import { Component, computed, input } from '@angular/core';
import { DealStatus, FullDealDto } from '../../core/models';
import { Icon, IconName } from '../../shared/icon';
import { statusLook } from '../../shared/status-badge';
import { unitCount } from '../../shared/unit';
import { FINAL_STATUSES } from './deal-money';

interface Step {
  label: string;
  meta: string;
  state: 'done' | 'current' | 'upcoming' | 'final';
  icon?: IconName;
  tone?: string;
  /** The line down to the next step is dashed when that step hasn't happened. */
  dashedAfter: boolean;
}

const FINAL_LABEL: Partial<Record<DealStatus, string>> = {
  Completed: 'Completed. Deposits returned.',
  Split: 'Closed with an agreed split',
  Undelivered: 'Closed: the seller missed the deadline',
  SellerRefunded: 'Refunded by the seller',
  Cancelled: 'Cancelled by the buyer',
  Expired: 'Expired before it was funded',
};

const WHEN = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' });

/** The deal's steps, with the time under each label, and how many parts are confirmed. */
@Component({
  selector: 'app-deal-timeline',
  imports: [Icon],
  styleUrl: './deal-timeline.css',
  host: { '[class.just-closed]': 'justClosed()' },
  template: `
    <ol class="steps">
      @for (step of steps(); track step.label) {
        <li [class]="step.state" [class.dashed-after]="step.dashedAfter">
          <span class="marker" [class]="'marker tone-' + (step.tone ?? 'none')">
            @if (step.icon) {
              <app-icon [name]="step.icon" size="14" />
            } @else if (step.state === 'done') {
              <app-icon name="check" size="14" />
            }
          </span>
          <span class="text">
            <span class="label">{{ step.label }}</span>
            @if (step.meta) {
              <span class="meta">{{ step.meta }}</span>
            }
          </span>
        </li>
      }
    </ol>

    @if (deal().milestones > 1) {
      <p class="parts-text">
        {{ deal().milestonesReleased }} of {{ unitCount(deal().unit, deal().milestones) }} confirmed
      </p>
      @if (deal().milestones <= 12) {
        <div class="cells" aria-hidden="true">
          @for (cell of cells(); track $index) {
            <span class="cell" [class.filled]="cell"></span>
          }
        </div>
      } @else {
        <div class="progress" aria-hidden="true">
          <span [style.width.%]="(deal().milestonesReleased / deal().milestones) * 100"></span>
        </div>
      }
    }
  `,
})
export class DealTimeline {
  readonly deal = input.required<FullDealDto>();
  /** The deal closed while the page was open: the final marker fills in after the vault drains. */
  readonly justClosed = input(false);
  protected readonly unitCount = unitCount;

  protected readonly cells = computed(() =>
    Array.from({ length: this.deal().milestones }, (_, i) => i < this.deal().milestonesReleased),
  );

  protected readonly steps = computed<Step[]>(() => {
    const d = this.deal();
    const isFinal = FINAL_STATUSES.includes(d.status);
    // The buyer can confirm straight from Active, so the seller may never mark delivered:
    // released money means something arrived, so that step counts as done. While only some
    // parts are confirmed on an open deal, delivery is still under way: that is the current step.
    const deliveredByBuyer = !d.deliveredAt && d.milestonesReleased > 0;
    const partly = deliveredByBuyer && d.milestonesReleased < d.milestones;
    const underWay = partly && !isFinal && d.status !== 'Disputed';

    const raw: Omit<Step, 'dashedAfter'>[] = [
      { label: 'Created', meta: WHEN.format(new Date(d.createdAt)), state: 'done' },
      {
        label: 'Accepted by the seller',
        meta: d.acceptedAt ? WHEN.format(new Date(d.acceptedAt)) : '',
        state: d.acceptedAt ? 'done' : 'upcoming',
      },
      {
        label: 'Delivered',
        meta: d.deliveredAt
          ? WHEN.format(new Date(d.deliveredAt))
          : underWay
            ? 'Now, part by part'
            : partly
              ? 'partly, confirmed by the buyer'
              : deliveredByBuyer
                ? 'confirmed by the buyer'
                : '',
        state: underWay ? 'current' : d.deliveredAt || deliveredByBuyer ? 'done' : 'upcoming',
      },
    ];

    let steps = raw;
    if (isFinal) {
      // A closed deal shows only what happened, then how it ended.
      const look = statusLook(d.status);
      steps = [
        ...raw.filter((s) => s.state === 'done'),
        {
          label: FINAL_LABEL[d.status] ?? look.label,
          meta: '',
          state: 'final',
          icon: look.icon,
          tone: look.tone,
        },
      ];
    } else {
      if (d.status === 'Disputed')
        steps.push({
          label: 'Disagreement',
          meta: 'Locked until you both accept the same split',
          state: 'final',
          icon: 'alert-triangle',
          tone: 'amber',
        });
      else steps.push({ label: 'Completed', meta: '', state: 'upcoming' });
      const current = steps.some((s) => s.state === 'current')
        ? undefined
        : steps.find((s) => s.state === 'upcoming');
      if (current) {
        current.state = 'current';
        current.meta ||= 'Now';
      }
    }

    return steps.map((step, i) => ({
      ...step,
      // The final step (how it ended, or the disagreement) has happened, so its line is solid too.
      dashedAfter:
        i < steps.length - 1 && (steps[i + 1].state === 'current' || steps[i + 1].state === 'upcoming'),
    }));
  });
}
