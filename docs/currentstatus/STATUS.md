# Current status: 2026-10-12

A snapshot of where things stand, kept short; history is in `git log`. Plan: [`../PLAN.md`](../PLAN.md). Design: [`../architecture/`](../architecture/README.md).

**Deadline:** submission closes 2026-10-13 06:59 UTC (10:59 Tbilisi). The team targets Oct 12.

## Where we are

| Part | State |
|---|---|
| Solana program | **Deployed to devnet** (`J4fL4Q8nnndro2xmVdCayRt5apXhexkkVQoqqwf4AKi4`) and initialized. Test stablecoin `7FutDfvw3Whjv6PcwCYdYb3FV25EHc3TRiUA4zw8pj21`. 8 program tests pass. No security review yet: devnet only. |
| Backend API | Complete for the alpha. **12 end-to-end tests pass on devnet** (through Helius): a full deal paid match by match, a dispute settled by a split, a missed 2-minute deadline refunded with the seller's deposit, my listings, access checks, validation, sign-in replay and impostor signatures. |
| Admin tool | `init` done. `fund <wallet>` gives a demo wallet 100 test tokens + 0.05 devnet SOL. |
| Frontend | **Not started; the critical path.** Everything it needs is in [`api.md`](../architecture/api.md) and [`frontend.md`](../architecture/frontend.md). |
| Demo, pitch, submission | Not started. |

