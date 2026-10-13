import { ApiError } from './api.service';

/** Wallet failures that mean "this wallet has no test money" (Solana's own wording varies). */
const NO_FUNDS =
  /insufficient (funds|lamports)|custom program error: 0x1\b|attempt to debit an account but found no record/i;
/** What Phantom says when it simulates against the main network, or the simulation fails. */
const WRONG_NETWORK = /simulat|unexpected error|blockhash not found|failed to send transaction/i;

export const OFFLINE = "You're offline. Changes will show when you reconnect.";
export const SLOW_SYNC =
  "Solana is taking longer than usual. Refresh in a minute to see if it went through. Don't approve it again yet.";
export const SIGNED_OUT = "You've been signed out. Sign in again to continue.";
export const WALLET_SWITCHED =
  "You switched wallets in Phantom, so you've been signed out. Sign in with the new wallet.";
export const TOO_MANY = 'Too many requests. Wait a few seconds and try again.';
export const NO_SOLANA =
  "Agora can't reach Solana right now. Your money is safe where it is. Try again in a minute.";

/** One line saying what went wrong and what to do (DESIGN.md §7, Errors). */
export function errorText(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 0)
      return navigator.onLine ? "Can't reach Agora right now. Try again in a minute." : OFFLINE;
    return error.message;
  }
  const message = error instanceof Error ? error.message : String(error ?? '');
  if (NO_FUNDS.test(message))
    return 'Not enough test money in this wallet. You need USDC (test) and a little test SOL for the network fee. Ask the Agora team to fund your wallet.';
  if (WRONG_NETWORK.test(message))
    return 'Phantom is on the main network. Agora runs on the test network. In Phantom: Settings → Developer settings → Testnet mode, then try again.';
  return message || 'Something went wrong. Try again.';
}
