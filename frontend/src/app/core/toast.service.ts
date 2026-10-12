import { Injectable, signal } from '@angular/core';
import { ApiError } from './api.service';

export interface Toast { id: number; text: string; kind: 'error' | 'info' }

@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly toasts = signal<Toast[]>([]);
  private nextId = 1;

  show(text: string, kind: Toast['kind'] = 'info'): void {
    const id = this.nextId++;
    this.toasts.update(list => [...list, { id, text, kind }]);
    setTimeout(() => this.dismiss(id), 6000);
  }

  /** Shows the message of a failed call. 401, 429 and 503 are already handled by the interceptor. */
  fromError(error: unknown): void {
    if (error instanceof ApiError && error.handled) return;
    this.show(error instanceof Error ? error.message : 'Something went wrong', 'error');
  }

  dismiss(id: number): void {
    this.toasts.update(list => list.filter(t => t.id !== id));
  }
}
