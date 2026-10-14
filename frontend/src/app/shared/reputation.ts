import { Component, computed, input } from '@angular/core';
import { ReputationDto } from '../core/models';
import { AmountPipe } from './amount.pipe';

/** A wallet's record on Solana in one role, as plain counts (no stars, no badges). */
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
        @if (role() === 'seller') {
          <div>
            <dt>not delivered</dt>
            <dd class="num">{{ reputation().dealsUndelivered }}</dd>
          </div>
        }
        <div>
          <dt>{{ reputation().dealsDisputed === 1 ? 'disagreement' : 'disagreements' }}</dt>
          <dd class="num">{{ reputation().dealsDisputed }}</dd>
        </div>
      </dl>
      @if (reputation().openDisputes > 0) {
        <p class="note open">
          <span class="num">{{ reputation().openDisputes }}</span>
          {{ reputation().openDisputes === 1 ? 'disagreement is' : 'disagreements are' }} still
          open. Until settled, each one raises this wallet's deposit.
        </p>
      }
      <p class="note">
        <span class="num">\${{ reputation().volume | amount }}</span> in finished deals. Read from
        Solana, not from reviews.
      </p>
    } @else {
      <p class="note">
        No finished deals as {{ role() }} yet. A new {{ role() }} starts with a clean record, not a
        bad one.
      </p>
    }
  `,
})
export class Reputation {
  readonly reputation = input.required<ReputationDto>();
  /** Records are kept per role; "not delivered" only applies to sellers. */
  readonly role = input<'seller' | 'buyer'>('seller');

  protected readonly hasRecord = computed(() => {
    const r = this.reputation();
    return r.dealsCompleted + r.dealsUndelivered + r.dealsDisputed + r.openDisputes > 0;
  });
}
