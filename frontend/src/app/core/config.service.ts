import { Injectable, computed, inject, signal } from '@angular/core';
import { ApiService } from './api.service';
import { ConfigDto } from './models';

@Injectable({ providedIn: 'root' })
export class ConfigService {
  private readonly api = inject(ApiService);

  readonly config = signal<ConfigDto | null>(null);
  readonly decimals = computed(() => this.config()?.mintDecimals ?? 6);
  readonly feeBps = computed(() => BigInt(this.config()?.feeBps ?? 0));

  /** Loaded once at startup. */
  async load(): Promise<void> {
    this.config.set(await this.api.get<ConfigDto>('/api/config'));
  }
}
