# Current status: 2026-10-13

A snapshot of where things stand, kept short; history is in `git log`. Plan: [`../PLAN.md`](../PLAN.md). Design: [`../architecture/`](../architecture/README.md).

**Submitted** (Oct 13) on the Colosseum portal and the Superteam Georgia Track. **Presentation:** Oct 14, offline Demo Day. **Results:** Oct 27.

## Where we are

| Part | State |
|---|---|
| Solana program | **Deployed to devnet** (`J4fL4Q8nnndro2xmVdCayRt5apXhexkkVQoqqwf4AKi4`) and initialized. Test stablecoin `7FutDfvw3Whjv6PcwCYdYb3FV25EHc3TRiUA4zw8pj21`. 11 program tests pass. **Upgraded Oct 14:** reputation is kept per role (buyer, seller) at a new address, so every record restarted from zero; open disagreements raise both parties' deposits until settled ([program.md](../architecture/program.md#deposits)). No security review yet: devnet only. |
| Backend API | Complete for the alpha. **15 end-to-end tests pass on devnet** (through Helius): a full deal paid match by match, a dispute settled by a split, a missed 2-minute deadline refunded with the seller's deposit, my listings, access checks, validation, sign-in replay and impostor signatures. |
| Admin tool | `init` done. `fund <wallet>` gives a demo wallet 100 test tokens + 0.05 devnet SOL. |
| Frontend | **Redesign done and deployed (Oct 13, 03:10 UTC), see [Frontend redesign](#frontend-redesign-done).** **First version pushed Oct 12** (`c4a2126`, Angular 21): every page in [`frontend.md`](../architecture/frontend.md) (home, game, listing, deal, sell, `/me` with pause/resume and profile editing, the `/u/:wallet` profile); sign-in with the Phantom browser extension (not Phantom Connect yet); every deal action through the backend-built transaction, with polling, the deadline refetch and a "where the money goes" table. `ng build` passes (~0.7 MB). **Clicked through end to end on devnet (Oct 12)** by an automated browser run (Chrome driven by Playwright, with a stand-in for Phantom that signs with throwaway keys): a deal paid match by match, the `2m` missed-deadline refund, pause/resume, profile editing, the public profile. All passed. **Sandro then tried most of it by hand with the real Phantom extension (Oct 12–13): fine.** |
| Demo, pitch, submission | **Submitted** on the Colosseum portal and the Superteam Georgia Track (Superteam Earn). Demo video recorded (2:52). |

Deploy wallet (`Hw4hT8bo2zsvsTm4W1PGTVmheBZgeGvBU5njNuSrf9a9`, keypair on Sandro's PC only): ~5.5 SOL (topped up Oct 12). A full test run uses ~0.3 SOL (the faucet at https://faucet.solana.com tops it up), and each funded demo wallet 0.05.

## Next steps
**The code is done (Oct 13, 05:15 UTC):** only fixes for something broken. Everything is deployed (frontend code last changed in `30aee15`). **All docs were brought up to date on Oct 13**, and `README.md` was rewritten for judges (live link, how to try it, repo map, tests, limits).

**Submission (Oct 13):** Agora is submitted on the **Colosseum portal** and the **Superteam Georgia Track** on Superteam Earn ([listing](https://superteam.fun/earn/listing/colosseum-crypto-worlds-fair-hackathon-superteam-georgia-track/)), whose winners are announced at the offline Demo Day on Oct 14.

1. **Team, for Oct 14:** prepare the presentation: the pitch plus a live deal on the hosted site, with the demo video (2:52) as the backup if devnet is slow. Start the `Demo: 2-minute deadline` deal a few minutes before presenting, so its refund is ready to show.
2. **Sandro, ongoing:** `Admin fund <wallet>` for judges who send their address; accept the deals judges start on demo listings (import a demo seller into Phantom, see below); keep the droplet running.

**Demo data on the live site (Oct 13):** three demo sellers (NovaCarry, LootSmith, RiftCoach) with 11 listings across all six games, including `Demo: 2-minute deadline (try the refund)`; each funded with 100 test tokens + 0.05 SOL. **Their keys are in `C:\Users\admin\.agora-demo\sellers.json` on Sandro's PC only** (`phantomPrivateKey` imports into Phantom: Add account → Import private key). Sandro's own test listings are paused. **Steam items are in scope** (Sandro, Oct 13; recorded in PLAN.md): a CS2 skin listing was swapped for "Faceit duo, 4 matches" on a wrong assumption, and it stays as is.

**Fine-tuning after the redesign (Oct 13, all deployed):** Pause/Resume in its own cell on `/me`; the deal timeline no longer shows "Completed: Now" mid-delivery; the header brand is the mark plus "AGORA" in Cinzel; a tab title per page; relative times and "See more" on notifications; transparent favicon and app icons; lists in pages of 10 (home, game, `/me` deals and listings; `shared/pager.ts` asks for one item past the page because the API has no total count); home search runs 0.5s after typing pauses; `/me` survives deal rows without `allowedActions`.

## Frontend redesign (done)

The brief is [`../design/DESIGN.md`](../design/DESIGN.md): tokens, components, page layouts, motion, copy. It is followed as written, with the decisions at its top: the CSS split, its wording wins over `frontend.md` (which is updated in the same commit), and data the API lacks is derived in the frontend or added to the backend in the same commit. The `frontend-design` skill (`.claude/skills/`) guides it. The source logo and background are in `docs/design/assets/`; `docs/design/assets/issues/` is Sandro's scratch folder, never commit it.

| Step (DESIGN.md §9) | State |
|---|---|
| 1–3 Tokens, base, header, brand assets, buttons, inputs, badges | Done (`fea18ca`, `6d2b67a`) |
| 4 Deal page | Done (`110638d`) |
| 5 Listing page | Done (`3a837c1`) |
| 6 Phone hand-off sheet and error copy | Done (`f8e9fba`) |
| 7 Motion: funds-lock and deal-complete | Done (`c2c359d`). The vault plays them when the deal page sees the change (your own action or the poll), and after Buy (router state from the listing page). |
| 8 Home hero with the example vault, games as a text index | Done (`d395347`) |
| 9 /sell, /me, /u and /g restyle (+ "Your move" flag on deal rows, empty states from §7) | Done (`a41d331`). Includes a backend change: `GET /api/me/deals` rows now carry `allowedActions`. |
| 10 Light theme pass | Done (`757f21f`). An `.ink` class re-applies the dark tokens to the home hero. **`index.html` still forces `data-theme="dark"`** for the demo (DESIGN.md §3); removing that attribute turns the light theme on for light-mode systems. |
| Fixes from Sandro's review | Done: compact phone header buttons, inset select arrows (`3afe672`), no scrollbar on the /me tabs (`e0b84c2`) |

**Where things are:**
- **Global CSS** (`frontend/src/styles/`, listed in `angular.json`): `tokens.css` (from DESIGN.md §3, plus `--lock-amount` and `--vault-border`; links are plain text with an underline, the gold rule wins over the brief's gold link token), `base.css`, `utilities.css`, `controls.css` (`.btn` variants, inputs, `.amount-input`), `surfaces.css` (`.panel`, `.tabs`, `dl.facts`, `.badge`, `.notice`, `.empty-state`).
- **`frontend/src/styles.css` is gone** (step 9): every page has its own css now.
- **Component CSS** sits next to each component (`styleUrl`): `app.css` (header, test-network band, sign-in/hand-off sheets, toasts), `pages/home/home.css`, `me.css`, `sell.css`, `game.css`, `profile.css`, `shared/vault.css`, `reputation.css`, `listing-card.css`, `wallet-address.css`, `pages/deal/deal.css`, `deal-timeline.css`, `pages/listing/listing.css`.
- **New shared pieces:** `shared/vault.ts` (the gold vault; deal page, listing preview, and the home hero in step 8), `icon.ts` (Lucide icons as inline SVG), `media.ts` (`mediaQuery()` signal), `tab-keys.ts` (arrow keys in tablists), `status-badge.ts` exports `statusLook()`. `pages/deal/deal-money.ts` holds every amount and sentence of the deal page. `core/error-text.ts` holds the error copy.
- **Brand files** cut with ImageMagick into `frontend/public/brand/` (lockup, mark, `mark-header.png` for the header, icons, `og-image.jpg`) plus `public/favicon.ico`. The header mark, the favicon and `icon-192/512` are transparent (alpha from brightness); `apple-touch-icon` stays opaque.

**How each step was checked:** `npx ng build`, then a throwaway Playwright script (not committed) that serves fixture API data and screenshots every page and deal state on desktop (1280px) and an iPhone viewport (390px), signed in and out, and drives the stepper, the Disagree confirmation and the split validation. Playwright's Chromium is installed at `%LOCALAPPDATA%\ms-playwright\chromium-1248`; `playwright-core` was installed into a scratch folder. Real Phantom and on-chain actions were not part of these checks: Sandro tries them on the live host after each deploy. **To redo the screenshots in a new session:** run `npx ng serve --port 4300` yourself (a dev server on 4200 left from an earlier session served a stale build, and `ng serve` must be restarted after `angular.json` changes); the harness from Oct 13 is `shots.mjs` in that session's scratchpad (`%TEMP%claude...cb0ae39b-…scratchpad`, it may be cleaned up): `node shots.mjs <outDir> "<regex of page names>"`, with `MOTION=1` for the vault animations and `LIGHT=1` for the light theme.

**Known gaps:** listing rows can't show "12 deals done" (`MinimalListingDto` has no reputation); signed out, the listing page can't show the buyer's deposit (it depends on the buyer's record), so it explains it in words. The per-component style budget warning is 6 kB.

## Hosting (backend, required)

**Decision (Sandro): the project definitely needs a public host. This is not up for discussion.** A live link goes into the Colosseum form (`liveProductLink`, plus `liveProductAccessInstructions` for how a judge signs in and gets test tokens) and is shown at the Oct 14 presentation. The rules and FAQ were checked and don't mention a link, so the exact requirement is unknown, but we host regardless. Still to ask the Build Station Tbilisi / Superteam Georgia organizers: how long the link must stay up after Oct 14.

**Decided (Oct 13, Sandro):** a DigitalOcean droplet ($6/mo, 1 GB, Frankfurt, Ubuntu 24.04; billed hourly at $0.00893, so the $5 credit lasts about 23 days: created the evening of Oct 12, it runs out around Nov 4–5, after the Oct 27 results. After that DigitalOcean bills the card; only Destroy (not power off) stops it) at `https://<ip-with-dashes>.sslip.io` (no domain), Caddy in front for HTTPS, a self-contained .NET publish (no .NET install on the server) run by systemd, SQLite kept, no CI/CD: one deploy script run from Sandro's PC. **The reasons, the step-by-step setup, deploys, checks and troubleshooting are in [`deploy/README.md`](../../deploy/README.md).** **Live since Oct 12, 23:30 UTC at https://46-101-154-81.sslip.io** (droplet `agora`, IP `46.101.154.81`): the first `deploy.ps1 -Setup` ran cleanly, the HTTPS certificate was issued, `/health` answers over HTTPS and the frontend loads. **Checked by Sandro (Oct 13):** Phantom sign-in on PC (no domain warning, the session survives a reload) and on a phone (Connect reopens the page in the Phantom app's browser, then sign-in and deal actions work). Not yet checked there: the per-IP rate limit from two devices, the full demo path.

**New address since Oct 13, ~17:05 UTC: https://agora-market.duckdns.org**, a free DuckDNS name (Sandro's DuckDNS account) pointed at the droplet. `46-101-154-81.sslip.io` now sends visitors there with a temporary redirect that keeps the path, so the link in the submitted access instructions and the demo video still works. Switched without `-Setup` (the JWT key is unchanged), as in [`deploy/README.md`](../../deploy/README.md#change-the-host-of-a-running-server); the old Caddy config and `agora.env` are backed up in `/root/*.bak` on the server. Deploys read the host from the server: `./deploy/deploy.ps1 -Server 46.101.154.81`. **Checked by Claude after the switch and after a fresh deploy:** Let's Encrypt certificate for the new name, `/health`, `/api/config`, the page, its scripts and deep links answer 200; http and the old address redirect with the path kept; a demo seller signed in through the API (the sign-in message names the new host, the cookie is `Secure`, `HttpOnly`, `SameSite=Lax`, host-only), the signed-in reads (`/api/me`, own listings, deals, a deal read from the chain, notifications) and logout work. **Sandro (Oct 13): everything works on the new address**; a phone check there is still open. Everyone signs in again once there: a session from the old address doesn't carry over. If DuckDNS ever goes down, remove the redirect block from `/etc/caddy/Caddyfile` and restore `/root/agora.env.bak` to go back to sslip.io.

**Done in the backend (Oct 12), all off by default so local runs don't change** (the deploy turns them on in `deploy/agora.service`):
- **Forwarded headers** (`Hosting:ForwardedHeaders`): behind a proxy or tunnel, the rate limits see each client's real IP instead of the proxy's (otherwise all signed-out visitors share 20 sign-in calls a minute), and the app sees `https`. Only loopback is trusted by default; a proxy on another machine goes in `Hosting:KnownProxies` (IPs) or `Hosting:KnownNetworks` (CIDR), or anyone could fake their IP. `Hosting:HttpsRedirection=false` skips the redirect when the proxy terminates TLS.
- **The frontend is served by the backend** when `wwwroot/index.html` exists: static files, deep links fall back to `index.html`, and unknown `/api/*` routes still return 404. One process, one origin (the API has no CORS and the cookie is same-site). `backend/colosseumbackend/wwwroot/` is git-ignored.
- **`GET /health`** returns `200 Healthy`, for the host's or Sandro's uptime check.

Checked locally on Oct 12: with the settings on, a second client IP gets its own rate-limit bucket, `/` and deep links serve a stand-in index, `/api/nope` is 404; with the defaults, `X-Forwarded-For` is ignored. The live host has since covered a real proxy (Caddy) and Phantom on the public host. Not yet checked: `X-Forwarded-Proto` as the app sees it, and the per-IP rate limit from two devices.

**Frontend side:** nothing to configure. The app already calls `/api` as a relative path, and the Phantom browser extension needs no origin registration (only Phantom Connect would, if it replaces the extension later). Phantom checks that the sign-in message's domain matches the page, which is why `Auth__Domain` must be the public host.

**Good to know when hosting (Oct 13):**
- The frontend build is `npm ci` then `npx ng build` in `frontend/`, output in `frontend/dist/frontend/browser/` (~0.7 MB). In the automated browser run, the only console error was a `401` on `/api/me` for signed-out visitors, which is normal.
- Database migrations run at startup, so a fresh `agora.db` on the host is fine. The newest one (`ListingUnitEnum`) applies without warnings.
- A running backend locks `bin/Debug`, so a build or test in another terminal needs `-c Release`.
- The access steps for judges are in [`README.md`](../../README.md#try-it) (Phantom's "unknown" token is a backlog item in [`BACKLOG.md`](BACKLOG.md)).
- `Auth__Domain` must be the exact public host, or Phantom warns on sign-in. `deploy.ps1 -Setup` sets it to the `-HostName` given, else the sslip.io host. It is `agora-market.duckdns.org` now.

## Things to know
- **Devnet RPC:** `Solana__RpcUrl` is set to Sandro's Helius devnet URL on his PC; the API, the Admin tool and the tests all use it (else the public devnet RPC, which throttles). It contains an API key: never commit it.
- **Rate limits are per wallet, else per client IP.** Behind a proxy, `Hosting__ForwardedHeaders=true` (set on the host by `deploy/agora.service`); see [Hosting](#hosting-backend-required).
- **Phantom sign-in:** the message's domain is `localhost:4200` in Development (matches `ng serve`). If Phantom still warns, `Auth:Domain` must match the page host.
- **Windows Defender** sometimes locks freshly built files, so a build can fail once and pass on retry.
- **Program tests** need OpenSSL environment variables; see `program/README.md`.
- **Production JWT key:** `dotnet user-secrets set Jwt:SigningKey <base64>`. In Development, a random key is generated when it's missing.
