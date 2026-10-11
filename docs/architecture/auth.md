# Authentication: Sign in with Solana

A user's wallet address is their account. They sign in by signing a one-time message with their wallet. No passwords or emails are stored, and no keys ever leave the wallet.

Users without a wallet sign in with email through **Phantom Connect**, which creates a wallet for them. After that, the flow is identical.

## Flow

```
Angular                      .NET API                    Wallet
  │ GET /api/auth/nonce?wallet=X │                          │
  │─────────────────────────────▶│ store nonce (5 min TTL)  │
  │◀──────── { nonce, message } ─│                          │
  │ signMessage(message) ───────────────────────────────────▶│ user approves
  │◀──────────────────────────────────────── signature ──────│
  │ POST /api/auth/verify {wallet, message, signature}       │
  │─────────────────────────────▶│ check nonce, domain, expiry│
  │                              │ verify ed25519 signature  │
  │◀── Set-Cookie: session (JWT) │ upsert User               │
```

## The message
The backend builds the message, so the frontend can't change it:

```
agora.example wants you to sign in with your Solana account:
<wallet address>

Sign in to Agora.

Nonce: <random 16 bytes, hex>
Issued At: <ISO 8601 UTC>
Expiration Time: <issued + 5 min>
```

## Verification (backend)
1. Look up the nonce for `wallet`. It must exist, be unused and be unexpired. Delete it right away so it can't be replayed.
2. The message must be byte-for-byte the one issued.
3. Base58-decode the wallet (32-byte ed25519 public key) and the signature (64 bytes). Verify with ed25519. .NET has no built-in ed25519, so use `BouncyCastle.Cryptography` (`Ed25519.Verify`) and a small Base58 decoder.
4. Issue a JWT with `sub = wallet`, a 7-day expiry, in an `HttpOnly`, `Secure`, `SameSite=Lax` cookie. ASP.NET's JWT bearer auth reads it from the cookie.

## What auth does *not* protect
Money. A session only lets a user edit their own listings, profile and off-chain deal details. Every fund movement needs a wallet signature on an on-chain transaction, which the program checks on its own.

## To verify during implementation
- That the Phantom Connect SDK exposes `signMessage` for embedded (email) wallets, and the exact package names. See https://docs.phantom.com/phantom-connect.
