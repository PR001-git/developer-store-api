import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { ApiError } from '../../../core/api/api-error';

/**
 * Renders an `ApiError`: title, detail, and an action button when one applies.
 *
 * - `NetworkError` always gets a "Try again" action.
 * - Any other type shows an action only when the caller supplies `actionLabel` (e.g. "Reload
 *   sale" for a `ConcurrencyConflict`) — the caller decides what "again" means for its screen.
 * - Every other API error is title + detail only, no action.
 */
@Component({
  selector: 'app-api-error-alert',
  templateUrl: './api-error-alert.html',
  styleUrl: './api-error-alert.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ApiErrorAlert {
  readonly error = input.required<ApiError>();
  /** The action button's label for anything other than a `NetworkError` (e.g. "Reload sale"). Omit to show no action. */
  readonly actionLabel = input<string | null>(null);

  readonly retry = output<void>();

  protected readonly isNetworkError = computed(() => this.error().type === 'NetworkError');
  protected readonly buttonLabel = computed(() => (this.isNetworkError() ? 'Try again' : this.actionLabel()));
}
