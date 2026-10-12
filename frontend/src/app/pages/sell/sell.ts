import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { ConfigService } from '../../core/config.service';
import { GameDto, ListingTermsDto, ListingType, StandardListingDto } from '../../core/models';
import { ToastService } from '../../core/toast.service';
import { formatAmount, parseAmount } from '../../shared/amount.pipe';
import { windowLabel } from '../../shared/delivery-window';

@Component({
  selector: 'app-sell',
  imports: [FormsModule],
  template: `
    <h1>{{ id() ? 'Edit listing' : 'Sell something' }}</h1>
    @if (id()) {
      <p class="muted">Changes apply to future deals only.</p>
    }

    <section class="panel">
      <label>
        Game
        <select [(ngModel)]="gameId">
          <option [ngValue]="0" disabled>Choose a game</option>
          @for (game of games(); track game.id) {
            <option [ngValue]="game.id">{{ game.name }}</option>
          }
        </select>
      </label>

      <label>
        Type
        <select [(ngModel)]="type">
          <option value="Service">Service (a carry, coaching…)</option>
          <option value="Item">Item or in-game currency</option>
        </select>
      </label>

      <label>
        Title
        <input maxlength="100" [(ngModel)]="title" />
      </label>

      <label>
        Description
        <textarea maxlength="2000" [(ngModel)]="description"></textarea>
      </label>

      <label>
        Price <span class="hint">(total, in dollars)</span>
        <input inputmode="decimal" placeholder="15.00" [(ngModel)]="price" />
      </label>

      <label>
        Released in
        <span class="inline-field">
          <input type="number" min="1" max="100" [(ngModel)]="milestones" style="width: 6rem" />
          <input maxlength="20" placeholder="match, hour, item…" [(ngModel)]="unitLabel" />
        </span>
        <span class="hint"
          >The price is paid out one part at a time as the buyer confirms each.</span
        >
      </label>

      <label>
        Delivery deadline
        <select [(ngModel)]="deliveryWindow">
          @for (w of config.config()?.deliveryWindows ?? []; track w.key) {
            <option [value]="w.key">{{ label(w.key) }}</option>
          }
        </select>
      </label>

      @if (problem(); as message) {
        <p class="error-text">{{ message }}</p>
      }
      <button type="button" [disabled]="saving() || problem() !== null" (click)="save()">
        {{ saving() ? 'Saving…' : id() ? 'Save changes' : 'Publish listing' }}
      </button>
    </section>
  `,
})
export class Sell {
  /** Set on /sell/:id (edit); empty on /sell (create). */
  readonly id = input<string>();

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
  protected readonly unitLabel = signal('');
  protected readonly deliveryWindow = signal(this.config.config()?.defaultDeliveryWindow ?? '24h');
  protected readonly label = windowLabel;

  /** Why the form cannot be submitted yet, or null. */
  protected readonly problem = computed(() => {
    const price = parseAmount(this.price(), this.config.decimals());
    const milestones = this.milestones();
    if (!this.gameId() || !this.title().trim() || !this.unitLabel().trim() || !this.price().trim())
      return 'Fill in game, title, price and unit.';
    if (price === null)
      return `Price must be a number with up to ${this.config.decimals()} decimals.`;
    if (price === 0n) return 'Price must be more than zero.';
    if (!Number.isInteger(milestones) || milestones < 1 || milestones > 100)
      return 'Parts must be between 1 and 100.';
    if (price % BigInt(milestones) !== 0n)
      return 'The price must divide evenly into the number of parts.';
    return null;
  });

  constructor() {
    this.api.get<GameDto[]>('/api/games').then(
      (g) => this.games.set(g),
      (e) => this.toast.fromError(e),
    );

    effect(() => {
      const id = this.id();
      if (id) void this.prefill(id);
    });
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
      this.unitLabel.set(listing.unitLabel);
      this.deliveryWindow.set(listing.deliveryWindow);
    } catch (error) {
      this.toast.fromError(error);
    }
  }

  protected async save(): Promise<void> {
    const price = parseAmount(this.price(), this.config.decimals());
    if (price === null || this.problem()) return;

    const terms: ListingTermsDto = {
      gameId: this.gameId(),
      type: this.type(),
      title: this.title().trim(),
      description: this.description().trim(),
      price: price.toString(),
      milestones: this.milestones(),
      unitLabel: this.unitLabel().trim(),
      deliveryWindow: this.deliveryWindow(),
    };

    this.saving.set(true);
    try {
      const id = this.id();
      const listing = id
        ? await this.api.patch<StandardListingDto>(`/api/listings/${id}`, terms)
        : await this.api.post<StandardListingDto>('/api/listings', terms);
      await this.router.navigate(['/l', listing.id]);
    } catch (error) {
      this.toast.fromError(error);
    } finally {
      this.saving.set(false);
    }
  }
}
