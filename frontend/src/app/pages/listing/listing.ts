import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiError, ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { ConfigService } from '../../core/config.service';
import { DealActionService } from '../../core/deal-action.service';
import { StandardListingDto, StandardUserDto } from '../../core/models';
import { ToastService } from '../../core/toast.service';
import { AmountPipe, formatAmount } from '../../shared/amount.pipe';
import { windowLabel } from '../../shared/delivery-window';
import { mediaQuery } from '../../shared/media';
import { unitCount, unitName } from '../../shared/unit';
import { Vault } from '../../shared/vault';

/** The note survives the trip into Phantom's in-app browser on the same device. */
const NOTE_KEY = 'agora:note:';

@Component({
  selector: 'app-listing',
  imports: [FormsModule, RouterLink, AmountPipe, Vault],
  styleUrl: './listing.css',
  template: `
    @if (listing(); as l) {
      <header class="head">
        <p class="meta">
          <a [routerLink]="['/g', l.gameSlug]">{{ l.gameName }}</a> · {{ l.type }}
        </p>
        <h1>{{ l.title }}</h1>
      </header>

      <div class="layout" [class.with-bar]="showBar()">
        <div class="col-main">
          <section class="area-what block">
            <h2>What you get</h2>
            <p class="description">{{ l.description }}</p>
          </section>

          @if (!isOwner()) {
            <section class="area-protect block">
              <h2>How your money is protected</h2>
              <ol class="steps">
                <li>
                  @if (l.depositPreview; as d) {
                    You lock the price plus a \${{ d.buyerDeposit | amount }} deposit.
                  } @else {
                    You lock the price plus a deposit set from your record on Solana.
                  }
                </li>
                <li>
                  @if (l.depositPreview; as d) {
                    The seller locks a \${{ d.sellerDeposit | amount }} deposit to accept.
                  } @else {
                    The seller locks a deposit of their own to accept.
                  }
                </li>
                <li>
                  @if (l.milestones > 1) {
                    You confirm each {{ unit() }}. Only then does that part (\${{
                      perPart() | amount
                    }}) reach the seller.
                  } @else {
                    You confirm when it arrives. Only then is the seller paid.
                  }
                </li>
              </ol>
              <p>
                If the seller doesn't deliver within {{ window() }}, you take your refund plus their
                deposit.
              </p>
              <p class="strong">Nobody else can move the money.</p>
            </section>
          }

          <section class="area-seller block">
            <h2>
              Seller:
              <a [routerLink]="['/u', l.seller.wallet]">{{
                l.seller.displayName || short(l.seller.wallet)
              }}</a>
            </h2>
            <p class="record-label">Record on Solana</p>
            @if (hasRecord()) {
              <p class="record">
                <span class="num">{{ l.sellerReputation.dealsCompleted }}</span> completed ·
                <span class="num">{{ l.sellerReputation.dealsUndelivered }}</span> not delivered ·
                <span class="num">{{ l.sellerReputation.dealsDisputed }}</span>
                {{ l.sellerReputation.dealsDisputed === 1 ? 'disagreement' : 'disagreements' }}
              </p>
            } @else {
              <p class="record">
                No finished deals on Solana yet. A new seller starts with a clean record, not a bad
                one.
              </p>
            }
            @if (seller()?.discord; as discord) {
              <p class="muted">Discord: {{ discord }}</p>
            }
          </section>
        </div>

        <div class="col-side">
          <section class="area-price panel">
            <p class="price num">\${{ l.price | amount }}</p>
            @if (l.milestones > 1) {
              <p class="muted">
                {{ unitCount(l.unit, l.milestones) }}, \${{ perPart() | amount }} each
              </p>
            }
            <p class="muted">Delivered within {{ window() }}</p>
          </section>

          @if (l.depositPreview; as d) {
            <app-vault
              class="area-vault"
              heading="You lock now"
              viewer="buyer"
              [buyerDeposit]="bigint(d.buyerDeposit)"
              [price]="bigint(l.price)"
              [sellerDeposit]="bigint(d.sellerDeposit)"
              [sellerPending]="true"
              [total]="lockNow()"
              [priceLabel]="
                l.milestones > 1 ? 'Price, ' + unitCount(l.unit, l.milestones) : 'Price'
              "
            >
              <p class="vault-line">
                You get the \${{ d.buyerDeposit | amount }} deposit back when the deal is done.
                Nobody else can move it.
              </p>
            </app-vault>
          }

          <section class="area-buy panel">
            @if (isOwner()) {
              <h2>This is your listing</h2>
              <p class="muted">Share its link where your buyers are: Discord, X or Reddit.</p>
              @if (!l.isActive) {
                <p class="notice">Paused. Buyers can't start a deal until you resume it.</p>
              }
              <div class="stack">
                <button type="button" class="btn btn--primary" (click)="copyLink()">
                  Copy deal link
                </button>
                <a [routerLink]="['/sell', l.id]" class="btn btn--secondary">Edit</a>
                <button
                  type="button"
                  class="btn btn--secondary"
                  [disabled]="toggling()"
                  (click)="toggle()"
                >
                  {{ l.isActive ? 'Pause' : 'Resume' }}
                </button>
              </div>
            } @else if (!l.isActive) {
              <h2>Paused</h2>
              <p>The seller paused this listing. You can't buy it right now.</p>
              <a routerLink="/" class="btn btn--secondary">Browse listings</a>
            } @else {
              @if (!auth.user()) {
                <p class="muted">
                  Your deposit is set from your record on Solana. Connect Phantom to see the exact
                  amount before you buy.
                </p>
              }
              <label for="note"> Note for the seller <span class="hint">(optional)</span> </label>
              <input
                id="note"
                maxlength="300"
                placeholder="Your in-game name…"
                [ngModel]="note()"
                (ngModelChange)="setNote($event)"
              />
              <button
                type="button"
                class="btn btn--primary buy-inline"
                [class.is-loading]="buying()"
                [disabled]="buying()"
                (click)="buy()"
              >
                {{ buyLabel() }}
              </button>
              <p class="fine">Approve in Phantom. The network fee is paid in test SOL.</p>
            }
          </section>
        </div>
      </div>

      @if (showBar()) {
        <div class="action-bar">
          <button
            type="button"
            class="btn btn--primary"
            [class.is-loading]="buying()"
            [disabled]="buying()"
            (click)="buy()"
          >
            {{ buyLabel() }}
          </button>
        </div>
      }
    } @else if (notFound()) {
      <div class="missing">
        <h1>Listing not found</h1>
        <p>This listing was removed or the link is wrong.</p>
        <a routerLink="/" class="btn btn--secondary">Browse listings</a>
      </div>
    } @else {
      <p class="muted">{{ error() ?? 'Loading…' }}</p>
    }
  `,
})
export class Listing {
  readonly id = input.required<string>();

