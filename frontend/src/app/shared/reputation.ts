import { Component, computed, input } from '@angular/core';
import { ReputationDto } from '../core/models';
import { AmountPipe } from './amount.pipe';

/** A wallet's record on Solana as plain counts (no stars, no badges). */
@Component({
  selector: 'app-reputation',
  imports: [AmountPipe],
  styleUrl: './reputation.css',
  template: `
    @if (hasRecord()) {
      <dl class="counts">
        <div>
          <dt>completed</dt>
          <dd class="num">{{ reputation().dealsCompleted }}</dd>
        </div>
        <div>
          <dt>not delivered</dt>
          <dd class="num">{{ reputation().dealsUndelivered }}</dd>
        </div>
        <div>
          <dt>{{ reputation().dealsDisputed === 1 ? 'disagreement' : 'disagreements' }}</dt>
          <dd class="num">{{ reputation().dealsDisputed }}</dd>
        </div>
      </dl>
      <p class="note">
        <span class="num">\${{ reputation().volume | amount }}</span> in finished deals. Read from
        Solana, not from reviews.
      </p>
    } @else {
      <p class="note">
        No finished deals on Solana yet. A new seller starts with a clean record, not a bad one.
      </p>
    }
  `,
})
export class Reputation {
  readonly reputation = input.required<ReputationDto>();

  protected readonly hasRecord = computed(() => {
    const r = this.reputation();
    return r.dealsCompleted + r.dealsUndelivered + r.dealsDisputed > 0;
  });
}
