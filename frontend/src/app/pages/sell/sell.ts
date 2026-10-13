import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { ConfigService } from '../../core/config.service';
import {
  GameDto,
  ListingTermsDto,
  ListingType,
  ListingUnit,
  StandardListingDto,
} from '../../core/models';
import { ToastService } from '../../core/toast.service';
import { formatAmount, parseAmount } from '../../shared/amount.pipe';
import { windowLabel } from '../../shared/delivery-window';
import { Icon } from '../../shared/icon';
import { LISTING_UNITS, unitName } from '../../shared/unit';

const MAX_PARTS = 100;

@Component({
  selector: 'app-sell',
  imports: [FormsModule, Icon],
  styleUrl: './sell.css',
  template: `
    <h1>{{ id() ? 'Edit listing' : 'Sell something' }}</h1>
    @if (id()) {
      <p class="muted">Changes apply to future deals only.</p>
    }

    <label>
      Game
      <select [(ngModel)]="gameId">
        <option [ngValue]="0" disabled>Choose a game</option>
        @for (game of games(); track game.id) {
          <option [ngValue]="game.id">{{ game.name }}</option>
        }
      </select>
    </label>

    <fieldset class="type">
      <legend>Type</legend>
      <label class="choice">
        <input type="radio" name="type" value="Service" [(ngModel)]="type" />
        Service <span class="hint">a carry, coaching…</span>
      </label>
      <label class="choice">
        <input type="radio" name="type" value="Item" [(ngModel)]="type" />
        Item <span class="hint">or in-game currency</span>
      </label>
    </fieldset>

    <label>
      Title
      <input maxlength="100" placeholder="Rank boost, 8 ranks" [(ngModel)]="title" />
    </label>

    <label>
      Description
      <textarea
        maxlength="2000"
        placeholder="What the buyer gets, and what you need from them (server, in-game name…)"
        [(ngModel)]="description"
      ></textarea>
    </label>

    <label for="price">Price</label>
    <div class="amount-input">
      <span class="prefix" aria-hidden="true">$</span>
      <input
        id="price"
        inputmode="decimal"
        placeholder="10.00"
        autocomplete="off"
        aria-describedby="price-note"
        [attr.aria-invalid]="priceError() ? true : null"
        [(ngModel)]="price"
      />
      <span class="suffix" aria-hidden="true">USDC (test)</span>
    </div>
    <p class="field-note" id="price-note" [class.error-text]="priceError()">
      @if (priceError(); as message) {
        <app-icon name="alert-triangle" />{{ message }}
      } @else {
        The whole price. The buyer locks it when they buy.
      }
    </p>

    <div class="parts">
      <label>
        Paid in parts
        <input
          type="number"
          min="1"
          [max]="maxParts"
          inputmode="numeric"
          aria-describedby="parts-note"
          [attr.aria-invalid]="partsError() ? true : null"
          [(ngModel)]="milestones"
        />
      </label>
      <label>
        Unit
        <select [(ngModel)]="unit">
          <option [ngValue]="null">None of these</option>
          @for (u of units; track u) {
            <option [ngValue]="u">{{ plural(u) }}</option>
          }
        </select>
      </label>
    </div>
    <p class="field-note" id="parts-note" [class.error-text]="partsError()">
      @if (partsError(); as message) {
        <app-icon name="alert-triangle" />{{ message }}
      } @else {
        {{ partsLine() }}
      }
    </p>

    <label>
      Deliver within
      <select [(ngModel)]="deliveryWindow">
        @for (w of windows(); track w.key) {
          <option [value]="w.key">{{ label(w.key) }}</option>
        }
      </select>
    </label>

    <section class="summary" aria-label="What gets locked">
      <p>
        Buyers lock the price plus a deposit set from their record on Solana. You lock a deposit
        set from your record when you accept.
      </p>
      <p>
        Both deposits go back when the deal is done. Agora's fee is {{ feePercent() }}% of what
        you're paid.
      </p>
    </section>

    <button
      type="button"
      class="btn btn--primary publish"
      [class.is-loading]="saving()"
      [disabled]="saving() || problem() !== null"
      (click)="save()"
    >
      {{ saving() ? 'Saving…' : id() ? 'Save changes' : 'Publish listing' }}
    </button>
    @if (missing(); as message) {
      <p class="field-note">{{ message }}</p>
    }
  `,
})
export class Sell {
  /** Set on /sell/:id (edit); empty on /sell (create). */
  readonly id = input<string>();
  /** `?game=<slug>` from a game page's empty state picks that game. */
  readonly game = input<string>();

  protected readonly config = inject(ConfigService);
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  protected readonly games = signal<GameDto[]>([]);
  protected readonly saving = signal(false);

  protected readonly gameId = signal(0);
  protected readonly type = signal<ListingType>('Service');
  protected readonly title = signal('');
  protected readonly description = signal('');
  protected readonly price = signal('');
  protected readonly milestones = signal(1);
  protected readonly unit = signal<ListingUnit | null>(null);
  protected readonly units = LISTING_UNITS;
  protected readonly maxParts = MAX_PARTS;
  protected readonly deliveryWindow = signal(this.config.config()?.defaultDeliveryWindow ?? '24h');
  protected readonly label = windowLabel;

