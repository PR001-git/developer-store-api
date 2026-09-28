# DeveloperStore Portal, Part 1 of 2: Foundation (Ticket 14a) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Ticket 14 is split in two plans, each ending in its own pull request into `develop`:**

| Part | Plan | Tasks | Branch | Pull request |
|---|---|---|---|---|
| **1: Foundation (this plan)** | `2026-09-28-14a-angular-portal-foundation.md` | 1–11, 11A, 11B | `feature/portal-foundation` | the workspace, the approved design direction and the tested non-visual core |
| 2: Screens and ship | `2026-09-28-14b-angular-portal-screens.md` | 11C, 12–28 | `feature/portal-screens` | Playwright, every screen, the quality pass, the demo videos, compose, CI e2e and the README |

Part 2 starts only after this part's pull request is merged. Task numbers run across both plans, so a reference like "Task 13" always means the same task.

**Goal of the whole ticket:** A signed-in user can do everything the Sales API offers from a browser: sign up, sign in, create a sale and watch the discounts apply, list sales with paging, ordering and every filter, open a sale, update it, cancel an item, cancel the sale, soft-delete it, and view or delete their own account. The UI has a deliberate visual direction set through the impeccable skill and a Claude Design comp, not a default component-library look. Playwright covers every feature end to end against the real API. A separate Playwright project records six narrated demo videos, exported to MP4 and GIF for the README.

**Goal of Part 1:** everything the screens will stand on, merged and green, with no screen built yet:
- the Angular 21 workspace in `portal/`, with the dev proxy and a pinned unit-test time zone;
- the approved design inputs: PRODUCT.md, the shape brief, the visual direction and the Claude Design comp;
- the whole non-visual core, test-first: API models, the error mapper, local-time helpers, the strict parameter codec, the sales-list query, pricing, the session, guards, interceptors, the API services, the auth flow, the demo catalog and the sale form;
- a CI job that builds the portal and runs its unit tests.

The app still renders an empty `<router-outlet />` when this part merges. That's expected: Part 2 adds the screens.

**Architecture:**
- **Location:** a standalone Angular 21 workspace in `portal/` at the repo root. The .NET solution doesn't change: no C#, no CORS, no new endpoints.
- **Talking to the API:** the browser only calls relative `/api/...` URLs. In development, `ng serve` proxies `/api` to `http://localhost:8080` (the compose API). In `docker compose` (Part 2), an nginx container serves the production build on `:8081` and reverse-proxies `/api` to the API container. The browser never makes a cross-origin call, so the API needs no CORS.
- **Layers inside `portal/src/app`:**
  - `core/` holds the non-visual code: API models, the error mapper, HTTP services, interceptors, session and guards, local-time helpers and the demo catalog. It's all written test-first in Phase 2, before any screen exists.
  - `features/` holds one folder per screen. This part writes only the pure logic: the list query, the pricing preview and the sale form, all unit-tested.
  - `shared/` holds formatting and form error copy. The UI primitives come in Part 2.
