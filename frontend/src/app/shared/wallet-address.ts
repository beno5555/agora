import { Component, computed, input, signal } from '@angular/core';

@Component({
  selector: 'app-wallet-address',
  template: `
    <button type="button" class="mono linklike" [title]="wallet()" (click)="copy()">
      {{ copied() ? 'Copied' : short() }}
    </button>
  `,
})
export class WalletAddress {
  readonly wallet = input.required<string>();
  protected readonly copied = signal(false);
  protected readonly short = computed(
    () => `${this.wallet().slice(0, 4)}…${this.wallet().slice(-4)}`,
  );

  protected async copy(): Promise<void> {
    await navigator.clipboard.writeText(this.wallet());
    this.copied.set(true);
    setTimeout(() => this.copied.set(false), 1500);
  }
}
