import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MinimalListingDto } from '../core/models';
import { AmountPipe } from './amount.pipe';
import { unitCount } from './unit';

@Component({
  selector: 'app-listing-card',
  imports: [RouterLink, AmountPipe],
  template: `
    <a class="row-link" [routerLink]="['/l', listing().id]">
      <div class="grow">
        <strong>{{ listing().title }}</strong>
        <div class="muted small">
          {{ listing().gameName }} · {{ listing().type }} ·
          {{ unitCount(listing().unit, listing().milestones) }} ·
          {{
            listing().seller.displayName ||
              listing().seller.wallet.slice(0, 4) + '…' + listing().seller.wallet.slice(-4)
          }}
        </div>
      </div>
      <div class="price">\${{ listing().price | amount }}</div>
    </a>
  `,
})
export class ListingCard {
  readonly listing = input.required<MinimalListingDto>();
  protected readonly unitCount = unitCount;
}
