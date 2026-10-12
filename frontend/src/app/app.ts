import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';
import { NotificationService } from './core/notification.service';
import { ToastService } from './core/toast.service';
import { WalletAddress } from './shared/wallet-address';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, WalletAddress],
  template: `
    <header class="top">
      <a routerLink="/" class="brand">Agora</a>
      <nav>
        <a routerLink="/sell" routerLinkActive="on">Sell</a>
        @if (auth.user(); as user) {
          <a routerLink="/me" routerLinkActive="on">
            My deals
            @if (notifications.unread() > 0) {
              <span class="count">{{ notifications.unread() }}</span>
            }
          </a>
          <app-wallet-address [wallet]="user.wallet" />
          <button type="button" class="ghost" (click)="auth.logout()">Sign out</button>
        } @else {
          <button type="button" (click)="auth.requestSignIn()">Sign in</button>
        }
      </nav>
    </header>

    <main><router-outlet /></main>

    @if (auth.dialogOpen()) {
      <div class="backdrop">
        <div class="dialog" role="dialog" aria-modal="true" aria-labelledby="signin-title">
          <h2 id="signin-title">Sign in with Phantom</h2>
          <p class="muted">
            Connect your wallet and sign a one-time message. This does not move any money.
          </p>
          <div class="actions">
            <button type="button" [disabled]="auth.busy()" (click)="auth.signIn()">
              {{ auth.busy() ? 'Waiting for wallet…' : 'Connect and sign in' }}
            </button>
            <button type="button" class="ghost" (click)="auth.closeDialog()">Cancel</button>
          </div>
        </div>
      </div>
    }

    <div class="toasts" aria-live="polite">
      @for (toast of toaster.toasts(); track toast.id) {
        <div class="toast" [class.error]="toast.kind === 'error'">
          {{ toast.text }}
          <button
            type="button"
            class="ghost"
            aria-label="Dismiss"
            (click)="toaster.dismiss(toast.id)"
          >
            ×
          </button>
        </div>
      }
    </div>
  `,
})
export class App {
  protected readonly auth = inject(AuthService);
  protected readonly notifications = inject(NotificationService);
  protected readonly toaster = inject(ToastService);
}
