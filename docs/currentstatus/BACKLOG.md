# Backlog

Planned improvements and features we want to do, but not now: they aren't on the path to the current milestone (the hackathon submission and demo). Pick from here when the main work is done or when a task touches the same code anyway. Whoever finishes an item removes its row in the same commit.

Not for known bugs or broken behavior left for later — that's [`ISSUES.md`](ISSUES.md). Not for what's being worked on now — that's [`STATUS.md`](STATUS.md). Not for product direction — that's [`../PLAN.md`](../PLAN.md).

Each row says enough to start without asking: where it applies, what "done" looks like, and any facts already checked.

| Added | Area | Task | Details |
|---|---|---|---|
| 10-12 | frontend | Show character limits on form fields | Show the limit where it's reasonable to have one, and a live count on the listing description (e.g. `465/2000`). Current limits (frontend `maxlength` = backend `Constants`): title 100, description 2000 (sell form); buyer note 300 (listing page); display name 40, Discord 40 (`/me`). Short fields could show the limit as a hint or a count once you're near it; the description always shows the count. |
| 10-13 | backend (Admin tool) | Give the test stablecoin a name, symbol and icon | Phantom shows our devnet test token as "unknown" in its transaction popup and balance list, because the mint has no metadata: `AdminChain.CreateMintAsync` only runs `InitializeMint` (`backend/colosseumbackend.Admin/AdminChain.cs`). Add on-chain token metadata (Metaplex Token Metadata, signed by the mint authority, which is the deploy wallet) with a name like "Agora Test USD", a symbol, and an icon hosted at a stable URL, as a new Admin command that works on the existing mint (`Solana:Mint`, `7FutDfv...`) so nothing has to be redeployed. **Check in a real Phantom on devnet that the name actually shows up** before relying on it. Not urgent: real users would use a stablecoin Phantom already knows (USDC), and until then the access instructions in the Colosseum form can say the token is our own devnet test token. |
