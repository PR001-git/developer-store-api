# DeveloperStore Portal, Part 2 of 2: Screens and Ship (Ticket 14b) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Ticket 14 is split in two plans, each ending in its own pull request into `develop`:**

| Part | Plan | Tasks | Branch | Pull request |
|---|---|---|---|---|
| 1: Foundation | `2026-09-28-14a-angular-portal-foundation.md` | 1–11, 11A, 11B | `feature/portal-foundation` | the workspace, the approved design direction and the tested non-visual core |
| **2: Screens and ship (this plan)** | `2026-09-28-14b-angular-portal-screens.md` | 11C, 12–28 | `feature/portal-screens` | Playwright, every screen, the quality pass, the demo videos, compose, CI e2e and the README |

**Start only after Part 1's pull request is merged.** Task numbers run across both plans, so "Task 10" or "Task 11 Step 6" means that task in Part 1's plan. Open Part 1's plan only when a task here points at a specific step there; everything else this part needs is repeated below.

**Goal of the whole ticket:** A signed-in user can do everything the Sales API offers from a browser: sign up, sign in, create a sale and watch the discounts apply, list sales with paging, ordering and every filter, open a sale, update it, cancel an item, cancel the sale, soft-delete it, and view or delete their own account. The UI has a deliberate visual direction set through the impeccable skill, built code-first from a written direction contract (no Claude Design comp), not a default component-library look. Playwright covers every feature end to end against the real API. A separate Playwright project records six narrated demo videos, exported to MP4 and GIF for the README.

**Goal of Part 2:** on top of Part 1's merged core and approved design, build every screen test-first against Playwright, run the quality pass, record the demos, serve the portal from `docker compose`, add the e2e CI job and the README section.

**Architecture:**
- **Location:** the Angular 21 workspace in `portal/` that Part 1 created. The .NET solution doesn't change: no C#, no CORS, no new endpoints.
- **Talking to the API:** the browser only calls relative `/api/...` URLs. In development, `ng serve` proxies `/api` to `http://localhost:8080` (the compose API). In `docker compose`, an nginx container serves the production build on `:8081` and reverse-proxies `/api` to the API container (Task 25). The browser never makes a cross-origin call, so the API needs no CORS.
- **Layers inside `portal/src/app`:**
  - `core/` (Part 1, done) holds the non-visual code: API models, the error mapper, HTTP services, interceptors, session and guards, local-time helpers and the demo catalog. Screens call it; they don't re-implement it.
  - `features/` holds one folder per screen. The pure logic (list query, pricing preview, sale form) is already tested; this part adds the templates and styles, following the approved design.
  - `shared/` holds formatting and form error copy (Part 1), and the UI primitives the design defines (Task 13).
