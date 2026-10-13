import { Component, input, output } from '@angular/core';
import { ApiService } from '../core/api.service';
import { Icon } from './icon';

type Params = Record<string, string | number | undefined | null>;

export interface Page<T> {
  items: T[];
  hasNext: boolean;
}

/**
 * One page of a list endpoint, and whether another page follows. The API returns no total count,
 * so the single item right after this page is asked for too (pageSize 1 lands on that offset).
 */
export async function fetchPage<T>(
  api: ApiService,
  path: string,
  params: Params,
  page: number,
  size: number,
): Promise<Page<T>> {
  const [items, next] = await Promise.all([
    api.get<T[]>(path, { ...params, pageNumber: page, pageSize: size }),
    api.get<T[]>(path, { ...params, pageNumber: page * size + 1, pageSize: 1 }),
  ]);
  return { items, hasNext: next.length > 0 };
}

/** Previous / page number / Next under a list. Hidden when everything fits on one page. */
@Component({
  selector: 'app-pager',
  imports: [Icon],
  styles: `
    .pager {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--s-3);
      margin-top: var(--s-3);
    }
    .page {
      color: var(--text-muted);
      font-size: var(--fs-sm);
    }
  `,
  template: `
    @if (page() > 1 || hasNext()) {
      <nav class="pager" [attr.aria-label]="label()">
        <button
          type="button"
          class="btn btn--secondary btn--sm"
          [disabled]="page() === 1"
          (click)="go.emit(page() - 1)"
        >
          <app-icon name="arrow-left" />Previous
        </button>
        <span class="page" aria-live="polite">Page {{ page() }}</span>
        <button
          type="button"
          class="btn btn--secondary btn--sm"
          [disabled]="!hasNext()"
          (click)="go.emit(page() + 1)"
        >
          Next<app-icon name="arrow-right" />
        </button>
      </nav>
    }
  `,
})
export class Pager {
  readonly page = input.required<number>();
  readonly hasNext = input.required<boolean>();
  readonly label = input('Pages');
  readonly go = output<number>();
}
