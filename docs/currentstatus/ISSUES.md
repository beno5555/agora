# Cross-side issues

For a problem found in the *other* person's area while working in your own (e.g. Saba hits a backend bug while building the frontend). Don't fix it yourself across the ownership line (see `CLAUDE.md` → Team workflow) — add a row here and keep working on your own side. The owner checks this file, fixes it in their area, clears the row, and (if it's an API change) updates `docs/architecture/api.md` in the same commit.

Not for product decisions or planning — that's `docs/PLAN.md`. Not for a snapshot of overall progress — that's `STATUS.md`.

| Date | Reported by | Area | Issue | Status |
|---|---|---|---|---|
<!-- example:
| 10-12 | Saba | backend | GET /api/listings/{id} 500s when game has no icon | fixed (a876b2c) |
-->