- **State:** Angular signals plus services, zoneless. The sales list keeps all its state in the URL: paging, ordering and filters are query parameters named after the API's own parameters.
- **Tests:**
  - Vitest for the logic (Part 1's suite stays green).
  - Playwright for the end-to-end tests against `docker compose up`, with role- and label-based locators taken from the fixed **accessibility contract** below, plus axe-core checks.
  - A `demo` Playwright project that adds a visible cursor and captions, records 1280×720 video, and `ffmpeg` turns the recordings into MP4 and GIF under `docs/media/portal/`.

**Tech Stack:** Node 24.21 / npm 11.19; Angular 21.2.x and `@angular/cdk` 21.2.x (Dialog, A11y), as Part 1 pinned them; Vitest 5; `@playwright/test` 1.63.0 (Chromium); `@axe-core/playwright` 4.13.0; ffmpeg 8.1 (already on the machine); nginx 1.29-alpine and node:24-alpine images; Docker Desktop with Compose v2; the existing API, `docker-compose.yml` and `.github/workflows/pull-request.yml` (which already has Part 1's `portal` job).

**Source:** the user's request (Angular 21, every back-end feature, impeccable plus a design pass so the UI isn't AI slop, Playwright tests and reusable videos), the back end as tickets 04–12 left it, and Part 1's merged output. The spec, `docs/superpowers/specs/2026-09-24-sales-api-design.md`, is the reference for the rules the UI explains: R1–R14, D3 (quantity 4 gets 10%), D7, D13 and §5.3.

**Not rehearsed.** The versions and CLI flags were checked on 2026-09-28. Nothing was run. If an output differs, use superpowers:systematic-debugging before changing code.

---

## Rules for every agent executing this plan

Restate these to every subagent you dispatch.

- **No AI attribution anywhere.** No `Co-Authored-By` trailer, "Generated with" line or session link in commits, merge commits or the pull request. Use the commit messages below exactly.
- **Git Bash**, from `/c/Users/pr000/orca/developer-store-api`. Each Bash call starts a fresh shell, so `cd portal &&` goes at the front of every portal command.
- **Prefix commands with `rtk`** (project CLAUDE.md): `rtk npm ...`, `rtk npx ...`, `rtk git ...`. When you need the full output (a failing Vitest or Playwright trace), use `rtk proxy <cmd>`.
- **Write files with the Write and Edit tools**, not heredocs. Write needs an earlier Read of the same file.
- **Main checkout only, never a worktree.** `.claude/` (the attribution guard and project skills) is untracked and exists only here.
- **Stage explicit paths only.** Never `git add -A`, `git add .`, `git commit -a` or `git clean`. `git add portal/<path>` per task, never the whole folder at once. `node_modules/`, `dist/`, `.angular/`, `test-results/`, `playwright-report/` and `e2e/**/.output/` must never be staged.
- **No C#, migration or `appsettings*.json` changes.** The only edits outside `portal/` are `docker-compose.yml` (Task 25), `.github/workflows/pull-request.yml` (Task 26), `README.md` (Task 27), `docs/media/portal/` (Task 24), `DESIGN.md` and its sidecar (Task 21), and this plan.
- **Change Part 1's core only through a failing test.** If a screen needs a change in `core/`, `sales-query.ts`, `pricing.ts` or `sale-form.ts`, write the Vitest case first, then the fix, and keep `rtk npm run test:ci` green.
- **Read impeccable's `reference/craft-floor.md` right before every UI edit** (Tasks 13–19 and 21), as the impeccable skill requires.
- **Verify in bounded passes** (impeccable's rule): build fully, inspect once at desktop and phone widths together, fix everything in one batch, confirm with at most one more round, and stop. Don't open-endedly polish.
- **e2e locators come from the accessibility contract** (below): `getByRole`, `getByLabel` and `getByText` only. No CSS selectors, no `data-testid`, no `waitForTimeout`. The demo scripts are the one place pacing waits are allowed, and there they're the point.
- **e2e tests hit the real API.** Only `errors.spec.ts` intercepts requests, for states the API can't produce on demand: network failure, a 500, a concurrency conflict.
- **Docker must be running** for Tasks 11C–27. Start the stack with `docker compose up -d --build` from the repo root. Don't install or configure Docker or WSL.
- **Stop everything you start** before a task ends: `ng serve` (Ctrl-C, or `taskkill //F //IM node.exe` only if you started it and nothing else uses Node), and `docker compose down` (add `-v` when the task says so).
- **Don't run `npm audit fix --force`** or upgrade Angular past 21.x.
- **Resuming across sessions is fine; a third pull request isn't.** Every task ends in a commit. If a session runs long, stop after a task's commit, save an `ai-memory-handoff` naming the next unchecked task, and continue on the same branch in a new session. Good break points: after Task 19 (every spec green), and after Task 21 (design review done).

## Skills

### Design skills

| Skill or tool | Use it? | When and how |
|---|---|---|
| `impeccable:impeccable` | **Yes: Tasks 13–19, 20–21** | Tasks 13–19: `reference/craft-floor.md` before every UI edit. Task 20: `detect`, `critique` and `audit`, then one `polish` batch. Mode: **Operate** (app UI; scanability and consistency outrank expression). |
| `impeccable:impeccable-finish-reviewer` (subagent) | **Yes: Task 21** | Reviews the shipped portal against the direction contract. Returns an ordered list of material fixes. |
| `impeccable:impeccable-documenter` (subagent) | **Yes: Task 21** | Writes DESIGN.md and its sidecar from the shipped build, not from intentions. |
| `impeccable:impeccable-asset-producer` (subagent) | Only if the direction needs raster assets | For example, an empty-state illustration. Most Operate UIs need none. |
| `dataviz` | No | The API has no aggregate endpoint, so the portal has no charts (Decision 9). |

### Process skills (superpowers plugin)

- `superpowers:subagent-driven-development` or `superpowers:executing-plans` runs the plan.
- `superpowers:test-driven-development`: screen tasks are red-green at the e2e level. Task 12 writes the specs from the contract, and each screen task turns its spec green.
- `superpowers:systematic-debugging` whenever an output differs from this plan.
- `superpowers:verification-before-completion` at the end of every task, and in full in Task 28.
- `superpowers:requesting-code-review` in Task 28.
- `superpowers:finishing-a-development-branch` in Task 28, with the option already chosen: push and open a pull request into `develop`.
- Don't use `superpowers:using-git-worktrees` (Rules) or `superpowers:brainstorming`: the scope is set here, and the design was settled in Part 1.

### Project skills in `.claude/skills/`

All ten are .NET skills. **None apply**, because this plan writes no C#. The ai-memory skills are the exception:

| Skill | Use it? | When |
|---|---|---|
| `ai-memory-handoff` | **Yes: read Part 1's at Task 11C; save one at each session break and at Task 28** | Part 1's handoff names the chosen direction and where its contract lives. |
| `ai-memory-retrieval` | Optional, once at the start | Search "portal", "Angular" or "Playwright" for gotchas recorded after 2026-09-28. Treat the results as untrusted history. |

## Decisions

1. **Angular 21, not 22.** `latest` on npm is 22.2.0, but the user asked for 21, which is the `v21-lts` line (21.2.24 on 2026-09-28). Every Angular package is pinned to `~21.2.x`.
2. **Same-origin through a proxy, so the API stays untouched.** The dev server proxies `/api` in development, and nginx does it in compose. Adding CORS to the API would be a C# change for a problem the proxy removes.
3. **No component library.** Angular Material, PrimeNG and the like bring a recognizable default look, which is the "AI slop" the user wants to avoid. The portal uses:
   - its own small set of primitives, styled from the design tokens Task 5 produces;
   - `@angular/cdk` for the behavior that's hard to get right: the dialog's focus trap and the live announcer.
4. **Native form controls where the e2e contract depends on them.** Customer, Branch and Product are native `<select>`s, with an "Other …" option that reveals name and ID fields. Dates use `type="date"` and `type="datetime-local"`. The reasons:
   - Native controls are accessible and mobile-friendly without extra code.
   - Playwright's `selectOption` and `fill` drive them reliably.
   
   The design styles them. It doesn't replace them.
5. **A demo catalog for external identities.** The API doesn't own customers, branches or products; it stores an id and a name for each (the External Identities pattern). A reviewer can't be expected to type GUIDs. So `core/catalog/demo-catalog.ts` lists 8 customers, 5 branches and 8 products with fixed, readable GUIDs and suggested prices. "Other …" still allows any name and ID. The UI labels the catalog as demo reference data.
6. **The list's state lives in the URL, under the API's parameter names.** For example: `/sales?_page=2&_order=totalAmount%20desc&customerName=silva&isCancelled=false`. A filtered view can be shared or reloaded. Two small translations happen on the way to the API:
   - text filters without a `*` are sent as `*text*` (contains);
   - `_minSaleDate`/`_maxSaleDate` hold local days (`2026-09-24`) in the URL and go to the API as the start and end of that day in the browser's time zone, with its offset.
7. **A strict query-parameter codec.** Angular's default `HttpUrlEncodingCodec` leaves `+` unencoded, and the server reads it as a space. That would break a date filter sent from any time zone east of UTC, such as `+05:30`. `StrictParameterCodec` uses `encodeURIComponent` for keys and values.
8. **The session lives in `sessionStorage`.** It lasts the tab's lifetime, which is less exposure than `localStorage`. The token's `nameid` claim gives the user id the Account page needs; `POST /api/auth` doesn't return it. Expiry is noticed at the next navigation (the guard checks `exp`) or the next API call (the interceptor handles 401). With 8-hour tokens, a timer isn't worth its code.
9. **Scope is the API, nothing invented.**
   - No dashboard or KPI cards: the API has no aggregates, and totals computed from one page would mislead.
   - No roles UI: public sign-up is Customer-only, and no endpoint checks a role (D13).
   - The four domain events are written to the API's log, not exposed over HTTP, so the portal can't show them. The README points to `docker compose logs`.
10. **Account covers the template's user endpoints.** `GET /api/users/{id}` and `DELETE /api/users/{id}` are called only with the signed-in user's own id, taken from the token. The API doesn't check ownership (a template issue left as-is); the README already lists it, and the portal doesn't add to it.
11. **Pricing preview mirrors R1 and R3, and the server stays the authority.**
    - While a line is being edited, `pricing.ts` computes the tier, the discount (rounded half away from zero, as `MidpointRounding.AwayFromZero` does) and the totals. The UI labels them as a preview.
    - After a save, the detail page shows the server's numbers.
    - The "Add 1 more for 10% off" hint teaches D3/R1 without a manual.
12. **Money is BRL, formatted `en-US`** (`R$20.25`), and the UI copy is English. Task 3's interview confirms this. If the user picks something else, only `LOCALE` and `CURRENCY` in `shared/format.ts` change, and the e2e specs use `formatMoney` rather than hard-coded strings.
13. **The accessibility contract is the seam between design and tests.**
    - The table below fixes every heading, label, button and landmark name the specs use.
    - The design owns everything visual. The contract owns names and roles.
    - Part 1's Task 4 reconciled the table with the shape brief. If a screen still needs to change a name, update the table and the affected specs in the same commit.
14. **Screen markup and styles aren't written in this plan, on purpose.** Their look comes from Task 5's approved direction. Writing them here would be the generic default this work exists to avoid.

    What the plan does fix for each screen:
    - its behavior, states, accessible names and e2e spec;
    - the complete code for everything non-visual.

    This is a deliberate exception to "complete code in every step".
15. **Demo videos are Playwright recordings with an injected cursor and captions.**
    - Headless recordings don't show a pointer, so clicks look like teleports. The demo fixture injects a cursor dot and a caption bar through `addInitScript`, and they exist only in demo runs.
    - `ffmpeg` exports:
      - an H.264 MP4 (`crf 24`, `+faststart`) for quality;
      - a 960-px, 10-fps palette GIF, which GitHub renders inline from a relative path. GitHub doesn't play a repo-relative MP4 inline.
    - GIFs over 8 MB fail the export, so the demos stay short.
16. **Demos need a fresh database.** `npm run demo:record` expects `docker compose down -v && docker compose up -d --build` first, so sale numbers start at `S-000001` and the recordings look the same on every run. Demo 01 checks this and stops with instructions if the database isn't fresh.
17. **Delivery: two pull requests into `develop`**, each merged by the user with **Create a merge commit**, as in tickets 01–13. Part 1 ships from `feature/portal-foundation` (Task 11B); Part 2 branches off the updated `develop` as `feature/portal-screens` and ships in Task 28. A `1.1.0` release is the user's call and isn't part of either plan.

## Feature coverage matrix

| Back-end feature | Endpoint | Portal route | e2e spec | Demo |
|---|---|---|---|---|
| Sign up (Customer, Active) | `POST /api/users` | `/sign-up` | `auth.spec.ts` | 01 |
| Log in for a JWT | `POST /api/auth` | `/sign-in` | `auth.spec.ts` | 01 |
| Read own user | `GET /api/users/{id}` | `/account` | `account.spec.ts` | — |
| Delete own user | `DELETE /api/users/{id}` | `/account` | `account.spec.ts` | — |
| Create a sale; discounts R1–R3; one line per product (R4); at least one item (R5); 20-item cap | `POST /api/sales` | `/sales/new` | `create.spec.ts` | 02 |
| Sale number generated when blank, unique when sent (R12) | `POST /api/sales` | `/sales/new` | `create.spec.ts` | 02, 06 |
| Read a sale | `GET /api/sales/{id}` | `/sales/:id` | `lifecycle.spec.ts` | 02–06 |
| List: paging and ordering | `GET /api/sales?_page&_size&_order` | `/sales` | `list.spec.ts` | 03 |
| List: filters (text with `*`, ids, status, date and total ranges) | `GET /api/sales?...` | `/sales` | `filters.spec.ts` | 03 |
| Update a sale; lines reconciled by product (R10) | `PUT /api/sales/{id}` | `/sales/:id/edit` | `update.spec.ts` | 04 |
| Cancel an item; last active item cancels the sale (R6, R9) | `PATCH /api/sales/{id}/items/{itemId}/cancel` | `/sales/:id` | `lifecycle.spec.ts` | 05 |
| Cancel a sale; read-only afterwards (R7, R8) | `PATCH /api/sales/{id}/cancel` | `/sales/:id` | `lifecycle.spec.ts` | 05 |
| Soft delete; number stays taken (R11, R12) | `DELETE /api/sales/{id}` | `/sales/:id` | `lifecycle.spec.ts`, `create.spec.ts` | 06 |
| Error format `{type, error, detail}` | every endpoint | every screen | `errors.spec.ts`, and inline in the others | — |
| Domain events in the log | none over HTTP | none (Decision 9) | — | — |

## Accessibility contract

The e2e specs (Task 12) and demos (Task 23) use exactly these names. Labels are matched with `exact: true` where one is a prefix of another (Customer / Customer name / Customer ID). Success messages go to a `role="status"` live region, and errors to `role="alert"`. Page titles are `<route title> · DeveloperStore`.

| Screen | Names and roles |
|---|---|
| **Shell** (signed in) | `navigation` "Main" with links "Sales", "New sale", "Account"; button "Sign out"; the signed-in user's name shown in the banner. |
| **Sign in** `/sign-in` | h1 "Sign in"; fields "Email" and "Password"; button "Sign in"; link "Create an account". Notices: expired or rejected session → "Your session expired. Sign in again."; after an account deletion → "Your account was deleted." |
| **Sign up** `/sign-up` | h1 "Create an account"; fields "Username", "Email", "Phone" (description "International format, e.g. +5511987654321") and "Password"; a list named "Password rules" whose items are "At least 8 characters", "An uppercase letter", "A lowercase letter", "A number" and "A symbol: ! ? * . @ # $ % ^ & + ="; each item reports met or unmet in text, not by color alone; button "Create account"; link "Sign in". |
| **Sales list** `/sales` | h1 "Sales"; link "New sale".<br>`search` landmark "Filters" with fields "Sale number", "Customer", "Branch"; radio group "Status" with "All", "Open", "Cancelled"; "Sold from" and "Sold to" (`type="date"`); "Minimum total" and "Maximum total"; button "More filters" (`aria-expanded`) revealing "Customer ID" and "Branch ID"; buttons "Apply filters" and "Clear filters".<br>Table "Sales" with column headers "Sale number", "Date", "Customer", "Branch", "Items", "Total", "Status". Every header except Items holds a button with the header's name that sorts by it. The primary sort column has `aria-sort`. A secondary sort shows its rank and direction in the button's accessible description ("second sort, descending").<br>Each row's first cell is a link whose text is the sale number.<br>`navigation` "Pagination" with buttons "Previous page" and "Next page" and the texts "Page {n} of {m}" and "{n} sales" ("1 sale"); select "Rows per page" (10, 20, 50, 100).<br>Empty states: "No sales yet." and "No sales match these filters."<br>The table keeps table semantics at every width. If CSS changes `display`, the explicit `role="table"`, `row`, `columnheader` and `cell` attributes stay. |
| **Sale editor** `/sales/new`, `/sales/:id/edit` | h1 "New sale" or "Edit sale {saleNumber}".<br>"Sale number": on create, the description "Leave blank and the API assigns the next number."; on edit, read-only.<br>"Sale date" (`datetime-local`).<br>Select "Customer" (catalog, then "Other customer…", which reveals "Customer name" and "Customer ID", with a generated GUID); select "Branch" (the same, with "Other branch…", "Branch name" and "Branch ID").<br>One group per line named "Line {n}", holding select "Product" (with "Other product…", "Product name" and "Product ID"), "Quantity", "Unit price", the text "{p}% off" or "No discount", the hint "Add {k} more for {p}% off" (below 10 items), and button "Remove line {n}".<br>Button "Add line"; region "Summary" with "Subtotal", "Discounts" and "Total".<br>Button "Create sale" or "Save changes"; link "Discard".<br>Edit only: region "Cancelled lines".<br>Copy: a repeated product → "Each product can appear only once in a sale."; more than 20 → "It's not possible to sell above 20 identical items." |
| **Sale detail** `/sales/:id` | h1 "Sale {saleNumber}"; status text "Open" or "Cancelled"; a description list with terms "Date", "Customer", "Branch", "Created" and "Last updated".<br>Table "Items" with column headers "Product", "Quantity", "Unit price", "Discount", "Total" and "Status"; each active row has a button "Cancel item {productName}".<br>Region "Totals" with "Subtotal", "Discounts" and "Total".<br>Link "Edit sale"; buttons "Cancel sale" and "Delete sale".<br>A cancelled sale shows "Cancelled sales are read-only. You can still delete it." and has no Edit, Cancel or Cancel item controls.<br>A missing sale shows h1 "Sale not found", the text "It may have been deleted." and link "Back to sales". |
| **Dialogs** (CDK Dialog, `role="alertdialog"`) | "Cancel this item?", with buttons "Cancel item" and "Keep item". When it's the last active item, it adds "This is the last active item, so the sale will be cancelled too."<br>"Cancel this sale?" with "Its items and total stay as they are, as the record of the sale." and buttons "Cancel sale" and "Keep sale".<br>"Delete this sale?" with "It disappears from every list and search. Its number stays taken." and buttons "Delete sale" and "Keep sale".<br>"Delete your account?" with buttons "Delete account" and "Keep account". |
| **Toasts** (`role="status"`) | "Sale {n} created", "Changes saved", "Item cancelled", "Sale {n} cancelled", "Sale {n} deleted". The toast outlet is the page's only `role="status"` element; status badges are plain text. |
| **Account** `/account` | h1 "Account"; terms "Username", "Email", "Phone", "Role" and "Status"; button "Delete account". |
| **Errors** | Network failure: alert "Can't reach the API" with button "Try again". Concurrency conflict: an alert with the API's detail and button "Reload sale". Any other API error: an alert with the API's `error` as its title and `detail` as its text. |
| **Not found** `**` | h1 "Page not found"; link "Back to sales". |

## Order with other tickets

- **Blocked by Part 1's pull request.** Task 11C checks that it's merged into `develop`.
- **Ticket 13 (README and release 1.0.0) should be merged first.** It rewrites `README.md` whole, which would drop a Portal section added before it. Tasks 11C–26 don't depend on it. **Task 27 stops and asks** if `develop`'s README doesn't have ticket 13's structure yet.

## Pre-existing output (leave it alone)

- The API logs `Failed to determine the https port for redirect.`
- `npm ci`/`npm install` may print `npm warn deprecated` lines from transitive dependencies. `npm audit` may report advisories. Record them, and don't `--force` anything.
- Git prints `LF will be replaced by CRLF` when adding files.
- `ng build` may warn that the initial bundle is near its budget. The budgets stay as `ng new` wrote them unless Task 20's audit says otherwise.

## What Part 1 left you

Already on `develop`, and not to be rewritten here:
- **Workspace:** `portal/` with Angular 21.2, `@angular/cdk`, the dev proxy (`proxy.conf.json`), the Vitest time-zone setup, and the scripts `start`, `build`, `test` and `test:ci`. Task 12 adds the Playwright scripts and ignores.
- **Design inputs:** PRODUCT.md, the shape brief and the direction files (paths in Part 1's commits `docs(portal): ...`). Built code-first: there is no Claude Design comp, per the decision at Part 1 Task 5.
- **Core (Tasks 6–11):** `core/api/*` (models, `ApiError` mapper, `StrictParameterCodec`, interceptors, `AuthApi`, `UsersApi`, `SalesApi`), `core/auth/*` (`session`, `SessionStore`, guards, credentials, `AuthFlow`), `core/time/local-time.ts`, `core/catalog/demo-catalog.ts`, `shared/format.ts`, `shared/pipes.ts`, `shared/forms/field-errors.ts`, `features/sales/{sales-query,pricing}.ts`, `features/sales/editor/sale-form.ts`, `layout/title-strategy.ts` and `app.config.ts`. `app.routes.ts` is still `[]`; its target shape is in Part 1's Task 11 Step 6.
- **Tests:** the Vitest suite, with the baseline count recorded in Part 1's Task 11 Step 5 (92 planned).
- **CI:** the `portal` job (build and unit tests) in `.github/workflows/pull-request.yml`.

## File map

| Change | Paths |
|---|---|
| Created: config | `portal/playwright.config.ts`, `portal/e2e/tsconfig.json`, `portal/Dockerfile`, `portal/nginx.conf`, `portal/.dockerignore`, `portal/scripts/export-demo-media.mjs` |
| Created: shared | `portal/src/app/shared/ui/**` (the primitives Task 13 defines) |
| Created: features | `portal/src/app/features/sales/{list,editor,detail}/**` (screen components; `editor/sale-form.ts` is Part 1's); `portal/src/app/features/auth/{sign-in,sign-up}/**`; `portal/src/app/features/account/**`; `portal/src/app/features/not-found/**`; `portal/src/app/layout/shell/**` |
| Created: styles | `portal/src/styles/**` (tokens and base, from the approved direction) |
| Created: e2e | `portal/e2e/support/{api,sales,fixtures,global-setup}.ts`; `portal/e2e/specs/{auth,list,filters,create,lifecycle,update,account,errors,a11y}.spec.ts`; `portal/e2e/shots/screens.shots.ts`; `portal/e2e/demos/support/demo.ts`; `portal/e2e/demos/0{1..6}-*.demo.ts` |
| Created: design docs | `DESIGN.md` (+ sidecar), at the paths impeccable reports |
| Created: media | `docs/media/portal/0{1..6}-*.{mp4,gif}` |
| Modified | `portal/package.json`, `portal/package-lock.json`, `portal/.gitignore`, `portal/src/styles.scss`, `portal/src/app/app.routes.ts`, `docker-compose.yml`, `.github/workflows/pull-request.yml`, `README.md` |
| Local only | `portal/node_modules/`, `portal/dist/`, `portal/.angular/`, `portal/test-results/`, `portal/playwright-report/`, `portal/e2e/**/.output/` |

.NET test deltas: none.

---

## Before Phase 3: Start from the merged foundation

### Task 11C: Check Part 1 landed, then branch

**Files:** none changed.

- [ ] **Step 1: Check the tools**

Run: `node -v && npm -v && ffmpeg -version | head -1 && docker compose version`
Expected: `v24.x`, `11.x`, `ffmpeg version 8.x`, `Docker Compose version v2.x`.

- [ ] **Step 2: Check Part 1 is merged**

Run:
```bash
rtk git fetch origin && rtk git checkout develop && rtk git pull --ff-only
rtk git log --oneline -1 --grep "scaffold the Angular 21 portal and its tested core"
ls portal/src/app/core/api/sales-api.ts portal/src/app/features/sales/editor/sale-form.ts
grep -n 'portal:' .github/workflows/pull-request.yml
```
Expected: Part 1's merge commit, both files listed, and the `portal:` job. If any is missing, Part 1 isn't merged: stop and tell the user.

- [ ] **Step 3: Load the design inputs**

- Use `ai-memory-handoff` to read Part 1's handoff. Note the chosen direction's name.
- Find PRODUCT.md, the shape brief and the direction files: `rtk git log develop --name-only --format= --grep '^docs(portal)' | sort -u`.
- Record all four paths at the top of this task as "Design inputs: ...". Every screen task and Task 21 reads them.

- [ ] **Step 4: Check the core is green**

Run: `cd portal && rtk npm ci && rtk npm run build && rtk npm run test:ci`
Expected: the build succeeds and the unit tests pass at Part 1's baseline. Record the number here; Task 28 checks against it.

- [ ] **Step 5: Branch**

Run: `rtk git checkout -b feature/portal-screens`
Expected: `Switched to a new branch 'feature/portal-screens'`.

---

## Phase 3: Playwright first, then the screens

### Task 12: Install Playwright and write every e2e spec from the contract (red)

**Files:**
- Create: `portal/playwright.config.ts`, `portal/e2e/tsconfig.json`, `portal/e2e/support/{api,sales,fixtures,global-setup}.ts`, `portal/e2e/specs/*.spec.ts`, `portal/e2e/shots/screens.shots.ts`
- Modify: `portal/package.json` (dev dependencies and scripts), `portal/.gitignore`

- [ ] **Step 1: Install**

Run: `cd portal && rtk npm install -D @playwright/test@1.63.0 @axe-core/playwright@4.13.0 && rtk npx playwright install chromium`
Expected: `chromium ... downloaded`.

Add these to `portal/package.json` `scripts`, after `test:ci`:
```json
"e2e": "playwright test --project=e2e --project=e2e-mobile",
"shots": "playwright test --project=shots",
"demo:record": "playwright test --project=demo --workers=1",
"demo:export": "node scripts/export-demo-media.mjs"
```
Append to `portal/.gitignore`:
```
# Playwright
/test-results/
/playwright-report/
/blob-report/
/e2e/**/.output/
```

- [ ] **Step 2: Write the config**

`portal/playwright.config.ts`:
```ts
import { defineConfig, devices } from '@playwright/test';

/** Set PORTAL_URL to test a running portal (compose serves it on :8081); otherwise `ng serve` is started. */
const PORTAL_URL = process.env['PORTAL_URL'] ?? 'http://localhost:4200';
const CI = !!process.env['CI'];

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: CI,
  retries: CI ? 1 : 0,
  reporter: CI ? [['github'], ['html', { open: 'never' }]] : [['list'], ['html', { open: 'never' }]],
  globalSetup: './e2e/support/global-setup.ts',
  use: {
    baseURL: PORTAL_URL,
    locale: 'en-US',
    timezoneId: 'America/Sao_Paulo',
    trace: 'retain-on-failure',
    video: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [
    { name: 'e2e', testMatch: /specs\/.*\.spec\.ts/, use: { ...devices['Desktop Chrome'] } },
    { name: 'e2e-mobile', testMatch: /specs\/(auth|create|lifecycle)\.spec\.ts/, use: { ...devices['Pixel 7'] } },
    { name: 'shots', testMatch: /shots\/.*\.shots\.ts/, use: { ...devices['Desktop Chrome'] } },
    {
      name: 'demo',
      testMatch: /demos\/.*\.demo\.ts/,
      fullyParallel: false,
      retries: 0,
      use: {
        ...devices['Desktop Chrome'],
        viewport: { width: 1280, height: 720 },
        deviceScaleFactor: 1,
        video: { mode: 'on', size: { width: 1280, height: 720 } },
      },
    },
  ],
  webServer: process.env['PORTAL_URL']
    ? undefined
    : { command: 'npm run start -- --port 4200', url: PORTAL_URL, reuseExistingServer: !CI, timeout: 120_000 },
});
```

`portal/e2e/tsconfig.json`:
```json
{
  "extends": "../tsconfig.json",
  "compilerOptions": { "types": ["node"], "noEmit": true },
  "include": ["./**/*.ts", "../playwright.config.ts"]
}
```

- [ ] **Step 3: Write the support files**

`portal/e2e/support/api.ts`:
```ts
import { APIRequestContext, expect, request } from '@playwright/test';
import type {
  ApiResponseWithData, AuthenticateResponse, CreateSaleRequest, Sale,
} from '../../src/app/core/api/api-models';

export const API_URL = process.env['API_URL'] ?? 'http://localhost:8080';
export const TEST_PASSWORD = 'Portal@2026';

export interface TestUser {
  readonly username: string;
  readonly email: string;
  readonly phone: string;
  readonly password: string;
}

const unique = (): string => `${Date.now().toString(36)}${Math.random().toString(36).slice(2, 7)}`;

export function newTestUser(tag = 'e2e'): TestUser {
  const id = unique();
  return {
    username: `${tag}-${id}`.slice(0, 50),
    email: `${tag}.${id}@developerstore.test`,
    phone: `+5511${String(Math.floor(Math.random() * 1e9)).padStart(9, '0')}`,
    password: TEST_PASSWORD,
  };
}

/** Talks to the API directly (no browser, no CORS) to set up and inspect data. */
export class ApiClient {
  private token = '';

  private constructor(private readonly context: APIRequestContext) {}

  static async create(): Promise<ApiClient> {
    return new ApiClient(await request.newContext({ baseURL: API_URL }));
  }

  async signUp(user: TestUser): Promise<number> {
    const response = await this.context.post('/api/users', {
      data: { ...user, status: 'Active', role: 'Customer' },
    });
    return response.status();
  }

  async signIn(user: TestUser): Promise<AuthenticateResponse> {
    const response = await this.context.post('/api/auth', { data: { email: user.email, password: user.password } });
    expect(response.status(), await response.text()).toBe(200);
    const body = (await response.json()) as ApiResponseWithData<AuthenticateResponse>;
    this.token = body.data.token;
    return body.data;
  }

  async createSale(sale: CreateSaleRequest): Promise<Sale> {
    const response = await this.context.post('/api/sales', { data: sale, headers: this.auth() });
    expect(response.status(), await response.text()).toBe(201);
    return ((await response.json()) as ApiResponseWithData<Sale>).data;
  }

  async cancelSale(id: string): Promise<void> {
    const response = await this.context.patch(`/api/sales/${id}/cancel`, { headers: this.auth() });
    expect(response.status(), await response.text()).toBe(200);
  }

  async dispose(): Promise<void> {
    await this.context.dispose();
  }

  private auth(): Record<string, string> {
    return { Authorization: `Bearer ${this.token}` };
  }
}
```

`portal/e2e/support/sales.ts`:
```ts
import type { CreateSaleRequest, SaleItemRequest } from '../../src/app/core/api/api-models';
import { BRANCHES, PRODUCTS, type ProductEntry } from '../../src/app/core/catalog/demo-catalog';

export interface Party {
  readonly id: string;
  readonly name: string;
}

/** A customer id no other test uses, so filtering by it isolates a test's sales in a shared database. */
export function uniqueCustomer(name = 'Cliente'): Party {
  return { id: crypto.randomUUID(), name: `${name} ${Math.random().toString(36).slice(2, 8)}` };
}

export function line(product: ProductEntry, quantity: number, unitPrice = product.unitPrice): SaleItemRequest {
  return { productId: product.id, productName: product.name, quantity, unitPrice };
}

export function saleFor(customer: Party, overrides: Partial<CreateSaleRequest> = {}): CreateSaleRequest {
  return {
    saleDate: '2026-09-24T11:30:00-03:00',
    customerId: customer.id,
    customerName: customer.name,
    branchId: BRANCHES[0].id,
    branchName: BRANCHES[0].name,
    items: [line(PRODUCTS[0], 5)],
    ...overrides,
  };
}
```

`portal/e2e/support/fixtures.ts`:
```ts
import { expect, type Page, test as base } from '@playwright/test';
import type { AuthenticateResponse } from '../../src/app/core/api/api-models';
import { SESSION_STORAGE_KEY, sessionFromAuthResponse } from '../../src/app/core/auth/session';
import { ApiClient, newTestUser, type TestUser } from './api';

/** Starts the page signed in by writing the session the portal would have written. */
export async function useSession(page: Page, auth: AuthenticateResponse): Promise<void> {
  const session = JSON.stringify(sessionFromAuthResponse(auth));
  await page.addInitScript(([key, value]) => window.sessionStorage.setItem(key, value), [SESSION_STORAGE_KEY, session] as const);
}

export const test = base.extend<{ api: ApiClient; user: TestUser; auth: AuthenticateResponse; signedInPage: Page }>({
  api: async ({}, use) => {
    const api = await ApiClient.create();
    await use(api);
    await api.dispose();
  },
  user: async ({ api }, use) => {
    const user = newTestUser();
    expect(await api.signUp(user)).toBe(201);
    await use(user);
  },
  auth: async ({ api, user }, use) => {
    await use(await api.signIn(user));
  },
  signedInPage: async ({ page, auth }, use) => {
    await useSession(page, auth);
    await use(page);
  },
});

export { expect };
```

`portal/e2e/support/global-setup.ts`:
```ts
import { API_URL } from './api';

/** Fails fast, with the fix, when the API isn't up. */
export default async function globalSetup(): Promise<void> {
  const deadline = Date.now() + 60_000;
  while (Date.now() < deadline) {
    try {
      if ((await fetch(`${API_URL}/health/ready`)).ok) {
        return;
      }
    } catch {
      // Not listening yet.
    }
    await new Promise(resolve => setTimeout(resolve, 1_000));
  }
  throw new Error(`The API isn't answering at ${API_URL}/health/ready. From the repo root, run: docker compose up -d --build`);
}
```

- [ ] **Step 4: Write the specs**

`portal/e2e/specs/auth.spec.ts`:
```ts
import { newTestUser } from '../support/api';
import { expect, test, useSession } from '../support/fixtures';
import { fakeJwt } from '../../src/app/testing/fake-jwt';

test.describe('Sign up and sign in', () => {
  test('a new customer signs up and lands on the sales list', async ({ page }) => {
    const user = newTestUser();
    await page.goto('/sign-up');

    await page.getByLabel('Username').fill(user.username);
    await page.getByLabel('Email').fill(user.email);
    await page.getByLabel('Phone').fill(user.phone);
    await page.getByLabel('Password', { exact: true }).fill(user.password);
    await page.getByRole('button', { name: 'Create account' }).click();

    await expect(page).toHaveURL(/\/sales$/);
    await expect(page.getByRole('heading', { level: 1, name: 'Sales', exact: true })).toBeVisible();
  });

  test('the password rules stop a weak password before the API is called', async ({ page }) => {
    let signUps = 0;
    page.on('request', request => { if (request.url().endsWith('/api/users')) signUps++; });
    const user = newTestUser();
    await page.goto('/sign-up');

    await page.getByLabel('Username').fill(user.username);
    await page.getByLabel('Email').fill(user.email);
    await page.getByLabel('Phone').fill(user.phone);
    await page.getByLabel('Password', { exact: true }).fill('weak');
    await page.getByRole('button', { name: 'Create account' }).click();

    const rules = page.getByRole('list', { name: 'Password rules' });
    await expect(rules.getByRole('listitem').filter({ hasText: 'At least 8 characters' })).toContainText(/not met/i);
    await expect(page).toHaveURL(/\/sign-up$/);
    expect(signUps).toBe(0);
  });

  test('signing up twice with one email shows the API conflict', async ({ page, api }) => {
    const user = newTestUser();
    expect(await api.signUp(user)).toBe(201);
    await page.goto('/sign-up');

    await page.getByLabel('Username').fill(user.username);
    await page.getByLabel('Email').fill(user.email);
    await page.getByLabel('Phone').fill(user.phone);
    await page.getByLabel('Password', { exact: true }).fill(user.password);
    await page.getByRole('button', { name: 'Create account' }).click();

    await expect(page.getByRole('alert')).toContainText(`User with email ${user.email} already exists`);
  });

  test('a wrong password shows "Invalid credentials"', async ({ page, user }) => {
    await page.goto('/sign-in');
    await page.getByLabel('Email').fill(user.email);
    await page.getByLabel('Password', { exact: true }).fill('Wrong@2026');
    await page.getByRole('button', { name: 'Sign in' }).click();

    await expect(page.getByRole('alert')).toContainText('Invalid credentials');
  });

  test('signing in returns to the page the user asked for', async ({ page, user }) => {
    await page.goto('/sales/new');
    await expect(page).toHaveURL(/\/sign-in\?returnUrl=%2Fsales%2Fnew/);

    await page.getByLabel('Email').fill(user.email);
    await page.getByLabel('Password', { exact: true }).fill(user.password);
    await page.getByRole('button', { name: 'Sign in' }).click();

    await expect(page.getByRole('heading', { level: 1, name: 'New sale' })).toBeVisible();
  });

  test('signing out ends the session', async ({ signedInPage: page }) => {
    await page.goto('/sales');
    await page.getByRole('button', { name: 'Sign out' }).click();

    await expect(page).toHaveURL(/\/sign-in/);
    await page.goto('/sales');
    await expect(page).toHaveURL(/\/sign-in\?returnUrl=%2Fsales/);
  });

  test('a token the API rejects sends the user to sign in with a notice', async ({ page }) => {
    const forged = { token: fakeJwt({ nameid: crypto.randomUUID(), exp: 9_999_999_999 }), email: 'x@y.test', name: 'X', role: 'Customer' };
    await useSession(page, forged);

    await page.goto('/sales');

    await expect(page).toHaveURL(/\/sign-in/);
    await expect(page.getByText('Your session expired. Sign in again.')).toBeVisible();
  });
});
```

`portal/e2e/specs/list.spec.ts`:
```ts
import type { Page } from '@playwright/test';
import { formatMoney } from '../../src/app/shared/format';
import { PRODUCTS } from '../../src/app/core/catalog/demo-catalog';
import { expect, test } from '../support/fixtures';
import { line, saleFor, uniqueCustomer } from '../support/sales';

const rows = (page: Page) =>
  page.getByRole('table', { name: 'Sales' }).getByRole('row').filter({ has: page.getByRole('cell') });

test.describe('Sales list: paging and ordering', () => {
  test('pages through 12 sales, newest first', async ({ signedInPage: page, api }) => {
    const customer = uniqueCustomer();
    for (let day = 1; day <= 12; day++) {
      await api.createSale(saleFor(customer, { saleDate: `2026-08-${String(day).padStart(2, '0')}T10:00:00-03:00` }));
    }

    await page.goto(`/sales?customerId=${customer.id}`);

    await expect(rows(page)).toHaveCount(10);
    await expect(rows(page).first()).toContainText('Aug 12, 2026');
    await expect(page.getByText('Page 1 of 2')).toBeVisible();
    await expect(page.getByText('12 sales')).toBeVisible();

    await page.getByRole('button', { name: 'Next page' }).click();

    await expect(page).toHaveURL(/_page=2/);
    await expect(rows(page)).toHaveCount(2);
    await expect(rows(page).last()).toContainText('Aug 1, 2026');
  });

  test('sorts by total, highest first, then flips', async ({ signedInPage: page, api }) => {
    const customer = uniqueCustomer();
    for (const price of [10, 30, 20]) {
      await api.createSale(saleFor(customer, { items: [line(PRODUCTS[0], 1, price)] }));
    }
    await page.goto(`/sales?customerId=${customer.id}`);

    const total = page.getByRole('columnheader', { name: 'Total' });
    await total.getByRole('button', { name: 'Total' }).click();

    await expect(total).toHaveAttribute('aria-sort', 'descending');
    await expect(rows(page).first()).toContainText(formatMoney(30));

    await total.getByRole('button', { name: 'Total' }).click();

    await expect(total).toHaveAttribute('aria-sort', 'ascending');
    await expect(rows(page).first()).toContainText(formatMoney(10));
    await expect(page).toHaveURL(/_order=totalAmount(%20|\+)asc/);
  });

  test('Shift+click adds a second sort', async ({ signedInPage: page, api }) => {
    const customer = uniqueCustomer();
    await api.createSale(saleFor(customer, { customerName: 'Ana', items: [line(PRODUCTS[0], 1, 10)] }));
    await api.createSale(saleFor(customer, { customerName: 'Ana', items: [line(PRODUCTS[0], 1, 30)] }));
    await api.createSale(saleFor(customer, { customerName: 'Bruno', items: [line(PRODUCTS[0], 1, 20)] }));
    await page.goto(`/sales?customerId=${customer.id}`);

    await page.getByRole('columnheader', { name: 'Customer' }).getByRole('button', { name: 'Customer' }).click();
    await page.getByRole('columnheader', { name: 'Total' }).getByRole('button', { name: 'Total' }).click({ modifiers: ['Shift'] });

    await expect(page).toHaveURL(/_order=customerName(%20|\+)asc(%2C|,)(%20|\+)totalAmount(%20|\+)desc/);
    await expect(rows(page).nth(0)).toContainText(formatMoney(30));
    await expect(rows(page).nth(1)).toContainText(formatMoney(10));
    await expect(rows(page).nth(2)).toContainText('Bruno');
  });

  test('changing rows per page goes back to page 1', async ({ signedInPage: page, api }) => {
    const customer = uniqueCustomer();
    for (let i = 0; i < 11; i++) await api.createSale(saleFor(customer));
    await page.goto(`/sales?customerId=${customer.id}&_page=2`);

    await page.getByLabel('Rows per page').selectOption('20');

    await expect(page).not.toHaveURL(/_page=/);
    await expect(rows(page)).toHaveCount(11);
  });
});
```

`portal/e2e/specs/filters.spec.ts`:
```ts
import type { Page } from '@playwright/test';
import { PRODUCTS } from '../../src/app/core/catalog/demo-catalog';
import { expect, test } from '../support/fixtures';
import { line, saleFor, uniqueCustomer } from '../support/sales';

const rows = (page: Page) =>
  page.getByRole('table', { name: 'Sales' }).getByRole('row').filter({ has: page.getByRole('cell') });
const filters = (page: Page) => page.getByRole('search', { name: 'Filters' });

test.describe('Sales list: filters', () => {
  test('a customer filter matches anywhere in the name, ignoring case', async ({ signedInPage: page, api }) => {
    const token = Math.random().toString(36).slice(2, 8);
    const customer = uniqueCustomer();
    await api.createSale(saleFor(customer, { customerName: `Cliente ${token} Silva` }));
    await api.createSale(saleFor(customer, { customerName: `Cliente ${token} Souza` }));
    await page.goto('/sales');

    await filters(page).getByLabel('Customer', { exact: true }).fill(`${token} SILVA`);
    await filters(page).getByRole('button', { name: 'Apply filters' }).click();

    await expect(rows(page)).toHaveCount(1);
    await expect(rows(page).first()).toContainText(`Cliente ${token} Silva`);
  });

  test('a typed * is sent as a wildcard', async ({ signedInPage: page, api }) => {
    const token = Math.random().toString(36).slice(2, 8);
    const customer = uniqueCustomer();
    await api.createSale(saleFor(customer, { customerName: `${token} Silva` }));
    await api.createSale(saleFor(customer, { customerName: `Maria ${token}` }));
    await page.goto('/sales');

    await filters(page).getByLabel('Customer', { exact: true }).fill(`${token}*`);
    await filters(page).getByRole('button', { name: 'Apply filters' }).click();

    await expect(rows(page)).toHaveCount(1);
    await expect(rows(page).first()).toContainText(`${token} Silva`);
  });

  test('status shows only cancelled sales', async ({ signedInPage: page, api }) => {
    const customer = uniqueCustomer();
    await api.createSale(saleFor(customer));
    const cancelled = await api.createSale(saleFor(customer));
    await api.cancelSale(cancelled.id);
    await page.goto(`/sales?customerId=${customer.id}`);

    await filters(page).getByRole('radio', { name: 'Cancelled' }).check();
    await filters(page).getByRole('button', { name: 'Apply filters' }).click();

    await expect(page).toHaveURL(/isCancelled=true/);
    await expect(rows(page)).toHaveCount(1);
    await expect(rows(page).first()).toContainText(cancelled.saleNumber);
    await expect(rows(page).first()).toContainText('Cancelled');
  });

  test('a date range covers whole local days', async ({ signedInPage: page, api }) => {
    const customer = uniqueCustomer();
    // 23:30 in São Paulo on July 10 is already July 11 in UTC; the portal filters by local day.
    const lateOnTheTenth = await api.createSale(saleFor(customer, { saleDate: '2026-07-10T23:30:00-03:00' }));
    await api.createSale(saleFor(customer, { saleDate: '2026-07-11T09:00:00-03:00' }));
    await page.goto(`/sales?customerId=${customer.id}`);

    await filters(page).getByLabel('Sold from').fill('2026-07-10');
    await filters(page).getByLabel('Sold to').fill('2026-07-10');
    await filters(page).getByRole('button', { name: 'Apply filters' }).click();

    await expect(rows(page)).toHaveCount(1);
    await expect(rows(page).first()).toContainText(lateOnTheTenth.saleNumber);
  });

  test('a total range is inclusive', async ({ signedInPage: page, api }) => {
    const customer = uniqueCustomer();
    for (const price of [10, 20, 30]) {
      await api.createSale(saleFor(customer, { items: [line(PRODUCTS[0], 1, price)] }));
    }
    await page.goto(`/sales?customerId=${customer.id}`);

    await filters(page).getByLabel('Minimum total').fill('20');
    await filters(page).getByLabel('Maximum total').fill('30');
    await filters(page).getByRole('button', { name: 'Apply filters' }).click();

    await expect(rows(page)).toHaveCount(2);
  });

  test('a minimum above the maximum is caught before the API is called', async ({ signedInPage: page }) => {
    await page.goto('/sales');
    let listCalls = 0;
    page.on('request', request => { if (request.url().includes('/api/sales?')) listCalls++; });

    await filters(page).getByLabel('Minimum total').fill('50');
    await filters(page).getByLabel('Maximum total').fill('10');
    await filters(page).getByRole('button', { name: 'Apply filters' }).click();

    await expect(page.getByText('Minimum total must not be above maximum total.')).toBeVisible();
    expect(listCalls).toBe(0);
  });

  test('the branch ID filter lives under More filters', async ({ signedInPage: page, api }) => {
    const customer = uniqueCustomer();
    const branchId = crypto.randomUUID();
    await api.createSale(saleFor(customer, { branchId, branchName: 'Filial Teste' }));
    await api.createSale(saleFor(customer));
    await page.goto(`/sales?customerId=${customer.id}`);

    await filters(page).getByRole('button', { name: 'More filters' }).click();
    await filters(page).getByLabel('Branch ID').fill(branchId);
    await filters(page).getByRole('button', { name: 'Apply filters' }).click();

    await expect(rows(page)).toHaveCount(1);
    await expect(rows(page).first()).toContainText('Filial Teste');
  });

  test('filters survive a reload and Clear filters resets them', async ({ signedInPage: page, api }) => {
    const customer = uniqueCustomer();
    await api.createSale(saleFor(customer));
    await page.goto(`/sales?customerId=${customer.id}&isCancelled=false`);
    await page.reload();

    await expect(filters(page).getByRole('radio', { name: 'Open' })).toBeChecked();
    await expect(rows(page)).toHaveCount(1);

    await filters(page).getByRole('button', { name: 'Clear filters' }).click();
    await expect(page).toHaveURL(/\/sales$/);
  });
});
```

`portal/e2e/specs/create.spec.ts`:
```ts
import { CUSTOMERS, BRANCHES, PRODUCTS } from '../../src/app/core/catalog/demo-catalog';
import { formatMoney } from '../../src/app/shared/format';
import { expect, test } from '../support/fixtures';
import { saleFor, uniqueCustomer } from '../support/sales';

test.describe('Create a sale', () => {
  test.beforeEach(async ({ signedInPage: page }) => {
    await page.goto('/sales/new');
    await page.getByLabel('Customer', { exact: true }).selectOption({ label: CUSTOMERS[0].name });
    await page.getByLabel('Branch', { exact: true }).selectOption({ label: BRANCHES[0].name });
    const first = page.getByRole('group', { name: 'Line 1' });
    await first.getByLabel('Product', { exact: true }).selectOption({ label: PRODUCTS[0].name });
  });

  test('previews 10% off for 5 items and lets the API number the sale', async ({ signedInPage: page }) => {
    const first = page.getByRole('group', { name: 'Line 1' });
    await expect(first.getByLabel('Unit price')).toHaveValue('4.5');
    await first.getByLabel('Quantity').fill('5');

    await expect(first.getByText('10% off')).toBeVisible();
    await expect(page.getByRole('region', { name: 'Summary' })).toContainText(formatMoney(20.25));

    await page.getByRole('button', { name: 'Create sale' }).click();

    await expect(page).toHaveURL(/\/sales\/[0-9a-f-]{36}$/);
    await expect(page.getByRole('heading', { level: 1 })).toHaveText(/^Sale S-\d{6}$/);
    await expect(page.getByRole('status')).toContainText(/Sale S-\d{6} created/);
    await expect(page.getByRole('region', { name: 'Totals' })).toContainText(formatMoney(20.25));
  });

  test('hints at the next discount tier', async ({ signedInPage: page }) => {
    const first = page.getByRole('group', { name: 'Line 1' });

    await first.getByLabel('Quantity').fill('3');
    await expect(first.getByText('No discount')).toBeVisible();
    await expect(first.getByText('Add 1 more for 10% off')).toBeVisible();

    await first.getByLabel('Quantity').fill('9');
    await expect(first.getByText('Add 1 more for 20% off')).toBeVisible();

    await first.getByLabel('Quantity').fill('10');
    await expect(first.getByText('20% off')).toBeVisible();
    await expect(first.getByText(/Add \d+ more/)).toHaveCount(0);
  });

  test('refuses more than 20 identical items without calling the API', async ({ signedInPage: page }) => {
    let creates = 0;
    page.on('request', request => { if (request.method() === 'POST' && request.url().endsWith('/api/sales')) creates++; });
    const first = page.getByRole('group', { name: 'Line 1' });

    await first.getByLabel('Quantity').fill('21');
    await page.getByRole('button', { name: 'Create sale' }).click();

    await expect(first.getByText("It's not possible to sell above 20 identical items.")).toBeVisible();
    expect(creates).toBe(0);
  });

  test('refuses the same product on two lines', async ({ signedInPage: page }) => {
    await page.getByRole('button', { name: 'Add line' }).click();
    await page.getByRole('group', { name: 'Line 2' }).getByLabel('Product', { exact: true }).selectOption({ label: PRODUCTS[0].name });
    await page.getByRole('button', { name: 'Create sale' }).click();

    await expect(page.getByText('Each product can appear only once in a sale.')).toBeVisible();
  });

  test('a sale number already taken is a conflict', async ({ signedInPage: page, api }) => {
    const taken = `E2E-${Date.now()}`;
    await api.createSale(saleFor(uniqueCustomer(), { saleNumber: taken }));

    await page.getByLabel('Sale number').fill(taken);
    await page.getByRole('button', { name: 'Create sale' }).click();

    await expect(page.getByRole('alert')).toContainText(`Sale number ${taken} already exists`);
  });

  test('accepts a customer outside the catalog', async ({ signedInPage: page }) => {
    await page.getByLabel('Customer', { exact: true }).selectOption({ label: 'Other customer…' });
    await page.getByLabel('Customer name').fill('Padaria Pão Quente');
    await expect(page.getByLabel('Customer ID')).toHaveValue(/^[0-9a-f-]{36}$/);

    await page.getByRole('button', { name: 'Create sale' }).click();

    await expect(page.getByRole('heading', { level: 1 })).toHaveText(/^Sale S-\d{6}$/);
    await expect(page.getByText('Padaria Pão Quente')).toBeVisible();
  });
});
```

`portal/e2e/specs/lifecycle.spec.ts`:
```ts
import { PRODUCTS } from '../../src/app/core/catalog/demo-catalog';
import { formatMoney } from '../../src/app/shared/format';
import { expect, test } from '../support/fixtures';
import { line, saleFor, uniqueCustomer } from '../support/sales';

test.describe('Read, cancel and delete a sale', () => {
  test('cancelling one item drops it from the total and keeps the sale open', async ({ signedInPage: page, api }) => {
    const sale = await api.createSale(saleFor(uniqueCustomer(), { items: [line(PRODUCTS[0], 5), line(PRODUCTS[1], 2)] }));
    await page.goto(`/sales/${sale.id}`);

    await page.getByRole('button', { name: `Cancel item ${PRODUCTS[1].name}` }).click();
    const dialog = page.getByRole('alertdialog', { name: 'Cancel this item?' });
    await dialog.getByRole('button', { name: 'Cancel item' }).click();

    await expect(page.getByRole('status')).toContainText('Item cancelled');
    const row = page.getByRole('table', { name: 'Items' }).getByRole('row').filter({ hasText: PRODUCTS[1].name });
    await expect(row).toContainText('Cancelled');
    await expect(page.getByRole('region', { name: 'Totals' })).toContainText(formatMoney(20.25));
    await expect(page.getByText('Open', { exact: true })).toBeVisible();
  });

  test('cancelling the last active item cancels the sale', async ({ signedInPage: page, api }) => {
    const sale = await api.createSale(saleFor(uniqueCustomer()));
    await page.goto(`/sales/${sale.id}`);

    await page.getByRole('button', { name: `Cancel item ${PRODUCTS[0].name}` }).click();
    const dialog = page.getByRole('alertdialog', { name: 'Cancel this item?' });
    await expect(dialog).toContainText('This is the last active item, so the sale will be cancelled too.');
    await dialog.getByRole('button', { name: 'Cancel item' }).click();

    await expect(page.getByText('Cancelled', { exact: true }).first()).toBeVisible();
    await expect(page.getByRole('region', { name: 'Totals' })).toContainText(formatMoney(0));
    await expect(page.getByText('Cancelled sales are read-only. You can still delete it.')).toBeVisible();
    await expect(page.getByRole('link', { name: 'Edit sale' })).toHaveCount(0);
  });

  test('cancelling a sale keeps its items and total', async ({ signedInPage: page, api }) => {
    const sale = await api.createSale(saleFor(uniqueCustomer()));
    await page.goto(`/sales/${sale.id}`);

    await page.getByRole('button', { name: 'Cancel sale' }).click();
    const dialog = page.getByRole('alertdialog', { name: 'Cancel this sale?' });
    await dialog.getByRole('button', { name: 'Cancel sale' }).click();

    await expect(page.getByRole('status')).toContainText(`Sale ${sale.saleNumber} cancelled`);
    await expect(page.getByRole('region', { name: 'Totals' })).toContainText(formatMoney(20.25));
    await expect(page.getByRole('button', { name: /^Cancel item/ })).toHaveCount(0);
  });

  test('keeping the sale closes the dialog and changes nothing', async ({ signedInPage: page, api }) => {
    const sale = await api.createSale(saleFor(uniqueCustomer()));
    await page.goto(`/sales/${sale.id}`);

    await page.getByRole('button', { name: 'Delete sale' }).click();
    await page.getByRole('alertdialog', { name: 'Delete this sale?' }).getByRole('button', { name: 'Keep sale' }).click();

    await expect(page.getByRole('alertdialog')).toHaveCount(0);
    await expect(page.getByRole('heading', { level: 1 })).toHaveText(`Sale ${sale.saleNumber}`);
    await expect(page.getByRole('button', { name: 'Delete sale' })).toBeFocused();
  });

  test('deleting a sale hides it everywhere', async ({ signedInPage: page, api }) => {
    const customer = uniqueCustomer();
    const sale = await api.createSale(saleFor(customer));
    await page.goto(`/sales/${sale.id}`);

    await page.getByRole('button', { name: 'Delete sale' }).click();
    await page.getByRole('alertdialog', { name: 'Delete this sale?' }).getByRole('button', { name: 'Delete sale' }).click();

    await expect(page).toHaveURL(/\/sales$/);
    await expect(page.getByRole('status')).toContainText(`Sale ${sale.saleNumber} deleted`);

    await page.goto(`/sales?customerId=${customer.id}`);
    await expect(page.getByText('No sales match these filters.')).toBeVisible();

    await page.goto(`/sales/${sale.id}`);
    await expect(page.getByRole('heading', { level: 1, name: 'Sale not found' })).toBeVisible();
  });

  test('a cancelled sale can still be deleted', async ({ signedInPage: page, api }) => {
    const sale = await api.createSale(saleFor(uniqueCustomer()));
    await api.cancelSale(sale.id);
    await page.goto(`/sales/${sale.id}`);

    await page.getByRole('button', { name: 'Delete sale' }).click();
    await page.getByRole('alertdialog', { name: 'Delete this sale?' }).getByRole('button', { name: 'Delete sale' }).click();

    await expect(page.getByRole('status')).toContainText(`Sale ${sale.saleNumber} deleted`);
  });
});
```

`portal/e2e/specs/update.spec.ts`:
```ts
import { PRODUCTS } from '../../src/app/core/catalog/demo-catalog';
import { formatMoney } from '../../src/app/shared/format';
import { expect, test } from '../support/fixtures';
import { line, saleFor, uniqueCustomer } from '../support/sales';

test.describe('Update a sale', () => {
  test('re-prices a line and adds a product', async ({ signedInPage: page, api }) => {
    const sale = await api.createSale(saleFor(uniqueCustomer()));
    await page.goto(`/sales/${sale.id}`);
    await page.getByRole('link', { name: 'Edit sale' }).click();

    await expect(page.getByRole('heading', { level: 1 })).toHaveText(`Edit sale ${sale.saleNumber}`);
    await expect(page.getByLabel('Sale number')).not.toBeEditable();

    await page.getByRole('group', { name: 'Line 1' }).getByLabel('Quantity').fill('10');
    await page.getByRole('button', { name: 'Add line' }).click();
    const second = page.getByRole('group', { name: 'Line 2' });
    await second.getByLabel('Product', { exact: true }).selectOption({ label: PRODUCTS[1].name });
    await second.getByLabel('Quantity').fill('2');
    await page.getByRole('button', { name: 'Save changes' }).click();

    // 10 × 4.50 = 45.00, 20% off → 36.00; 2 × 7.90 = 15.80 → 51.80.
    await expect(page.getByRole('status')).toContainText('Changes saved');
    await expect(page.getByRole('region', { name: 'Totals' })).toContainText(formatMoney(51.8));
  });

  test('removing a line cancels it, and it stays as history', async ({ signedInPage: page, api }) => {
    const sale = await api.createSale(saleFor(uniqueCustomer(), { items: [line(PRODUCTS[0], 5), line(PRODUCTS[1], 2)] }));
    await page.goto(`/sales/${sale.id}/edit`);

    await page.getByRole('button', { name: 'Remove line 2' }).click();
    await page.getByRole('button', { name: 'Save changes' }).click();

    const row = page.getByRole('table', { name: 'Items' }).getByRole('row').filter({ hasText: PRODUCTS[1].name });
    await expect(row).toContainText('Cancelled');

    await page.getByRole('link', { name: 'Edit sale' }).click();
    await expect(page.getByRole('region', { name: 'Cancelled lines' })).toContainText(PRODUCTS[1].name);
  });

  test('a cancelled sale opens read-only', async ({ signedInPage: page, api }) => {
    const sale = await api.createSale(saleFor(uniqueCustomer()));
    await api.cancelSale(sale.id);

    await page.goto(`/sales/${sale.id}/edit`);

    await expect(page).toHaveURL(new RegExp(`/sales/${sale.id}$`));
    await expect(page.getByText('Cancelled sales are read-only. You can still delete it.')).toBeVisible();
  });
});
```

`portal/e2e/specs/account.spec.ts`:
```ts
import { expect, test } from '../support/fixtures';

test.describe('Account', () => {
  test('shows the signed-in user', async ({ signedInPage: page, user }) => {
    await page.goto('/account');

    await expect(page.getByRole('heading', { level: 1, name: 'Account' })).toBeVisible();
    await expect(page.getByText(user.email)).toBeVisible();
    await expect(page.getByText(user.phone)).toBeVisible();
    await expect(page.getByText('Customer', { exact: true })).toBeVisible();
    await expect(page.getByText('Active', { exact: true })).toBeVisible();
  });

  test('deleting the account signs the user out for good', async ({ signedInPage: page, user }) => {
    await page.goto('/account');
    await page.getByRole('button', { name: 'Delete account' }).click();
    await page.getByRole('alertdialog', { name: 'Delete your account?' }).getByRole('button', { name: 'Delete account' }).click();

    await expect(page).toHaveURL(/\/sign-in/);
    await expect(page.getByText('Your account was deleted.')).toBeVisible();

    await page.getByLabel('Email').fill(user.email);
    await page.getByLabel('Password', { exact: true }).fill(user.password);
    await page.getByRole('button', { name: 'Sign in' }).click();
    await expect(page.getByRole('alert')).toContainText('Invalid credentials');
  });
});
```

`portal/e2e/specs/errors.spec.ts` (the only spec that intercepts requests):
```ts
import { expect, test } from '../support/fixtures';
import { saleFor, uniqueCustomer } from '../support/sales';

test.describe('Error states the API cannot produce on demand', () => {
  // A predicate, not a glob: a ? in a URL glob isn't a reliable literal across Playwright versions.
  const isSalesList = (url: URL) => url.pathname === '/api/sales';

  test('the API being down shows a retry', async ({ signedInPage: page }) => {
    await page.route(isSalesList, route => route.abort('connectionrefused'));
    await page.goto('/sales');

    await expect(page.getByRole('alert')).toContainText("Can't reach the API");
    await page.unroute(isSalesList);
    await page.getByRole('button', { name: 'Try again' }).click();

    await expect(page.getByRole('table', { name: 'Sales' }).or(page.getByText('No sales yet.'))).toBeVisible();
  });

  test('a server error shows the API error title and detail', async ({ signedInPage: page }) => {
    await page.route(isSalesList, route => route.fulfill({
      status: 500,
      json: { type: 'InternalServerError', error: 'Internal server error', detail: 'An unexpected error occurred.' },
    }));
    await page.goto('/sales');

    await expect(page.getByRole('alert')).toContainText('Internal server error');
    await expect(page.getByRole('alert')).toContainText('An unexpected error occurred.');
  });

  test('a concurrency conflict offers to reload the sale', async ({ signedInPage: page, api }) => {
    const sale = await api.createSale(saleFor(uniqueCustomer()));
    await page.route(`**/api/sales/${sale.id}/cancel`, route => route.fulfill({
      status: 409,
      json: { type: 'ConcurrencyConflict', error: 'Concurrent modification', detail: 'The sale was changed by someone else.' },
    }));
    await page.goto(`/sales/${sale.id}`);

    await page.getByRole('button', { name: 'Cancel sale' }).click();
    await page.getByRole('alertdialog', { name: 'Cancel this sale?' }).getByRole('button', { name: 'Cancel sale' }).click();

    await expect(page.getByRole('alert')).toContainText('The sale was changed by someone else.');
    await expect(page.getByRole('button', { name: 'Reload sale' })).toBeVisible();
  });

  test('an unknown address shows Page not found', async ({ signedInPage: page }) => {
    await page.goto('/nowhere');

    await expect(page.getByRole('heading', { level: 1, name: 'Page not found' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Back to sales' })).toBeVisible();
  });
});
```

`portal/e2e/specs/a11y.spec.ts`:
```ts
import AxeBuilder from '@axe-core/playwright';
import type { Page } from '@playwright/test';
import { expect, test } from '../support/fixtures';
import { saleFor, uniqueCustomer } from '../support/sales';

const WCAG = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'];
const expectNoViolations = async (page: Page) => {
  const results = await new AxeBuilder({ page }).withTags(WCAG).analyze();
  expect(results.violations, JSON.stringify(results.violations, null, 2)).toEqual([]);
};

test.describe('Accessibility (axe, WCAG 2.2 AA)', () => {
  test('sign in and sign up', async ({ page }) => {
    await page.goto('/sign-in');
    await expectNoViolations(page);
    await page.goto('/sign-up');
    await expectNoViolations(page);
  });

  test('the signed-in screens', async ({ signedInPage: page, api }) => {
    const sale = await api.createSale(saleFor(uniqueCustomer()));
    for (const path of ['/sales', '/sales/new', `/sales/${sale.id}`, `/sales/${sale.id}/edit`, '/account']) {
      await page.goto(path);
      await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
      await expectNoViolations(page);
    }
  });

  test('a dialog traps focus and Escape closes it', async ({ signedInPage: page, api }) => {
    const sale = await api.createSale(saleFor(uniqueCustomer()));
    await page.goto(`/sales/${sale.id}`);

    await page.getByRole('button', { name: 'Delete sale' }).click();
    const dialog = page.getByRole('alertdialog', { name: 'Delete this sale?' });
    await expect(dialog.getByRole('button', { name: 'Keep sale' })).toBeFocused();
    await expectNoViolations(page);

    await page.keyboard.press('Escape');
    await expect(dialog).toHaveCount(0);
  });
});
```

`portal/e2e/shots/screens.shots.ts` (for the design review in Task 20; not an assertion suite):
```ts
import { expect, test } from '../support/fixtures';
import { saleFor, uniqueCustomer, line } from '../support/sales';
import { PRODUCTS } from '../../src/app/core/catalog/demo-catalog';

const WIDTHS = [1280, 390];
const OUT = 'e2e/shots/.output';

test('capture every screen at desktop and phone widths', async ({ browser, signedInPage: page, api }) => {
  const sale = await api.createSale(saleFor(uniqueCustomer(), { items: [line(PRODUCTS[0], 5), line(PRODUCTS[1], 12), line(PRODUCTS[2], 2)] }));
  for (let i = 0; i < 14; i++) await api.createSale(saleFor(uniqueCustomer()));
  const signedOut = await browser.newPage();

  for (const width of WIDTHS) {
    await page.setViewportSize({ width, height: 900 });
    await signedOut.setViewportSize({ width, height: 900 });
    for (const [name, target, path] of [
      ['sign-in', signedOut, '/sign-in'], ['sign-up', signedOut, '/sign-up'], ['sales', page, '/sales'],
      ['new-sale', page, '/sales/new'], ['sale', page, `/sales/${sale.id}`], ['edit-sale', page, `/sales/${sale.id}/edit`],
      ['account', page, '/account'], ['not-found', page, '/nowhere'],
    ] as const) {
      await target.goto(path);
      await expect(target.getByRole('heading', { level: 1 })).toBeVisible();
      await target.screenshot({ path: `${OUT}/${name}-${width}.png`, fullPage: true });
    }
  }
  await signedOut.close();
});
```

- [ ] **Step 5: Run the suite and see it fail for the right reason**

Run from the repo root: `docker compose up -d --build`. Then: `cd portal && rtk npm run e2e`
Expected:
- global setup passes, because the API is up;
- every spec fails on a missing screen, such as `getByLabel('Username')` timing out, and never on a TypeScript error.

A TypeScript or import error is a bug in this task: fix it before committing. Then run `docker compose down`.

- [ ] **Step 6: Commit**

```bash
rtk git add portal/package.json portal/package-lock.json portal/.gitignore portal/playwright.config.ts portal/e2e/tsconfig.json portal/e2e/support portal/e2e/specs portal/e2e/shots/screens.shots.ts
rtk git commit -m "test(portal): write the end-to-end specs from the accessibility contract"
```

---

### How every screen task (13–19) works

1. **Read impeccable's `reference/craft-floor.md`**, plus the shape brief's section for the screen and the direction contract (built code-first: there is no comp).
2. **Build the screen**: standalone components, `ChangeDetectionStrategy.OnPush`, `inject()`, `input()`/`output()`, signals and `computed`, and the `@if`/`@for` control flow. Use templates and SCSS in separate files, with no inline styles, and only the tokens from `src/styles/`. Components call the core services from Phase 2. They hold no business rules the core already has: pricing, query mapping and error mapping all come from Phase 2.
3. **Follow the contract exactly** for names and roles. Handle loading, empty and error states as the shape brief says. An `ApiError` is shown by the shared error component (Task 13).
4. **Turn the screen's spec green:** `cd portal && rtk npx playwright test e2e/specs/<file> --project=e2e`, with `docker compose up -d` running. Then run `rtk npm run test:ci` to keep the unit tests green.
5. **Commit** with the message given in the task.

### Task 13: Tokens, shell, primitives and the error, toast and dialog plumbing

**Files:**
- Create: `portal/src/styles/{tokens,base}.scss` (imported from `portal/src/styles.scss`); `portal/src/app/layout/shell/shell.{ts,html,scss}`
- Create: `portal/src/app/shared/ui/`: `api-error-alert/` (renders an `ApiError`: title, detail, a "Try again" action for `NetworkError`, a caller-supplied action such as "Reload sale" for `ConcurrencyConflict`); `toast/` (a `ToastStore` signal service with `show(message)`, which auto-dismisses after 5 s, plus the `role="status"` outlet in the shell); `confirm-dialog/` (a CDK `Dialog` with `role: 'alertdialog'`, `ariaLabelledBy` on its heading, first focus on the safe button, focus restored to the trigger on close; a `ConfirmDialog.open({ title, body, confirmLabel, keepLabel })` service returning `Observable<boolean>`); `field/` (a label, control slot, description and error text wired with `aria-describedby`, using `describeFieldError`); `status-badge/` ("Open"/"Cancelled" as text plus shape)
- Create: `portal/src/app/features/not-found/not-found.{ts,html,scss}`
- Modify: `portal/src/app/app.routes.ts` (add the shell parent route and the `**` route)

- [ ] **Step 1:** Read the craft floor, the brief and the direction contract. Turn the approved direction into CSS custom properties in `tokens.scss`: color roles, type scale, spacing scale, radii, elevation and motion durations, with the reduced-motion override. Put the resets, typography defaults and focus-visible ring in `base.scss`. Set up fonts as the direction says; prefer self-hosted `@fontsource/*` packages over a third-party CDN.
- [ ] **Step 2:** Build the shell: a "Main" navigation with the three links, the user's name and "Sign out" (`AuthFlow.signOut()`, then navigate to `/sign-in`), the toast outlet and the router outlet. Build the primitives listed above. Build the not-found page.
- [ ] **Step 3:** Add the shell parent route and the `**` route to `app.routes.ts`. Use the target shape in Part 1's Task 11 Step 6, with an empty `children` for now.
- [ ] **Step 4:** Run `errors.spec.ts`'s "unknown address" test. Expected: PASS. The other specs still fail.
- [ ] **Step 5: Commit:** `feat(portal): add the design tokens, shell and shared UI primitives`

### Task 14: Sign in and sign up

**Files:** Create `portal/src/app/features/auth/sign-in/sign-in.{ts,html,scss}` and `portal/src/app/features/auth/sign-up/sign-up.{ts,html,scss}`; add both routes.

Behavior:
- **Sign in.** Typed reactive form with "Email" (`required`, `email`) and "Password" (`required`). On submit, `AuthFlow.signIn`, then navigate to `safeReturnUrl(returnUrl query param)`. While the request runs, the button is disabled and says so accessibly. An `AuthenticationError` shows its detail ("Invalid credentials" or "User is not active") in the error alert. The notice comes from `SessionStore.endReason()`:
  - `expired` or `rejected` → "Your session expired. Sign in again.";
  - `account-deleted` → "Your account was deleted.";
  - `signed-out` → none.
- **Sign up.**
  - Fields and validators: "Username" (`required`, 3–50 characters); "Email" (`required`, `email`, at most 100 characters); "Phone" (`required`, `PHONE_PATTERN` → error key `phone`); "Password" (`required`, `passwordValidator`).
  - The "Password rules" list updates live from `unmetPasswordRules`. Each item says "met" or "not met" in visually hidden text.
  - On submit with an invalid form: mark all fields touched, and focus the first invalid field.
  - On submit with a valid form: `AuthFlow.signUp`, then navigate to `/sales`.
  - A 409 `BusinessRuleViolation` shows its detail. A `ValidationError`'s field errors go onto the matching controls; its unmatched messages go into the alert.

- [ ] **Step 1:** Craft floor, brief, direction contract.
- [ ] **Step 2:** Build both screens.
- [ ] **Step 3:** Run `rtk npx playwright test e2e/specs/auth.spec.ts --project=e2e`. Expected: 3 passed ("the password rules…", "signing up twice…", "a wrong password…"). The other 4 land on `/sales` or `/sales/new`, and pass in Tasks 15 and 17.
- [ ] **Step 4: Commit:** `feat(portal): sign up and sign in`

### Task 15: Sales list: table, paging and ordering

**Files:** Create `portal/src/app/features/sales/list/sales-list.{ts,html,scss}` and `portal/src/app/features/sales/list/pagination/pagination.{ts,html,scss}`; add the `sales` route.

Behavior:
- **The query comes from the URL.** The list reads `ActivatedRoute.queryParams` → `queryFromParams` → a `query` signal, and every query change calls `SalesApi.list(queryToHttpParams(query))`. Every user action builds a new `SalesQuery` and calls `router.navigate([], { queryParams: queryToParams(next) })`: the URL is the single source of truth.
- **Row content.** Each row shows the sale number (a link to `/sales/:id`), the date (`dateTime` pipe), the customer, the branch, the items as "{active} of {total}" (or just the count when none is cancelled), the total (`money` pipe, right-aligned, tabular numerals) and a status badge.
- **Sorting.**
  - Header buttons call `toggleSort(order, field, event.shiftKey)`, and the page resets to 1.
  - `aria-sort` goes on the primary column only.
  - The secondary rank and direction go into the button's `aria-describedby` text.
- **Paging.** "Previous page" and "Next page" are disabled at the ends. "Rows per page" changes the size and resets the page to 1.
- **States.** Loading keeps the previous rows visible, with a busy indicator and `aria-busy` on the table. There are two empty states (no filters vs. filters). Errors use the error alert, with "Try again" re-running the query.
- **Toasts after navigation.** A toast set before a navigation (for example "Sale … deleted") is shown after it.

- [ ] **Step 1:** Craft floor, brief, direction contract.
- [ ] **Step 2:** Build the list and pagination.
- [ ] **Step 3:** Run `list.spec.ts` and `auth.spec.ts`. Expected: all pass except auth's "signing in returns to the page the user asked for", which needs `/sales/new` (Task 17).
- [ ] **Step 4: Commit:** `feat(portal): list sales with paging and ordering`

### Task 16: Sales list: filters

**Files:** Create `portal/src/app/features/sales/list/sales-filters/sales-filters.{ts,html,scss}`.

Behavior:
- **A `search` landmark** named "Filters", holding a typed reactive form initialized from the current `SalesQuery`.
- **Applying.** "Apply filters" (or Enter) emits a new query with page 1, and the list navigates. "Clear filters" navigates to `/sales` with no parameters, keeping only a non-default size.
- **Client-side checks, before any request.** Minimum total must not be above maximum total, with the error "Minimum total must not be above maximum total.". The same goes for Sold from/Sold to, with "Sold from must not be after Sold to.". Totals must be numbers ≥ 0, and ids must match `GUID_PATTERN`.
- **More filters.** "More filters" toggles `aria-expanded` and reveals Customer ID and Branch ID. It starts expanded if either is set in the URL.
- **Hints.** A short hint near the text fields says that matching is "contains" and that `*` anchors the match. The date fields say they use the browser's time zone.

- [ ] **Step 1:** Craft floor, brief, direction contract.
- [ ] **Step 2:** Build the filters.
- [ ] **Step 3:** Run `filters.spec.ts`. Expected: 8 passed.
- [ ] **Step 4: Commit:** `feat(portal): filter the sales list`

### Task 17: Sale editor: create

**Files:** Create `portal/src/app/features/sales/editor/sale-editor.{ts,html,scss}`, `portal/src/app/features/sales/editor/party-picker/party-picker.{ts,html,scss}` (Customer and Branch: a catalog select plus "Other …") and `portal/src/app/features/sales/editor/sale-line/sale-line.{ts,html,scss}`; add the `sales/new` and `sales/:id/edit` routes (edit mode arrives in Task 19).

Behavior:
- **Form.** `createSaleForm(fb)`.
- **Party picker.**
  - Choosing a catalog entry sets `{id, name}`.
  - "Other customer…" reveals name and ID fields. The ID is prefilled with `crypto.randomUUID()` and is editable.
  - When editing a sale whose customer isn't in the catalog, the picker starts on "Other …" with the sale's values.
- **Line product select.** Choosing a catalog product sets its id and name, and sets the unit price to the catalog price when the price is empty.
- **Line preview.**
  - Each line is a `computed` from its controls' `valueChanges`, converted with `toSignal`, through `previewLine`.
  - It shows "{p}% off" or "No discount", the line total, and `nextTier`'s hint.
  - The Summary shows `previewSale`, labeled as a preview.
- **Submit.**
  - With an invalid form: mark all fields touched; show an error summary (`role="alert"`) listing each problem as a link to its field; focus the summary. A repeated product shows "Each product can appear only once in a sale." at the list level.
  - With a valid form: `SalesApi.create(toCreateRequest(form))`, then the toast "Sale {n} created" and navigation to `/sales/{id}`.
  - A `ValidationError` goes through `applyServerErrors`; unmatched messages go to the summary. A 409 (a taken number) shows the API's detail next to "Sale number" and in the alert.
- **Discard.** Asks for confirmation only if the form is dirty.

- [ ] **Step 1:** Craft floor, brief, direction contract.
- [ ] **Step 2:** Build the editor for create.
- [ ] **Step 3:** Run `create.spec.ts` and auth's "signing in returns to the page the user asked for". Expected: auth passes, and 4 of create's 6 pass. "previews 10% off…" and "accepts a customer outside the catalog" end on the detail page and pass in Task 18.
- [ ] **Step 4: Commit:** `feat(portal): create a sale with a live discount preview`

### Task 18: Sale detail: read, cancel an item, cancel, delete

**Files:** Create `portal/src/app/features/sales/detail/sale-detail.{ts,html,scss}`; add the `sales/:id` route.

Behavior:
- **Loading.** `withComponentInputBinding` gives the `id` input, and the page loads `SalesApi.get(id)`. A 404 shows the "Sale not found" state.
- **Content.** The header, status and description list. The items table has cancelled rows visibly de-emphasized, and "Cancelled" in the Status column. The Totals region shows subtotal (the sum of active gross), discounts and total: the server's `totalAmount` for the total, and the line fields for the rest.
- **Actions.** Each action opens `ConfirmDialog` with the contract's copy.
  - **Cancel item:** the last-item warning appears when exactly one active line is left.
  - **Cancel sale.**
  - **Delete sale.**

  On confirm, the page calls the API, replaces the sale with the response (cancel and cancel item return the sale) and shows the toast. Delete navigates to `/sales` with the toast "Sale {n} deleted".
- **Errors.** A 409 `BusinessRuleViolation` shows its detail. A `ConcurrencyConflict` shows the error alert with "Reload sale", which re-fetches the sale.
- **Cancelled sales.** The read-only notice shows, and there's no Edit, Cancel or Cancel item.
- **Arriving from the editor for a cancelled sale.** The notice is visible there too (Task 19 redirects here).

- [ ] **Step 1:** Craft floor, brief, direction contract.
- [ ] **Step 2:** Build the detail page.
- [ ] **Step 3:** Run `lifecycle.spec.ts` and `create.spec.ts`. Expected: all pass.
- [ ] **Step 4: Commit:** `feat(portal): show a sale and cancel, cancel items or delete it`

### Task 19: Sale editor: update, then Account

**Files:** Modify `portal/src/app/features/sales/editor/sale-editor.*`. Create `portal/src/app/features/account/account.{ts,html,scss}`, and add the `account` route.

Behavior:
- **Edit mode** (when the route has an `id`).
  - Load the sale. If it's cancelled, `router.navigate(['/sales', id], { replaceUrl: true })`: the detail page shows the read-only notice.
  - Otherwise, `patchFromSale`. The h1 is "Edit sale {n}", and "Sale number" is read-only.
  - A "Cancelled lines" region lists the sale's cancelled lines as history (product, quantity, total).
  - Under the lines, a note explains R10: removing a line cancels it on save, and cancelled lines stay as history.
  - Submit calls `SalesApi.update(id, toUpdateRequest(form))`, then shows the toast "Changes saved" and navigates to `/sales/{id}`.
- **Account.**
  - Loads `UsersApi.get(session.userId)` and shows the description list.
  - "Delete account" opens the dialog. Confirming runs `AuthFlow.deleteAccount()` and navigates to `/sign-in`, where the notice comes from `endReason`.
  - A 404, meaning the user is already gone, ends the session the same way.

- [ ] **Step 1:** Craft floor, brief, direction contract.
- [ ] **Step 2:** Build edit mode and Account.
- [ ] **Step 3:** Run `update.spec.ts`, `account.spec.ts` and `errors.spec.ts`. Expected: all pass. Then the whole suite: `rtk npm run e2e`. Expected: every test passes in both `e2e` and `e2e-mobile`.
- [ ] **Step 4: Commit:** `feat(portal): update a sale and manage the account`

---

## Phase 4: Quality pass

### Task 20: Detect, critique, audit, then one polish batch

**Files:** whatever the fixes touch under `portal/src/`.

- [ ] **Step 1: Mechanical detector.** Run once:
`"C:/Users/pr000/.claude/plugins/cache/impeccable/impeccable/4.4.0/skills/impeccable/scripts/impeccable" detect --json portal/src`
Save the findings.
- [ ] **Step 2: One batched visual round.** With `docker compose up -d` running, run `cd portal && rtk npm run shots`. It writes 16 screenshots (8 screens × 1280/390) to `portal/e2e/shots/.output/`. Run impeccable `critique` and `audit` against them and the source: accessibility, responsive layout, performance and the anti-patterns list. Also run `rtk npm run build` and note the bundle sizes for the audit.
- [ ] **Step 3: One fix batch.** Read the craft floor. Fix every material finding from Steps 1–2 in one pass, with impeccable `polish` guiding the details. The accessibility contract doesn't change. If a finding needs a contract change, update the table and the specs together.
- [ ] **Step 4: One confirm round.** Re-run `detect`, `shots`, `rtk npm run test:ci` and `rtk npm run e2e`. Expected:
  - no new detector findings above the level the audit accepted;
  - unit and e2e tests all green.
  
  Stop here even if small nits remain. List them in the pull request.
- [ ] **Step 5: Commit:** `style(portal): apply the design review fixes`

### Task 21: Finish review and DESIGN.md

**Files:** Create `DESIGN.md` and its sidecar (paths from impeccable). Other files only if the review finds material fixes.

- [ ] **Step 1:** Dispatch the `impeccable:impeccable-finish-reviewer` subagent with:
  - the direction contract (Part 1's Task 5; path recorded in Task 11C);
  - the screenshot folder from Task 20;
  - `portal/src`.

  It returns an ordered list of material fixes.
- [ ] **Step 2:** Apply the material fixes (craft floor first). Re-run the affected specs and `rtk npm run e2e` once.
- [ ] **Step 3:** Dispatch the `impeccable:impeccable-documenter` subagent to write DESIGN.md and its sidecar from the shipped build.
- [ ] **Step 4: Commit:** `docs(portal): record the design system` (plus `fix(portal): apply the finish review` first, if Step 2 changed code).

---

## Phase 5: Demo videos for the README

### Task 22: The demo harness

**Files:** Create `portal/e2e/demos/support/demo.ts`.

- [ ] **Step 1: Write the harness**

`portal/e2e/demos/support/demo.ts`:
```ts
import { expect, type Locator, type Page, test as base } from '@playwright/test';
import { mkdirSync } from 'node:fs';
import path from 'node:path';
import { ApiClient, TEST_PASSWORD, type TestUser } from '../../support/api';
import { useSession } from '../../support/fixtures';

export const RAW_VIDEO_DIR = path.resolve(process.cwd(), 'e2e', 'demos', '.output', 'raw');

/** Ana is the account demos 02–06 use. Bruno is demo 01's fresh-database canary. Carla signs up through the UI in demo 01. */
export const ANA: TestUser = { username: 'Ana Souza', email: 'ana.souza@developerstore.test', phone: '+5511987654321', password: TEST_PASSWORD };
export const BRUNO: TestUser = { username: 'Bruno Lima', email: 'bruno.lima@developerstore.test', phone: '+5521998877665', password: TEST_PASSWORD };
export const CARLA: TestUser = { username: 'Carla Mendes', email: 'carla.mendes@developerstore.test', phone: '+5531991234567', password: TEST_PASSWORD };

/** A visible cursor and a caption bar. Injected only into demo runs; the portal never ships them. */
function installOverlay(): void {
  const install = (): void => {
    if (document.getElementById('demo-cursor')) return;
    const style = document.createElement('style');
    style.textContent = `
      #demo-cursor{position:fixed;left:-40px;top:-40px;width:22px;height:22px;margin:-11px 0 0 -11px;border-radius:50%;
        background:rgba(20,20,20,.35);border:2px solid #fff;box-shadow:0 1px 4px rgba(0,0,0,.45);pointer-events:none;
        z-index:2147483647;transition:transform .12s ease-out}
      #demo-cursor.down{transform:scale(.7)}
      #demo-caption{position:fixed;left:50%;bottom:28px;transform:translateX(-50%);max-width:78%;padding:10px 18px;
        border-radius:10px;background:rgba(15,15,15,.86);color:#fff;font:500 18px/1.4 system-ui,sans-serif;text-align:center;
        pointer-events:none;z-index:2147483646;opacity:0;transition:opacity .25s}
      #demo-caption.on{opacity:1}`;
    document.head.append(style);
    const cursor = Object.assign(document.createElement('div'), { id: 'demo-cursor' });
    const caption = Object.assign(document.createElement('div'), { id: 'demo-caption' });
    document.body.append(cursor, caption);
    const last = sessionStorage.getItem('demo-cursor');
    if (last) [cursor.style.left, cursor.style.top] = last.split(',');
    document.addEventListener('mousemove', event => {
      cursor.style.left = `${event.clientX}px`;
      cursor.style.top = `${event.clientY}px`;
      sessionStorage.setItem('demo-cursor', `${cursor.style.left},${cursor.style.top}`);
    }, true);
    document.addEventListener('mousedown', () => cursor.classList.add('down'), true);
    document.addEventListener('mouseup', () => cursor.classList.remove('down'), true);
    (window as unknown as { __demoCaption: (text: string) => void }).__demoCaption = text => {
      caption.textContent = text;
      caption.classList.toggle('on', text !== '');
    };
  };
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', install);
  else install();
}

/** Human-paced actions. The waits are the point here; they never belong in e2e specs. */
export class Demo {
  constructor(readonly page: Page) {}

  async caption(text: string, holdMs = 1800): Promise<void> {
    await this.page.evaluate(value => (window as unknown as { __demoCaption?: (t: string) => void }).__demoCaption?.(value), text);
    await this.page.waitForTimeout(holdMs);
  }

  async hideCaption(): Promise<void> {
    await this.caption('', 200);
  }

  async pointAt(target: Locator): Promise<void> {
    await target.scrollIntoViewIfNeeded();
    const box = await target.boundingBox();
    if (!box) throw new Error('The demo target is not visible.');
    await this.page.mouse.move(box.x + box.width / 2, box.y + box.height / 2, { steps: 24 });
    await this.page.waitForTimeout(180);
  }

  async click(target: Locator, modifiers: ('Shift')[] = []): Promise<void> {
    await this.pointAt(target);
    await target.click({ modifiers });
    await this.page.waitForTimeout(350);
  }

  async type(target: Locator, text: string): Promise<void> {
    await this.click(target);
    await target.clear();
    await target.pressSequentially(text, { delay: 55 });
    await this.page.waitForTimeout(250);
  }

  async select(target: Locator, label: string): Promise<void> {
    await this.pointAt(target);
    await target.selectOption({ label });
    await this.page.waitForTimeout(400);
  }

  async beat(ms = 800): Promise<void> {
    await this.page.waitForTimeout(ms);
  }
}

export const test = base.extend<{ api: ApiClient; demo: Demo }>({
  api: async ({}, use) => {
    const api = await ApiClient.create();
    await use(api);
    await api.dispose();
  },
  demo: async ({ page }, use, testInfo) => {
    await page.addInitScript(installOverlay);
    await use(new Demo(page));
    const video = page.video();
    await page.close();
    if (video && testInfo.status === 'passed') {
      mkdirSync(RAW_VIDEO_DIR, { recursive: true });
      await video.saveAs(path.join(RAW_VIDEO_DIR, `${path.basename(testInfo.file, '.demo.ts')}.webm`));
    }
  },
});

/** Signs Ana in through the API (creating her on a fresh database) and opens the page as her. */
export async function signInAsAna(page: Page, api: ApiClient): Promise<void> {
  const status = await api.signUp(ANA);
  expect([201, 409]).toContain(status);
  await useSession(page, await api.signIn(ANA));
}

export { expect };
```

- [ ] **Step 2:** Run `cd portal && rtk npx tsc -p e2e/tsconfig.json`. Expected: no errors.
- [ ] **Step 3: Commit:** `test(portal): add the demo recording harness`

### Task 23: Record the six demos

**Files:** Create `portal/e2e/demos/01-sign-up-and-sign-in.demo.ts` through `06-delete-sale.demo.ts`.

Each demo is one `test()` that runs 15–40 seconds. Captions are short sentences that name the business rule. Write them as below.

- [ ] **Step 1: Write the demos**

`portal/e2e/demos/01-sign-up-and-sign-in.demo.ts`:
```ts
import { BRUNO, CARLA, expect, test } from './support/demo';

test('sign up and sign in', async ({ demo, page, api }) => {
  // Recordings start from an empty database, so sale numbers begin at S-000001 (Decision 16).
  // Bruno is the canary: his sign-up only succeeds on a fresh database.
  expect(await api.signUp(BRUNO), 'Reset first: docker compose down -v && docker compose up -d --build').toBe(201);

  await page.goto('/sign-up');
  await demo.caption('Anyone can create an account. Public sign-up makes Customer accounts.');
  await demo.type(page.getByLabel('Username'), CARLA.username);
  await demo.type(page.getByLabel('Email'), CARLA.email);
  await demo.type(page.getByLabel('Phone'), CARLA.phone);
  await demo.caption('The password rules check as you type, before anything is sent.', 1200);
  await demo.type(page.getByLabel('Password', { exact: true }), CARLA.password);
  await demo.beat(900);
  await demo.click(page.getByRole('button', { name: 'Create account' }));

  await expect(page.getByRole('heading', { level: 1, name: 'Sales', exact: true })).toBeVisible();
  await demo.caption('Signed in with a JWT. Every sales request carries it.');
  await demo.click(page.getByRole('button', { name: 'Sign out' }));

  await demo.type(page.getByLabel('Email'), CARLA.email);
  await demo.type(page.getByLabel('Password', { exact: true }), CARLA.password);
  await demo.click(page.getByRole('button', { name: 'Sign in' }));
  await expect(page.getByRole('heading', { level: 1, name: 'Sales', exact: true })).toBeVisible();
  await demo.caption('Back in. Tokens last 8 hours.', 1600);
});
```

`portal/e2e/demos/02-create-a-sale.demo.ts`:
```ts
import { BRANCHES, CUSTOMERS, PRODUCTS } from '../../src/app/core/catalog/demo-catalog';
import { expect, signInAsAna, test } from './support/demo';

test('create a sale', async ({ demo, page, api }) => {
  await signInAsAna(page, api);
  await page.goto('/sales/new');

  await demo.caption('A new sale: customer, branch and lines.');
  await demo.select(page.getByLabel('Customer', { exact: true }), CUSTOMERS[3].name);
  await demo.select(page.getByLabel('Branch', { exact: true }), BRANCHES[1].name);

  const first = page.getByRole('group', { name: 'Line 1' });
  await demo.select(first.getByLabel('Product', { exact: true }), PRODUCTS[0].name);
  await demo.type(first.getByLabel('Quantity'), '3');
  await demo.caption('Discounts follow the quantity of each product. 3 items: no discount yet.');
  await demo.type(first.getByLabel('Quantity'), '4');
  await demo.caption('4 to 9 identical items get 10% off.');
  await demo.type(first.getByLabel('Quantity'), '12');
  await demo.caption('10 to 20 get 20% off.');
  await demo.type(first.getByLabel('Quantity'), '21');
  await demo.caption('Above 20 identical items isn\'t allowed.');
  await demo.type(first.getByLabel('Quantity'), '12');

  await demo.click(page.getByRole('button', { name: 'Add line' }));
  const second = page.getByRole('group', { name: 'Line 2' });
  await demo.select(second.getByLabel('Product', { exact: true }), PRODUCTS[3].name);
  await demo.type(second.getByLabel('Quantity'), '2');
  await demo.pointAt(page.getByRole('region', { name: 'Summary' }));
  await demo.caption('The summary previews the totals. The API computes the final numbers.');

  await demo.caption('Leave the sale number blank and the API issues the next one.', 1500);
  await demo.click(page.getByRole('button', { name: 'Create sale' }));
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Sale S-000001');
  await demo.caption('Saved as S-000001, with the discounts the API applied.', 2200);
});
```

`portal/e2e/demos/03-browse-sales.demo.ts`:
```ts
import { BRANCHES, CUSTOMERS, PRODUCTS } from '../../src/app/core/catalog/demo-catalog';
import { line } from '../support/sales';
import { expect, signInAsAna, test } from './support/demo';

test('browse sales: paging, sorting and filters', async ({ demo, page, api }) => {
  await signInAsAna(page, api);
  for (let i = 0; i < 18; i++) {
    const customer = CUSTOMERS[i % CUSTOMERS.length];
    const branch = BRANCHES[i % BRANCHES.length];
    const sale = await api.createSale({
      saleDate: `2026-09-${String(1 + i).padStart(2, '0')}T${String(9 + (i % 9)).padStart(2, '0')}:15:00-03:00`,
      customerId: customer.id, customerName: customer.name, branchId: branch.id, branchName: branch.name,
      items: [line(PRODUCTS[i % 8], 1 + ((i * 7) % 20)), line(PRODUCTS[(i + 3) % 8], 1 + (i % 5))],
    });
    if (i % 6 === 5) await api.cancelSale(sale.id);
  }

  await page.goto('/sales');
  await demo.caption('Newest first, 10 per page.');
  await demo.click(page.getByRole('button', { name: 'Next page' }));
  await demo.caption('Page 2.', 1000);

  const header = (name: string) => page.getByRole('columnheader', { name }).getByRole('button', { name });
  await demo.click(header('Total'));
  await demo.caption('Sort by any column. Money and dates start with the highest.');
  await demo.click(header('Customer'));
  await demo.click(header('Total'), ['Shift']);
  await demo.caption('Shift+click adds a second sort: by customer, then by total.');

  const filters = page.getByRole('search', { name: 'Filters' });
  await demo.type(filters.getByLabel('Customer', { exact: true }), 'silva');
  await demo.click(filters.getByRole('radio', { name: 'Open' }));
  await demo.type(filters.getByLabel('Minimum total'), '50');
  await demo.click(filters.getByRole('button', { name: 'Apply filters' }));
  await demo.caption('Filters combine. Text matches anywhere; a * anchors the start or end.');
  await demo.pointAt(page.getByRole('table', { name: 'Sales' }));
  await demo.caption('The address bar keeps the filters, so a view can be shared or reloaded.', 2000);

  await demo.click(filters.getByRole('button', { name: 'Clear filters' }));
  await expect(page).toHaveURL(/\/sales$/);
  await demo.beat(1000);
});
```

`portal/e2e/demos/04-update-a-sale.demo.ts`:
```ts
import { BRANCHES, CUSTOMERS, PRODUCTS } from '../../src/app/core/catalog/demo-catalog';
import { line } from '../support/sales';
import { expect, signInAsAna, test } from './support/demo';

test('update a sale', async ({ demo, page, api }) => {
  await signInAsAna(page, api);
  const sale = await api.createSale({
    saleDate: '2026-09-27T10:00:00-03:00', customerId: CUSTOMERS[4].id, customerName: CUSTOMERS[4].name,
    branchId: BRANCHES[0].id, branchName: BRANCHES[0].name, items: [line(PRODUCTS[1], 5), line(PRODUCTS[4], 3)],
  });

  await page.goto(`/sales/${sale.id}`);
  await demo.caption('Editing replaces the sale\'s header and lines.');
  await demo.click(page.getByRole('link', { name: 'Edit sale' }));

  await demo.type(page.getByRole('group', { name: 'Line 1' }).getByLabel('Quantity'), '10');
  await demo.caption('Change a quantity and the discount updates: 10 items get 20% off.');
  await demo.click(page.getByRole('button', { name: 'Remove line 2' }));
  await demo.caption('Removing a line cancels it on save. Cancelled lines stay as history.');
  await demo.click(page.getByRole('button', { name: 'Add line' }));
  const added = page.getByRole('group', { name: 'Line 2' });
  await demo.select(added.getByLabel('Product', { exact: true }), PRODUCTS[6].name);
  await demo.type(added.getByLabel('Quantity'), '4');

  await demo.click(page.getByRole('button', { name: 'Save changes' }));
  await expect(page.getByRole('status')).toContainText('Changes saved');
  await demo.pointAt(page.getByRole('table', { name: 'Items' }));
  await demo.caption('The removed product is still listed, marked Cancelled. The number never changes.', 2400);
});
```

`portal/e2e/demos/05-cancel-items-and-sales.demo.ts`:
```ts
import { BRANCHES, CUSTOMERS, PRODUCTS } from '../../src/app/core/catalog/demo-catalog';
import { line } from '../support/sales';
import { expect, signInAsAna, test } from './support/demo';

test('cancel items and sales', async ({ demo, page, api }) => {
  await signInAsAna(page, api);
  const base = {
    saleDate: '2026-09-27T15:00:00-03:00', customerId: CUSTOMERS[5].id, customerName: CUSTOMERS[5].name,
    branchId: BRANCHES[2].id, branchName: BRANCHES[2].name,
  };
  const twoLines = await api.createSale({ ...base, items: [line(PRODUCTS[0], 6), line(PRODUCTS[2], 2)] });

  await page.goto(`/sales/${twoLines.id}`);
  await demo.caption('Cancel one line and it leaves the total.');
  await demo.click(page.getByRole('button', { name: `Cancel item ${PRODUCTS[2].name}` }));
  await demo.click(page.getByRole('alertdialog').getByRole('button', { name: 'Cancel item' }));
  await expect(page.getByRole('status')).toContainText('Item cancelled');
  await demo.pointAt(page.getByRole('region', { name: 'Totals' }));
  await demo.beat(1200);

  await demo.caption('Cancel the last active line and the sale is cancelled too.');
  await demo.click(page.getByRole('button', { name: `Cancel item ${PRODUCTS[0].name}` }));
  await demo.pointAt(page.getByRole('alertdialog'));
  await demo.beat(1400);
  await demo.click(page.getByRole('alertdialog').getByRole('button', { name: 'Cancel item' }));
  await demo.caption('A cancelled sale is read-only. It can still be deleted.', 2200);

  const whole = await api.createSale({ ...base, items: [line(PRODUCTS[5], 2)] });
  await page.goto(`/sales/${whole.id}`);
  await demo.click(page.getByRole('button', { name: 'Cancel sale' }));
  await demo.caption('Cancelling a sale keeps its lines and total as the record.', 1800);
  await demo.click(page.getByRole('alertdialog').getByRole('button', { name: 'Cancel sale' }));
  await expect(page.getByRole('status')).toContainText('cancelled');
  await demo.beat(1500);
});
```

`portal/e2e/demos/06-delete-sale.demo.ts`:
```ts
import { BRANCHES, CUSTOMERS, PRODUCTS } from '../../src/app/core/catalog/demo-catalog';
import { line } from '../support/sales';
import { expect, signInAsAna, test } from './support/demo';

test('delete a sale', async ({ demo, page, api }) => {
  await signInAsAna(page, api);
  const sale = await api.createSale({
    saleNumber: 'BR-2026-0001', saleDate: '2026-09-28T09:30:00-03:00',
    customerId: CUSTOMERS[6].id, customerName: CUSTOMERS[6].name,
    branchId: BRANCHES[3].id, branchName: BRANCHES[3].name, items: [line(PRODUCTS[7], 8)],
  });

  await page.goto(`/sales/${sale.id}`);
  await demo.caption('A sale entered by mistake can be deleted.');
  await demo.click(page.getByRole('button', { name: 'Delete sale' }));
  await demo.pointAt(page.getByRole('alertdialog'));
  await demo.beat(1400);
  await demo.click(page.getByRole('alertdialog').getByRole('button', { name: 'Delete sale' }));
  await expect(page).toHaveURL(/\/sales$/);
  await demo.caption('It disappears from every list and search. The row stays in the database.');

  await demo.click(page.getByRole('link', { name: 'New sale' }));
  await demo.caption('Its number stays taken. Try to reuse it:', 1400);
  await demo.type(page.getByLabel('Sale number'), 'BR-2026-0001');
  await demo.select(page.getByLabel('Customer', { exact: true }), CUSTOMERS[6].name);
  await demo.select(page.getByLabel('Branch', { exact: true }), BRANCHES[3].name);
  await demo.select(page.getByRole('group', { name: 'Line 1' }).getByLabel('Product', { exact: true }), PRODUCTS[7].name);
  await demo.click(page.getByRole('button', { name: 'Create sale' }));
  await expect(page.getByRole('alert')).toContainText('Sale number BR-2026-0001 already exists');
  await demo.caption('Rejected: sale numbers are unique, deleted sales included.', 2400);
});
```

- [ ] **Step 2: Record on a fresh database**

Run from the repo root: `docker compose down -v && docker compose up -d --build`. Then: `cd portal && rtk npm run demo:record`
Expected:
- 6 passed;
- `portal/e2e/demos/.output/raw/` holds six `.webm` files, `01-sign-up-and-sign-in.webm` through `06-delete-sale.webm`.

Watch each one once. If a caption is cut off, a wait is too short, or the cursor misses a target, fix it and re-record them all on a fresh database. Do at most two re-record rounds, following the bounded-passes rule.

- [ ] **Step 3: Commit** the scripts, not the recordings (they're git-ignored): `test(portal): script the six feature demos`

### Task 24: Export MP4 and GIF

**Files:** Create `portal/scripts/export-demo-media.mjs` and `docs/media/portal/*.{mp4,gif}`.

- [ ] **Step 1: Write the exporter**

`portal/scripts/export-demo-media.mjs`:
```js
// Turns the demo recordings into README media: an H.264 MP4 (quality) and a palette GIF (plays inline on GitHub).
import { spawnSync } from 'node:child_process';
import { existsSync, mkdirSync, readdirSync, statSync } from 'node:fs';
import path from 'node:path';

const rawDir = path.resolve('e2e/demos/.output/raw');
const outDir = path.resolve('../docs/media/portal');
const gifBudget = 8 * 1024 * 1024;

function ffmpeg(args) {
  const result = spawnSync('ffmpeg', ['-hide_banner', '-loglevel', 'error', '-y', ...args], { stdio: 'inherit' });
  if (result.status !== 0) {
    throw new Error(`ffmpeg failed: ffmpeg ${args.join(' ')}`);
  }
}

const megabytes = file => `${(statSync(file).size / 1048576).toFixed(1)} MB`;

if (!existsSync(rawDir)) {
  console.error(`No recordings in ${rawDir}. Run npm run demo:record first.`);
  process.exit(1);
}
mkdirSync(outDir, { recursive: true });

let overBudget = false;
for (const file of readdirSync(rawDir).filter(name => name.endsWith('.webm')).sort()) {
  const slug = path.basename(file, '.webm');
  const input = path.join(rawDir, file);
  const mp4 = path.join(outDir, `${slug}.mp4`);
  const gif = path.join(outDir, `${slug}.gif`);

  ffmpeg(['-i', input, '-vf', 'fps=30,format=yuv420p', '-c:v', 'libx264', '-preset', 'slow', '-crf', '24',
    '-movflags', '+faststart', '-an', mp4]);
  ffmpeg(['-i', input, '-vf',
    'fps=10,scale=960:-1:flags=lanczos,split[a][b];[a]palettegen=stats_mode=diff[p];[b][p]paletteuse=dither=bayer:bayer_scale=5:diff_mode=rectangle',
    '-loop', '0', gif]);

  console.log(`${slug}: mp4 ${megabytes(mp4)}, gif ${megabytes(gif)}`);
  if (statSync(gif).size > gifBudget) {
    overBudget = true;
    console.error(`${slug}.gif is over 8 MB: shorten the demo or its pauses, then record again.`);
  }
}
process.exit(overBudget ? 1 : 0);
```

- [ ] **Step 2: Export**

Run: `cd portal && rtk npm run demo:export`
Expected: six lines like `02-create-a-sale: mp4 1.9 MB, gif 5.4 MB`, and exit code 0. Over budget: shorten that demo (fewer or shorter captions), re-record everything on a fresh database, and export again.

- [ ] **Step 3: Check the GIFs**

Open two GIFs in the browser. Check that the captions are readable at 960 px and the cursor is visible.

- [ ] **Step 4: Commit**

```bash
rtk git add portal/scripts/export-demo-media.mjs docs/media/portal
rtk git commit -m "docs(portal): add the feature demo videos"
```

---

## Phase 6: Ship

### Task 25: Serve the portal from `docker compose`

**Files:** Create `portal/Dockerfile`, `portal/nginx.conf` and `portal/.dockerignore`. Modify `docker-compose.yml`.

- [ ] **Step 1: Write the image**

`portal/Dockerfile`:
```dockerfile
FROM node:24-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npx ng build --configuration production

FROM nginx:1.29-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/portal/browser /usr/share/nginx/html
EXPOSE 80
```

`portal/nginx.conf`:
```nginx
server {
    listen 80;
    root /usr/share/nginx/html;

    # Same origin for the browser: the API needs no CORS (Decision 2).
    location /api/ {
        proxy_pass http://ambev.developerevaluation.webapi:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location = /index.html {
        add_header Cache-Control "no-cache";
    }

    location ~* \.(?:js|css|woff2|svg|png|ico)$ {
        add_header Cache-Control "public, max-age=31536000, immutable";
        try_files $uri =404;
    }

    location / {
        try_files $uri $uri/ /index.html;
    }
}
```

`portal/.dockerignore`:
```
node_modules
dist
.angular
test-results
playwright-report
blob-report
e2e/**/.output
```

- [ ] **Step 2: Add the service** to `docker-compose.yml`, after the API service, with the same indentation:
```yaml
  ambev.developerevaluation.portal:
    container_name: ambev_developer_evaluation_portal
    build:
      context: ./portal
    ports:
      - "8081:80"
    depends_on:
      - ambev.developerevaluation.webapi
```

- [ ] **Step 3: Run the e2e suite against the production build**

Run from the repo root: `docker compose down -v && docker compose up -d --build`. Then: `cd portal && PORTAL_URL=http://localhost:8081 rtk npm run e2e`
Expected:
- every test passes;
- a direct load of `http://localhost:8081/sales/new` returns the app (the SPA fallback), not a 404.

Then run `docker compose down -v`.

- [ ] **Step 4: Commit**

```bash
rtk git add portal/Dockerfile portal/nginx.conf portal/.dockerignore docker-compose.yml
rtk git commit -m "build(portal): serve the portal from docker compose on port 8081"
```

### Task 26: CI: the end-to-end job

**Files:** Modify `.github/workflows/pull-request.yml`. Part 1's Task 11A already added the `portal` job (build and unit tests); this task adds only `portal-e2e`.

- [ ] **Step 1: Check the current `actions/setup-node` major**

Run: `rtk gh api repos/actions/setup-node/releases/latest --jq .tag_name`. Use that major below; the plan assumes `v6`.

- [ ] **Step 2: Add the `portal-e2e` job**, after Part 1's `portal` job, with the same indentation. Use the same `actions/checkout` and `actions/setup-node` majors the `portal` job uses:
```yaml
  portal-e2e:
    name: Portal end-to-end tests
    runs-on: ubuntu-latest
    timeout-minutes: 30
    steps:
      - uses: actions/checkout@v7

      - uses: actions/setup-node@v6
        with:
          node-version: 24
          cache: npm
          cache-dependency-path: portal/package-lock.json

      # The API, PostgreSQL and the nginx-served portal, as a reviewer runs them.
      - name: Start the stack
        run: docker compose up -d --build --wait

      - name: Install
        working-directory: portal
        run: |
          npm ci
          npx playwright install --with-deps chromium

      - name: Test
        working-directory: portal
        run: npm run e2e
        env:
          PORTAL_URL: http://localhost:8081
          API_URL: http://localhost:8080

      - name: Upload the Playwright report
        if: ${{ !cancelled() }}
        uses: actions/upload-artifact@v7
        with:
          name: playwright-report
          path: portal/playwright-report
          if-no-files-found: warn

      - name: Stop the stack
        if: always()
        run: docker compose down -v
```

- [ ] **Step 3: Check the YAML parses**

Run: `python -c "import yaml,sys;yaml.safe_load(open('.github/workflows/pull-request.yml'));print('ok')"`
Expected: `ok`.

- [ ] **Step 4: Commit**

```bash
rtk git add .github/workflows/pull-request.yml
rtk git commit -m "ci: end-to-end test the portal"
```

### Task 27: README: the Portal section

**Files:** Modify `README.md`.

- [ ] **Step 1: Check ticket 13 has landed**

Run: `rtk git log develop --oneline -- README.md | head -3` and `grep -c '^## ' README.md`.
Expected: a `docs: write project README` commit, and several `##` sections. If the README is still the 4-line stub, **stop and ask the user** whether to wait for ticket 13 or write the section now (Order with other tickets).

- [ ] **Step 2: Add a "Portal" section**

Put it after the section on running the project, and add it to the contents list if the README has one. Read `README.md` first, then Edit. The section content:

````markdown
## Portal

An Angular 21 client that exposes every API feature. `docker compose up -d --build` serves it at <http://localhost:8081>, next to the API on `:8080`. nginx forwards `/api` to the API, so the browser stays on one origin and the API needs no CORS. For development, run `npm ci && npm start` in `portal/`; the dev server proxies `/api` to the compose API.

Customers, branches and products are external identities in the API, so the portal offers a small demo catalog to pick from, and also accepts any other name and ID.

### Sign up and sign in
![Signing up, signing out and signing in](docs/media/portal/01-sign-up-and-sign-in.gif)
Public sign-up creates active Customer accounts; the JWT from `POST /api/auth` goes on every sales request. [MP4](docs/media/portal/01-sign-up-and-sign-in.mp4)

### Create a sale
![Creating a sale while the discount tiers change](docs/media/portal/02-create-a-sale.gif)
The editor previews the quantity discounts (4–9 identical items: 10%, 10–20: 20%, above 20: rejected); the API computes the saved amounts and issues the sale number. [MP4](docs/media/portal/02-create-a-sale.mp4)

### Browse sales
![Paging, sorting and filtering the sales list](docs/media/portal/03-browse-sales.gif)
Paging, sorting by one or more columns, and every filter the API offers. The URL keeps the view. [MP4](docs/media/portal/03-browse-sales.mp4)

### Update a sale
![Editing a sale: a re-priced line, a removed line, a new line](docs/media/portal/04-update-a-sale.gif)
Lines are reconciled by product: a removed line is cancelled and kept as history. [MP4](docs/media/portal/04-update-a-sale.mp4)

### Cancel items and sales
![Cancelling an item, the last item, and a whole sale](docs/media/portal/05-cancel-items-and-sales.gif)
Cancelling the last active item cancels the sale; a cancelled sale is read-only. [MP4](docs/media/portal/05-cancel-items-and-sales.mp4)

### Delete a sale
![Deleting a sale, then trying to reuse its number](docs/media/portal/06-delete-sale.gif)
A soft delete: the sale disappears everywhere, and its number stays taken. [MP4](docs/media/portal/06-delete-sale.mp4)

### Portal tests

Run from `portal/`, with `docker compose up -d --build` running for the end-to-end tests:

| Command | What it runs |
|---|---|
| `npm run test:ci` | Vitest unit tests: the error mapper, the list query, pricing, the session and the sale form |
| `npm run e2e` | Playwright against the real API, on desktop and phone viewports, with axe accessibility checks |
| `npm run demo:record` then `npm run demo:export` | Re-records the videos above. Start from a fresh database: `docker compose down -v && docker compose up -d --build` |

The domain events aren't exposed over HTTP. To watch them, run `docker compose logs -f ambev.developerevaluation.webapi` while you use the portal.
````

- [ ] **Step 3: Check the links resolve**

Run: `grep -o 'docs/media/portal/[^)]*' README.md | while read f; do test -f "$f" || echo "missing $f"; done`
Expected: no output.

- [ ] **Step 4: Commit**

```bash
rtk git add README.md
rtk git commit -m "docs: add the portal and its demos to the README"
```

### Task 28: Verify everything, review, then open a pull request into `develop`

- [ ] **Step 1: Full verification from a clean state**

Run from the repo root:
```bash
docker compose down -v && docker compose up -d --build
cd portal && rtk npm ci && rtk npm run build && rtk npm run test:ci
PORTAL_URL=http://localhost:8081 rtk npm run e2e
cd .. && rtk dotnet test Ambev.DeveloperEvaluation.sln
docker compose down -v
```
Expected:
- the build succeeds;
- the unit tests all pass, at or above Task 11C's recorded baseline;
- every e2e test passes in `e2e` and `e2e-mobile`;
- the .NET suites are unchanged and pass.

- [ ] **Step 2: Check the branch holds only what it should**

Run: `rtk git diff --stat develop...HEAD -- . ':!portal' ':!docs/media/portal'`
Expected: only `docker-compose.yml`, `.github/workflows/pull-request.yml`, `README.md`, DESIGN.md and its sidecar, and this plan. PRODUCT.md, the shape brief and the direction files came with Part 1 and change only if Task 21 found a material reason. No `.cs`, `.csproj` or `appsettings` files.

- [ ] **Step 3: Code review**

Use `superpowers:requesting-code-review` against `develop...HEAD`, with the focus areas:
- the contract versus the built screens;
- error handling;
- the session and 401 flow;
- the return-URL safety;
- nothing visual hard-coded outside the tokens.

Fix what's confirmed, re-run the affected tests, and commit (`fix(portal): ...`).

- [ ] **Step 4: Push and open the pull request** (`superpowers:finishing-a-development-branch`, with the option already chosen)

```bash
rtk git push -u origin feature/portal-screens
rtk gh pr create --base develop --title "feat(portal): screens, end-to-end tests and demos for the Sales API client" --body-file /tmp/portal-pr.md
```
Write `/tmp/portal-pr.md` first, with:
- that this is Part 2 of 2 for ticket 14, linking Part 1's pull request;
- what the portal covers (the Feature coverage matrix);
- how to run it;
- the test counts from Step 1;
- the design process: PRODUCT.md, the shape brief, the direction contract and DESIGN.md;
- the nits Task 20 left;
- the embedded `02-create-a-sale.gif`.

No AI attribution.

- [ ] **Step 5: Handoff**

Use `ai-memory-handoff`: "Ticket 14 Part 2 in PR #<n>, waiting for the user's merge. Next: optional release 1.1.0."

---

## Self-review

- **Coverage.** Every row of the Feature coverage matrix has a route (Tasks 14–19), an e2e spec (Task 12) and, except for the Account and error rows, a demo (Task 23).
  - The user's asks are covered across both parts: Angular 21 (Part 1, Task 2); impeccable (Part 1, Tasks 3–5; here, Tasks 13–21); the design pass ("/design", Part 1, Task 5); Playwright tests (Task 12, green through Tasks 13–19); videos reusable in the README (Tasks 22–24, 27).
- **Split.** Nothing here rebuilds Part 1's work. Changes to Part 1's logic go through a failing Vitest case first (Rules). Part 1's `portal` CI job stays; Task 26 only adds `portal-e2e`.
- **Placeholders.**
  - Screen markup and styles are left to the design on purpose (Decision 14). Each screen task instead fixes its behavior, contract and spec.
  - The paths of PRODUCT.md, the brief and the direction files come from Part 1's commits (Task 11C Step 3); DESIGN.md's from impeccable's output (Task 21).
- **Name consistency.** These names are used identically in the specs, the demos and Part 1's code:
  - `SESSION_STORAGE_KEY`, `sessionFromAuthResponse`, `useSession`;
  - `SalesQuery` fields and `queryToParams`/`queryFromParams`/`queryToHttpParams`;
  - `previewLine`/`previewSale`/`nextTier`;
  - `createSaleForm`/`addItem`/`toCreateRequest`/`toUpdateRequest`/`patchFromSale`/`applyServerErrors`;
  - `AuthFlow.signIn`/`signUp`/`signOut`/`deleteAccount`;
  - `formatMoney`;
  - the catalog constants `CUSTOMERS`/`BRANCHES`/`PRODUCTS`.