  /** The demo window ("2 minutes (demo)") goes last. */
  protected readonly windows = computed(() =>
    [...(this.config.config()?.deliveryWindows ?? [])].sort(
      (a, b) => Number(a.key === '2m') - Number(b.key === '2m'),
    ),
  );
  protected readonly feePercent = computed(() => Number(this.config.feeBps()) / 100);

  private readonly amount = computed(() => parseAmount(this.price(), this.config.decimals()));

  protected readonly priceError = computed(() => {
    if (!this.price().trim()) return null;
    const amount = this.amount();
    if (amount === null) return `Use at most ${this.config.decimals()} decimal places.`;
    return amount === 0n ? 'The price must be more than $0.00.' : null;
  });

  protected readonly partsError = computed(() => {
    const parts = this.milestones();
    if (!Number.isInteger(parts) || parts < 1 || parts > MAX_PARTS)
      return `Use a whole number from 1 to ${MAX_PARTS}.`;
    const amount = this.amount();
    if (!amount || amount % BigInt(parts) === 0n) return null;
    const [lower, upper] = this.evenPrices(amount, parts);
    const tries = [lower, upper].filter((p) => p !== null).map((p) => this.money(p!));
    return `The price must split evenly into ${parts} parts. Try ${tries.join(' or ')}.`;
  });

  protected readonly partsLine = computed(() => {
    const parts = this.milestones();
    const amount = this.amount();
    const one = unitName(this.unit(), 1);
    if (parts <= 1) return 'Paid in one go when the buyer confirms.';
    const each = amount ? ` and ${this.money(amount / BigInt(parts))} is paid per ${one}` : '';
    return `The buyer confirms each ${one}${each}.`;
  });

  /** Why Publish is disabled when no field shows an error yet. */
  protected readonly missing = computed(() =>
    !this.gameId() || !this.title().trim() || !this.price().trim()
      ? 'Choose a game and fill in the title and price to publish.'
      : null,
  );

  protected readonly problem = computed(
    () => this.missing() ?? this.priceError() ?? this.partsError(),
  );

  constructor() {
    this.api.get<GameDto[]>('/api/games').then(
      (games) => {
        this.games.set(games);
        const slug = this.game();
        if (slug && !this.id() && !this.gameId())
          this.gameId.set(games.find((g) => g.slug === slug)?.id ?? 0);
      },
      (e) => this.toast.fromError(e),
    );

    effect(() => {
      const id = this.id();
      if (id) void this.prefill(id);
    });
  }

  protected plural(unit: ListingUnit): string {
    const name = unitName(unit, 2);
    return name[0].toUpperCase() + name.slice(1);
  }

  protected async save(): Promise<void> {
    const price = this.amount();
    if (price === null || this.problem()) return;

    const terms: ListingTermsDto = {
      gameId: this.gameId(),
      type: this.type(),
      title: this.title().trim(),
      description: this.description().trim(),
      price: price.toString(),
      milestones: this.milestones(),
      unit: this.unit(),
      deliveryWindow: this.deliveryWindow(),
    };

    this.saving.set(true);
    try {
      const id = this.id();
      const listing = id
        ? await this.api.patch<StandardListingDto>(`/api/listings/${id}`, terms)
        : await this.api.post<StandardListingDto>('/api/listings', terms);
      await this.router.navigate(['/l', listing.id]);
      this.toast.show(
        id
          ? 'Changes saved. They apply to deals started from now on.'
          : 'Listing is live. Copy the link and share it where your buyers are.',
        'success',
      );
    } catch (error) {
      this.toast.fromError(error);
    } finally {
      this.saving.set(false);
    }
  }

  private money(amount: bigint): string {
    return '$' + formatAmount(amount.toString(), this.config.decimals());
  }

  /** The nearest whole-cent prices below and above `amount` that split evenly into `parts`. */
  private evenPrices(amount: bigint, parts: number): [bigint | null, bigint | null] {
    const cent = 10n ** BigInt(Math.max(this.config.decimals() - 2, 0));
    const n = BigInt(parts);
    let lower: bigint | null = (amount / cent) * cent;
    while (lower > 0n && lower % n !== 0n) lower -= cent;
    if (lower <= 0n) lower = null;
    let upper = (amount / cent + 1n) * cent;
    while (upper % n !== 0n) upper += cent;
    return [lower, upper];
  }

  private async prefill(id: string): Promise<void> {
    try {
      const [listing, games] = await Promise.all([
        this.api.get<StandardListingDto>(`/api/listings/${id}`),
        this.api.get<GameDto[]>('/api/games'),
      ]);
      this.gameId.set(games.find((g) => g.slug === listing.gameSlug)?.id ?? 0);
      this.type.set(listing.type);
      this.title.set(listing.title);
      this.description.set(listing.description);
      this.price.set(formatAmount(listing.price, this.config.decimals()));
      this.milestones.set(listing.milestones);
      this.unit.set(listing.unit);
      this.deliveryWindow.set(listing.deliveryWindow);
    } catch (error) {
      this.toast.fromError(error);
    }
  }
}
