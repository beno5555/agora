# Current status: 2026-10-13

A snapshot of where things stand, kept short; history is in `git log`. Plan: [`../PLAN.md`](../PLAN.md). Design: [`../architecture/`](../architecture/README.md).

**Deadline:** submission closes 2026-10-13 06:59 UTC (10:59 Tbilisi). The team targets Oct 12.

## Where we are

| Part | State |
|---|---|
| Solana program | **Deployed to devnet** (`J4fL4Q8nnndro2xmVdCayRt5apXhexkkVQoqqwf4AKi4`) and initialized. Test stablecoin `7FutDfvw3Whjv6PcwCYdYb3FV25EHc3TRiUA4zw8pj21`. 8 program tests pass. No security review yet: devnet only. |
| Backend API | Complete for the alpha. **15 end-to-end tests pass on devnet** (through Helius): a full deal paid match by match, a dispute settled by a split, a missed 2-minute deadline refunded with the seller's deposit, my listings, access checks, validation, sign-in replay and impostor signatures. |
| Admin tool | `init` done. `fund <wallet>` gives a demo wallet 100 test tokens + 0.05 devnet SOL. |
| Frontend | **First version pushed Oct 12** (`c4a2126`, Angular 21): every page in [`frontend.md`](../architecture/frontend.md) (home, game, listing, deal, sell, `/me` with pause/resume and profile editing, the `/u/:wallet` profile); sign-in with the Phantom browser extension (not Phantom Connect yet); every deal action through the backend-built transaction, with polling, the deadline refetch and a "where the money goes" table. `ng build` passes (~0.7 MB). **Clicked through end to end on devnet (Oct 12)** by an automated browser run (Chrome driven by Playwright, with a stand-in for Phantom that signs with throwaway keys): a deal paid match by match, the `2m` missed-deadline refund, pause/resume, profile editing, the public profile. All passed. **Sandro then tried most of it by hand with the real Phantom extension (Oct 12–13): fine.** |
| Demo, pitch, submission | Not started. |

