import { Directive } from '@angular/core';

/** Left and Right arrows move between the tabs of a `role="tablist"` and select the focused one. */
@Directive({
  selector: '[appTabKeys]',
  host: { '(keydown)': 'onKey($event)' },
})
export class TabKeys {
  protected onKey(event: KeyboardEvent): void {
    if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') return;
    const list = event.currentTarget as HTMLElement;
    const tabs = Array.from(list.querySelectorAll<HTMLElement>('[role="tab"]'));
    const current = tabs.indexOf(document.activeElement as HTMLElement);
    if (current < 0) return;
    const step = event.key === 'ArrowRight' ? 1 : -1;
    const next = tabs[(current + step + tabs.length) % tabs.length];
    event.preventDefault();
    next.focus();
    next.click();
  }
}
