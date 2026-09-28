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

## Selected direction (structural/interaction thesis; visual world is the direction contract below)

- **Shell:** a top app bar spanning the full width — "DeveloperStore" wordmark, the "Main" nav (Sales / New sale / Account) inline on desktop, the signed-in user's name and "Sign out" at the trailing edge. Below ~640px the three nav links collapse into a single "Menu" disclosure button next to the wordmark; sign-out stays reachable without opening it if that fits, otherwise inside it. No sidebar — three destinations don't earn one, and a sidebar would fight the list's own width budget.
- **Sales list:** filters live in a `search`-landmark panel directly above the table, always visible (Sale number / Customer / Branch / Status / Sold from–to / Min–max total in one row-wrapping group), with "More filters" as inline progressive disclosure for the two ID fields immediately below that group — not a drawer or modal, so the applied filter state is always in view. On phones, the table's rows reflow into a stacked card per sale (sale number as the card's heading link, the rest as label/value pairs), keeping `role="table"/"row"/"columnheader"/"cell"` in markup exactly as the contract requires even though the visual layout is no longer tabular.
- **Sale editor:** each line is a self-contained card-like group ("Line {n}") stacked vertically — product select, quantity, unit price, then the computed "{p}% off"/"No discount" + tier hint, right-aligned money — rather than a dense multi-column grid, because the live-updating discount text needs room to read as a sentence, not a cramped cell. The Summary sits pinned below the last line (sticky on desktop only) with subtotal/discounts/total in that fixed order, always labeled "(preview)" until saved.
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
- Transitions: motion is a stamp/settle motion on state changes (see Direction contract), never decorative.

## Constraints and open decisions

- Platform: web, Angular 21 standalone components, zoneless, signals — fixed by Part 1.
- Accessibility: WCAG 2.2 AA plus the contract's exact names/roles/live-region choices — not renegotiable here.
- Localization: English copy, BRL/`en-US` money — fixed (Decision 12).
- Reusable components: `api-error-alert`, `toast`, `confirm-dialog`, `field`, `status-badge` (Task 13) are the only shared primitives; screens don't invent parallel ones.
- Resolved by the Direction contract below: color, type, spacing/density scale, radii, elevation, motion stance, and fonts.

## Direction contract

**Build path: code-led** (`.impeccable/config.json` → `buildPath: "code"`). No comp exists or will be produced; ambition lives in this contract's FIRST VIEWPORT block and named signature interaction, audited in behavior by the finish reviewer (Part 2, Task 21).

**Roll record.** `impeccable concept-seed --scope direction --mode operate`, seed key `232b6df2`. Grounded list (mine, ranked by resonance before the roll): 1. POS thermal receipt tape, 2. wholesale price-list/catalog sheet, 3. dot-matrix invoice/packing slip, 4. bank/accounting ledger card system, 5. paper delivery/order ledger book, 6. freight/logistics manifest board, 7. commodities-exchange price board. Assigned index: **4 — ledger card system**. Since the assignment isn't my top-ranked candidate (index 1), that candidate rides as IMPECCABLE'S PICK. User locked the assigned card.

**Challenger verdicts** (catalog challengers dealt against the assignment):
- *TDR info-noise sleeve* (hazard-brand density) — declined on both axes (too hostile/loud for Operate scanability). Raised into the assignment: **decisive, stamped-certainty marks** — no soft hover fades or wishy-washy transitional states; a state is stamped in or it isn't.
- *One-bit desktop* (dithered chrome, marching ants) — declined on both axes (toy/nostalgic register, no mapped interaction). Raised into the assignment: **every state is drawn as an explicit graphic**, never inferred from a color shift alone (dimmed = disabled, a stamped word = status), reinforcing PRODUCT.md's own "status by text and shape, never color alone."
- *Civic bureau prospectus* (soft SaaS gradient/ribbon) — declined on audience identification (this is the generic "AI slop" look Decision 3 exists to avoid); no discipline donated, since the ledger world's own restraint already covers what this card does well.
- *Broadcast teletext* (rigid character grid, REVEAL mechanic) — competitive, holds product clarity strongly (its REVEAL maps well onto "pricing preview until saved," and its grid discipline reinforces "dense but calm"); did not win outright since a beverage distributor's own back office is still closer to paper ledgers than broadcast terminals. Kept as a named full alternate; not merged in, to avoid diluting one world with two grammars.
- *Transforming drawcord cape*, *gravity-rain garden* — declined on both axes, unrelated domains, no donation.

---

**THESIS.** A sale is a ledger card, not a dashboard row: discount tiers are stamped rate codes read at a glance, never numbers you have to trust blind. Refuses the category default of a soft SaaS card grid with a colored status pill.

**OWN-WORLD.** Pale ruled card stock (warm off-white — `--surface: #F6F3EC`, not cream-magazine cliché), hairline rules in slate (`--rule: #C7C2B4`), ink near-black (`--ink: #23211D`), one stamp-red (`--stamp: #A13D2E`) reserved for void/destructive marks and nothing else. Color strategy: **Restrained** (neutrals + one accent), the Operate default, spent entirely on the stamp. **Light only** — a ledger card has no dark-mode analogue without breaking the metaphor, and PRODUCT.md never asked for one. Type: **Inter** for labels, body and controls (a workhorse Operate face, not a display face); **Courier Prime** for every tabular figure — money, quantities, dates, sale numbers, GUIDs — set with `font-variant-numeric: tabular-nums`, so the fixed-pitch "stamped" character is real, not simulated with letter-spacing hacks. 4px spacing base, tight but not cramped density; rectangular corners with a single 2px radius only on interactive controls (buttons, inputs) — a card has square corners, its controls don't need to pretend otherwise. Flat surfaces, no elevation/shadow system; a raised state is shown by a heavier rule, never a shadow.

**STORY.** Staff scan a card catalog (the sales list, read top-to-bottom like an index), open one card, watch its rate code stamp itself into the margin as they add lines, sign it (save), and can void a line or the whole card later — the void stays visible in place, struck but never erased, because a real ledger never tears a page out.

**FIRST VIEWPORT** (Sales list, desktop 1280px). A ruled index strip across the top holds the always-visible filters (Sale number / Customer / Branch / Status / date range / total range in one wrapping row), "More filters" as an inline disclosure beneath it. Below, the card-catalog table: sale number set in Courier Prime as the row's tab-edge (a left-ruled link), date/customer/branch in Inter, items as "{active} of {total}", the running total pinned to the fixed rightmost column in Courier Prime, right-aligned, and status as a small stamped word in a thin-ruled box (not a pill, not a color dot). Primary action "New sale" sits top-right of the index strip, the one filled control on the page. On phones (≤640px), each row becomes a stacked card: the sale-number tab-edge as its heading link, the rest as ruled label/value pairs, running total still pinned right within the card.

**Signature interaction.** Saving a sale (create, update, or any status change) plays a single "stamp" motion once: the changed field/badge scales from 1.04 to 1 over 120ms with a slight ink-darken, as if freshly inked, then settles — never repeated, never on hover, off entirely under `prefers-reduced-motion`. This is the only authored motion; everything else (toasts, dialogs) uses plain, fast (150–200ms) fades/translates with no bounce.

**FORM.** Candidate 4 of my own 7-direction ranked list, assigned by `concept-seed --scope direction --mode operate` (seed `232b6df2`), locked by the user over the pick card (receipt tape), the competitive challenger (teletext), and the standing exit (conventional admin UI).

**FINISH.** Unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance.
