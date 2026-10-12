# Authentication: Sign in with Solana

A user's wallet address is their account. They sign in by signing a one-time message with their wallet. No passwords or emails are stored, and no keys leave the wallet. Phantom Connect users who sign in with email get an embedded wallet, and the flow is the same. Endpoint details: [api.md](api.md#auth).

## Flow

```
Angular                          .NET API                         Wallet
  │ GET /api/auth/nonce?wallet=X   │ store message (5 min, one use)  │
  │◀──────────────── { message } ──│                                 │
  │ signMessage(UTF-8 message) ───────────────────────────────────▶│ user approves
  │◀──────────────────────────────────────── 64-byte signature ─────│
  │ POST /api/auth/verify { wallet, message, signature (base58) }    │
  │──────────────────────────────▶│ verify, create user if new      │
  │◀── Set-Cookie: agora_session ──│                                 │
```

## The message

Built by the backend (`Solana/WalletSignIn.cs`), so the frontend can't change it. `<domain>` is `Auth:Domain` in appsettings; it must match the page host or Phantom may warn (`localhost:4200` in Development, matching `ng serve`).

```
<domain> wants you to sign in with your Solana account:
<wallet address>

Sign in to Agora.

Nonce: <16 random bytes, hex>
Issued At: <ISO 8601 UTC>
Expiration Time: <issued + 5 min>
```

## Verification (backend)
1. Take the pending message for `wallet` and delete it at once, so it can't be replayed. It must exist and be unexpired.
2. The submitted message must match it byte for byte.
3. Base58-decode the signature (64 bytes) and verify it with ed25519 against the wallet's public key (Solnet's `PublicKey.Verify`).
4. Issue a JWT (`sub` = wallet, 7 days) in an `HttpOnly`, `Secure`, `SameSite=Lax` cookie named `agora_session`. ASP.NET's JWT bearer auth reads it from the cookie.

Every failure returns the same `401 "Invalid Or Expired Sign-In"`, so a caller can't tell which check failed.

## What auth does *not* protect
Money. A session only lets a user edit their own profile and listings and see their own deals. Every fund movement needs a wallet signature on an on-chain transaction, which the program checks on its own.
