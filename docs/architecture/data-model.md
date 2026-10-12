# Backend data model

EF Core with **SQLite** for the alpha (zero setup, one file). Switching to Postgres later only changes the provider. Money columns store token base units as `long` (u64 values up to 2^63 are enough for stablecoin amounts). Times are UTC; SQLite drops the kind, so `AgoraDbContext` marks every `DateTime` it reads as UTC.

Fund movements are never stored as the truth. The `Deal*` mirror columns are a cache of the on-chain account.

## Tables

**User**
| Column | Type | Notes |
|---|---|---|
| `Wallet` | string (PK) | base58 address |
| `DisplayName` | string? | |
| `Discord` | string? | |
| `CreatedAt` | DateTime | |

**AuthNonce**: `Wallet` (PK), `Message`, `ExpiresAt`. Deleted as soon as it's used.

**Game**: `Id`, `Slug` (unique), `Name`, `IconUrl`. Seeded (Fortnite, Valorant, League of Legends, CS2, …).

**Listing**
| Column | Type | Notes |
|---|---|---|
| `Id` | Guid (PK) | used in the shareable link |
| `SellerWallet` | FK → User | |
| `GameId` | FK → Game | |
| `Type` | enum | `Service`, `Item` |
| `Title`, `Description` | string | |
| `Price` | long | total, base units |
| `Milestones` | int | 1 for items |
| `Unit` | `ListingUnit`?, stored as its name | "Match", "Hour", …; null when none fits |
| `DeliveryWindowSeconds` | long | from the presets (15m to 7d, plus 2m on devnet; listed by `GET /api/config`). Deposits aren't stored on listings; the program works them out per deal |
| `IsActive` | bool | paused = false |
| `CreatedAt` | DateTime | |

**Deal**
| Column | Type | Notes |
|---|---|---|
| `Id` | Guid (PK) | used in URLs |
| `OnchainDealId` | long | u64 stored as long (bit-cast) |
| `DealPda` | string (unique) | |
| `ListingId` | FK → Listing | the listing terms are copied into the deal on-chain at creation |
| `BuyerWallet`, `SellerWallet` | FK → User | |
| `BuyerNote` | string? | in-game name or instructions; visible to both parties only |
| `Status` | enum | `Draft` (off-chain only; becomes `Expired` if never funded), the on-chain open states `AwaitingSeller`, `Active`, `Delivered`, `Disputed`, then the final outcomes `Completed`, `Split`, `Undelivered`, `SellerRefunded`, `Cancelled` |
| `Price`, `Milestones`, `MilestonesReleased`, `BuyerDeposit`, `SellerDeposit` | | copied from the chain |
| `AcceptedAt`, `DeliveredAt` | DateTime? | copied from the chain |
| `DeliveryDeadline` | DateTime? | computed |
| `SplitOfferBy`, `SplitOfferBuyerAmount` | string?, long? | copied from the chain |
| `LastSignature` | string? | last transaction seen |
| `DeadlineReminderSent`, `RefundNoticeSent` | bool | so each deadline notification goes out once |
| `CreatedAt`, `SyncedAt` | DateTime, DateTime? | |

When a deal closes on-chain, its account disappears. The sync then sets the final status from the `DealClosed` event in the closing transaction's logs and keeps the row as history.

**Notification**: `Id`, `Wallet` (FK), `DealId` (FK?), `Kind` (enum), `CreatedAt`, `ReadAt?`.

**ActionLog**: `Id`, `Action` (`{Service}.{Method}`), `Entity`, `EntityId`, `Succeeded`, `Wallet?` (null for background jobs), `Details`, `CreatedAt`. Business outcomes written by services; failures always, data changes on success.

## Indexes
`Listing(GameId, IsActive)`, `Listing(SellerWallet)`, `Deal(BuyerWallet)`, `Deal(SellerWallet)`, `Deal(Status)` (for the background job), `Notification(Wallet, ReadAt)`.

## Reputation
Not stored in the database. It's read from each wallet's on-chain `Reputation` account, so nobody, including us, can fake a seller's record. It can be cached for a minute if that's needed.