- **State:** Angular signals plus services, zoneless (the Angular 21 default). The sales list keeps all its state in the URL: paging, ordering and filters are query parameters named after the API's own parameters.
- **Tests in this part:** Vitest (Angular 21's default runner) for the logic. Playwright arrives in Part 2.

**Tech Stack:** Node 24.21 / npm 11.19; Angular 21.2.x (`@angular/cli`, `@angular/build`, `@angular/core`, `@angular/router`, `@angular/forms` with typed reactive forms), `@angular/cdk` 21.2.x; Vitest 5 with jsdom (as `ng new` installs it); Docker Desktop with Compose v2 (Task 1 only); the existing API, `docker-compose.yml` and `.github/workflows/pull-request.yml`.

**Source:** there's no ticket file yet. The requirements are the user's request (Angular 21, every back-end feature, impeccable plus a design pass so the UI isn't AI slop, Playwright tests and reusable videos) and the back end as tickets 04–12 left it. The spec, `docs/superpowers/specs/2026-09-24-sales-api-design.md`, is the reference for the rules the UI explains: R1–R14, D3 (quantity 4 gets 10%), D7, D13 and §5.3.

**Not rehearsed.** The versions and CLI flags were checked on 2026-09-28 (`npm view`, `ng new --help` for 21.2). The API shapes come from reading `SalesController`, `UsersController`, `AuthController`, their DTOs, `ExceptionHandlingMiddleware` and `JwtTokenGenerator`. Nothing was run. If an output differs, use superpowers:systematic-debugging before changing code.

---

## Rules for every agent executing this plan

Restate these to every subagent you dispatch.

- **No AI attribution anywhere.** No `Co-Authored-By` trailer, "Generated with" line or session link in commits, merge commits or the pull request. Use the commit messages below exactly. `ng new` runs with `--ai-config=none` so it writes no agent files.
- **Git Bash**, from `/c/Users/pr000/orca/developer-store-api`. Each Bash call starts a fresh shell, so `cd portal &&` goes at the front of every portal command.
- **Prefix commands with `rtk`** (project CLAUDE.md): `rtk npm ...`, `rtk npx ...`, `rtk git ...`. When you need the full output (a failing Vitest run), use `rtk proxy <cmd>`.
- **Write files with the Write and Edit tools**, not heredocs. Write needs an earlier Read of the same file.
- **Main checkout only, never a worktree.** `.claude/` (the attribution guard and project skills) is untracked and exists only here.
- **Stage explicit paths only.** Never `git add -A`, `git add .`, `git commit -a` or `git clean`. `portal/` is new, so `git add portal/<path>` per task, never the whole folder at once. `node_modules/`, `dist/` and `.angular/` must never be staged.
- **No C#, migration or `appsettings*.json` changes.** The only edits outside `portal/` in this part are `.github/workflows/pull-request.yml` (Task 11A), the design documents impeccable writes (Tasks 3–5) and the two plan files.
- **Stay inside Part 1.** Don't build screens, install Playwright or touch `docker-compose.yml`; those are Part 2's.
- **Design gates are real stops.** Tasks 3, 4 and 5 end at a user decision. Ask with AskUserQuestion, or the decision page impeccable offers, and wait. Don't infer approval.
- **Docker must be running** for Task 1. Start the stack with `docker compose up -d --build` from the repo root. Don't install or configure Docker or WSL.
- **Stop everything you start** before a task ends: `ng serve` (Ctrl-C, or `taskkill //F //IM node.exe` only if you started it and nothing else uses Node), and `docker compose down`.
- **Don't run `npm audit fix --force`** or upgrade Angular past 21.x.

## Skills

### Design skills

| Skill or tool | Use it? | When and how |
|---|---|---|
| `impeccable:impeccable` | **Yes: Tasks 3–5** | Task 3: `init` writes PRODUCT.md from an interview. Task 4: `shape` produces the UX brief (information architecture, flows, states). Task 5: `new-work` sets the visual world, then one `critique` of the comps. Mode: **Operate** (app UI; scanability and consistency outrank expression). |
| Claude Design (the Artifact tool's **Design** type): the user's "/design" | **Yes: Task 5** | No `/design` skill is installed. This plan reads "/design" as the Claude Design canvas. `Artifact` with `action: "quickstart"` and `intent: "design"` returns the Design type and design systems. The agent then publishes a Design artifact with comps of three screens in the direction impeccable chose. The approved comp is the reference Part 2 builds against and its finish reviewer checks against. |
| `impeccable:impeccable-asset-producer` (subagent) | Only if Task 5's direction needs raster assets | For example, an empty-state illustration. Most Operate UIs need none. |
| `dataviz` | No | The API has no aggregate endpoint, so the portal has no charts (Decision 9). |

### Process skills (superpowers plugin)

- `superpowers:subagent-driven-development` or `superpowers:executing-plans` runs the plan. **Tasks 3–5 run in the main session**, not in a subagent, because they need the user.
- `superpowers:test-driven-development`: Phase 2's logic tasks are strict red-green; the plan gives the failing test first.
- `superpowers:systematic-debugging` whenever an output differs from this plan.
- `superpowers:verification-before-completion` at the end of every task, and in full in Task 11B.
- `superpowers:requesting-code-review` in Task 11B.
- `superpowers:finishing-a-development-branch` in Task 11B, with the option already chosen: push and open a pull request into `develop`.
- Don't use `superpowers:using-git-worktrees` (Rules) or `superpowers:brainstorming`: the scope is set here, and the design discovery happens inside impeccable.

### Project skills in `.claude/skills/`

All ten are .NET skills (`dotnet-best-practices`, `efcore-patterns`, `testcontainers-integration-tests`, `dotnet-slopwatch`, and so on). **None apply**, because this plan writes no C#. The ai-memory skills are the exception:

| Skill | Use it? | When |
|---|---|---|
| `ai-memory-handoff` | **Yes, at each design gate (Tasks 3–5) and at Task 11B** | Save a handoff naming the last completed task, the gate's outcome (for example, the approved comp's URL) and the next task. |
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
    - The Accessibility contract table in Part 2's plan fixes every heading, label, button and landmark name the specs use.
    - The design owns everything visual. The contract owns names and roles.
    - If Task 4's `shape` needs to change a name, it updates that table in Part 2's plan. No spec exists yet, so nothing else changes.
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

## Where the rest of the ticket lives

Part 2's plan, `2026-09-28-14b-angular-portal-screens.md`, holds three sections this part reads but doesn't own:
- the **Feature coverage matrix** (every back-end feature, its route, e2e spec and demo);
- the **Accessibility contract** (every heading, label, button and landmark name the screens and specs use);
- the **per-screen behavior** in Tasks 13–19.

Task 4 feeds all three to impeccable's `shape`, and edits the contract there if the brief renames anything. Tasks 3 and 5 may change the Decisions, which are copied in both plans: edit both.

## Order with other tickets

- **Blocked by 12** (soft delete), which gives the last endpoint. Task 1 checks that `develop` has `DELETE /api/sales/{id}`.
- **Blocks Part 2.** Part 2's Task 11C starts from `develop` after this part's pull request is merged.
- Ticket 13 (README and release 1.0.0) doesn't affect this part. Only Part 2's Task 27 depends on it.

## Pre-existing output (leave it alone)

- The API logs `Failed to determine the https port for redirect.`
- `npm ci`/`npm install` may print `npm warn deprecated` lines from transitive dependencies. `npm audit` may report advisories. Record them, and don't `--force` anything.
- Git prints `LF will be replaced by CRLF` when adding files.
- `ng build` may warn that the initial bundle is near its budget. The budgets stay as `ng new` wrote them.

## File map

| Change | Paths |
|---|---|
| Created: workspace | `portal/` from `ng new` (`angular.json`, `package.json`, `package-lock.json`, `tsconfig*.json`, `src/index.html`, `src/main.ts`, `src/styles.scss`, `public/`) |
| Created: config | `portal/proxy.conf.json`, `portal/src/test-setup.ts` |
| Created: core | `portal/src/app/core/api/{api-models,api-error,strict-parameter-codec,interceptors,auth-api,users-api,sales-api}.ts`; `portal/src/app/core/auth/{session,session-store,guards,credentials,auth-flow}.ts`; `portal/src/app/core/time/local-time.ts`; `portal/src/app/core/catalog/demo-catalog.ts`; a `.spec.ts` next to each file that has logic |
| Created: shared | `portal/src/app/shared/format.ts`, `portal/src/app/shared/pipes.ts`, `portal/src/app/shared/forms/field-errors.ts` |
| Created: features (logic only) | `portal/src/app/features/sales/{sales-query,pricing}.ts`; `portal/src/app/features/sales/editor/sale-form.ts`; `portal/src/app/layout/title-strategy.ts` |
| Created: testing | `portal/src/app/testing/{memory-storage,fake-jwt,sale-fixture}.ts` |
| Created: design docs | `PRODUCT.md`, the shape brief and the direction files, at the paths impeccable reports |
| Modified | `.github/workflows/pull-request.yml` (the `portal` job) |
| Committed on `develop` first | both plan files |
| Local only | `portal/node_modules/`, `portal/dist/`, `portal/.angular/` |

.NET test deltas: none.

---

## Phase 1: Start, scaffold, and set the design direction

### Task 1: Check the starting point, commit this plan, branch

**Files:** none changed except this plan.

- [ ] **Step 1: Check the tools**

Run: `node -v && npm -v && ffmpeg -version | head -1 && docker compose version`
Expected: `v24.x`, `11.x`, `ffmpeg version 8.x`, `Docker Compose version v2.x`.

- [ ] **Step 2: Check `develop` has every endpoint**

Run:
```bash
rtk git fetch origin && rtk git checkout develop && rtk git pull --ff-only
grep -n 'HttpDelete("{id}")\|HttpPatch("{id}/items/{itemId}/cancel")\|HttpPut("{id}")' src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs
```
Expected: three matches. If `HttpDelete` is missing, ticket 12 isn't merged: stop and tell the user.

- [ ] **Step 3: Run the API and check the three facts the client depends on**

Run from the repo root: `docker compose up -d --build`, then:
```bash
until curl -sf http://localhost:8080/health/ready >/dev/null; do sleep 2; done
EMAIL="plan14.$(date +%s)@developerstore.test"
curl -s -X POST http://localhost:8080/api/users -H 'Content-Type: application/json' \
  -d "{\"username\":\"plan14\",\"email\":\"$EMAIL\",\"phone\":\"+5511987654321\",\"password\":\"Portal@2026\",\"status\":\"Active\",\"role\":\"Customer\"}"
TOKEN=$(curl -s -X POST http://localhost:8080/api/auth -H 'Content-Type: application/json' \
  -d "{\"email\":\"$EMAIL\",\"password\":\"Portal@2026\"}" | python -c 'import sys,json;print(json.load(sys.stdin)["data"]["token"])')
echo "$TOKEN" | cut -d. -f2 | python -c 'import sys,base64,json;s=sys.stdin.read().strip();print(json.loads(base64.urlsafe_b64decode(s+"="*(-len(s)%4))))'
curl -s "http://localhost:8080/api/sales?_size=1" -H "Authorization: Bearer $TOKEN"
```
Expected:
- the sign-up returns `"success":true`;
- the decoded payload has `nameid` (a GUID), `unique_name`, `role` and `exp`;
- the list returns `"data":[...]` as a JSON **array**, plus `currentPage`, `totalPages` and `totalItems`.

If the claim is named differently, note it: `session.ts` (Task 10) already accepts `nameid`, `sub` and the long URI form. Then run `docker compose down`.

- [ ] **Step 4: Commit both plans on `develop` and push**

```bash
rtk git add docs/superpowers/plans/2026-09-28-14a-angular-portal-foundation.md docs/superpowers/plans/2026-09-28-14b-angular-portal-screens.md
rtk git commit -m "docs: add the two-part plan for the Angular portal"
rtk git push origin develop
```

- [ ] **Step 5: Branch**

Run: `rtk git checkout -b feature/portal-foundation`
Expected: `Switched to a new branch 'feature/portal-foundation'`.

---

### Task 2: Scaffold the Angular 21 workspace

**Files:**
- Create: `portal/` (from `ng new`), `portal/proxy.conf.json`, `portal/src/test-setup.ts`
- Modify: `portal/angular.json`, `portal/package.json`, `portal/tsconfig.spec.json`, `portal/.gitignore`, `portal/src/app/app.ts`, `portal/src/app/app.html`, `portal/src/index.html`
- Delete: `portal/src/app/app.spec.ts` (the generated "Hello" test)

- [ ] **Step 1: Generate the workspace**

Run from the repo root:
```bash
npx -y @angular/cli@21.2 new portal --directory portal --style=scss --routing --ssr=false \
  --zoneless --test-runner=vitest --ai-config=none --commit=false --package-manager=npm --defaults
```
Expected: `✔ Packages installed successfully.` Then check `ls -a portal`. If there's a `portal/.git` directory, it's a nested repository created by the CLI: run `rm -rf portal/.git`, after checking `git -C portal log` shows no commits or only the CLI's initial one.

- [ ] **Step 2: Pin Angular to 21.2 and add the CDK**

Run: `cd portal && rtk npm install @angular/cdk@~21.2.14 --save-exact=false`
Then open `portal/package.json` and change every `^21.` Angular range to `~21.` (`@angular/*`, `@angular/build`, `@angular/cli`), so an install never jumps to 22.
Run: `cd portal && rtk npm install`
Expected: no errors. `npm ls @angular/core` shows `21.2.x`.

- [ ] **Step 3: Add the dev proxy**

Create `portal/proxy.conf.json`:
```json
{
  "/api": {
    "target": "http://localhost:8080",
    "secure": false,
    "changeOrigin": true
  }
}
```
In `portal/angular.json`, under `projects.portal.architect.serve`, add `"options": { "proxyConfig": "proxy.conf.json" }`. Merge it with any existing `options`.

- [ ] **Step 4: Pin the unit-test time zone**

Create `portal/src/test-setup.ts`:
```ts
// Local-time helpers are tested against a fixed zone: São Paulo is UTC-03:00 all year (no DST since 2019).
process.env['TZ'] = 'America/Sao_Paulo';
```
- In `portal/angular.json`, under `projects.portal.architect.test.options`, add `"setupFiles": ["src/test-setup.ts"]`.
- In `portal/tsconfig.spec.json`, add `"src/test-setup.ts"` to `include` (or `files`), and add `"node"` to `compilerOptions.types` so `process` type-checks. If `npm ls @types/node` finds nothing, run `cd portal && rtk npm install -D @types/node@24` first.

If the builder rejects `setupFiles`, remove it and instead change the `test` script to `cross-env TZ=America/Sao_Paulo ng test`, after `npm install -D cross-env`. Task 8's canary test tells you which way worked.

- [ ] **Step 5: Replace the starter page and set the document basics**

- Delete `portal/src/app/app.spec.ts`.
- Replace `portal/src/app/app.html` with the single line `<router-outlet />`.
- In `portal/src/app/app.ts`, keep the generated component but make sure `imports: [RouterOutlet]` is its only import, and remove the `title` signal.
- In `portal/src/index.html`, set `<html lang="en">` and `<title>DeveloperStore</title>`.

- [ ] **Step 6: Add the scripts**

In `portal/package.json` `scripts`, set:
```json
"start": "ng serve",
"build": "ng build",
"test": "ng test",
"test:ci": "ng test --watch=false"
```
Part 2's Task 12 adds the Playwright scripts and ignores, together with Playwright itself.

- [ ] **Step 7: Check that build, tests and ignores work**

Run: `cd portal && rtk npm run build && rtk npm run test:ci`
Expected:
- the build succeeds and writes `dist/portal/browser/`;
- Vitest reports `No test files found` or 0 tests. A non-zero exit here only because there are no tests is fine; Task 6 adds the first test.

Then run from the repo root:
`git check-ignore -v portal/src/app/app.ts portal/package-lock.json portal/node_modules/.package-lock.json`
Expected: only the `node_modules` path is ignored. If the root `.gitignore` (Visual Studio template) ignores anything else under `portal/`, add a negation to `portal/.gitignore` and note it in the commit body.

- [ ] **Step 8: Commit**

```bash
rtk git add portal/angular.json portal/package.json portal/package-lock.json portal/tsconfig.json portal/tsconfig.app.json \
  portal/tsconfig.spec.json portal/.gitignore portal/.editorconfig portal/README.md portal/proxy.conf.json portal/public \
  portal/src/index.html portal/src/main.ts portal/src/styles.scss portal/src/test-setup.ts \
  portal/src/app/app.ts portal/src/app/app.html portal/src/app/app.scss portal/src/app/app.config.ts portal/src/app/app.routes.ts
rtk git status --short portal
rtk git commit -m "chore(portal): scaffold the Angular 21 workspace"
```
`git status --short portal` must show nothing left untracked except ignored folders. If `ng new` created a file not listed here, such as `.vscode/`, stage it only if it's project configuration. Leave editor settings out.

---

### Task 3: Capture the product context (impeccable `init`) — GATE

**Runs in the main session.** It needs the user.

**Files:** Create `PRODUCT.md` at the path impeccable reports.

- [ ] **Step 1: Load impeccable and resolve context**

Invoke the Skill `impeccable:impeccable` with args `init`. As its Setup says, run once:
`"C:/Users/pr000/.claude/plugins/cache/impeccable/impeccable/4.4.0/skills/impeccable/scripts/impeccable" context --target portal/src/app`
Record `productPath` and `projectRoot` from `RESOLVED_CONTEXT`. On 2026-09-28, before `portal/` existed, it reported `projectRoot` as the repo root and `NO_PRODUCT_MD`.

- [ ] **Step 2: Run the init interview**

Follow `reference/init.md`. Give the interview these known facts, so it only asks what's open:
- **What:** the DeveloperStore portal, a browser client for a prototype Sales API built for the Ambev developer evaluation.
- **Who:**
  - first, the evaluation's reviewers: developers who want to see every API feature working and understand the business rules;
  - in the story, sales staff at a beverage distributor's branches who record and correct sales.
- **Jobs:** record a sale fast and trust its discounts; find a sale by number, customer, branch, date or total; correct or cancel a sale or an item; remove a sale entered by mistake.
- **Constraints:**
  - No Ambev name, logo or brand colors: the product is "DeveloperStore", and imitating a real brand isn't acceptable.
  - English copy.
  - Money in BRL (Decision 12).
  - Desktop first, fully usable on a phone.
  - WCAG 2.2 AA.
- **Surfaces:** the screens in the accessibility contract. **Mode: Operate.**

Open questions for the user:
- tone;
- light, dark or both;
- any aesthetic references or anti-references;
- confirming BRL/`en-US`.

- [ ] **Step 3: Gate: the user approves PRODUCT.md**

Show the file. Apply the user's edits. Don't continue until they approve.

- [ ] **Step 4: Save a handoff and commit**

- Use `ai-memory-handoff` to record: "Task 3 done: PRODUCT.md approved at <path>. Next: Task 4 (shape)."
- If the user changed the currency or locale, update Decision 12 in both plan files now.
```bash
rtk git add <productPath> docs/superpowers/plans/2026-09-28-14a-angular-portal-foundation.md docs/superpowers/plans/2026-09-28-14b-angular-portal-screens.md
rtk git commit -m "docs(portal): capture the product context"
```

---

### Task 4: Shape the UX (impeccable `shape`) — GATE

**Runs in the main session.**

**Files:** Create the shape brief at the path impeccable reports. Possibly modify the Accessibility contract in Part 2's plan.

- [ ] **Step 1: Run `shape`**

Invoke `impeccable:impeccable` with args `shape portal`. Load `reference/shape.md` and follow it. Give it these inputs:
- the Feature coverage matrix (Part 2's plan);
- the Accessibility contract (Part 2's plan);
- the per-screen behavior in Tasks 13–19 (Part 2's plan).

Read only those sections of Part 2's plan, not the whole file.

The brief must settle:
- navigation;
- the list's filter layout (always visible or collapsible; how "More filters" discloses);
- how the editor lays out lines and the live pricing preview;
- how the detail page separates active and cancelled lines;
- confirmation-dialog patterns;
- toasts and inline errors;
- empty, loading and error states for every screen;
- phone layouts (the list table at 390 px).

- [ ] **Step 2: Reconcile the contract**

If the brief renames or restructures anything in the contract, edit the Accessibility contract table in Part 2's plan now, and the matching names in its Tasks 13–19. Task 12 writes the specs from the table, so nothing else needs to change yet.

- [ ] **Step 3: Gate: the user approves the brief**

- [ ] **Step 4: Handoff and commit**

```bash
rtk git add <shape brief path> docs/superpowers/plans/2026-09-28-14b-angular-portal-screens.md
rtk git commit -m "docs(portal): shape the portal UX"
```

---

### Task 5: Set the visual direction and approve a Claude Design comp — GATE

**Runs in the main session.**

**Files:** Create whatever direction files `new-work` writes (surface brief, direction contract). Comps live in a Claude Design artifact, not in the repo.

- [ ] **Step 1: Choose the world with `new-work`**

Load impeccable's `reference/new-work.md` and follow it for the portal surface, with PRODUCT.md and the shape brief loaded. It proposes directions and the user picks one. It owns the typography, color, spacing, density, shape language and motion stance. Hold the choice to these domain facts:
- money right-aligned in tabular numerals;
- status shown by text and shape, never by color alone;
- a dense but calm table;
- a pricing preview that reads at a glance.

- [ ] **Step 2: Make the comps in Claude Design ("/design")**

Call `Artifact` with `action: "quickstart"` and `intent: "design"`. Use the Design type it names (and a design system if one fits the chosen direction, or `design_systems: false` if not). Publish a Design artifact titled "DeveloperStore Portal comps" with three screens in the chosen direction, using real catalog data and not lorem ipsum:
1. **Sales list** at 1280 px with filters, a sorted column, a cancelled row and pagination, plus the same list at 390 px.
2. **Sale editor** with three lines showing all three tiers (2 items: no discount and "Add 2 more for 10% off"; 5 items: 10%; 12 items: 20%) and the summary.
3. **Sale detail** of a sale with one cancelled line, and the "Cancel this item?" dialog open.

Show light and dark only if PRODUCT.md includes both.

- [ ] **Step 3: One critique round, then the gate**

- Run impeccable `critique` on the comps once. Fix what it finds in one batch.
- Show the user the Design artifact link. The user approves, or asks for one revision round.
- Record the approved artifact URL at the top of this task, as "Approved comp: <url>".

- [ ] **Step 4: Handoff and commit**

Handoff: "Task 5 done: direction <name>, comp <url>. Next: Task 6."
```bash
rtk git add <direction files> docs/superpowers/plans/2026-09-28-14a-angular-portal-foundation.md
rtk git commit -m "docs(portal): set the visual direction"
```

---

## Phase 2: The non-visual core, test-first

Every task here follows the same loop:
1. Write the spec.
2. Run `cd portal && rtk npm run test:ci`, and see the new tests fail. The first run fails on the missing module.
3. Write the code.
4. Run it again and see everything pass.
5. Commit.

Test files use explicit imports from `vitest`, so they don't depend on the builder's globals setting.

### Task 6: API models and the error mapper

**Files:**
- Create: `portal/src/app/core/api/api-models.ts`, `portal/src/app/core/api/api-error.ts`
- Test: `portal/src/app/core/api/api-error.spec.ts`

- [ ] **Step 1: Write the models** (types only; nothing to test)

`portal/src/app/core/api/api-models.ts`:
```ts
/** Response and request shapes of the Sales API, as its controllers and DTOs define them. Enums travel as strings. */

export type UserRole = 'None' | 'Customer' | 'Manager' | 'Admin';
export type UserStatus = 'Unknown' | 'Active' | 'Inactive' | 'Suspended';

export interface ApiResponse {
  readonly success: boolean;
  readonly message: string;
}

export interface ApiResponseWithData<T> extends ApiResponse {
  readonly data: T;
}

export interface PaginatedResponse<T> extends ApiResponseWithData<T[]> {
  readonly currentPage: number;
  readonly totalPages: number;
  readonly totalItems: number;
}

export interface ApiErrorBody {
  readonly type: string;
  readonly error: string;
  readonly detail: string;
}

export interface AuthenticateRequest {
  readonly email: string;
  readonly password: string;
}

export interface AuthenticateResponse {
  readonly token: string;
  readonly email: string;
  readonly name: string;
  readonly role: string;
}

export interface CreateUserRequest {
  readonly username: string;
  readonly password: string;
  readonly phone: string;
  readonly email: string;
  readonly status: UserStatus;
  readonly role: UserRole;
}

export interface User {
  readonly id: string;
  readonly name: string;
  readonly email: string;
  readonly phone: string;
  readonly role: UserRole;
  readonly status: UserStatus;
}

export interface SaleItemRequest {
  readonly productId: string;
  readonly productName: string;
  readonly quantity: number;
  readonly unitPrice: number;
}

export interface UpdateSaleRequest {
  readonly saleDate: string;
  readonly customerId: string;
  readonly customerName: string;
  readonly branchId: string;
  readonly branchName: string;
  readonly items: readonly SaleItemRequest[];
}

export interface CreateSaleRequest extends UpdateSaleRequest {
  readonly saleNumber?: string;
}

export interface SaleItem {
  readonly id: string;
  readonly productId: string;
  readonly productName: string;
  readonly quantity: number;
  readonly unitPrice: number;
  readonly discountPercentage: number;
  readonly discountAmount: number;
  readonly totalAmount: number;
  readonly isCancelled: boolean;
}

export interface Sale {
  readonly id: string;
  readonly saleNumber: string;
  readonly saleDate: string;
  readonly customerId: string;
  readonly customerName: string;
  readonly branchId: string;
  readonly branchName: string;
  readonly totalAmount: number;
  readonly isCancelled: boolean;
  readonly createdAt: string;
  readonly updatedAt: string | null;
  readonly items: readonly SaleItem[];
}

export interface SalesPage {
  readonly sales: readonly Sale[];
  readonly currentPage: number;
  readonly totalPages: number;
  readonly totalItems: number;
}
```

- [ ] **Step 2: Write the failing test**

`portal/src/app/core/api/api-error.spec.ts`:
```ts
import { HttpErrorResponse } from '@angular/common/http';
import { describe, expect, it } from 'vitest';
import { ApiError, parseValidationDetail, toApiError, toFieldPath } from './api-error';

describe('toApiError', () => {
  it('reads the {type, error, detail} body of an API error', () => {
    const error = toApiError(new HttpErrorResponse({
      status: 404,
      error: { type: 'ResourceNotFound', error: 'Resource not found', detail: 'Sale with ID 42 not found' },
    }));

    expect(error).toBeInstanceOf(ApiError);
    expect(error.status).toBe(404);
    expect(error.type).toBe('ResourceNotFound');
    expect(error.title).toBe('Resource not found');
    expect(error.detail).toBe('Sale with ID 42 not found');
    expect(error.fieldErrors).toEqual({});
  });

  it('splits a ValidationError detail into field paths', () => {
    const error = toApiError(new HttpErrorResponse({
      status: 400,
      error: {
        type: 'ValidationError',
        error: 'Invalid input data',
        detail: "SaleDate: 'Sale Date' must not be empty.; Items[0].Quantity: It's not possible to sell above 20 identical items",
      },
    }));

    expect(error.fieldErrors).toEqual({
      saleDate: ["'Sale Date' must not be empty."],
      'items[0].quantity': ["It's not possible to sell above 20 identical items"],
    });
  });

  it('answers NetworkError when the API cannot be reached', () => {
    const error = toApiError(new HttpErrorResponse({ status: 0 }));

    expect(error.type).toBe('NetworkError');
    expect(error.title).toBe("Can't reach the API");
  });

  it('answers UnexpectedResponse for a body that is not the API error format', () => {
    const error = toApiError(new HttpErrorResponse({
      status: 415,
      error: { title: 'Unsupported Media Type', status: 415 },
    }));

    expect(error.type).toBe('UnexpectedResponse');
    expect(error.detail).toBe('The API answered 415.');
  });
});

describe('parseValidationDetail', () => {
  it('keeps a failure without a property as a form-level message', () => {
    expect(parseValidationDetail('A sale must have at least one item')).toEqual({
      '': ['A sale must have at least one item'],
    });
  });

  it('collects several messages for one property', () => {
    expect(parseValidationDetail('Password: Too short.; Password: Needs a number.')).toEqual({
      password: ['Too short.', 'Needs a number.'],
    });
  });

  it('returns no errors for an empty detail', () => {
    expect(parseValidationDetail('')).toEqual({});
  });
});

describe('toFieldPath', () => {
  it('camel-cases each segment of a FluentValidation property', () => {
    expect(toFieldPath('Items[0].UnitPrice')).toBe('items[0].unitPrice');
  });

  it('drops the $. prefix of a model-binding path', () => {
    expect(toFieldPath('$.items[1].quantity')).toBe('items[1].quantity');
  });
});
```

- [ ] **Step 3: Run it and see it fail**

Run: `cd portal && rtk npm run test:ci`
Expected: FAIL, because `./api-error` can't be resolved.

- [ ] **Step 4: Write the mapper**

`portal/src/app/core/api/api-error.ts`:
```ts
import { HttpErrorResponse } from '@angular/common/http';
import { ApiErrorBody } from './api-models';

/** Messages by field path (`items[0].quantity`); the empty key holds messages that name no field. */
export type FieldErrors = Readonly<Record<string, readonly string[]>>;

/** Every failed API call reaches components as an ApiError, whatever went wrong. */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly type: string,
    readonly title: string,
    readonly detail: string,
    readonly fieldErrors: FieldErrors = {},
  ) {
    super(detail || title);
    this.name = 'ApiError';
  }
}

export function toApiError(response: HttpErrorResponse): ApiError {
  if (response.status === 0) {
    return new ApiError(0, 'NetworkError', "Can't reach the API",
      'Check that the API is running (docker compose up) and try again.');
  }

  const body: unknown = response.error;
  if (isApiErrorBody(body)) {
    const fieldErrors = body.type === 'ValidationError' ? parseValidationDetail(body.detail) : {};
    return new ApiError(response.status, body.type, body.error, body.detail, fieldErrors);
  }

  // The framework's own 404, 405 and 415 answers keep ASP.NET Core's default body.
  return new ApiError(response.status, 'UnexpectedResponse', 'Unexpected response',
    `The API answered ${response.status}.`);
}

/** Splits the API's `Property: message; Property: message` detail. */
export function parseValidationDetail(detail: string): FieldErrors {
  const errors: Record<string, string[]> = {};
  for (const part of detail.split('; ').map(text => text.trim()).filter(text => text !== '')) {
    const separator = part.indexOf(': ');
    const property = separator > 0 ? part.slice(0, separator) : '';
    const namesField = property !== '' && !/\s/.test(property);
    const key = namesField ? toFieldPath(property) : '';
    (errors[key] ??= []).push(namesField ? part.slice(separator + 2) : part);
  }
  return errors;
}

export function toFieldPath(property: string): string {
  return property
    .replace(/^\$\./, '')
    .split('.')
    .map(segment => segment.charAt(0).toLowerCase() + segment.slice(1))
    .join('.');
}

function isApiErrorBody(value: unknown): value is ApiErrorBody {
  if (typeof value !== 'object' || value === null) {
    return false;
  }
  const candidate = value as Record<string, unknown>;
  return typeof candidate['type'] === 'string'
    && typeof candidate['error'] === 'string'
    && typeof candidate['detail'] === 'string';
}
```

- [ ] **Step 5: Run the tests and see them pass**

Run: `cd portal && rtk npm run test:ci`
Expected: PASS, 9 tests.

- [ ] **Step 6: Commit**

```bash
rtk git add portal/src/app/core/api/api-models.ts portal/src/app/core/api/api-error.ts portal/src/app/core/api/api-error.spec.ts
rtk git commit -m "feat(portal): map API errors and validation details"
```

---

### Task 7: Local-time helpers and the strict parameter codec

**Files:**
- Create: `portal/src/app/core/time/local-time.ts`, `portal/src/app/core/api/strict-parameter-codec.ts`
- Test: `portal/src/app/core/time/local-time.spec.ts`, `portal/src/app/core/api/strict-parameter-codec.spec.ts`

- [ ] **Step 1: Write the failing tests**

`portal/src/app/core/time/local-time.spec.ts`:
```ts
import { describe, expect, it } from 'vitest';
import { endOfDayIso, fromDateTimeLocal, parseDay, startOfDayIso, toDateTimeLocal, toOffsetIso } from './local-time';

describe('local time (tests run in America/Sao_Paulo, UTC-03:00)', () => {
  it('runs in the pinned zone', () => {
    expect(new Date(2026, 8, 24).getTimezoneOffset()).toBe(180);
  });

  it('writes a local date with its offset', () => {
    expect(toOffsetIso(new Date(2026, 8, 24, 11, 30))).toBe('2026-09-24T11:30:00.000-03:00');
  });

  it('turns a day into its first and last local instant', () => {
    expect(startOfDayIso('2026-09-24')).toBe('2026-09-24T00:00:00.000-03:00');
    expect(endOfDayIso('2026-09-24')).toBe('2026-09-24T23:59:59.999-03:00');
  });

  it('rejects a day that does not exist', () => {
    expect(parseDay('2026-02-30')).toBeNull();
    expect(startOfDayIso('')).toBeNull();
    expect(endOfDayIso('24/09/2026')).toBeNull();
  });

  it('shows a UTC instant as local datetime-local text', () => {
    expect(toDateTimeLocal('2026-09-24T14:30:00Z')).toBe('2026-09-24T11:30');
  });

  it('reads datetime-local text as local time', () => {
    expect(fromDateTimeLocal('2026-09-24T11:30')?.toISOString()).toBe('2026-09-24T14:30:00.000Z');
    expect(fromDateTimeLocal('not a date')).toBeNull();
  });
});
```

`portal/src/app/core/api/strict-parameter-codec.spec.ts`:
```ts
import { HttpParams } from '@angular/common/http';
import { describe, expect, it } from 'vitest';
import { StrictParameterCodec } from './strict-parameter-codec';

describe('StrictParameterCodec', () => {
  it('encodes a plus sign, which the default codec leaves as a space for the server', () => {
    const params = new HttpParams({ encoder: new StrictParameterCodec() })
      .set('_minSaleDate', '2026-09-24T00:00:00.000+05:30');

    expect(params.toString()).toBe('_minSaleDate=2026-09-24T00%3A00%3A00.000%2B05%3A30');
  });

  it('keeps the * wildcard readable', () => {
    const params = new HttpParams({ encoder: new StrictParameterCodec() }).set('customerName', '*silva');

    expect(params.toString()).toBe('customerName=*silva');
  });
});
```

- [ ] **Step 2: Run them and see them fail**

Run: `cd portal && rtk npm run test:ci`
Expected: FAIL on the missing modules. If the zone canary fails once the modules exist, Task 2 Step 4's `setupFiles` didn't take effect: switch to the `cross-env` fallback.

- [ ] **Step 3: Write the code**

`portal/src/app/core/time/local-time.ts`:
```ts
const pad = (value: number, length = 2): string => String(value).padStart(length, '0');

/** `2026-09-24T11:30:00.000-03:00`: the local wall-clock time with its offset, so the API converts it to UTC (R13). */
export function toOffsetIso(date: Date): string {
  const offset = -date.getTimezoneOffset();
  const sign = offset >= 0 ? '+' : '-';
  const absolute = Math.abs(offset);
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
    + `T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}.${pad(date.getMilliseconds(), 3)}`
    + `${sign}${pad(Math.floor(absolute / 60))}:${pad(absolute % 60)}`;
}

/** Reads `YYYY-MM-DD` as local midnight, or null when the text isn't a real day. */
export function parseDay(day: string): Date | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(day);
  if (!match) {
    return null;
  }
  const [year, month, date] = [Number(match[1]), Number(match[2]), Number(match[3])];
  const parsed = new Date(year, month - 1, date);
  return parsed.getMonth() === month - 1 && parsed.getDate() === date ? parsed : null;
}

export function startOfDayIso(day: string): string | null {
  const parsed = parseDay(day);
  return parsed ? toOffsetIso(parsed) : null;
}

export function endOfDayIso(day: string): string | null {
  const parsed = parseDay(day);
  if (!parsed) {
    return null;
  }
  parsed.setHours(23, 59, 59, 999);
  return toOffsetIso(parsed);
}

/** Reads the value of an `<input type="datetime-local">` as local time. */
export function fromDateTimeLocal(value: string): Date | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})$/.exec(value);
  if (!match) {
    return null;
  }
  const [year, month, date, hours, minutes] = match.slice(1).map(Number);
  return new Date(year, month - 1, date, hours, minutes);
}

/** Formats an instant for an `<input type="datetime-local">`, in local time. */
export function toDateTimeLocal(iso: string): string {
  const date = new Date(iso);
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}
```

`portal/src/app/core/api/strict-parameter-codec.ts`:
```ts
import { HttpParameterCodec } from '@angular/common/http';

/**
 * Encodes query keys and values with encodeURIComponent. Angular's default codec leaves `+` as is,
 * and the server reads it as a space, which breaks dates with a positive offset such as `+05:30`.
 */
export class StrictParameterCodec implements HttpParameterCodec {
  encodeKey(key: string): string {
    return encodeURIComponent(key);
  }

  encodeValue(value: string): string {
    return encodeURIComponent(value);
  }

  decodeKey(key: string): string {
    return decodeURIComponent(key);
  }

  decodeValue(value: string): string {
    return decodeURIComponent(value);
  }
}
```

- [ ] **Step 4: Run the tests and see them pass**

Run: `cd portal && rtk npm run test:ci`
Expected: PASS, 17 tests (9 + 8).

- [ ] **Step 5: Commit**

```bash
rtk git add portal/src/app/core/time portal/src/app/core/api/strict-parameter-codec.ts portal/src/app/core/api/strict-parameter-codec.spec.ts
rtk git commit -m "feat(portal): send local dates with their offset and encode + in queries"
```

---

### Task 8: The sales-list query: URL, ordering and API parameters

**Files:**
- Create: `portal/src/app/features/sales/sales-query.ts`
- Test: `portal/src/app/features/sales/sales-query.spec.ts`

- [ ] **Step 1: Write the failing test**

`portal/src/app/features/sales/sales-query.spec.ts`:
```ts
import { describe, expect, it } from 'vitest';
import {
  DEFAULT_QUERY, formatOrder, parseOrder, queryFromParams, queryToHttpParams, queryToParams, toMatchPattern, toggleSort,
} from './sales-query';

describe('parseOrder', () => {
  it('reads the fields and directions of _order', () => {
    expect(parseOrder('totalAmount desc, customerName')).toEqual([
      { field: 'totalAmount', direction: 'desc' },
      { field: 'customerName', direction: 'asc' },
    ]);
  });

  it('matches field names in any case, as the API does', () => {
    expect(parseOrder('SALEDATE ASC')).toEqual([{ field: 'saleDate', direction: 'asc' }]);
  });

  it('falls back to newest first for a missing or unusable order', () => {
    expect(parseOrder(null)).toEqual([{ field: 'saleDate', direction: 'desc' }]);
    expect(parseOrder('items desc, , id')).toEqual([{ field: 'saleDate', direction: 'desc' }]);
  });

  it('drops a repeated field', () => {
    expect(parseOrder('branchName, branchName desc')).toEqual([{ field: 'branchName', direction: 'asc' }]);
  });
});

describe('toggleSort', () => {
  const byDate = [{ field: 'saleDate', direction: 'desc' }] as const;

  it('sorts by a new column, money and dates highest first', () => {
    expect(toggleSort(byDate, 'totalAmount', false)).toEqual([{ field: 'totalAmount', direction: 'desc' }]);
    expect(toggleSort(byDate, 'customerName', false)).toEqual([{ field: 'customerName', direction: 'asc' }]);
  });

  it('flips the primary column', () => {
    expect(toggleSort(byDate, 'saleDate', false)).toEqual([{ field: 'saleDate', direction: 'asc' }]);
  });

  it('adds a secondary sort, then flips it in place', () => {
    const two = toggleSort(byDate, 'branchName', true);
    expect(two).toEqual([{ field: 'saleDate', direction: 'desc' }, { field: 'branchName', direction: 'asc' }]);
    expect(toggleSort(two, 'branchName', true)).toEqual([
      { field: 'saleDate', direction: 'desc' },
      { field: 'branchName', direction: 'desc' },
    ]);
  });
});

describe('the URL form of the query', () => {
  it('omits every default, so /sales stays clean', () => {
    expect(queryToParams(DEFAULT_QUERY)).toEqual({});
  });

  it('round-trips a filtered, sorted page', () => {
    const query = {
      ...DEFAULT_QUERY,
      page: 2,
      size: 20,
      order: parseOrder('totalAmount desc'),
      customerName: 'silva',
      status: 'open' as const,
      soldFrom: '2026-09-01',
      maxTotal: '100',
    };

    const params = queryToParams(query);
    expect(params).toEqual({
      _page: '2', _size: '20', _order: 'totalAmount desc', customerName: 'silva',
      isCancelled: 'false', _minSaleDate: '2026-09-01', _maxTotalAmount: '100',
    });
    expect(queryFromParams(params)).toEqual(query);
  });

  it('repairs a page or size the portal would not offer', () => {
    const query = queryFromParams({ _page: '0', _size: '7' });
    expect(query.page).toBe(1);
    expect(query.size).toBe(10);
  });
});

describe('the API form of the query', () => {
  it('always sends paging and ordering', () => {
    expect(queryToHttpParams(DEFAULT_QUERY).toString()).toBe('_page=1&_size=10&_order=saleDate%20desc');
  });

  it('sends text filters as contains unless the user typed a wildcard', () => {
    expect(toMatchPattern(' silva ')).toBe('*silva*');
    expect(toMatchPattern('S-0001*')).toBe('S-0001*');
    expect(toMatchPattern('   ')).toBe('');
  });

  it('turns local days into offset instants and status into isCancelled', () => {
    const params = queryToHttpParams({
      ...DEFAULT_QUERY, soldFrom: '2026-09-24', soldTo: '2026-09-24', status: 'cancelled', minTotal: '20.00',
    });

    expect(params.get('_minSaleDate')).toBe('2026-09-24T00:00:00.000-03:00');
    expect(params.get('_maxSaleDate')).toBe('2026-09-24T23:59:59.999-03:00');
    expect(params.get('isCancelled')).toBe('true');
    expect(params.get('_minTotalAmount')).toBe('20.00');
    expect(params.has('customerName')).toBe(false);
  });
});

describe('formatOrder', () => {
  it('writes every direction explicitly', () => {
    expect(formatOrder(parseOrder('customerName, totalAmount desc'))).toBe('customerName asc, totalAmount desc');
  });
});
```

- [ ] **Step 2: Run it and see it fail**

Run: `cd portal && rtk npm run test:ci`
Expected: FAIL, because `./sales-query` can't be resolved.

- [ ] **Step 3: Write the code**

`portal/src/app/features/sales/sales-query.ts`:
```ts
import { HttpParams } from '@angular/common/http';
import { Params } from '@angular/router';
import { StrictParameterCodec } from '../../core/api/strict-parameter-codec';
import { endOfDayIso, startOfDayIso } from '../../core/time/local-time';

/** The fields `_order` accepts (the API's SaleOrderParser). Item count isn't one of them. */
export const SORT_FIELDS = ['saleNumber', 'saleDate', 'customerName', 'branchName', 'totalAmount', 'isCancelled'] as const;
export type SortField = (typeof SORT_FIELDS)[number];
export type SortDirection = 'asc' | 'desc';
export interface SortClause {
  readonly field: SortField;
  readonly direction: SortDirection;
}
export type StatusFilter = 'all' | 'open' | 'cancelled';
export const PAGE_SIZES = [10, 20, 50, 100] as const;

/** What the list shows. Text, ids and totals are kept as typed; days are `YYYY-MM-DD` in local time. */
export interface SalesQuery {
  readonly page: number;
  readonly size: number;
  readonly order: readonly SortClause[];
  readonly saleNumber: string;
  readonly customerName: string;
  readonly branchName: string;
  readonly customerId: string;
  readonly branchId: string;
  readonly status: StatusFilter;
  readonly soldFrom: string;
  readonly soldTo: string;
  readonly minTotal: string;
  readonly maxTotal: string;
}

export const DEFAULT_ORDER: readonly SortClause[] = [{ field: 'saleDate', direction: 'desc' }];

export const DEFAULT_QUERY: SalesQuery = {
  page: 1, size: 10, order: DEFAULT_ORDER,
  saleNumber: '', customerName: '', branchName: '', customerId: '', branchId: '',
  status: 'all', soldFrom: '', soldTo: '', minTotal: '', maxTotal: '',
};

const HIGHEST_FIRST: ReadonlySet<SortField> = new Set<SortField>(['saleDate', 'totalAmount']);

export function parseOrder(text: string | null | undefined): readonly SortClause[] {
  const clauses: SortClause[] = [];
  for (const raw of (text ?? '').split(',')) {
    const [name = '', direction = 'asc', ...extra] = raw.trim().split(/\s+/);
    const field = SORT_FIELDS.find(candidate => candidate.toLowerCase() === name.toLowerCase());
    const normalized = direction.toLowerCase();
    if (!field || extra.length > 0 || (normalized !== 'asc' && normalized !== 'desc')
      || clauses.some(clause => clause.field === field)) {
      continue;
    }
    clauses.push({ field, direction: normalized });
  }
  return clauses.length > 0 ? clauses : DEFAULT_ORDER;
}

export function formatOrder(order: readonly SortClause[]): string {
  return order.map(clause => `${clause.field} ${clause.direction}`).join(', ');
}

/** A header click sorts by that column alone; Shift+click adds it as the next sort, or flips it. */
export function toggleSort(order: readonly SortClause[], field: SortField, additive: boolean): readonly SortClause[] {
  const index = order.findIndex(clause => clause.field === field);
  const flip = (clause: SortClause): SortClause =>
    ({ field: clause.field, direction: clause.direction === 'asc' ? 'desc' : 'asc' });
  const fresh: SortClause = { field, direction: HIGHEST_FIRST.has(field) ? 'desc' : 'asc' };

  if (!additive) {
    return index === 0 ? [flip(order[0])] : [fresh];
  }
  return index === -1 ? [...order, fresh] : order.map((clause, i) => (i === index ? flip(clause) : clause));
}

export function queryFromParams(params: Params): SalesQuery {
  const text = (key: string): string => (typeof params[key] === 'string' ? params[key] : '');
  const page = Number.parseInt(text('_page'), 10);
  const size = Number.parseInt(text('_size'), 10);
  const isCancelled = text('isCancelled');

  return {
    page: Number.isInteger(page) && page >= 1 ? page : 1,
    size: (PAGE_SIZES as readonly number[]).includes(size) ? size : 10,
    order: parseOrder(text('_order')),
    saleNumber: text('saleNumber'),
    customerName: text('customerName'),
    branchName: text('branchName'),
    customerId: text('customerId'),
    branchId: text('branchId'),
    status: isCancelled === 'true' ? 'cancelled' : isCancelled === 'false' ? 'open' : 'all',
    soldFrom: text('_minSaleDate'),
    soldTo: text('_maxSaleDate'),
    minTotal: text('_minTotalAmount'),
    maxTotal: text('_maxTotalAmount'),
  };
}

/** The router query parameters for a query, without its defaults. */
export function queryToParams(query: SalesQuery): Params {
  const params: Params = {};
  const put = (key: string, value: string): void => {
    if (value.trim() !== '') {
      params[key] = value.trim();
    }
  };

  if (query.page !== 1) params['_page'] = String(query.page);
  if (query.size !== 10) params['_size'] = String(query.size);
  const order = formatOrder(query.order);
  if (order !== formatOrder(DEFAULT_ORDER)) params['_order'] = order;
  put('saleNumber', query.saleNumber);
  put('customerName', query.customerName);
  put('branchName', query.branchName);
  put('customerId', query.customerId);
  put('branchId', query.branchId);
  if (query.status !== 'all') params['isCancelled'] = String(query.status === 'cancelled');
  put('_minSaleDate', query.soldFrom);
  put('_maxSaleDate', query.soldTo);
  put('_minTotalAmount', query.minTotal);
  put('_maxTotalAmount', query.maxTotal);
  return params;
}

/** Plain text means "contains"; text with a `*` is sent as typed (`value*`, `*value`, `*value*`). */
export function toMatchPattern(text: string): string {
  const trimmed = text.trim();
  return trimmed === '' || trimmed.includes('*') ? trimmed : `*${trimmed}*`;
}

export function queryToHttpParams(query: SalesQuery): HttpParams {
  let params = new HttpParams({ encoder: new StrictParameterCodec() })
    .set('_page', query.page)
    .set('_size', query.size)
    .set('_order', formatOrder(query.order));
  const set = (key: string, value: string | null): void => {
    if (value) {
      params = params.set(key, value);
    }
  };

  set('saleNumber', toMatchPattern(query.saleNumber));
  set('customerName', toMatchPattern(query.customerName));
  set('branchName', toMatchPattern(query.branchName));
  set('customerId', query.customerId.trim());
  set('branchId', query.branchId.trim());
  if (query.status !== 'all') {
    params = params.set('isCancelled', query.status === 'cancelled');
  }
  set('_minSaleDate', startOfDayIso(query.soldFrom));
  set('_maxSaleDate', endOfDayIso(query.soldTo));
  set('_minTotalAmount', query.minTotal.trim());
  set('_maxTotalAmount', query.maxTotal.trim());
  return params;
}

const FILTER_KEYS = [
  'saleNumber', 'customerName', 'branchName', 'customerId', 'branchId', 'status', 'soldFrom', 'soldTo', 'minTotal', 'maxTotal',
] as const;

/** Picks the empty state: "No sales yet." or "No sales match these filters." */
export function hasFilters(query: SalesQuery): boolean {
  return FILTER_KEYS.some(key => query[key] !== DEFAULT_QUERY[key]);
}
```

- [ ] **Step 4: Run the tests and see them pass**

Run: `cd portal && rtk npm run test:ci`
Expected: PASS, 31 tests (17 + 14).

- [ ] **Step 5: Commit**

```bash
rtk git add portal/src/app/features/sales/sales-query.ts portal/src/app/features/sales/sales-query.spec.ts
rtk git commit -m "feat(portal): keep the sales-list query in the URL and send it as API parameters"
```

---

### Task 9: Pricing preview and formatting

**Files:**
- Create: `portal/src/app/features/sales/pricing.ts`, `portal/src/app/shared/format.ts`, `portal/src/app/shared/pipes.ts`
- Test: `portal/src/app/features/sales/pricing.spec.ts`, `portal/src/app/shared/format.spec.ts`

- [ ] **Step 1: Write the failing tests**

`portal/src/app/features/sales/pricing.spec.ts`:
```ts
import { describe, expect, it } from 'vitest';
import { discountPercentage, nextTier, previewLine, previewSale } from './pricing';

describe('discountPercentage (R1, with quantity 4 at 10% per D3)', () => {
  it.each([
    [1, 0], [3, 0], [4, 10], [9, 10], [10, 20], [20, 20],
  ])('gives %i items %i%%', (quantity, expected) => {
    expect(discountPercentage(quantity)).toBe(expected);
  });

  it.each([0, 21, 2.5])('rejects %s items', quantity => {
    expect(() => discountPercentage(quantity)).toThrow(RangeError);
  });
});

describe('previewLine (R3)', () => {
  it('prices the spec example: 5 items at 4.50 get 10% off', () => {
    expect(previewLine(5, 4.5)).toEqual({ gross: 22.5, discountPercentage: 10, discountAmount: 2.25, total: 20.25 });
  });

  it('rounds a half cent away from zero, as the API does', () => {
    // 5 × 0.05 = 0.25; 10% is 0.025, which is 0.03 away from zero (banker's rounding would give 0.02).
    expect(previewLine(5, 0.05)?.discountAmount).toBe(0.03);
  });

  it('has no preview while the line is incomplete or invalid', () => {
    expect(previewLine(null, 4.5)).toBeNull();
    expect(previewLine(5, null)).toBeNull();
    expect(previewLine(21, 4.5)).toBeNull();
    expect(previewLine(5, 0)).toBeNull();
    expect(previewLine(5, 1.234)).toBeNull();
  });
});

describe('previewSale', () => {
  it('adds up the complete lines and skips the rest', () => {
    expect(previewSale([previewLine(5, 4.5), previewLine(12, 7.9), null])).toEqual({
      gross: 117.3, discount: 21.21, total: 96.09,
    });
  });
});

describe('nextTier', () => {
  it('says how many more items reach the next tier', () => {
    expect(nextTier(3)).toEqual({ itemsToNext: 1, nextPercentage: 10 });
    expect(nextTier(4)).toEqual({ itemsToNext: 6, nextPercentage: 20 });
    expect(nextTier(9)).toEqual({ itemsToNext: 1, nextPercentage: 20 });
  });

  it('has no hint at the top tier or for an invalid quantity', () => {
    expect(nextTier(10)).toBeNull();
    expect(nextTier(null)).toBeNull();
    expect(nextTier(0)).toBeNull();
  });
});
```

The `previewSale` numbers:
- 5 × 4.50 = 22.50, minus 2.25 → 20.25.
- 12 × 7.90 = 94.80, minus 20% = 18.96 → 75.84.
- Gross is 117.30, discount 21.21, total 96.09.

`portal/src/app/shared/format.spec.ts`:
```ts
import { describe, expect, it } from 'vitest';
import { formatDateTime, formatMoney } from './format';

describe('format', () => {
  it('formats money in BRL', () => {
    expect(formatMoney(20.25)).toBe('R$20.25');
    expect(formatMoney(0)).toBe('R$0.00');
  });

  it('formats an instant in local time', () => {
    // Intl may put a narrow no-break space before AM/PM.
    expect(formatDateTime('2026-09-24T14:30:00Z').replace(/\u202f/g, ' ')).toBe('Sep 24, 2026, 11:30 AM');
  });
});
```

- [ ] **Step 2: Run them and see them fail**

Run: `cd portal && rtk npm run test:ci`
Expected: FAIL on the missing modules.

- [ ] **Step 3: Write the code**

`portal/src/app/features/sales/pricing.ts`:
```ts
/** A preview of the API's R1 and R3 rules while a line is edited. After a save, the server's numbers are shown. */

export const MAX_QUANTITY = 20;
export type DiscountPercentage = 0 | 10 | 20;

export interface LinePreview {
  readonly gross: number;
  readonly discountPercentage: DiscountPercentage;
  readonly discountAmount: number;
  readonly total: number;
}

export interface SalePreview {
  readonly gross: number;
  readonly discount: number;
  readonly total: number;
}

export interface TierHint {
  readonly itemsToNext: number;
  readonly nextPercentage: 10 | 20;
}

const isValidQuantity = (quantity: number): boolean =>
  Number.isInteger(quantity) && quantity >= 1 && quantity <= MAX_QUANTITY;

export function discountPercentage(quantity: number): DiscountPercentage {
  if (!isValidQuantity(quantity)) {
    throw new RangeError(`Quantity must be a whole number from 1 to ${MAX_QUANTITY}.`);
  }
  if (quantity >= 10) return 20;
  if (quantity >= 4) return 10;
  return 0;
}

export function previewLine(quantity: number | null, unitPrice: number | null): LinePreview | null {
  if (quantity === null || unitPrice === null || !isValidQuantity(quantity)) {
    return null;
  }
  const priceCents = Math.round(unitPrice * 100);
  if (priceCents <= 0 || Math.abs(unitPrice * 100 - priceCents) > 1e-6) {
    return null;
  }

  const percentage = discountPercentage(quantity);
  const grossCents = quantity * priceCents;
  // Amounts are positive, so Math.round's "half up" is the API's MidpointRounding.AwayFromZero.
  const discountCents = Math.round((grossCents * percentage) / 100);
  return {
    gross: grossCents / 100,
    discountPercentage: percentage,
    discountAmount: discountCents / 100,
    total: (grossCents - discountCents) / 100,
  };
}

export function previewSale(lines: readonly (LinePreview | null)[]): SalePreview {
  const cents = (value: number): number => Math.round(value * 100);
  const complete = lines.filter((line): line is LinePreview => line !== null);
  const gross = complete.reduce((sum, line) => sum + cents(line.gross), 0);
  const discount = complete.reduce((sum, line) => sum + cents(line.discountAmount), 0);
  return { gross: gross / 100, discount: discount / 100, total: (gross - discount) / 100 };
}

export function nextTier(quantity: number | null): TierHint | null {
  if (quantity === null || !isValidQuantity(quantity) || quantity >= 10) {
    return null;
  }
  return quantity < 4
    ? { itemsToNext: 4 - quantity, nextPercentage: 10 }
    : { itemsToNext: 10 - quantity, nextPercentage: 20 };
}
```

`portal/src/app/shared/format.ts`:
```ts
/** The portal's one place for locale and currency (Decision 12). */
export const LOCALE = 'en-US';
export const CURRENCY = 'BRL';

const money = new Intl.NumberFormat(LOCALE, { style: 'currency', currency: CURRENCY });
const dateTime = new Intl.DateTimeFormat(LOCALE, { dateStyle: 'medium', timeStyle: 'short' });

export function formatMoney(value: number): string {
  return money.format(value);
}

export function formatDateTime(iso: string): string {
  return dateTime.format(new Date(iso));
}
```

`portal/src/app/shared/pipes.ts`:
```ts
import { Pipe, PipeTransform } from '@angular/core';
import { formatDateTime, formatMoney } from './format';

@Pipe({ name: 'money' })
export class MoneyPipe implements PipeTransform {
  transform(value: number): string {
    return formatMoney(value);
  }
}

@Pipe({ name: 'dateTime' })
export class DateTimePipe implements PipeTransform {
  transform(value: string): string {
    return formatDateTime(value);
  }
}
```

- [ ] **Step 4: Run the tests and see them pass**

Run: `cd portal && rtk npm run test:ci`
Expected: PASS, 48 tests (31 + 15 pricing + 2 format). If `formatMoney` gives `R$ 20.25` (a different ICU), update both format assertions to what Node 24's ICU gives for `en-US`/`BRL`, and note it in the commit body.

- [ ] **Step 5: Commit**

```bash
rtk git add portal/src/app/features/sales/pricing.ts portal/src/app/features/sales/pricing.spec.ts portal/src/app/shared/format.ts portal/src/app/shared/format.spec.ts portal/src/app/shared/pipes.ts
rtk git commit -m "feat(portal): preview discounts and totals as the API computes them"
```

---

### Task 10: Session, store, guards and interceptors

**Files:**
- Create: `portal/src/app/core/auth/session.ts`, `session-store.ts`, `guards.ts`; `portal/src/app/core/api/interceptors.ts`; `portal/src/app/testing/memory-storage.ts`, `portal/src/app/testing/fake-jwt.ts`
- Test: `portal/src/app/core/auth/session.spec.ts`, `session-store.spec.ts`, `guards.spec.ts`; `portal/src/app/core/api/interceptors.spec.ts`

- [ ] **Step 1: Write the test helpers**

`portal/src/app/testing/memory-storage.ts`:
```ts
/** An in-memory Storage, so tests never share the browser's sessionStorage. */
export class MemoryStorage implements Storage {
  [name: string]: unknown;
  private readonly items = new Map<string, string>();

  get length(): number {
    return this.items.size;
  }

  clear(): void {
    this.items.clear();
  }

  getItem(key: string): string | null {
    return this.items.get(key) ?? null;
  }

  key(index: number): string | null {
    return [...this.items.keys()][index] ?? null;
  }

  removeItem(key: string): void {
    this.items.delete(key);
  }

  setItem(key: string, value: string): void {
    this.items.set(key, value);
  }
}
```

`portal/src/app/testing/fake-jwt.ts`:
```ts
const base64Url = (value: object): string =>
  btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');

/** A token with the given payload and a fake signature: good for the client, rejected by the API. */
export function fakeJwt(payload: Record<string, unknown>): string {
  return `${base64Url({ alg: 'HS256', typ: 'JWT' })}.${base64Url(payload)}.signature`;
}
```

- [ ] **Step 2: Write the failing tests**

`portal/src/app/core/auth/session.spec.ts`:
```ts
import { describe, expect, it } from 'vitest';
import { fakeJwt } from '../../testing/fake-jwt';
import { parseStoredSession, sessionFromAuthResponse } from './session';

const USER_ID = '6f1c1c52-8a7e-4a4f-9c1d-2d8f0b9d3e11';
const response = (payload: Record<string, unknown>) =>
  ({ token: fakeJwt(payload), email: 'ana@developerstore.test', name: 'Ana Souza', role: 'Customer' });

describe('sessionFromAuthResponse', () => {
  it('takes the user id and expiry from the token', () => {
    const session = sessionFromAuthResponse(response({ nameid: USER_ID, exp: 1_790_000_000 }));

    expect(session).toEqual({
      token: expect.any(String), userId: USER_ID, name: 'Ana Souza',
      email: 'ana@developerstore.test', role: 'Customer', expiresAt: 1_790_000_000_000,
    });
  });

  it('accepts the long name-identifier claim too', () => {
    const claim = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';
    expect(sessionFromAuthResponse(response({ [claim]: USER_ID, exp: 1 })).userId).toBe(USER_ID);
  });

  it('rejects a token without a user id or expiry', () => {
    expect(() => sessionFromAuthResponse(response({ exp: 1 }))).toThrow();
    expect(() => sessionFromAuthResponse(response({ nameid: USER_ID }))).toThrow();
  });
});

describe('parseStoredSession', () => {
  it('reads back what it stored', () => {
    const session = sessionFromAuthResponse(response({ nameid: USER_ID, exp: 1 }));
    expect(parseStoredSession(JSON.stringify(session))).toEqual(session);
  });

  it('ignores anything that is not a session', () => {
    expect(parseStoredSession(null)).toBeNull();
    expect(parseStoredSession('{not json')).toBeNull();
    expect(parseStoredSession('{"token":1}')).toBeNull();
  });
});
```

`portal/src/app/core/auth/session-store.spec.ts`:
```ts
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { fakeJwt } from '../../testing/fake-jwt';
import { MemoryStorage } from '../../testing/memory-storage';
import { SESSION_STORAGE_KEY } from './session';
import { CLOCK, SESSION_STORAGE, SessionStore } from './session-store';

describe('SessionStore', () => {
  let storage: MemoryStorage;
  let now: number;
  const auth = {
    token: fakeJwt({ nameid: '6f1c1c52-8a7e-4a4f-9c1d-2d8f0b9d3e11', exp: 2_000 }),
    email: 'ana@developerstore.test', name: 'Ana Souza', role: 'Customer',
  };

  beforeEach(() => {
    storage = new MemoryStorage();
    now = 1_000_000;
    TestBed.configureTestingModule({
      providers: [
        { provide: SESSION_STORAGE, useValue: storage },
        { provide: CLOCK, useValue: () => now },
      ],
    });
  });

  it('starts signed out', () => {
    expect(TestBed.inject(SessionStore).isSignedIn()).toBe(false);
  });

  it('starts a session and keeps it in storage', () => {
    const store = TestBed.inject(SessionStore);
    store.start(auth);

    expect(store.isSignedIn()).toBe(true);
    expect(store.session()?.name).toBe('Ana Souza');
    expect(storage.getItem(SESSION_STORAGE_KEY)).toContain('Ana Souza');
  });

  it('restores a stored session', () => {
    TestBed.inject(SessionStore).start(auth);
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({ providers: [{ provide: SESSION_STORAGE, useValue: storage }] });

    expect(TestBed.inject(SessionStore).session()?.email).toBe('ana@developerstore.test');
  });

  it('ends a session and remembers why', () => {
    const store = TestBed.inject(SessionStore);
    store.start(auth);
    store.end('expired');

    expect(store.isSignedIn()).toBe(false);
    expect(store.endReason()).toBe('expired');
    expect(storage.getItem(SESSION_STORAGE_KEY)).toBeNull();
  });

  it('knows when the token has expired', () => {
    const store = TestBed.inject(SessionStore);
    store.start(auth);
    expect(store.isExpired()).toBe(false);

    now = 2_000_000;
    expect(store.isExpired()).toBe(true);
  });
});
```

`portal/src/app/core/auth/guards.spec.ts`:
```ts
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { beforeEach, describe, expect, it } from 'vitest';
import { fakeJwt } from '../../testing/fake-jwt';
import { MemoryStorage } from '../../testing/memory-storage';
import { authGuard, guestGuard, safeReturnUrl } from './guards';
import { CLOCK, SESSION_STORAGE, SessionStore } from './session-store';

describe('guards', () => {
  let now: number;
  const route = {} as ActivatedRouteSnapshot;
  const state = { url: '/sales?customerName=silva' } as RouterStateSnapshot;
  const signIn = () => TestBed.inject(SessionStore).start({
    token: fakeJwt({ nameid: '6f1c1c52-8a7e-4a4f-9c1d-2d8f0b9d3e11', exp: 2_000 }),
    email: 'ana@developerstore.test', name: 'Ana Souza', role: 'Customer',
  });
  const serialize = (result: unknown) => TestBed.inject(Router).serializeUrl(result as UrlTree);

  beforeEach(() => {
    now = 1_000_000;
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: SESSION_STORAGE, useValue: new MemoryStorage() },
        { provide: CLOCK, useValue: () => now },
      ],
    });
  });

  it('sends a signed-out visitor to sign in, remembering where they were going', () => {
    const result = TestBed.runInInjectionContext(() => authGuard(route, state));
    expect(serialize(result)).toBe('/sign-in?returnUrl=%2Fsales%3FcustomerName%3Dsilva');
  });

  it('lets a signed-in user through', () => {
    signIn();
    expect(TestBed.runInInjectionContext(() => authGuard(route, state))).toBe(true);
  });

  it('ends an expired session and sends the user to sign in', () => {
    signIn();
    now = 3_000_000;

    const result = TestBed.runInInjectionContext(() => authGuard(route, state));

    expect(serialize(result)).toContain('/sign-in');
    expect(TestBed.inject(SessionStore).endReason()).toBe('expired');
  });

  it('sends a signed-in user away from the sign-in page', () => {
    signIn();
    expect(serialize(TestBed.runInInjectionContext(() => guestGuard(route, state)))).toBe('/sales');
  });

  it('only returns to paths inside the portal', () => {
    expect(safeReturnUrl('/sales/42')).toBe('/sales/42');
    expect(safeReturnUrl('//evil.example')).toBe('/sales');
    expect(safeReturnUrl('https://evil.example')).toBe('/sales');
    expect(safeReturnUrl('/sign-in')).toBe('/sales');
    expect(safeReturnUrl(undefined)).toBe('/sales');
  });
});
```

`portal/src/app/core/api/interceptors.spec.ts`:
```ts
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fakeJwt } from '../../testing/fake-jwt';
import { MemoryStorage } from '../../testing/memory-storage';
import { CLOCK, SESSION_STORAGE, SessionStore } from '../auth/session-store';
import { ApiError } from './api-error';
import { apiErrorInterceptor, authInterceptor } from './interceptors';

describe('interceptors', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let store: SessionStore;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor, apiErrorInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: SESSION_STORAGE, useValue: new MemoryStorage() },
        { provide: CLOCK, useValue: () => 0 },
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    store = TestBed.inject(SessionStore);
  });

  const signIn = () => store.start({
    token: fakeJwt({ nameid: '6f1c1c52-8a7e-4a4f-9c1d-2d8f0b9d3e11', exp: 9_999_999_999 }),
    email: 'ana@developerstore.test', name: 'Ana Souza', role: 'Customer',
  });

  it('sends the bearer token to the API', () => {
    const session = signIn();
    http.get('/api/sales').subscribe();

    expect(backend.expectOne('/api/sales').request.headers.get('Authorization')).toBe(`Bearer ${session.token}`);
  });

  it('sends no token to the sign-in endpoint', () => {
    signIn();
    http.post('/api/auth', {}).subscribe();

    expect(backend.expectOne('/api/auth').request.headers.has('Authorization')).toBe(false);
  });

  it('turns an HTTP failure into an ApiError', () => {
    let failure: unknown;
    http.get('/api/sales/1').subscribe({ error: error => (failure = error) });

    backend.expectOne('/api/sales/1').flush(
      { type: 'ResourceNotFound', error: 'Resource not found', detail: 'Sale with ID 1 not found' },
      { status: 404, statusText: 'Not Found' });

    expect(failure).toBeInstanceOf(ApiError);
    expect((failure as ApiError).type).toBe('ResourceNotFound');
  });

  it('ends a rejected session and sends the user to sign in', () => {
    signIn();
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    http.get('/api/sales').subscribe({ error: () => undefined });
    backend.expectOne('/api/sales').flush(
      { type: 'AuthenticationError', error: 'Authentication failed', detail: 'The token is invalid' },
      { status: 401, statusText: 'Unauthorized' });

    expect(store.isSignedIn()).toBe(false);
    expect(store.endReason()).toBe('rejected');
    expect(navigate).toHaveBeenCalledWith(['/sign-in'], { queryParams: { returnUrl: '/' } });
  });

  it('leaves the session alone when sign-in itself answers 401', () => {
    http.post('/api/auth', {}).subscribe({ error: () => undefined });
    backend.expectOne('/api/auth').flush(
      { type: 'AuthenticationError', error: 'Authentication failed', detail: 'Invalid credentials' },
      { status: 401, statusText: 'Unauthorized' });

    expect(store.endReason()).toBeNull();
  });
});
```

- [ ] **Step 3: Run them and see them fail**

Run: `cd portal && rtk npm run test:ci`
Expected: FAIL on the missing modules.

- [ ] **Step 4: Write the code**

`portal/src/app/core/auth/session.ts` (no Angular imports: the e2e fixtures import it):
```ts
import type { AuthenticateResponse } from '../api/api-models';

export const SESSION_STORAGE_KEY = 'ds.session';

export interface Session {
  readonly token: string;
  readonly userId: string;
  readonly name: string;
  readonly email: string;
  readonly role: string;
  /** Milliseconds since the epoch, from the token's `exp`. */
  readonly expiresAt: number;
}

// JwtSecurityTokenHandler writes ClaimTypes.NameIdentifier as `nameid`; the others are fallbacks.
const USER_ID_CLAIMS = ['nameid', 'sub', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];

export function decodeJwtPayload(token: string): Record<string, unknown> {
  const part = token.split('.')[1];
  if (!part) {
    throw new Error('The token is not a JWT.');
  }
  const base64 = part.replace(/-/g, '+').replace(/_/g, '/').padEnd(Math.ceil(part.length / 4) * 4, '=');
  const bytes = Uint8Array.from(atob(base64), character => character.charCodeAt(0));
  return JSON.parse(new TextDecoder().decode(bytes)) as Record<string, unknown>;
}

export function sessionFromAuthResponse(response: AuthenticateResponse): Session {
  const payload = decodeJwtPayload(response.token);
  const userId = USER_ID_CLAIMS.map(claim => payload[claim]).find((value): value is string => typeof value === 'string');
  const expiry = payload['exp'];
  if (!userId || typeof expiry !== 'number') {
    throw new Error('The token has no user id or expiry.');
  }
  return {
    token: response.token,
    userId,
    name: response.name,
    email: response.email,
    role: response.role,
    expiresAt: expiry * 1000,
  };
}

export function parseStoredSession(json: string | null): Session | null {
  if (json === null) {
    return null;
  }
  try {
    const value = JSON.parse(json) as Record<string, unknown>;
    const strings = ['token', 'userId', 'name', 'email', 'role'].every(key => typeof value[key] === 'string');
    return strings && typeof value['expiresAt'] === 'number' ? (value as unknown as Session) : null;
  } catch {
    return null;
  }
}
```

`portal/src/app/core/auth/session-store.ts`:
```ts
import { computed, inject, Injectable, InjectionToken, signal } from '@angular/core';
import { AuthenticateResponse } from '../api/api-models';
import { parseStoredSession, Session, SESSION_STORAGE_KEY, sessionFromAuthResponse } from './session';

export type SessionEndReason = 'signed-out' | 'expired' | 'rejected' | 'account-deleted';

export const SESSION_STORAGE = new InjectionToken<Storage>('SESSION_STORAGE', {
  providedIn: 'root',
  factory: () => sessionStorage,
});

export const CLOCK = new InjectionToken<() => number>('CLOCK', {
  providedIn: 'root',
  factory: () => () => Date.now(),
});

@Injectable({ providedIn: 'root' })
export class SessionStore {
  private readonly storage = inject(SESSION_STORAGE);
  private readonly now = inject(CLOCK);
  private readonly state = signal<Session | null>(parseStoredSession(this.storage.getItem(SESSION_STORAGE_KEY)));
  private readonly endReasonState = signal<SessionEndReason | null>(null);

  readonly session = this.state.asReadonly();
  readonly endReason = this.endReasonState.asReadonly();
  readonly isSignedIn = computed(() => this.state() !== null);

  start(response: AuthenticateResponse): Session {
    const session = sessionFromAuthResponse(response);
    this.storage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session));
    this.state.set(session);
    this.endReasonState.set(null);
    return session;
  }

  end(reason: SessionEndReason): void {
    this.storage.removeItem(SESSION_STORAGE_KEY);
    this.state.set(null);
    this.endReasonState.set(reason);
  }

  isExpired(): boolean {
    const session = this.state();
    return session !== null && session.expiresAt <= this.now();
  }
}
```

`portal/src/app/core/auth/guards.ts`:
```ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessionStore } from './session-store';

const HOME = '/sales';

export const authGuard: CanActivateFn = (_route, state) => {
  const store = inject(SessionStore);
  if (store.isSignedIn() && !store.isExpired()) {
    return true;
  }
  if (store.isSignedIn()) {
    store.end('expired');
  }
  return inject(Router).createUrlTree(['/sign-in'], { queryParams: { returnUrl: state.url } });
};

export const guestGuard: CanActivateFn = () =>
  inject(SessionStore).isSignedIn() ? inject(Router).parseUrl(HOME) : true;

/** Only paths inside the portal: never another origin, never the auth pages. */
export function safeReturnUrl(value: unknown): string {
  if (typeof value !== 'string' || !value.startsWith('/') || value.startsWith('//')
    || value.startsWith('/sign-in') || value.startsWith('/sign-up')) {
    return HOME;
  }
  return value;
}
```

`portal/src/app/core/api/interceptors.ts`:
```ts
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { SessionStore } from '../auth/session-store';
import { toApiError } from './api-error';

const SIGN_IN_URL = '/api/auth';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const session = inject(SessionStore).session();
  if (!session || !request.url.startsWith('/api/') || request.url === SIGN_IN_URL) {
    return next(request);
  }
  return next(request.clone({ setHeaders: { Authorization: `Bearer ${session.token}` } }));
};

export const apiErrorInterceptor: HttpInterceptorFn = (request, next) => {
  const store = inject(SessionStore);
  const router = inject(Router);

  return next(request).pipe(
    catchError((failure: unknown) => {
      if (!(failure instanceof HttpErrorResponse)) {
        return throwError(() => failure);
      }
      if (failure.status === 401 && request.url !== SIGN_IN_URL && store.isSignedIn()) {
        store.end('rejected');
        void router.navigate(['/sign-in'], { queryParams: { returnUrl: router.url } });
      }
      return throwError(() => toApiError(failure));
    }),
  );
};
```

- [ ] **Step 5: Run the tests and see them pass**

Run: `cd portal && rtk npm run test:ci`
Expected: PASS, 68 tests (48 + 5 session + 5 store + 5 guards + 5 interceptors).

- [ ] **Step 6: Commit**

```bash
rtk git add portal/src/app/core/auth/session.ts portal/src/app/core/auth/session.spec.ts portal/src/app/core/auth/session-store.ts \
  portal/src/app/core/auth/session-store.spec.ts portal/src/app/core/auth/guards.ts portal/src/app/core/auth/guards.spec.ts \
  portal/src/app/core/api/interceptors.ts portal/src/app/core/api/interceptors.spec.ts portal/src/app/testing
rtk git commit -m "feat(portal): keep the JWT session, guard routes and handle rejected tokens"
```

---

### Task 11: API services, auth flow, credentials, catalog and the sale form

**Files:**
- Create: `portal/src/app/core/api/{auth-api,users-api,sales-api}.ts`, `portal/src/app/core/auth/{auth-flow,credentials}.ts`, `portal/src/app/core/catalog/demo-catalog.ts`, `portal/src/app/features/sales/editor/sale-form.ts`, `portal/src/app/shared/forms/field-errors.ts`, `portal/src/app/testing/sale-fixture.ts`
- Test: `portal/src/app/core/api/sales-api.spec.ts`, `portal/src/app/core/auth/auth-flow.spec.ts`, `portal/src/app/core/auth/credentials.spec.ts`, `portal/src/app/features/sales/editor/sale-form.spec.ts`, `portal/src/app/shared/forms/field-errors.spec.ts`

- [ ] **Step 1: Write the catalog and the fixture** (data; no test of their own)

`portal/src/app/core/catalog/demo-catalog.ts`:
```ts
/**
 * Demo reference data. The Sales API stores customers, branches and products as external identities (an id and a name)
 * and owns none of them, so the portal offers these for convenience. Any other name and id works too.
 */
export interface CatalogEntry {
  readonly id: string;
  readonly name: string;
}

export interface ProductEntry extends CatalogEntry {
  readonly unitPrice: number;
}

export const CUSTOMERS: readonly CatalogEntry[] = [
  { id: '00000000-0000-4000-8000-c00000000001', name: 'Maria Silva' },
  { id: '00000000-0000-4000-8000-c00000000002', name: 'João Souza' },
  { id: '00000000-0000-4000-8000-c00000000003', name: 'Ana Oliveira' },
  { id: '00000000-0000-4000-8000-c00000000004', name: 'Bar do Zé' },
  { id: '00000000-0000-4000-8000-c00000000005', name: 'Mercado Bom Preço' },
  { id: '00000000-0000-4000-8000-c00000000006', name: 'Restaurante Sabor da Terra' },
  { id: '00000000-0000-4000-8000-c00000000007', name: 'Empório Paulista' },
  { id: '00000000-0000-4000-8000-c00000000008', name: 'Distribuidora Rio Claro' },
];

export const BRANCHES: readonly CatalogEntry[] = [
  { id: '00000000-0000-4000-8000-b00000000001', name: 'Filial Centro' },
  { id: '00000000-0000-4000-8000-b00000000002', name: 'Filial Paulista' },
  { id: '00000000-0000-4000-8000-b00000000003', name: 'Filial Campinas' },
  { id: '00000000-0000-4000-8000-b00000000004', name: 'Filial Rio de Janeiro' },
  { id: '00000000-0000-4000-8000-b00000000005', name: 'Filial Belo Horizonte' },
];

export const PRODUCTS: readonly ProductEntry[] = [
  { id: '00000000-0000-4000-8000-a00000000001', name: 'Cerveja Pilsen 350ml', unitPrice: 4.5 },
  { id: '00000000-0000-4000-8000-a00000000002', name: 'Cerveja Pilsen 600ml', unitPrice: 7.9 },
  { id: '00000000-0000-4000-8000-a00000000003', name: 'Cerveja Puro Malte 350ml', unitPrice: 5.2 },
  { id: '00000000-0000-4000-8000-a00000000004', name: 'Refrigerante Guaraná 2L', unitPrice: 9.5 },
  { id: '00000000-0000-4000-8000-a00000000005', name: 'Água Mineral 500ml', unitPrice: 2.5 },
  { id: '00000000-0000-4000-8000-a00000000006', name: 'Chope Pilsen Barril 30L', unitPrice: 489.9 },
  { id: '00000000-0000-4000-8000-a00000000007', name: 'Energético 250ml', unitPrice: 8.75 },
  { id: '00000000-0000-4000-8000-a00000000008', name: 'Cerveja sem Álcool 350ml', unitPrice: 4.9 },
];
```

`portal/src/app/testing/sale-fixture.ts`:
```ts
import { Sale, SaleItem } from '../core/api/api-models';

export function anItem(overrides: Partial<SaleItem> = {}): SaleItem {
  return {
    id: '7f9c2a44-6666-4d1e-8a3b-000000000011',
    productId: '00000000-0000-4000-8000-a00000000001',
    productName: 'Cerveja Pilsen 350ml',
    quantity: 5, unitPrice: 4.5, discountPercentage: 10, discountAmount: 2.25, totalAmount: 20.25,
    isCancelled: false,
    ...overrides,
  };
}

export function aSale(overrides: Partial<Sale> = {}): Sale {
  return {
    id: '7f9c2a44-5555-4d1e-8a3b-000000000010',
    saleNumber: 'S-000123',
    saleDate: '2026-09-24T14:30:00Z',
    customerId: '00000000-0000-4000-8000-c00000000001', customerName: 'Maria Silva',
    branchId: '00000000-0000-4000-8000-b00000000001', branchName: 'Filial Centro',
    totalAmount: 20.25, isCancelled: false,
    createdAt: '2026-09-24T14:31:02Z', updatedAt: null,
    items: [anItem()],
    ...overrides,
  };
}
```

- [ ] **Step 2: Write the failing tests**

`portal/src/app/core/api/sales-api.spec.ts`:
```ts
import { HttpParams, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { aSale } from '../../testing/sale-fixture';
import { SalesApi } from './sales-api';

describe('SalesApi', () => {
  let api: SalesApi;
  let backend: HttpTestingController;
  const SALE = aSale();

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(SalesApi);
    backend = TestBed.inject(HttpTestingController);
  });

  it('lists sales with the given parameters and unwraps the page', () => {
    let page: unknown;
    api.list(new HttpParams().set('_page', 2).set('customerName', '*silva*')).subscribe(result => (page = result));

    const request = backend.expectOne(candidate => candidate.url === '/api/sales');
    expect(request.request.params.get('_page')).toBe('2');
    expect(request.request.params.get('customerName')).toBe('*silva*');
    request.flush({ success: true, message: 'Sales retrieved successfully', data: [SALE], currentPage: 2, totalPages: 3, totalItems: 21 });

    expect(page).toEqual({ sales: [SALE], currentPage: 2, totalPages: 3, totalItems: 21 });
  });

  it('unwraps a single sale', () => {
    let sale: unknown;
    api.get(SALE.id).subscribe(result => (sale = result));
    backend.expectOne(`/api/sales/${SALE.id}`).flush({ success: true, message: 'Sale retrieved successfully', data: SALE });

    expect(sale).toEqual(SALE);
  });

  it.each([
    ['cancel', 'PATCH', `/api/sales/${SALE.id}/cancel`, () => TestBed.inject(SalesApi).cancel(SALE.id)],
    ['cancelItem', 'PATCH', `/api/sales/${SALE.id}/items/i1/cancel`, () => TestBed.inject(SalesApi).cancelItem(SALE.id, 'i1')],
    ['update', 'PUT', `/api/sales/${SALE.id}`, () => TestBed.inject(SalesApi).update(SALE.id, {} as never)],
    ['create', 'POST', '/api/sales', () => TestBed.inject(SalesApi).create({} as never)],
  ])('%s calls %s %s and returns the sale', (_name, method, url, call) => {
    let sale: unknown;
    call().subscribe(result => (sale = result));
    const request = backend.expectOne(url);
    expect(request.request.method).toBe(method);
    request.flush({ success: true, message: 'ok', data: SALE });

    expect(sale).toEqual(SALE);
  });

  it('deletes a sale', () => {
    let done = false;
    api.delete(SALE.id).subscribe(() => (done = true));
    const request = backend.expectOne(`/api/sales/${SALE.id}`);
    expect(request.request.method).toBe('DELETE');
    request.flush({ success: true, message: 'Sale deleted successfully' });

    expect(done).toBe(true);
  });
});
```

`portal/src/app/core/auth/auth-flow.spec.ts`:
```ts
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { fakeJwt } from '../../testing/fake-jwt';
import { MemoryStorage } from '../../testing/memory-storage';
import { AuthFlow } from './auth-flow';
import { SESSION_STORAGE, SessionStore } from './session-store';

describe('AuthFlow', () => {
  let flow: AuthFlow;
  let backend: HttpTestingController;
  const USER_ID = '6f1c1c52-8a7e-4a4f-9c1d-2d8f0b9d3e11';
  const auth = { token: fakeJwt({ nameid: USER_ID, exp: 9_999_999_999 }), email: 'ana@developerstore.test', name: 'Ana Souza', role: 'Customer' };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), { provide: SESSION_STORAGE, useValue: new MemoryStorage() }],
    });
    flow = TestBed.inject(AuthFlow);
    backend = TestBed.inject(HttpTestingController);
  });

  it('signs up an active customer, then signs in with the same credentials', () => {
    flow.signUp({ username: 'Ana Souza', email: 'ana@developerstore.test', phone: '+5511987654321', password: 'Portal@2026' }).subscribe();

    const signUp = backend.expectOne('/api/users');
    expect(signUp.request.body).toEqual({
      username: 'Ana Souza', email: 'ana@developerstore.test', phone: '+5511987654321', password: 'Portal@2026',
      status: 'Active', role: 'Customer',
    });
    signUp.flush({ success: true, message: 'User created successfully', data: { id: USER_ID } }, { status: 201, statusText: 'Created' });

    const signIn = backend.expectOne('/api/auth');
    expect(signIn.request.body).toEqual({ email: 'ana@developerstore.test', password: 'Portal@2026' });
    signIn.flush({ success: true, message: 'User authenticated successfully', data: auth });

    expect(TestBed.inject(SessionStore).session()?.userId).toBe(USER_ID);
  });

  it('deletes the signed-in account and ends the session', () => {
    TestBed.inject(SessionStore).start(auth);
    flow.deleteAccount().subscribe();

    const request = backend.expectOne(`/api/users/${USER_ID}`);
    expect(request.request.method).toBe('DELETE');
    request.flush({ success: true, message: 'User deleted successfully' });

    expect(TestBed.inject(SessionStore).endReason()).toBe('account-deleted');
  });
});
```

`portal/src/app/core/auth/credentials.spec.ts`:
```ts
import { FormControl } from '@angular/forms';
import { describe, expect, it } from 'vitest';
import { passwordValidator, PHONE_PATTERN, unmetPasswordRules } from './credentials';

describe('credentials', () => {
  it('lists the password rules a value misses', () => {
    expect(unmetPasswordRules('weak')).toEqual(['length', 'uppercase', 'number', 'symbol']);
    expect(unmetPasswordRules('Portal@2026')).toEqual([]);
  });

  it('fails a control whose password misses a rule', () => {
    expect(passwordValidator(new FormControl('Portal2026'))).toEqual({ password: ['symbol'] });
    expect(passwordValidator(new FormControl('Portal@2026'))).toBeNull();
  });

  it('accepts only phones both API validators accept: + and 11 to 15 digits', () => {
    expect(PHONE_PATTERN.test('+5511987654321')).toBe(true);
    expect(PHONE_PATTERN.test('5511987654321')).toBe(false);
    expect(PHONE_PATTERN.test('+551198')).toBe(false);
  });
});
```

`portal/src/app/features/sales/editor/sale-form.spec.ts`:
```ts
import { TestBed } from '@angular/core/testing';
import { NonNullableFormBuilder } from '@angular/forms';
import { beforeEach, describe, expect, it } from 'vitest';
import { anItem, aSale } from '../../../testing/sale-fixture';
import { addItem, applyServerErrors, createSaleForm, patchFromSale, SaleForm, toCreateRequest, toUpdateRequest } from './sale-form';

describe('sale form', () => {
  let fb: NonNullableFormBuilder;
  let form: SaleForm;

  const fill = (target: SaleForm) => {
    target.controls.saleDate.setValue('2026-09-24T11:30');
    target.controls.customer.setValue({ id: '00000000-0000-4000-8000-c00000000001', name: ' Maria Silva ' });
    target.controls.branch.setValue({ id: '00000000-0000-4000-8000-b00000000001', name: 'Filial Centro' });
    target.controls.items.at(0).setValue({
      productId: '00000000-0000-4000-8000-a00000000001', productName: 'Cerveja Pilsen 350ml', quantity: 5, unitPrice: 4.5,
    });
  };

  beforeEach(() => {
    fb = TestBed.inject(NonNullableFormBuilder);
    form = createSaleForm(fb);
  });

  it('starts with one empty line and is invalid until filled', () => {
    expect(form.controls.items.length).toBe(1);
    expect(form.valid).toBe(false);

    fill(form);
    expect(form.valid).toBe(true);
  });

  it('builds a create request with the local date and its offset, trimmed names and no blank number', () => {
    fill(form);

    expect(toCreateRequest(form)).toEqual({
      saleDate: '2026-09-24T11:30:00.000-03:00',
      customerId: '00000000-0000-4000-8000-c00000000001', customerName: 'Maria Silva',
      branchId: '00000000-0000-4000-8000-b00000000001', branchName: 'Filial Centro',
      items: [{ productId: '00000000-0000-4000-8000-a00000000001', productName: 'Cerveja Pilsen 350ml', quantity: 5, unitPrice: 4.5 }],
    });
  });

  it('sends a typed sale number, trimmed', () => {
    fill(form);
    form.controls.saleNumber.setValue('  BR-2026-0001 ');

    expect(toCreateRequest(form).saleNumber).toBe('BR-2026-0001');
  });

  it('rejects a repeated product, more than 20 items and a third decimal place', () => {
    fill(form);
    addItem(fb, form, { productId: '00000000-0000-4000-8000-A00000000001', productName: 'Again', quantity: 1, unitPrice: 1 });
    expect(form.controls.items.errors).toEqual({ repeatedProduct: ['00000000-0000-4000-8000-a00000000001'] });

    form.controls.items.at(0).controls.quantity.setValue(21);
    expect(form.controls.items.at(0).controls.quantity.errors).toEqual({ quantityMax: true });

    form.controls.items.at(0).controls.unitPrice.setValue(1.234);
    expect(form.controls.items.at(0).controls.unitPrice.errors).toEqual({ priceDecimals: true });
  });

  it('loads a sale for editing: active lines only, number read-only', () => {
    patchFromSale(fb, form, aSale({
      items: [anItem(), anItem({ id: 'x', productId: '00000000-0000-4000-8000-a00000000002', productName: 'Old', isCancelled: true })],
    }));

    expect(form.controls.saleNumber.disabled).toBe(true);
    expect(form.controls.saleDate.value).toBe('2026-09-24T11:30');
    expect(form.controls.items.length).toBe(1);
    expect(toUpdateRequest(form).items).toEqual([
      { productId: '00000000-0000-4000-8000-a00000000001', productName: 'Cerveja Pilsen 350ml', quantity: 5, unitPrice: 4.5 },
    ]);
  });

  it('puts server errors on their controls and returns the rest', () => {
    fill(form);
    const unmatched = applyServerErrors(form, {
      'items[0].quantity': ["It's not possible to sell above 20 identical items"],
      customerName: ['Too long'],
      '': ['A sale must have at least one item'],
    });

    expect(form.controls.items.at(0).controls.quantity.errors).toEqual({ server: "It's not possible to sell above 20 identical items" });
    expect(form.controls.customer.controls.name.errors).toEqual({ server: 'Too long' });
    expect(unmatched).toEqual(['A sale must have at least one item']);
  });
});
```

`portal/src/app/shared/forms/field-errors.spec.ts`:
```ts
import { describe, expect, it } from 'vitest';
import { describeFieldError } from './field-errors';

describe('describeFieldError', () => {
  it('prefers the server message', () => {
    expect(describeFieldError({ server: 'Sale number S-1 already exists', required: true }, 'Sale number')).toBe('Sale number S-1 already exists');
  });

  it.each([
    [{ required: true }, 'Quantity is required.'],
    [{ quantityMax: true }, "It's not possible to sell above 20 identical items."],
    [{ quantityMin: true }, 'Quantity must be a whole number of at least 1.'],
    [{ maxlength: { requiredLength: 50 } }, 'Quantity must have at most 50 characters.'],
  ])('describes %j', (errors, expected) => {
    expect(describeFieldError(errors, 'Quantity')).toBe(expected);
  });

  it('describes nothing for a valid control', () => {
    expect(describeFieldError(null, 'Quantity')).toBeNull();
  });
});
```

- [ ] **Step 3: Run them and see them fail**

Run: `cd portal && rtk npm run test:ci`
Expected: FAIL on the missing modules.

- [ ] **Step 4: Write the code**

`portal/src/app/core/api/auth-api.ts`:
```ts
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiResponseWithData, AuthenticateRequest, AuthenticateResponse } from './api-models';

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);

  signIn(body: AuthenticateRequest): Observable<AuthenticateResponse> {
    return this.http.post<ApiResponseWithData<AuthenticateResponse>>('/api/auth', body).pipe(map(response => response.data));
  }
}
```

`portal/src/app/core/api/users-api.ts`:
```ts
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiResponse, ApiResponseWithData, CreateUserRequest, User } from './api-models';

@Injectable({ providedIn: 'root' })
export class UsersApi {
  private readonly http = inject(HttpClient);

  signUp(body: CreateUserRequest): Observable<User> {
    return this.http.post<ApiResponseWithData<User>>('/api/users', body).pipe(map(response => response.data));
  }

  get(id: string): Observable<User> {
    return this.http.get<ApiResponseWithData<User>>(`/api/users/${id}`).pipe(map(response => response.data));
  }

  delete(id: string): Observable<void> {
    return this.http.delete<ApiResponse>(`/api/users/${id}`).pipe(map(() => undefined));
  }
}
```

`portal/src/app/core/api/sales-api.ts`:
```ts
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import {
  ApiResponse, ApiResponseWithData, CreateSaleRequest, PaginatedResponse, Sale, SalesPage, UpdateSaleRequest,
} from './api-models';

@Injectable({ providedIn: 'root' })
export class SalesApi {
  private readonly http = inject(HttpClient);

  /** The list screen builds the parameters with queryToHttpParams, so core never depends on a feature. */
  list(params: HttpParams): Observable<SalesPage> {
    return this.http.get<PaginatedResponse<Sale>>('/api/sales', { params }).pipe(
      map(response => ({
        sales: response.data,
        currentPage: response.currentPage,
        totalPages: response.totalPages,
        totalItems: response.totalItems,
      })),
    );
  }

  get(id: string): Observable<Sale> {
    return this.unwrap(this.http.get<ApiResponseWithData<Sale>>(`/api/sales/${id}`));
  }

  create(body: CreateSaleRequest): Observable<Sale> {
    return this.unwrap(this.http.post<ApiResponseWithData<Sale>>('/api/sales', body));
  }

  update(id: string, body: UpdateSaleRequest): Observable<Sale> {
    return this.unwrap(this.http.put<ApiResponseWithData<Sale>>(`/api/sales/${id}`, body));
  }

  cancel(id: string): Observable<Sale> {
    return this.unwrap(this.http.patch<ApiResponseWithData<Sale>>(`/api/sales/${id}/cancel`, null));
  }

  cancelItem(id: string, itemId: string): Observable<Sale> {
    return this.unwrap(this.http.patch<ApiResponseWithData<Sale>>(`/api/sales/${id}/items/${itemId}/cancel`, null));
  }

  delete(id: string): Observable<void> {
    return this.http.delete<ApiResponse>(`/api/sales/${id}`).pipe(map(() => undefined));
  }

  private unwrap(response: Observable<ApiResponseWithData<Sale>>): Observable<Sale> {
    return response.pipe(map(body => body.data));
  }
}
```

`portal/src/app/core/auth/credentials.ts`:
```ts
import { ValidatorFn } from '@angular/forms';

/** Both API validators must pass: UserValidator wants `+` and 11–15 digits; PhoneValidator is looser. */
export const PHONE_PATTERN = /^\+[1-9]\d{10,14}$/;

export const PASSWORD_RULES = [
  { key: 'length', label: 'At least 8 characters', test: (value: string) => value.length >= 8 },
  { key: 'uppercase', label: 'An uppercase letter', test: (value: string) => /[A-Z]/.test(value) },
  { key: 'lowercase', label: 'A lowercase letter', test: (value: string) => /[a-z]/.test(value) },
  { key: 'number', label: 'A number', test: (value: string) => /[0-9]/.test(value) },
  { key: 'symbol', label: 'A symbol: ! ? * . @ # $ % ^ & + =', test: (value: string) => /[!?*.@#$%^&+=]/.test(value) },
] as const;

export function unmetPasswordRules(value: string): string[] {
  return PASSWORD_RULES.filter(rule => !rule.test(value)).map(rule => rule.key);
}

export const passwordValidator: ValidatorFn = control => {
  const unmet = unmetPasswordRules(typeof control.value === 'string' ? control.value : '');
  return unmet.length > 0 ? { password: unmet } : null;
};
```

`portal/src/app/core/auth/auth-flow.ts`:
```ts
import { inject, Injectable } from '@angular/core';
import { map, Observable, switchMap, tap, throwError } from 'rxjs';
import { AuthApi } from '../api/auth-api';
import { AuthenticateRequest, CreateUserRequest } from '../api/api-models';
import { UsersApi } from '../api/users-api';
import { Session } from './session';
import { SessionStore } from './session-store';

export type SignUpDetails = Pick<CreateUserRequest, 'username' | 'email' | 'phone' | 'password'>;

@Injectable({ providedIn: 'root' })
export class AuthFlow {
  private readonly authApi = inject(AuthApi);
  private readonly usersApi = inject(UsersApi);
  private readonly store = inject(SessionStore);

  signIn(credentials: AuthenticateRequest): Observable<Session> {
    return this.authApi.signIn(credentials).pipe(map(response => this.store.start(response)));
  }

  /** Public sign-up creates only active Customer accounts; the user is signed in straight after. */
  signUp(details: SignUpDetails): Observable<Session> {
    return this.usersApi.signUp({ ...details, status: 'Active', role: 'Customer' }).pipe(
      switchMap(() => this.signIn({ email: details.email, password: details.password })),
    );
  }

  signOut(): void {
    this.store.end('signed-out');
  }

  deleteAccount(): Observable<void> {
    const session = this.store.session();
    if (!session) {
      return throwError(() => new Error('Not signed in.'));
    }
    return this.usersApi.delete(session.userId).pipe(tap(() => this.store.end('account-deleted')));
  }
}
```

`portal/src/app/features/sales/editor/sale-form.ts`:
```ts
import { FormArray, FormControl, FormGroup, NonNullableFormBuilder, ValidatorFn, Validators } from '@angular/forms';
import { CreateSaleRequest, Sale, SaleItemRequest, UpdateSaleRequest } from '../../../core/api/api-models';
import { FieldErrors } from '../../../core/api/api-error';
import { fromDateTimeLocal, toDateTimeLocal, toOffsetIso } from '../../../core/time/local-time';
import { MAX_QUANTITY } from '../pricing';

export const GUID_PATTERN = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;
const NAME_MAX_LENGTH = 100;
const SALE_NUMBER_MAX_LENGTH = 50;

export type SaleItemForm = FormGroup<{
  productId: FormControl<string>;
  productName: FormControl<string>;
  quantity: FormControl<number | null>;
  unitPrice: FormControl<number | null>;
}>;
export type PartyForm = FormGroup<{ id: FormControl<string>; name: FormControl<string> }>;
export type SaleForm = FormGroup<{
  saleNumber: FormControl<string>;
  saleDate: FormControl<string>;
  customer: PartyForm;
  branch: PartyForm;
  items: FormArray<SaleItemForm>;
}>;

const requiredTrimmed: ValidatorFn = control =>
  typeof control.value === 'string' && control.value.trim() === '' ? { required: true } : null;

const trimmedMaxLength = (max: number): ValidatorFn => control =>
  typeof control.value === 'string' && control.value.trim().length > max
    ? { maxlength: { requiredLength: max, actualLength: control.value.trim().length } }
    : null;

export const quantityValidator: ValidatorFn = control => {
  const value: unknown = control.value;
  if (value === null || value === '') return { required: true };
  if (typeof value !== 'number' || !Number.isInteger(value) || value < 1) return { quantityMin: true };
  if (value > MAX_QUANTITY) return { quantityMax: true };
  return null;
};

export const unitPriceValidator: ValidatorFn = control => {
  const value: unknown = control.value;
  if (value === null || value === '') return { required: true };
  if (typeof value !== 'number' || value <= 0) return { pricePositive: true };
  if (Math.abs(value * 100 - Math.round(value * 100)) > 1e-6) return { priceDecimals: true };
  return null;
};

/** R4: one line per product. Ids are compared case-insensitively, as Guids are. */
export const uniqueProductsValidator: ValidatorFn = control => {
  const seen = new Set<string>();
  const repeated = new Set<string>();
  for (const item of (control as FormArray<SaleItemForm>).controls) {
    const id = item.controls.productId.value.trim().toLowerCase();
    if (id === '') continue;
    (seen.has(id) ? repeated : seen).add(id);
  }
  return repeated.size > 0 ? { repeatedProduct: [...repeated] } : null;
};

const atLeastOneItem: ValidatorFn = control =>
  (control as FormArray).length === 0 ? { noItems: true } : null;

function createPartyForm(fb: NonNullableFormBuilder): PartyForm {
  return fb.group({
    id: fb.control('', [Validators.required, Validators.pattern(GUID_PATTERN)]),
    name: fb.control('', [requiredTrimmed, trimmedMaxLength(NAME_MAX_LENGTH)]),
  });
}

export function createItemForm(fb: NonNullableFormBuilder, item?: Partial<SaleItemRequest>): SaleItemForm {
  return fb.group({
    productId: fb.control(item?.productId ?? '', [Validators.required, Validators.pattern(GUID_PATTERN)]),
    productName: fb.control(item?.productName ?? '', [requiredTrimmed, trimmedMaxLength(NAME_MAX_LENGTH)]),
    quantity: fb.control<number | null>(item?.quantity ?? 1, quantityValidator),
    unitPrice: fb.control<number | null>(item?.unitPrice ?? null, unitPriceValidator),
  });
}

export function createSaleForm(fb: NonNullableFormBuilder, now: Date = new Date()): SaleForm {
  return fb.group({
    saleNumber: fb.control('', trimmedMaxLength(SALE_NUMBER_MAX_LENGTH)),
    saleDate: fb.control(toDateTimeLocal(now.toISOString()), Validators.required),
    customer: createPartyForm(fb),
    branch: createPartyForm(fb),
    items: fb.array<SaleItemForm>([createItemForm(fb)], [atLeastOneItem, uniqueProductsValidator]),
  });
}

export function addItem(fb: NonNullableFormBuilder, form: SaleForm, item?: Partial<SaleItemRequest>): void {
  form.controls.items.push(createItemForm(fb, item));
}

export function toUpdateRequest(form: SaleForm): UpdateSaleRequest {
  const value = form.getRawValue();
  const saleDate = fromDateTimeLocal(value.saleDate);
  if (!saleDate) {
    throw new Error('The sale date is not a valid date and time.');
  }
  return {
    saleDate: toOffsetIso(saleDate),
    customerId: value.customer.id,
    customerName: value.customer.name.trim(),
    branchId: value.branch.id,
    branchName: value.branch.name.trim(),
    items: value.items.map(item => ({
      productId: item.productId,
      productName: item.productName.trim(),
      quantity: item.quantity ?? 0,
      unitPrice: item.unitPrice ?? 0,
    })),
  };
}

/** A blank number is left out, so the API issues the next one (R12). */
export function toCreateRequest(form: SaleForm): CreateSaleRequest {
  const saleNumber = form.controls.saleNumber.value.trim();
  return saleNumber === '' ? toUpdateRequest(form) : { saleNumber, ...toUpdateRequest(form) };
}

/** Loads a sale for a PUT: its active lines only, since cancelled lines are history (R10). */
export function patchFromSale(fb: NonNullableFormBuilder, form: SaleForm, sale: Sale): void {
  form.controls.saleNumber.setValue(sale.saleNumber);
  form.controls.saleNumber.disable();
  form.controls.saleDate.setValue(toDateTimeLocal(sale.saleDate));
  form.controls.customer.setValue({ id: sale.customerId, name: sale.customerName });
  form.controls.branch.setValue({ id: sale.branchId, name: sale.branchName });
  form.controls.items.clear();
  for (const item of sale.items.filter(line => !line.isCancelled)) {
    addItem(fb, form, item);
  }
}

const PARTY_FIELDS: Readonly<Record<string, string>> = {
  customerId: 'customer.id', customerName: 'customer.name', branchId: 'branch.id', branchName: 'branch.name',
};

export function toControlPath(apiPath: string): string {
  return PARTY_FIELDS[apiPath] ?? apiPath.replace(/\[(\d+)\]/g, '.$1');
}

/** Shows each API validation message on its control; returns the messages no control owns. */
export function applyServerErrors(form: SaleForm, fieldErrors: FieldErrors): string[] {
  const unmatched: string[] = [];
  for (const [path, messages] of Object.entries(fieldErrors)) {
    const control = path === '' ? null : form.get(toControlPath(path));
    if (control) {
      control.setErrors({ server: messages.join(' ') });
      control.markAsTouched();
    } else {
      unmatched.push(...messages);
    }
  }
  return unmatched;
}
```

`portal/src/app/shared/forms/field-errors.ts`:
```ts
import { ValidationErrors } from '@angular/forms';

/** The one place form error copy lives, so every screen words errors the same way. */
export function describeFieldError(errors: ValidationErrors | null, label: string): string | null {
  if (!errors) return null;
  if (typeof errors['server'] === 'string') return errors['server'];
  if (errors['required']) return `${label} is required.`;
  if (errors['pattern']) return `${label} must be a valid ID (a GUID).`;
  if (errors['email']) return 'Enter an email address like name@example.com.';
  if (errors['maxlength']) return `${label} must have at most ${errors['maxlength'].requiredLength} characters.`;
  if (errors['minlength']) return `${label} must have at least ${errors['minlength'].requiredLength} characters.`;
  if (errors['quantityMin']) return 'Quantity must be a whole number of at least 1.';
  if (errors['quantityMax']) return "It's not possible to sell above 20 identical items.";
  if (errors['pricePositive']) return 'Unit price must be above 0.';
  if (errors['priceDecimals']) return 'Unit price can have at most 2 decimal places.';
  if (errors['phone']) return 'Use the international format, e.g. +5511987654321.';
  if (errors['password']) return 'The password must meet every rule below.';
  return `${label} is invalid.`;
}
```

- [ ] **Step 5: Run the tests and see them pass**

Run: `cd portal && rtk npm run test:ci`
Expected: PASS. The total is 68 plus this task's tests (sales-api 7, auth-flow 2, credentials 3, sale-form 6, field-errors 6) = 92. Record the exact number reported here as the unit baseline; Task 11B and Part 2 check against it.

- [ ] **Step 6: Wire the app config and routes**

These are needed before any screen. The route components are created in Tasks 13–19; until then each `loadComponent` points at a file that doesn't exist yet, so **write each route as its screen task creates the component**. Write this `app.config.ts` now.

`portal/src/app/app.config.ts` (keep any provider `ng new` generated, such as `provideBrowserGlobalErrorListeners()`, and add the rest):
```ts
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, TitleStrategy, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { apiErrorInterceptor, authInterceptor } from './core/api/interceptors';
import { PortalTitleStrategy } from './layout/title-strategy';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withFetch(), withInterceptors([authInterceptor, apiErrorInterceptor])),
    { provide: TitleStrategy, useClass: PortalTitleStrategy },
  ],
};
```

`portal/src/app/layout/title-strategy.ts`:
```ts
import { inject, Injectable } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';

@Injectable()
export class PortalTitleStrategy extends TitleStrategy {
  private readonly title = inject(Title);

  override updateTitle(snapshot: RouterStateSnapshot): void {
    const page = this.buildTitle(snapshot);
    this.title.setTitle(page ? `${page} · DeveloperStore` : 'DeveloperStore');
  }
}
```

The target shape of `portal/src/app/app.routes.ts` once Tasks 13–19 are done. Leave it as `export const routes: Routes = [];` for now:
```ts
import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/guards';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'sales' },
  { path: 'sign-in', title: 'Sign in', canActivate: [guestGuard], loadComponent: () => import('./features/auth/sign-in/sign-in').then(m => m.SignIn) },
  { path: 'sign-up', title: 'Create an account', canActivate: [guestGuard], loadComponent: () => import('./features/auth/sign-up/sign-up').then(m => m.SignUp) },
  {
    path: '',
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    loadComponent: () => import('./layout/shell/shell').then(m => m.Shell),
    children: [
      { path: 'sales', title: 'Sales', loadComponent: () => import('./features/sales/list/sales-list').then(m => m.SalesList) },
      { path: 'sales/new', title: 'New sale', loadComponent: () => import('./features/sales/editor/sale-editor').then(m => m.SaleEditor) },
      { path: 'sales/:id', title: 'Sale', loadComponent: () => import('./features/sales/detail/sale-detail').then(m => m.SaleDetail) },
      { path: 'sales/:id/edit', title: 'Edit sale', loadComponent: () => import('./features/sales/editor/sale-editor').then(m => m.SaleEditor) },
      { path: 'account', title: 'Account', loadComponent: () => import('./features/account/account').then(m => m.Account) },
    ],
  },
  { path: '**', title: 'Page not found', loadComponent: () => import('./features/not-found/not-found').then(m => m.NotFound) },
];
```

Run: `cd portal && rtk npm run build`
Expected: the build succeeds.

- [ ] **Step 7: Commit**

```bash
rtk git add portal/src/app/core/api/auth-api.ts portal/src/app/core/api/users-api.ts portal/src/app/core/api/sales-api.ts \
  portal/src/app/core/api/sales-api.spec.ts portal/src/app/core/auth/auth-flow.ts portal/src/app/core/auth/auth-flow.spec.ts \
  portal/src/app/core/auth/credentials.ts portal/src/app/core/auth/credentials.spec.ts portal/src/app/core/catalog \
  portal/src/app/features/sales/editor/sale-form.ts portal/src/app/features/sales/editor/sale-form.spec.ts \
  portal/src/app/shared/forms portal/src/app/testing/sale-fixture.ts portal/src/app/app.config.ts portal/src/app/layout/title-strategy.ts
rtk git commit -m "feat(portal): call the Sales and Users APIs and model the sale form"
```

---

## Phase 2b: Close Part 1

### Task 11A: CI job for the build and unit tests

**Files:** Modify `.github/workflows/pull-request.yml`.

- [ ] **Step 1: Check the current `actions/setup-node` major**

Run: `rtk gh api repos/actions/setup-node/releases/latest --jq .tag_name`. Use that major below; the plan assumes `v6`. Read the workflow first and match the `actions/checkout` major the other jobs use; the plan assumes `v7`.

- [ ] **Step 2: Add the `portal` job**, after `docker-image`, with the same indentation:
```yaml
  portal:
    name: Portal build and unit tests
    runs-on: ubuntu-latest
    timeout-minutes: 15
    defaults:
      run:
        working-directory: portal
    steps:
      - uses: actions/checkout@v7

      - uses: actions/setup-node@v6
        with:
          node-version: 24
          cache: npm
          cache-dependency-path: portal/package-lock.json

      - run: npm ci
      - run: npm run build
      - run: npm run test:ci
```
Part 2's Task 26 adds the `portal-e2e` job next to it.

- [ ] **Step 3: Check the YAML parses**

Run: `python -c "import yaml,sys;yaml.safe_load(open('.github/workflows/pull-request.yml'));print('ok')"`
Expected: `ok`.

- [ ] **Step 4: Commit**

```bash
rtk git add .github/workflows/pull-request.yml
rtk git commit -m "ci: build and unit-test the portal"
```

---

### Task 11B: Verify, review, then open the Part 1 pull request into `develop`

- [ ] **Step 1: Full verification from a clean install**

Run from the repo root:
```bash
cd portal && rtk npm ci && rtk npm run build && rtk npm run test:ci
cd .. && rtk dotnet test Ambev.DeveloperEvaluation.sln
```
Expected:
- the build succeeds;
- the unit tests all pass, matching Task 11's recorded baseline (92, or the number recorded there);
- the .NET suites are unchanged and pass.

- [ ] **Step 2: Check the branch holds only what it should**

Run: `rtk git diff --stat develop...HEAD -- . ':!portal'`
Expected: only `.github/workflows/pull-request.yml`, PRODUCT.md, the shape brief and the direction files, and the two plan files. No `.cs`, `.csproj`, `appsettings` or `docker-compose.yml` changes.

Run: `rtk git ls-files portal | grep -E 'playwright|e2e/|Dockerfile|nginx|features/(auth|account|not-found)|sales/(list|detail)/'`
Expected: no output. Nothing from Part 2 slipped in.

- [ ] **Step 3: Code review**

Use `superpowers:requesting-code-review` against `develop...HEAD`, with the focus areas:
- the error mapper against `ExceptionHandlingMiddleware`'s `{type, error, detail}` shapes;
- the strict parameter codec and the local-day to offset conversion;
- the session and 401 flow, and the return-URL safety;
- pricing rounding against `MidpointRounding.AwayFromZero`;
- the sale form's request mapping against the API DTOs.

Fix what's confirmed, re-run `rtk npm run test:ci`, and commit (`fix(portal): ...`).

- [ ] **Step 4: Push and open the pull request** (`superpowers:finishing-a-development-branch`, with the option already chosen)

```bash
rtk git push -u origin feature/portal-foundation
rtk gh pr create --base develop --title "feat(portal): scaffold the Angular 21 portal and its tested core" --body-file /tmp/portal-foundation-pr.md
```
Write `/tmp/portal-foundation-pr.md` first, with:
- that this is Part 1 of 2 for ticket 14, and that the screens come in Part 2;
- what's in it: the workspace, the design process so far (PRODUCT.md, the shape brief, the direction and the comp link) and the core modules;
- how to run the unit tests;
- the test count from Step 1.

No AI attribution.

- [ ] **Step 5: Handoff**

Use `ai-memory-handoff`: "Ticket 14 Part 1 in PR #<n>, waiting for the user's merge. Approved comp: <url>. Next: Part 2, `2026-09-28-14b-angular-portal-screens.md`, Task 11C, after the merge."

---

## Self-review

- **Coverage.** This part delivers the user's asks that don't need a screen: Angular 21 (Decision 1, Task 2); impeccable and the design pass (Tasks 3–5, "/design" in Task 5); the logic every feature in the matrix relies on (Tasks 6–11). Screens, Playwright, videos and compose are Part 2's.
- **Mergeable on its own.** The build and unit tests pass and run in CI (Task 11A). No script, dependency or file refers to Playwright, a screen or compose yet.
- **Placeholders.** The paths impeccable chooses for PRODUCT.md, the brief and the direction files are read from its `context` output (Task 3 Step 1) rather than guessed.
- **Name consistency.** Part 2 uses these names exactly as this part defines them:
  - `SESSION_STORAGE_KEY`, `sessionFromAuthResponse`, `useSession`;
  - `SalesQuery` fields and `queryToParams`/`queryFromParams`/`queryToHttpParams`;
  - `previewLine`/`previewSale`/`nextTier`;
  - `createSaleForm`/`addItem`/`toCreateRequest`/`toUpdateRequest`/`patchFromSale`/`applyServerErrors`;
  - `AuthFlow.signIn`/`signUp`/`signOut`/`deleteAccount`;
  - `formatMoney`;
  - the catalog constants `CUSTOMERS`/`BRANCHES`/`PRODUCTS`.

  If a Part 1 review fix renames any of them, update Part 2's plan in the same commit.
