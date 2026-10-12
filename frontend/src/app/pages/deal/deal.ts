import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { ConfigService } from '../../core/config.service';
import { DealActionService } from '../../core/deal-action.service';
import { DealAction, DealActionDto, DealStatus, FullDealDto } from '../../core/models';
import { AmountPipe, formatAmount, parseAmount } from '../../shared/amount.pipe';
import { ClockService } from '../../shared/clock.service';
import { Countdown } from '../../shared/countdown';
import { ExplorerLink } from '../../shared/explorer-link';
import { plural } from '../../shared/plural';
import { StatusBadge } from '../../shared/status-badge';
import { WalletAddress } from '../../shared/wallet-address';

const FINAL: DealStatus[] = [
  'Completed',
  'Split',
  'Undelivered',
  'SellerRefunded',
  'Cancelled',
  'Expired',
];
const NOTHING = '—';

/** One row of the "Where the money goes" table. */
interface Outcome {
  label: string;
  buyer: string;
  seller: string;
}

@Component({
  selector: 'app-deal',
  imports: [
    DatePipe,
    FormsModule,
    RouterLink,
    AmountPipe,
    Countdown,
    ExplorerLink,
    StatusBadge,
    WalletAddress,
  ],
  template: `
    @if (deal(); as d) {
      <p class="muted small">
        <a [routerLink]="['/l', d.listingId]">{{ d.gameName }}</a> · you are the {{ d.role }}
      </p>
      <h1>{{ d.title }} <app-status-badge [status]="d.status" /></h1>

      @if (actions.phase() === 'confirming') {
        <p class="notice">Confirming on Solana…</p>
      }

      <section class="panel">
        <h2>The vault</h2>
        @if (isLocked()) {
          <div class="vault-bar" aria-hidden="true">
            @if (width(remaining()) > 0) {
              <span class="price-seg" [style.width.%]="width(remaining())">Price</span>
            }
            @if (width(buyerDeposit()) > 0) {
              <span class="buyer-seg" [style.width.%]="width(buyerDeposit())">Buyer deposit</span>
            }
            @if (width(sellerDeposit()) > 0) {
              <span class="seller-seg" [style.width.%]="width(sellerDeposit())"
                >Seller deposit</span
              >
            }
          </div>
        }
        <dl class="facts">
          <dt>Price</dt>
          <dd>\${{ d.price | amount }}</dd>
          @if (d.milestonesReleased > 0) {
            <dt>Already released</dt>
            <dd>\${{ released() | amount }}</dd>
          }
          <dt>Buyer deposit</dt>
          <dd>\${{ d.buyerDeposit | amount }}</dd>
          <dt>Seller deposit</dt>
          <dd>
            {{
              d.status === 'AwaitingSeller'
                ? 'locks when the seller accepts'
                : '$' + (d.sellerDeposit | amount)
            }}
          </dd>
          <dt class="total">Total locked</dt>
          <dd class="total">\${{ locked() | amount }}</dd>
        </dl>
        <p class="muted small" style="margin: 0.75rem 0 0">
          {{
            isLocked()
              ? 'Held by the Solana program. Nobody outside this deal can move it.'
              : 'This deal is closed. Nothing is locked.'
          }}
        </p>
      </section>

      @if (outcomes().length > 0) {
        <section class="panel">
          <h2>Where the money goes</h2>
          <table class="outcomes">
            <thead>
              <tr>
                <th>If…</th>
                <th>Buyer gets</th>
                <th>Seller gets</th>
              </tr>
            </thead>
            <tbody>
              @for (o of outcomes(); track o.label) {
                <tr>
                  <td>{{ o.label }}</td>
                  <td>{{ o.buyer }}</td>
                  <td>{{ o.seller }}</td>
                </tr>
              }
            </tbody>
          </table>
          <p class="muted small" style="margin: 0.75rem 0 0">
            What the seller is paid has the {{ feePercent() }}% fee taken out. Refunds to the buyer
            have no fee.
          </p>
        </section>
      }

      <section class="panel">
        <h2>Progress</h2>
        <p>{{ d.milestonesReleased }} of {{ plural(d.unitLabel, d.milestones) }} released</p>
        <ul class="timeline">
          <li class="done">
            Created <span class="when">{{ d.createdAt | date: 'medium' }}</span>
          </li>
          <li [class.done]="d.acceptedAt">
            Accepted by seller
            @if (d.acceptedAt) {
              <span class="when">{{ d.acceptedAt | date: 'medium' }}</span>
            }
          </li>
          <li [class.done]="d.deliveredAt">
            Delivered
            @if (d.deliveredAt) {
              <span class="when">{{ d.deliveredAt | date: 'medium' }}</span>
            }
          </li>
          @if (isFinal()) {
            <li class="done">{{ finalLabel() }}</li>
          }
        </ul>

        @if (d.status === 'Active' && d.deliveryDeadline) {
          <p style="margin-top: 0.75rem">
            Delivery deadline in <strong><app-countdown [deadline]="d.deliveryDeadline" /></strong>
            @if (pastDeadline()) {
              <br />
              <span class="muted">
                {{
                  d.role === 'buyer' && !can('RefundUndelivered')
                    ? 'Refund available in a few seconds…'
                    : 'The deadline has passed.'
                }}
              </span>
            }
          </p>
        }
      </section>

      @if (d.status === 'Disputed') {
        <section class="panel">
          <h2>Disagreement</h2>
          @if (d.splitOffer; as offer) {
            <p>
              {{ offer.by === me() ? 'You offered' : 'The other side offered' }}
              to send <strong>\${{ offer.buyerAmount | amount }}</strong> of the remaining \${{
                remaining() | amount
              }}
              back to the buyer.
            </p>
          } @else {
            <p>No offer yet. Either side can propose how to split what is left.</p>
          }
          <p class="muted small" style="margin: 0">Funds stay locked until both sides agree.</p>
        </section>
      }

      @if (d.allowedActions.length > 0) {
        <section class="panel">
          <h2>Your options</h2>
          <div class="actions" style="gap: 1rem">
            @if (can('Accept')) {
              <button type="button" [disabled]="busy()" (click)="run({ action: 'Accept' })">
                Accept &amp; lock deposit
              </button>
            }
            @if (can('MarkDelivered')) {
              <button type="button" [disabled]="busy()" (click)="run({ action: 'MarkDelivered' })">
                Mark delivered
              </button>
            }
            @if (can('Confirm')) {
              <span class="stepper">
                <button
                  type="button"
                  class="secondary"
                  aria-label="Fewer"
                  [disabled]="busy() || parts() <= 1"
                  (click)="parts.set(parts() - 1)"
                >
                  −
                </button>
                <button
                  type="button"
                  class="secondary"
                  aria-label="More"
                  [disabled]="busy() || parts() >= maxParts()"
                  (click)="parts.set(parts() + 1)"
                >
                  +
                </button>
                <button
                  type="button"
                  [disabled]="busy()"
                  (click)="run({ action: 'Confirm', parts: parts() })"
                >
                  Confirm {{ plural(d.unitLabel, parts()) }}
                </button>
              </span>
            }
            @if (can('RefundUndelivered')) {
              <button
                type="button"
                [disabled]="busy()"
                (click)="run({ action: 'RefundUndelivered' })"
              >
                Take refund + seller's deposit
              </button>
            }
            @if (can('AcceptSplit')) {
              <button type="button" [disabled]="busy()" (click)="run({ action: 'AcceptSplit' })">
                Accept their offer: you get \${{ splitYouGet() | amount }}
              </button>
            }
            @if (can('SellerRefund')) {
              <button
                type="button"
                class="secondary"
                [disabled]="busy()"
                (click)="run({ action: 'SellerRefund' })"
              >
                Refund buyer
              </button>
            }
            @if (can('Cancel')) {
              <button
                type="button"
                class="danger"
                [disabled]="busy()"
                (click)="run({ action: 'Cancel' })"
              >
                Cancel &amp; refund
              </button>
            }
            @if (can('RaiseDispute')) {
              <button
                type="button"
                class="danger"
                [disabled]="busy()"
                (click)="run({ action: 'RaiseDispute' })"
              >
                Disagree
              </button>
            }
          </div>

          @if (can('ProposeSplit')) {
            <label style="margin-top: 1.25rem">
              Offer a split
              <span class="hint"
                >(amount going back to the buyer, up to \${{ remaining() | amount }})</span
              >
              <span class="inline-field">
                <input inputmode="decimal" placeholder="0.00" [(ngModel)]="splitInput" />
                <button
                  type="button"
                  [disabled]="busy() || splitAmount() === null"
                  (click)="proposeSplit()"
                >
                  Offer a split
                </button>
              </span>
            </label>
            @if (splitError(); as message) {
              <p class="error-text">{{ message }}</p>
            }
          }
        </section>
      }

      <section class="panel">
        <h2>On Solana</h2>
        <dl class="facts">
          <dt>Deal account</dt>
          <dd><app-explorer-link kind="address" [value]="d.dealPda" /></dd>
          @if (d.lastSignature) {
            <dt>Last transaction</dt>
            <dd><app-explorer-link kind="tx" [value]="d.lastSignature" /></dd>
          }
          <dt>Buyer</dt>
          <dd>
            <a [routerLink]="['/u', d.buyerWallet]">profile</a>
            <app-wallet-address [wallet]="d.buyerWallet" />
          </dd>
          <dt>Seller</dt>
          <dd>
            <a [routerLink]="['/u', d.sellerWallet]">profile</a>
            <app-wallet-address [wallet]="d.sellerWallet" />
          </dd>
          @if (d.buyerNote) {
            <dt>Buyer note</dt>
            <dd>{{ d.buyerNote }}</dd>
          }
        </dl>
      </section>
    } @else {
      <p class="muted">{{ error() ?? 'Loading…' }}</p>
    }
  `,
})
export class Deal {
  readonly id = input.required<string>();

