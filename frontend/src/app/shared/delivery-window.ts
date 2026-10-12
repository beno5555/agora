const UNITS: Record<string, [string, string]> = {
  s: ['second', 'seconds'],
  m: ['minute', 'minutes'],
  h: ['hour', 'hours'],
  d: ['day', 'days'],
};

/** "24h" → "24 hours"; the devnet "2m" window is flagged as a demo. */
export function windowLabel(key: string): string {
  const match = /^(\d+)([smhd])$/.exec(key);
  if (!match) return key;
  const n = Number(match[1]);
  const label = `${n} ${UNITS[match[2]][n === 1 ? 0 : 1]}`;
  return key === '2m' ? `${label} (demo)` : label;
}
