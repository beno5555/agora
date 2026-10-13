import { Injectable, signal } from '@angular/core';
import { ApiError } from './api.service';
import { errorText } from './error-text';

export interface Toast {
  id: number;
  text: string;
  kind: 'success' | 'info' | 'error';
}

const AUTO_DISMISS_MS = 6000;

@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly toasts = signal<Toast[]>([]);
  private nextId = 1;

  /** Success and info go away by themselves; an error stays until the user closes it. */
  show(text: string, kind: Toast['kind'] = 'info'): void {
    const id = this.nextId++;
    this.toasts.update((list) => [...list, { id, text, kind }]);
    if (kind !== 'error') setTimeout(() => this.dismiss(id), AUTO_DISMISS_MS);
  }

  /** Shows the message of a failed call. 401, 429 and 503 are already handled by the interceptor. */
  fromError(error: unknown): void {
    if (error instanceof ApiError && error.handled) return;
    this.show(errorText(error), 'error');
  }

  dismiss(id: number): void {
    this.toasts.update((list) => list.filter((t) => t.id !== id));
  }
}