  protected readonly actions = inject(DealActionService);
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly config = inject(ConfigService);
  private readonly clock = inject(ClockService);

  protected readonly deal = signal<FullDealDto | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly parts = signal(1);
  protected readonly splitInput = signal('');
  protected readonly plural = plural;

  protected readonly me = computed(() => this.auth.user()?.wallet);
  protected readonly busy = computed(() => this.actions.phase() !== 'idle');
  protected readonly isFinal = computed(() => FINAL.includes(this.deal()?.status ?? 'Draft'));
  private readonly isOpen = computed(() => !!this.deal() && !this.isFinal());
  protected readonly isLocked = computed(() => {
    const status = this.deal()?.status;
    return (
      status === 'AwaitingSeller' ||
      status === 'Active' ||
      status === 'Delivered' ||
      status === 'Disputed'
    );
  });

  private readonly perPart = computed(() => {
    const d = this.deal();
    return d ? BigInt(d.price) / BigInt(d.milestones) : 0n;
  });
  protected readonly released = computed(
    () => this.perPart() * BigInt(this.deal()?.milestonesReleased ?? 0),
  );
  protected readonly remaining = computed(() => BigInt(this.deal()?.price ?? 0) - this.released());
  protected readonly buyerDeposit = computed(() => BigInt(this.deal()?.buyerDeposit ?? 0));
  /** Set when the deal is created, but only in the vault once the seller accepts. */
  protected readonly sellerDeposit = computed(() => {
    const d = this.deal();
    const sellerHasPaid = !!d && d.status !== 'AwaitingSeller';
    return sellerHasPaid ? BigInt(d.sellerDeposit) : 0n;
  });
  protected readonly locked = computed(() =>
    this.isLocked() ? this.remaining() + this.buyerDeposit() + this.sellerDeposit() : 0n,
  );

