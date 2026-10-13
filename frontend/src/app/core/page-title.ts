import { Injectable, inject } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';

const SITE = 'Agora';
const HOME = 'Agora: gaming deals where nobody holds your money';

/**
 * The browser tab title, "<page> · Agora". A route's `title` sets it on navigation; a page with a
 * name of its own (a listing, a deal, a game, a profile) replaces it once that name has loaded.
 */
@Injectable({ providedIn: 'root' })
export class PageTitle extends TitleStrategy {
  private readonly title = inject(Title);

  override updateTitle(snapshot: RouterStateSnapshot): void {
    this.set(this.buildTitle(snapshot) ?? null);
  }

  set(page: string | null): void {
    this.title.setTitle(page ? `${page} · ${SITE}` : HOME);
  }
}
