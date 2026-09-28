import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type SaleStatus = 'Open' | 'Cancelled';

/**
 * "Open"/"Cancelled" as text plus shape — never color alone (PRODUCT.md, the direction contract).
 * Plain text, not a `role="status"` region: the toast outlet is the page's only live region.
 */
@Component({
  selector: 'app-status-badge',
  templateUrl: './status-badge.html',
  styleUrl: './status-badge.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusBadge {
  readonly status = input.required<SaleStatus>();
}
