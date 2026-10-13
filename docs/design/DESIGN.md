# Agora redesign brief (DESIGN.md)

This file is the visual design: tokens, components, page layouts, motion, copy and assets. Claude implements it. Behaviour, data and routes are in `docs/architecture/frontend.md`; this file does not change them, except for the wording it proposes (see below). No CSS framework.

**How the CSS is organised (decided; it overrides anything below that says otherwise).** The old single `src/styles.css` is split up:

- `src/styles/`: `tokens.css`, `base.css`, `controls.css`, `surfaces.css`, `utilities.css`. Only what several components share lives here.
- `src/app/app.css`: the header, the test-network band, the sign-in dialog and the toast host.
- A component's own `.css` file sits next to it and is loaded with `styleUrl` (`pages/deal/deal.css`, `shared/status-badge.css`, ...). Anything used by one component goes there, so class names stay short and cannot collide.
- Every colour comes from a token. No inline `style=""` in templates.

**Wording.** Where this file proposes different wording from `frontend.md` (button labels, badge text, status lines), this file wins. The commit that changes the wording also updates `frontend.md` (and `api.md` if needed).

**Data.** Where the design needs something the API does not return yet, the frontend derives it if it can. Otherwise the backend, `api.md` and the frontend change together in one commit.

Status names, routes, action labels and the outcome table come from `frontend.md`. Where this brief proposes new wording, it says so.

---

## 0. What the current build gets wrong (from the screenshots)

These are the problems the redesign fixes:

- **The brand isn't there.** The app is green on light grey, and the logo is gold on near-black. Green is also the "Completed" status color, so brand and status blur together.
- **Every section looks the same.** The vault, progress, options and "On Solana" are identical white rounded cards. Nothing says "this is the important one".
- **The vault confuses people when a deal closes.** It shows Price $10.00 and then Total locked $0.00, with no visual link between the rows.
- **On a phone, the actions sit below the fold.** In the Phantom screenshot, "Your options" comes after the progress card.
- **The disabled "Offer a split" button looks clickable.** It is pale green, and it doesn't say why it is disabled.
- **Timeline timestamps wrap badly at 390px.** "Accepted by / seller" sits next to a date split over two lines.
- **The "Completed" badge has low contrast** (pale green on pale green).

---

## 1. Brand read

**What the assets show**

- **The mark.** A Greek temple column-portal with a heavy entablature on top, two arched openings and a stepped base. Inside it stands a simple figure: a sphere head on a cube body. It is drawn in faceted, bevelled gold with a light inner glow: an agora (public square) with a person in it. It reads as "a public place you can trust", not "a coin".
- **Colors.** The gold runs from pale highlight `#FFF2AE` through mid gold `#F7E084` and `#F8C23A` to burnt edge `#BD650B`. The background is near-black with a blue cast (`#07080E` to `#01030B`).
- **The background template.** Deep ink with large faint angled planes (shards). Thin amber light lines run along the plane edges, top-left and bottom-right, with a warm horizontal floor glow under the lockup. The mood is calm, premium and lit from inside. It is not neon or "cyber".
- **The wordmark.** A bold geometric sans: single-storey `a` and `g`, a round `o`, a short `r`, and an `A` with a slightly flattened apex. It is close to **Outfit** or **Urbanist** Bold, but not confirmed. Treat the wordmark as an image, not live text.
- **Note on the background file.** The background template has the logo lockup baked into its center. That decides how it is used (section 8).

**Fixed**
- The mark, its gold-on-ink pairing, and the wordmark's shape.
- Gold is the brand color. Ink (near-black, blue cast) is the brand ground.
- The bevel and glow live only in the image assets. The UI does not imitate them.

**Open to change**
- The current green. It is dropped as a brand color and kept only as the "Completed" status color.
- The UI typefaces (the wordmark stays an image).
- The light theme. It is supported, but dark is the default because the assets, Phantom and most gaming UIs are dark.

---

## 2. Direction

**Concept: "Two keys and a clock."** An agora was the square where anyone could trade in the open. In Agora, every deal is a small vault in that square. The buyer and the seller each hold a key, and the deal's deadline is the only other thing that can open it. Agora has no key. The design makes that physical: the deal page leads with the vault (how much is locked, split into the price and the two deposits) and directly under it lists who can move the money. The answer is always the two of you and the clock, never "support" or "admin". Everything else is quiet ink and paper-like type, so gold appears only where money is at stake or an action is waiting on you.

**The memorable element:** a gold "lock strip" (the vault bar) showing buyer deposit, price and seller deposit side by side, with the line "Nobody else. Agora has no key." right under it.

**Gold rule (the one rule that keeps this coherent).** Gold appears only on:
1. The brand (logo, header lockup).
2. Money that is currently locked (the vault bar and the locked total).
3. The single primary action on a screen.

Hover, selection, links in dark mode and decoration use other means.

**Rejected looks**
- **Neon-on-black crypto** (glowing cyan or purple, gradient borders, glow on every button). The background flirts with this, so its light lines are used once, in the hero and the share image, and never as a UI effect. Gamers who were scammed in Discord read neon crypto as "another rug".
- **Glassmorphism and blur panels.** They hurt legibility on cheap phones and inside Phantom's webview, and they say nothing about money.
- **Identical rounded cards for every section.** This is what the current build does. Sections become ruled panels with clear rank. Only listings get a card-like row.
- **Gamer HUD styling** (clipped corners, angled panels, RGB stripes). It would compete with the logo and age badly.
- **Solana purple and green gradients.** Agora depends on Solana but shows it through explorer links and the "Locked on Solana" label, not through borrowed branding.
- **Star ratings and "verified" shields.** Reputation is the seller's actual record on Solana, shown as plain counts.

---

## 3. Tokens (paste-ready)

Paste the block below into `src/styles/tokens.css`. Fonts load with one `<link>` in `index.html`:

```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Figtree:wght@400;500;600&family=IBM+Plex+Mono:wght@500&family=Outfit:wght@600;700&display=swap" rel="stylesheet">
```

| Font | Role | Weights |
|---|---|---|
| Outfit | Page titles, section titles, big numbers. Geometric like the wordmark. | 600, 700 |
| Figtree | Everything else: body, labels, buttons. More readable than Outfit at small sizes. | 400, 500, 600 |
| IBM Plex Mono | Amounts in the vault and ledgers, wallet addresses, transaction ids, countdown digits. Tabular, receipt-like. | 500 |

