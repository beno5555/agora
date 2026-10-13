# Known issues left for later

Problems we know about and decided not to fix yet, so they aren't forgotten. A problem that gets fixed when it's found doesn't go here. Whoever fixes one removes its row in the same commit (and updates `docs/architecture/api.md` if the fix changes the API).

Not for improvements or features we plan to add later — that's [`BACKLOG.md`](BACKLOG.md). Not for product decisions or planning — that's `docs/PLAN.md`. Not for a snapshot of overall progress — that's `STATUS.md`.

| Date | Area | Issue | Why it waits |
|---|---|---|---|
| 10-14 | backend | The session JWT lives 7 days, with no refresh token and no server-side revocation (logout only clears the cookie). A stolen token works off-chain (listings, profile, own deals) until it expires. Fix: 5-minute access token plus a rotating, hashed refresh token in the database, revoked on logout | not our main concern while we build the MVP: a session can't move funds (every fund movement needs a wallet signature checked on-chain) and the cookie is HttpOnly |
<!-- example:
| 10-12 | backend | GET /api/listings/{id} is slow with 1000+ listings | fine at demo scale; revisit after the hackathon |
-->