Deploy wallet (`Hw4hT8bo2zsvsTm4W1PGTVmheBZgeGvBU5njNuSrf9a9`, keypair on Sandro's PC only): **~0.5 SOL, needs a top-up** before funding demo wallets. A full test run uses ~0.3 SOL (the faucet at https://faucet.solana.com tops it up), and each funded demo wallet 0.05.

## Next steps
1. **Saba:** the Angular app, starting with the listing page, the deal page and wallet sign-in (the demo path).
2. **Sandro/Claude:** `Admin fund` demo wallets on request. Support the frontend: fix whatever the real UI uncovers.
3. **Mate and Dato:** the demo script (a listing with the `2m` deadline shows the missed-deadline refund live), the pitch, the video, test carriers and coaches.
4. **Everyone, by Oct 12:** repo README (Claude's draft is in `README.md`; rewrite it), demo video, Colosseum form, in the team's own words.
5. **Sandro, optional, undecided:** backend hosting prep, see [Hosting preparation](#hosting-preparation-backend-optional-undecided) below.

## Hosting preparation (backend, optional, undecided)

**Status of the decision:** the default is *local demo plus video*. Whether a public link is needed is **unconfirmed**: neither the [official rules](https://colosseum.com/legal/Crypto%20World's%20Fair%20Hackathon%20Rules.pdf) nor Colosseum's submission FAQ require one, but the submission form has `liveProductLink` and `liveProductAccessInstructions` fields (both empty, requirement unknown), and Sandro heard a live link is needed. **Ask the Build Station Tbilisi / Superteam Georgia organizers:** is the link for their local presentation (Oct 14) or for the Colosseum form, and must it stay up after Oct 14? Where to host (a tunnel from Sandro's PC as the last resort, or a real host) is **not decided**. The list below is host-independent, so it can be done first. It is all in `backend/`, Sandro's area, except where noted. Nothing here is started.

**Why it's needed.** Behind any reverse proxy or tunnel, Kestrel sees plain `http` from the proxy's IP, which breaks three things:
- **Rate limits:** every signed-out visitor shares the proxy's IP, so all of them together get 20 sign-in calls a minute ([`ServiceCollectionExtensions.cs`](../../backend/colosseumbackend/Extensions/ServiceCollectionExtensions.cs), `FixedWindow` keys on the wallet, else `RemoteIpAddress`).
- **HTTPS redirect:** `UseHttpsRedirection` in [`Program.cs`](../../backend/colosseumbackend/Program.cs) would redirect the proxy's `http` request to `https` forever.
- **Sign-in message:** it must name the public host, not `localhost`.

**Tasks:**
1. **Forwarded headers.** Add `UseForwardedHeaders` (`XForwardedFor | XForwardedProto`) at the top of the pipeline in `Program.cs`, before `UseHttpsRedirection` and `UseRateLimiter`. Keep it **opt-in by config** (for example `ForwardedHeaders:Enabled`), so local runs don't change, and trust only the proxy (`KnownProxies`/`KnownNetworks`), or anyone can fake their IP and dodge the limits. If the proxy terminates TLS, also skip `UseHttpsRedirection` (config flag).
2. **Production settings**, all through environment variables (never committed):
   - `Auth__Domain`: exactly the public host the page is served from, including any port (the sign-in message names it; Phantom may warn otherwise).
   - `Jwt__SigningKey`: base64, 32+ bytes. In Production a missing key makes the app throw at startup (only Development generates a random one).
   - `Solana__RpcUrl`: the private Helius URL, from the environment only.
   - `ConnectionStrings__Agora`: a SQLite path on **persistent storage** (migrations already run at startup).
   - `AllowedHosts`: the public host instead of `*`.
   - `ASPNETCORE_ENVIRONMENT=Production` turns Swagger and the OpenAPI document **off** (they are Development-only); decide whether the demo needs them.
3. **One origin for the frontend and the API.** The API has no CORS and the cookie is same-site, so the Angular app and `/api` must share a host. Either the backend serves the Angular build from `wwwroot` (`UseDefaultFiles`, `UseStaticFiles`, then `MapFallbackToFile("index.html")` after `MapControllers`, leaving `/api/*` alone), or a reverse proxy routes `/api` to the backend and everything else to the app. The first is one process and one URL. It needs Saba's build output (`frontend/dist/...`) copied into `wwwroot`, so **ask Saba** for the build command and output path. Not possible until the frontend exists.
4. **Health check (optional).** `MapHealthChecks("/health")`, for the host's or Sandro's uptime check.
5. **Write down how to run it** in this file or `README.md`: `dotnet publish -c Release`, the environment variables above, and, if a tunnel is used, the tunnel command. A quick tunnel's URL usually changes on every start, and `Auth__Domain` has to match it each time, so a fixed hostname is much better. Verify the tunnel product's behavior before relying on it.

**Frontend side (Saba, for later):** register the public origin in the Phantom Connect developer portal alongside `localhost:4200`, and build the app with `/api` as a relative path so it works on any host.

**How to check it works** (from a phone on mobile data, not the dev PC): `GET https://<host>/api/config` returns 200; sign in with Phantom with no domain warning; the session cookie sticks across a reload; two devices signing in at once don't hit `429` (that proves the per-IP limit sees real client IPs); the app opens a deal page end to end. Then run `dotnet test` (about 0.3 SOL: top up the deploy wallet first) to confirm nothing regressed; tests run without proxy settings, so they exercise the unchanged local path.

## Things to know
- **Devnet RPC:** `Solana__RpcUrl` is set to Sandro's Helius devnet URL on his PC; the API, the Admin tool and the tests all use it (else the public devnet RPC, which throttles). It contains an API key: never commit it.
- **Rate limits are per wallet, else per client IP.** Behind a proxy or tunnel they need forwarded-headers handling first; see [Hosting preparation](#hosting-preparation-backend-optional-undecided).
- **Phantom sign-in:** the message's domain is `localhost:4200` in Development (matches `ng serve`). If Phantom still warns, `Auth:Domain` must match the page host.
- **Windows Defender** sometimes locks freshly built files, so a build can fail once and pass on retry.
- **Program tests** need OpenSSL environment variables; see `program/README.md`.
- **Production JWT key:** `dotnet user-secrets set Jwt:SigningKey <base64>`. In Development, a random key is generated when it's missing.
