import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ToastStore } from './toast-store';

/** The page's single `role="status"` live region. Rendered once, in the shell. */
@Component({
  selector: 'app-toast',
  templateUrl: './toast.html',
  styleUrl: './toast.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Toast {
  protected readonly store = inject(ToastStore);
}
