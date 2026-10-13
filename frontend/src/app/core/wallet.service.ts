import { Injectable, signal } from '@angular/core';
import { Transaction } from '@solana/web3.js';
import bs58 from 'bs58';

/** The slice of Phantom's injected provider (window.phantom.solana) that we use. */
interface PhantomProvider {
  isPhantom?: boolean;
  publicKey: { toString(): string } | null;
  connect(options?: { onlyIfTrusted?: boolean }): Promise<{ publicKey: { toString(): string } }>;
  disconnect(): Promise<void>;
  signMessage(message: Uint8Array, display?: 'utf8' | 'hex'): Promise<{ signature: Uint8Array }>;
  signAndSendTransaction(tx: Transaction): Promise<{ signature: string }>;
  on(
    event: 'accountChanged' | 'disconnect',
    handler: (key: { toString(): string } | null) => void,
  ): void;
}

export class WalletRejectedError extends Error {
  constructor() {
    super('Request rejected in the wallet');
  }
}

/** Wraps Phantom. Swap the internals for Phantom Connect later without touching callers. */
@Injectable({ providedIn: 'root' })
export class WalletService {
  /** Base58 address of the connected account, or null. */
  readonly publicKey = signal<string | null>(null);

  private provider: PhantomProvider | null = null;

  constructor() {
    this.provider =
      (window as unknown as { phantom?: { solana?: PhantomProvider } }).phantom?.solana ?? null;
    this.provider?.on('accountChanged', (key) => this.publicKey.set(key ? key.toString() : null));
    this.provider?.on('disconnect', () => this.publicKey.set(null));
  }

  /** Reconnects without a popup if the user already approved this site. */
  async restore(): Promise<void> {
    try {
      const res = await this.provider?.connect({ onlyIfTrusted: true });
      if (res) this.publicKey.set(res.publicKey.toString());
    } catch {
      // not trusted yet: the user connects from the sign-in dialog
    }
  }

  async connect(): Promise<string> {
    if (!this.provider) {
      if (this.isMobile()) {
        // Phone browsers have no extensions: reopen this page in the Phantom app's own browser,
        // which injects the same provider. Without the app installed, the link offers the download.
        const page = encodeURIComponent(window.location.href);
        const origin = encodeURIComponent(window.location.origin);
        window.location.href = `https://phantom.app/ul/browse/${page}?ref=${origin}`;
        throw new Error('Opening this page in the Phantom app');
      }
      window.open('https://phantom.app/download', '_blank');
      throw new Error('Install the Phantom wallet extension, then reload this page');
    }
    try {
      const res = await this.provider.connect();
      const key = res.publicKey.toString();
      this.publicKey.set(key);
      return key;
    } catch (error) {
      throw this.rejected(error) ? new WalletRejectedError() : error;
    }
  }

  async disconnect(): Promise<void> {
    await this.provider?.disconnect();
    this.publicKey.set(null);
  }

  /** Returns the signature as base58, ready for POST /api/auth/verify. */
  async signMessage(bytes: Uint8Array): Promise<string> {
    if (!this.provider) throw new Error('Wallet not connected');
    try {
      const { signature } = await this.provider.signMessage(bytes, 'utf8');
      return bs58.encode(signature);
    } catch (error) {
      throw this.rejected(error) ? new WalletRejectedError() : error;
    }
  }

  /** Signs with the user's wallet and sends. Returns the transaction signature (base58). */
  async signAndSendTransaction(tx: Transaction): Promise<string> {
    if (!this.provider) throw new Error('Wallet not connected');
    try {
      const { signature } = await this.provider.signAndSendTransaction(tx);
      return signature;
    } catch (error) {
      throw this.rejected(error) ? new WalletRejectedError() : error;
    }
  }

  /** iPadOS reports itself as a Mac, so a Mac with a touch screen counts too. */
  private isMobile(): boolean {
    const agent = navigator.userAgent;
    return (
      /Android|iPhone|iPad|iPod/i.test(agent) ||
      (/Macintosh/.test(agent) && navigator.maxTouchPoints > 1)
    );
  }

  private rejected(error: unknown): boolean {
    const e = error as { code?: number; message?: string };
    return e?.code === 4001 || /reject|denied|cancel/i.test(e?.message ?? '');
  }
}