  protected readonly auth = inject(AuthService);
  protected readonly actions = inject(DealActionService);
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly config = inject(ConfigService);

  protected readonly listing = signal<StandardListingDto | null>(null);
  protected readonly seller = signal<StandardUserDto | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly notFound = signal(false);
  protected readonly note = signal('');
  protected readonly toggling = signal(false);
  protected readonly unitCount = unitCount;
  private readonly wide = mediaQuery('(min-width: 960px)');

  protected readonly isOwner = computed(
    () => this.listing()?.seller.wallet === this.auth.user()?.wallet,
  );
  protected readonly buying = computed(() => this.actions.phase() !== 'idle');
  /** On a phone, Buy stays in reach at the bottom of the screen. */
  protected readonly showBar = computed(() => {
    const l = this.listing();
    return !!l && l.isActive && !this.isOwner() && !this.wide();
  });

  protected readonly perPart = computed(() => {
    const l = this.listing();
    return l ? BigInt(l.price) / BigInt(l.milestones) : 0n;
  });
  protected readonly unit = computed(() => unitName(this.listing()?.unit ?? null));
  /** "24 hours"; the demo window drops its "(demo)" tag inside a sentence. */
  protected readonly window = computed(() =>
    windowLabel(this.listing()?.deliveryWindow ?? '').replace(' (demo)', ''),
  );
  protected readonly lockNow = computed(() => {
    const l = this.listing();
    return l?.depositPreview ? BigInt(l.price) + BigInt(l.depositPreview.buyerDeposit) : null;
  });
  protected readonly hasRecord = computed(() => {
    const r = this.listing()?.sellerReputation;
    return !!r && r.dealsCompleted + r.dealsDisputed + r.dealsUndelivered > 0;
  });