  protected readonly maxParts = computed(() => {
    const d = this.deal();
    return d ? Math.max(1, d.milestones - d.milestonesReleased) : 1;
  });

  protected readonly pastDeadline = computed(() => {
    const deadline = this.deal()?.deliveryDeadline;
    return !!deadline && this.clock.now() >= new Date(deadline).getTime();
  });

  protected readonly splitAmount = computed(() => {
    const amount = parseAmount(this.splitInput(), this.config.decimals());
    return amount !== null && amount <= this.remaining() ? amount : null;
  });
  protected readonly splitError = computed(() => {
    const input = this.splitInput().trim();
    if (!input) return null;
    const amount = parseAmount(input, this.config.decimals());
    if (amount === null) return `Enter an amount with up to ${this.config.decimals()} decimals`;
    return amount > this.remaining() ? 'That is more than what is left in the deal' : null;
  });

  /** What the viewer receives (price part plus own deposit) if they accept the other side's offer. */
  protected readonly splitYouGet = computed(() => {
    const d = this.deal();
    if (!d?.splitOffer) return 0n;
    const toBuyer = BigInt(d.splitOffer.buyerAmount);
    if (d.role === 'buyer') return toBuyer + this.buyerDeposit();
    const toSeller = this.remaining() - toBuyer;
    return toSeller - (toSeller * this.config.feeBps()) / 10_000n + this.sellerDeposit();
  });

  protected readonly feePercent = computed(() => Number(this.config.feeBps()) / 100);

