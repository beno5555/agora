import { Component, computed, input } from '@angular/core';
import { Icon } from './icon';

/** A short account or transaction id that opens Solana Explorer (devnet) in a new tab. */
@Component({
  selector: 'app-explorer-link',
  imports: [Icon],
  styles: `
    a {
      display: inline-flex;
      align-items: center;
      gap: var(--s-1);
      min-height: 44px;
      font-family: var(--font-mono);
      font-size: var(--fs-sm);
    }
  `,
  template: `<a [href]="href()" target="_blank" rel="noopener" [title]="value()"
    >{{ short() }}<app-icon name="external-link" /><span class="visually-hidden">
      (view on Solana Explorer)</span
    ></a
  >`,
})
export class ExplorerLink {
  readonly kind = input.required<'tx' | 'address'>();
  readonly value = input.required<string>();

  protected readonly href = computed(
    () => `https://explorer.solana.com/${this.kind()}/${this.value()}?cluster=devnet`,
  );
  protected readonly short = computed(
    () => `${this.value().slice(0, 6)}…${this.value().slice(-6)}`,
  );
}
