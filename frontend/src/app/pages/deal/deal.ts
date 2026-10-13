import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiError, ApiService } from '../../core/api.service';
import { ConfigService } from '../../core/config.service';
import { DealActionService } from '../../core/deal-action.service';
import { DealAction, DealActionDto, FullDealDto, StandardUserDto } from '../../core/models';
import { ToastService } from '../../core/toast.service';
import { parseAmount } from '../../shared/amount.pipe';
import { ClockService } from '../../shared/clock.service';
import { Countdown } from '../../shared/countdown';
import { ExplorerLink } from '../../shared/explorer-link';
import { Icon } from '../../shared/icon';
import { mediaQuery } from '../../shared/media';
import { StatusBadge } from '../../shared/status-badge';
import { Vault } from '../../shared/vault';
import { WalletAddress } from '../../shared/wallet-address';
import { DealMoney, changeNotice } from './deal-money';
import { DealTimeline } from './deal-timeline';

const POLL_MS = 12_000;
/** RefundUndelivered only appears 15 s after the deadline, so the page refetches right after. */
const REFUND_DELAY_MS = 16_000;
const TEN_MINUTES = 600;

/** Button labels (frontend.md) and the toast after each action succeeds. */
const LABEL: Record<DealAction, string> = {
  Accept: 'Accept & lock deposit',
  Cancel: 'Cancel & refund',
  MarkDelivered: 'Mark delivered',
  Confirm: 'Confirm',
  RefundUndelivered: "Take refund + seller's deposit",
  RaiseDispute: 'Disagree',
  ProposeSplit: 'Offer this split',
  AcceptSplit: 'Accept their offer',
  SellerRefund: 'Refund buyer',
};
const DONE: Record<DealAction, string> = {
  Accept: 'Accepted. Your deposit is locked and the clock has started.',
  Cancel: 'Cancelled. Your money is back in your wallet.',
  MarkDelivered: 'Marked delivered. The buyer can confirm now.',
  Confirm: 'Confirmed. The money is on its way to the seller.',
  RefundUndelivered: 'Refund taken, with the seller’s deposit.',
  RaiseDispute: 'You disagreed. The money stays locked until you both agree.',
  ProposeSplit: 'Offer sent.',
  AcceptSplit: 'Split accepted. The deal is closed.',
  SellerRefund: 'Buyer refunded.',
};

