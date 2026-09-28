# DeveloperStore Sales API

A prototype Sales API for the Ambev DeveloperStore developer evaluation, built on the .NET 8 template the challenge provides. It records sales with complete CRUD, applies the quantity-based discount rules inside a DDD aggregate, requires a JWT on every sales endpoint, and writes the `SaleCreated`, `SaleModified`, `SaleCancelled` and `ItemCancelled` events to the application log. An Angular portal in `portal/` is the client for it.

- The challenge statement: [.doc/challenge.md](.doc/challenge.md)
- The API conventions it follows (paging, ordering, filtering, errors): [.doc/general-api.md](.doc/general-api.md)
- The full design spec: [docs/superpowers/specs/2026-09-24-sales-api-design.md](docs/superpowers/specs/2026-09-24-sales-api-design.md)

## Contents

- [Run it](#run-it)
- [Portal](#portal)
- [Get a token](#get-a-token)
- [Endpoints](#endpoints)
- [Errors](#errors)
- [Business rules](#business-rules)
- [Architecture](#architecture)
- [Decisions](#decisions)
- [Tests and coverage](#tests-and-coverage)
- [Migrations](#migrations)
- [Template fixes](#template-fixes)
- [Template issues left as-is](#template-issues-left-as-is)
- [Known limitations](#known-limitations)

## Run it

You need the .NET 8 SDK or newer and Docker Desktop (on Windows, with WSL2). Run the commands from the repository root.

### Everything in Docker

```bash
docker compose up --build
```

| What | Where |
|---|---|
| API | `http://localhost:8080` |
| Swagger UI | `http://localhost:8080/swagger` |
| Health check | `http://localhost:8080/health` |
| Portal | `http://localhost:8081` |
| PostgreSQL 13 | `localhost:5432`, database `developer_evaluation`, user `developer`, password `ev@luAt10n` |

The API runs in the Development environment, where `Database:MigrateOnStartup` is on, so it applies pending EF Core migrations at startup. `docker compose down -v` stops the containers and deletes the data.

### The API with `dotnet run`

Start only the database, then run the API:

```bash
docker compose up -d --wait ambev.developerevaluation.database
dotnet run --project src/Ambev.DeveloperEvaluation.WebApi
```

The API listens on `http://localhost:5119` (Swagger at `/swagger`) in Development. It uses the connection string in `src/Ambev.DeveloperEvaluation.WebApi/appsettings.json`, which points at `localhost:5432`.

### The whole flow in one file

[Ambev.DeveloperEvaluation.WebApi.http](src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http) runs in Visual Studio 2022, or in VS Code with the REST Client extension. Send its requests from top to bottom against `docker compose up`: sign up, log in, create a sale with and without a sale number, get it, list sales (paged, ordered, filtered), update, cancel an item, cancel a sale, delete. Each request's title gives the expected status. The file can be re-run: the sign-up then returns 409, sent sale numbers are random, and every id comes from an earlier response. For `dotnet run`, set `@baseUrl` to `http://localhost:5119`.

## Portal

An Angular 21 client for the API, in [portal/](portal/). `docker compose up --build` builds it and serves it with nginx at `http://localhost:8081`. nginx forwards `/api` to the API container, so the browser stays on one origin and the API needs no CORS.

For development, with the API running on `:8080` (`docker compose up`):

```bash
cd portal
npm ci
npm start       # http://localhost:4200; the dev server proxies /api to http://localhost:8080
npm run test:ci # Vitest unit tests
```

The portal only calls the API's relative `/api/...` routes, with the JWT from `POST /api/auth` on every sales request. Its sign-up creates active Customer accounts, as the API allows. Customers, branches and products are external identities in the API, so the portal ships a small demo catalog to pick from.

What's here so far is the foundation: the workspace, the design direction ([PRODUCT.md](PRODUCT.md)) and the tested core (the API clients and error mapping, the session and route guards, the list query, the discount preview and the sale form). The screens are built on top of it in the next portal pull request; until then, the served page is an empty shell.

## Get a token

Every `/api/sales` route needs `Authorization: Bearer <token>`. The Users and Auth routes are anonymous. A token is valid for 8 hours.

1. Create an **Active** user with `POST /api/users`. Public sign-up creates Customer accounts only.
2. Log in with `POST /api/auth`. The token is at `data.token`.
3. Send it as `Authorization: Bearer <token>`. In Swagger, click **Authorize** and paste the token without the `Bearer` prefix.

```bash
curl -X POST http://localhost:8080/api/users -H "Content-Type: application/json" \
  -d '{"username":"customer","password":"Customer@123","phone":"+5511999999999","email":"customer@developerstore.com","status":"Active","role":"Customer"}'

curl -X POST http://localhost:8080/api/auth -H "Content-Type: application/json" \
  -d '{"email":"customer@developerstore.com","password":"Customer@123"}'
```

The sign-up answers 201. The same email again, in any letter case, answers 409.

```json
{
  "success": true,
  "message": "User created successfully",
  "data": {
    "id": "0b7f7a8e-2f4e-4c1a-9d53-8a3c2f1e6b10",
    "name": "customer",
    "email": "customer@developerstore.com",
    "phone": "+5511999999999",
    "role": "Customer",
    "status": "Active"
  }
}
```

The login answers 200:

```json
{
  "success": true,
  "message": "User authenticated successfully",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6ImN1c3RvbWVyIn0.c2lnbmF0dXJl",
    "email": "customer@developerstore.com",
    "name": "customer",
    "role": "Customer"
  }
}
```

A password needs at least 8 characters, with an upper-case letter, a lower-case letter, a digit and a special character. `status` and `role` travel as strings, such as `"Active"` and `"Customer"`; numbers are rejected. A `role` of `Admin` or `Manager` is a 400. A user who isn't active can't log in (401).

## Endpoints

Success bodies use the template envelope `{success, message, data}`, and errors use `{type, error, detail}` (see [Errors](#errors)). JSON is camelCase, and dates are UTC ISO 8601.

| Method | Route | Auth | Success | Errors |
|---|---|---|---|---|
| `POST` | `/api/users` | none | 201 with the user | 400, 409 |
| `GET` | `/api/users/{id}` | none | 200 with the user | 400, 404 |
| `DELETE` | `/api/users/{id}` | none | 200 | 400, 404 |
| `POST` | `/api/auth` | none | 200 with the token | 400, 401 |
| `POST` | `/api/sales` | Bearer | 201 with the sale and `Location: /api/sales/{id}` | 400, 401, 409 |
| `GET` | `/api/sales/{id}` | Bearer | 200 with the sale | 400, 401, 404 |
| `GET` | `/api/sales` | Bearer | 200 with a page of sales | 400, 401 |
| `PUT` | `/api/sales/{id}` | Bearer | 200 with the sale | 400, 401, 404, 409 |
| `PATCH` | `/api/sales/{id}/cancel` | Bearer | 200 with the sale | 400, 401, 404, 409 |
| `PATCH` | `/api/sales/{id}/items/{itemId}/cancel` | Bearer | 200 with the sale | 400, 401, 404, 409 |
| `DELETE` | `/api/sales/{id}` | Bearer | 200 with `{success, message}` | 400, 401, 404 |

A malformed id in a route is a 400.

### Create a sale

`POST /api/sales`:

```json
{
  "saleNumber": "S-000123",
  "saleDate": "2026-09-24T14:30:00Z",
  "customerId": "3f2b8c1e-1111-4a5b-9c2d-000000000001",
  "customerName": "Maria Silva",
  "branchId": "3f2b8c1e-2222-4a5b-9c2d-000000000002",
  "branchName": "Filial Centro",
  "items": [
    { "productId": "3f2b8c1e-3333-4a5b-9c2d-000000000003", "productName": "Cerveja 350ml", "quantity": 5, "unitPrice": 4.50 }
  ]
}
```

`saleNumber` is optional. Leave it out, or send `null`, and the server issues the next free number: `S-000001`, `S-000002`, and so on. A number that is sent must be unique among all sales, deleted ones included, or the answer is 409. The request has no discount fields: the domain computes them.

The answer is 201 with `Location: /api/sales/{id}`:

```json
{
  "success": true,
  "message": "Sale created successfully",
  "data": {
    "id": "7f9c2a44-5555-4d1e-8a3b-000000000010",
    "saleNumber": "S-000123",
    "saleDate": "2026-09-24T14:30:00Z",
    "customerId": "3f2b8c1e-1111-4a5b-9c2d-000000000001",
    "customerName": "Maria Silva",
    "branchId": "3f2b8c1e-2222-4a5b-9c2d-000000000002",
    "branchName": "Filial Centro",
    "totalAmount": 20.25,
    "isCancelled": false,
    "createdAt": "2026-09-24T14:31:02.1234567Z",
    "updatedAt": null,
    "items": [
      {
        "id": "7f9c2a44-6666-4d1e-8a3b-000000000011",
        "productId": "3f2b8c1e-3333-4a5b-9c2d-000000000003",
        "productName": "Cerveja 350ml",
        "quantity": 5,
        "unitPrice": 4.50,
        "discountPercentage": 10,
        "discountAmount": 2.25,
        "totalAmount": 20.25,
        "isCancelled": false
      }
    ]
  }
}
```

### Get a sale

`GET /api/sales/{id}` answers 200 with the same shape and `"message": "Sale retrieved successfully"`, or 404 for an unknown or deleted sale. Values read back from PostgreSQL keep two decimal places, so the line above reads `"discountPercentage": 10.00` here and `10` in the create response. They are the same number.

### List sales

`GET /api/sales` takes the paging, ordering and filtering parameters of [.doc/general-api.md](.doc/general-api.md):

| Parameter | Meaning |
|---|---|
| `_page` | Page number: default 1, at least 1 |
| `_size` | Page size: default 10, from 1 to 100 |
| `_order` | Comma-separated `field [asc\|desc]`, quoted or not. The direction defaults to `asc`. Fields: `saleNumber`, `saleDate`, `customerName`, `branchName`, `totalAmount`, `isCancelled`. The default is `saleDate desc`, and `id` always breaks ties |
| `saleNumber`, `customerName`, `branchName` | Case-insensitive: `value*` starts with, `*value` ends with, `*value*` contains, no `*` equals. Only a leading or trailing `*` is a wildcard |
| `customerId`, `branchId`, `isCancelled` | Exact match |
| `_minSaleDate`, `_maxSaleDate` | Inclusive. A date without an offset is UTC, and a `_maxSaleDate` at midnight (`2026-01-31`) covers that whole day |
| `_minTotalAmount`, `_maxTotalAmount` | Inclusive |

An unknown or repeated `_order` field, a `_size` outside 1–100, a minimum above its maximum, or a malformed value is a 400. A page past the end has an empty `data` and correct totals.

```
GET /api/sales?_page=1&_size=10&_order=saleDate%20desc,totalAmount&customerName=Maria*&_minSaleDate=2026-09-01
```

```json
{
  "success": true,
  "message": "Sales retrieved successfully",
  "data": [
    {
      "id": "7f9c2a44-5555-4d1e-8a3b-000000000010",
      "saleNumber": "S-000123",
      "saleDate": "2026-09-24T14:30:00Z",
      "customerId": "3f2b8c1e-1111-4a5b-9c2d-000000000001",
      "customerName": "Maria Silva",
      "branchId": "3f2b8c1e-2222-4a5b-9c2d-000000000002",
      "branchName": "Filial Centro",
      "totalAmount": 20.25,
      "isCancelled": false,
      "createdAt": "2026-09-24T14:31:02.123456Z",
      "updatedAt": null,
      "items": [
        {
          "id": "7f9c2a44-6666-4d1e-8a3b-000000000011",
          "productId": "3f2b8c1e-3333-4a5b-9c2d-000000000003",
          "productName": "Cerveja 350ml",
          "quantity": 5,
          "unitPrice": 4.50,
          "discountPercentage": 10.00,
          "discountAmount": 2.25,
          "totalAmount": 20.25,
          "isCancelled": false
        }
      ]
    }
  ],
  "currentPage": 1,
  "totalPages": 1,
  "totalItems": 1
}
```

### Update a sale

`PUT /api/sales/{id}` takes the create body without `saleNumber` (a sent `saleNumber` is ignored). It answers 200 with the sale and `"message": "Sale updated successfully"`. It replaces the date, customer and branch, and it reconciles the items by `productId` (see [Business rules](#business-rules)). For the sale above, this body moves the beer to 10 items (20%) and adds a second product, so the total becomes 52.00:

```json
{
  "saleDate": "2026-09-24T14:30:00Z",
  "customerId": "3f2b8c1e-1111-4a5b-9c2d-000000000001",
  "customerName": "Maria Silva",
  "branchId": "3f2b8c1e-2222-4a5b-9c2d-000000000002",
  "branchName": "Filial Centro",
  "items": [
    { "productId": "3f2b8c1e-3333-4a5b-9c2d-000000000003", "productName": "Cerveja 350ml", "quantity": 10, "unitPrice": 4.50 },
    { "productId": "3f2b8c1e-4444-4a5b-9c2d-000000000004", "productName": "Refrigerante 2L", "quantity": 2, "unitPrice": 8.00 }
  ]
}
```

A cancelled sale can't be updated (409).

### Cancel a sale

`PATCH /api/sales/{id}/cancel` has no body. It answers 200 with the sale, `"isCancelled": true` and `"message": "Sale cancelled successfully"`. The items and the total stay as they were. A cancelled sale is read-only: updating it, cancelling it again or cancelling one of its items is a 409. It can still be deleted.

### Cancel an item

`PATCH /api/sales/{id}/items/{itemId}/cancel` has no body. The line leaves the total. Cancelling the last active line also cancels the sale, and its total becomes 0. An item that isn't in the sale is a 404, and an item that is already cancelled is a 409. For a sale with the beer line and a line of 2 × 8.00, cancelling the beer answers 200:

```json
{
  "success": true,
  "message": "Sale item cancelled successfully",
  "data": {
    "id": "7f9c2a44-7777-4d1e-8a3b-000000000020",
    "saleNumber": "S-000124",
    "saleDate": "2026-09-24T16:00:00Z",
    "customerId": "3f2b8c1e-1111-4a5b-9c2d-000000000001",
    "customerName": "Maria Silva",
    "branchId": "3f2b8c1e-2222-4a5b-9c2d-000000000002",
    "branchName": "Filial Centro",
    "totalAmount": 16.00,
    "isCancelled": false,
    "createdAt": "2026-09-24T16:01:10.123456Z",
    "updatedAt": "2026-09-24T16:02:30.6543210Z",
    "items": [
      {
        "id": "7f9c2a44-8888-4d1e-8a3b-000000000021",
        "productId": "3f2b8c1e-3333-4a5b-9c2d-000000000003",
        "productName": "Cerveja 350ml",
        "quantity": 5,
        "unitPrice": 4.50,
        "discountPercentage": 10.00,
        "discountAmount": 2.25,
        "totalAmount": 20.25,
        "isCancelled": true
      },
      {
        "id": "7f9c2a44-9999-4d1e-8a3b-000000000022",
        "productId": "3f2b8c1e-4444-4a5b-9c2d-000000000004",
        "productName": "Refrigerante 2L",
        "quantity": 2,
        "unitPrice": 8.00,
        "discountPercentage": 0.00,
        "discountAmount": 0.00,
        "totalAmount": 16.00,
        "isCancelled": false
      }
    ]
  }
}
```

### Delete a sale

`DELETE /api/sales/{id}` is a soft delete. The rows stay in the database, but the sale disappears from `GET` (404) and from the list, and its number stays taken. Cancelled sales can be deleted too. No event is published.

```json
{
  "success": true,
  "message": "Sale deleted successfully"
}
```

## Errors

Every error body is `{type, error, detail}`, as [.doc/general-api.md](.doc/general-api.md) describes:

| Status | `type` | `error` | When |
|---|---|---|---|
| 400 | `ValidationError` | Invalid input data | A validator or model binding rejects the input. `detail` lists each failure as `Property: message`, joined with `; ` |
| 401 | `AuthenticationError` | Invalid authentication token | No token, or an invalid or expired one |
| 401 | `AuthenticationError` | Authentication failed | A wrong email or password, or a user who isn't active |
| 404 | `ResourceNotFound` | Resource not found | An unknown or deleted sale, an unknown item or user |
| 409 | `BusinessRuleViolation` | Business rule violation | A business rule refuses the change: a cancelled sale, a taken sale number or email |
| 409 | `ConcurrencyConflict` | Concurrent modification | Another request changed the sale first. Reload it and try again |
| 500 | `InternalServerError` | Internal server error | Anything unexpected. The stack trace is logged, never returned |

```json
{
  "type": "ValidationError",
  "error": "Invalid input data",
  "detail": "Items[0].Quantity: It's not possible to sell above 20 identical items"
}
```

```json
{
  "type": "AuthenticationError",
  "error": "Invalid authentication token",
  "detail": "The provided authentication token has expired or is invalid"
}
```

```json
{
  "type": "AuthenticationError",
  "error": "Authentication failed",
  "detail": "Invalid credentials"
}
```

```json
{
  "type": "ResourceNotFound",
  "error": "Resource not found",
  "detail": "The sale with ID 7f9c2a44-5555-4d1e-8a3b-000000000010 does not exist"
}
```

```json
{
  "type": "BusinessRuleViolation",
  "error": "Business rule violation",
  "detail": "Sale S-000123 is cancelled and cannot be modified"
}
```

ASP.NET Core's own 404 (unknown route), 405 (wrong method) and 415 (missing or wrong `Content-Type`) keep the framework's default responses. See [Known limitations](#known-limitations).

## Business rules

The discount depends on the quantity of identical items (the same `productId`) in one line:

| Quantity | Discount |
|---|---|
| 1–3 | none |
| 4–9 | 10% |
| 10–20 | 20% |
| 0 or less | rejected (400) |
| above 20 | rejected (400): "It's not possible to sell above 20 identical items" |

**Why 4 items get 10% (decision D3).** The challenge contradicts itself about exactly 4 items. "Purchases above 4 identical items have a 10% discount" leaves 4 out, while "4+ items: 10% discount" and "Purchases below 4 items cannot have a discount" put it in. Two of the three statements discount 4, so 4 gets 10%.

- **Money** is `decimal`. A line's gross amount is quantity × unit price. The discount is gross × percentage ÷ 100, rounded to 2 places with midpoint away from zero. The line total is gross − discount, and the sale total is the sum of the active lines' totals. A unit price is above 0, with at most 2 decimal places.
- **The domain computes every discount.** Requests have no discount fields.
- **Identical items:** a create or update body that repeats a `productId` is a 400, and a sale holds at most one active line per product. If an update sends a product whose line was cancelled, the sale gets a new active line next to the cancelled one, which stays as history.
- **At least one item** in every create and update body. An open sale always has an active item: cancelling the last one cancels the sale, and its total becomes 0.
- **A cancelled sale is read-only:** update, cancel and cancel item answer 409, and delete still works. Cancelling a sale keeps its items and total as the historical record.
- **Cancelling an item** requires the item to be in the sale (else 404) and still active (else 409). The item leaves the total.
- **An update (PUT) reconciles lines by `productId`.** An active line whose product is sent gets the new quantity, price and name, and its discount is recalculated. A product with no active line gets a new line. An active line whose product isn't sent is cancelled, not deleted. Cancelled lines never change.
- **Delete is soft.** The sale disappears from reads and lists, its rows stay, and no event is published.
- **Sale numbers:** optional on create, and trimmed. When it's omitted, the server takes the next value of the `sale_number_seq` sequence, formats it as `S-` plus six digits, and skips numbers that clients already took. A sent number must be unique across all sales, deleted ones included; the comparison is exact. The number never changes.
- **Dates:** `saleDate` is required. A value without an offset is read as UTC, and a value with an offset is converted to UTC. `createdAt` is set on create, and `updatedAt` on every update, cancel, item cancel and delete.
- **Events** are published only after the save succeeds: `SaleCreated`; `SaleModified` on every PUT, even one that changes nothing; `SaleCancelled`; `ItemCancelled`. A PUT that drops lines publishes one `ItemCancelled` per line, then `SaleModified`. Cancelling the last item publishes `ItemCancelled`, then `SaleCancelled`. One handler writes each event to the log as `Sale event {EventName} published {@Event}`, in place of a message broker:

```bash
docker compose logs ambev.developerevaluation.webapi | grep "Sale event"
```

## Architecture

The template's projects and dependency direction are unchanged. Sales adds a DDD aggregate in Domain, one MediatR use case per operation in Application, EF Core persistence in ORM, and a controller in WebApi.

```mermaid
flowchart LR
  WebApi --> IoC
  IoC --> Application
  IoC --> ORM
  IoC --> Common
  Application --> Domain
  ORM --> Domain
  Domain --> Common
```

| Project | What it holds |
|---|---|
| `src/Ambev.DeveloperEvaluation.Domain` | The `Sale` aggregate and its `SaleItem` lines, the `ExternalIdentity` value object (customer, branch and product: an id plus a copied name), `DiscountPolicy`, the domain events and the repository contracts |
| `src/Ambev.DeveloperEvaluation.Application` | One folder per use case (command or query, FluentValidation validator, handler), `SaleResult` and its AutoMapper profile, the `_order` parser, event publishing and the event-log handler |
| `src/Ambev.DeveloperEvaluation.ORM` | `DefaultContext`, the EF Core mappings and migrations, `SaleRepository` and `SaleNumberGenerator`. Handlers load the tracked aggregate, change it and save it; the list query uses `AsNoTracking` and split queries |
| `src/Ambev.DeveloperEvaluation.IoC` | Dependency-injection registrations, one module initializer per layer |
| `src/Ambev.DeveloperEvaluation.WebApi` | Controllers, request and response contracts, the exception middleware, and the JSON, Swagger and JWT wiring |
| `src/Ambev.DeveloperEvaluation.Common` | JWT, password hashing, the MediatR `ValidationBehavior`, logging and health checks |
| `tests/Ambev.DeveloperEvaluation.Unit` | Domain, validators, handlers (with NSubstitute), mappings, middleware and controllers. No Docker |
| `tests/Ambev.DeveloperEvaluation.Integration` | Repositories and queries against PostgreSQL 13 in Testcontainers |
| `tests/Ambev.DeveloperEvaluation.Functional` | HTTP through `WebApplicationFactory<Program>`, against PostgreSQL 13 in Testcontainers |
| `portal` | The Angular 21 client (see [Portal](#portal)), served by nginx in compose |

A command travels like this:

```mermaid
sequenceDiagram
  participant C as Client
  participant API as SalesController
  participant M as MediatR + ValidationBehavior
  participant H as Handler
  participant S as Sale
  participant R as SaleRepository
  participant L as Event log handler
  C->>API: HTTP request + JWT
  API->>M: Send(command)
  M->>H: validated command
  H->>R: load sale / reserve number
  H->>S: domain method (rules, totals, events)
  H->>R: save (SaveChanges)
  H->>L: publish recorded events
  H-->>API: SaleResult
  API-->>C: 2xx envelope, or {type, error, detail}
```

Stack: .NET 8, ASP.NET Core, EF Core 8 with Npgsql, PostgreSQL 13, MediatR 12, FluentValidation 11, AutoMapper 13 and Serilog. Tests: xUnit, NSubstitute, Bogus, FluentAssertions 6, Testcontainers and `Microsoft.AspNetCore.Mvc.Testing`. Portal: Angular 21 with Vitest, served by nginx.

## Decisions

The [design spec](docs/superpowers/specs/2026-09-24-sales-api-design.md) records each decision in full.

| # | Decision | Why |
|---|---|---|
| D1 | A rich `Sale` aggregate enforces the rules and records events; handlers stay thin | No code path can skip a rule, and the rules are tested without mocks |
| D2 | Customer, branch and product are `ExternalIdentity(Id, Name)` values: an id plus a copied name, with no foreign key | The challenge's External Identities pattern |
| D3 | Quantity 4 gets 10% | Two of the challenge's three statements discount 4 (see [Business rules](#business-rules)) |
| D4 | The sale number is optional: generated from a PostgreSQL sequence when omitted, unique when sent, never changed | Supports numbers issued by a till as well as ad-hoc sales |
| D5 | Cancelling the last active item cancels the sale | An open sale always has an active item |
| D6 | A PUT cancels the active lines missing from its body instead of deleting them | Nothing leaves the database, and each removal is an `ItemCancelled` event |
| D7 | DELETE is a soft delete with a global EF Core query filter | History stays, and no query can forget the filter |
| D8 | The aggregate records events; handlers publish them through MediatR only after the save; one handler logs them | Unsaved changes are never announced, and the challenge accepts logging |
| D9 | Optimistic concurrency on PostgreSQL's `xmin` | Two simultaneous changes can't both save totals computed from stale state; the loser gets 409 |
| D10 | FluentValidation validators in Application, run by the MediatR `ValidationBehavior` | One source of truth for input rules |
| D11 | Success bodies keep the template envelope; errors use `{type, error, detail}` | Matches the existing code and the documented error contract |
| D12 | Flat fields (`customerName`, not `customer.name`) | `general-api.md` filters and orders by JSON field names |
| D13 | `[Authorize]` on Sales, with tokens from the template's Users and Auth endpoints | The JWT extra, without role checks |
| D14 | AutoMapper stays on 13.0.1 | The advisory's fixes are only in commercially licensed versions, and it can't be triggered here (see [Known limitations](#known-limitations)) |
| D15 | The template moved from `template/backend/` to the repository root | The layout in [.doc/project-structure.md](.doc/project-structure.md) |
| D16 | Git Flow and Conventional Commits | `feature/*` branches merged into `develop` with merge commits; `main` is the release branch |

## Tests and coverage

```bash
dotnet test Ambev.DeveloperEvaluation.sln
```

Name the solution. `docker-compose.dcproj` sits next to `Ambev.DeveloperEvaluation.sln`, so a bare `dotnet test` (or `dotnet build`) stops with `MSB1011: Specify which project or solution file to use`. To run one suite:

```bash
dotnet test tests/Ambev.DeveloperEvaluation.Unit
dotnet test tests/Ambev.DeveloperEvaluation.Integration
dotnet test tests/Ambev.DeveloperEvaluation.Functional
```

- **Unit** tests need nothing else.
- **Integration** and **functional** tests need Docker running. Each project starts one `postgres:13` container through Testcontainers, applies the EF Core migrations, and resets the data between test classes. The functional tests also host the API in memory with `WebApplicationFactory<Program>`.

The tests follow the template's conventions: xUnit, NSubstitute, Bogus test-data builders, FluentAssertions, and `Given … When … Then …` names. The portal's unit tests run with `npm run test:ci` in `portal/` (see [Portal](#portal)).

Coverage for all three .NET suites, as an HTML report at `TestResults/CoverageReport/index.html`:

```bash
./coverage-report.sh      # Linux, macOS, Git Bash
coverage-report.bat       # Windows cmd
```

The scripts install the `coverlet.console` and `dotnet-reportgenerator-globaltool` global tools if they're missing, and they need Docker for the integration and functional suites. `coverage-report.bat` ends with the template's `pause`.

## Migrations

`dotnet-ef` 8.0.10 is pinned in `.config/dotnet-tools.json`. Add a migration from the repository root:

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi
```

The migrations live in the ORM project. The startup project must be WebApi, because the design-time factory (`DefaultContextFactory`) reads `appsettings.json` from the startup project's folder. The API applies pending migrations at startup when `Database:MigrateOnStartup` is on, as it is in `appsettings.Development.json`. `dotnet ef` also prints a `[FTL] … HostAbortedException` stack trace: that's how the EF tools stop `Program.Main`, and the command still succeeds.

To check that the model and the migrations agree:

```bash
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi
```

## Template fixes

These template issues affected Sales, the JWT flow or running the app, so they're fixed:

- **Layout:** `template/backend/*` moved to the repository root with `git mv`, so the history is kept. The challenge text moved to `.doc/challenge.md`.
- **Login:** the missing AutoMapper maps for `POST /api/auth` are added. Login used to fail.
- **Get user:** the missing map for `GET /api/users/{id}` is added, and `Username` maps to `name`.
- **Connection string:** Npgsql format with the compose credentials, instead of SQL Server's format.
- **Docker Compose:** HTTP only, without the certificate and user-secrets volumes or the HTTPS redirection; fixed host ports 8080 and 5432; the API gets the database connection string and waits for a `pg_isready` healthcheck; the unused MongoDB and Redis services are gone.
- **Migrations:** the design-time factory, renamed `DefaultContextFactory`, targets the ORM migrations assembly as `Program.cs` does, and `dotnet-ef` is pinned in a local tool manifest.
- **Startup migrations:** pending migrations apply at startup when `Database:MigrateOnStartup` is on. Before, nothing created the schema.
- **Errors:** one global exception middleware, the model-binding 400 and the JWT 401 all return `{type, error, detail}`. The Users and Auth controllers no longer return raw FluentValidation lists.
- **Validation:** validators are registered in dependency injection, so the template's `ValidationBehavior`, which was registered but never ran, now validates every MediatR request.
- **Paging:** `PaginatedResponse.TotalCount` is renamed `TotalItems`, as `general-api.md` names it.
- **Small fixes:** `DomainException` gets the `Ambev.DeveloperEvaluation.Domain.Exceptions` namespace. A missing or short `Jwt:SecretKey` (under 32 bytes) stops startup with an error that names the setting; it used to be a possible-null warning (CS8604).
- **`.http` file:** the weather-forecast request is replaced by the whole Sales flow.
- **Swagger and JSON:** Swagger has a Bearer scheme for its **Authorize** button, shown only on the endpoints that need it, and enums travel as strings.

Found while reviewing the work, beyond the original list:

- **Missing `Users` timestamp columns:** `User` has `CreatedAt` and `UpdatedAt`, but the initial migration didn't create them, so every sign-up failed once migrations ran. A migration adds them.
- **Silent startup errors:** `Program.Main` logged startup exceptions before Serilog had a sink, and exited with code 0. It now rethrows, so a startup failure prints the error and exits non-zero.
- **Warnings and errors never logged:** the Serilog filter meant to hide successful `/health` requests dropped every warning, error and fatal event.
- **Responses wrapped twice:** `BaseController.Ok` wrapped an envelope inside another one, so the login token sat at `data.data.token` and paged bodies were nested. Each body is now built once.
- **Duplicate sign-up:** an email that already exists returned 500. It's now a 409 `BusinessRuleViolation`. A unique index on `Users.Email` makes the check race-safe, and emails are compared case-insensitively.
- **Empty sign-up response:** `POST /api/users` returned only the id, with an empty name and email. It now returns the saved user.
- **Self-assigned roles:** anyone could sign up as `Admin`, and the role landed in the JWT. Public sign-up now accepts only `Customer`, and it rejects roles and statuses sent as numbers.
- **Login leaks:** an unknown email answered faster than a wrong password, and a short wrong password failed as a 400 that revealed the password rules. Both now take the same path to the same 401.
- **Coverage:** the integration and functional projects get `coverlet.msbuild`, so the coverage scripts cover all three suites. `coverage-report.sh` passes its MSBuild properties as `-p:` (Git Bash rewrote `/p:` and MSBuild failed), escapes the commas in its `Exclude` filter as the `.bat` does, and keeps LF line endings through `.gitattributes`: with Windows line endings, bash can't run it.

## Template issues left as-is

These don't affect Sales, the JWT flow or running the app:

- The phone rule differs between the `User` entity (`+` and 11–15 digits) and the sign-up validators (an optional `+` and 2–15 digits).
- `BaseController.GetCurrentUserId()` parses an `int`, although ids are `Guid`s.
- The template's routes differ from `.doc/users-api.md` and `.doc/auth-api.md`: `/api/users`, and `POST /api/auth` by email.
- `IJwtTokenGenerator` is registered twice, and the `CreateUserRequest` AutoMapper map is declared twice.
- One startup log line is written before Serilog is configured.
- The Users endpoints, including `DELETE /api/users/{id}`, are anonymous. The solution folder is spelled "Aplication", `coverage-report.bat` ends with `pause`, and there are two equivalent API Dockerfiles.

## Known limitations

- **No outbox.** Events are published after the commit, so a crash between the commit and the publish loses them. Production would use a transactional outbox.
- **Events are only logged.** There is no message broker.
- **External identity names are snapshots.** They're copied when a sale is written and never refreshed from their source domains.
- **Sale numbers are compared exactly** (case-sensitive). If a client sends the number that the generator hands out at the same moment, the unique index rejects one of the two requests with 409. A retry gets the next number.
- **AutoMapper 13.0.1 has advisory [GHSA-rvv3-g6hj-g44x](https://github.com/advisories/GHSA-rvv3-g6hj-g44x):** a stack overflow (denial of service) when it maps deeply nested object graphs. The build shows it as warning NU1903. It can't be triggered here, because no mapped type is recursive and System.Text.Json limits request bodies to a depth of 64. The fixes exist only in the commercially licensed 15.1.1+ and 16.1.1+.
- **The template's Users endpoints stay anonymous,** including `DELETE /api/users/{id}`, and they don't check that the caller owns the account.
- **Framework responses:** ASP.NET Core's own 404 (unknown route), 405 (wrong method) and 415 (missing or wrong `Content-Type`) keep the framework's defaults instead of `{type, error, detail}`.
- **A create response and a read format numbers differently.** A create response comes from memory (`"discountPercentage": 10`), while reads come from PostgreSQL's `numeric` columns (`10.00`). Likewise, `createdAt` has 100 ns precision after a create and microseconds after a read. The values are equal.