  protected readonly buyLabel = computed(() => {
    const phase = this.actions.phase();
    if (phase === 'wallet') return 'Approve in Phantom…';
    if (phase === 'confirming') return 'Confirming on Solana…';
    if (!this.auth.user()) return 'Connect Phantom to buy';
    const lock = this.lockNow();
    return lock === null ? 'Buy' : `Buy · lock $${formatAmount(lock, this.config.decimals())}`;
  });

  constructor() {
    // Reload when the listing changes or the user signs in (depositPreview is null when signed out).
    effect(() => {
      this.auth.user();
      void this.load(this.id());
    });
    effect(() => this.note.set(this.readNote(this.id())));
  }

  protected bigint(value: string): bigint {
    return BigInt(value);
  }

  protected short(wallet: string): string {
    return `${wallet.slice(0, 4)}…${wallet.slice(-4)}`;
  }

  protected setNote(value: string): void {
    this.note.set(value);
    try {
      sessionStorage.setItem(NOTE_KEY + this.id(), value);
    } catch {
      // storage blocked: the note just isn't kept
    }
  }

  protected async buy(): Promise<void> {
    const wasSignedIn = !!this.auth.user();
    if (!(await this.auth.requestSignIn())) return;
    // Signing in reloads the listing with the buyer's deposit: let them see it before the wallet asks.
    if (!wasSignedIn) return;
    const deal = await this.actions.buy(this.id(), this.note().trim());
    if (!deal) return;
    this.setNote('');
    // The deal page plays the funds-lock animation on arrival.
    await this.router.navigate(['/d', deal.id], { state: { moment: 'lock' } });
  }

  protected async copyLink(): Promise<void> {
    await navigator.clipboard.writeText(window.location.href);
    this.toast.show('Deal link copied. Share it where your buyers are.', 'success');
  }

  protected async toggle(): Promise<void> {
    const l = this.listing();
    if (!l) return;
    this.toggling.set(true);
    try {
      await this.api.post(`/api/listings/${l.id}/${l.isActive ? 'pause' : 'resume'}`);
      await this.load(l.id);
    } catch (error) {
      this.toast.fromError(error);
    } finally {
      this.toggling.set(false);
    }
  }

  private readNote(id: string): string {
    try {
      return sessionStorage.getItem(NOTE_KEY + id) ?? '';
    } catch {
      return '';
    }
  }

  private async load(id: string): Promise<void> {
    try {
      const listing = await this.api.get<StandardListingDto>(`/api/listings/${id}`);
      this.listing.set(listing);
      this.notFound.set(false);
      if (this.seller()?.wallet !== listing.seller.wallet)
        void this.loadSeller(listing.seller.wallet);
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) this.notFound.set(true);
      else this.error.set(error instanceof Error ? error.message : 'Could not load this listing.');
    }
  }

  private async loadSeller(wallet: string): Promise<void> {
    try {
      this.seller.set(await this.api.get<StandardUserDto>(`/api/users/${wallet}`));
    } catch {
      // the seller panel works without the Discord name
    }
  }
}
