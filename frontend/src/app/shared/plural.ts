/** "match", 3 → "3 matches"; "hour", 1 → "1 hour". */
export function plural(label: string, n: number): string {
  if (n === 1) return `${n} ${label}`;
  return `${n} ${/(s|x|z|ch|sh)$/i.test(label) ? label + 'es' : label + 's'}`;
}
