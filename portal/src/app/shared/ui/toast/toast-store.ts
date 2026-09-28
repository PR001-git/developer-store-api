import { Injectable, signal } from '@angular/core';

export interface ToastMessage {
  readonly id: number;
  readonly message: string;
}

const AUTO_DISMISS_MS = 5_000;

/**
 * Success messages surface here and the shell's single `role="status"` outlet (Toast) renders
 * them. `show` auto-dismisses each message after 5s.
 */
@Injectable({ providedIn: 'root' })
export class ToastStore {
  private readonly state = signal<readonly ToastMessage[]>([]);
  private nextId = 0;

  readonly toasts = this.state.asReadonly();

  show(message: string): void {
    const id = ++this.nextId;
    this.state.update(toasts => [...toasts, { id, message }]);
    setTimeout(() => this.dismiss(id), AUTO_DISMISS_MS);
  }

  dismiss(id: number): void {
    this.state.update(toasts => toasts.filter(toast => toast.id !== id));
  }
}