@Component({
  selector: 'app-deal',
  imports: [
    FormsModule,
    RouterLink,
    Countdown,
    DealTimeline,
    ExplorerLink,
    Icon,
    StatusBadge,
    Vault,
    WalletAddress,
  ],
  styleUrl: './deal.css',
  template: `
    @if (money(); as m) {
      @let d = m.deal;
      <header class="head">
        <p class="meta">
          <a [routerLink]="['/l', d.listingId]">{{ d.gameName }}</a> · you are the {{ d.role }}
        </p>
        <div class="title-row">
          <h1>{{ d.title }}</h1>
          <app-status-badge [status]="d.status" />
        </div>
        <p class="status-line">{{ m.statusLine }}</p>
        @if (d.status === 'Active' && d.deliveryDeadline) {
          <div class="deadline">
            @if (!pastDeadline()) {
              <span
                >{{ m.isBuyer ? 'Seller must deliver by' : 'Deliver by' }}
                {{ m.deadlineText }}</span
              >
              <app-countdown class="clock" [deadline]="d.deliveryDeadline" />
              @if (secondsLeft() <= tenMinutes) {
                <span class="soon"><app-icon name="clock" />Less than 10 minutes left</span>
              }
            } @else if (m.isBuyer && !can('RefundUndelivered')) {
              <span class="soon"><app-icon name="clock" />Refund available in a few seconds…</span>
            }
          </div>
        }
      </header>

      <div class="layout" [class.with-bar]="showBar()">
        <div class="col-main">
          <app-vault
            class="area-vault"
            [buyerDeposit]="m.buyerDeposit"
            [price]="m.price"
            [released]="m.released"
            [sellerDeposit]="m.sellerDepositTerms"
            [sellerPending]="!m.sellerHasLocked"
            [state]="vaultState()"
            [heading]="m.isLocked ? 'Locked on Solana' : 'Deal on Solana'"
            [viewer]="d.role"
            [priceLabel]="d.milestones > 1 ? 'Price, ' + m.units(d.milestones) : 'Price'"
            [priceNote]="d.milestones > 1 ? m.money(m.perPart) + ' each' : ''"
            [releasedNote]="releasedNote()"
            [recordHref]="explorer('address', d.dealPda)"
          >
            @if (m.isLocked) {
              @if (d.status === 'Disputed') {
                <p class="vault-line strong">The money stays locked until you both agree.</p>
                @if (m.offerLine; as offer) {
                  @if (!can('AcceptSplit')) {
                    <p class="vault-line">{{ offer }}</p>
                  }
                } @else {
                  <p class="vault-line">No offer yet. Either side can propose a split.</p>
                }
              }
              <p class="no-key">Nobody else. Agora has no key.</p>
              <details class="keys" [open]="wide()">
                <summary>
                  Who can move this money<app-icon name="chevron-down" class="chev" />
                </summary>
                <ul>
                  @for (line of m.keyLines; track line.who) {
                    <li>
                      <app-icon [name]="line.icon" size="18" />
                      <span
                        ><strong>{{ line.who }}:</strong> {{ line.what }}</span
                      >
                    </li>
                  }
                </ul>
              </details>
            } @else {
              <p class="vault-line">{{ m.closedSentence }}</p>
            }
          </app-vault>

          @if (m.outcomes.length > 0) {
            <details class="panel area-outcomes outcomes" [open]="wide()">
              <summary>
                <h2>What happens if…</h2>
                <app-icon name="chevron-down" size="20" class="chev" />
              </summary>
              <dl>
                @for (o of m.outcomes; track o.kind) {
                  <div class="outcome" [class.next]="o.kind === nextStep()">
                    <dt>
                      {{ o.label }}
                      @if (o.kind === nextStep()) {
                        <span class="next-tag">Your next step</span>
                      }
                    </dt>
                    <dd>{{ o.result }}</dd>
                  </div>
                }
              </dl>
              <p class="fine">
                What the seller is paid has the {{ m.feePercent }}% fee taken out. Refunds to the
                buyer have no fee.
              </p>
            </details>
          }

          <section class="panel area-progress">
            <h2>Progress</h2>
            <app-deal-timeline [deal]="d" />
          </section>

          <section class="panel area-solana">
            <h2>On Solana</h2>
            <dl class="facts">
              <dt>Deal record</dt>
              <dd><app-explorer-link kind="address" [value]="d.dealPda" /></dd>
              @if (d.lastSignature) {
                <dt>Last transaction</dt>
                <dd><app-explorer-link kind="tx" [value]="d.lastSignature" /></dd>
              }
              <dt>Buyer{{ m.isBuyer ? ' (you)' : '' }}</dt>
              <dd class="who">
                <app-wallet-address [wallet]="d.buyerWallet" />
                <a [routerLink]="['/u', d.buyerWallet]">profile</a>
              </dd>
              <dt>Seller{{ m.isBuyer ? '' : ' (you)' }}</dt>
              <dd class="who">
                <app-wallet-address [wallet]="d.sellerWallet" />
                <a [routerLink]="['/u', d.sellerWallet]">profile</a>
              </dd>
            </dl>
          </section>
        </div>

        <div class="col-side">
          @if (!m.isFinal) {
            <section class="panel area-move move" aria-labelledby="move-title">
              @if (d.allowedActions.length > 0) {
                <h2 id="move-title">Your move</h2>

                @if (can('Confirm')) {
                  <div class="action">
                    @if (m.partsLeft > 1) {
                      <p class="prompt">Got some {{ m.unitPlural }}?</p>
                      <div class="stepper" role="group" aria-label="How many to confirm">
                        <button
                          type="button"
                          class="btn btn--secondary step"
                          aria-label="One fewer"
                          [disabled]="busy() || parts() <= 1"
                          (click)="parts.set(parts() - 1)"
                        >
                          <app-icon name="minus" size="20" />
                        </button>
                        <span class="count num" aria-live="polite">{{ m.units(parts()) }}</span>
                        <button
                          type="button"
                          class="btn btn--secondary step"
                          aria-label="One more"
                          [disabled]="busy() || parts() >= m.partsLeft"
                          (click)="parts.set(parts() + 1)"
                        >
                          <app-icon name="plus" size="20" />
                        </button>
                      </div>
                    }
                    <button
                      type="button"
                      [class]="btnClass('Confirm')"
                      [class.is-loading]="running() === 'Confirm'"
                      [disabled]="busy()"
                      (click)="run({ action: 'Confirm', parts: parts() })"
                    >
                      {{ label('Confirm', 'Confirm ' + m.units(parts())) }}
                    </button>
                    <p class="consequence">{{ m.consequence('Confirm', parts()) }}</p>
                  </div>
                }

                @for (action of simpleActions(); track action) {
                  <div class="action">
                    @if (action === 'AcceptSplit' && m.offerLine) {
                      <p class="prompt">{{ m.offerLine }}</p>
                    }
                    @if (confirming() === action) {
                      <div class="confirm-box" role="group" [attr.aria-label]="LABEL[action]">
                        <p>
                          <strong>{{ LABEL[action] }}?</strong> {{ m.consequence(action) }}
                        </p>
                        <button
                          type="button"
                          [class]="btnClass(action)"
                          [disabled]="busy()"
                          (click)="run({ action })"
                        >
                          Yes, {{ LABEL[action].toLowerCase() }}
                        </button>
                        <button type="button" class="btn btn--ghost" (click)="confirming.set(null)">
                          Keep the deal as it is
                        </button>
                      </div>
                    } @else {
                      <button
                        type="button"
                        [class]="btnClass(action)"
                        [class.is-loading]="running() === action"
                        [disabled]="busy()"
                        (click)="press(action)"
                      >
                        {{ label(action, actionText(action, m)) }}
                      </button>
                      <p class="consequence">{{ m.consequence(action) }}</p>
                    }
                  </div>
                }

                @if (can('ProposeSplit')) {
                  <div class="action">
                    <label for="split-amount" class="split-label">
                      {{ d.splitOffer ? 'Or offer a different split' : 'Offer a split' }}
                    </label>
                    <p class="hint" id="split-hint">
                      The amount that goes back to the buyer, up to {{ m.money(m.remaining) }}.
                    </p>
                    <div class="amount-input">
                      <span class="prefix" aria-hidden="true">$</span>
                      <input
                        id="split-amount"
                        inputmode="decimal"
                        placeholder="0.00"
                        autocomplete="off"
                        [attr.aria-invalid]="splitError() ? true : null"
                        aria-describedby="split-hint split-note"
                        [(ngModel)]="splitInput"
                      />
                      <span class="suffix" aria-hidden="true">USDC (test)</span>
                    </div>
                    <p class="consequence" id="split-note" [class.error-text]="splitError()">
                      @if (splitError(); as message) {
                        <app-icon name="alert-triangle" />{{ message }}
                      } @else if (splitAmount() !== null) {
                        {{ m.splitResult(splitAmount()!) }}
                      } @else {
                        Enter an amount to offer it.
                      }
                    </p>
                    <button
                      type="button"
                      class="btn btn--secondary"
                      [class.is-loading]="running() === 'ProposeSplit'"
                      [disabled]="busy() || splitAmount() === null"
                      (click)="proposeSplit()"
                    >
                      {{ label('ProposeSplit', 'Offer this split') }}
                    </button>
                  </div>
                }

                @if (can('RaiseDispute')) {
                  <div class="action">
                    <p class="prompt">Something wrong?</p>
                    @if (confirming() === 'RaiseDispute') {
                      <div class="confirm-box" role="group" aria-label="Disagree">
                        <p>
                          <strong>Disagree?</strong> {{ m.consequence('RaiseDispute') }} Nobody else
                          can decide for you.
                        </p>
                        <button
                          type="button"
                          class="btn btn--danger"
                          [disabled]="busy()"
                          (click)="run({ action: 'RaiseDispute' })"
                        >
                          <app-icon name="alert-triangle" />Yes, disagree
                        </button>
                        <button type="button" class="btn btn--ghost" (click)="confirming.set(null)">
                          Keep the deal as it is
                        </button>
                      </div>
                    } @else {
                      <button
                        type="button"
                        class="btn btn--danger"
                        [class.is-loading]="running() === 'RaiseDispute'"
                        [disabled]="busy()"
                        (click)="confirming.set('RaiseDispute')"
                      >
                        <app-icon name="alert-triangle" />{{ label('RaiseDispute', 'Disagree') }}
                      </button>
                      <p class="consequence">{{ m.consequence('RaiseDispute') }}</p>
                    }
                  </div>
                }
              } @else {
                <h2 id="move-title">Nothing to do right now</h2>
                <p class="consequence">
                  This page updates by itself when the other side does something.
                </p>
              }
            </section>
          }

          <section class="panel area-contact">
            <h2>Contact</h2>
            <dl class="facts">
              <dt>{{ m.isBuyer ? 'Seller' : 'Buyer' }}</dt>
              <dd>
                <a [routerLink]="['/u', otherWallet()]">{{
                  other()?.displayName || short(otherWallet())
                }}</a>
              </dd>
              <dt>Discord</dt>
              <dd>
                @if (other()?.discord; as discord) {
                  <button type="button" class="copy" (click)="copy(discord, 'Discord name')">
                    {{ discord }}<app-icon name="copy" />
                  </button>
                } @else {
                  <span class="muted">not on their profile</span>
                }
              </dd>
              @if (d.buyerNote) {
                <dt>{{ m.isBuyer ? 'Your note' : 'Buyer’s note' }}</dt>
                <dd class="note">“{{ d.buyerNote }}”</dd>
              }
            </dl>
          </section>
        </div>
      </div>

      @if (showBar()) {
        <div class="action-bar">
          <button
            type="button"
            [class]="btnClass(primary()!)"
            [class.is-loading]="running() === primary()"
            [disabled]="busy()"
            (click)="pressPrimary()"
          >
            {{
              primary() === 'Confirm'
                ? label('Confirm', 'Confirm ' + m.units(parts()))
                : label(primary()!, actionText(primary()!, m))
            }}
          </button>
        </div>
      }
    } @else if (notFound()) {
      <div class="missing">
        <h1>Deal not found</h1>
        <p>This deal doesn't exist, or the link is wrong.</p>
        <a routerLink="/me" class="btn btn--secondary">Go to my deals</a>
      </div>
    } @else {
      <p class="muted">{{ error() ?? 'Loading…' }}</p>
    }
  `,
})
export class Deal {
  readonly id = input.required<string>();

