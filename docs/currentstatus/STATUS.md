# Current status: 2026-10-12

A snapshot of where things stand, kept short; history is in `git log`. Plan: [`../PLAN.md`](../PLAN.md). Design: [`../architecture/`](../architecture/README.md).

**Deadline:** submission closes 2026-10-13 06:59 UTC (10:59 Tbilisi). The team targets Oct 12.

## Where we are

| Part | State |
|---|---|
| Solana program | **Deployed to devnet** (`J4fL4Q8nnndro2xmVdCayRt5apXhexkkVQoqqwf4AKi4`) and initialized. Test stablecoin `7FutDfvw3Whjv6PcwCYdYb3FV25EHc3TRiUA4zw8pj21`. 8 program tests pass. No security review yet: devnet only. |
| Backend API | Complete for the alpha. **12 end-to-end tests pass on devnet** (through Helius): a full deal paid match by match, a dispute settled by a split, a missed 2-minute deadline refunded with the seller's deposit, my listings, access checks, validation, sign-in replay and impostor signatures. |
| Admin tool | `init` done. `fund <wallet>` gives a demo wallet 100 test tokens + 0.05 devnet SOL. |
| Frontend | **First version pushed Oct 12** (`c4a2126`, Angular 21): every page in [`frontend.md`](../architecture/frontend.md) (home, game, listing, deal, sell, `/me` with pause/resume and profile editing, the `/u/:wallet` profile); sign-in with the Phantom browser extension (not Phantom Connect yet); every deal action through the backend-built transaction, with polling, the deadline refetch and a "where the money goes" table. `ng build` passes (~0.7 MB). **Not yet tried end to end on devnet.** |
| Demo, pitch, submission | Not started. |

