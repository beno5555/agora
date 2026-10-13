import { Component, computed, inject, input } from '@angular/core';
import { ClockService } from './clock.service';

const TEN_MINUTES = 600;

/**
 * Time left until a deadline: "01:42:07", or "1 day 4 hours" when more than a day is left.
 * Screen readers hear it only when it crosses ten minutes and when it runs out.
 */
@Component({
  selector: 'app-countdown',
  styles: `
    :host {
      display: inline;
    }
    .digits {
      font-family: var(--font-mono);
      font-variant-numeric: tabular-nums;
    }
  `,
  template: `
    <span class="digits" role="timer" aria-live="off">{{ text() }}</span>
    <span class="visually-hidden" aria-live="polite">{{ announcement() }}</span>
  `,
})
export class Countdown {
  readonly deadline = input.required<string>();
  private readonly clock = inject(ClockService);

  readonly secondsLeft = computed(() =>
    Math.max(0, Math.floor((new Date(this.deadline()).getTime() - this.clock.now()) / 1000)),
  );

  protected readonly text = computed(() => {
    const seconds = this.secondsLeft();
    const d = Math.floor(seconds / 86_400);
    const h = Math.floor((seconds % 86_400) / 3_600);
    const m = Math.floor((seconds % 3_600) / 60);
    const s = seconds % 60;
    if (d > 0) return `${d} day${d === 1 ? '' : 's'} ${h} hour${h === 1 ? '' : 's'}`;
    const pad = (n: number): string => String(n).padStart(2, '0');
    return `${pad(h)}:${pad(m)}:${pad(s)}`;
  });

  protected readonly announcement = computed(() => {
    const seconds = this.secondsLeft();
    if (seconds === 0) return 'The deadline has passed.';
    return seconds <= TEN_MINUTES ? 'Less than 10 minutes left.' : '';
  });
}
