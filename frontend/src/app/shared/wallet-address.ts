import { Component, computed, input, signal } from '@angular/core';
import { Icon } from './icon';

/** A short wallet address; pressing it copies the full address. */
@Component({
  selector: 'app-wallet-address',
  imports: [Icon],
  styleUrl: './wallet-address.css',
  template: `
    <button type="button" class="address" [title]="wallet()" (click)="copy()">
      {{ copied() ? 'Copied' : short() }}
      <app-icon [name]="copied() ? 'check' : 'copy'" />
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
