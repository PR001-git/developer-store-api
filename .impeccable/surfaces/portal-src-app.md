---
version: 1
slug: "portal-src-app"
primary_target: "portal/src/app"
related_targets: []
---

# Portal — shape brief

## Job and audience

A reviewer of this Ambev developer-evaluation submission arrives to exercise every Sales API feature and see its business rules (discount tiers, cancellation, soft delete) working and legible. In the product's own fiction, they're sales staff at a beverage distributor's branch, recording and correcting sales. Mode: **Operate** — task completion, scanability and consistency outrank expression.

## Outcome and proof

Primary task: create, find, and correct a sale, always able to see why a discount applied. Success is a reviewer completing every feature-matrix row without needing outside knowledge or fabricated data — the demo catalog and inline hints carry that weight. Proof lives in the live pricing preview (a computed discount tier shown before saving) and in the accessibility contract's explicit names/roles, which the e2e specs hold it to.

## Selected direction (structural/interaction thesis; visual world is Task 5's)

- **Shell:** a top app bar spanning the full width — "DeveloperStore" wordmark, the "Main" nav (Sales / New sale / Account) inline on desktop, the signed-in user's name and "Sign out" at the trailing edge. Below ~640px the three nav links collapse into a single "Menu" disclosure button next to the wordmark; sign-out stays reachable without opening it if that fits, otherwise inside it. No sidebar — three destinations don't earn one, and a sidebar would fight the list's own width budget.
- **Sales list:** filters live in a `search`-landmark panel directly above the table, always visible (Sale number / Customer / Branch / Status / Sold from–to / Min–max total in one row-wrapping group), with "More filters" as inline progressive disclosure for the two ID fields immediately below that group — not a drawer or modal, so the applied filter state is always in view. On phones, the table's rows reflow into a stacked card per sale (sale number as the card's heading link, the rest as label/value pairs), keeping `role="table"/"row"/"columnheader"/"cell"` in markup exactly as the contract requires even though the visual layout is no longer tabular.
- **Sale editor:** each line is a self-contained card-like group ("Line {n}") stacked vertically — product select, quantity, unit price, then the computed "{p}% off"/"No discount" + tier hint, right-aligned money — rather than a dense multi-column grid, because the live-updating discount text needs room to read as a sentence, not a cramped cell. The Summary sits pinned below the last line (sticky on desktop only, remains a mild latency for the phone reader's own good) with subtotal/discounts/total in that fixed order, always labeled "(preview)" until saved.
- **Sale detail:** one items table, not two — cancelled rows stay in place, visually de-emphasized (reduced-opacity text, "Cancelled" in the Status cell) rather than moved to a separate "history" table, so row order still matches the sale as entered. This matches Task 18's contract directly.
- **Confirmations:** every destructive action (cancel item, cancel sale, delete sale, delete account) opens the same `alertdialog` pattern — title, one line of consequence copy, a trailing pair of buttons with the safe action first, the destructive action visually secondary (never the default-styled button).
- **Feedback:** exactly one toast outlet (bottom-of-viewport, `role="status"`), one error-alert pattern reused everywhere an `ApiError` surfaces, both defined once in Task 13 and never rebuilt per screen.

## Scope and boundaries

- Fidelity: production code, not a mockup — every screen ships real Angular components wired to the real API, per Decision 14 (no comp; code-first).
- Breadth: exactly the nine screens/states in the accessibility contract. Nothing else (Decision 9: no dashboard, no roles UI).
- Untouched: the accessibility contract's names/roles are fixed inputs, not proposals — this brief does not rename anything in it. Route paths, request/response shapes and business logic (pricing, query mapping, error mapping) are Part 1's and are not revisited here.
- Anti-goal: no component library look, no decorative chrome that isn't load-bearing for scanning a dense table or reading a discount preview at a glance.

## States and ranges

- Sales list realistically holds 0–few hundred rows across pages of 10/20/50/100; a sale has 1–20 lines (contract cap), each 1–20 units.
- Every list/detail/editor screen needs loading (busy, previous content kept), empty (two variants: no data vs. no matches), and error (network vs. API error) states — all already behaviorally specified per screen in Tasks 15/17/18.
- The catalog's 8 customers / 5 branches / 8 products are the typical case; "Other …" free entry is the edge case every party-picker and product-select must support without extra friction.

## Interaction and layout

- Hierarchy: on every screen, the primary heading (h1) names the record or action; the primary button is always the rightmost/bottom-most affordance in its group, never floating separately.
- Topology: three top-level destinations (Sales, New sale, Account) plus the implicit detail/edit routes reached only from the list or a toast — no route is orphaned from a link.
- Responsiveness: single reflow breakpoint around 640px is enough (Decision: desktop-first, phone-usable) — no intermediate tablet-specific layout is required by the contract.
- Feedback timing: toasts auto-dismiss at 5s (already fixed in Task 13); inline field errors appear on submit, not on blur, to avoid nagging mid-entry.
- Transitions: none load-bearing for correctness; motion is Task 5's call within the "purposeful, not decorative" principle already in PRODUCT.md.

## Constraints and open decisions

- Platform: web, Angular 21 standalone components, zoneless, signals — fixed by Part 1.
- Accessibility: WCAG 2.2 AA plus the contract's exact names/roles/live-region choices — not renegotiable here.
- Localization: English copy, BRL/`en-US` money — fixed (Decision 12).
- Reusable components: `api-error-alert`, `toast`, `confirm-dialog`, `field`, `status-badge` (Task 13) are the only shared primitives; screens don't invent parallel ones.
- Left open for Task 5 (new-work): color, type, spacing/density scale, radii, elevation, motion stance, and fonts. This brief fixes structure and behavior only.
