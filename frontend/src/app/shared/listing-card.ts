import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MinimalListingDto } from '../core/models';
import { AmountPipe } from './amount.pipe';
import { unitCount } from './unit';

/** One listing as a ledger-style row: what it is on the left, the price on the right. */
@Component({
  selector: 'app-listing-card',
  imports: [RouterLink, AmountPipe],
  styleUrl: './listing-card.css',
  template: `
    <a class="listing-row" [routerLink]="['/l', listing().id]">
      <div class="info">
        <div class="meta">{{ listing().gameName }} · {{ listing().type }}</div>
        <div class="title">{{ listing().title }}</div>
        <div class="meta">
          {{ unitCount(listing().unit, listing().milestones) }} ·
          {{
            listing().seller.displayName ||
              listing().seller.wallet.slice(0, 4) + '…' + listing().seller.wallet.slice(-4)
          }}
        </div>
      </div>
      <div class="price num">\${{ listing().price | amount }}</div>
    </a>
  `,
})
export class ListingCard {
  readonly listing = input.required<MinimalListingDto>();
  protected readonly unitCount = unitCount;
}
