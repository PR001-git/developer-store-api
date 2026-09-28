# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Stack

Angular 21 (standalone workspace in `portal/`), delegated by the ticket that requested this portal. Talks to the existing .NET Sales API only over relative `/api/...` calls through a dev-server/nginx proxy; no CORS, no new endpoints.

## Users

- **Primary:** the evaluation's reviewers — developers assessing this Ambev developer-evaluation submission, who want to see every Sales API feature working end to end and understand the business rules (discount tiers, cancellation, soft delete) as they use the UI.
- **In the product's own story:** sales staff at a beverage distributor's branches who record and correct sales day to day.

## Product Purpose

A browser client that exposes everything the Sales API offers: sign up, sign in, create a sale and watch tiered discounts apply, list sales with paging/ordering/filters, open a sale, update it, cancel an item, cancel the sale, soft-delete it, and view or delete the signed-in user's own account. It exists to make the API's business rules legible and operable, not just callable.

## Positioning

Unlike a generic CRUD admin panel, this portal makes the discount-tier logic (R1/R3/D3: quantity-based percentage breaks) visible and understandable at the moment a sale is being built — a live pricing preview with a plain-language nudge ("Add 1 more for 10% off") — rather than only revealing the result after saving.

## Operating Context

- Runs against the existing Docker Compose stack: the API on `:8080` (dev) or behind nginx (compose), Postgres, no other services.
- A signed-in session is required for every feature except sign-up and sign-in; the token is an 8-hour JWT.
- External identities (customer, branch, product) are not owned by the API — it stores only an id and a name for each. The portal ships a small demo catalog of readable names/GUIDs so a reviewer isn't forced to type raw GUIDs; free-entry ("Other …") always remains available.
- No aggregate/reporting endpoint exists, so there is no dashboard or KPI surface.
- Domain events are written to the API's log only, not exposed over HTTP; the README points to `docker compose logs` for anyone who wants to see them.

## Capabilities and Constraints

- Every back-end feature (04–12) must be reachable from the UI: auth, sales CRUD, item/sale cancellation, soft delete, paging/ordering/filtering, and the user's own account view/delete.
- No component library (Angular Material, PrimeNG, etc.) — a deliberate constraint to avoid a generic, recognizable "AI slop" look. Native form controls (`<select>`, `type="date"`, `type="datetime-local"`) are used wherever the future e2e contract depends on them; `@angular/cdk` supplies only hard-to-get-right behavior (focus trap, live announcer).
- No Ambev name, logo, or brand colors — the product is "DeveloperStore," and imitating a real brand is out of scope.
- English UI copy.
- **Money: BRL, formatted with `en-US` number conventions** (e.g. `R$20.25`) — confirmed in this interview.
- Desktop-first, but fully usable on a phone.
- Public sign-up is Customer-only; no endpoint enforces role-based access, and the portal does not add a roles UI on top of that gap.
- `GET/DELETE /api/users/{id}` are only ever called with the signed-in user's own id (from the token); the API itself doesn't check ownership, and the portal doesn't paper over that.

## Brand Commitments

None beyond the name "DeveloperStore" itself; no logo, no color commitment, no existing visual identity to preserve. This is a from-scratch product name, not a reference to any real company.

## Evidence on Hand

No customer, testimonial, or press content exists or should be fabricated. The only real content is the API's own data shapes and business rules (read from `SalesController`, `UsersController`, `AuthController`, their DTOs, and the validation/exception-handling middleware) and a small hand-authored demo catalog of customers/branches/products for filling in forms during review.

## Product Principles

1. Every business rule the API enforces should be visible in the UI at the moment it applies, not just after the fact.
2. Never invent scope the API doesn't have (no dashboards, no roles UI, no fabricated aggregates).
3. Native, accessible controls over a component library's default look — precision over decoration.
4. The API stays the single source of truth; anything the UI previews (like pricing) is labeled as a preview.
5. A reviewer with no prior context should be able to exercise every feature without needing to fabricate test data by hand.

## Accessibility & Inclusion

WCAG 2.2 AA is the required standard; no additional product-specific accessibility need was identified beyond it (confirmed in this interview).
