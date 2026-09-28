import { Dialog } from '@angular/cdk/dialog';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { CONFIRM_DIALOG_KEEP_ID, CONFIRM_DIALOG_TITLE_ID, ConfirmDialogContent, ConfirmDialogOptions } from './confirm-dialog-content';

export type { ConfirmDialogOptions };

/**
 * Opens the one `role="alertdialog"` pattern every destructive action reuses (cancel item,
 * cancel sale, delete sale, delete account) — title, one line (or more) of consequence copy, and
 * a trailing safe/destructive button pair. Resolves `true` when the destructive action was
 * confirmed, `false` when the dialog was kept/dismissed.
 */
@Injectable({ providedIn: 'root' })
export class ConfirmDialog {
  private readonly dialog = inject(Dialog);

  open(options: ConfirmDialogOptions): Observable<boolean> {
    const ref = this.dialog.open<boolean, ConfirmDialogOptions, ConfirmDialogContent>(ConfirmDialogContent, {
      role: 'alertdialog',
      ariaLabelledBy: CONFIRM_DIALOG_TITLE_ID,
      autoFocus: `#${CONFIRM_DIALOG_KEEP_ID}`,
      restoreFocus: true,
      data: options,
    });
    return ref.closed.pipe(map(result => result === true));
  }
}