  /** How each way of ending the deal from its current state pays out (frontend.md, deal page §2). */
  protected readonly outcomes = computed<Outcome[]>(() => {
    const d = this.deal();
    const money = (amount: bigint): string => `$${formatAmount(amount, this.config.decimals())}`;
    const minusFee = (amount: bigint): bigint => amount - (amount * this.config.feeBps()) / 10_000n;
    const remaining = this.remaining();
    const buyerDeposit = this.buyerDeposit();
    const sellerDeposit = this.sellerDeposit();
    const rows: Outcome[] = [];

    if (d?.status === 'AwaitingSeller') {
      rows.push(
        { label: 'The buyer cancels', buyer: money(remaining + buyerDeposit), seller: NOTHING },
        { label: 'The seller declines', buyer: money(remaining + buyerDeposit), seller: NOTHING },
      );
    } else if (d && this.isLocked()) {
      const partsLeft = d.milestones - d.milestonesReleased;
      if (partsLeft > 1) {
        rows.push({
          label: `The buyer confirms 1 ${d.unitLabel}`,
          buyer: NOTHING,
          seller: money(minusFee(this.perPart())),
        });
      }
      rows.push({
        label: partsLeft > 1 ? `The buyer confirms all ${partsLeft} left` : 'The buyer confirms',
        buyer: `${money(buyerDeposit)} (deposit back)`,
        seller: money(minusFee(remaining) + sellerDeposit),
      });
      if (d.status === 'Active') {
        rows.push({
          label: 'The seller misses the deadline',
          buyer: money(remaining + buyerDeposit + sellerDeposit),
          seller: NOTHING,
        });
      }
      if (d.splitOffer) {
        const toBuyer = BigInt(d.splitOffer.buyerAmount);
        rows.push({
          label: 'The current split offer is accepted',
          buyer: money(toBuyer + buyerDeposit),
          seller: money(minusFee(remaining - toBuyer) + sellerDeposit),
        });
      } else if (d.status === 'Disputed') {
        rows.push({
          label: 'A split is agreed',
          buyer: 'their share + deposit',
          seller: 'the rest + deposit',
        });
      }
      rows.push({
        label: 'The seller refunds',
        buyer: money(remaining + buyerDeposit),
        seller: money(sellerDeposit),
      });
    }

    return rows;
  });

  protected readonly finalLabel = computed(() => {
    switch (this.deal()?.status) {
      case 'Completed':
        return 'Completed. Deposits returned.';
      case 'Split':
        return 'Closed with an agreed split';
      case 'Undelivered':
        return 'Closed: the seller missed the deadline';
      case 'SellerRefunded':
        return 'Refunded by the seller';
      case 'Cancelled':
        return 'Cancelled by the buyer';
      default:
        return 'Expired before it was funded';
    }
  });

  constructor() {
    effect(() => void this.load(this.id()));

    // Poll while the deal is open so the other side's actions show up.
    effect((onCleanup) => {
      if (!this.isOpen()) return;
      const timer = setInterval(() => {
        if (this.actions.phase() === 'idle') void this.refresh();
      }, 12_000);
      onCleanup(() => clearInterval(timer));
    });

    // RefundUndelivered only appears 15 s after the deadline: refetch right after.
    effect((onCleanup) => {
      const d = this.deal();
      if (d?.status !== 'Active' || !d.deliveryDeadline) return;
      const wait = new Date(d.deliveryDeadline).getTime() + 16_000 - Date.now();
      if (wait <= 0) return;
      const timer = setTimeout(() => void this.refresh(), wait);
      onCleanup(() => clearTimeout(timer));
    });
  }

  protected can(action: DealAction): boolean {
    return this.deal()?.allowedActions.includes(action) ?? false;
  }

  protected width(part: bigint): number {
    const total = this.locked();
    return total === 0n ? 0 : Number((part * 10_000n) / total) / 100;
  }

  protected async run(dto: DealActionDto): Promise<void> {
    const d = this.deal();
    if (!d) return;
    const updated = await this.actions.act(d.id, dto);
    if (updated) {
      this.deal.set(updated);
      this.parts.set(1);
      this.splitInput.set('');
    }
  }

  protected proposeSplit(): Promise<void> {
    return this.run({ action: 'ProposeSplit', buyerAmount: (this.splitAmount() ?? 0n).toString() });
  }

  private async load(id: string): Promise<void> {
    this.deal.set(null);
    this.error.set(null);
    await this.refresh(id);
  }

  private async refresh(id = this.id()): Promise<void> {
    try {
      this.deal.set(await this.api.get<FullDealDto>(`/api/deals/${id}`));
    } catch (error) {
      // A failed poll keeps what is on screen; only the first load shows an error.
      if (!this.deal()) this.error.set(error instanceof Error ? error.message : 'Deal not found');
    }
  }
}
