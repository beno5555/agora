# Cross-side issues

For a problem found in the *other* person's area while working in your own (e.g. Saba hits a backend bug while building the frontend). Don't fix it yourself across the ownership line (see `CLAUDE.md` → Team workflow) — add a row here and keep working on your own side. The owner checks this file, fixes it in their area, clears the row, and (if it's an API change) updates `docs/architecture/api.md` in the same commit.

Not for product decisions or planning — that's `docs/PLAN.md`. Not for a snapshot of overall progress — that's `STATUS.md`.

| Date | Reported by | Area | Issue | Status |
|---|---|---|---|---|
| 10-12 | Sandro | frontend | `core/models.ts` `ConfigDto` has `rpcUrl`, which isn't in `api.md` and the backend never sends (and never will: the RPC URL holds the Helius API key). Remove it. | open |
| 10-12 | Sandro | frontend | `/u/:wallet` public profile page is missing (listed in `frontend.md`, data from `GET /api/users/{wallet}`). Seller names on the listing and deal pages could link to it. | open |
| 10-12 | Sandro | frontend | `/me` builds "My listings" from `GET /api/users/{wallet}`, which returns active listings only. Use `GET /api/me/listings` (includes paused ones). Pause/resume buttons and profile editing (`PATCH /api/me`: display name, Discord) aren't there yet. | open |
| 10-12 | Sandro | frontend | Deal page: the "what happens under each outcome" table (`frontend.md`, deal page §2) isn't implemented. Lower priority than the demo path. | open |
| 10-12 | Sandro | frontend | `core/auth.service.ts` has a commented-out `domain` param on `/api/auth/nonce`. The backend ignores it on purpose (the domain comes from `Auth:Domain`, so a page can't choose what it asks you to sign). Delete it; if Phantom warns about the domain, report which host/port here. | open |
| 10-12 | Sandro | frontend | Housekeeping: run Prettier (indentation is off in `api.service.ts` `messageOf`, `auth.service.ts` `catch`, `app.config.ts`); delete `frontend/.gitkeep`; update the file list and the wallet note in `frontend.md` (it uses the Phantom extension, and adds `auth.guard`, `toast.service`, `clock.service`, `listing-card`, `status-badge`, `plural`, `delivery-window`). | open |
<!-- example:
| 10-12 | Saba | backend | GET /api/listings/{id} 500s when game has no icon | fixed (a876b2c) |
-->