```css
/* ============ Agora tokens ============ */
:root {
  color-scheme: dark;

  /* Brand, theme-independent */
  --gold-100: #FFF2AE;
  --gold-300: #FFD866;
  --gold-500: #F8C23A;
  --gold-700: #B8860B;
  --gold-900: #8A5300;
  --ink-950:  #07090E;
  --ink-900:  #1A160E;
  --header-bg: var(--ink-950);   /* the header stays ink in both themes */
  --header-text: #F3EFE6;

  /* Dark theme (default) */
  --bg:            #07090E;
  --surface-1:     #0E121A;   /* panels, inputs */
  --surface-2:     #151B26;   /* raised rows, hover */
  --surface-3:     #1C2431;   /* disabled fill, pressed */
  --text:          #F3EFE6;   /* 16.3:1 on surface-1 */
  --text-muted:    #A7B0BE;   /* 8.6:1 */
  --text-faint:    #8790A0;   /* 5.8:1, still AA for body text */
  --border:        #232B38;   /* decorative dividers only */
  --border-strong: #5A667A;   /* input and control edges, 3.2:1 on surface-1 */

  --primary:       var(--gold-500);
  --primary-hover: var(--gold-300);
  --on-primary:    #1A1203;   /* 11.3:1 on gold-500 */
  --link:          var(--gold-300);   /* 13.6:1; links are always underlined */
  --link-underline: var(--gold-700);
  --danger:        #FF8077;   /* 7.7:1 */
  --danger-bg:     #33171A;
  --focus:         #FFE7A1;   /* 15:1 ring */

  /* Locked money (the vault bar) */
  --lock-price:    var(--gold-500);
  --lock-deposit:  var(--gold-700);   /* 5.8:1 against surface-1 */
  --lock-released: var(--text-faint);

  /* Status: fg = text and icon, bg = pill fill. Every pair passes AA (4.5:1+). */
  --st-neutral-fg: #E6E0D4; --st-neutral-bg: #0E121A; --st-neutral-bd: #5A667A;  /* Draft, AwaitingSeller */
  --st-blue-fg:    #8DB8FF; --st-blue-bg:    #13233D;  /* Active, Delivered  7.8:1 */
  --st-amber-fg:   #FF9A52; --st-amber-bg:   #2E1A0C;  /* Disputed           7.9:1 (orange, so it never reads as brand gold) */
  --st-green-fg:   #5FD68A; --st-green-bg:   #10291B;  /* Completed          8.5:1 */
  --st-red-fg:     #FF8077; --st-red-bg:     #33171A;  /* Undelivered        6.7:1 */
  --st-grey-fg:    #A3ABB8; --st-grey-bg:    #1C2129;  /* all other statuses 7.0:1 */

  /* Type */
  --font-display: "Outfit", ui-sans-serif, system-ui, sans-serif;
  --font-body: "Figtree", ui-sans-serif, system-ui, -apple-system, "Segoe UI", sans-serif;
  --font-mono: "IBM Plex Mono", ui-monospace, "SFMono-Regular", Menlo, Consolas, monospace;

  --fs-xs: 0.8125rem;   /* 13px meta, timestamps; the smallest size used */
  --fs-sm: 0.875rem;    /* 14px hints, badges */
  --fs-base: 1rem;      /* 16px body; inputs never go below this (stops iOS zoom) */
  --fs-lg: 1.125rem;    /* 18px lead text, panel titles on phone */
  --fs-xl: 1.375rem;    /* 22px panel titles */
  --fs-2xl: clamp(1.5rem, 1.2rem + 1.2vw, 2rem);         /* section heads */
  --fs-3xl: clamp(1.75rem, 1.25rem + 2.2vw, 2.75rem);    /* page titles */
  --fs-amount: clamp(2.25rem, 1.5rem + 3.2vw, 3.5rem);   /* vault total */

  --lh-tight: 1.15;
  --lh-base: 1.5;

  /* Spacing (4px base) */
  --s-1: 4px;  --s-2: 8px;  --s-3: 12px; --s-4: 16px;
  --s-5: 24px; --s-6: 32px; --s-7: 48px; --s-8: 64px;

  /* Radius: three values, no more */
  --r-sm: 6px;      /* buttons, inputs, rows */
  --r-md: 10px;     /* panels, vault */
  --r-pill: 999px;  /* badges, counts */

  /* Elevation: borders do the work; shadows only for overlays */
  --shadow-overlay: 0 12px 32px rgb(0 0 0 / 0.5), 0 0 0 1px var(--border);
  --vault-edge: inset 0 1px 0 rgb(255 216 102 / 0.35);  /* the one gold hairline */

  /* Layout */
  --w-page: 1120px;
  --w-read: 680px;
  --gutter: 16px;   /* 24px at >= 600px, 32px at >= 960px (set in media queries) */
  --tap: 48px;      /* minimum control height */
  --header-h: 56px;

  /* Motion */
  --dur-1: 80ms;  --dur-2: 150ms; --dur-3: 250ms; --dur-4: 600ms;
  --ease-out: cubic-bezier(0.2, 0.8, 0.2, 1);
  --ease-in: cubic-bezier(0.4, 0, 1, 1);
}

/* Light theme: follows the system. <html data-theme="dark"> forces dark (e.g. for the demo). */
@media (prefers-color-scheme: light) {
:root:not([data-theme="dark"]) {
  color-scheme: light;
  --bg:            #F6F2EA;   /* warm paper, not cold grey */
  --surface-1:     #FFFFFF;
  --surface-2:     #F0EBE1;
  --surface-3:     #E7E0D3;
  --text:          #1A160E;   /* 18:1 on white */
  --text-muted:    #5A5244;   /* 7.7:1 */
  --text-faint:    #6E6656;   /* 5.7:1 */
  --border:        #E2DACB;
  --border-strong: #8D8474;   /* 3.7:1 on white */

  --primary:       var(--ink-900);   /* gold-on-white fails 3:1, so the primary button is ink with gold text, like the logo */
  --primary-hover: #2C251A;
  --on-primary:    var(--gold-300);  /* 13:1 on ink */
  --link:          #8A5300;          /* 6.3:1 */
  --link-underline: #8A5300;
  --danger:        #B5322A;          /* 6.1:1 */
  --danger-bg:     #FBE1DE;
  --focus:         #6B3F00;          /* 9:1 */

  --lock-price:    var(--gold-500);
  --lock-deposit:  var(--gold-900);
  --lock-released: var(--text-faint);

  --st-neutral-fg: #1A160E; --st-neutral-bg: #FFFFFF; --st-neutral-bd: #8D8474;
  --st-blue-fg:    #1D5BC2; --st-blue-bg:    #E3ECFB;  /* 5.3:1 */
  --st-amber-fg:   #9A4500; --st-amber-bg:   #FCE6D2;  /* 5.4:1 */
  --st-green-fg:   #1C7443; --st-green-bg:   #DDF1E4;  /* 4.9:1 */
  --st-red-fg:     #B5322A; --st-red-bg:     #FBE1DE;  /* 4.9:1 */
  --st-grey-fg:    #5B6270; --st-grey-bg:    #ECEAE6;  /* 5.1:1 */

  --shadow-overlay: 0 12px 32px rgb(26 22 14 / 0.16), 0 0 0 1px var(--border);
  --vault-edge: inset 0 2px 0 var(--gold-500);
}
}

@media (min-width: 600px) { :root { --gutter: 24px; } }
@media (min-width: 960px) { :root { --gutter: 32px; --header-h: 64px; } }
```

**Implementation notes**
- **Theme:** dark is the default. The light theme turns on only from the system setting. Set `<html data-theme="dark">` for the demo so the judges see the brand ground regardless of their settings. To check the light theme, use DevTools → Rendering → "Emulate prefers-color-scheme: light".
- **Breakpoints:** `600px` (phone to tablet) and `960px` (two-column layouts). Design at 390px first.
- **Contrast** was checked with the WCAG formula for every text/background pair above. Ratios are in the comments. One limit: `--border-strong` drops to 2.97:1 on `surface-2` in dark mode, so put inputs on `surface-1` or `bg`, never on `surface-2`.
- **Base rules:**
  - `body { background: var(--bg); color: var(--text); font: 400 var(--fs-base)/var(--lh-base) var(--font-body); }`
  - `h1, h2, h3 { font-family: var(--font-display); font-weight: 700; line-height: var(--lh-tight); }`
  - `.num, .addr { font-family: var(--font-mono); font-variant-numeric: tabular-nums; }`
