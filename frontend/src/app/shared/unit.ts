import { ListingUnit } from '../core/models';

/** Singular and plural of each unit, in the order the sell form lists them. */
const LABELS: Record<ListingUnit, [string, string]> = {
  Match: ['match', 'matches'],
  Game: ['game', 'games'],
  Round: ['round', 'rounds'],
  Win: ['win', 'wins'],
  Run: ['run', 'runs'],
  Level: ['level', 'levels'],
  Rank: ['rank', 'ranks'],
  Hour: ['hour', 'hours'],
  Day: ['day', 'days'],
  Session: ['session', 'sessions'],
  Item: ['item', 'items'],
};

/** A listing without a unit counts its parts as plain "parts". */
const NO_UNIT: [string, string] = ['part', 'parts'];

export const LISTING_UNITS = Object.keys(LABELS) as ListingUnit[];

/** Match, 1 → "match"; Match, 3 → "matches"; null, 2 → "parts". */
export function unitName(unit: ListingUnit | null, count = 1): string {
  const [one, many] = unit ? LABELS[unit] : NO_UNIT;
  return count === 1 ? one : many;
}

/** Match, 3 → "3 matches"; null, 1 → "1 part". */
export function unitCount(unit: ListingUnit | null, count: number): string {
  return `${count} ${unitName(unit, count)}`;
}
