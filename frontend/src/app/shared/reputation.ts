import { Component, input } from '@angular/core';
import { ReputationDto } from '../core/models';
import { AmountPipe } from './amount.pipe';

/** A wallet's on-chain track record. */
@Component({
  selector: 'app-reputation',
  imports: [AmountPipe],
  template: `
    <dl class="facts">
      <dt>Deals completed</dt><dd>{{ reputation().dealsCompleted }}</dd>
      <dt>Disputed</dt><dd>{{ reputation().dealsDisputed }}</dd>
      <dt>Missed deadlines</dt><dd>{{ reputation().dealsUndelivered }}</dd>
      <dt>Volume</dt><dd>\${{ reputation().volume | amount }}</dd>
    </dl>
    <p class="muted small" style="margin: 0.75rem 0 0">Recorded on Solana, not by us.</p>
  `,
})
export class Reputation {
  readonly reputation = input.required<ReputationDto>();
}
