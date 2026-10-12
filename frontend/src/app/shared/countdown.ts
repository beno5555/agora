import { Component, computed, inject, input } from '@angular/core';
import { ClockService } from './clock.service';

@Component({
  selector: 'app-countdown',
  template: `{{ text() }}`,
})
export class Countdown {
  readonly deadline = input.required<string>();
  private readonly clock = inject(ClockService);

  protected readonly text = computed(() => {
    const seconds = Math.floor((new Date(this.deadline()).getTime() - this.clock.now()) / 1000);
    if (seconds <= 0) return '0s';
    const d = Math.floor(seconds / 86400);
    const h = Math.floor((seconds % 86400) / 3600);
    const m = Math.floor((seconds % 3600) / 60);
    const s = seconds % 60;
    if (d > 0) return `${d}d ${h}h ${m}m`;
    if (h > 0) return `${h}h ${m}m ${s}s`;
    return m > 0 ? `${m}m ${s}s` : `${s}s`;
  });
}
