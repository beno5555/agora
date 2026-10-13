import { Component, computed, input } from '@angular/core';
import { AmountPipe } from './amount.pipe';
import { Icon } from './icon';

/** A money event the vault animates (DESIGN.md §6): funds just locked, or the deal just closed. */
export type MoneyMoment = 'lock' | 'close';

/**
 * The locked money of one deal: the total, a bar of buyer deposit | price | seller deposit, and the
 * amounts under it (DESIGN.md §4, "Vault breakdown"). The bar is decoration; the numbers are in the
 * text. Whatever goes inside the tag (who can move the money, the disagreement) is shown below.
 */
@Component({
  selector: 'app-vault',
  imports: [AmountPipe, Icon],
  styleUrl: './vault.css',
  host: {
    '[class.is-disputed]': "state() === 'disputed'",
    '[class.is-closed]': "state() === 'closed'",
    '[class.play-lock]': "moment() === 'lock'",
    '[class.play-close]': "moment() === 'close'",
  },
  template: `
    <div class="top">
      <span class="heading">
        <!-- Lucide "lock", inline so its shackle can close when the funds lock. -->
        <svg
          viewBox="0 0 24 24"
          width="16"
          height="16"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          stroke-linecap="round"
          stroke-linejoin="round"
          aria-hidden="true"
        >
          <rect width="18" height="11" x="3" y="11" rx="2" ry="2" />
          <path class="shackle" d="M7 11V7a5 5 0 0 1 10 0v4" />
        </svg>
        {{ heading() }}
      </span>
      @if (recordHref(); as href) {
        <a class="record" [href]="href" target="_blank" rel="noopener"
          >Deal record<app-icon name="external-link" /><span class="visually-hidden">
            (opens Solana Explorer)</span
          ></a
        >
      }
    </div>
    <p class="total num">\${{ shownTotal() | amount }}</p>
    @if (state() === 'closed') {
      <p class="total-label">Nothing locked</p>
    }

    <div class="bar" [class.bar--empty]="state() === 'closed'" aria-hidden="true">
      <!-- On a close, the segments stay for the length of the drain, then the dashed outline shows. -->
      @if (state() !== 'closed' || moment() === 'close') {
        <span class="seg seg--deposit" [style.flex-grow]="share(buyerDeposit())"></span>
        <span class="seg seg--price" [style.flex-grow]="share(price())">
          @if (releasedPercent() > 0) {
            <span class="released" [style.width.%]="releasedPercent()"></span>
          }
        </span>
        <span
          class="seg seg--deposit"
          [class.seg--pending]="sellerPending()"
          [style.flex-grow]="share(sellerDeposit())"
        ></span>
      }
    </div>

    <dl class="amounts">
      <div>
        <dt>{{ viewer() === 'buyer' ? 'Your deposit' : 'Buyer deposit' }}</dt>
        <dd class="num">\${{ buyerDeposit() | amount }}</dd>
      </div>
      <div>
        <dt>{{ priceLabel() }}</dt>
        <dd>
          <span class="num">\${{ price() | amount }}</span>
          @if (priceNote()) {
            <span class="note">{{ priceNote() }}</span>
          }
        </dd>
      </div>
      <div>
        <dt>{{ viewer() === 'seller' ? 'Your deposit' : 'Seller deposit' }}</dt>
        <dd>
          <span class="num">\${{ sellerDeposit() | amount }}</span>
          @if (sellerPending()) {
            <span class="note"
              >locks when {{ viewer() === 'seller' ? 'you accept' : 'they accept' }}</span
            >
          }
        </dd>
      </div>
    </dl>
    @if (releasedNote()) {
      <p class="released-note">{{ releasedNote() }}</p>
    }

    <ng-content />
  `,
})
export class Vault {
  readonly heading = input('Locked on Solana');
  readonly buyerDeposit = input.required<bigint>();
  /** The full price; the part already sent to the seller is `released`. */
  readonly price = input.required<bigint>();
  readonly released = input(0n);
  readonly sellerDeposit = input.required<bigint>();
  /** The seller has not accepted yet: their slot is drawn empty. */
  readonly sellerPending = input(false);
  readonly state = input<'locked' | 'disputed' | 'closed'>('locked');
  readonly viewer = input<'buyer' | 'seller' | null>(null);
  readonly priceLabel = input('Price');
  readonly priceNote = input('');
  readonly releasedNote = input('');
  /** Overrides the computed total (e.g. "You lock now" on a listing). */
  readonly total = input<bigint | null>(null);
  readonly recordHref = input<string | null>(null);
  /** Plays the funds-lock or the close animation while set; the page clears it afterwards. */
  readonly moment = input<MoneyMoment | null>(null);

  protected readonly shownTotal = computed(() => {
    const total = this.total();
    if (total !== null) return total;
    if (this.state() === 'closed') return 0n;
    const seller = this.sellerPending() ? 0n : this.sellerDeposit();
    return this.price() - this.released() + this.buyerDeposit() + seller;
  });

  protected readonly releasedPercent = computed(() => {
    const price = this.price();
    return price === 0n ? 0 : Number((this.released() * 10_000n) / price) / 100;
  });

  /** Flex weight of a segment; the css gives every segment at least 12% so small deposits stay visible. */
  protected share(amount: bigint): number {
    return Math.max(Number(amount), 1);
  }
}
