# Backlog

Planned improvements and features we want to do, but not now: they aren't on the path to the current milestone (the hackathon submission and demo). Pick from here when the main work is done or when a task touches the same code anyway. Whoever finishes an item removes its row in the same commit.

Not for known bugs or broken behavior left for later — that's [`ISSUES.md`](ISSUES.md). Not for what's being worked on now — that's [`STATUS.md`](STATUS.md). Not for product direction — that's [`../PLAN.md`](../PLAN.md).

Each row says enough to start without asking: where it applies, what "done" looks like, and any facts already checked.

| Added | Area | Task | Details |
|---|---|---|---|
| 10-12 | frontend | Show character limits on form fields | Show the limit where it's reasonable to have one, and a live count on the listing description (e.g. `465/2000`). Current limits (frontend `maxlength` = backend `Constants`): title 100, description 2000, unit label 20 (sell form); buyer note 300 (listing page); display name 40, Discord 40 (`/me`). Short fields could show the limit as a hint or a count once you're near it; the description always shows the count. |
