import { DestroyRef, Signal, inject, signal } from '@angular/core';

/** Whether a media query matches, kept up to date. Call it in an injection context. */
export function mediaQuery(query: string): Signal<boolean> {
  const list = window.matchMedia(query);
  const matches = signal(list.matches);
  const update = (event: MediaQueryListEvent): void => matches.set(event.matches);
  list.addEventListener('change', update);
  inject(DestroyRef).onDestroy(() => list.removeEventListener('change', update));
  return matches.asReadonly();
}
