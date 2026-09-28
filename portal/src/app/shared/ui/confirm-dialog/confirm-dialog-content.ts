import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

export interface ConfirmDialogOptions {
  readonly title: string;
  /** One consequence line, or several (e.g. a base line plus a conditional warning). */
  readonly body: string | readonly string[];
  /** The destructive action's label, e.g. "Cancel item", "Delete sale". */
  readonly confirmLabel: string;
  /** The safe action's label, e.g. "Keep item", "Keep sale" — gets first focus. */
  readonly keepLabel: string;
}

export const CONFIRM_DIALOG_TITLE_ID = 'confirm-dialog-title';
export const CONFIRM_DIALOG_KEEP_ID = 'confirm-dialog-keep';

@Component({
  selector: 'app-confirm-dialog-content',
  templateUrl: './confirm-dialog-content.html',
  styleUrl: './confirm-dialog-content.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmDialogContent {
  protected readonly options = inject<ConfirmDialogOptions>(DIALOG_DATA);
  private readonly dialogRef = inject(DialogRef<boolean, ConfirmDialogContent>);

  protected readonly titleId = CONFIRM_DIALOG_TITLE_ID;
  protected readonly keepId = CONFIRM_DIALOG_KEEP_ID;
  protected readonly bodyLines = Array.isArray(this.options.body) ? this.options.body : [this.options.body];

  keep(): void {
    this.dialogRef.close(false);
  }

  confirm(): void {
    this.dialogRef.close(true);
  }
}
