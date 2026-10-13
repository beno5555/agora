import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';
import { NotificationService } from './core/notification.service';
import { ToastService } from './core/toast.service';
import { Icon } from './shared/icon';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Icon],
  styleUrl: './app.css',
  host: { '(document:keydown.escape)': 'closeOverlays()' },
  template: `
    <header class="site-header">
      <a routerLink="/" class="brand" aria-label="Agora, home">
        <img src="brand/lockup.png" alt="" height="32" />
      </a>
      <nav aria-label="Main">
        <a routerLink="/sell" routerLinkActive="is-active" class="nav-link only-desk">Sell</a>
        @if (auth.user(); as user) {
          <a routerLink="/me" routerLinkActive="is-active" class="nav-link">
            My deals
            @if (notifications.unread() > 0) {
              <span class="count"
                >{{ notifications.unread()
                }}<span class="visually-hidden"> unread</span></span
              >
            }
          </a>
          <div class="menu-wrap">
            <button
              type="button"
              class="btn btn--secondary btn--sm menu-trigger"
              aria-haspopup="menu"
              [attr.aria-expanded]="menuOpen()"
              aria-label="Account menu"
              (click)="menuOpen.set(!menuOpen())"
            >
              <app-icon name="wallet" class="only-desk" />
              <span class="addr only-desk">{{ short(user.wallet) }}</span>
              <app-icon name="chevron-down" class="only-desk" />
              <app-icon name="menu" size="20" class="only-phone" />
            </button>
            @if (menuOpen()) {
              <div class="menu-scrim" (click)="menuOpen.set(false)"></div>
              <div class="menu" role="menu">
                <a
                  role="menuitem"
                  routerLink="/sell"
                  class="menu-item only-phone"
                  (click)="menuOpen.set(false)"
                  >Sell something</a
                >
                <a
                  role="menuitem"
                  [routerLink]="['/u', user.wallet]"
                  class="menu-item"
                  (click)="menuOpen.set(false)"
                  >Public profile</a
                >
                <button type="button" role="menuitem" class="menu-item" (click)="copyAddress(user.wallet)">
                  <app-icon name="copy" />Copy address
                  <span class="addr muted small">{{ short(user.wallet) }}</span>
                </button>
                <button type="button" role="menuitem" class="menu-item" (click)="signOut()">
                  Sign out
                </button>
              </div>
            }
          </div>
        } @else {
          <button type="button" class="btn btn--primary btn--sm" (click)="auth.requestSignIn()">
            <span class="only-desk">Connect Phantom</span>
            <span class="only-phone">Connect</span>
          </button>
        }
      </nav>
    </header>
    <div class="test-band">
      <span class="only-desk">Test network. All money here is test USDC.</span>
      <span class="only-phone">Test network. Test USDC only.</span>
    </div>

    <main><router-outlet /></main>

    @if (auth.dialogOpen()) {
      <div class="backdrop">
        <div class="dialog" role="dialog" aria-modal="true" aria-labelledby="signin-title">
          <h2 id="signin-title">Sign in with Phantom</h2>
          <p class="muted">
            Connect your wallet and sign a one-time message. This does not move any money.
          </p>
          <div class="actions">
            <button
              type="button"
              class="btn btn--primary"
              [class.is-loading]="auth.busy()"
              [disabled]="auth.busy()"
              (click)="auth.signIn()"
            >
              {{ auth.busy() ? 'Waiting for Phantom…' : 'Connect and sign in' }}
            </button>
            <button type="button" class="btn btn--ghost" (click)="auth.closeDialog()">Cancel</button>
          </div>
        </div>
      </div>
    }

    <div class="toasts" role="status" aria-live="polite">
      @for (toast of toaster.toasts(); track toast.id) {
        <div class="toast" [class]="'toast toast--' + toast.kind" [attr.role]="toast.kind === 'error' ? 'alert' : null">
          <app-icon [name]="toast.kind === 'error' ? 'alert-triangle' : toast.kind === 'success' ? 'check' : 'info'" size="20" />
          <span class="toast-text">{{ toast.text }}</span>
          <button type="button" class="toast-close" aria-label="Dismiss" (click)="toaster.dismiss(toast.id)">
            <app-icon name="x" size="18" />
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
  protected readonly menuOpen = signal(false);

  protected short(wallet: string): string {
    return `${wallet.slice(0, 4)}…${wallet.slice(-4)}`;
  }

  protected async copyAddress(wallet: string): Promise<void> {
    this.menuOpen.set(false);
    await navigator.clipboard.writeText(wallet);
    this.toaster.show('Address copied', 'success');
  }

  protected signOut(): void {
    this.menuOpen.set(false);
    void this.auth.logout();
  }

  protected closeOverlays(): void {
    this.menuOpen.set(false);
    if (this.auth.dialogOpen()) this.auth.closeDialog();
  }
}