- **Focus ring (global):** `:focus-visible { outline: 3px solid var(--focus); outline-offset: 2px; }`. Never `outline: none` without this replacement.

---

## 4. Components

Class names are suggestions. Shared controls use short global names (`.btn`, `.btn--primary`) in `src/styles/controls.css`; anything specific to one component is named plainly in that component's own `.css`.

### Buttons `.btn`

Base: `min-height: var(--tap)` (48px), padding `0 var(--s-5)`, `--r-sm`, Figtree 600 `--fs-base`, inline-flex with an 8px gap for an icon. Use the full width on phones for primary page actions.

| Variant | Rest | Hover | Pressed | Use |
|---|---|---|---|---|
| `--primary` | bg `--primary`, text `--on-primary` | bg `--primary-hover` | `transform: scale(.98)` for `--dur-1` | One per screen: Buy, Accept & lock deposit, Confirm N ranks, Connect Phantom |
| `--secondary` | transparent, 1px `--border-strong`, text `--text` | bg `--surface-2` | bg `--surface-3` | Cancel & refund, Refund buyer, Mark delivered when it isn't the main step, Copy link |
| `--danger` | transparent, 1px `--danger`, text `--danger`, alert icon | bg `--danger-bg` | bg `--danger-bg` + scale | Disagree only |
| `--ghost` | no border, text `--text-muted`, underline on hover | text `--text` | | Sign out, "More options" |

- **Disabled** (`[disabled]`, `aria-disabled="true"`): bg `--surface-3`, no border, text `--text-faint`, `cursor: not-allowed`, no hover. Always pair it with a visible reason under the button, for example "Enter an amount up to $1.00". Never fade the button with opacity alone; that is the current bug.
- **Loading** (`.is-loading`, `aria-busy="true"`):
  - Same size as at rest; set a min-width so the button doesn't jump.
  - A 16px spinner ring replaces the icon.
  - The label changes to the stage: "Approve in Phantom…", then "Confirming on Solana…".
  - The button is disabled while loading.
- **Sizes:**
  - Default: 48px.
  - `--sm`: 40px, for desktop table rows (Pause/Resume). On phones `--sm` is still 48px.
  - Only the confirm stepper's +/- use 48x48 icon buttons with `aria-label`.

### Inputs `.field`

