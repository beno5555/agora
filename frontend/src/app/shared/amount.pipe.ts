import { Pipe, PipeTransform, inject } from '@angular/core';
import { ConfigService } from '../core/config.service';

/** Base units → "15.00". Always at least 2 decimals, up to `decimals` when the value needs them. */
export function formatAmount(base: string | bigint, decimals: number): string {
  const value = BigInt(base);
  const negative = value < 0n;
  const digits = (negative ? -value : value).toString().padStart(decimals + 1, '0');
  const whole = digits.slice(0, digits.length - decimals);
  let frac = digits.slice(digits.length - decimals).replace(/0+$/, '');
  frac = frac.padEnd(2, '0');
  return `${negative ? '-' : ''}${whole}.${frac}`;
}

/** "15.5" → 15500000n at 6 decimals. Returns null for anything that is not a positive-or-zero amount with ≤ `decimals` places. */
export function parseAmount(input: string, decimals: number): bigint | null {
  const match = /^(\d+)(?:\.(\d*))?$/.exec(input.trim());
  if (!match) return null;
  const frac = match[2] ?? '';
  if (frac.length > decimals) return null;
  return BigInt(match[1] + frac.padEnd(decimals, '0'));
}

@Pipe({ name: 'amount' })
export class AmountPipe implements PipeTransform {
  private readonly decimals = inject(ConfigService).decimals;

  transform(value: string | bigint | null | undefined): string {
    return value === null || value === undefined ? '' : formatAmount(value, this.decimals());
  }
}
