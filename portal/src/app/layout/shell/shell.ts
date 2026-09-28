import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthFlow } from '../../core/auth/auth-flow';
import { SessionStore } from '../../core/auth/session-store';
import { Toast } from '../../shared/ui/toast/toast';

/**
 * The signed-in app shell: the "Main" nav, the signed-in user's name, "Sign out", the toast
 * outlet, and the router outlet for every `authGuard`-protected screen.
 */
@Component({
  selector: 'app-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet, Toast],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Shell {
  private readonly sessionStore = inject(SessionStore);
  private readonly authFlow = inject(AuthFlow);
  private readonly router = inject(Router);

  protected readonly session = this.sessionStore.session;
  protected readonly menuOpen = signal(false);

  toggleMenu(): void {
    this.menuOpen.update(open => !open);
  }

  signOut(): void {
    this.authFlow.signOut();
    this.menuOpen.set(false);
    void this.router.navigate(['/sign-in']);
  }
}