  protected readonly actions = inject(DealActionService);
  private readonly api = inject(ApiService);
  private readonly config = inject(ConfigService);
  private readonly clock = inject(ClockService);
  private readonly toast = inject(ToastService);

  protected readonly LABEL = LABEL;
  protected readonly tenMinutes = TEN_MINUTES;

  protected readonly deal = signal<FullDealDto | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly notFound = signal(false);
  protected readonly other = signal<StandardUserDto | null>(null);
  protected readonly parts = signal(1);
  protected readonly splitInput = signal('');
  /** The action whose button shows the wallet / Solana stage. */
  protected readonly running = signal<DealAction | null>(null);
  /** Disagree and Refund buyer ask once more before they run. */
  protected readonly confirming = signal<DealAction | null>(null);
  protected readonly wide = mediaQuery('(min-width: 960px)');

  protected readonly secondsLeft = computed(() => {
    const deadline = this.deal()?.deliveryDeadline;
    if (!deadline) return Infinity;
    return Math.floor((new Date(deadline).getTime() - this.clock.now()) / 1000);
  });
  protected readonly pastDeadline = computed(() => this.secondsLeft() <= 0);

  protected readonly money = computed(() => {
    const d = this.deal();
    return d
      ? new DealMoney(d, this.config.decimals(), this.config.feeBps(), this.pastDeadline())
      : null;
  });