Deploy wallet (`Hw4hT8bo2zsvsTm4W1PGTVmheBZgeGvBU5njNuSrf9a9`, keypair on Sandro's PC only): ~5.5 SOL (topped up Oct 12). A full test run uses ~0.3 SOL (the faucet at https://faucet.solana.com tops it up), and each funded demo wallet 0.05.

## Next steps
1. **Sandro:** the app is live at **https://46-101-154-81.sslip.io** (see [Hosting](#hosting-backend-required)). Run the checks in [`deploy/README.md`](../../deploy/README.md#check-that-it-works) from a phone, then the demo path once on the hosted site with the real Phantom extension: list → buy → accept → deliver → confirm, and the `2m` missed-deadline refund.
2. **Sandro:** `Admin fund` demo and judges' wallets on request. **Decided (Oct 13): a judge sends their wallet address to Sandro, who funds it** (the backend holds no keys, so judges can't fund themselves). The form's access instructions need Sandro's contact (Discord or Telegram).
3. **Mate and Dato:** the demo script (a listing with the `2m` deadline shows the missed-deadline refund live), the pitch, the video, test carriers and coaches.
4. **Everyone, by Oct 12:** repo README (Claude's draft is in `README.md`; rewrite it), demo video, Colosseum form, in the team's own words.

## Hosting (backend, required)

**Decision (Sandro): the project definitely needs a public host. This is not up for discussion.** A live link goes into the Colosseum form (`liveProductLink`, plus `liveProductAccessInstructions` for how a judge signs in and gets test tokens) and is shown at the Oct 14 presentation. The rules and FAQ were checked and don't mention a link, so the exact requirement is unknown, but we host regardless. Still to ask the Build Station Tbilisi / Superteam Georgia organizers: how long the link must stay up after Oct 14.

**Decided (Oct 13, Sandro):** a DigitalOcean droplet ($6/mo, 1 GB, Frankfurt, Ubuntu 24.04; billed hourly, so the $5 credit lasts ~25 days) at `https://<ip-with-dashes>.sslip.io` (no domain), Caddy in front for HTTPS, a self-contained .NET publish (no .NET install on the server) run by systemd, SQLite kept, no CI/CD: one deploy script run from Sandro's PC. **The reasons, the step-by-step setup, deploys, checks and troubleshooting are in [`deploy/README.md`](../../deploy/README.md).** **Live since Oct 12, 23:30 UTC at https://46-101-154-81.sslip.io** (droplet `agora`, IP `46.101.154.81`): the first `deploy.ps1 -Setup` ran cleanly, the HTTPS certificate was issued, `/health` answers over HTTPS and the frontend loads. Not yet checked there: Phantom sign-in, the per-IP rate limit from two devices, the demo path.

**Done in the backend (Oct 12), all off by default so local runs don't change** (the deploy turns them on in `deploy/agora.service`):
- **Forwarded headers** (`Hosting:ForwardedHeaders`): behind a proxy or tunnel, the rate limits see each client's real IP instead of the proxy's (otherwise all signed-out visitors share 20 sign-in calls a minute), and the app sees `https`. Only loopback is trusted by default; a proxy on another machine goes in `Hosting:KnownProxies` (IPs) or `Hosting:KnownNetworks` (CIDR), or anyone could fake their IP. `Hosting:HttpsRedirection=false` skips the redirect when the proxy terminates TLS.
- **The frontend is served by the backend** when `wwwroot/index.html` exists: static files, deep links fall back to `index.html`, and unknown `/api/*` routes still return 404. One process, one origin (the API has no CORS and the cookie is same-site). `backend/colosseumbackend/wwwroot/` is git-ignored.
- **`GET /health`** returns `200 Healthy`, for the host's or Sandro's uptime check.

Checked locally on Oct 12: with the settings on, a second client IP gets its own rate-limit bucket, `/` and deep links serve a stand-in index, `/api/nope` is 404; with the defaults, `X-Forwarded-For` is ignored. Not yet checked: a real proxy or tunnel, `X-Forwarded-Proto`, Phantom on the public host.

**Frontend side:** nothing to configure. The app already calls `/api` as a relative path, and the Phantom browser extension needs no origin registration (only Phantom Connect would, if it replaces the extension later). Phantom checks that the sign-in message's domain matches the page, which is why `Auth__Domain` must be the public host.

**Good to know when hosting (Oct 13):**
- The frontend build is `npm ci` then `npx ng build` in `frontend/`, output in `frontend/dist/frontend/browser/` (~0.7 MB). In the automated browser run, the only console error was a `401` on `/api/me` for signed-out visitors, which is normal.
- Database migrations run at startup, so a fresh `agora.db` on the host is fine. The newest one (`ListingUnitEnum`) applies without warnings.
- A running backend locks `bin/Debug`, so a build or test in another terminal needs `-c Release`.
- The access instructions for judges should say: install Phantom, turn on Testnet Mode and pick Solana Devnet (Settings → Developer Settings), send your wallet address to Sandro (contact to be added) to get test tokens and devnet SOL, and expect Phantom to show our test token as "unknown" (backlog item in [`BACKLOG.md`](BACKLOG.md)).
- `Auth__Domain` must be the exact public host, or Phantom warns on sign-in. `deploy.ps1 -Setup` sets it to the sslip.io host.

## Things to know
- **Devnet RPC:** `Solana__RpcUrl` is set to Sandro's Helius devnet URL on his PC; the API, the Admin tool and the tests all use it (else the public devnet RPC, which throttles). It contains an API key: never commit it.
- **Rate limits are per wallet, else per client IP.** Behind a proxy, `Hosting__ForwardedHeaders=true` (set on the host by `deploy/agora.service`); see [Hosting](#hosting-backend-required).
- **Phantom sign-in:** the message's domain is `localhost:4200` in Development (matches `ng serve`). If Phantom still warns, `Auth:Domain` must match the page host.
- **Windows Defender** sometimes locks freshly built files, so a build can fail once and pass on retry.
- **Program tests** need OpenSSL environment variables; see `program/README.md`.
- **Production JWT key:** `dotnet user-secrets set Jwt:SigningKey <base64>`. In Development, a random key is generated when it's missing.