Deploy wallet (`Hw4hT8bo2zsvsTm4W1PGTVmheBZgeGvBU5njNuSrf9a9`, keypair on Sandro's PC only): ~5.5 SOL (topped up Oct 12). A full test run uses ~0.3 SOL (the faucet at https://faucet.solana.com tops it up), and each funded demo wallet 0.05.

## Next steps
1. **Sandro/Claude:** run the demo path end to end on devnet with two funded wallets (list → buy → accept → deliver → confirm; and the `2m` missed-deadline refund), fixing whatever the real UI uncovers on either side.
2. **Sandro:** `Admin fund` demo wallets on request.
3. **Mate and Dato:** the demo script (a listing with the `2m` deadline shows the missed-deadline refund live), the pitch, the video, test carriers and coaches.
4. **Everyone, by Oct 12:** repo README (Claude's draft is in `README.md`; rewrite it), demo video, Colosseum form, in the team's own words.
5. **Sandro, required:** pick the host and deploy, see [Hosting](#hosting-backend-required) below. The backend code for it is done. The project must be hosted at a public link.

## Hosting (backend, required)

**Decision (Sandro): the project definitely needs a public host. This is not up for discussion.** A live link goes into the Colosseum form (`liveProductLink`, plus `liveProductAccessInstructions` for how a judge signs in and gets test tokens) and is shown at the Oct 14 presentation. The rules and FAQ were checked and don't mention a link, so the exact requirement is unknown, but we host regardless. Still to ask the Build Station Tbilisi / Superteam Georgia organizers: how long the link must stay up after Oct 14. *Where* to host is still open: a real host is preferred, and a tunnel from Sandro's PC is the last resort, since it dies when the PC sleeps.

**Done in the backend (Oct 12), all off by default so local runs don't change:**
- **Forwarded headers** (`Hosting:ForwardedHeaders`): behind a proxy or tunnel, the rate limits see each client's real IP instead of the proxy's (otherwise all signed-out visitors share 20 sign-in calls a minute), and the app sees `https`. Only loopback is trusted by default; a proxy on another machine goes in `Hosting:KnownProxies` (IPs) or `Hosting:KnownNetworks` (CIDR), or anyone could fake their IP. `Hosting:HttpsRedirection=false` skips the redirect when the proxy terminates TLS.
- **The frontend is served by the backend** when `wwwroot/index.html` exists: static files, deep links fall back to `index.html`, and unknown `/api/*` routes still return 404. One process, one origin (the API has no CORS and the cookie is same-site). `backend/colosseumbackend/wwwroot/` is git-ignored.
- **`GET /health`** returns `200 Healthy`, for the host's or Sandro's uptime check.

Checked locally on Oct 12: with the settings on, a second client IP gets its own rate-limit bucket, `/` and deep links serve a stand-in index, `/api/nope` is 404; with the defaults, `X-Forwarded-For` is ignored. Not yet checked: a real proxy or tunnel, `X-Forwarded-Proto`, Phantom on the public host.

**How to run it hosted** (PowerShell, from `backend/`):
```powershell
dotnet publish colosseumbackend -c Release -o <publish dir>    # outside the repo
# build the frontend (from frontend/: npm ci; npx ng build) and copy frontend/dist/frontend/browser/* into <publish dir>/wwwroot/
cd <publish dir>                                               # the content root: wwwroot and a relative DB path resolve from here
$env:ASPNETCORE_ENVIRONMENT    = "Production"                  # Swagger and /openapi are off outside Development
$env:ASPNETCORE_URLS           = "http://localhost:8080"       # the port the proxy or tunnel forwards to
$env:Auth__Domain              = "<public host>"               # exactly the page's host (plus port, if any); the sign-in message names it
$env:Jwt__SigningKey           = "<base64, 32+ bytes>"         # required: the app won't start without it in Production
$env:Solana__RpcUrl            = "<Helius devnet URL>"         # has an API key: environment only, never committed
$env:ConnectionStrings__Agora  = "Data Source=<persistent path>\agora.db"   # migrations run at startup
$env:AllowedHosts              = "<public host>;localhost"     # localhost keeps a local /health check working
$env:Hosting__ForwardedHeaders = "true"
$env:Hosting__HttpsRedirection = "false"                       # the proxy terminates TLS
# $env:Hosting__KnownProxies__0 = "<proxy IP>"                 # only if the proxy isn't on this machine
dotnet colosseumbackend.dll
```
A quick tunnel's URL usually changes on every start, and `Auth__Domain` must match it each time, so a fixed hostname is much better. Verify the tunnel product's behavior before relying on it.

**Frontend side:** nothing to configure. The app already calls `/api` as a relative path, and the Phantom browser extension needs no origin registration (only Phantom Connect would, if it replaces the extension later). Phantom checks that the sign-in message's domain matches the page, which is why `Auth__Domain` must be the public host.

**How to check it works** (from a phone on mobile data, not the dev PC): `GET https://<host>/health` and `/api/config` return 200; sign in with Phantom with no domain warning; the session cookie sticks across a reload; two devices signing in at once don't hit `429` (that proves the per-IP limit sees real client IPs); the app opens a deal page end to end.

## Things to know
- **Devnet RPC:** `Solana__RpcUrl` is set to Sandro's Helius devnet URL on his PC; the API, the Admin tool and the tests all use it (else the public devnet RPC, which throttles). It contains an API key: never commit it.
- **Rate limits are per wallet, else per client IP.** Behind a proxy or tunnel, set `Hosting__ForwardedHeaders=true`; see [Hosting](#hosting-backend-required).
- **Phantom sign-in:** the message's domain is `localhost:4200` in Development (matches `ng serve`). If Phantom still warns, `Auth:Domain` must match the page host.
- **Windows Defender** sometimes locks freshly built files, so a build can fail once and pass on retry.
- **Program tests** need OpenSSL environment variables; see `program/README.md`.
- **Production JWT key:** `dotnet user-secrets set Jwt:SigningKey <base64>`. In Development, a random key is generated when it's missing.
