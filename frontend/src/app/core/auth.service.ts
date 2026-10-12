import { Injectable, effect, inject, signal } from '@angular/core';
import { ApiService } from './api.service';
import { FullUserDto } from './models';
import { ToastService } from './toast.service';
import { WalletRejectedError, WalletService } from './wallet.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiService);
  private readonly wallet = inject(WalletService);
  private readonly toast = inject(ToastService);

  readonly user = signal<FullUserDto | null>(null);
  readonly dialogOpen = signal(false);
  readonly busy = signal(false);

  private pending: ((signedIn: boolean) => void)[] = [];

  constructor() {
    // The session belongs to one wallet: if the wallet account changes, sign out.
    effect(() => {
      const key = this.wallet.publicKey();
      const user = this.user();
      if (user && key && key !== user.wallet) void this.logout();
    });
  }

  /** App start: is the cookie still valid? Also reconnects the wallet quietly. */
  async init(): Promise<void> {
    await this.wallet.restore();
    try {
      this.user.set(await this.api.get<FullUserDto>('/api/me'));
    } catch {
      this.user.set(null);
    }
  }

  /** Opens the sign-in dialog. Resolves true once signed in, false if the user closes it. */
  requestSignIn(): Promise<boolean> {
    if (this.user()) return Promise.resolve(true);
    this.dialogOpen.set(true);
    return new Promise(resolve => this.pending.push(resolve));
  }

  closeDialog(): void {
    this.dialogOpen.set(false);
    this.settle(false);
  }

   /** Connect, sign the server's message, verify. Called from the dialog's button. */
  async signIn(): Promise<void> {
    this.busy.set(true);
    try {
      const wallet = await this.wallet.connect();
      const { message } = await this.api.get<{ message: string }>('/api/auth/nonce', { wallet });  
      // const { message } = await this.api.get<{ message: string }>('/api/auth/nonce', {
      //   wallet,
      //   // Phantom only shows the request if the message's domain equals this page's host.
      //   domain: window.location.host,
      // });
      const signature = await this.wallet.signMessage(new TextEncoder().encode(message));
      const user = await this.api.post<FullUserDto>('/api/auth/verify', { wallet, message, signature });
      this.user.set(user);
      this.dialogOpen.set(false);
      this.settle(true);
    }  catch (error) {
  console.error('Sign-in failed:', error);
  if (!(error instanceof WalletRejectedError)) this.toast.fromError(error);
    } finally {
      this.busy.set(false);
    }
  }

  async logout(): Promise<void> {
    this.user.set(null);
    try {
      await this.api.post('/api/auth/logout');
    } catch {
      // cookie is cleared server-side on success; nothing else to do
    }
  }

  private settle(signedIn: boolean): void {
    const waiting = this.pending;
    this.pending = [];
    waiting.forEach(resolve => resolve(signedIn));
  }
}