- **Structure:** label above (Figtree 600 `--fs-sm`, `--text`), hint below (`--fs-sm`, `--text-muted`), error below the hint.
- **Control:** 48px tall, bg `--surface-1`, 1px `--border-strong`, `--r-sm`, text `--fs-base` (16px, so iOS doesn't zoom).
- **States:**
  - Hover: border `--text-muted`.
  - Focus: the global focus ring plus border `--text`.
  - Error: border `--danger` plus an alert icon and message in `--danger`, linked with `aria-describedby` and `aria-invalid="true"`.
  - Disabled: bg `--surface-3`, text `--text-faint`.
- **Amount input** (`.field--amount`): a fixed `$` prefix inside the left edge and a "USDC (test)" suffix on the right, both `--text-muted`; mono digits; `inputmode="decimal"`.
- **Select** (the delivery window on /sell): the same box with a chevron icon; it uses the native `<select>`.

### Panels and listing rows

There is no generic card. Use two kinds of container:

- **`.panel`** (deal sections, forms):
  - bg `--surface-1`, 1px `--border`, `--r-md`, padding `--s-5` (`--s-4` on phones).
  - Title in Outfit 700 `--fs-xl`.
  - Panels hold information only.
- **`.vault`** (one per page, deal and listing): a panel plus `box-shadow: var(--vault-edge)`, 1px `--gold-900` border in dark mode. It is the only gold-edged box in the app.
- **`.listing-row`** (the shared `listing-card` component):
  - Full-width row, 1px `--border`, `--r-sm`.
  - Left: game and type as meta, title in Figtree 600 `--fs-lg`, then "8 ranks · Dealer1 · 12 deals done".
  - Right: price in mono `--fs-xl`.
  - Hover: bg `--surface-2` plus border `--border-strong`; no gold, no lift.
  - The whole row is one `<a>`.
- **Lists of rows** stack with a 8px gap. Never use grids of tiles.

### Status badge `.badge` (the shared `status-badge`)

- **Shape:** pill, 28px tall, padding `0 10px`, Figtree 600 `--fs-sm`.
- **Colors:** `color: var(--st-*-fg)`, `background: var(--st-*-bg)`, 1px border in the same fg at 40% (neutral uses `--st-neutral-bd`).
- **Always icon plus word**, so color is never the only signal:

| Status | Badge text (proposed copy) | Color | Icon |
|---|---|---|---|
| Draft | Not funded | neutral | hollow circle |
| AwaitingSeller | Waiting for seller | neutral | hourglass |
| Active | In progress | blue | half-filled circle |
| Delivered | Delivered | blue | package |
| Disputed | Disagreement | amber | alert triangle |
| Completed | Completed | green | check |
| Undelivered | Not delivered | red | x circle |
| any other (e.g. Cancelled) | the status name in sentence case | grey | minus |

Icons are inline SVGs at 16px, `aria-hidden="true"`; the word carries the meaning. Copy the needed Lucide icons (MIT) into one `icons.ts`. Needed: lock, key, clock, check, x-circle, alert-triangle, hourglass, package, circle, circle-half, minus, external-link, copy, chevron-down, menu, wallet.

**Count badge** (unread notifications): a pill with the number, bg `--text`, text `--bg`, plus visually hidden text "unread". It is not red, because red means "not delivered" here.

### Tabs `.tabs`

- **Used for:** buying and selling on /me, and All, Services, Items on /g/:slug.
- **Markup:** `role="tablist"` / `role="tab"` / `aria-selected`.
- **Style:**
  - Text tabs, 48px tall, Figtree 600.
  - Unselected: `--text-muted`.
  - Selected: `--text` plus a 3px underline in `--text`. The underline is not gold, per the gold rule.
- **Keyboard:** Left/Right arrow keys move between tabs.
- **Phone:** the row wraps if it overflows (it won't with 2–3 tabs). It isn't a scroll box: that showed a scrollbar for the underline and clipped the focus ring.

### Toasts `.toast` (the `toast.service`)

- **Position:** bottom-center on phones, above `env(safe-area-inset-bottom)` and the sticky action bar; bottom-right on desktop. Max width 420px.
- **Style:** bg `--surface-2`, `--shadow-overlay`, `--r-md`, icon plus text plus an optional link, for example "View on Solana Explorer".
- **Kinds:**
  - Success: check icon.
  - Info: info icon.
  - Error: alert icon with `--danger` text for the title.
- **Timing:** success and info stay 6s, and hovering or focusing pauses the timer. Errors stay until closed. The close button is 44x44.
- **Accessibility:** a `role="status"` region (polite) for success and info; `role="alert"` for errors.

### Timeline `.timeline` (deal progress)

- **Layout:** vertical. Each step has a 24px marker in a 32px column, the label (Figtree 500), and meta below the label (`--fs-xs`, `--text-muted`): the time, or "confirmed by the buyer". Meta always sits under the label, never beside it, which fixes the 390px wrapping.
- **Markers, shape and state together:**
  - Done: filled circle with a check, `--text`.
  - Current: ring in `--st-blue-fg` with a dot, plus "Now" in the meta.
  - Upcoming: dashed ring in `--border-strong`.
  - Final, closed: the status icon in its status color.
- **Connector:** 2px between markers; solid after done steps, dashed before upcoming ones.
- **Parts:** shown as text under the steps, for example "2 of 8 ranks confirmed", plus a small segmented bar of 8 cells (filled = confirmed). Above 12 parts it becomes a plain progress bar. The text is always present.

### Countdown `.countdown` (the shared `countdown`)

- **Layout:** a label line ("Seller must deliver by 18:30") and digits in mono `--fs-2xl`: `01:42:07`. Over 24 hours, show "1 day 4 hours".
- **Under 10 minutes:** add the line "Less than 10 minutes left" with a clock icon. Weight changes, not just color.
- **At zero:** the digits are replaced by "Refund available in a few seconds…" (text, no timer), per the frontend doc.
- **Accessibility:** `role="timer"`, `aria-live="off"`. A visually hidden live region announces only at 10 minutes and at zero.

### Vault breakdown `.vault` (the signature component)

```
┌──────────────────────────────────────────────────────────┐  ← gold hairline top
│ [lock] Locked on Solana                  Deal record ↗   │  label --fs-sm muted
│ $14.00                                                   │  mono --fs-amount, gold-300 (dark) / text (light)
│ ▕████████▏▕███████████████████████████▏▕████████▏         │  vault bar, 16px, aria-hidden
│  Buyer deposit   Price, 8 ranks            Seller deposit │
│  $2.00           $10.00 ($1.25 each)       $2.00          │  mono --fs-lg
│  Released to the seller so far: $0.00 (0 of 8 ranks)     │
│ ──────────────────────────────────────────────────────── │
│ Who can move this money                                  │  Outfit 600 --fs-lg
│ [key]   You (buyer): confirm ranks, or disagree          │
│ [key]   Seller: refund you, or agree to a split          │
│ [clock] The deadline, 18:30: if nothing is delivered,    │
│         you can take your refund plus their deposit      │
│ Nobody else. Agora has no key.                           │  Figtree 600, --text
└──────────────────────────────────────────────────────────┘
```

- **The bar:** three segments with a 2px gap, `--r-sm` on the outer ends, 16px tall. Widths are proportional to the amounts, with a 12% minimum so small deposits stay visible. Price uses `--lock-price`; deposits use `--lock-deposit`.
- **Released parts** become a hatched area (`repeating-linear-gradient` 45deg in `--lock-released`) at the start of the price segment, labelled under the bar.
- **The data lives in the text below the bar** (a `<dl>`), so the bar carries no meaning on its own (`aria-hidden="true"`).
- **Viewer-relative wording:** say "You" and "the seller" or "the buyer", from the session wallet. A signed-out viewer sees "Buyer" and "Seller".
- **Closed deals:**
  - The bar becomes an empty dashed outline.
  - The total shows `$0.00` with the label "Nothing locked".
  - A sentence replaces the key list, for example "Closed. The seller was paid $10.00 and both deposits went back."
  - This fixes the "$10 price, $0 locked" confusion.
- **Disputed:**
  - The top hairline and border switch to `--st-amber-fg`.
  - Add the line: "You disagree, so this money stays locked until you both accept the same split."
  - Show the current offer: "The seller offered: you get $0.60, they get $0.40 (minus the 2% fee)."
- **Key list:** it changes with status and comes from `allowedActions` for the viewer plus the known rules for the other side. Only list things that can still happen.

### Outcomes ("What happens if…") `.outcomes`

- **Content:** a two-column definition list per outcome: the outcome on the left; on the right, "You get $X · They get $Y". Computed with the formulas in `frontend.md`, from the viewer's side.
- **Filtering:** only list outcomes still reachable from the current status, typically 2–4 rows.
- **Placement:** collapsed in `<details>` on phones (summary: "What happens if…"); open on desktop.
- **Style:** amounts in mono; the row for the action the viewer is about to take is marked with a 3px `--text` left bar and the label "Your next step".

---

## 5. Pages

The ASCII wireframes are layout, not pixels. Width: desktop ≥ 960px, phone = 390px.

### 5.0 Header (all pages) and the test-network band

The header is ink in both themes, `--header-h` tall, sticky. Under it, a 32px band reads: **"Test network. All money here is test USDC."** It is `--fs-sm`, centered, with a `--surface-2` background. Judges and users both need this, and it stops the "is this real money?" question.

```
Desktop, signed in
┌────────────────────────────────────────────────────────────────────────────┐
│ [mark]Agora        Sell    My deals (1)    [wallet] CwjS…9HYJ  v           │
└────────────────────────────────────────────────────────────────────────────┘
│                Test network. All money here is test USDC.                  │

Desktop, signed out:     … Sell    [ Connect Phantom ]   (primary, gold)
Desktop, connecting:     … Sell    [ (spinner) Waiting for Phantom… ]
Connected, not signed in: … Sell   [ Sign in ]   hint popover: "Approve the sign-in
                                                 message in Phantom. It's free."

Phone 390, signed in                     Phone 390, signed out
┌──────────────────────────────────┐     ┌──────────────────────────────────┐
│ [mark]Agora     Deals(1)  [≡]    │     │ [mark]Agora        [ Connect ]   │
└──────────────────────────────────┘     └──────────────────────────────────┘
│ Test network. Test USDC only.    │     │ Test network. Test USDC only.    │
```

- The wallet chip opens a menu: Copy address, Public profile, Sign out. On a phone, the `≡` sheet holds Sell, My listings, Public profile, the wallet address with copy, and Sign out.
- "My deals (1)": the count is a pill plus hidden text "1 unread".
- The logo lockup links to `/`. Its height is 28px on phones and 32px on desktop (section 8).

**Phantom hand-off (phone, no injected provider).** Tapping Connect opens a bottom sheet (on desktop without Phantom, a centered dialog):

```
┌──────────────────────────────────┐
│ Open Agora in Phantom            │  Outfit 700 --fs-xl
│                                  │
│ Phone browsers can't connect to  │
│ a wallet. Phantom will open this │
│ same page in its own browser,    │
│ where you can connect.           │
│                                  │
│ [   Open in Phantom   ]          │  primary → phantom.app/ul/browse/<url>
│ [   Copy page link    ]          │  secondary
│                                  │
│ No Phantom yet? Get Phantom ↗    │  link
│ ──────────────────────────────── │
│ Agora uses Solana's test network.│  --fs-sm muted
│ In Phantom: Settings → Developer │
│ settings → Testnet mode.         │
└──────────────────────────────────┘
```

Desktop without the extension: same layout, title "Install Phantom to continue", primary "Get Phantom", and a note to reload this page after installing.

### 5.1 Home `/`

The first screen judges see. Lead with the promise and a picture of the mechanism, not a game grid.

```
Desktop
┌ hero: ink + CSS light lines (section 8), full width, ~440px ─────────────────┐
│  Gaming deals where nobody                 ┌ example vault (static) ───────┐ │
│  holds your money.                         │ Locked on Solana   $14.00     │ │
│                                            │ ▕███▏▕████████████▏▕███▏      │ │
│  You and the seller each lock a deposit    │ Buyer $2 · Price $10 · Seller │ │
│  on Solana. Money moves when you both      │ Nobody else. Agora has no key.│ │
│  agree, or when the seller's deadline      └───────────────────────────────┘ │
│  runs out. Agora never holds it.                                             │
│  [ Browse listings ]  [ Sell something ]                                     │
└──────────────────────────────────────────────────────────────────────────────┘
 Games                                              (text index, not tiles)
 Apex Legends 1 · Counter-Strike 2 0 · Fortnite 0 · League of Legends 1 · Valorant 0 · World of Warcraft 0
 Newest listings
 [ Search listings…                                   ] [ Search ]
 ┌ listing-row ───────────────────────────────────────────────────── $10.00 ┐
 ┌ listing-row ──────────────────────────────────────────────────────  $2.00 ┐

Phone 390
┌──────────────────────────────────┐
│ Gaming deals where               │  --fs-3xl
│ nobody holds your money.         │
│ You and the seller each lock a   │
│ deposit on Solana. Agora never   │
│ holds it.                        │
│ ┌ example vault (compact) ─────┐ │
│ │ $14.00 locked  ▕██▏▕█████▏▕██▏│ │
│ │ Nobody else has a key.        │ │
│ └───────────────────────────────┘ │
│ [      Browse listings      ]    │
├──────────────────────────────────┤
│ Games                            │
│ Apex Legends            1 →      │  48px rows
│ League of Legends       1 →      │
│ Counter-Strike 2        0 →      │
│ …                                │
│ Newest listings                  │
│ [ Search listings…   ] [Search]  │
│ ┌ Rank boost, 8 ranks     $10.00 ┐│
│ │ Apex Legends · Service         ││
│ │ Dealer1 · 12 deals done        ││
│ └────────────────────────────────┘│
└──────────────────────────────────┘
```

- **Hierarchy:** the headline, then the example vault (the product explained in one picture), then listings.
- **Game list:** games are a text index with counts. Use no game art; it isn't ours to use, and listing images are out of scope.
- **Sorting:** games with 0 listings sort last and render `--text-muted`.

### 5.2 Game storefront `/g/:slug`

```
Desktop                                          Phone 390
 Apex Legends                (--fs-3xl)          ┌──────────────────────────────┐
 1 listing                                       │ Apex Legends                 │
 [All] [Services] [Items]    (tabs)              │ 1 listing                    │
 ──────────────────────────────────────          │ All  Services  Items         │
 ┌ listing-row ─────────────────── $10.00 ┐      │ ━━━                          │
                                                 │ ┌ listing-row ──── $10.00 ┐  │
                                                 │ └─────────────────────────┘  │
                                                 └──────────────────────────────┘
```

- **Content width:** `--w-read` on desktop.
- **Tabs** filter in place and update `?type=`.
- **Empty state:** see section 7.

### 5.3 Listing `/l/:id` (the link sellers share)

Most visitors arrive from Discord, X or Reddit, on a phone, signed out, often in a normal browser rather than Phantom. In one screen the page must say three things: what you get, what you lock, and why you can't be scammed.

```
Desktop
 Apex Legends · Service                                   (meta, link to /g)
 Rank boost, 8 ranks                                      (--fs-3xl)
┌ left 7/12 ──────────────────────────────────┐ ┌ right 5/12, sticky ──────────┐
│ What you get                                 │ │ $10.00          (mono 2xl)   │
│ seller's description…                        │ │ 8 ranks, $1.25 each          │
│ Paid per rank: you confirm each one, and     │ │ Delivered within 24 hours    │
│ only that part is paid.                      │ │ ──────────────────────────── │
│ ──────────────────────────────────────────── │ │ You lock now      $12.00     │
│ How your money is protected                  │ │  Price $10.00 + deposit $2.00│
│ 1 You lock the price plus a $2 deposit.      │ │ You get the $2.00 back when  │
│ 2 The seller locks a $2 deposit to accept.   │ │ the deal is done.            │
│ 3 You confirm each rank. Only then does that │ │ Note for the seller (opt.)   │
│   part reach the seller.                     │ │ [ Your in-game name…      ]  │
│ If the seller doesn't deliver in 24 hours,   │ │ [   Buy · lock $12.00     ]  │
│ you take your refund plus their deposit.     │ │ Approve in Phantom. Network  │
│ Nobody else can move the money.              │ │ fee is paid in test SOL.     │
│ ──────────────────────────────────────────── │ └──────────────────────────────┘
│ Seller: Dealer1  (link /u/:wallet)           │
│ Record on Solana                             │
│ 12 completed · 0 not delivered · 1 split     │
│ Discord: dealer1                             │
└──────────────────────────────────────────────┘

Phone 390
┌──────────────────────────────────┐
│ Apex Legends · Service           │
│ Rank boost, 8 ranks              │  --fs-3xl
│ $10.00  · 8 ranks, $1.25 each    │  mono
│ Delivered within 24 hours        │
│ ┌ vault preview ───────────────┐ │
│ │ You lock now         $12.00  │ │  vault component, pre-deal state
│ │ ▕██▏▕███████████▏▕ ? ▏        │ │  seller segment dashed: "locked when they accept"
│ │ $2.00 back when it's done.   │ │
│ │ Nobody else can move it.     │ │
│ └──────────────────────────────┘ │
│ Seller: Dealer1                  │
│ 12 completed · 0 not delivered   │
│ What you get …                   │
│ How your money is protected 1-2-3│
│ Note for the seller (optional)   │
│ [ Your in-game name…          ]  │
├──────────────────────────────────┤  sticky bottom bar
│ [     Buy · lock $12.00      ]   │
└──────────────────────────────────┘
```

- **Hierarchy:** title, price, then the vault preview. The seller's on-chain record sits next to the price because it is the second trust question.
- **The sticky bottom bar** (phone) holds only Buy. It sits above `env(safe-area-inset-bottom)` with a top border in `--border`.
- **Seller viewing their own listing:** the buy panel becomes "This is your listing" with `[ Copy deal link ]` (primary), `[ Edit ]` and Pause. Sellers share this page, so copying the link is their main action.
- **Signed out:** Buy reads `Connect Phantom to buy` and runs the connect flow, or the phone hand-off. The note the user typed is kept in sessionStorage, so it survives the trip into Phantom's browser only on the same device and browser. Say nothing about it if it's lost.
- **Proposed copy:** "Buy · lock $12.00" (the doc says "Buy"). It states the amount before the wallet does.

### 5.4 Deal `/d/:id` (the heart of the demo)

The page answers, top to bottom: **What state is it in, and whose move is it? How much is locked, and who can move it? What can I do now? What happens next? Proof on Solana.**

```
Desktop (buyer view, Active)
 Apex Legends · Service · you are the buyer
 Rank boost, 8 ranks                                 [(half-circle) In progress]
 The seller is delivering. Confirm each rank when you get it.   (--fs-lg, --text)
 Seller must deliver by 18:30    01:42:07                       (countdown)
┌ left 7/12 ──────────────────────────────────────┐ ┌ right 5/12, sticky ─────────┐
│ VAULT (section 4): $14.00 locked, bar, amounts,  │ │ Your move                   │
│ who can move this money, "Agora has no key",     │ │ Got some ranks?             │
│ Deal record ↗                                    │ │ [-]  2 ranks  [+]           │
└──────────────────────────────────────────────────┘ │ [ Confirm 2 ranks ]         │
┌ What happens if… ────────────────────────────────┐ │ Sends $2.50 to the seller,  │
│ You confirm all 8     You: $2 back · Seller: $9.80│ │ minus the 2% fee.           │
│                       + $2 back                   │ │ ─────────────────────────── │
│ Seller misses 18:30   You: $12 + their $2 deposit │ │ Something wrong?            │
│ You disagree          Locked until you both agree │ │ [ Disagree ]                │
└──────────────────────────────────────────────────┘ │ Locks everything until you  │
┌ Progress ────────────────────────────────────────┐ │ both accept the same split. │
│ (o) Created         Oct 13, 05:17                 │ └─────────────────────────────┘
│ (o) Accepted        Oct 13, 05:20                 │ ┌ Contact ────────────────────┐
│ ( ) Delivered       Now                           │ │ Seller: Dealer1 ↗           │
│ ( ) Completed                                     │ │ Discord: dealer1 [copy]     │
│ 0 of 8 ranks confirmed  ▢▢▢▢▢▢▢▢                  │ │ Your note: "hi I want…"     │
└──────────────────────────────────────────────────┘ └─────────────────────────────┘
┌ On Solana ───────────────────────────────────────┐
│ Deal record     C9NcJE…LV3cbn ↗   [copy]          │  mono, external-link icon
│ Last transaction 5RaGWB…QHiTh1 ↗                  │
│ Buyer 9DJL…7VRH (profile) · Seller 9PPs…wUe8      │
└──────────────────────────────────────────────────┘

Phone 390 (buyer view, Active)
┌──────────────────────────────────┐
│ Apex Legends · you are the buyer │  --fs-xs
│ Rank boost, 8 ranks              │  --fs-2xl
│ [(◐) In progress]                │
│ The seller is delivering.        │
│ Confirm each rank when you get it│
│ Deliver by 18:30     01:42:07    │
│ ┌ VAULT ───────────────────────┐ │  above the fold: total, bar,
│ │ [lock] Locked on Solana      │ │  and the "no key" line
│ │ $14.00                       │ │
│ │ ▕██▏▕██████████▏▕██▏          │ │
│ │ You $2 · Price $10 · Seller $2│ │
│ │ Nobody else. Agora has no key.│ │
│ │ Who can move it ▾            │ │  key list collapsible on phone
│ └──────────────────────────────┘ │
│ Your move                        │
│ [-]   2 ranks   [+]              │
│ Sends $2.50 to the seller.       │
│ Something wrong? [ Disagree ]    │
│ What happens if… ▸               │  <details>
│ Progress (timeline)              │
│ Contact                          │
│ On Solana                        │
├──────────────────────────────────┤ sticky bottom bar
│ [     Confirm 2 ranks       ]    │ primary action only
└──────────────────────────────────┘
```

**Rules for the deal page**
- **The status line under the title is the most important text.** It always says whose move it is (copy table in section 7). Below the title, nothing else competes with it except the countdown.
- **One primary button.** The first action in `allowedActions` order of importance gets primary: Accept, Confirm, MarkDelivered, AcceptSplit, RefundUndelivered. Cancel, SellerRefund and ProposeSplit are secondary. RaiseDispute is always danger. On phones the primary also sits in the sticky bottom bar; danger actions never go in the sticky bar.
- **Every action shows its consequence** in one line under the button, with real amounts, for example "Sends $2.50 to the seller, minus the 2% fee." This line replaces most confirm dialogs. Keep a confirm dialog only for Disagree and Refund buyer.
- **Disputed state:**
  - The vault turns amber-edged.
  - "Your move" becomes the split panel: current offer first ("The seller offered you $0.60 of the $1.00 left"), then `[ Accept their offer: you get $0.60 ]` (primary), then "Or offer a different split" with the amount field, max shown in the hint.
  - The line "The money stays locked until you both agree. Nobody else can decide." sits in the vault, not in small print.
- **Countdown at zero (buyer):** the status line becomes "The seller missed the deadline." The primary button appears after the refetch as "Take refund + seller's deposit", with the consequence line "You get $12.00: your $10.00, your $2.00 deposit and the seller's $2.00 deposit." This is the demo moment: make it the largest button state on the page.
- **Updates from polling:** when the deal changes because of the other side, toast it ("The seller accepted. Their $2.00 deposit is locked.") and briefly highlight the changed section (motion).
- **"On Solana" is always present** and always last. Show addresses with the `wallet-address` component (mono, short form, copy button with 44px hit area). Links say what they open: "View deal record on Solana Explorer ↗".
- **Seller view, AwaitingSeller:** the status line reads "A buyer locked $12.00. Accept to lock your $2.00 deposit and start the 24-hour clock." The primary is `Accept & lock deposit`, and `Refund buyer` is secondary.

### 5.5 Sell `/sell`, `/sell/:id`

```
Desktop (single column, --w-read)          Phone 390 (same order)
 Sell something                            ┌──────────────────────────────┐
 Game          [ Apex Legends     v ]      │ Sell something               │
 Type          ( ) Service ( ) Item        │ Game [ Apex Legends   v ]    │
 Title         [ Rank boost, 8 ranks ]     │ Type (o) Service ( ) Item    │
 Description   [ textarea           ]      │ Title [                  ]   │
 Price         [$ 10.00   USDC (test)]     │ Description [            ]   │
 Paid in parts [ 8 ] unit [ ranks v ]      │ Price [$ 10.00  USDC (test)] │
   "The buyer confirms each rank and       │ Parts [8] [ranks v]          │
    $1.25 is paid per rank."               │ Deliver within [24 hours v]  │
 Deliver within [ 24 hours v ]             │ ┌ summary ──────────────┐    │
 ┌ summary panel ───────────────────┐      │ │ Buyer locks $12.00    │    │
 │ Buyers lock $12.00 to buy.       │      │ │ You lock $2.00 to      │    │
 │ You lock a $2.00 deposit when    │      │ │ accept.               │    │
 │ you accept. Agora fee: 2% of     │      │ └───────────────────────┘    │
 │ what you're paid.                │      ├──────────────────────────────┤
 └──────────────────────────────────┘      │ [     Publish listing     ]  │ sticky
 [ Publish listing ]                       └──────────────────────────────┘
```

- **Inline validation** (section 7 copy): more than 6 decimals; price not divisible by parts.
- **Delivery windows:** the `2m` option reads "2 minutes (demo)" and comes last.
- **After publish:** go to `/l/:id` with a toast "Listing is live. Copy the link and share it where your buyers are."

### 5.6 Dashboard `/me`

```
Desktop                                              Phone 390
 My deals                                            ┌──────────────────────────────┐
 [Buying] [Selling]                                  │ My deals                     │
 ┌ deal row: title · badge · "Your move" flag · $ ┐  │ Buying  Selling              │
 │ Rank boost      [◐ In progress] Your move  $10 │  │ ┌ Rank boost        $10.00 ┐ │
 └────────────────────────────────────────────────┘  │ │ [◐ In progress]          │ │
 ┌ two columns ≥960: ───────────────────────────────┐│ │ Your move: confirm ranks │ │
 │ Notifications (unread first)  │ My listings      ││ └──────────────────────────┘ │
 │ • A buyer funded a deal  05:45 │ row  $10 [Pause]││ Notifications                │
 │   Your deal is closed    04:49 │                 ││ My listings                  │
 └──────────────────────────────────────────────────┘│ Profile                      │
 Profile (display name, Discord) [Save profile]      └──────────────────────────────┘
```

- **Deal rows** show "Your move" (Figtree 600 plus an arrow icon) when `allowedActions` for the viewer is non-empty. This is the dashboard's one emphasis. Sort rows with "Your move" first.
- **Notifications:** unread rows get a 3px `--text` left bar plus hidden text "unread"; read rows are `--text-muted`.
- **Profile form** at the bottom, as today.

### 5.7 Public profile `/u/:wallet`

```
Desktop                                        Phone 390
 Dealer1                       (--fs-3xl)      ┌──────────────────────────────┐
 9PPs…wUe8 [copy]  View on Solana ↗            │ Dealer1                      │
 ┌ Record on Solana ───────────────────┐       │ 9PPs…wUe8 [copy]             │
 │ 12          0              1        │       │ Record on Solana             │
 │ completed   not delivered  splits   │       │ 12 completed                 │
 │ Read from the program, not reviews. │       │ 0 not delivered · 1 split    │
 └─────────────────────────────────────┘       │ Active listings              │
 Active listings                               │ ┌ listing-row ┐              │
 ┌ listing-row ┐                               └──────────────────────────────┘
```

Use the counts the `reputation` component already exposes; the labels above are examples. Numbers are in mono `--fs-2xl`, with plain labels below.

---

## 6. Motion

Animate only in two cases: as a reply to something the user did, or to mark a money state change. There are no idle animations, glows or hover lifts.

| Moment | What moves | Duration and easing |
|---|---|---|
| Button press | `scale(.98)` | `--dur-1`, ease-out |
| Waiting on wallet or Solana | 16px spinner ring rotates | 800ms linear, infinite |
| Toast in / out | `translateY(8px)` to 0 plus fade; out is a fade | in `--dur-3` ease-out; out `--dur-2` ease-in |
| **Funds lock** (Buy synced, or Accept synced) | Vault bar segments grow from 0 to full width left to right, staggered 80ms; the lock icon's shackle closes; one gold light sweep crosses the vault's top hairline (a gradient `background-position` move, echoing the background's light lines) | segments `--dur-4` ease-out; shackle `--dur-3`; sweep 900ms ease-out, once |
| Part released (Confirm) | The hatched released area widens; the amount crossfades | `--dur-3` ease-out; crossfade `--dur-2` |
| **Deal completed** | Segments shrink to 0, then the bar becomes the dashed outline; the final timeline marker fills; the badge crossfades to "Completed" | shrink `--dur-4` ease-in-out; others `--dur-2` |
| Disagreement raised | The vault edge color crossfades to amber; nothing else | `--dur-2` |
| Change from the other side (polling) | The changed section's background goes from `--surface-2` to `--surface-1` | 1200ms ease-out |
| Sheet or dialog open | Slides up 16px plus fade; backdrop fades | `--dur-3` ease-out |

No confetti. A completed deal is calm: money went where it should.

```css
@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
    scroll-behavior: auto !important;
  }
  .spinner { display: none; }          /* the loading label text carries the state */
}
```

With reduced motion, the states still change instantly and the toasts still announce them. Nothing depends on seeing the animation.

---

## 7. Copy and tone

**Voice**
1. **Say who gets what, in dollars.** "Sends $2.50 to the seller", not "Release milestone".
2. **Second person, short sentences, sentence case everywhere**, including buttons and titles. Use "Accept & lock deposit", not "Accept & Lock Deposit".
3. **Calm, not cheerful.** No exclamation marks, no "Oops!", no "Awesome".
4. **Name the next step and whose it is.** Every status line ends with what happens next.
5. **The amount format:** the first mention on a page reads "$12.00 USDC (test)", then "$12.00". Always use two decimals.
6. **Say "Solana" where it builds trust** ("Locked on Solana", "Record on Solana"). Keep everything else plain.

**Jargon replacements**

| Don't say | Say |
|---|---|
| escrow, escrow PDA, vault account | locked money; deal record (on Solana) |
| smart contract, program | Solana (or leave it out) |
| sign transaction | approve in Phantom |
| signature, tx hash | transaction |
| milestone | the listing's unit (rank, match, item), or "part" |
| dispute | disagreement / disagree |
| release funds | send to the seller |
| collateral, stake | deposit |
| mint, lamports, base units | (never shown) |
| devnet | test network |
| connect wallet | Connect Phantom |
| on-chain reputation | record on Solana |

**Status lines (under the deal title)**

| Status | Buyer sees | Seller sees |
|---|---|---|
| Draft | Approve the payment in Phantom to fund this deal. | The buyer hasn't funded this deal yet. |
| AwaitingSeller | Your $12.00 is locked. Waiting for the seller to accept. | A buyer locked $12.00. Accept to lock your $2.00 deposit and start the clock. |
| Active | The seller is delivering. Confirm each rank when you get it. | Deliver by 18:30, then mark it delivered. |
| Delivered | The seller says it's delivered. Check, then confirm. | Waiting for the buyer to confirm. |
| Active, deadline passed | The seller missed the deadline. You can take your refund plus their deposit. | You missed the deadline. The buyer can take a refund and your deposit. |
| Disputed | You disagree. The money stays locked until you both accept the same split. | (same) |
| Completed | Done. The seller was paid and both deposits are back. | Done. You were paid $9.80 and your deposit is back. |
| Undelivered | The seller didn't deliver in time. You got your money back plus their deposit. | You didn't deliver in time. The buyer got a refund and your deposit. |
| Other closed | Closed. Nothing is locked. | Closed. Nothing is locked. |

Signed-out viewers see a neutral version, for example "Waiting for the seller to accept."

**Empty states**
- **Home, no listings:** "No listings yet. Selling a carry, coaching or items? Create the first listing." [Sell something]
- **Game, none:** "No {Game} listings yet." [Sell something for {Game}]. Filtered: "No {services/items} for {Game} yet." [Show all]
- **Search, none:** "Nothing matches "{q}". Try a game name or a shorter word."
- **My deals, buying:** "You haven't bought anything yet. Every deal you start shows up here." [Browse listings]
- **My deals, selling:** "No one has bought from you yet. Share your listing link where your buyers are: Discord, X or Reddit." [Go to my listings]
- **Notifications:** "Nothing new. You'll get a note here when the other side of a deal does something."
- **My listings:** "You have no listings." [Create a listing]
- **Profile, no record:** "No finished deals on Solana yet. A new seller starts with a clean record, not a bad one."

**Errors** (title, then one line of what to do)
- **Phone, no wallet:** the hand-off sheet (section 5.0).
- **Desktop, no Phantom:** "Install Phantom to continue. Reload this page after installing." [Get Phantom]
- **Rejected in the wallet:** no message (per the frontend doc). Return the button to rest.
- **Phantom on mainnet / simulation fails:** "Phantom is on the main network. Agora runs on the test network. In Phantom: Settings → Developer settings → Testnet mode, then try again."
- **Not enough test USDC or SOL:** "Not enough test money in this wallet. You need $12.00 USDC (test) and a little test SOL for the network fee. Ask the Agora team to fund your wallet."
- **Sync slow (over 30s):** "Solana is taking longer than usual. Refresh in a minute to see if it went through. Don't approve it again yet." Never imply a second try is safe before the refresh.
- **Session expired (401):** "You've been signed out. Sign in again to continue." The sign-in opens automatically, per the doc.
- **Wallet account switched:** "You switched wallets in Phantom, so you've been signed out. Sign in with the new wallet."
- **429:** "Too many requests. Wait a few seconds and try again."
- **503:** "Agora can't reach Solana right now. Your money is safe where it is. Try again in a minute."
- **Deal or listing not found:** "This deal doesn't exist, or the link is wrong." [Go to my deals] / "This listing was removed or the link is wrong." [Browse listings]
- **Listing paused:** "The seller paused this listing. You can't buy it right now."
- **Buying your own listing:** "This is your listing. Share the link with buyers instead." [Copy deal link]
- **Amount validation:**
  - "Use at most 6 decimal places."
  - "The price must split evenly into 8 parts. Try $10.00 or $10.08."
  - "You can offer at most $1.00, what's left in the deal."
- **Offline:** "You're offline. Changes will show when you reconnect."

---

## 8. Asset usage

**Files to cut from the supplied assets.** Do this once and commit to `src/assets/brand/`.

| File | From | How |
|---|---|---|
| `mark.png` (and `@2x`) | `AgoraLogo.webp` (1024×1024) | Square crop around the mark: x 162–862, y 139–839. Export 64 and 128 px. |
| `lockup.png` (and `@2x`) | `AgoraBackground.webp` (1672×941) | Crop the mark plus wordmark: about x 440–1290, y 270–630. Export at 64 and 128 px tall. |
| `og-image.jpg` | `AgoraBackground.webp` | Scale to 1200 wide, then center-crop to 1200×630. Under 300 KB. |
| favicons | `mark.png` | `favicon.ico` (16, 32), `icon-192.png`, `icon-512.png`, `apple-touch-icon.png` (180, on an `#07090E` square, no transparency) |

- **The black backgrounds.** Both source files have black backgrounds, not transparency. In the header (always ink), use them with `mix-blend-mode: screen`: the black drops out and the gold and glow stay. Ask whoever made the logo for a transparent PNG or SVG export; when it exists, remove the blend mode.
- **Logo sizes:** header lockup 28px tall on phones, 32px on desktop. Mark alone at 24px minimum (it becomes a gold column, which is fine). Never use the lockup below 24px tall.
- **Clear space:** the height of the mark's entablature (top block, about 1/5 of the mark's height) on all sides. Nothing else goes inside that space.
- **The logo never goes on a light surface.** In the light theme it lives only in the ink header and ink hero.

**The background template.** The file has the logo baked into its center, so it can't sit behind page text: the logo would collide with the headline.
- **Use the file itself for:** the social share image (og:image), the pitch video's opening and closing frames, and the Build Station title slide.
- **Recreate the background in CSS** for the home hero. It's cheap and crisp at any width:
  - bg `--ink-950`.
  - Two 1px angled lines (pseudo-elements, `linear-gradient` from transparent to `--gold-500` to transparent) at about -28° top-left and bottom-right, matching the template.
  - A floor glow: `radial-gradient(ellipse 60% 18% at 50% 100%, rgb(248 194 58 / .18), transparent)`.
- **Hero text** sits on plain ink, never on the lines. Keep lines out of the left 60% at heights where text sits. The ink ground gives `--text` 17:1, so no overlay is needed.
- **Phone crop:** hide the top-left line below 600px. Keep the floor glow under the example vault.
- **If a version without the centered logo exists,** it can replace the CSS at `background-size: cover; background-position: center 70%`, with a left-to-right ink overlay `linear-gradient(90deg, rgb(7 9 14 / .9) 0 45%, transparent 75%)` behind the text. On phones, use a full `rgb(7 9 14 / .8)` overlay.
- **Light theme:** the hero stays ink (a dark band at the top of a light page). The rest of the page follows the theme.

**Social share.** Static tags in `index.html`:
- `og:title` "Agora: gaming deals where nobody holds your money"
- `og:description` "Buyer and seller both lock a deposit on Solana. Money moves when you both agree."
- `og:image` (absolute URL)
- `twitter:card` = `summary_large_image`

Discord and X don't run Angular, so per-listing previews would need the backend to inject meta tags for `/l/:id`. That's optional backend work after the alpha.

---

## 9. Build order

Each step is visible on its own, so the demo improves even if you stop early.

1. **Tokens and base** (≈1 hour). Paste section 3, the font link, the base and focus rules. The whole app turns ink, paper and gold, and green disappears. This is the biggest visual change for the least work.
2. **Header, test-network band and logo** (≈1–2 hours). Includes the brand crops and the favicon. Every screenshot now looks like Agora.
3. **Buttons, inputs, badges** (≈2 hours). Global classes; existing components pick them up by class. Fix the disabled state and add badge icons.
4. **Deal page** (≈1 day, the priority):
   1. Vault component, including the "who can move this money" list and closed and disputed states.
   2. Status line and countdown.
   3. "Your move" panel with consequence lines, and the phone sticky bar.
   4. Timeline restyle (meta under labels).
   5. "What happens if…".
   6. "On Solana".
5. **Listing page** (≈half a day): price plus the vault preview, seller record, buy panel, own-listing "Copy deal link".
6. **Phone hand-off sheet and the error copy** (≈2 hours).
7. **Motion:** funds-lock and deal-complete only (≈2 hours). Skip the rest if time is short.
8. **Home hero with the example vault, and the games text index** (≈half a day).
9. **/sell, /me, /u restyle.** These mostly inherit; add the "Your move" flag on deal rows.
10. **Light theme pass.** Check every page once with the light color scheme emulated in DevTools.

Each step is deployed on its own, after it has been reviewed and approved. Steps 1–3 are built together because they share the tokens.

---

## 10. Review: what was revised for being generic

Before finalizing, the first plan was checked against the question "would I produce this for any marketplace?" These parts failed and were changed:

| First plan (generic) | Revised (Agora-specific) |
|---|---|
| Home hero: headline, subtext, a grid of game tiles with art | The hero shows an example vault with "Nobody else. Agora has no key." The product is explained by its mechanism. Games become a text index, and no game art is used. |
| Listing cards in a responsive grid with images and star ratings | Ledger-style listing rows with price in mono and the seller's record on Solana (completed / not delivered counts) instead of stars |
| "Secure payments" trust badges and a shield icon on the listing page | A three-step "how your money is protected" with real amounts from the listing, plus a vault preview showing the seller's deposit slot still open |
| Deal page as a stack of equal cards (the current build) | A ranked page: status line saying whose move it is, the gold-edged vault, a key list naming exactly who can move the money, and a "Your move" panel with the consequence of each button in dollars |
| Success animation with confetti on completion | Motion only on money events: segments filling when funds lock, emptying when the deal completes |
| Brand color used for every active state (tabs, hovers, links) | The gold rule: gold only for the brand, locked money and the one primary action |
| Generic error copy ("Something went wrong") | Errors written for this product's real failures: Phantom on mainnet, missing test funds, a phone without a wallet, slow Solana confirmation |
| Light-first theme with brand green | Dark-first to match the assets and Phantom; the light theme uses an ink primary button with gold text, because gold on white fails contrast |

---

## Open items

- The wordmark typeface is unconfirmed. The header uses the cropped image, so this only matters if someone wants live-text wordmarks.
- A transparent export of the mark and lockup would remove the `mix-blend-mode` workaround.
- Status names beyond the seven in `frontend.md` (e.g. Cancelled) are rendered grey with their own name. Check the enum in `api.md` and add status lines for any that need specific wording.
- The reputation labels in 5.3 and 5.7 are examples. Use the fields `GET /api/users/:wallet` actually returns.
