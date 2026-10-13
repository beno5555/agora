import { Pipe, PipeTransform } from '@angular/core';

const RELATIVE = new Intl.RelativeTimeFormat(undefined, { numeric: 'auto' });
const DATE = new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric' });
const MINUTE = 60_000;
const HOUR = 60 * MINUTE;
const DAY = 24 * HOUR;

/** "just now", "12 minutes ago", "3 hours ago", "yesterday", then a short date after a week. */
@Pipe({ name: 'timeAgo' })
export class TimeAgoPipe implements PipeTransform {
  transform(iso: string): string {
    const time = new Date(iso);
    const ago = Date.now() - time.getTime();
    if (ago < MINUTE) return 'just now';
    if (ago < HOUR) return RELATIVE.format(-Math.floor(ago / MINUTE), 'minute');
    if (ago < DAY) return RELATIVE.format(-Math.floor(ago / HOUR), 'hour');
    if (ago < 7 * DAY) return RELATIVE.format(-Math.floor(ago / DAY), 'day');
    return DATE.format(time);
  }
}
