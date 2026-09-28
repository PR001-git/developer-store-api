import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ValidationErrors } from '@angular/forms';
import { describeFieldError } from '../../forms/field-errors';

/**
 * A label, control slot, description and error text wired with `aria-describedby`.
 *
 * The control itself is projected (`<ng-content />`) so this stays usable with any native
 * control (`<input>`, `<select>`, a radio group, …). Because the control lives in the caller's
 * template, the caller wires `id`, `aria-describedby` and `aria-invalid` on it directly, reading
 * `controlId()` / `describedBy()` off a template reference variable on this component, e.g.:
 *
 * ```html
 * <app-field #f controlId="email" label="Email">
 *   <input id="email" [attr.aria-describedby]="f.describedBy()" formControlName="email" />
 * </app-field>
 * ```
 */
@Component({
  selector: 'app-field',
  templateUrl: './field.html',
  styleUrl: './field.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Field {
  readonly controlId = input.required<string>();
  readonly label = input.required<string>();
  readonly description = input<string | null>(null);
  /** The control's current validation errors, e.g. `control.errors` — only shown once the field has been submitted. */
  readonly errors = input<ValidationErrors | null>(null);

  readonly descriptionId = computed(() => `${this.controlId()}-description`);
  readonly errorId = computed(() => `${this.controlId()}-error`);
  readonly errorMessage = computed(() => describeFieldError(this.errors(), this.label()));

  /** The `aria-describedby` value the caller wires onto the projected control. */
  readonly describedBy = computed(() => {
    const ids = [
      this.description() !== null ? this.descriptionId() : null,
      this.errorMessage() !== null ? this.errorId() : null,
    ].filter((id): id is string => id !== null);
    return ids.length > 0 ? ids.join(' ') : null;
  });
}
