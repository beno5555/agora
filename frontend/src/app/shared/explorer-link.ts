import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-explorer-link',
  template: `<a [href]="href()" target="_blank" rel="noopener" class="mono">{{ short() }}</a>`,
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