  protected readonly busy = computed(() => this.actions.phase() !== 'idle');
  protected readonly primary = computed(() => {
    const m = this.money();
    return m ? m.primaryOf(m.deal.allowedActions) : null;
  });
  protected readonly nextStep = computed(
    () => this.money()?.nextStepKind(this.primary(), this.parts()) ?? null,
  );
  /** On a phone the primary action also sits in a bar at the bottom of the screen. */
  protected readonly showBar = computed(() => !!this.primary() && !this.wide());

  /** One-button actions in the order they appear, the primary first; Confirm, ProposeSplit and Disagree have their own blocks. */
  protected readonly simpleActions = computed<DealAction[]>(() => {
    const allowed = this.deal()?.allowedActions ?? [];
    const order: DealAction[] = [
      'Accept',
      'MarkDelivered',
      'AcceptSplit',
      'RefundUndelivered',
      'Cancel',
      'SellerRefund',
    ];
    const primary = this.primary();
    return order
      .filter((a) => allowed.includes(a))
      .sort((a, b) => Number(b === primary) - Number(a === primary));
  });

  protected readonly vaultState = computed(() => {
    const m = this.money();
    if (m?.deal.status === 'Disputed') return 'disputed';
    return m?.isLocked ? 'locked' : 'closed';
  });

  protected readonly releasedNote = computed(() => {
    const m = this.money();
    if (!m || m.deal.milestonesReleased === 0 || m.isFinal) return '';
    return `Released to the seller so far: ${m.money(m.released)} (${m.deal.milestonesReleased} of ${m.units(m.deal.milestones)}).`;
  });

  protected readonly otherWallet = computed(() => {
    const d = this.deal();
    if (!d) return '';
    return d.role === 'buyer' ? d.sellerWallet : d.buyerWallet;
  });

  protected readonly splitAmount = computed(() => {
    const m = this.money();
    const amount = parseAmount(this.splitInput(), this.config.decimals());
    return m && amount !== null && amount <= m.remaining ? amount : null;
  });
  protected readonly splitError = computed(() => {
    const m = this.money();
    const text = this.splitInput().trim();
    if (!m || !text) return null;
    const amount = parseAmount(text, this.config.decimals());
    if (amount === null)
      return `Use a plain amount with at most ${this.config.decimals()} decimal places.`;
    return amount > m.remaining
      ? `You can offer at most ${m.money(m.remaining)}, what's left in the deal.`
      : null;
  });

  constructor() {
    effect(() => void this.load(this.id()));

    // Poll while the deal is open so the other side's actions show up.
    effect((onCleanup) => {
      if (!this.deal() || this.money()?.isFinal) return;
      const timer = setInterval(() => {
        if (this.actions.phase() === 'idle') void this.refresh();
      }, POLL_MS);
      onCleanup(() => clearInterval(timer));
    });

    effect((onCleanup) => {
      const d = this.deal();
      if (d?.status !== 'Active' || !d.deliveryDeadline) return;
      const wait = new Date(d.deliveryDeadline).getTime() + REFUND_DELAY_MS - Date.now();
      if (wait <= 0) return;
      const timer = setTimeout(() => void this.refresh(), wait);
      onCleanup(() => clearTimeout(timer));
    });

    effect(() => {
      const wallet = this.otherWallet();
      if (wallet) void this.loadOther(wallet);
    });
  }

  protected can(action: DealAction): boolean {
    return this.deal()?.allowedActions.includes(action) ?? false;
  }

  protected btnClass(action: DealAction): string {
    if (action === 'RaiseDispute') return 'btn btn--danger';
    if (action !== this.primary()) return 'btn btn--secondary';
    return action === 'RefundUndelivered' ? 'btn btn--primary btn--lg' : 'btn btn--primary';
  }

  /** The stage while this action runs, else its normal label. */
  protected label(action: DealAction, text: string): string {
    if (this.running() !== action) return text;
    return this.actions.phase() === 'confirming' ? 'Confirming on Solana…' : 'Approve in Phantom…';
  }

  protected actionText(action: DealAction, m: DealMoney): string {
    return action === 'AcceptSplit'
      ? `Accept their offer: you get ${m.money(m.splitYouGet)}`
      : LABEL[action];
  }

  protected explorer(kind: 'address' | 'tx', value: string): string {
    return `https://explorer.solana.com/${kind}/${value}?cluster=devnet`;
  }

  protected short(wallet: string): string {
    return `${wallet.slice(0, 4)}…${wallet.slice(-4)}`;
  }

  /** Refund buyer asks once more; everything else runs straight away. */
  protected press(action: DealAction): void {
    if (action === 'SellerRefund') this.confirming.set(action);
    else void this.run({ action });
  }

  protected pressPrimary(): void {
    const primary = this.primary();
    if (primary === 'Confirm') void this.run({ action: 'Confirm', parts: this.parts() });
    else if (primary) this.press(primary);
  }

  protected proposeSplit(): Promise<void> {
    return this.run({ action: 'ProposeSplit', buyerAmount: (this.splitAmount() ?? 0n).toString() });
  }

  protected async copy(text: string, what: string): Promise<void> {
    await navigator.clipboard.writeText(text);
    this.toast.show(`${what} copied`, 'success');
  }

  protected async run(dto: DealActionDto): Promise<void> {
    const d = this.deal();
    if (!d) return;
    this.confirming.set(null);
    this.running.set(dto.action);
    const updated = await this.actions.act(d.id, dto);
    this.running.set(null);
    if (!updated) return;
    this.deal.set(updated);
    this.parts.set(1);
    this.splitInput.set('');
    this.toast.show(DONE[dto.action], 'success');
  }

  private async load(id: string): Promise<void> {
    this.deal.set(null);
    this.other.set(null);
    this.error.set(null);
    this.notFound.set(false);
    await this.refresh(id);
  }

  private async refresh(id = this.id()): Promise<void> {
    try {
      const next = await this.api.get<FullDealDto>(`/api/deals/${id}`);
      const prev = this.deal();
      this.deal.set(next);
      const m = this.money();
      const notice = prev && m && prev.id === next.id ? changeNotice(prev, m) : null;
      if (notice) this.toast.show(notice);
    } catch (error) {
      // A failed poll keeps what is on screen; only the first load shows an error.
      if (this.deal()) return;
      if (error instanceof ApiError && error.status === 404) this.notFound.set(true);
      else this.error.set(error instanceof Error ? error.message : 'Could not load this deal.');
    }
  }

  private async loadOther(wallet: string): Promise<void> {
    try {
      this.other.set(await this.api.get<StandardUserDto>(`/api/users/${wallet}`));
    } catch {
      // The contact panel falls back to the short wallet address.
    }
  }
}
