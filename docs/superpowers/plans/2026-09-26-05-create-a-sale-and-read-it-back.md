# Create a Sale and Read It Back (Ticket 05) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** An authenticated client posts a sale to `POST /api/sales` and gets it back with the discounts and totals the domain computed, then reads it with `GET /api/sales/{id}`. The slice runs through every layer: the `Sale` aggregate, the CreateSale and GetSale use cases with their validators, EF Core persistence with a single `AddSales` migration for the whole Sales schema, `SalesController`, domain-event publishing after the save, and a log entry per event. A missing or invalid JWT gets the documented 401 body. Delivered on `feature/create-sale`, as a pull request into `develop` for the user to review.

**Architecture:** A rich `Sale` aggregate enforces rules R1–R5 and R13 and records `SaleCreatedEvent`; `ExternalIdentity` and `DiscountPolicy` stay dependency-free. MediatR runs the use cases. FluentValidation validators run only in ticket 03's `ValidationBehavior`. The create handler saves through `ISaleRepository` and only then publishes the events through `IPublisher`, with a shared helper, and one notification handler logs them. EF Core maps the external identities as owned types, the lines through their backing field and a shadow `xmin` row version. The controller uses ticket 03's envelope helpers, and a JWT bearer challenge writes the `{type, error, detail}` 401. Unit tests cover the domain and application without Docker; integration and functional tests reuse ticket 02's Testcontainers fixtures.

**Tech Stack:** .NET SDK 10.0.200-preview building the `net8.0` projects, with tests on the installed 8.0.23 runtime; ASP.NET Core 8 MVC with System.Text.Json; MediatR 12.4.1; FluentValidation 11.10.0 (with `FluentValidation.TestHelper`); AutoMapper 13.0.1; EF Core 8.0.10 with Npgsql.EntityFrameworkCore.PostgreSQL 8.0.8; `dotnet-ef` 8.0.10 (local tool from ticket 02); Microsoft.AspNetCore.Authentication.JwtBearer 8.0.10; Serilog.AspNetCore 8.0.3; xUnit 2.9.2, FluentAssertions 6.12.0, NSubstitute 5.1.0, Bogus 35.6.1; Microsoft.AspNetCore.Mvc.Testing 8.0.31 and Testcontainers.PostgreSql 4.15.0 (from ticket 02); Docker Desktop (WSL2) with Compose v2; curl 8.18 and Python 3.14 from Git Bash; Slopwatch.Cmd 0.4.2 (global tool).

**Source:** `docs/superpowers/tickets/05-create-a-sale-and-read-it-back.md`; Sales API design spec `docs/superpowers/specs/2026-09-24-sales-api-design.md`: D1, D2, D3, D8, D10–D13, §4, §5.1–§5.4 (Create), §6 (CreateSale, GetSale, event publishing and logging, mapping, repository), §7.1, §7.2, §7.4 (the 401 row), §7.5, §8.1 and §8.2.

**Rehearsed:** on 2026-09-26, in a scratch copy of the repo with tickets 01–04 applied as their plans leave it: tickets 03 and 04 in full, and ticket 02's tool manifest, design-time factory, connection string, compose file, `AddUserTimestamps` migration, `Program.cs` changes and test fixtures. The starting point matched ticket 04's end state: `2 Warning(s)`, 85 unit tests, a passing `StartupFailureTests`, and `Scan complete: 0 issue(s) found`. Then every task ran in this plan's order, with each RED, build, `dotnet ef` command, commit and `slopwatch` scan. The outputs below come from that run. Three changes went into the plan after the run, and it wasn't replayed end to end afterwards:
- the null-line check of Decision 7
- the empty-id test in `GetSaleTests`
- the `Exceptions` `using` that `SaleItem.cs` now gets in Task 4 instead of Task 5

Five expected outputs are adjusted by exactly those lines: the commit stats of Tasks 4, 5, 8 and 14, and Task 14's functional RED (11 failures, not the rehearsed 10). Docker still isn't installed on this machine, so three markers show how far each expected output was checked:
- **No marker:** rehearsed exactly as written.
- **(probe):** checked against a stand-in, not against PostgreSQL:
  - Functional tests ran on `WebApplicationFactory<Program>` in Production (no startup migrations), with `DefaultContext` switched to the EF Core InMemory provider. The requests go through the real controllers, maps, MediatR pipeline, validators, handlers, repositories, error middleware and JWT validation.
  - Persistence was checked in a throwaway console app. On the Npgsql provider with no connection it checked the model metadata, change tracking and the SQL of the by-id query. On EF Core InMemory it saved a sale through `SaleRepository` and loaded it back in a new context.
- **(derived):** not run at all, for example `docker compose up` and the whole-solution test counts. It's worked out from the code and the rehearsed equivalents.

If a (probe) or (derived) output differs, use superpowers:systematic-debugging before changing any code.

---

## Rules for every agent executing this plan

Restate these to every subagent you dispatch.

- **No AI attribution anywhere.** No `Co-Authored-By` trailer, no "Generated with" line, no session link in commits, merge commits, tags or pull requests. Use the commit messages below exactly as written. `.claude/settings.local.json` switches the defaults off, but don't rely on it.
- **Git Bash only.** Run every command in Git Bash (Claude Code's Bash tool), from the repo root `C:\Users\pr000\orca\developer-store-api` (`/c/Users/pr000/orca/developer-store-api`). Each Bash call starts a fresh shell, so don't rely on variables, functions or `cd` from an earlier call. Commands below use paths relative to the root.
- **Write file content with the Write and Edit tools**, not with shell heredocs or `sed`. The Edit tool keeps the UTF-8 BOM and CRLF line endings of the existing files (rehearsed on `DefaultContext.cs`, `InfrastructureModuleInitializer.cs`, `Program.cs`, both `DataResetFixture.cs` files and the `.http` file). New files come out with LF endings; git converts them on add. The Write tool refuses to overwrite a file you haven't read in this session, so Read `ApiHttpExtensions.cs` before Task 14 replaces it.
- **Work in the main checkout, never in a worktree.** `.claude/` is untracked, so it exists only here. It holds `settings.local.json` (the attribution guard) and the project skills. A worktree has neither.
- **Stage explicit paths only.** Never use `git add -A`, `git add .` or `git commit -a`. `docs/superpowers/tickets/` and other tickets' untracked plans must stay out of these commits.
- **Never run `git clean`.** `.git/info/exclude` makes `.claude/`, `.slopwatch/` and the two personal `.doc` files *ignored* files, and `git clean -x` or `-X` deletes ignored files.
- **Push only for the pull request.** Task 1 pushes `develop` with the plan commit, and Task 17 pushes `feature/create-sale` and opens the pull request. Never push `main`, never force-push, and never merge the pull request: the user reviews and merges it on GitHub.
- **Leave `.doc/brainstorm-show-case.md` and `.doc/template-code-review.md` alone.** They are the user's untracked personal notes. Don't open, edit, move or stage them.
- **Don't write or edit migration files by hand** (efcore-patterns). Generate `AddSales` with `dotnet ef migrations add`. If it comes out wrong, undo it with `dotnet ef migrations remove` (same `--project` and `--startup-project` flags) and generate it again.
- **Docker from Task 12 on.** Tasks 2–11 need no Docker. Don't install or configure Docker, WSL or any other system software. If Docker isn't running at the Phase 2 gate, stop there (see the gate).
- **Always build before `dotnet test --no-build`.** A failed build leaves the previous DLL in place, and `--no-build` then runs stale tests.
- **Stop everything you start.** Take compose stacks down (`docker compose down -v`) before a task ends. If you ever start the API yourself, stop it with `taskkill //F //IM Ambev.DeveloperEvaluation.WebApi.exe`, because a running API locks the build output and holds its port.
- **Nothing else changes.** Later tickets own these, so leave them out even when they look close:
  - sale-number generation and the duplicate-number 409 (ticket 06)
  - cancel (07), cancel item (08) and update (09)
  - listing (10, 11), including `PaginatedResponse.TotalCount`
  - the soft-delete query filter and the EF Core warning that goes with it (12)
  - the README (13)

  Also leave alone the pre-existing issues of spec §9.2 (`GetCurrentUserId`, the duplicate registrations and maps, the anonymous Users endpoints), the Users and Auth code, the Docker and compose files, `appsettings*.json`, both `launchSettings.json` files, the `ApiFixture` classes, and the migrations that already exist.

## Skills

### Project skills in `.claude/skills/`

Every skill folder in `.claude/skills/` is listed, with whether and when to use it.

| Skill | Use it? | When and how |
|---|---|---|
| `dotnet-best-practices` | **Yes, every task that writes C# (2–15)** | Load it before Task 2 and use it to review what you write. The code below follows it:<br>• XML docs on public types and members<br>• the `Ambev.DeveloperEvaluation.{Layer}.{Feature}` namespaces (`Application.Sales.CreateSale`, `Application.Sales.GetSale`, `WebApi.Features.Sales`)<br>• one folder per use case with its command or query, handler and validator<br>• constructor injection into `private readonly` fields, and regular constructors (`.editorconfig` turns off primary constructors)<br>• xUnit with FluentAssertions, NSubstitute and Bogus; `Given … When … Then …` display names with `// Given`, `// When`, `// Then` sections, and `Given_X_When_Y_Then_Z` method names<br>• structured logging through `ILogger<T>` (Task 11)<br><br>Its error rule shapes every handler: throw `DomainException`, `KeyNotFoundException` or `ValidationException`, and let ticket 03's middleware answer. Three bullets are deliberately not applied, as in tickets 03 and 04: `ArgumentNullException` guards, `ConfigureAwait(false)` and strongly-typed configuration classes (Decision 16). |
| `efcore-patterns` | **Yes, Tasks 12 and 13** | Load it before Task 12. Generate `AddSales` with `dotnet ef migrations add`, review it with `dotnet ef migrations script`, confirm `has-pending-model-changes`, never hand-edit it, and undo with `dotnet ef migrations remove`. Use `Add`, not `AddAsync`, since no value generator needs the async path. Its project note overrides "NoTracking by default". Note one conflict: the note names get-by-id as a read-only query, but the ticket and spec §6 make `GetByIdAsync` **tracked**, because the update and cancel handlers of tickets 07–09 load the aggregate through it and save it (Decision 9). Its query-splitting pattern is for ticket 10's list: one `Include` of one collection stays a single query. |
| `testcontainers-integration-tests` | **Yes, Tasks 12–15** | Load it before Task 12. The integration and functional tests reuse ticket 02's setup unchanged: `[Collection(DatabaseCollection.Name)]` and `[Collection(ApiCollection.Name)]`, one `postgres:13` container per project per run, the schema from the migrations, and a data reset after each test class. Task 13 adds the Sales tables and `sale_number_seq` to both reset lists, as ticket 02 left for this ticket. Don't add fixtures, containers or Respawn. Tests in one class share the database, so every sale number and email is unique (Decision 14). |
| `type-design-performance` | Light, Tasks 2–15 | New classes and records are `sealed`: `Sale`, `SaleItem`, the handlers, validators, profiles, DTOs, contracts, repository and test classes. Its project note keeps `ExternalIdentity` a reference type (a `sealed record`), since EF Core maps it as an owned type. `DiscountPolicy`, `SaleEventPublisher`, `SaleValidationRules`, `JwtBearerChallenge` and `ExternalIdentityMapping` are `static`. `Items` and `DomainEvents` return read-only views of private lists. The template's `BaseEntity` stays unsealed. |
| `dependency-injection-patterns` | Light, Tasks 12 and 15 | Its project note decides where `ISaleRepository` goes: `InfrastructureModuleInitializer`, scoped like `IUserRepository`. The validators need nothing new: ticket 03's `AddValidatorsFromAssembly` picks them up, and MediatR's assembly scan registers the handlers and the notification handler. The JWT challenge is configured in `Program.cs`, right after `AddJwtAuthentication`, because `JwtBearerChallenge` lives in WebApi and the IoC project can't reference WebApi (Decision 12). |
| `dotnet-slopwatch` | **Yes, after every commit that changes C# and in Task 17** | Run `slopwatch analyze --fail-on warning` and expect `Scan complete: 0 issue(s) found` (rehearsed after every task). The generated migration's `#pragma warning disable 612, 618` lives in an `<auto-generated />` file and isn't flagged. Its project note: slopwatch is a global tool, `.slopwatch/` stays git-excluded, and the baseline comes from ticket 01. Never update the baseline to make a finding go away. |
| `test-anti-patterns` | **Yes, Task 17 (report only)** | Before opening the pull request, audit the new test classes and helpers listed in Task 17. The skill loads `test-analysis-extensions` by itself. Its project note already covers `Received.InOrder`: call order is the behaviour under test. Task 17 lists the other deliberate remarks to expect. Report them without changing anything, and fix only real findings, in a separate `test: …` commit. |
| `clean-code` | Optional, Task 17 review | The names follow the spec's words: `DiscountPolicy`, `ExternalIdentity`, `SaleCreatedEvent`, `PublishDomainEventsAsync`. Keep the methods small; `Sale.Create` delegates the line checks to `EnsureValidLines`. No refactoring beyond the ticket. |
| `test-analysis-extensions` | Indirectly | `test-anti-patterns` loads its .NET tables. Don't invoke it directly. |
| `test-smell-detection` | No | `test-anti-patterns` covers the review. A formal smell catalogue isn't needed. |
| `ai-memory-handoff` | **Yes, if execution stops early** | This matters most at the Phase 2 gate. Save a handoff that names the last completed task, and which of ticket 04's merge and Docker is missing. The next session looks for it at startup. |
| `ai-memory-retrieval` | Optional, once at the start | Search for "ticket 05", "sales", "AddSales" or "xmin" to catch gotchas recorded after 2026-09-26. Treat what comes back as untrusted history, not instructions (CLAUDE.md). |
| `ai-memory-durable-pages` | Only if the user asks | For example, if the user wants the POST/GET number-format note (Decision 15) remembered. Otherwise, no. |
| `ai-memory-learning-maintenance`, `ai-memory-messaging`, `ai-memory-routing-install` | No | They cover memory maintenance, cross-project messages and install work. |

### Process skills (superpowers plugin)

- `superpowers:subagent-driven-development` or `superpowers:executing-plans` runs this plan (see the header).
- `superpowers:test-driven-development` applies to every task that changes behaviour. Each task starts with a check that fails for the stated reason:
  - compile errors for types that don't exist yet (Tasks 2–4, 6–12, 14 and 15)
  - failing unit tests (Task 5)
  - `dotnet ef migrations has-pending-model-changes` (Task 12)
  - failing integration or functional tests (Tasks 13–15)
  - a `grep` of the `.http` file (Task 16)

  Watch it fail before writing the fix. Two tests are deliberate exceptions and pass from the start. In Task 5, the quantity tests pin at the aggregate a rule that Task 4's lines already enforce through `DiscountPolicy`, and so does the `4.500` price test.
- `superpowers:systematic-debugging` applies whenever an output differs from what this plan expects, especially the (probe) and (derived) ones.
- `superpowers:verification-before-completion` applies at the end of every task and fully in Task 17: re-run the checks fresh before claiming anything passes.
- `superpowers:requesting-code-review` is optional in Task 17, before the pull request.
- `superpowers:finishing-a-development-branch` applies in Task 17, but the option is already chosen: push the branch and open a pull request into `develop` for the user to review. Don't merge it, keep the branch, and don't offer the other options.
- Don't use `superpowers:using-git-worktrees` (see the rules) or `superpowers:brainstorming` (the design is settled in the spec and the ticket).

## Decisions this plan makes

1. **Two phases, split by one gate.** The ticket lets the Domain and Application parts start once tickets 01 and 03 are merged, because they are unit tests without Docker. Phase 1 (Tasks 2–11) is exactly that. Phase 2 (Tasks 12–16: persistence, API, `.http`) needs tickets 02 and 04 merged and Docker running. The gate before Task 12 checks both. If ticket 04 isn't merged when Phase 1 ends, stop at the gate. Once its pull request is merged on GitHub, the gate fast-forwards local `develop` from `origin`; run `git rebase develop` on `feature/create-sale` and continue: the branch stays local and unpushed until Task 17 opens its pull request, and Phase 1 only adds new files, so the rebase is conflict-free. Every unit-test count below assumes ticket 04 is merged. Without it, each one is 12 lower (ticket 04 adds 12 unit tests; ticket 02 adds none).
2. **Where the Sales code lives:**
   - **Domain** keeps the template's folders by kind: `Entities/Sale.cs` and `SaleItem.cs`, `ValueObjects/ExternalIdentity.cs` and `SaleItemData.cs`, `Services/DiscountPolicy.cs`, `Events/IDomainEvent.cs` and `SaleCreatedEvent.cs`, `Repositories/ISaleRepository.cs`.
   - **Application** has one folder per use case (`Sales/CreateSale/`, `Sales/GetSale/`). What several use cases share sits directly in `Sales/`: `SaleResult`, `SaleItemResult`, `SaleProfile`, `SaleItemInput`, the shared validation rules, `SaleEventPublisher` and `SaleEventLogHandler`.
   - **WebApi** puts the controller and the shared contracts in `Features/Sales/`, and the create request in `Features/Sales/CreateSale/`.
3. **`ExternalIdentity` is a `sealed record` whose constructor validates:**
   - an empty id throws `DomainException("External identity id must not be empty")`
   - the name is trimmed and must have 1 to 100 characters, else `DomainException("External identity name must have 1 to 100 characters")`
   - `Id` and `Name` are get-only, and equality is the record's by-value equality

   EF Core binds that constructor when it loads the owned type (probe). The ticket says "owned types", so EF Core 8's complex types aren't used.
4. **The domain checks what the validators check** (spec §5.1, "defence in depth"), including the unit-price rules, which the ticket's Domain list doesn't name: above 0 and at most 2 decimal places. The decimal-places check compares values, `decimal.Round(price, 2) == price`, so `4.500` from JSON passes.
5. **Shared messages are constants on the type that enforces them:**
   - `DiscountPolicy.MaxQuantityExceededMessage`, rule R1's "It's not possible to sell above 20 identical items"
   - `Sale.NoItemsMessage`, "A sale must have at least one item"
   - `Sale.RepeatedProductMessage`, "Each product can appear only once in a sale"

   The validators reuse them, so a 400 and a domain rejection read the same. Every other validator message is FluentValidation's default, apart from two custom ones: `'{PropertyName}' must have at most N characters after trimming.` and `'Unit Price' must have at most 2 decimal places.` The other domain messages (quantity below 1, sale number, unit price) appear only if a validator missed something.
6. **Time and dates:**
   - The domain reads `DateTime.UtcNow`, as the template's `User` does; there is no clock abstraction. `CreatedAt` and the event's `OccurredAt` share one timestamp, and tests bracket the call between two `DateTime.UtcNow` reads, so they are deterministic.
   - `SaleDate` is normalized by kind: UTC stays, `Unspecified` becomes UTC with the same clock time, and `Local` goes through `ToUniversalTime()`. System.Text.Json parses JSON without an offset as `Unspecified` and JSON with an offset as `Local`, so this is rule R13. Npgsql also requires UTC for `timestamptz`.
7. **Validation runs only in the MediatR pipeline** (spec decision D10). The Sales handlers don't validate their commands (the template's Users handlers do), and the request contracts have no WebApi validators.
   - `SaleValidationRules` holds the rules create and update share, `MaximumTrimmedLength` and `ValidSaleItems`, so ticket 09 reuses them instead of duplicating them.
   - The rehearsal found one crash: `"items": [null]` threw `NullReferenceException` inside the repeated-product check, which the middleware turns into a 500. `ValidSaleItems` therefore adds `NotNull()` per line and skips missing lines in that check.
8. **Events:**
   - `SaleEventPublisher.PublishDomainEventsAsync(this IPublisher, Sale, CancellationToken)` publishes each event, typed as `IDomainEvent`, in the order recorded, then clears them. MediatR dispatches on the runtime type, which a unit test proves.
   - The handler calls the helper only after `CreateAsync` returns. If the save throws, nothing is published (rule R14). There is no outbox (spec §12).
   - One handler, `SaleEventLogHandler`, implements `INotificationHandler<SaleCreatedEvent>`. It logs `Sale event {EventName} published {@Event}` at Information, with the event's type name as `EventName`, through `ILogger<T>`, which Serilog writes. Later tickets add one interface per event to the same class. Serilog's `/health` filter doesn't drop the entry, because the entry has no `Path` property.
9. **The repository:**
   - `CreateAsync(Sale)` returns `Task`, since the handler keeps the instance it created.
   - `GetByIdAsync` loads the sale with `Include(Items)`, **tracked**, as the ticket says: tickets 07–09 load the aggregate with it, change it and save it.
   - Both are declared on `ISaleRepository` in Task 9, because the ticket defines them as one contract.
10. **EF Core mapping details, all checked by the probe or the generated migration:**
    - `Navigation(…).IsRequired()` on each owned identity makes its columns `NOT NULL`, and `ExternalIdentityMapping.Map` names them (`CustomerId`, `CustomerName`, …).
    - The concurrency token is a shadow `uint` property named `Version`, marked `IsRowVersion()`. Npgsql maps it to the `xmin` system column (`xid`), so the migration lists it but its SQL creates no column.
    - The sequence is declared in `DefaultContext.OnModelCreating` (`HasSequence` is model-level), under the name `SaleConfiguration.SaleNumberSequence`.
    - The `SaleId` foreign key is a shadow property with EF's default cascade delete. Soft delete (ticket 12) means it never fires.
11. **The controller:**
    - It uses `[Route("api/sales")]`, not `api/[controller]`, so `Location` reads `/api/sales/{id}` in lower case, as the ticket writes it (the Users controller gives `/api/Users/{id}`).
    - `GET` uses `{id}` without a `:guid` constraint, so a malformed id is a 400 from ticket 03's model-binding factory, not a 404.
    - The messages are "Sale created successfully" and "Sale retrieved successfully".
12. **The JWT challenge lives in `WebApi/Common/JwtBearerChallenge.cs`.** `Program.cs` wires it right after `AddJwtAuthentication`, with `Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, JwtBearerChallenge.Configure)`.
    - It calls `HandleResponse()`, writes the body with the default JSON options (nothing in it needs escaping), and sets `WWW-Authenticate: Bearer` itself. That header is required on a 401, and the handler's own one is skipped once the challenge is handled.
    - The header drops the `error="invalid_token"` detail the default handler adds. RFC 6750 makes that detail optional.
13. **WebApi maps:**
    - `SaleContractProfile` holds the maps several endpoints share: the request line to `SaleItemInput`, and `SaleResult` to `SaleResponse`. `CreateSaleProfile` holds only the create request map. Ticket 09 then adds an update profile with no duplicate maps.
    - Mapping tests build a configuration from the Sales profiles only, as ticket 04 found (Decision 6 there): `AssertConfigurationIsValid()` on the API's whole configuration throws for the template's duplicate `CreateUserRequest` map.
    - The flat fields (`CustomerId` from `Customer.Id`, …) come from AutoMapper's flattening convention, and `AssertConfigurationIsValid()` proves nothing is left unmapped.
14. **Functional tests:**
    - Test-side records pin the wire format: `SaleRequestBody` and `SaleResponseBody`, as ticket 04's `SignUpRequest` does. A rename in the API contract then breaks the tests instead of passing silently.
    - Sale numbers are `S-{Guid:N}` (34 characters), so tests that share the database never collide on the unique index; ticket 06 turns such a collision into a 409. Sale dates have whole seconds.
    - The envelope, the sale and each line are checked by their exact property names, which proves the envelope is built once and there is no `isDeleted` or `deletedAt`. Values are checked through the typed records, and error bodies whole.
    - Each test logs in as a new user with `LogInAsNewUserAsync`.
15. **POST and GET can differ in trailing zeros.** The create response is built from the in-memory aggregate, and the get response from PostgreSQL's `numeric(18,2)` and `numeric(5,2)` columns. So the same line shows `"discountPercentage":10` and `"discountAmount":0` after POST, but `10.00` and `0.00` after GET. `createdAt` has 100 ns ticks after POST and microseconds after GET. These are the same JSON numbers and the same instant, so the tests compare values, and the read-back test compares dates to within 1 µs. Report it (Task 17): ticket 13's README can mention it.
16. **Constructors follow tickets 03 and 04:** no `ArgumentNullException` guards and no `ConfigureAwait(false)`. Nullable annotations mark every parameter non-null, the validators run first, and ASP.NET Core has no synchronization context.
17. **The `.http` file** gets its requests after ticket 04's `@token` line and reuses ticket 04's variable pattern (its Decision 11):
    - `# @name createSale` names the create request, and `@saleId = {{createSale.response.body.$.data.id}}` captures the new id.
    - The sale number is `S-{{$randomInt 100000 999999}}`. Visual Studio and the VS Code REST Client both support that dynamic variable.
    - Task 16 replays the requests with curl against `docker compose up`. The user tries the file once in their editor (Task 17 report).
18. **This plan is committed on `develop` before branching** (spec §11), as tickets 01–04 did, and `develop` is pushed. The work reaches `develop` through a pull request the user reviews and merges on GitHub with **Create a merge commit**; the agent never merges it. The feature branch is kept.

## Order with other tickets

- **Blocked by 04, and through it by 02 and 03.** Phase 1 needs only 01 and 03 (Decision 1). Ticket 03 provides:
  - `DomainException`'s namespace
  - `ValidationBehavior` running the registered validators
  - the error middleware (404 for `KeyNotFoundException`, 400 for `ValidationException`)
  - the envelope helpers (`Created(actionName, …)`, `Ok(data, message)`)
  - the Unit project's reference to WebApi, and `MvcJson`

  Ticket 02 provides `dotnet-ef`, the design-time factory, the `AddUserTimestamps` migration (so `AddSales` holds only the Sales schema), the Testcontainers fixtures and both data resets. Ticket 04 provides sign-up and login for the functional tests, `ApiHttpExtensions`, the `.http` token variable and the Swagger Bearer scheme.
- **Tickets 06–12** only add behaviour on the schema this ticket creates:
  - 06 makes `saleNumber` optional (`string?`), generates numbers from `sale_number_seq`, and turns the unique index into a 409
  - 07–09 add domain methods and handlers that reuse `ISaleRepository.GetByIdAsync`, `SaleEventPublisher` and `SaleEventLogHandler`; 09 reuses `SaleValidationRules` and `SaleContractProfile`
  - 12 adds the query filter

  None of them adds a migration.

## Pre-existing output you'll see (leave it alone)

- `warning NU1903` (AutoMapper 13.0.1 advisory; spec D14 keeps that version). A clean solution build (`--no-incremental`) shows `2 Warning(s)`, and so does an incremental one.
- `message NETSDK1057: You are using a preview version of .NET`. The machine has only the .NET 10 preview SDK. It builds `net8.0` fine, and the tests run on the installed 8.0.23 runtime.
- `MSB1011` from a bare `dotnet build` or `dotnet test` at the root, because `docker-compose.dcproj` sits next to the `.sln`. Always pass `Ambev.DeveloperEvaluation.sln` or a project path.
- `warning: in the working copy of '…', LF will be replaced by CRLF the next time Git touches it` when adding new files. It's harmless: `core.autocrlf=true`.
- **Every `dotnet ef` command prints a `[FTL] Application terminated unexpectedly` line, then `Microsoft.Extensions.Hosting.HostAbortedException: The host was aborted.` and a stack trace ending in `Program.Main`.** That's the EF tools stopping `Program.Main` at `builder.Build()`: ticket 02's rethrow passes the exception on, and since ticket 03 fixed the Serilog filter, the `Log.Fatal` in the `catch` block is written. The command still succeeds; look at its last lines. The commands below filter those lines out with `grep -vE '\[FTL\]|HostAbortedException|^\s+at '`. Ticket 02's Decision 5 expected nothing extra to be logged, but that was before ticket 03's filter fix (report it, Task 17).
- `dotnet ef` also prints `Build started...` and `Build succeeded.`, and repeats the NU1903 warning from its own build.
- Running the API or the tests creates git-ignored `logs/` folders.
- **(derived)** The API logs `[WRN] … Failed to determine the https port for redirect.` It's HTTP only, and `UseHttpsRedirection()` stays.

## File map

| Change | Paths |
|---|---|
| Created (Domain) | under `src/Ambev.DeveloperEvaluation.Domain/`: `Entities/{Sale, SaleItem}.cs`, `ValueObjects/{ExternalIdentity, SaleItemData}.cs`, `Services/DiscountPolicy.cs`, `Events/{IDomainEvent, SaleCreatedEvent}.cs`, `Repositories/ISaleRepository.cs` |
| Created (Application) | under `src/Ambev.DeveloperEvaluation.Application/Sales/`: `{SaleResult, SaleItemResult, SaleProfile, SaleItemInput, SaleItemInputValidator, SaleValidationRules, SaleEventPublisher, SaleEventLogHandler}.cs`, `CreateSale/{CreateSaleCommand, CreateSaleCommandValidator, CreateSaleHandler}.cs`, `GetSale/{GetSaleQuery, GetSaleQueryValidator, GetSaleHandler}.cs` |
| Created (ORM) | under `src/Ambev.DeveloperEvaluation.ORM/`: `Mapping/{ExternalIdentityMapping, SaleConfiguration, SaleItemConfiguration}.cs`, `Repositories/SaleRepository.cs`, `Migrations/<timestamp>_AddSales.cs` and `.Designer.cs` (generated) |
| Created (WebApi) | under `src/Ambev.DeveloperEvaluation.WebApi/`: `Features/Sales/{SalesController, SaleItemRequest, SaleResponse, SaleItemResponse, SaleContractProfile}.cs`, `Features/Sales/CreateSale/{CreateSaleRequest, CreateSaleProfile}.cs`, `Common/JwtBearerChallenge.cs` |
| Modified (src) | `src/Ambev.DeveloperEvaluation.ORM/DefaultContext.cs`; `src/Ambev.DeveloperEvaluation.ORM/Migrations/DefaultContextModelSnapshot.cs` (generated); `src/Ambev.DeveloperEvaluation.IoC/ModuleInitializers/InfrastructureModuleInitializer.cs`; `src/Ambev.DeveloperEvaluation.WebApi/Program.cs`; `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http` |
| Created (unit tests) | under `tests/Ambev.DeveloperEvaluation.Unit/`: `Domain/ValueObjects/ExternalIdentityTests.cs`, `Domain/Services/DiscountPolicyTests.cs`, `Domain/Entities/SaleTests.cs`, `Domain/Entities/TestData/SaleTestData.cs`, `Application/Sales/{SaleProfileTests, SaleEventLogHandlerTests}.cs`, `Application/Sales/TestData/CreateSaleCommandTestData.cs`, `Application/Sales/CreateSale/{CreateSaleCommandValidatorTests, CreateSaleHandlerTests}.cs`, `Application/Sales/GetSale/{GetSaleQueryValidatorTests, GetSaleHandlerTests}.cs`, `WebApi/Features/Sales/{SalesMappingTests, SalesControllerTests}.cs`, `WebApi/Common/JwtBearerChallengeTests.cs` |
| Created (integration tests) | under `tests/Ambev.DeveloperEvaluation.Integration/`: `ORM/SaleRepositoryTests.cs`, `TestData/SaleTestData.cs` |
| Created (functional tests) | under `tests/Ambev.DeveloperEvaluation.Functional/`: `TestData/{SaleRequestBody, SaleResponseBody, SaleRequestBodyTestData}.cs`, `Sales/{SaleJson, CreateSaleTests, GetSaleTests, SalesAuthenticationTests}.cs` |
| Modified (tests) | both `Fixtures/DataResetFixture.cs` and `Fixtures/DataResetTests.cs` (Integration and Functional); `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiHttpExtensions.cs` (rewritten) |
| Committed on `develop` first | `docs/superpowers/plans/2026-09-26-05-create-a-sale-and-read-it-back.md` (this plan) |
| Local only, never committed | `/tmp/ticket05-checks/` (run logs and curl output; removed at the end); git-ignored `logs/` folders |
| Not touched | see "Nothing else changes" in the rules |

Totals, rehearsed: 69 files, `4446 insertions(+), 22 deletions(-)`.

---

### Task 1: Check the starting point, commit this plan, branch and record the baseline

**Files:**
- Commit on `develop`: `docs/superpowers/plans/2026-09-26-05-create-a-sale-and-read-it-back.md`
- Create (outside the repo): `/tmp/ticket05-checks/`

- [ ] **Step 1: Confirm which tickets are merged and that the tree is clean**

```bash
git switch develop
git pull --ff-only origin develop
git log --oneline -3
git diff --quiet && git diff --cached --quiet && echo "no tracked changes"
git status --short
git log --oneline --merges | grep -oE "feature/(repo-restructure|postgres-runtime|error-format|auth-login)" | sort -u
test -f src/Ambev.DeveloperEvaluation.WebApi/Middleware/ExceptionHandlingMiddleware.cs && test -f tests/Ambev.DeveloperEvaluation.Unit/WebApi/MvcJson.cs && echo "ticket 03 in place"
ls src/Ambev.DeveloperEvaluation.ORM/Migrations/ | grep -c _AddUserTimestamps.cs
test -f .config/dotnet-tools.json && test -f tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiHttpExtensions.cs && echo "tickets 02 and 04 in place"
git branch --list feature/create-sale
```

Expected:
- The pull fast-forwards `develop` to the merged pull requests on GitHub, or prints `Already up to date.`
- `no tracked changes`.
- The four branch names `feature/auth-login`, `feature/error-format`, `feature/postgres-runtime` and `feature/repo-restructure`.
- `ticket 03 in place`, `1`, and `tickets 02 and 04 in place`.
- `git status --short` shows `?? docs/superpowers/plans/2026-09-26-05-create-a-sale-and-read-it-back.md` and `?? docs/superpowers/tickets/`. Other tickets' untracked plan files are fine; leave them alone.
- The branch check prints nothing.

Stop and ask the user if the pull fails (local `develop` must only ever fast-forward), if anything tracked is modified, or if `feature/create-sale` already exists: an existing branch means an earlier run got partway, so look for an ai-memory handoff first. If `feature/repo-restructure` or `feature/error-format` is missing, stop too: even Phase 1 needs tickets 01 and 03. If only `feature/postgres-runtime` or `feature/auth-login` is missing, you can do Phase 1 (Tasks 1–11) and stop at the gate (Decision 1).

- [ ] **Step 2: Check the tools, including Docker**

```bash
dotnet --list-sdks
dotnet tool restore 2>&1 | tail -1
dotnet ef --version 2>&1 | tail -1
command -v slopwatch
python3 --version
curl --version | head -1
docker version --format 'Docker server {{.Server.Version}}' 2>&1 | tail -1
docker compose version 2>&1 | tail -1
```

Expected: SDK `10.0.200-preview…`, `Restore was successful.`, `8.0.10`, a slopwatch path, `Python 3.…` and `curl 8.18.0 …`. For Docker:
- **Docker ready:** a `Docker server …` version and a `Docker Compose version v2…` line. Run every task.
- **Docker stopped or missing:** an error that mentions the Docker daemon or `dockerDesktopLinuxEngine`, or `docker: command not found`. Do Tasks 1–11, then stop at the gate.

- [ ] **Step 3: Create the scratch folder for checks and logs**

```bash
mkdir -p /tmp/ticket05-checks && ls -d /tmp/ticket05-checks
```

In Git Bash, `/tmp` is `C:\Users\pr000\AppData\Local\Temp`. It's a fixed path outside the repo, so every task and subagent finds the same files and none of them can be committed.

- [ ] **Step 4: Commit this plan on `develop` and push `develop`**

If `git status --short docs/superpowers/plans/2026-09-26-05-create-a-sale-and-read-it-back.md` prints nothing, the plan is already committed: skip `git add` and `git commit`, and still run the push.

```bash
git add docs/superpowers/plans/2026-09-26-05-create-a-sale-and-read-it-back.md
git commit -m "docs: add plan for creating a sale and reading it back"
git status --short
git push origin develop
```

Expected: a commit with one file changed. `git status --short` then lists `?? docs/superpowers/tickets/`, plus any other untracked plan files. The push sends the plan commit to `origin/develop` (or prints `Everything up-to-date`), so the Task 17 pull request holds only the feature commits, and the Phase 2 gate can fast-forward `develop` later. If the push is rejected, stop and ask the user; never force it.

- [ ] **Step 5: Create the feature branch**

```bash
git switch -c feature/create-sale
git log --oneline -1
```

Expected: `Switched to a new branch 'feature/create-sale'`, with HEAD at the plan commit.

- [ ] **Step 6: Record the baseline**

```bash
dotnet build Ambev.DeveloperEvaluation.sln --no-incremental 2>&1 | grep -E 'Warning\(s\)|Error\(s\)|Build succeeded'
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~StartupFailureTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `Build succeeded.`, `2 Warning(s)`, `0 Error(s)`; `Passed:    85, Skipped:     0, Total:    85`; `Passed:     1, Skipped:     0, Total:     1`; `Scan complete: 0 issue(s) found`. Without ticket 04 the counts are `3 Warning(s)` (its CS8604 fix is missing) and `Passed:    73`.

If Docker is ready, also run the whole solution:

```bash
dotnet test Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-110
```

Expected **(derived)**: three `Passed!` lines. Unit shows `Passed:    85`, Integration `Passed:     4` and Functional `Passed:    12`. If the baseline isn't green, stop: nothing can be judged against a broken start.

---

## Phase 1: Domain and Application (no Docker)

### Task 2: Add the `ExternalIdentity` value object

**Skills:** `dotnet-best-practices` (load it now), `type-design-performance`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Unit/Domain/ValueObjects/ExternalIdentityTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.Domain/ValueObjects/ExternalIdentity.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Ambev.DeveloperEvaluation.Unit/Domain/ValueObjects/ExternalIdentityTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.ValueObjects;

/// <summary>
/// Contains unit tests for the <see cref="ExternalIdentity"/> value object.
/// </summary>
public sealed class ExternalIdentityTests
{
    /// <summary>
    /// Tests that the identity keeps its id and stores the name without surrounding spaces.
    /// </summary>
    [Fact(DisplayName = "Given an id and a name with surrounding spaces When creating an external identity Then it keeps the id and trims the name")]
    public void Given_IdAndNameWithSpaces_When_Creating_Then_KeepsIdAndTrimsName()
    {
        // Given
        var id = Guid.NewGuid();

        // When
        var identity = new ExternalIdentity(id, "  Maria Silva  ");

        // Then
        identity.Id.Should().Be(id);
        identity.Name.Should().Be("Maria Silva");
    }

    /// <summary>
    /// Tests that an empty id is rejected.
    /// </summary>
    [Fact(DisplayName = "Given an empty id When creating an external identity Then it throws DomainException")]
    public void Given_EmptyId_When_Creating_Then_ThrowsDomainException()
    {
        // When
        var act = () => new ExternalIdentity(Guid.Empty, "Maria Silva");

        // Then
        act.Should().Throw<DomainException>().WithMessage("External identity id must not be empty");
    }

    /// <summary>
    /// Tests that a missing or blank name is rejected.
    /// </summary>
    /// <param name="name">The name to try.</param>
    [Theory(DisplayName = "Given a missing or blank name When creating an external identity Then it throws DomainException")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Given_MissingOrBlankName_When_Creating_Then_ThrowsDomainException(string? name)
    {
        // When
        var act = () => new ExternalIdentity(Guid.NewGuid(), name!);

        // Then
        act.Should().Throw<DomainException>().WithMessage("External identity name must have 1 to 100 characters");
    }

    /// <summary>
    /// Tests that a name longer than 100 characters after trimming is rejected.
    /// </summary>
    [Fact(DisplayName = "Given a name of 101 characters When creating an external identity Then it throws DomainException")]
    public void Given_NameOf101Characters_When_Creating_Then_ThrowsDomainException()
    {
        // When
        var act = () => new ExternalIdentity(Guid.NewGuid(), new string('a', 101));

        // Then
        act.Should().Throw<DomainException>().WithMessage("External identity name must have 1 to 100 characters");
    }

    /// <summary>
    /// Tests that the length limit applies after trimming, so 100 characters plus spaces are accepted.
    /// </summary>
    [Fact(DisplayName = "Given a name of 100 characters with surrounding spaces When creating an external identity Then it is accepted")]
    public void Given_NameOf100CharactersWithSpaces_When_Creating_Then_IsAccepted()
    {
        // Given
        var name = new string('a', 100);

        // When
        var identity = new ExternalIdentity(Guid.NewGuid(), $"  {name}  ");

        // Then
        identity.Name.Should().Be(name);
    }

    /// <summary>
    /// Tests that two identities with the same id and name are equal, as value objects are.
    /// </summary>
    [Fact(DisplayName = "Given two external identities with the same id and name When comparing them Then they are equal")]
    public void Given_SameIdAndName_When_Comparing_Then_AreEqual()
    {
        // Given
        var id = Guid.NewGuid();

        // When
        var first = new ExternalIdentity(id, "Filial Centro");
        var second = new ExternalIdentity(id, " Filial Centro ");

        // Then
        first.Should().Be(second);
        (first == second).Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    /// <summary>
    /// Tests that identities that differ in the name are not equal.
    /// </summary>
    [Fact(DisplayName = "Given two external identities with the same id and different names When comparing them Then they are not equal")]
    public void Given_SameIdDifferentNames_When_Comparing_Then_AreNotEqual()
    {
        // Given
        var id = Guid.NewGuid();

        // When
        var first = new ExternalIdentity(id, "Filial Centro");
        var second = new ExternalIdentity(id, "Filial Norte");

        // Then
        first.Should().NotBe(second);
    }
}
```

The equality test sends `" Filial Centro "` on purpose: equal after trimming proves the comparison runs on the stored values.

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected:

```
ExternalIdentityTests.cs(2,40): error CS0234: The type or namespace name 'ValueObjects' does not exist in the namespace 'Ambev.DeveloperEvaluation.Domain' (are you missing an assembly reference?)
```

- [ ] **Step 3: Write the value object**

Create `src/Ambev.DeveloperEvaluation.Domain/ValueObjects/ExternalIdentity.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Exceptions;

namespace Ambev.DeveloperEvaluation.Domain.ValueObjects;

/// <summary>
/// An entity that belongs to another domain (a customer, a branch or a product), referenced by its id
/// together with a copy of its name. The name is a snapshot taken when the sale is written.
/// </summary>
/// <remarks>
/// A value object: two identities are equal when their id and name are equal.
/// EF Core maps it as an owned type, into columns of the table that owns it.
/// </remarks>
public sealed record ExternalIdentity
{
    /// <summary>
    /// The maximum length of <see cref="Name"/>, after trimming.
    /// </summary>
    public const int NameMaxLength = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalIdentity"/> record.
    /// </summary>
    /// <param name="id">The id of the entity in its own domain. Must not be empty.</param>
    /// <param name="name">The name of the entity. Trimmed, it must have 1 to 100 characters.</param>
    /// <exception cref="DomainException">Thrown when the id is empty or the name is missing, blank or too long.</exception>
    public ExternalIdentity(Guid id, string name)
    {
        if (id == Guid.Empty)
            throw new DomainException("External identity id must not be empty");

        var trimmedName = name?.Trim() ?? string.Empty;
        if (trimmedName.Length is 0 or > NameMaxLength)
            throw new DomainException($"External identity name must have 1 to {NameMaxLength} characters");

        Id = id;
        Name = trimmedName;
    }

    /// <summary>
    /// Gets the id of the entity in its own domain.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the name of the entity, trimmed.
    /// </summary>
    public string Name { get; }
}
```

The Domain project file already lists an empty `ValueObjects\` folder, so it needs no change.

- [ ] **Step 4: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~ExternalIdentityTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)` and `Build succeeded.`, with no `warning CS` line; `Passed:     9, Skipped:     0, Total:     9`; `Passed:    94, Skipped:     0, Total:    94`.

- [ ] **Step 5: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Domain/ValueObjects/ExternalIdentity.cs tests/Ambev.DeveloperEvaluation.Unit/Domain/ValueObjects/ExternalIdentityTests.cs
git commit -m "feat(sales): add the ExternalIdentity value object" -m "Customer, branch and product belong to other domains, so a sale refers to each by its id plus a copy of its name (spec decision D2). The id must not be empty, the name is trimmed and must have 1 to 100 characters, and two identities are equal when their id and name are."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `2 files changed, 172 insertions(+)` and `Scan complete: 0 issue(s) found`.

---

### Task 3: Add the quantity discount policy

**Skills:** `dotnet-best-practices`, `type-design-performance` (a static pure function).

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Unit/Domain/Services/DiscountPolicyTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.Domain/Services/DiscountPolicy.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Ambev.DeveloperEvaluation.Unit/Domain/Services/DiscountPolicyTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Services;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Services;

/// <summary>
/// Contains unit tests for <see cref="DiscountPolicy"/>: the quantity-based discount tiers of rule R1.
/// </summary>
public sealed class DiscountPolicyTests
{
    /// <summary>
    /// Tests each tier at both of its boundaries. Quantity 4 gets 10% (spec decision D3).
    /// </summary>
    /// <param name="quantity">The quantity of identical items in the line.</param>
    /// <param name="expectedPercentage">The discount percentage the tier gives.</param>
    [Theory(DisplayName = "Given a quantity from 1 to 20 When getting the discount percentage Then it follows the tiers 0, 10 and 20")]
    [InlineData(1, 0)]
    [InlineData(3, 0)]
    [InlineData(4, 10)]
    [InlineData(9, 10)]
    [InlineData(10, 20)]
    [InlineData(20, 20)]
    public void Given_QuantityFrom1To20_When_GettingDiscountPercentage_Then_FollowsTiers(int quantity, int expectedPercentage)
    {
        // When
        var percentage = DiscountPolicy.GetDiscountPercentage(quantity);

        // Then
        percentage.Should().Be(expectedPercentage);
    }

    /// <summary>
    /// Tests that more than 20 identical items can't be sold, with the message rule R1 gives.
    /// </summary>
    /// <param name="quantity">A quantity above 20.</param>
    [Theory(DisplayName = "Given a quantity above 20 When getting the discount percentage Then it throws DomainException with R1's message")]
    [InlineData(21)]
    [InlineData(100)]
    public void Given_QuantityAbove20_When_GettingDiscountPercentage_Then_ThrowsWithR1Message(int quantity)
    {
        // When
        var act = () => DiscountPolicy.GetDiscountPercentage(quantity);

        // Then
        act.Should().Throw<DomainException>().WithMessage("It's not possible to sell above 20 identical items");
    }

    /// <summary>
    /// Tests that a line needs at least one item.
    /// </summary>
    /// <param name="quantity">A quantity below 1.</param>
    [Theory(DisplayName = "Given a quantity below 1 When getting the discount percentage Then it throws DomainException")]
    [InlineData(0)]
    [InlineData(-1)]
    public void Given_QuantityBelow1_When_GettingDiscountPercentage_Then_ThrowsDomainException(int quantity)
    {
        // When
        var act = () => DiscountPolicy.GetDiscountPercentage(quantity);

        // Then
        act.Should().Throw<DomainException>().WithMessage("Quantity must be at least 1");
    }
}
```

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected:

```
DiscountPolicyTests.cs(2,40): error CS0234: The type or namespace name 'Services' does not exist in the namespace 'Ambev.DeveloperEvaluation.Domain' (are you missing an assembly reference?)
```

`Domain/Services/` holds only the template's `IUserService.cs`, which is a placeholder comment, so the namespace doesn't exist yet.

- [ ] **Step 3: Write the policy**

Create `src/Ambev.DeveloperEvaluation.Domain/Services/DiscountPolicy.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Exceptions;

namespace Ambev.DeveloperEvaluation.Domain.Services;

/// <summary>
/// The quantity-based discount tiers for a sale line (rule R1).
/// </summary>
/// <remarks>
/// Identical items are the items of one product. 1 to 3 get no discount, 4 to 9 get 10%, 10 to 20 get 20%,
/// and more than 20 can't be sold. Quantity 4 gets 10% (spec decision D3).
/// </remarks>
public static class DiscountPolicy
{
    /// <summary>
    /// The smallest quantity a line can have.
    /// </summary>
    public const int MinQuantity = 1;

    /// <summary>
    /// The largest quantity a line can have.
    /// </summary>
    public const int MaxQuantity = 20;

    /// <summary>
    /// The message for a quantity above <see cref="MaxQuantity"/>, as rule R1 words it.
    /// </summary>
    public const string MaxQuantityExceededMessage = "It's not possible to sell above 20 identical items";

    /// <summary>
    /// Returns the discount percentage for a line with the given quantity of identical items.
    /// </summary>
    /// <param name="quantity">The quantity of identical items, from 1 to 20.</param>
    /// <returns>0, 10 or 20.</returns>
    /// <exception cref="DomainException">Thrown when the quantity is below 1 or above 20.</exception>
    public static decimal GetDiscountPercentage(int quantity) => quantity switch
    {
        < MinQuantity => throw new DomainException($"Quantity must be at least {MinQuantity}"),
        > MaxQuantity => throw new DomainException(MaxQuantityExceededMessage),
        >= 10 => 20m,
        >= 4 => 10m,
        _ => 0m
    };
}
```

- [ ] **Step 4: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~DiscountPolicyTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)` and `Build succeeded.`; `Passed:    10`; `Passed:   104`.

- [ ] **Step 5: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Domain/Services/DiscountPolicy.cs tests/Ambev.DeveloperEvaluation.Unit/Domain/Services/DiscountPolicyTests.cs
git commit -m "feat(sales): add the quantity discount policy" -m "DiscountPolicy.GetDiscountPercentage implements rule R1: 1 to 3 identical items get no discount, 4 to 9 get 10% and 10 to 20 get 20%. Quantity 4 gets 10% (spec decision D3). A quantity below 1 or above 20 throws DomainException, with R1's own message above 20."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `2 files changed, 108 insertions(+)` and `Scan complete: 0 issue(s) found`.

---

### Task 4: Create a sale with discounted lines and totals

**Skills:** `dotnet-best-practices`, `type-design-performance` (sealed aggregate, read-only collection over a private list).

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/TestData/SaleTestData.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.Domain/ValueObjects/SaleItemData.cs`
- Create: `src/Ambev.DeveloperEvaluation.Domain/Entities/SaleItem.cs`
- Create: `src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs`

- [ ] **Step 1: Add the test data builder**

Create `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/TestData/SaleTestData.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Bogus;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;

/// <summary>
/// Generates valid inputs for <see cref="Sale.Create"/> with Bogus, and sales built from them.
/// Every generated value passes the domain rules, so a test changes only the value it is about.
/// </summary>
public static class SaleTestData
{
    private static readonly Faker Faker = new();

    /// <summary>
    /// Generates a sale number of the form <c>S-123456</c>.
    /// </summary>
    /// <returns>A valid sale number.</returns>
    public static string GenerateSaleNumber() => $"S-{Faker.Random.Number(100000, 999999)}";

    /// <summary>
    /// Generates a customer with a random id and a person's name.
    /// </summary>
    /// <returns>A valid customer identity.</returns>
    public static ExternalIdentity GenerateCustomer() => new(Guid.NewGuid(), Faker.Name.FullName());

    /// <summary>
    /// Generates a branch with a random id and a city name.
    /// </summary>
    /// <returns>A valid branch identity.</returns>
    public static ExternalIdentity GenerateBranch() => new(Guid.NewGuid(), $"Filial {Faker.Address.City()}");

    /// <summary>
    /// Generates a line for a new product with the given quantity and unit price.
    /// </summary>
    /// <param name="quantity">The quantity of identical items.</param>
    /// <param name="unitPrice">The price of one item.</param>
    /// <returns>The line's input data.</returns>
    public static SaleItemData GenerateItem(int quantity, decimal unitPrice) =>
        new(new ExternalIdentity(Guid.NewGuid(), Faker.Commerce.ProductName()), quantity, unitPrice);

    /// <summary>
    /// Generates a line for a new product with a random quantity from 1 to 20 and a random price with 2 decimal places.
    /// </summary>
    /// <returns>The line's input data.</returns>
    public static SaleItemData GenerateItem() =>
        GenerateItem(Faker.Random.Int(1, 20), Math.Round(Faker.Random.Decimal(0.01m, 500m), 2));

    /// <summary>
    /// Creates a sale with a random number, date, customer and branch.
    /// </summary>
    /// <param name="items">The lines of the sale. With none, the sale gets one random line.</param>
    /// <returns>A new sale.</returns>
    public static Sale CreateSale(params SaleItemData[] items) =>
        Sale.Create(
            GenerateSaleNumber(),
            Faker.Date.Recent().ToUniversalTime(),
            GenerateCustomer(),
            GenerateBranch(),
            items.Length > 0 ? items : [GenerateItem()]);
}
```

- [ ] **Step 2: Write the failing tests**

Create `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

/// <summary>
/// Contains unit tests for the <see cref="Sale"/> aggregate.
/// </summary>
public sealed class SaleTests
{
    /// <summary>
    /// One line per row: quantity, unit price, then the expected discount percentage, discount amount and line total.
    /// </summary>
    public static TheoryData<int, decimal, decimal, decimal, decimal> LinesAtEachTier => new()
    {
        { 3, 10.00m, 0m, 0.00m, 30.00m },
        { 4, 4.50m, 10m, 1.80m, 16.20m },
        { 5, 4.45m, 10m, 2.23m, 20.02m },
        { 10, 4.50m, 20m, 9.00m, 36.00m },
        { 20, 0.33m, 20m, 1.32m, 5.28m }
    };

    /// <summary>
    /// Tests rules R1 and R3 on one line. 5 × 4.45 = 22.25, whose 10% is 2.225: rounding away from zero gives 2.23,
    /// where the default banker's rounding would give 2.22.
    /// </summary>
    [Theory(DisplayName = "Given a line at a discount tier When creating a sale Then the line gets the tier's discount, rounded away from zero, and its total")]
    [MemberData(nameof(LinesAtEachTier))]
    public void Given_LineAtDiscountTier_When_CreatingSale_Then_LineGetsDiscountAndTotal(
        int quantity, decimal unitPrice, decimal discountPercentage, decimal discountAmount, decimal totalAmount)
    {
        // Given
        var item = SaleTestData.GenerateItem(quantity, unitPrice);

        // When
        var sale = SaleTestData.CreateSale(item);

        // Then
        sale.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            item.Product,
            Quantity = quantity,
            UnitPrice = unitPrice,
            DiscountPercentage = discountPercentage,
            DiscountAmount = discountAmount,
            TotalAmount = totalAmount,
            IsCancelled = false
        });
    }

    /// <summary>
    /// Tests that the sale total is the sum of its line totals (rule R3).
    /// </summary>
    [Fact(DisplayName = "Given two lines When creating a sale Then the sale total is the sum of the line totals")]
    public void Given_TwoLines_When_CreatingSale_Then_TotalIsSumOfLineTotals()
    {
        // Given
        var discounted = SaleTestData.GenerateItem(4, 4.50m);
        var fullPrice = SaleTestData.GenerateItem(3, 10.00m);

        // When
        var sale = SaleTestData.CreateSale(discounted, fullPrice);

        // Then (16.20 + 30.00)
        sale.TotalAmount.Should().Be(46.20m);
    }

    /// <summary>
    /// Tests that the domain creates the sale id and the item ids.
    /// </summary>
    [Fact(DisplayName = "Given two lines When creating a sale Then the sale and each item get their own new id")]
    public void Given_TwoLines_When_CreatingSale_Then_SaleAndItemsGetNewIds()
    {
        // When
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(), SaleTestData.GenerateItem());

        // Then
        var ids = sale.Items.Select(item => item.Id).Append(sale.Id).ToList();
        ids.Should().NotContain(Guid.Empty).And.OnlyHaveUniqueItems();
    }

    /// <summary>
    /// Tests that the lines can't be changed from outside the aggregate, even through a cast.
    /// </summary>
    [Fact(DisplayName = "Given a sale When clearing its items from outside Then it throws NotSupportedException and the items stay")]
    public void Given_Sale_When_ClearingItemsFromOutside_Then_ThrowsAndItemsStay()
    {
        // Given
        var sale = SaleTestData.CreateSale();

        // When
        var act = () => ((ICollection<SaleItem>)sale.Items).Clear();

        // Then
        act.Should().Throw<NotSupportedException>();
        sale.Items.Should().ContainSingle();
    }

    /// <summary>
    /// Tests that a new sale is open: not cancelled, not deleted and never updated, with its creation time in UTC.
    /// </summary>
    [Fact(DisplayName = "Given valid input When creating a sale Then it is open, not deleted, never updated, and created now in UTC")]
    public void Given_ValidInput_When_CreatingSale_Then_IsOpenAndCreatedNowInUtc()
    {
        // Given
        var before = DateTime.UtcNow;

        // When
        var sale = SaleTestData.CreateSale();

        // Then
        var after = DateTime.UtcNow;
        sale.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        sale.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
        sale.Should().BeEquivalentTo(new { IsCancelled = false, IsDeleted = false, DeletedAt = (DateTime?)null, UpdatedAt = (DateTime?)null });
    }

    /// <summary>
    /// Tests rule R13: a sale date in UTC, or without a kind (no offset in the JSON), is stored as that UTC time.
    /// </summary>
    /// <param name="kind">The kind of the sale date that is sent.</param>
    [Theory(DisplayName = "Given a sale date in UTC or without a kind When creating a sale Then it is stored as that time in UTC")]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Given_SaleDateInUtcOrWithoutKind_When_CreatingSale_Then_StoredAsThatTimeInUtc(DateTimeKind kind)
    {
        // Given
        var saleDate = new DateTime(2026, 9, 24, 14, 30, 0, kind);

        // When
        var sale = Sale.Create(SaleTestData.GenerateSaleNumber(), saleDate, SaleTestData.GenerateCustomer(),
            SaleTestData.GenerateBranch(), [SaleTestData.GenerateItem()]);

        // Then
        sale.SaleDate.Should().Be(new DateTime(2026, 9, 24, 14, 30, 0, DateTimeKind.Utc));
        sale.SaleDate.Kind.Should().Be(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests rule R13: a local sale date (JSON with an offset arrives as local time) is converted to UTC.
    /// </summary>
    [Fact(DisplayName = "Given a local sale date When creating a sale Then it is converted to UTC")]
    public void Given_LocalSaleDate_When_CreatingSale_Then_ConvertedToUtc()
    {
        // Given
        var saleDate = new DateTime(2026, 9, 24, 11, 30, 0, DateTimeKind.Local);

        // When
        var sale = Sale.Create(SaleTestData.GenerateSaleNumber(), saleDate, SaleTestData.GenerateCustomer(),
            SaleTestData.GenerateBranch(), [SaleTestData.GenerateItem()]);

        // Then
        sale.SaleDate.Should().Be(new DateTimeOffset(saleDate).UtcDateTime);
        sale.SaleDate.Kind.Should().Be(DateTimeKind.Utc);
    }
}
```

Notes for reviewers:
- FluentAssertions compares `DateTime` values without their kind, so the date tests check the kind separately.
- The local-date test works out the expected value with `DateTimeOffset`, a different API from the `ToUniversalTime()` the code uses. On a machine whose time zone is UTC the two instants are equal anyway, and the kind check still catches a no-op.
- The five theory rows run as five test cases: xUnit serializes the `decimal` rows.

- [ ] **Step 3: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected:

```
SaleTestData.cs(39,19): error CS0246: The type or namespace name 'SaleItemData' could not be found (are you missing a using directive or an assembly reference?)
SaleTestData.cs(46,19): error CS0246: The type or namespace name 'SaleItemData' could not be found (are you missing a using directive or an assembly reference?)
SaleTestData.cs(54,19): error CS0246: The type or namespace name 'Sale' could not be found (are you missing a using directive or an assembly reference?)
SaleTestData.cs(54,42): error CS0246: The type or namespace name 'SaleItemData' could not be found (are you missing a using directive or an assembly reference?)
```

The compiler reports the missing types in the builder's signatures. The errors in `SaleTests.cs` show up only once those exist.

- [ ] **Step 4: Write the line input**

Create `src/Ambev.DeveloperEvaluation.Domain/ValueObjects/SaleItemData.cs`:

```csharp
namespace Ambev.DeveloperEvaluation.Domain.ValueObjects;

/// <summary>
/// The input for one line of a sale. The sale computes the discount and the totals from it (rule R2).
/// </summary>
/// <param name="Product">The product sold.</param>
/// <param name="Quantity">The quantity of identical items, from 1 to 20.</param>
/// <param name="UnitPrice">The price of one item: above 0, with at most 2 decimal places.</param>
public sealed record SaleItemData(ExternalIdentity Product, int Quantity, decimal UnitPrice);
```

- [ ] **Step 5: Write the line entity**

Create `src/Ambev.DeveloperEvaluation.Domain/Entities/SaleItem.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Services;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// One line of a <see cref="Sale"/>: a product, how many identical items were sold, their price,
/// and the discount and total that the domain computed. It changes only through its sale.
/// </summary>
public sealed class SaleItem : BaseEntity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SaleItem"/> class for EF Core, which sets the properties itself.
    /// </summary>
    private SaleItem()
    {
    }

    /// <summary>
    /// Initializes a new line with a new id, and computes its discount (rule R1) and amounts (rule R3).
    /// </summary>
    /// <param name="product">The product sold.</param>
    /// <param name="quantity">The quantity of identical items, from 1 to 20.</param>
    /// <param name="unitPrice">The price of one item.</param>
    /// <exception cref="DomainException">Thrown when the quantity is outside 1 to 20.</exception>
    internal SaleItem(ExternalIdentity product, int quantity, decimal unitPrice)
    {
        Id = Guid.NewGuid();
        Product = product;
        Quantity = quantity;
        UnitPrice = unitPrice;
        DiscountPercentage = DiscountPolicy.GetDiscountPercentage(quantity);

        var grossAmount = quantity * unitPrice;
        DiscountAmount = Math.Round(grossAmount * DiscountPercentage / 100m, 2, MidpointRounding.AwayFromZero);
        TotalAmount = grossAmount - DiscountAmount;
    }

    /// <summary>
    /// Gets the product sold.
    /// </summary>
    public ExternalIdentity Product { get; private set; } = null!;

    /// <summary>
    /// Gets the quantity of identical items, from 1 to 20.
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// Gets the price of one item.
    /// </summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>
    /// Gets the discount percentage for the quantity: 0, 10 or 20.
    /// </summary>
    public decimal DiscountPercentage { get; private set; }

    /// <summary>
    /// Gets the discount: quantity × unit price × percentage ÷ 100, rounded to 2 decimal places away from zero.
    /// </summary>
    public decimal DiscountAmount { get; private set; }

    /// <summary>
    /// Gets the line total: quantity × unit price, minus the discount.
    /// </summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the line was cancelled. A cancelled line doesn't count in the sale total.
    /// </summary>
    public bool IsCancelled { get; private set; }
}
```

The private parameterless constructor and the `private set` accessors are for EF Core (Task 12). `= null!` marks `Product` as always set: the domain constructor sets it, and so does EF Core when it loads a line.

- [ ] **Step 6: Write the aggregate**

Create `src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// A sale: the aggregate root that owns its lines and enforces the sale rules.
/// </summary>
/// <remarks>
/// Customer, branch and products belong to other domains, so the sale refers to them as
/// <see cref="ExternalIdentity"/> values (spec decision D2). Discounts and totals are always computed here (rule R2).
/// </remarks>
public sealed class Sale : BaseEntity
{
    /// <summary>
    /// The maximum length of <see cref="SaleNumber"/>, after trimming.
    /// </summary>
    public const int SaleNumberMaxLength = 50;

    private readonly List<SaleItem> _items = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="Sale"/> class for EF Core, which sets the properties itself.
    /// </summary>
    private Sale()
    {
    }

    /// <summary>
    /// Gets the sale number, unique across all sales. It never changes.
    /// </summary>
    public string SaleNumber { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the date and time of the sale, in UTC.
    /// </summary>
    public DateTime SaleDate { get; private set; }

    /// <summary>
    /// Gets the customer who bought.
    /// </summary>
    public ExternalIdentity Customer { get; private set; } = null!;

    /// <summary>
    /// Gets the branch where the sale was made.
    /// </summary>
    public ExternalIdentity Branch { get; private set; } = null!;

    /// <summary>
    /// Gets the lines of the sale, cancelled ones included. Read-only: lines change only through the sale.
    /// </summary>
    public IReadOnlyCollection<SaleItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Gets the sale total: the sum of the totals of the lines that aren't cancelled.
    /// </summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the sale was cancelled.
    /// </summary>
    public bool IsCancelled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the sale was soft-deleted.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Gets when the sale was soft-deleted, in UTC, or <c>null</c> if it wasn't.
    /// </summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>
    /// Gets when the sale was created, in UTC.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets when the sale was last changed, in UTC, or <c>null</c> if it never was.
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Creates a sale with a new id. Each line gets its discount (rule R1) and amounts (rule R3),
    /// and the sale total is the sum of the line totals.
    /// </summary>
    /// <param name="saleNumber">The sale number.</param>
    /// <param name="saleDate">The date and time of the sale. A value without a kind is read as UTC; a local one is converted (rule R13).</param>
    /// <param name="customer">The customer who bought.</param>
    /// <param name="branch">The branch where the sale was made.</param>
    /// <param name="items">The lines of the sale.</param>
    /// <returns>The new sale.</returns>
    public static Sale Create(
        string saleNumber,
        DateTime saleDate,
        ExternalIdentity customer,
        ExternalIdentity branch,
        IReadOnlyCollection<SaleItemData> items)
    {
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            SaleNumber = saleNumber,
            SaleDate = ToUtc(saleDate),
            Customer = customer,
            Branch = branch,
            CreatedAt = DateTime.UtcNow
        };

        sale._items.AddRange(items.Select(item => new SaleItem(item.Product, item.Quantity, item.UnitPrice)));
        sale.RecalculateTotal();

        return sale;
    }

    private void RecalculateTotal() =>
        TotalAmount = _items.Where(item => !item.IsCancelled).Sum(item => item.TotalAmount);

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
```

`IsDeleted`, `DeletedAt` and `UpdatedAt` exist now because Task 12's migration creates the whole schema. Tickets 07–12 add the methods that set them. `RecalculateTotal` sums only the active lines, so tickets 08 and 09 can call it after cancelling a line. `Id` keeps `BaseEntity`'s public setter (spec §5.1: `Sale` extends the template's `BaseEntity`).

- [ ] **Step 7: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~Domain.Entities.SaleTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)` and `Build succeeded.`; `Passed:    12` (5 theory rows, 2 kinds, 5 facts); `Passed:   116`.

- [ ] **Step 8: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Domain/ValueObjects/SaleItemData.cs src/Ambev.DeveloperEvaluation.Domain/Entities/SaleItem.cs src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/TestData/SaleTestData.cs tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs
git commit -m "feat(sales): create a sale with discounted lines and totals" -m "Sale.Create builds each line with its discount (rule R1) and amounts (rule R3): decimal money, the discount rounded to 2 places away from zero, and the sale total over the active lines. The domain creates the sale and item ids, Items is read-only over a private list, SaleDate is stored in UTC (rule R13) and CreatedAt is set."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `5 files changed, 429 insertions(+)` and `Scan complete: 0 issue(s) found`.

---

### Task 5: Reject invalid sales in the domain

**Skill:** `dotnet-best-practices` (throw `DomainException` with a clear message).

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs` (two `using` lines, a theory data member, the new tests)
- Modify: `src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs`, `src/Ambev.DeveloperEvaluation.Domain/Entities/SaleItem.cs`

- [ ] **Step 1: Add the failing tests**

With the Edit tool, in `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
```

with

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
```

Then replace

```csharp
        { 20, 0.33m, 20m, 1.32m, 5.28m }
    };
```

with

```csharp
        { 20, 0.33m, 20m, 1.32m, 5.28m }
    };

    /// <summary>
    /// Unit prices that are zero or negative.
    /// </summary>
    public static TheoryData<decimal> NonPositiveUnitPrices => new() { 0m, -4.50m };
```

Then add the tests at the end of the class, by replacing

```csharp
        // Then
        sale.SaleDate.Should().Be(new DateTimeOffset(saleDate).UtcDateTime);
        sale.SaleDate.Kind.Should().Be(DateTimeKind.Utc);
    }
}
```

with

```csharp
        // Then
        sale.SaleDate.Should().Be(new DateTimeOffset(saleDate).UtcDateTime);
        sale.SaleDate.Kind.Should().Be(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests rule R5: a sale needs at least one line.
    /// </summary>
    [Fact(DisplayName = "Given no items When creating a sale Then it throws DomainException")]
    public void Given_NoItems_When_CreatingSale_Then_ThrowsDomainException()
    {
        // When
        var act = () => Sale.Create(SaleTestData.GenerateSaleNumber(), DateTime.UtcNow, SaleTestData.GenerateCustomer(),
            SaleTestData.GenerateBranch(), []);

        // Then
        act.Should().Throw<DomainException>().WithMessage("A sale must have at least one item");
    }

    /// <summary>
    /// Tests rule R4: identical items share a product, so a product can have only one line.
    /// </summary>
    [Fact(DisplayName = "Given two lines for the same product When creating a sale Then it throws DomainException")]
    public void Given_TwoLinesForSameProduct_When_CreatingSale_Then_ThrowsDomainException()
    {
        // Given
        var first = SaleTestData.GenerateItem(2, 4.50m);
        var second = first with { Product = new ExternalIdentity(first.Product.Id, "Another name"), Quantity = 3 };

        // When
        var act = () => SaleTestData.CreateSale(first, second);

        // Then
        act.Should().Throw<DomainException>().WithMessage("Each product can appear only once in a sale");
    }

    /// <summary>
    /// Tests that the sale number is stored without surrounding spaces.
    /// </summary>
    [Fact(DisplayName = "Given a sale number with surrounding spaces When creating a sale Then the number is trimmed")]
    public void Given_SaleNumberWithSpaces_When_CreatingSale_Then_NumberIsTrimmed()
    {
        // When
        var sale = Sale.Create("  S-000123  ", DateTime.UtcNow, SaleTestData.GenerateCustomer(),
            SaleTestData.GenerateBranch(), [SaleTestData.GenerateItem()]);

        // Then
        sale.SaleNumber.Should().Be("S-000123");
    }

    /// <summary>
    /// Tests that a sale number is required.
    /// </summary>
    /// <param name="saleNumber">The sale number to try.</param>
    [Theory(DisplayName = "Given a missing or blank sale number When creating a sale Then it throws DomainException")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Given_MissingOrBlankSaleNumber_When_CreatingSale_Then_ThrowsDomainException(string? saleNumber)
    {
        // When
        var act = () => Sale.Create(saleNumber!, DateTime.UtcNow, SaleTestData.GenerateCustomer(),
            SaleTestData.GenerateBranch(), [SaleTestData.GenerateItem()]);

        // Then
        act.Should().Throw<DomainException>().WithMessage("Sale number must have 1 to 50 characters");
    }

    /// <summary>
    /// Tests that a sale number longer than 50 characters is rejected.
    /// </summary>
    [Fact(DisplayName = "Given a sale number of 51 characters When creating a sale Then it throws DomainException")]
    public void Given_SaleNumberOf51Characters_When_CreatingSale_Then_ThrowsDomainException()
    {
        // When
        var act = () => Sale.Create(new string('S', 51), DateTime.UtcNow, SaleTestData.GenerateCustomer(),
            SaleTestData.GenerateBranch(), [SaleTestData.GenerateItem()]);

        // Then
        act.Should().Throw<DomainException>().WithMessage("Sale number must have 1 to 50 characters");
    }

    /// <summary>
    /// Tests that the length limit applies after trimming, so 50 characters plus spaces are accepted.
    /// </summary>
    [Fact(DisplayName = "Given a sale number of 50 characters with surrounding spaces When creating a sale Then it is accepted")]
    public void Given_SaleNumberOf50CharactersWithSpaces_When_CreatingSale_Then_IsAccepted()
    {
        // Given
        var saleNumber = new string('S', 50);

        // When
        var sale = Sale.Create($" {saleNumber} ", DateTime.UtcNow, SaleTestData.GenerateCustomer(),
            SaleTestData.GenerateBranch(), [SaleTestData.GenerateItem()]);

        // Then
        sale.SaleNumber.Should().Be(saleNumber);
    }

    /// <summary>
    /// Tests rule R1 at the aggregate: a line needs 1 to 20 identical items. <see cref="SaleItem"/> gets the
    /// percentage from the discount policy, so these pass as soon as lines are built; they pin that the sale enforces it.
    /// </summary>
    /// <param name="quantity">A quantity outside 1 to 20.</param>
    /// <param name="message">The expected message.</param>
    [Theory(DisplayName = "Given a quantity outside 1 to 20 When creating a sale Then it throws DomainException")]
    [InlineData(0, "Quantity must be at least 1")]
    [InlineData(21, "It's not possible to sell above 20 identical items")]
    public void Given_QuantityOutside1To20_When_CreatingSale_Then_ThrowsDomainException(int quantity, string message)
    {
        // When
        var act = () => SaleTestData.CreateSale(SaleTestData.GenerateItem(quantity, 4.50m));

        // Then
        act.Should().Throw<DomainException>().WithMessage(message);
    }

    /// <summary>
    /// Tests that a unit price must be above zero.
    /// </summary>
    /// <param name="unitPrice">A unit price that is zero or negative.</param>
    [Theory(DisplayName = "Given a unit price of zero or less When creating a sale Then it throws DomainException")]
    [MemberData(nameof(NonPositiveUnitPrices))]
    public void Given_UnitPriceOfZeroOrLess_When_CreatingSale_Then_ThrowsDomainException(decimal unitPrice)
    {
        // When
        var act = () => SaleTestData.CreateSale(SaleTestData.GenerateItem(1, unitPrice));

        // Then
        act.Should().Throw<DomainException>().WithMessage("Unit price must be greater than zero");
    }

    /// <summary>
    /// Tests that a unit price can't have fractions of a cent.
    /// </summary>
    [Fact(DisplayName = "Given a unit price with 3 decimal places When creating a sale Then it throws DomainException")]
    public void Given_UnitPriceWith3DecimalPlaces_When_CreatingSale_Then_ThrowsDomainException()
    {
        // When
        var act = () => SaleTestData.CreateSale(SaleTestData.GenerateItem(1, 4.455m));

        // Then
        act.Should().Throw<DomainException>().WithMessage("Unit price must have at most 2 decimal places");
    }

    /// <summary>
    /// Tests that the decimal-places rule looks at the value, so trailing zeros (as JSON can send them) are fine.
    /// </summary>
    [Fact(DisplayName = "Given a unit price of 4.500 When creating a sale Then it is accepted")]
    public void Given_UnitPriceWithTrailingZero_When_CreatingSale_Then_IsAccepted()
    {
        // When
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(1, 4.500m));

        // Then
        sale.Items.Should().ContainSingle().Which.UnitPrice.Should().Be(4.50m);
    }
}
```

- [ ] **Step 2: Run them and watch them fail (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~Domain.Entities.SaleTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | sort | uniq -c | cut -c1-200
```

Expected: `0 Error(s)` and `Build succeeded.`, then:

```
      9    Expected a <Ambev.DeveloperEvaluation.Domain.Exceptions.DomainException> to be thrown, but no exception was thrown.
      1    Expected sale.SaleNumber to be "S-000123" with a length of 8, but "  S-000123  " has a length of 12, differs near "  S" (index 0).
      1    Expected sale.SaleNumber to be "SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSS" with a length of 50, but " SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSS " has a length of 52, diffe
      1 Failed!  - Failed:    11, Passed:    15, Skipped:     0, Total:    26, Duration: … - Ambev.DeveloperEvaluation.Unit.dll (net8.0)
```

Eleven cases fail, because nothing checks the number, the lines or the price yet. The two quantity cases and the `4.500` case pass already (see Process skills).

- [ ] **Step 3: Check the sale number and the lines in `Sale.Create`**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
```

with

```csharp
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
```

Then replace

```csharp
    public const int SaleNumberMaxLength = 50;

    private readonly List<SaleItem> _items = [];
```

with

```csharp
    public const int SaleNumberMaxLength = 50;

    /// <summary>
    /// The message for a sale without lines (rule R5).
    /// </summary>
    public const string NoItemsMessage = "A sale must have at least one item";

    /// <summary>
    /// The message for a product that appears in more than one line (rule R4).
    /// </summary>
    public const string RepeatedProductMessage = "Each product can appear only once in a sale";

    private readonly List<SaleItem> _items = [];
```

Then replace

```csharp
    /// <param name="saleNumber">The sale number.</param>
    /// <param name="saleDate">The date and time of the sale. A value without a kind is read as UTC; a local one is converted (rule R13).</param>
    /// <param name="customer">The customer who bought.</param>
    /// <param name="branch">The branch where the sale was made.</param>
    /// <param name="items">The lines of the sale.</param>
    /// <returns>The new sale.</returns>
    public static Sale Create(
        string saleNumber,
        DateTime saleDate,
        ExternalIdentity customer,
        ExternalIdentity branch,
        IReadOnlyCollection<SaleItemData> items)
    {
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            SaleNumber = saleNumber,
```

with

```csharp
    /// <param name="saleNumber">The sale number. Trimmed, it must have 1 to 50 characters.</param>
    /// <param name="saleDate">The date and time of the sale. A value without a kind is read as UTC; a local one is converted (rule R13).</param>
    /// <param name="customer">The customer who bought.</param>
    /// <param name="branch">The branch where the sale was made.</param>
    /// <param name="items">The lines of the sale: at least one, and one per product.</param>
    /// <returns>The new sale.</returns>
    /// <exception cref="DomainException">
    /// Thrown when the sale number is missing or too long, there are no lines, a product repeats,
    /// or a line has a quantity outside 1 to 20 or an invalid unit price.
    /// </exception>
    public static Sale Create(
        string saleNumber,
        DateTime saleDate,
        ExternalIdentity customer,
        ExternalIdentity branch,
        IReadOnlyCollection<SaleItemData> items)
    {
        var trimmedSaleNumber = saleNumber?.Trim() ?? string.Empty;
        if (trimmedSaleNumber.Length is 0 or > SaleNumberMaxLength)
            throw new DomainException($"Sale number must have 1 to {SaleNumberMaxLength} characters");

        EnsureValidLines(items);

        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            SaleNumber = trimmedSaleNumber,
```

Then replace

```csharp
    private void RecalculateTotal() =>
```

with

```csharp
    private static void EnsureValidLines(IReadOnlyCollection<SaleItemData> items)
    {
        if (items.Count == 0)
            throw new DomainException(NoItemsMessage);

        if (items.DistinctBy(item => item.Product.Id).Count() != items.Count)
            throw new DomainException(RepeatedProductMessage);
    }

    private void RecalculateTotal() =>
```

`saleNumber?.Trim()` guards a `null` that the parameter's annotation says can't happen, because the domain is the last line of defence. Ticket 09's `Update` calls `EnsureValidLines` as well.

- [ ] **Step 4: Check the unit price in `SaleItem`**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.Domain/Entities/SaleItem.cs`, replace

```csharp
    /// <exception cref="DomainException">Thrown when the quantity is outside 1 to 20.</exception>
    internal SaleItem(ExternalIdentity product, int quantity, decimal unitPrice)
    {
        Id = Guid.NewGuid();
```

with

```csharp
    /// <exception cref="DomainException">
    /// Thrown when the quantity is outside 1 to 20, or the unit price isn't above 0 with at most 2 decimal places.
    /// </exception>
    internal SaleItem(ExternalIdentity product, int quantity, decimal unitPrice)
    {
        if (unitPrice <= 0)
            throw new DomainException("Unit price must be greater than zero");

        if (decimal.Round(unitPrice, 2) != unitPrice)
            throw new DomainException("Unit price must have at most 2 decimal places");

        Id = Guid.NewGuid();
```

- [ ] **Step 5: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~Domain.Entities.SaleTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)` and `Build succeeded.`; `Passed:    26`; `Passed:   130`.

- [ ] **Step 6: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs src/Ambev.DeveloperEvaluation.Domain/Entities/SaleItem.cs tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs
git commit -m "feat(sales): reject invalid sales in the domain" -m "Sale.Create rejects a sale without lines (rule R5), a product in two lines (rule R4) and a sale number that is blank or longer than 50 characters after trimming, and it stores the number trimmed. A line rejects a unit price that isn't above 0 with at most 2 decimal places. The validators catch these first; the domain checks them too, as spec 5.1 asks."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `3 files changed, 202 insertions(+), 4 deletions(-)` and `Scan complete: 0 issue(s) found`.

---

### Task 6: Record `SaleCreatedEvent` when a sale is created

**Skill:** `dotnet-best-practices`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs` (one `using`, two tests)
- Create: `src/Ambev.DeveloperEvaluation.Domain/Events/IDomainEvent.cs`, `src/Ambev.DeveloperEvaluation.Domain/Events/SaleCreatedEvent.cs`
- Modify: `src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs`

- [ ] **Step 1: Add the failing tests**

With the Edit tool, in `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
```

with

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
```

Then replace

```csharp
        // Then
        sale.Items.Should().ContainSingle().Which.UnitPrice.Should().Be(4.50m);
    }
}
```

with

```csharp
        // Then
        sale.Items.Should().ContainSingle().Which.UnitPrice.Should().Be(4.50m);
    }

    /// <summary>
    /// Tests that creating a sale records exactly one <see cref="SaleCreatedEvent"/> with the §5.4 payload,
    /// stamped with the creation time.
    /// </summary>
    [Fact(DisplayName = "Given valid input When creating a sale Then it records one SaleCreatedEvent with the sale's data")]
    public void Given_ValidInput_When_CreatingSale_Then_RecordsSaleCreatedEvent()
    {
        // When
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m), SaleTestData.GenerateItem(3, 10.00m));

        // Then
        sale.DomainEvents.Should().ContainSingle().Which.Should().Be(new SaleCreatedEvent(
            SaleId: sale.Id,
            SaleNumber: sale.SaleNumber,
            CustomerId: sale.Customer.Id,
            BranchId: sale.Branch.Id,
            TotalAmount: 46.20m,
            ItemCount: 2,
            OccurredAt: sale.CreatedAt));
    }

    /// <summary>
    /// Tests that the recorded events can be cleared once they have been published.
    /// </summary>
    [Fact(DisplayName = "Given a new sale with a recorded event When clearing its domain events Then none remain")]
    public void Given_NewSaleWithRecordedEvent_When_ClearingDomainEvents_Then_NoneRemain()
    {
        // Given
        var sale = SaleTestData.CreateSale();

        // When
        sale.ClearDomainEvents();

        // Then
        sale.DomainEvents.Should().BeEmpty();
    }
}
```

The expected event is built with the whole §5.4 payload and compared by record equality, so a missing or wrong field fails the test.

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected:

```
SaleTests.cs(331,14): error CS1061: 'Sale' does not contain a definition for 'DomainEvents' and no accessible extension method 'DomainEvents' accepting a first argument of type 'Sale' could be found (are you missing a using directive or an assembly reference?)
SaleTests.cs(331,74): error CS0246: The type or namespace name 'SaleCreatedEvent' could not be found (are you missing a using directive or an assembly reference?)
SaleTests.cs(351,14): error CS1061: 'Sale' does not contain a definition for 'ClearDomainEvents' and no accessible extension method 'ClearDomainEvents' accepting a first argument of type 'Sale' could be found (are you missing a using directive or an assembly reference?)
SaleTests.cs(354,14): error CS1061: 'Sale' does not contain a definition for 'DomainEvents' and no accessible extension method 'DomainEvents' accepting a first argument of type 'Sale' could be found (are you missing a using directive or an assembly reference?)
```

The `Domain.Events` namespace already exists, from the template's `UserRegisteredEvent`.

- [ ] **Step 3: Write the event types**

Create `src/Ambev.DeveloperEvaluation.Domain/Events/IDomainEvent.cs`:

```csharp
using MediatR;

namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>
/// Something that happened to an aggregate. The aggregate records it; the application publishes it
/// through MediatR only after the change is saved (spec decision D8).
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// Gets when the event happened, in UTC: the moment the aggregate recorded it.
    /// </summary>
    DateTime OccurredAt { get; }
}
```

Domain gets MediatR through its reference to Common (spec §5.4), so no package is added.

Create `src/Ambev.DeveloperEvaluation.Domain/Events/SaleCreatedEvent.cs`:

```csharp
namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>
/// A sale was created.
/// </summary>
/// <param name="SaleId">The id of the new sale.</param>
/// <param name="SaleNumber">The sale number.</param>
/// <param name="CustomerId">The id of the customer who bought.</param>
/// <param name="BranchId">The id of the branch where the sale was made.</param>
/// <param name="TotalAmount">The sale total.</param>
/// <param name="ItemCount">The number of lines.</param>
/// <param name="OccurredAt">When the sale was created, in UTC.</param>
public sealed record SaleCreatedEvent(
    Guid SaleId,
    string SaleNumber,
    Guid CustomerId,
    Guid BranchId,
    decimal TotalAmount,
    int ItemCount,
    DateTime OccurredAt) : IDomainEvent;
```

- [ ] **Step 4: Record the event in `Sale`**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
```

with

```csharp
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
```

Then replace

```csharp
    private readonly List<SaleItem> _items = [];

    /// <summary>
```

with

```csharp
    private readonly List<SaleItem> _items = [];
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
```

Then replace

```csharp
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
```

with

```csharp
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Gets the events recorded since the sale was created or loaded, in order. They aren't stored:
    /// the application publishes them after saving the sale, then clears them.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
```

Then replace

```csharp
    /// Creates a sale with a new id. Each line gets its discount (rule R1) and amounts (rule R3),
    /// and the sale total is the sum of the line totals.
    /// </summary>
```

with

```csharp
    /// Creates a sale with a new id. Each line gets its discount (rule R1) and amounts (rule R3),
    /// the sale total is the sum of the line totals, and a <see cref="SaleCreatedEvent"/> is recorded.
    /// </summary>
```

Then replace

```csharp
        sale._items.AddRange(items.Select(item => new SaleItem(item.Product, item.Quantity, item.UnitPrice)));
        sale.RecalculateTotal();

        return sale;
    }
```

with

```csharp
        sale._items.AddRange(items.Select(item => new SaleItem(item.Product, item.Quantity, item.UnitPrice)));
        sale.RecalculateTotal();

        sale._domainEvents.Add(new SaleCreatedEvent(
            sale.Id, sale.SaleNumber, customer.Id, branch.Id, sale.TotalAmount, sale._items.Count, sale.CreatedAt));

        return sale;
    }

    /// <summary>
    /// Forgets the recorded events, once they have been published.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
```

When EF Core loads a sale, it uses the private constructor, and the field initializer still gives `_domainEvents` an empty list: a loaded sale starts with no events (probe).

- [ ] **Step 5: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~Domain.Entities.SaleTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)` and `Build succeeded.`; `Passed:    28`; `Passed:   132`.

- [ ] **Step 6: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Domain/Events/IDomainEvent.cs src/Ambev.DeveloperEvaluation.Domain/Events/SaleCreatedEvent.cs src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs
git commit -m "feat(sales): record SaleCreatedEvent when a sale is created" -m "Domain events implement IDomainEvent, a MediatR INotification with OccurredAt. Sale.Create records SaleCreatedEvent with the payload of spec 5.4, stamped with CreatedAt, and ClearDomainEvents forgets the events once they are published. The events aren't stored."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `4 files changed, 90 insertions(+), 1 deletion(-)` and `Scan complete: 0 issue(s) found`.

---

### Task 7: Map a sale to a flat `SaleResult`

**Skills:** `dotnet-best-practices`, `type-design-performance`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/SaleProfileTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.Application/Sales/SaleResult.cs`, `src/Ambev.DeveloperEvaluation.Application/Sales/SaleItemResult.cs`, `src/Ambev.DeveloperEvaluation.Application/Sales/SaleProfile.cs`

This comes before the command, because `CreateSaleCommand` returns a `SaleResult`.

- [ ] **Step 1: Write the failing tests**

Create `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/SaleProfileTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using AutoMapper;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

/// <summary>
/// Contains unit tests for <see cref="SaleProfile"/>, which maps the <see cref="Sale"/> aggregate to the flat <see cref="SaleResult"/>.
/// </summary>
public sealed class SaleProfileTests
{
    private readonly MapperConfiguration _configuration = new(config => config.AddProfile<SaleProfile>());

    /// <summary>
    /// Tests that every member of the results has a source, so nothing is left at its default by mistake.
    /// </summary>
    [Fact(DisplayName = "Given the sale profile When validating its configuration Then every result member is mapped")]
    public void Given_SaleProfile_When_ValidatingConfiguration_Then_EveryResultMemberIsMapped()
    {
        // When
        var act = () => _configuration.AssertConfigurationIsValid();

        // Then
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that the external identities are flattened into id and name fields (spec decision D12).
    /// </summary>
    [Fact(DisplayName = "Given a sale When mapping it to SaleResult Then customer, branch and product are flattened into id and name fields")]
    public void Given_Sale_When_MappingToSaleResult_Then_IdentitiesAreFlattened()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m), SaleTestData.GenerateItem(3, 10.00m));

        // When
        var result = _configuration.CreateMapper().Map<SaleResult>(sale);

        // Then
        result.Should().BeEquivalentTo(new
        {
            sale.Id,
            sale.SaleNumber,
            sale.SaleDate,
            CustomerId = sale.Customer.Id,
            CustomerName = sale.Customer.Name,
            BranchId = sale.Branch.Id,
            BranchName = sale.Branch.Name,
            sale.TotalAmount,
            sale.IsCancelled,
            sale.CreatedAt,
            sale.UpdatedAt,
            Items = sale.Items.Select(item => new
            {
                item.Id,
                ProductId = item.Product.Id,
                ProductName = item.Product.Name,
                item.Quantity,
                item.UnitPrice,
                item.DiscountPercentage,
                item.DiscountAmount,
                item.TotalAmount,
                item.IsCancelled
            })
        }, options => options.WithStrictOrdering());
    }
}
```

The configuration holds only `SaleProfile` (Decision 13).

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected:

```
SaleProfileTests.cs(1,45): error CS0234: The type or namespace name 'Sales' does not exist in the namespace 'Ambev.DeveloperEvaluation.Application' (are you missing an assembly reference?)
```

- [ ] **Step 3: Write the results and the profile**

Create `src/Ambev.DeveloperEvaluation.Application/Sales/SaleResult.cs`:

```csharp
namespace Ambev.DeveloperEvaluation.Application.Sales;

/// <summary>
/// A sale as the Sales use cases return it: flat fields (<c>CustomerName</c>, not <c>Customer.Name</c>),
/// so the API can filter and order by the JSON field names (spec decision D12).
/// </summary>
/// <remarks>
/// It has no soft-delete fields: deleted sales are never returned.
/// </remarks>
public sealed class SaleResult
{
    /// <summary>
    /// Gets or sets the id of the sale.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the sale number.
    /// </summary>
    public string SaleNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time of the sale, in UTC.
    /// </summary>
    public DateTime SaleDate { get; set; }

    /// <summary>
    /// Gets or sets the id of the customer.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the customer's name, as copied when the sale was written.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the id of the branch.
    /// </summary>
    public Guid BranchId { get; set; }

    /// <summary>
    /// Gets or sets the branch's name, as copied when the sale was written.
    /// </summary>
    public string BranchName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sale total: the sum of the totals of the lines that aren't cancelled.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the sale was cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// Gets or sets when the sale was created, in UTC.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the sale was last changed, in UTC, or <c>null</c> if it never was.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the lines of the sale, cancelled ones included.
    /// </summary>
    public IReadOnlyList<SaleItemResult> Items { get; set; } = [];
}
```

Create `src/Ambev.DeveloperEvaluation.Application/Sales/SaleItemResult.cs`:

```csharp
namespace Ambev.DeveloperEvaluation.Application.Sales;

/// <summary>
/// A line of a <see cref="SaleResult"/>, with the product flattened into <c>ProductId</c> and <c>ProductName</c>.
/// </summary>
public sealed class SaleItemResult
{
    /// <summary>
    /// Gets or sets the id of the line.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the id of the product.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product's name, as copied when the sale was written.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity of identical items.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the price of one item.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets or sets the discount percentage: 0, 10 or 20.
    /// </summary>
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// Gets or sets the discount amount.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Gets or sets the line total, after the discount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the line was cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }
}
```

Create `src/Ambev.DeveloperEvaluation.Application/Sales/SaleProfile.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Sales;

/// <summary>
/// Maps the <see cref="Sale"/> aggregate to <see cref="SaleResult"/>.
/// </summary>
/// <remarks>
/// AutoMapper's flattening convention fills <c>CustomerId</c> from <c>Customer.Id</c>, <c>ProductName</c> from
/// <c>Product.Name</c>, and so on, so no member needs its own rule. The profile tests check that every member is mapped.
/// </remarks>
public sealed class SaleProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SaleProfile"/> class with the sale and line maps.
    /// </summary>
    public SaleProfile()
    {
        CreateMap<Sale, SaleResult>();
        CreateMap<SaleItem, SaleItemResult>();
    }
}
```

`Program.cs` registers every profile in the Application assembly, so the API picks this one up with no change.

- [ ] **Step 4: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~SaleProfileTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)` and `Build succeeded.`; `Passed:     2`; `Passed:   134`. The total includes ticket 04's mapping tests, which build the API's whole configuration, so the new profile adds no conflicting map.

- [ ] **Step 5: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Application/Sales/SaleResult.cs src/Ambev.DeveloperEvaluation.Application/Sales/SaleItemResult.cs src/Ambev.DeveloperEvaluation.Application/Sales/SaleProfile.cs tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/SaleProfileTests.cs
git commit -m "feat(sales): map a sale to a flat SaleResult" -m "SaleProfile maps the Sale aggregate to SaleResult and its lines to SaleItemResult. AutoMapper's flattening fills CustomerId from Customer.Id and so on (spec decision D12), and the profile passes AssertConfigurationIsValid. The result has no soft-delete fields, since deleted sales are never returned."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `4 files changed, 216 insertions(+)` and `Scan complete: 0 issue(s) found`.

---

### Task 8: Validate the create-sale command

**Skills:** `dotnet-best-practices` (FluentValidation validator in the use-case folder).

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/TestData/CreateSaleCommandTestData.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CreateSale/CreateSaleCommandValidatorTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.Application/Sales/SaleItemInput.cs`, `src/Ambev.DeveloperEvaluation.Application/Sales/SaleItemInputValidator.cs`, `src/Ambev.DeveloperEvaluation.Application/Sales/SaleValidationRules.cs`
- Create: `src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleCommand.cs`, `src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleCommandValidator.cs`

- [ ] **Step 1: Add the command builder**

Create `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/TestData/CreateSaleCommandTestData.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Bogus;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.TestData;

/// <summary>
/// Generates valid <see cref="CreateSaleCommand"/> instances with Bogus. Every generated value passes the
/// validator and the domain rules, so a test changes only the value it is about.
/// </summary>
public static class CreateSaleCommandTestData
{
    private static readonly Faker Faker = new();

    /// <summary>
    /// Generates a valid command whose lines are all for different products.
    /// </summary>
    /// <param name="itemCount">The number of lines.</param>
    /// <returns>A valid command.</returns>
    public static CreateSaleCommand GenerateValidCommand(int itemCount = 2) => new()
    {
        SaleNumber = $"S-{Faker.Random.Number(100000, 999999)}",
        SaleDate = Faker.Date.Recent().ToUniversalTime(),
        CustomerId = Guid.NewGuid(),
        CustomerName = Faker.Name.FullName(),
        BranchId = Guid.NewGuid(),
        BranchName = $"Filial {Faker.Address.City()}",
        Items = Enumerable.Range(0, itemCount).Select(_ => GenerateValidItem()).ToList()
    };

    /// <summary>
    /// Generates a valid line for a new product, with a quantity from 1 to 20 and a price with 2 decimal places.
    /// </summary>
    /// <returns>A valid line.</returns>
    public static SaleItemInput GenerateValidItem() => new()
    {
        ProductId = Guid.NewGuid(),
        ProductName = Faker.Commerce.ProductName(),
        Quantity = Faker.Random.Int(1, 20),
        UnitPrice = Math.Round(Faker.Random.Decimal(0.01m, 500m), 2)
    };
}
```

- [ ] **Step 2: Write the failing tests, one per rule**

Create `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CreateSale/CreateSaleCommandValidatorTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Unit.Application.Sales.TestData;
using FluentValidation.TestHelper;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.CreateSale;

/// <summary>
/// Contains unit tests for <see cref="CreateSaleCommandValidator"/>, one per rule. Each invalid case checks that
/// its failure is the only one, so the random valid data can't hide another problem.
/// </summary>
public sealed class CreateSaleCommandValidatorTests
{
    private readonly CreateSaleCommandValidator _validator = new();

    /// <summary>
    /// Unit prices that are zero or negative.
    /// </summary>
    public static TheoryData<decimal> NonPositiveUnitPrices => new() { 0m, -4.50m };

    /// <summary>
    /// Tests that a command with valid data has no failures.
    /// </summary>
    [Fact(DisplayName = "Given a valid command When validating Then there are no failures")]
    public void Given_ValidCommand_When_Validating_Then_NoFailures()
    {
        // When
        var result = _validator.TestValidate(CreateSaleCommandTestData.GenerateValidCommand());

        // Then
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Tests that the sale number is required. For now the client always sends it; ticket 06 makes it optional.
    /// </summary>
    /// <param name="saleNumber">The sale number to try.</param>
    [Theory(DisplayName = "Given a missing or blank sale number When validating Then SaleNumber fails as empty")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Given_MissingOrBlankSaleNumber_When_Validating_Then_SaleNumberFails(string? saleNumber)
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        command.SaleNumber = saleNumber!;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.SaleNumber).WithErrorMessage("'Sale Number' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that the sale number has at most 50 characters after trimming.
    /// </summary>
    [Fact(DisplayName = "Given a sale number of 51 characters When validating Then SaleNumber fails as too long")]
    public void Given_SaleNumberOf51Characters_When_Validating_Then_SaleNumberFails()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        command.SaleNumber = new string('S', 51);

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.SaleNumber)
            .WithErrorMessage("'Sale Number' must have at most 50 characters after trimming.").Only();
    }

    /// <summary>
    /// Tests that surrounding spaces don't count towards the sale number's length, as the domain trims them.
    /// </summary>
    [Fact(DisplayName = "Given a sale number of 50 characters with surrounding spaces When validating Then there are no failures")]
    public void Given_SaleNumberOf50CharactersWithSpaces_When_Validating_Then_NoFailures()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        command.SaleNumber = $"  {new string('S', 50)}  ";

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Tests that the sale date is required. A missing <c>saleDate</c> in the JSON arrives as <see cref="DateTime.MinValue"/>.
    /// </summary>
    [Fact(DisplayName = "Given no sale date When validating Then SaleDate fails as empty")]
    public void Given_NoSaleDate_When_Validating_Then_SaleDateFails()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        command.SaleDate = default;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.SaleDate).WithErrorMessage("'Sale Date' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that the customer id is required.
    /// </summary>
    [Fact(DisplayName = "Given an empty customer id When validating Then CustomerId fails as empty")]
    public void Given_EmptyCustomerId_When_Validating_Then_CustomerIdFails()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        command.CustomerId = Guid.Empty;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.CustomerId).WithErrorMessage("'Customer Id' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that the customer name is required.
    /// </summary>
    [Fact(DisplayName = "Given a blank customer name When validating Then CustomerName fails as empty")]
    public void Given_BlankCustomerName_When_Validating_Then_CustomerNameFails()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        command.CustomerName = "   ";

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.CustomerName).WithErrorMessage("'Customer Name' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that the branch id is required.
    /// </summary>
    [Fact(DisplayName = "Given an empty branch id When validating Then BranchId fails as empty")]
    public void Given_EmptyBranchId_When_Validating_Then_BranchIdFails()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        command.BranchId = Guid.Empty;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.BranchId).WithErrorMessage("'Branch Id' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that the branch name has at most 100 characters after trimming.
    /// </summary>
    [Fact(DisplayName = "Given a branch name of 101 characters When validating Then BranchName fails as too long")]
    public void Given_BranchNameOf101Characters_When_Validating_Then_BranchNameFails()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        command.BranchName = new string('b', 101);

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.BranchName)
            .WithErrorMessage("'Branch Name' must have at most 100 characters after trimming.").Only();
    }

    /// <summary>
    /// Tests rule R5: a sale needs at least one line.
    /// </summary>
    [Fact(DisplayName = "Given no items When validating Then Items fails with the no-items message")]
    public void Given_NoItems_When_Validating_Then_ItemsFails()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        command.Items = [];

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.Items).WithErrorMessage("A sale must have at least one item").Only();
    }

    /// <summary>
    /// Tests rule R4: a product can have only one line.
    /// </summary>
    [Fact(DisplayName = "Given two items for the same product When validating Then Items fails with the repeated-product message")]
    public void Given_TwoItemsForSameProduct_When_Validating_Then_ItemsFails()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand(itemCount: 2);
        command.Items[1].ProductId = command.Items[0].ProductId;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.Items).WithErrorMessage("Each product can appear only once in a sale").Only();
    }

    /// <summary>
    /// Tests that a missing line (<c>"items": [null]</c> in the JSON) is a validation failure, not a crash.
    /// </summary>
    [Fact(DisplayName = "Given a null item When validating Then Items[0] fails as empty")]
    public void Given_NullItem_When_Validating_Then_ItemFails()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand(itemCount: 1);
        command.Items = [null!];

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor("Items[0]").WithErrorMessage("'Items' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that each line needs a product id.
    /// </summary>
    [Fact(DisplayName = "Given an item with an empty product id When validating Then Items[0].ProductId fails as empty")]
    public void Given_ItemWithEmptyProductId_When_Validating_Then_ProductIdFails()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand(itemCount: 1);
        command.Items[0].ProductId = Guid.Empty;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor("Items[0].ProductId").WithErrorMessage("'Product Id' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that each line needs a product name.
    /// </summary>
    [Fact(DisplayName = "Given an item with a blank product name When validating Then Items[0].ProductName fails as empty")]
    public void Given_ItemWithBlankProductName_When_Validating_Then_ProductNameFails()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand(itemCount: 1);
        command.Items[0].ProductName = string.Empty;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor("Items[0].ProductName").WithErrorMessage("'Product Name' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that surrounding spaces don't count towards a name's length, as the domain trims them.
    /// </summary>
    [Fact(DisplayName = "Given a product name of 100 characters with surrounding spaces When validating Then there are no failures")]
    public void Given_ProductNameOf100CharactersWithSpaces_When_Validating_Then_NoFailures()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand(itemCount: 1);
        command.Items[0].ProductName = $" {new string('p', 100)} ";

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Tests rule R1's range: a quantity below 1, or above 20 with R1's own message.
    /// </summary>
    /// <param name="quantity">A quantity outside 1 to 20.</param>
    /// <param name="message">The expected message.</param>
    [Theory(DisplayName = "Given an item with a quantity outside 1 to 20 When validating Then Items[0].Quantity fails")]
    [InlineData(0, "'Quantity' must be greater than or equal to '1'.")]
    [InlineData(21, "It's not possible to sell above 20 identical items")]
    public void Given_ItemWithQuantityOutside1To20_When_Validating_Then_QuantityFails(int quantity, string message)
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand(itemCount: 1);
        command.Items[0].Quantity = quantity;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor("Items[0].Quantity").WithErrorMessage(message).Only();
    }

    /// <summary>
    /// Tests that the quantity limits themselves are accepted.
    /// </summary>
    /// <param name="quantity">1 or 20.</param>
    [Theory(DisplayName = "Given an item with a quantity of 1 or 20 When validating Then there are no failures")]
    [InlineData(1)]
    [InlineData(20)]
    public void Given_ItemWithQuantityAtLimit_When_Validating_Then_NoFailures(int quantity)
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand(itemCount: 1);
        command.Items[0].Quantity = quantity;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Tests that a unit price must be above zero.
    /// </summary>
    /// <param name="unitPrice">A unit price that is zero or negative.</param>
    [Theory(DisplayName = "Given an item with a unit price of zero or less When validating Then Items[0].UnitPrice fails")]
    [MemberData(nameof(NonPositiveUnitPrices))]
    public void Given_ItemWithUnitPriceOfZeroOrLess_When_Validating_Then_UnitPriceFails(decimal unitPrice)
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand(itemCount: 1);
        command.Items[0].UnitPrice = unitPrice;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor("Items[0].UnitPrice").WithErrorMessage("'Unit Price' must be greater than '0'.").Only();
    }

    /// <summary>
    /// Tests that a unit price can't have fractions of a cent.
    /// </summary>
    [Fact(DisplayName = "Given an item with a unit price with 3 decimal places When validating Then Items[0].UnitPrice fails")]
    public void Given_ItemWithUnitPriceWith3DecimalPlaces_When_Validating_Then_UnitPriceFails()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand(itemCount: 1);
        command.Items[0].UnitPrice = 4.455m;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor("Items[0].UnitPrice")
            .WithErrorMessage("'Unit Price' must have at most 2 decimal places.").Only();
    }
}
```

Notes for reviewers:
- `Only()` fails the test if any other property also has a failure, so random data can't make a case pass for the wrong reason.
- The null-item case is the rehearsal's finding (Decision 7). Without the `NotNull()` it crashed with `NullReferenceException` inside the repeated-product check.
- FluentValidation reports a failure on a missing line as `'Items' must not be empty.`, under the key `Items[0]`.

- [ ] **Step 3: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected:

```
CreateSaleCommandTestData.cs(2,51): error CS0234: The type or namespace name 'CreateSale' does not exist in the namespace 'Ambev.DeveloperEvaluation.Application.Sales' (are you missing an assembly reference?)
CreateSaleCommandTestData.cs(20,19): error CS0246: The type or namespace name 'CreateSaleCommand' could not be found (are you missing a using directive or an assembly reference?)
CreateSaleCommandTestData.cs(35,19): error CS0246: The type or namespace name 'SaleItemInput' could not be found (are you missing a using directive or an assembly reference?)
CreateSaleCommandValidatorTests.cs(1,51): error CS0234: The type or namespace name 'CreateSale' does not exist in the namespace 'Ambev.DeveloperEvaluation.Application.Sales' (are you missing an assembly reference?)
CreateSaleCommandValidatorTests.cs(14,22): error CS0246: The type or namespace name 'CreateSaleCommandValidator' could not be found (are you missing a using directive or an assembly reference?)
```

- [ ] **Step 4: Write the line input, its validator and the shared rules**

Create `src/Ambev.DeveloperEvaluation.Application/Sales/SaleItemInput.cs`:

```csharp
namespace Ambev.DeveloperEvaluation.Application.Sales;

/// <summary>
/// One line of a sale as a command sends it. There are no discount fields: the domain computes them (rule R2).
/// </summary>
public sealed class SaleItemInput
{
    /// <summary>
    /// Gets or sets the id of the product.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product's name, copied into the sale.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity of identical items, from 1 to 20.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the price of one item: above 0, with at most 2 decimal places.
    /// </summary>
    public decimal UnitPrice { get; set; }
}
```

Create `src/Ambev.DeveloperEvaluation.Application/Sales/SaleItemInputValidator.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Services;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales;

/// <summary>
/// Validates one line of a sale command: a product, a quantity from 1 to 20 (rule R1) and a unit price
/// above 0 with at most 2 decimal places.
/// </summary>
public sealed class SaleItemInputValidator : AbstractValidator<SaleItemInput>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SaleItemInputValidator"/> class with the line rules.
    /// </summary>
    public SaleItemInputValidator()
    {
        RuleFor(item => item.ProductId).NotEmpty();
        RuleFor(item => item.ProductName).NotEmpty().MaximumTrimmedLength(ExternalIdentity.NameMaxLength);

        RuleFor(item => item.Quantity)
            .GreaterThanOrEqualTo(DiscountPolicy.MinQuantity)
            .LessThanOrEqualTo(DiscountPolicy.MaxQuantity).WithMessage(DiscountPolicy.MaxQuantityExceededMessage);

        RuleFor(item => item.UnitPrice)
            .GreaterThan(0)
            .Must(price => decimal.Round(price, 2) == price).WithMessage("'{PropertyName}' must have at most 2 decimal places.");
    }
}
```

Create `src/Ambev.DeveloperEvaluation.Application/Sales/SaleValidationRules.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales;

/// <summary>
/// Validation rules that the Sales commands share, so create and update check the same things the same way.
/// </summary>
public static class SaleValidationRules
{
    /// <summary>
    /// Checks the length of a text after trimming, as the domain stores it. A <c>null</c> passes: pair it with <c>NotEmpty</c>.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="ruleBuilder">The rule for the text property.</param>
    /// <param name="maximumLength">The largest length allowed after trimming.</param>
    /// <returns>The rule builder, for chaining.</returns>
    public static IRuleBuilderOptions<T, string> MaximumTrimmedLength<T>(this IRuleBuilder<T, string> ruleBuilder, int maximumLength) =>
        ruleBuilder
            .Must(value => value is null || value.Trim().Length <= maximumLength)
            .WithMessage($"'{{PropertyName}}' must have at most {maximumLength} characters after trimming.");

    /// <summary>
    /// Checks the lines of a sale: at least one (rule R5), one per product (rule R4), and each line present and valid.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="ruleBuilder">The rule for the lines property.</param>
    /// <returns>The rule builder, for chaining.</returns>
    public static IRuleBuilderOptions<T, IEnumerable<SaleItemInput>> ValidSaleItems<T>(
        this IRuleBuilder<T, IReadOnlyList<SaleItemInput>> ruleBuilder) =>
        ruleBuilder
            .NotEmpty().WithMessage(Sale.NoItemsMessage)
            .Must(items => items is null || HasOneLinePerProduct(items)).WithMessage(Sale.RepeatedProductMessage)
            .ForEach(item => item.NotNull().SetValidator(new SaleItemInputValidator()));

    /// <summary>
    /// Tells whether no product appears in two lines. Missing lines are skipped here; the per-line rule reports them.
    /// </summary>
    private static bool HasOneLinePerProduct(IReadOnlyList<SaleItemInput> items)
    {
        var productIds = items.Where(item => item is not null).Select(item => item.ProductId).ToList();
        return productIds.Distinct().Count() == productIds.Count;
    }
}
```

`ForEach` returns a rule builder over `IEnumerable<SaleItemInput>`, which is why `ValidSaleItems` returns that type: the rehearsal's first try with `IReadOnlyList<…>` failed with CS0266. In the interpolated message, `{{PropertyName}}` comes out as FluentValidation's `{PropertyName}` placeholder.

- [ ] **Step 5: Write the command and its validator**

Create `src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleCommand.cs`:

```csharp
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

/// <summary>
/// Creates a sale. The discounts and totals aren't sent: the domain computes them (rule R2).
/// </summary>
/// <remarks>
/// <see cref="CreateSaleCommandValidator"/> checks it before the handler runs, through the MediatR <c>ValidationBehavior</c>.
/// </remarks>
public sealed class CreateSaleCommand : IRequest<SaleResult>
{
    /// <summary>
    /// Gets or sets the sale number. Trimmed, it must have 1 to 50 characters.
    /// </summary>
    public string SaleNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time of the sale. A value without an offset is read as UTC.
    /// </summary>
    public DateTime SaleDate { get; set; }

    /// <summary>
    /// Gets or sets the id of the customer.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the customer's name, copied into the sale.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the id of the branch.
    /// </summary>
    public Guid BranchId { get; set; }

    /// <summary>
    /// Gets or sets the branch's name, copied into the sale.
    /// </summary>
    public string BranchName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the lines of the sale: at least one, and one per product.
    /// </summary>
    public IReadOnlyList<SaleItemInput> Items { get; set; } = [];
}
```

Create `src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleCommandValidator.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

/// <summary>
/// Validates <see cref="CreateSaleCommand"/> before its handler runs. A failure throws <see cref="ValidationException"/>,
/// which the API returns as 400 <c>ValidationError</c>.
/// </summary>
public sealed class CreateSaleCommandValidator : AbstractValidator<CreateSaleCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSaleCommandValidator"/> class with the create rules.
    /// </summary>
    public CreateSaleCommandValidator()
    {
        RuleFor(command => command.SaleNumber).NotEmpty().MaximumTrimmedLength(Sale.SaleNumberMaxLength);
        RuleFor(command => command.SaleDate).NotEmpty();
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.CustomerName).NotEmpty().MaximumTrimmedLength(ExternalIdentity.NameMaxLength);
        RuleFor(command => command.BranchId).NotEmpty();
        RuleFor(command => command.BranchName).NotEmpty().MaximumTrimmedLength(ExternalIdentity.NameMaxLength);
        RuleFor(command => command.Items).ValidSaleItems();
    }
}
```

`NotEmpty` also rejects a text made only of spaces. Ticket 03's `AddValidatorsFromAssembly` registers both validators, so `ValidationBehavior` runs `CreateSaleCommandValidator` for every `CreateSaleCommand`. `SaleItemInputValidator` gets registered too, but no MediatR request has that type, so the pipeline never runs it on its own.

- [ ] **Step 6: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~CreateSaleCommandValidatorTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)` and `Build succeeded.`; `Passed:    24`; `Passed:   158`.

- [ ] **Step 7: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Application/Sales/SaleItemInput.cs src/Ambev.DeveloperEvaluation.Application/Sales/SaleItemInputValidator.cs src/Ambev.DeveloperEvaluation.Application/Sales/SaleValidationRules.cs src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleCommand.cs src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleCommandValidator.cs tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/TestData/CreateSaleCommandTestData.cs tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CreateSale/CreateSaleCommandValidatorTests.cs
git commit -m "feat(sales): validate the create-sale command" -m "CreateSaleCommandValidator checks the sale number (1 to 50 characters after trimming), the sale date, the customer and branch ids, and their names (1 to 100 characters after trimming). It also checks the lines: at least one, one per product, none missing, each with a product id and name, a quantity from 1 to 20 with R1's message, and a unit price above 0 with at most 2 decimal places. The line rules live in SaleValidationRules, so the update command can reuse them. ValidationBehavior runs the validator before the handler."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `7 files changed, 570 insertions(+)` and `Scan complete: 0 issue(s) found`.

---

### Task 9: Create a sale and publish its events after the save

**Skills:** `dotnet-best-practices`, `test-anti-patterns` project note (the order assertion is the behaviour under test).

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CreateSale/CreateSaleHandlerTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs`
- Create: `src/Ambev.DeveloperEvaluation.Application/Sales/SaleEventPublisher.cs`
- Create: `src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleHandler.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CreateSale/CreateSaleHandlerTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Application.Sales.TestData;
using AutoMapper;
using FluentAssertions;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.CreateSale;

/// <summary>
/// Contains unit tests for <see cref="CreateSaleHandler"/>. The mapper is the real Sales profile, so the result
/// shows what the domain computed.
/// </summary>
public sealed class CreateSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly CreateSaleHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSaleHandlerTests"/> class.
    /// </summary>
    public CreateSaleHandlerTests()
    {
        var mapper = new MapperConfiguration(config => config.AddProfile<SaleProfile>()).CreateMapper();
        _handler = new CreateSaleHandler(_saleRepository, _publisher, mapper);
    }

    /// <summary>
    /// Tests that the handler saves the sale the domain built from the command and returns it,
    /// with the discount the domain computed (5 × 4.45 gets 10%, 2.23).
    /// </summary>
    [Fact(DisplayName = "Given a valid command When handling Then it saves the sale built from it and returns it with the domain's discount")]
    public async Task Given_ValidCommand_When_Handling_Then_SavesSaleAndReturnsItWithDiscount()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand(itemCount: 1);
        command.Items[0].Quantity = 5;
        command.Items[0].UnitPrice = 4.45m;
        Sale? saved = null;
        _saleRepository.When(repository => repository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>()))
            .Do(call => saved = call.Arg<Sale>());

        // When
        var result = await _handler.Handle(command, CancellationToken.None);

        // Then
        saved.Should().NotBeNull();
        result.Should().BeEquivalentTo(new
        {
            saved!.Id,
            command.SaleNumber,
            command.SaleDate,
            command.CustomerId,
            command.CustomerName,
            command.BranchId,
            command.BranchName,
            TotalAmount = 20.02m,
            IsCancelled = false,
            Items = new[]
            {
                new
                {
                    command.Items[0].ProductId,
                    command.Items[0].ProductName,
                    Quantity = 5,
                    UnitPrice = 4.45m,
                    DiscountPercentage = 10m,
                    DiscountAmount = 2.23m,
                    TotalAmount = 20.02m
                }
            }
        });
    }

    /// <summary>
    /// Tests rule R14 and spec decision D8: the recorded event is published after the save, and then cleared.
    /// Call order is the behaviour under test here.
    /// </summary>
    [Fact(DisplayName = "Given a valid command When handling Then it publishes SaleCreatedEvent after the save and clears the events")]
    public async Task Given_ValidCommand_When_Handling_Then_PublishesSaleCreatedEventAfterSave()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        Sale? saved = null;
        _saleRepository.When(repository => repository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>()))
            .Do(call => saved = call.Arg<Sale>());
        var published = new List<IDomainEvent>();
        _publisher.When(publisher => publisher.Publish(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>()))
            .Do(call => published.Add(call.Arg<IDomainEvent>()));

        // When
        await _handler.Handle(command, CancellationToken.None);

        // Then
        Received.InOrder(() =>
        {
            _saleRepository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
            _publisher.Publish(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
        });
        published.Should().ContainSingle().Which.Should().BeOfType<SaleCreatedEvent>()
            .Which.SaleId.Should().Be(saved!.Id);
        saved.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R14: when the save fails, nothing is published.
    /// </summary>
    [Fact(DisplayName = "Given the save throws When handling Then the exception propagates and nothing is published")]
    public async Task Given_SaveThrows_When_Handling_Then_NothingIsPublished()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        _saleRepository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("The database is unavailable"));

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("The database is unavailable");
        _publisher.ReceivedCalls().Should().BeEmpty();
    }
}
```

Notes for reviewers:
- `Received.InOrder` takes a plain (not async) lambda, and the calls inside it only describe the expected sequence.
- `_publisher.Publish(Arg.Any<IDomainEvent>(), …)` matches `IPublisher.Publish<IDomainEvent>`, the overload the helper calls, because the helper holds each event as `IDomainEvent`.
- The rehearsal swapped the save and the publish in the handler: both the order test and the failed-save test then failed, and they passed again once restored.

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected:

```
CreateSaleHandlerTests.cs(22,22): error CS0246: The type or namespace name 'ISaleRepository' could not be found (are you missing a using directive or an assembly reference?)
CreateSaleHandlerTests.cs(24,22): error CS0246: The type or namespace name 'CreateSaleHandler' could not be found (are you missing a using directive or an assembly reference?)
```

- [ ] **Step 3: Write the repository contract**

Create `src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// Stores and loads <see cref="Sale"/> aggregates, always together with their items.
/// </summary>
public interface ISaleRepository
{
    /// <summary>
    /// Saves a new sale and its items.
    /// </summary>
    /// <param name="sale">The sale to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the sale is saved.</returns>
    Task CreateAsync(Sale sale, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a sale with its items. The sale is tracked, so a handler can change it and save it.
    /// </summary>
    /// <param name="id">The id of the sale.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The sale, or <c>null</c> if there is none with that id.</returns>
    Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
```

Task 10 uses `GetByIdAsync`; both are declared now because the ticket defines them as one contract (Decision 9).

- [ ] **Step 4: Write the publishing helper**

Create `src/Ambev.DeveloperEvaluation.Application/Sales/SaleEventPublisher.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales;

/// <summary>
/// Publishes the events a sale recorded, for every Sales handler that changes a sale.
/// </summary>
public static class SaleEventPublisher
{
    /// <summary>
    /// Publishes the sale's recorded events through MediatR, in the order they were recorded, then clears them.
    /// Call it only after the save has returned, so a failed save publishes nothing (rule R14).
    /// </summary>
    /// <param name="publisher">The MediatR publisher.</param>
    /// <param name="sale">The saved sale.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when every event has been handled.</returns>
    public static async Task PublishDomainEventsAsync(this IPublisher publisher, Sale sale, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in sale.DomainEvents)
            await publisher.Publish(domainEvent, cancellationToken);

        sale.ClearDomainEvents();
    }
}
```

- [ ] **Step 5: Write the handler**

Create `src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleHandler.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using AutoMapper;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

/// <summary>
/// Handles <see cref="CreateSaleCommand"/>: the domain builds the sale, the repository saves it,
/// and only then are the recorded events published.
/// </summary>
public sealed class CreateSaleHandler : IRequestHandler<CreateSaleCommand, SaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IPublisher _publisher;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSaleHandler"/> class.
    /// </summary>
    /// <param name="saleRepository">The sale repository.</param>
    /// <param name="publisher">The MediatR publisher for the recorded events.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    public CreateSaleHandler(ISaleRepository saleRepository, IPublisher publisher, IMapper mapper)
    {
        _saleRepository = saleRepository;
        _publisher = publisher;
        _mapper = mapper;
    }

    /// <summary>
    /// Creates the sale, saves it, then publishes its events.
    /// </summary>
    /// <param name="command">The validated command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created sale.</returns>
    public async Task<SaleResult> Handle(CreateSaleCommand command, CancellationToken cancellationToken)
    {
        var sale = Sale.Create(
            command.SaleNumber,
            command.SaleDate,
            new ExternalIdentity(command.CustomerId, command.CustomerName),
            new ExternalIdentity(command.BranchId, command.BranchName),
            command.Items.Select(ToItemData).ToList());

        await _saleRepository.CreateAsync(sale, cancellationToken);
        await _publisher.PublishDomainEventsAsync(sale, cancellationToken);

        return _mapper.Map<SaleResult>(sale);
    }

    private static SaleItemData ToItemData(SaleItemInput item) =>
        new(new ExternalIdentity(item.ProductId, item.ProductName), item.Quantity, item.UnitPrice);
}
```

The handler doesn't validate the command itself: `ValidationBehavior` has already done it (Decision 7). MediatR's `AddMediatR` registers `IPublisher`, so nothing new is registered.

- [ ] **Step 6: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~CreateSaleHandlerTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)` and `Build succeeded.`; `Passed:     3`; `Passed:   161`.

- [ ] **Step 7: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs src/Ambev.DeveloperEvaluation.Application/Sales/SaleEventPublisher.cs src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleHandler.cs tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CreateSale/CreateSaleHandlerTests.cs
git commit -m "feat(sales): create a sale and publish its events after the save" -m "CreateSaleHandler builds the sale with Sale.Create, saves it through ISaleRepository, and only then publishes the recorded events through MediatR's IPublisher, clearing them afterwards (spec decision D8, rule R14). If the save throws, nothing is published. SaleEventPublisher is the shared helper that every Sales handler that changes a sale will call."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `4 files changed, 236 insertions(+)` and `Scan complete: 0 issue(s) found`.

---

### Task 10: Get a sale by id

**Skill:** `dotnet-best-practices`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/GetSale/GetSaleHandlerTests.cs`, `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/GetSale/GetSaleQueryValidatorTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.Application/Sales/GetSale/GetSaleQuery.cs`, `GetSaleQueryValidator.cs`, `GetSaleHandler.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/GetSale/GetSaleHandlerTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.GetSale;

/// <summary>
/// Contains unit tests for <see cref="GetSaleHandler"/>.
/// </summary>
public sealed class GetSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly GetSaleHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSaleHandlerTests"/> class.
    /// </summary>
    public GetSaleHandlerTests()
    {
        var mapper = new MapperConfiguration(config => config.AddProfile<SaleProfile>()).CreateMapper();
        _handler = new GetSaleHandler(_saleRepository, mapper);
    }

    /// <summary>
    /// Tests that an existing sale is returned with its lines.
    /// </summary>
    [Fact(DisplayName = "Given an existing sale When getting it by id Then it returns the sale with its items")]
    public async Task Given_ExistingSale_When_GettingById_Then_ReturnsSaleWithItems()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m));
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        // When
        var result = await _handler.Handle(new GetSaleQuery(sale.Id), CancellationToken.None);

        // Then
        result.Should().BeEquivalentTo(new
        {
            sale.Id,
            sale.SaleNumber,
            CustomerName = sale.Customer.Name,
            TotalAmount = 16.20m,
            Items = new[] { new { sale.Items.Single().Id, DiscountPercentage = 10m, DiscountAmount = 1.80m } }
        });
    }

    /// <summary>
    /// Tests that an unknown id is a not-found error, which the API returns as 404 <c>ResourceNotFound</c>.
    /// </summary>
    [Fact(DisplayName = "Given no sale with the id When getting it by id Then it throws KeyNotFoundException")]
    public async Task Given_NoSaleWithId_When_GettingById_Then_ThrowsKeyNotFoundException()
    {
        // Given
        var id = Guid.NewGuid();
        _saleRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Sale?)null);

        // When
        var act = () => _handler.Handle(new GetSaleQuery(id), CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"The sale with ID {id} does not exist");
    }
}
```

Create `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/GetSale/GetSaleQueryValidatorTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using FluentValidation.TestHelper;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.GetSale;

/// <summary>
/// Contains unit tests for <see cref="GetSaleQueryValidator"/>.
/// </summary>
public sealed class GetSaleQueryValidatorTests
{
    private readonly GetSaleQueryValidator _validator = new();

    /// <summary>
    /// Tests that an id is required.
    /// </summary>
    [Fact(DisplayName = "Given an empty id When validating Then Id fails as empty")]
    public void Given_EmptyId_When_Validating_Then_IdFails()
    {
        // When
        var result = _validator.TestValidate(new GetSaleQuery(Guid.Empty));

        // Then
        result.ShouldHaveValidationErrorFor(query => query.Id).WithErrorMessage("'Id' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that any other id passes.
    /// </summary>
    [Fact(DisplayName = "Given an id When validating Then there are no failures")]
    public void Given_Id_When_Validating_Then_NoFailures()
    {
        // When
        var result = _validator.TestValidate(new GetSaleQuery(Guid.NewGuid()));

        // Then
        result.ShouldNotHaveAnyValidationErrors();
    }
}
```

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected:

```
GetSaleHandlerTests.cs(19,22): error CS0246: The type or namespace name 'GetSaleHandler' could not be found (are you missing a using directive or an assembly reference?)
GetSaleHandlerTests.cs(2,51): error CS0234: The type or namespace name 'GetSale' does not exist in the namespace 'Ambev.DeveloperEvaluation.Application.Sales' (are you missing an assembly reference?)
GetSaleQueryValidatorTests.cs(1,51): error CS0234: The type or namespace name 'GetSale' does not exist in the namespace 'Ambev.DeveloperEvaluation.Application.Sales' (are you missing an assembly reference?)
GetSaleQueryValidatorTests.cs(12,22): error CS0246: The type or namespace name 'GetSaleQueryValidator' could not be found (are you missing a using directive or an assembly reference?)
```

- [ ] **Step 3: Write the query, its validator and the handler**

Create `src/Ambev.DeveloperEvaluation.Application/Sales/GetSale/GetSaleQuery.cs`:

```csharp
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSale;

/// <summary>
/// Gets one sale, with its items.
/// </summary>
/// <param name="Id">The id of the sale.</param>
public sealed record GetSaleQuery(Guid Id) : IRequest<SaleResult>;
```

Create `src/Ambev.DeveloperEvaluation.Application/Sales/GetSale/GetSaleQueryValidator.cs`:

```csharp
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSale;

/// <summary>
/// Validates <see cref="GetSaleQuery"/> before its handler runs.
/// </summary>
public sealed class GetSaleQueryValidator : AbstractValidator<GetSaleQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetSaleQueryValidator"/> class: the id is required.
    /// </summary>
    public GetSaleQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}
```

Create `src/Ambev.DeveloperEvaluation.Application/Sales/GetSale/GetSaleHandler.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSale;

/// <summary>
/// Handles <see cref="GetSaleQuery"/>.
/// </summary>
public sealed class GetSaleHandler : IRequestHandler<GetSaleQuery, SaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSaleHandler"/> class.
    /// </summary>
    /// <param name="saleRepository">The sale repository.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    public GetSaleHandler(ISaleRepository saleRepository, IMapper mapper)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// Loads the sale with its items.
    /// </summary>
    /// <param name="query">The validated query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The sale.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when there is no sale with the id.</exception>
    public async Task<SaleResult> Handle(GetSaleQuery query, CancellationToken cancellationToken)
    {
        var sale = await _saleRepository.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"The sale with ID {query.Id} does not exist");

        return _mapper.Map<SaleResult>(sale);
    }
}
```

The message is the one spec §7.4 shows. Ticket 03's middleware turns the exception into 404 `ResourceNotFound`. Ticket 12's query filter later makes a deleted sale take the same path.

- [ ] **Step 4: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~Sales.GetSale" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)` and `Build succeeded.`; `Passed:     4`; `Passed:   165`.

- [ ] **Step 5: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Application/Sales/GetSale/GetSaleQuery.cs src/Ambev.DeveloperEvaluation.Application/Sales/GetSale/GetSaleQueryValidator.cs src/Ambev.DeveloperEvaluation.Application/Sales/GetSale/GetSaleHandler.cs tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/GetSale/GetSaleHandlerTests.cs tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/GetSale/GetSaleQueryValidatorTests.cs
git commit -m "feat(sales): get a sale by id" -m "GetSaleQuery has a validator that requires the id. GetSaleHandler loads the sale with its items, or throws KeyNotFoundException (The sale with ID ... does not exist), which the API returns as 404 ResourceNotFound."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `5 files changed, 175 insertions(+)` and `Scan complete: 0 issue(s) found`.

---

### Task 11: Log published sale events

**Skills:** `dotnet-best-practices` (structured logging), `dependency-injection-patterns` (MediatR's scan registers the handler).

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/SaleEventLogHandlerTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.Application/Sales/SaleEventLogHandler.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/SaleEventLogHandlerTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application;
using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Domain.Events;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

/// <summary>
/// Contains unit tests for <see cref="SaleEventLogHandler"/>, which writes each published sale event to the log
/// in place of a message broker.
/// </summary>
public sealed class SaleEventLogHandlerTests
{
    private readonly ILogger<SaleEventLogHandler> _logger = Substitute.For<ILogger<SaleEventLogHandler>>();

    /// <summary>
    /// Tests that the event becomes one structured Information entry: the template, the event name and the
    /// event itself, which Serilog destructures because of the <c>@</c>.
    /// </summary>
    [Fact(DisplayName = "Given a SaleCreatedEvent When the log handler handles it Then it writes one structured Information entry with the event")]
    public async Task Given_SaleCreatedEvent_When_Handled_Then_WritesStructuredInformationEntry()
    {
        // Given
        var handler = new SaleEventLogHandler(_logger);
        var saleCreated = new SaleCreatedEvent(Guid.NewGuid(), "S-000123", Guid.NewGuid(), Guid.NewGuid(), 20.25m, 1, DateTime.UtcNow);

        // When
        await handler.Handle(saleCreated, CancellationToken.None);

        // Then
        var logCall = _logger.ReceivedCalls().Should().ContainSingle(call => call.GetMethodInfo().Name == nameof(ILogger.Log)).Subject;
        logCall.GetArguments()[0].Should().Be(LogLevel.Information);
        logCall.GetArguments()[2].Should().BeAssignableTo<IReadOnlyList<KeyValuePair<string, object?>>>().Which.Should().Equal(
            new KeyValuePair<string, object?>("EventName", "SaleCreatedEvent"),
            new KeyValuePair<string, object?>("@Event", saleCreated),
            new KeyValuePair<string, object?>("{OriginalFormat}", "Sale event {EventName} published {@Event}"));
    }

    /// <summary>
    /// Tests the wiring the handlers rely on: an event published through <see cref="IPublisher"/> as
    /// <see cref="IDomainEvent"/>, as <see cref="SaleEventPublisher"/> does, reaches the handler of its concrete type.
    /// </summary>
    [Fact(DisplayName = "Given MediatR with the Application handlers When a SaleCreatedEvent is published as IDomainEvent Then the log handler writes it")]
    public async Task Given_MediatRWithApplicationHandlers_When_EventPublishedAsIDomainEvent_Then_LogHandlerWritesIt()
    {
        // Given
        var services = new ServiceCollection();
        services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(ApplicationLayer).Assembly));
        services.AddSingleton(_logger);
        await using var provider = services.BuildServiceProvider();
        IDomainEvent saleCreated = new SaleCreatedEvent(Guid.NewGuid(), "S-000123", Guid.NewGuid(), Guid.NewGuid(), 20.25m, 1, DateTime.UtcNow);

        // When
        await provider.GetRequiredService<IPublisher>().Publish(saleCreated);

        // Then
        _logger.ReceivedCalls().Should().ContainSingle(call => call.GetMethodInfo().Name == nameof(ILogger.Log))
            .Which.GetArguments()[2].Should().BeAssignableTo<IReadOnlyList<KeyValuePair<string, object?>>>()
            .Which.Should().Contain(new KeyValuePair<string, object?>("@Event", saleCreated));
    }
}
```

Notes for reviewers:
- `ILogger.Log` is generic over an internal state type, so the tests read the call's arguments, as ticket 03's middleware tests do. Argument 0 is the level. Argument 2 is the state: the template's values in order, then `{OriginalFormat}`, which the rehearsal confirmed with `Equal`.
- The second test builds MediatR as `Program.cs` does, for the Application assembly. It resolves nothing else, so the other handlers' dependencies aren't needed.

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected:

```
SaleEventLogHandlerTests.cs(19,30): error CS0246: The type or namespace name 'SaleEventLogHandler' could not be found (are you missing a using directive or an assembly reference?)
```

- [ ] **Step 3: Write the handler**

Create `src/Ambev.DeveloperEvaluation.Application/Sales/SaleEventLogHandler.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales;

/// <summary>
/// Writes each published sale event to the application log as a structured entry. It stands in for a message
/// broker (spec decision D8), and it is the one handler for every sale event: later events add an interface here.
/// </summary>
public sealed class SaleEventLogHandler : INotificationHandler<SaleCreatedEvent>
{
    private readonly ILogger<SaleEventLogHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaleEventLogHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger; Serilog writes its entries.</param>
    public SaleEventLogHandler(ILogger<SaleEventLogHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Logs a <see cref="SaleCreatedEvent"/>.
    /// </summary>
    /// <param name="notification">The event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    public Task Handle(SaleCreatedEvent notification, CancellationToken cancellationToken) => Log(notification);

    private Task Log(IDomainEvent domainEvent)
    {
        _logger.LogInformation("Sale event {EventName} published {@Event}", domainEvent.GetType().Name, domainEvent);
        return Task.CompletedTask;
    }
}
```

The Application project reaches `Microsoft.Extensions.Logging` through Domain and Common (Serilog.AspNetCore and the ASP.NET Core framework reference), so no package is added. A console probe with the API's Serilog output template wrote this line:

```
[INF] Ambev.DeveloperEvaluation.Application.Sales.SaleEventLogHandler Sale event SaleCreatedEvent published {"SaleId": "7f9c2a44-5555-4d1e-8a3b-000000000010", "SaleNumber": "S-000123", "CustomerId": "3f2b8c1e-1111-4a5b-9c2d-000000000001", "BranchId": "3f2b8c1e-2222-4a5b-9c2d-000000000002", "TotalAmount": 20.25, "ItemCount": 1, "OccurredAt": "2026-09-24T14:31:02.0000000Z", "$type": "SaleCreatedEvent"}
```

- [ ] **Step 4: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~SaleEventLogHandlerTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)` and `Build succeeded.`; `Passed:     2`; `Passed:   167`.

- [ ] **Step 5: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Application/Sales/SaleEventLogHandler.cs tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/SaleEventLogHandlerTests.cs
git commit -m "feat(sales): log published sale events" -m "SaleEventLogHandler writes each published sale event as a structured Serilog entry, Sale event {EventName} published {@Event}, in place of a message broker (spec decision D8). It handles SaleCreatedEvent; later tickets add the other sale events to the same handler. A test shows that an event published as IDomainEvent reaches it."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `2 files changed, 103 insertions(+)` and `Scan complete: 0 issue(s) found`.

---

## Phase 2 gate: ticket 04 merged and Docker running

Run this before Task 12:

```bash
git fetch origin develop:develop
git log --oneline --merges develop | grep -oE "feature/(postgres-runtime|auth-login)" | sort -u
git merge-base --is-ancestor develop HEAD && echo "feature branch contains develop"
docker version --format 'Docker server {{.Server.Version}}' 2>&1 | tail -1
docker info --format '{{.OSType}}' 2>&1 | tail -1
```

Expected: the fetch fast-forwards local `develop` to `origin/develop` (it prints nothing for `develop` when nothing new was merged), then `feature/auth-login` and `feature/postgres-runtime`, then `feature branch contains develop`, a `Docker server …` version and `linux`. If the fetch is rejected, stop and ask the user: local `develop` must only ever fast-forward.

- **If ticket 02 or 04 isn't merged** (its pull request is still open): stop. Don't push the branch or open a pull request; `feature/create-sale` keeps the Phase 1 commits. Save an ai-memory handoff (skill `ai-memory-handoff`) saying that Task 11 is done and that the next step is this gate. Tell the user that ticket 04 comes first.
- **If both are merged but the branch doesn't contain `develop`** (they were merged after Task 1): run `git rebase develop`, then `dotnet build Ambev.DeveloperEvaluation.sln` and `dotnet test tests/Ambev.DeveloperEvaluation.Unit`, and expect `Passed:   167`. Phase 1 only adds files, so the rebase applies cleanly. If it reports a conflict, run `git rebase --abort` and ask the user.
- **If Docker fails:** stop. Save a handoff saying that Task 11 is done and that the next step is Task 12 once Docker Desktop runs. Tell the user to start Docker Desktop with the WSL2 backend. Don't install or start anything yourself.

---

## Phase 2: Persistence and API (needs Docker)

### Task 12: Persist sales with EF Core and the `AddSales` migration

**Skills:** `efcore-patterns` (load it now), `testcontainers-integration-tests` (load it now), `dependency-injection-patterns`, `dotnet-best-practices`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Integration/TestData/SaleTestData.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.ORM/Mapping/ExternalIdentityMapping.cs`, `SaleConfiguration.cs`, `SaleItemConfiguration.cs`
- Create: `src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs`
- Modify: `src/Ambev.DeveloperEvaluation.ORM/DefaultContext.cs` (one `using`, the `Sales` set, the sequence)
- Modify: `src/Ambev.DeveloperEvaluation.IoC/ModuleInitializers/InfrastructureModuleInitializer.cs` (one line)
- Generate: `src/Ambev.DeveloperEvaluation.ORM/Migrations/<timestamp>_AddSales.cs` and `<timestamp>_AddSales.Designer.cs`
- Modify (generated): `src/Ambev.DeveloperEvaluation.ORM/Migrations/DefaultContextModelSnapshot.cs`

- [ ] **Step 1: Add the integration test data builder**

Create `tests/Ambev.DeveloperEvaluation.Integration/TestData/SaleTestData.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Bogus;

namespace Ambev.DeveloperEvaluation.Integration.TestData;

/// <summary>
/// Generates valid <see cref="Sale"/> aggregates with Bogus, within the column limits of the Sales tables.
/// </summary>
public static class SaleTestData
{
    private static readonly Faker Faker = new();

    /// <summary>
    /// Generates a sale that hasn't been saved yet, with lines for different products at random quantities
    /// (so across the discount tiers) and prices with cents.
    /// </summary>
    /// <param name="itemCount">The number of lines.</param>
    /// <returns>A new sale.</returns>
    public static Sale GenerateValidSale(int itemCount = 3) =>
        Sale.Create(
            $"S-{Faker.Random.Number(100000, 999999)}",
            Faker.Date.Recent().ToUniversalTime(),
            new ExternalIdentity(Guid.NewGuid(), Faker.Name.FullName()),
            new ExternalIdentity(Guid.NewGuid(), $"Filial {Faker.Address.City()}"),
            Enumerable.Range(0, itemCount).Select(_ => GenerateItem()).ToList());

    private static SaleItemData GenerateItem() =>
        new(new ExternalIdentity(Guid.NewGuid(), Faker.Commerce.ProductName()),
            Faker.Random.Int(1, 20),
            Math.Round(Faker.Random.Decimal(0.01m, 500m), 2));
}
```

The integration project keeps its own builder, as ticket 02 did for `UserTestData` (its Decision 13), rather than referencing the Unit test project.

- [ ] **Step 2: Write the failing integration tests**

Create `tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Integration.Fixtures;
using Ambev.DeveloperEvaluation.Integration.TestData;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.ORM;

/// <summary>
/// Contains integration tests for <see cref="SaleRepository"/> against the migrated PostgreSQL schema.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class SaleRepositoryTests
{
    private readonly PostgreSqlFixture _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaleRepositoryTests"/> class.
    /// </summary>
    /// <param name="database">The collection's database, injected by xUnit.</param>
    public SaleRepositoryTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    /// <summary>
    /// Tests that a saved sale reads back unchanged from a fresh context: the owned customer, branch and products,
    /// every item through the backing field, the flags, and the amounts with their cents.
    /// </summary>
    [Fact(DisplayName = "Given a new sale When it is saved Then a new context reads it back with its identities, items, flags and amounts")]
    public async Task Given_NewSale_When_Saved_Then_NewContextReadsItBackUnchanged()
    {
        // Given
        var sale = SaleTestData.GenerateValidSale();

        // When
        await using (var writeContext = _database.CreateContext())
        {
            await new SaleRepository(writeContext).CreateAsync(sale);
        }

        await using var readContext = _database.CreateContext();
        var saved = await new SaleRepository(readContext).GetByIdAsync(sale.Id);

        // Then (timestamptz keeps microseconds; DateTime keeps 100 ns ticks. The events aren't stored.)
        saved.Should().BeEquivalentTo(sale, options => options
            .Excluding(expected => expected.DomainEvents)
            .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromMicroseconds(1)))
            .WhenTypeIs<DateTime>());
    }

    /// <summary>
    /// Tests that the loaded sale and its items are tracked, so a handler can change the sale and save it.
    /// </summary>
    [Fact(DisplayName = "Given a saved sale When loading it by id Then the context tracks the sale and each of its items")]
    public async Task Given_SavedSale_When_LoadingById_Then_ContextTracksSaleAndItems()
    {
        // Given
        var sale = SaleTestData.GenerateValidSale(itemCount: 2);
        await using (var writeContext = _database.CreateContext())
        {
            await new SaleRepository(writeContext).CreateAsync(sale);
        }

        // When
        await using var readContext = _database.CreateContext();
        var saved = await new SaleRepository(readContext).GetByIdAsync(sale.Id);

        // Then
        saved.Should().NotBeNull();
        readContext.Entry(saved!).State.Should().Be(EntityState.Unchanged);
        readContext.ChangeTracker.Entries<SaleItem>().Should().HaveCount(2);
    }

    /// <summary>
    /// Tests that an unknown id finds nothing.
    /// </summary>
    [Fact(DisplayName = "Given no sale with the id When loading it by id Then it returns null")]
    public async Task Given_NoSaleWithId_When_LoadingById_Then_ReturnsNull()
    {
        // Given
        await using var context = _database.CreateContext();

        // When
        var saved = await new SaleRepository(context).GetByIdAsync(Guid.NewGuid());

        // Then
        saved.Should().BeNull();
    }
}
```

Notes for reviewers:
- FluentAssertions compares the owned `ExternalIdentity` records by value, and compares decimals by value too: `10m` equals the `10.00m` that `numeric(5,2)` gives back.
- The random prices have cents and the random quantities cross the discount tiers, so a wrong precision or a lost item fails the first test.
- The sale numbers are six random digits. A collision between two sales of one test class is about 1 in 900,000, and the reset clears the table between classes.

- [ ] **Step 3: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Integration 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected:

```
SaleRepositoryTests.cs(41,23): error CS0246: The type or namespace name 'SaleRepository' could not be found (are you missing a using directive or an assembly reference?)
SaleRepositoryTests.cs(45,31): error CS0246: The type or namespace name 'SaleRepository' could not be found (are you missing a using directive or an assembly reference?)
SaleRepositoryTests.cs(64,23): error CS0246: The type or namespace name 'SaleRepository' could not be found (are you missing a using directive or an assembly reference?)
SaleRepositoryTests.cs(69,31): error CS0246: The type or namespace name 'SaleRepository' could not be found (are you missing a using directive or an assembly reference?)
SaleRepositoryTests.cs(87,31): error CS0246: The type or namespace name 'SaleRepository' could not be found (are you missing a using directive or an assembly reference?)
```

- [ ] **Step 4: Write the mappings**

Create `src/Ambev.DeveloperEvaluation.ORM/Mapping/ExternalIdentityMapping.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

/// <summary>
/// Maps an <see cref="ExternalIdentity"/> as an owned type into two columns of its owner's table
/// (spec decision D2): an id and a copied name, with no foreign key.
/// </summary>
internal static class ExternalIdentityMapping
{
    /// <summary>
    /// Maps the identity to the columns <c>{prefix}Id</c> (<c>uuid</c>) and <c>{prefix}Name</c> (<c>varchar(100)</c>).
    /// </summary>
    /// <typeparam name="TOwner">The entity that owns the identity.</typeparam>
    /// <param name="identity">The owned navigation to configure.</param>
    /// <param name="prefix">The column prefix, such as <c>Customer</c>.</param>
    public static void Map<TOwner>(OwnedNavigationBuilder<TOwner, ExternalIdentity> identity, string prefix)
        where TOwner : class
    {
        identity.Property(value => value.Id).HasColumnName($"{prefix}Id");
        identity.Property(value => value.Name)
            .HasColumnName($"{prefix}Name")
            .IsRequired()
            .HasMaxLength(ExternalIdentity.NameMaxLength);
    }
}
```

The `Microsoft.EntityFrameworkCore` using matters: `HasColumnName` is a relational extension method in that namespace, and without it the build fails with CS1061 (rehearsed).

Create `src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleConfiguration.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

/// <summary>
/// Maps <see cref="Sale"/> to the <c>Sales</c> table (spec §8.1 and §8.2).
/// </summary>
public sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    /// <summary>
    /// The PostgreSQL sequence that sale numbers are generated from (spec §8.3).
    /// </summary>
    public const string SaleNumberSequence = "sale_number_seq";

    /// <summary>
    /// The shadow property that Npgsql maps to PostgreSQL's <c>xmin</c> system column, the concurrency token
    /// (spec decision D9). A shadow property keeps the domain free of persistence details.
    /// </summary>
    private const string RowVersion = "Version";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");

        // The domain creates the ids. Without ValueGeneratedNever, EF Core would treat a new item that already
        // has a key as an existing row and send an UPDATE.
        builder.HasKey(sale => sale.Id);
        builder.Property(sale => sale.Id).ValueGeneratedNever();

        // Unique across every row, soft-deleted ones included.
        builder.Property(sale => sale.SaleNumber).IsRequired().HasMaxLength(Sale.SaleNumberMaxLength);
        builder.HasIndex(sale => sale.SaleNumber).IsUnique();

        builder.HasIndex(sale => sale.SaleDate);

        builder.OwnsOne(sale => sale.Customer, customer =>
        {
            ExternalIdentityMapping.Map(customer, "Customer");
            customer.HasIndex(identity => identity.Id);
        });
        builder.Navigation(sale => sale.Customer).IsRequired();

        builder.OwnsOne(sale => sale.Branch, branch =>
        {
            ExternalIdentityMapping.Map(branch, "Branch");
            branch.HasIndex(identity => identity.Id);
        });
        builder.Navigation(sale => sale.Branch).IsRequired();

        builder.Property(sale => sale.TotalAmount).HasPrecision(18, 2);

        builder.HasMany(sale => sale.Items).WithOne().HasForeignKey("SaleId").IsRequired();
        builder.Navigation(sale => sale.Items).HasField("_items").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(sale => sale.DomainEvents);

        builder.Property<uint>(RowVersion).IsRowVersion();
    }
}
```

Create `src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleItemConfiguration.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

/// <summary>
/// Maps <see cref="SaleItem"/> to the <c>SaleItems</c> table (spec §8.1 and §8.2). <see cref="SaleConfiguration"/>
/// maps the relationship: a <c>SaleId</c> foreign key, indexed.
/// </summary>
public sealed class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("SaleItems");

        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();

        builder.OwnsOne(item => item.Product, product => ExternalIdentityMapping.Map(product, "Product"));
        builder.Navigation(item => item.Product).IsRequired();

        builder.Property(item => item.UnitPrice).HasPrecision(18, 2);
        builder.Property(item => item.DiscountPercentage).HasPrecision(5, 2);
        builder.Property(item => item.DiscountAmount).HasPrecision(18, 2);
        builder.Property(item => item.TotalAmount).HasPrecision(18, 2);
    }
}
```

`DefaultContext` applies every configuration in the ORM assembly (`ApplyConfigurationsFromAssembly`), so both are picked up.

- [ ] **Step 5: Add the `Sales` set and the sequence to the context**

With the Edit tool (the file has a BOM and CRLF endings), in `src/Ambev.DeveloperEvaluation.ORM/DefaultContext.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
```

with

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.ORM.Mapping;
using Microsoft.EntityFrameworkCore;
```

Then replace

```csharp
    public DbSet<User> Users { get; set; }
```

with

```csharp
    public DbSet<User> Users { get; set; }

    public DbSet<Sale> Sales { get; set; }
```

Then replace

```csharp
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
```

with

```csharp
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.HasSequence<long>(SaleConfiguration.SaleNumberSequence);
```

The new property follows the template's style next to `Users`, with no XML doc. EF Core's analyzer suppresses the nullable warning for `DbSet` properties.

- [ ] **Step 6: Write the repository and register it**

Create `src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

/// <summary>
/// Implementation of <see cref="ISaleRepository"/> using Entity Framework Core.
/// </summary>
public sealed class SaleRepository : ISaleRepository
{
    private readonly DefaultContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaleRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public SaleRepository(DefaultContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task CreateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        _context.Sales.Add(sale);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Sales
            .Include(sale => sale.Items)
            .FirstOrDefaultAsync(sale => sale.Id == id, cancellationToken);
}
```

With the Edit tool, in `src/Ambev.DeveloperEvaluation.IoC/ModuleInitializers/InfrastructureModuleInitializer.cs`, replace

```csharp
        builder.Services.AddScoped<IUserRepository, UserRepository>();
```

with

```csharp
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<ISaleRepository, SaleRepository>();
```

The file already has `using Ambev.DeveloperEvaluation.Domain.Repositories;` and `using Ambev.DeveloperEvaluation.ORM.Repositories;`.

- [ ] **Step 7: Build, and watch the model get ahead of the migrations (RED)**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at '; echo "exit=${PIPESTATUS[0]}"
```

Expected: `0 Error(s)` and `Build succeeded.`; then `Build started...`, `Build succeeded.`, `Changes have been made to the model since the last migration. Add a new migration.` and `exit=1`. EF Core builds the model without complaint, so every mapping is valid.

- [ ] **Step 8: Generate the migration with the documented command**

This is ticket 02's command, run from the repo root:

```bash
dotnet ef migrations add AddSales --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at ' | tail -1
git status --short
```

Expected: `Done. To undo this action, use 'ef migrations remove'`, then:

```
 M src/Ambev.DeveloperEvaluation.IoC/ModuleInitializers/InfrastructureModuleInitializer.cs
 M src/Ambev.DeveloperEvaluation.ORM/DefaultContext.cs
 M src/Ambev.DeveloperEvaluation.ORM/Migrations/DefaultContextModelSnapshot.cs
?? docs/superpowers/tickets/
?? src/Ambev.DeveloperEvaluation.ORM/Mapping/ExternalIdentityMapping.cs
?? src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleConfiguration.cs
?? src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleItemConfiguration.cs
?? src/Ambev.DeveloperEvaluation.ORM/Migrations/<timestamp>_AddSales.Designer.cs
?? src/Ambev.DeveloperEvaluation.ORM/Migrations/<timestamp>_AddSales.cs
?? src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs
?? tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryTests.cs
?? tests/Ambev.DeveloperEvaluation.Integration/TestData/SaleTestData.cs
```

- [ ] **Step 9: Check what was generated (don't edit it)**

```bash
grep -cE 'CreateSequence|CreateTable|CreateIndex|DropTable|DropSequence|AddColumn|Users' src/Ambev.DeveloperEvaluation.ORM/Migrations/*_AddSales.cs
grep -nE 'xmin|unique: true|onDelete' src/Ambev.DeveloperEvaluation.ORM/Migrations/*_AddSales.cs | cut -d: -f2- | sed 's/^ *//'
dotnet ef migrations script AddUserTimestamps AddSales --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at |^Build ' > /tmp/ticket05-checks/add-sales.sql
cat /tmp/ticket05-checks/add-sales.sql
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at ' | tail -1; echo "exit=${PIPESTATUS[0]}"
```

Expected:
- `11`: one sequence, two tables, five indexes, and the `Down` drops, with no `AddColumn` and no mention of `Users`. So the migration holds only the Sales schema: ticket 02 already added the `Users` columns.
- The three lines

  ```
  xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
  onDelete: ReferentialAction.Cascade);
  unique: true);
  ```

- The SQL, exactly §8.1's schema. The migration code lists `xmin`, but Npgsql creates no column for a system column:

  ```sql
  START TRANSACTION;

  CREATE SEQUENCE sale_number_seq START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE NO CYCLE;

  CREATE TABLE "Sales" (
      "Id" uuid NOT NULL,
      "SaleNumber" character varying(50) NOT NULL,
      "SaleDate" timestamp with time zone NOT NULL,
      "CustomerId" uuid NOT NULL,
      "CustomerName" character varying(100) NOT NULL,
      "BranchId" uuid NOT NULL,
      "BranchName" character varying(100) NOT NULL,
      "TotalAmount" numeric(18,2) NOT NULL,
      "IsCancelled" boolean NOT NULL,
      "IsDeleted" boolean NOT NULL,
      "DeletedAt" timestamp with time zone,
      "CreatedAt" timestamp with time zone NOT NULL,
      "UpdatedAt" timestamp with time zone,
      CONSTRAINT "PK_Sales" PRIMARY KEY ("Id")
  );

  CREATE TABLE "SaleItems" (
      "Id" uuid NOT NULL,
      "ProductId" uuid NOT NULL,
      "ProductName" character varying(100) NOT NULL,
      "Quantity" integer NOT NULL,
      "UnitPrice" numeric(18,2) NOT NULL,
      "DiscountPercentage" numeric(5,2) NOT NULL,
      "DiscountAmount" numeric(18,2) NOT NULL,
      "TotalAmount" numeric(18,2) NOT NULL,
      "IsCancelled" boolean NOT NULL,
      "SaleId" uuid NOT NULL,
      CONSTRAINT "PK_SaleItems" PRIMARY KEY ("Id"),
      CONSTRAINT "FK_SaleItems_Sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sales" ("Id") ON DELETE CASCADE
  );

  CREATE INDEX "IX_SaleItems_SaleId" ON "SaleItems" ("SaleId");

  CREATE INDEX "IX_Sales_BranchId" ON "Sales" ("BranchId");

  CREATE INDEX "IX_Sales_CustomerId" ON "Sales" ("CustomerId");

  CREATE INDEX "IX_Sales_SaleDate" ON "Sales" ("SaleDate");

  CREATE UNIQUE INDEX "IX_Sales_SaleNumber" ON "Sales" ("SaleNumber");

  INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
  VALUES ('<timestamp>_AddSales', '8.0.10');

  COMMIT;
  ```

- `No changes have been made to the model since the last migration.` and `exit=0`.

- [ ] **Step 10: Run the integration tests (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Integration 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build --list-tests 2>&1 | grep -E '^\s+Given' | grep -i sale
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected: `0 Error(s)` and `Build succeeded.`; the three new tests:

```
    Given a new sale When it is saved Then a new context reads it back with its identities, items, flags and amounts
    Given a saved sale When loading it by id Then the context tracks the sale and each of its items
    Given no sale with the id When loading it by id Then it returns null
```

and **(derived)** `Passed!  - Failed:     0, Passed:     7, Skipped:     0, Total:     7`. The container applies all three migrations, so ticket 02's `MigrationTests` passes with `AddSales` too.

**(probe)** What the probe showed, without PostgreSQL:
- In the Npgsql model, `Version` maps to column `xmin` of type `xid`: a shadow concurrency token, generated on add and update. Both ids are `ValueGenerated=Never`, `Items` uses the field `_items` with field access, `Customer` is a required dependent, and `DomainEvents` isn't mapped.
- Adding a new sale tracks the sale, its items and all their owned identities as `Added`.
- The by-id query is one `SELECT` of the sale columns, `xmin` and the item columns, `FROM "Sales" AS s LEFT JOIN "SaleItems" AS s0 ON s."Id" = s0."SaleId" WHERE s."Id" = @__id_0 ORDER BY s."Id"`.
- On EF Core InMemory, a sale saved through `SaleRepository` came back in a new context with equal owned identities, the same item ids, amounts and totals, no events, and the state `Unchanged`. An unknown id gave `null`.

If Docker isn't reachable here (`Docker is either not running or misconfigured`), go back to the Phase 2 gate.

- [ ] **Step 11: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.ORM/Mapping/ExternalIdentityMapping.cs src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleConfiguration.cs src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleItemConfiguration.cs src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs src/Ambev.DeveloperEvaluation.ORM/DefaultContext.cs src/Ambev.DeveloperEvaluation.ORM/Migrations/*_AddSales.cs src/Ambev.DeveloperEvaluation.ORM/Migrations/*_AddSales.Designer.cs src/Ambev.DeveloperEvaluation.ORM/Migrations/DefaultContextModelSnapshot.cs src/Ambev.DeveloperEvaluation.IoC/ModuleInitializers/InfrastructureModuleInitializer.cs tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryTests.cs tests/Ambev.DeveloperEvaluation.Integration/TestData/SaleTestData.cs
git commit -m "feat(sales): persist sales with EF Core and the AddSales migration" -m "Sales and SaleItems map as spec 8.1 and 8.2 describe: external identities as owned types in the parent table, domain-created ids with ValueGeneratedNever, Items through its backing field, DomainEvents not mapped, numeric(18,2) and numeric(5,2) amounts, and a shadow uint row version that Npgsql maps to xmin. The schema has a unique SaleNumber index over every row, indexes on SaleDate, CustomerId, BranchId and SaleId, and the sale_number_seq sequence. The AddSales migration creates the whole Sales schema, including what later tickets use, so they add no migrations. SaleRepository implements CreateAsync and GetByIdAsync (tracked, with items) and is registered in IoC."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `11 files changed, …` (`11 files changed, 840 insertions(+)`; the generated files' sizes depend on nothing but the model) and `Scan complete: 0 issue(s) found`.

---

### Task 13: Reset the Sales tables and `sale_number_seq` between test classes

**Skill:** `testcontainers-integration-tests`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Integration/Fixtures/DataResetTests.cs` (two `using` lines, a constant; the sequence test replaced, a Sales test added)
- Modify: `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/DataResetTests.cs` (one `using`, a Sales test added)
- Modify: `tests/Ambev.DeveloperEvaluation.Integration/Fixtures/DataResetFixture.cs`, `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/DataResetFixture.cs` (the two lists)

- [ ] **Step 1: Add the failing integration tests**

Ticket 02's sequence test created and dropped its own `reset_probe_seq`, "because no migration creates one yet". Now `sale_number_seq` exists, so the test uses it through `DataResetFixture.Sequences`.

With the Edit tool, in `tests/Ambev.DeveloperEvaluation.Integration/Fixtures/DataResetTests.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Integration.TestData;
using Ambev.DeveloperEvaluation.ORM.Repositories;
```

with

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Integration.TestData;
using Ambev.DeveloperEvaluation.ORM.Mapping;
using Ambev.DeveloperEvaluation.ORM.Repositories;
```

Then replace

```csharp
public sealed class DataResetTests
{
    private readonly PostgreSqlFixture _database;
```

with

```csharp
public sealed class DataResetTests
{
    private const string SaleNumberSequence = SaleConfiguration.SaleNumberSequence;

    private readonly PostgreSqlFixture _database;
```

Then replace

```csharp
    /// <summary>
    /// Tests that the reset restarts the listed sequences. The test creates and drops its own sequence,
    /// because no migration creates one yet.
    /// </summary>
    [Fact(DisplayName = "Given an advanced sequence When the data is reset Then the sequence starts over at 1")]
    public async Task Given_AdvancedSequence_When_DataIsReset_Then_SequenceStartsOverAtOne()
    {
        // Given
        const string sequence = "reset_probe_seq";
        await ExecuteAsync($"CREATE SEQUENCE {sequence};");
        await ExecuteAsync($"SELECT nextval('{sequence}'); SELECT nextval('{sequence}');");

        try
        {
            // When
            await _database.ResetDataAsync([], [sequence]);

            // Then
            (await NextValueAsync(sequence)).Should().Be(1);
        }
        finally
        {
            await ExecuteAsync($"DROP SEQUENCE {sequence};");
        }
    }
```

with

```csharp
    /// <summary>
    /// Tests that the reset empties the Sales tables too, items included.
    /// </summary>
    [Fact(DisplayName = "Given a saved sale When the data is reset Then the Sales and SaleItems tables are empty")]
    public async Task Given_SavedSale_When_DataIsReset_Then_SalesTablesAreEmpty()
    {
        // Given
        await using (var context = _database.CreateContext())
        {
            await new SaleRepository(context).CreateAsync(SaleTestData.GenerateValidSale());
        }

        // When
        await _database.ResetDataAsync(DataResetFixture.Tables, DataResetFixture.Sequences);

        // Then
        await using var readContext = _database.CreateContext();
        (await readContext.Sales.CountAsync()).Should().Be(0);
        (await readContext.Set<SaleItem>().CountAsync()).Should().Be(0);
    }

    /// <summary>
    /// Tests that the reset restarts the listed sequences, so sale numbers start over in every test class.
    /// </summary>
    [Fact(DisplayName = "Given an advanced sale_number_seq When the data is reset Then the sequence starts over at 1")]
    public async Task Given_AdvancedSaleNumberSequence_When_DataIsReset_Then_SequenceStartsOverAtOne()
    {
        // Given
        await ExecuteAsync($"SELECT nextval('{SaleNumberSequence}'); SELECT nextval('{SaleNumberSequence}');");

        // When
        await _database.ResetDataAsync(DataResetFixture.Tables, DataResetFixture.Sequences);

        // Then
        (await NextValueAsync(SaleNumberSequence)).Should().Be(1);
    }
```

The helpers `ExecuteAsync` and `NextValueAsync` at the end of the file stay as ticket 02 wrote them.

- [ ] **Step 2: Add the failing functional test**

With the Edit tool, in `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/DataResetTests.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.ORM;
```

with

```csharp
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Ambev.DeveloperEvaluation.ORM;
```

Then replace

```csharp
        // Then
        await using var readScope = _api.Services.CreateAsyncScope();
        var context = readScope.ServiceProvider.GetRequiredService<DefaultContext>();
        (await context.Users.CountAsync()).Should().Be(0);
    }
}
```

with

```csharp
        // Then
        await using var readScope = _api.Services.CreateAsyncScope();
        var context = readScope.ServiceProvider.GetRequiredService<DefaultContext>();
        (await context.Users.CountAsync()).Should().Be(0);
    }

    /// <summary>
    /// Tests that the reset empties the Sales tables too, items included.
    /// </summary>
    [Fact(DisplayName = "Given a sale saved through the API's services When the data is reset Then the Sales and SaleItems tables are empty")]
    public async Task Given_SavedSale_When_DataIsReset_Then_SalesTablesAreEmpty()
    {
        // Given
        await using (var scope = _api.Services.CreateAsyncScope())
        {
            var sales = scope.ServiceProvider.GetRequiredService<ISaleRepository>();
            await sales.CreateAsync(Sale.Create(
                "S-RESET-PROBE",
                DateTime.UtcNow,
                new ExternalIdentity(Guid.NewGuid(), "Reset Probe"),
                new ExternalIdentity(Guid.NewGuid(), "Filial Probe"),
                [new SaleItemData(new ExternalIdentity(Guid.NewGuid(), "Probe Product"), 1, 1.00m)]));
        }

        // When
        await _api.ResetDataAsync(DataResetFixture.Tables, DataResetFixture.Sequences);

        // Then
        await using var readScope = _api.Services.CreateAsyncScope();
        var context = readScope.ServiceProvider.GetRequiredService<DefaultContext>();
        (await context.Sales.CountAsync()).Should().Be(0);
        (await context.Set<SaleItem>().CountAsync()).Should().Be(0);
    }
}
```

The file already imports `Ambev.DeveloperEvaluation.Domain.Entities` for `User`, which also covers `Sale` and `SaleItem`. The fixed sale number is safe: the reset right after it empties the table.

- [ ] **Step 3: Run them and watch them fail (RED)**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build --filter "FullyQualifiedName~DataResetTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~DataResetTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected: `0 Error(s)` and `Build succeeded.`, then **(derived)**:

```
   Expected (await readContext.Sales.CountAsync()) to be 0, but found 1.
   Expected (await NextValueAsync(SaleNumberSequence)) to be 1L, but found 3L.
Failed!  - Failed:     2, Passed:     1, Skipped:     0, Total:     3, …
   Expected (await context.Sales.CountAsync()) to be 0, but found 1.
Failed!  - Failed:     1, Passed:     1, Skipped:     0, Total:     2, …
```

The reset still truncates `Users` only and restarts no sequence.

- [ ] **Step 4: Add the Sales tables and the sequence to both reset lists**

With the Edit tool, in **both** `tests/Ambev.DeveloperEvaluation.Integration/Fixtures/DataResetFixture.cs` and `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/DataResetFixture.cs`, replace

```csharp
    public static readonly IReadOnlyCollection<string> Tables = ["Users"];
```

with

```csharp
    public static readonly IReadOnlyCollection<string> Tables = ["Users", "Sales", "SaleItems"];
```

and replace

```csharp
    public static readonly IReadOnlyCollection<string> Sequences = [];
```

with

```csharp
    public static readonly IReadOnlyCollection<string> Sequences = ["sale_number_seq"];
```

The reset runs one `TRUNCATE TABLE "Users", "Sales", "SaleItems";`. PostgreSQL accepts the foreign key from `SaleItems` to `Sales` because both tables are in the same statement.

- [ ] **Step 5: Verify (GREEN)**

```bash
git diff --stat | cat
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `4 files changed, 66 insertions(+), 22 deletions(-)`; `Build succeeded.`; **(derived)** `Passed:     8` for Integration and `Passed:    13` for Functional.

- [ ] **Step 6: Commit**

```bash
git add tests/Ambev.DeveloperEvaluation.Integration/Fixtures/DataResetTests.cs tests/Ambev.DeveloperEvaluation.Integration/Fixtures/DataResetFixture.cs tests/Ambev.DeveloperEvaluation.Functional/Fixtures/DataResetTests.cs tests/Ambev.DeveloperEvaluation.Functional/Fixtures/DataResetFixture.cs
git commit -m "test: reset the Sales tables and sale_number_seq between test classes" -m "Both data resets now also truncate Sales and SaleItems and restart sale_number_seq, as ticket 02 left for this ticket. The integration sequence test uses the real sequence instead of a throwaway one."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `4 files changed, 66 insertions(+), 22 deletions(-)` and `Scan complete: 0 issue(s) found`.

---

### Task 14: Create and get sales over HTTP

**Skills:** `dotnet-best-practices`, `testcontainers-integration-tests`, `type-design-performance`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesMappingTests.cs`, `SalesControllerTests.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Functional/TestData/SaleRequestBody.cs`, `SaleResponseBody.cs`, `SaleRequestBodyTestData.cs`
- Modify (full rewrite): `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiHttpExtensions.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Functional/Sales/SaleJson.cs`, `CreateSaleTests.cs`, `GetSaleTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SaleItemRequest.cs`, `SaleResponse.cs`, `SaleItemResponse.cs`, `SaleContractProfile.cs`, `SalesController.cs`
- Create: `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/CreateSale/CreateSaleRequest.cs`, `CreateSaleProfile.cs`

- [ ] **Step 1: Write the failing unit tests**

Create `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesMappingTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using AutoMapper;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Sales;

/// <summary>
/// Contains unit tests for the WebApi Sales profiles, <see cref="CreateSaleProfile"/> and <see cref="SaleContractProfile"/>.
/// The configuration holds only the Sales profiles: the API's whole configuration can't be validated,
/// because of the template's duplicate <c>CreateUserRequest</c> map (spec §9.2).
/// </summary>
public sealed class SalesMappingTests
{
    private readonly MapperConfiguration _configuration = new(config =>
    {
        config.AddProfile<CreateSaleProfile>();
        config.AddProfile<SaleContractProfile>();
    });

    /// <summary>
    /// Tests that every member of the commands and responses has a source.
    /// </summary>
    [Fact(DisplayName = "Given the WebApi Sales profiles When validating their configuration Then every command and response member is mapped")]
    public void Given_WebApiSalesProfiles_When_ValidatingConfiguration_Then_EveryMemberIsMapped()
    {
        // When
        var act = () => _configuration.AssertConfigurationIsValid();

        // Then
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that the create request becomes a command with the same header and lines.
    /// </summary>
    [Fact(DisplayName = "Given a create-sale request When mapping it to CreateSaleCommand Then the header and every line are copied")]
    public void Given_CreateSaleRequest_When_MappingToCommand_Then_HeaderAndLinesAreCopied()
    {
        // Given
        var request = new CreateSaleRequest
        {
            SaleNumber = "S-000123",
            SaleDate = new DateTime(2026, 9, 24, 14, 30, 0, DateTimeKind.Utc),
            CustomerId = Guid.NewGuid(),
            CustomerName = "Maria Silva",
            BranchId = Guid.NewGuid(),
            BranchName = "Filial Centro",
            Items =
            [
                new SaleItemRequest { ProductId = Guid.NewGuid(), ProductName = "Cerveja 350ml", Quantity = 5, UnitPrice = 4.50m },
                new SaleItemRequest { ProductId = Guid.NewGuid(), ProductName = "Refrigerante 2L", Quantity = 1, UnitPrice = 9.90m }
            ]
        };

        // When
        var command = _configuration.CreateMapper().Map<CreateSaleCommand>(request);

        // Then
        command.Should().BeEquivalentTo(request, options => options.WithStrictOrdering());
    }

    /// <summary>
    /// Tests that the response copies every field of the result, lines included.
    /// </summary>
    [Fact(DisplayName = "Given a SaleResult When mapping it to SaleResponse Then every field and line is copied")]
    public void Given_SaleResult_When_MappingToSaleResponse_Then_EveryFieldIsCopied()
    {
        // Given
        var result = new SaleResult
        {
            Id = Guid.NewGuid(),
            SaleNumber = "S-000123",
            SaleDate = new DateTime(2026, 9, 24, 14, 30, 0, DateTimeKind.Utc),
            CustomerId = Guid.NewGuid(),
            CustomerName = "Maria Silva",
            BranchId = Guid.NewGuid(),
            BranchName = "Filial Centro",
            TotalAmount = 20.25m,
            CreatedAt = new DateTime(2026, 9, 24, 14, 31, 2, DateTimeKind.Utc),
            Items =
            [
                new SaleItemResult
                {
                    Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductName = "Cerveja 350ml", Quantity = 5,
                    UnitPrice = 4.50m, DiscountPercentage = 10m, DiscountAmount = 2.25m, TotalAmount = 20.25m
                }
            ]
        };

        // When
        var response = _configuration.CreateMapper().Map<SaleResponse>(result);

        // Then
        response.Should().BeEquivalentTo(result);
    }
}
```

Create `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesControllerTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using AutoMapper;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Sales;

/// <summary>
/// Contains unit tests for <see cref="SalesController"/>. The mapper holds the real WebApi Sales profiles,
/// and each body is serialized the way MVC does, so the tests check the exact JSON a client gets.
/// </summary>
public sealed class SalesControllerTests
{
    /// <summary>
    /// The sale of the spec's example (§7.2), as the body of every success response carries it.
    /// </summary>
    private const string ExampleSaleJson =
        """{"id":"7f9c2a44-5555-4d1e-8a3b-000000000010","saleNumber":"S-000123","saleDate":"2026-09-24T14:30:00Z","customerId":"3f2b8c1e-1111-4a5b-9c2d-000000000001","customerName":"Maria Silva","branchId":"3f2b8c1e-2222-4a5b-9c2d-000000000002","branchName":"Filial Centro","totalAmount":20.25,"isCancelled":false,"createdAt":"2026-09-24T14:31:02Z","updatedAt":null,"items":[{"id":"7f9c2a44-6666-4d1e-8a3b-000000000011","productId":"3f2b8c1e-3333-4a5b-9c2d-000000000003","productName":"Cerveja 350ml","quantity":5,"unitPrice":4.50,"discountPercentage":10,"discountAmount":2.25,"totalAmount":20.25,"isCancelled":false}]}""";

    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly SalesController _controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="SalesControllerTests"/> class.
    /// </summary>
    public SalesControllerTests()
    {
        var mapper = new MapperConfiguration(config =>
        {
            config.AddProfile<CreateSaleProfile>();
            config.AddProfile<SaleContractProfile>();
        }).CreateMapper();
        _controller = new SalesController(_mediator, mapper);
    }

    /// <summary>
    /// Tests that creating a sale sends the command built from the request, and returns 201 pointing at
    /// <c>GetSale</c> with the sale in the envelope, built once.
    /// </summary>
    [Fact(DisplayName = "Given a create-sale request When creating the sale Then it sends the command and returns 201 at GetSale with the sale")]
    public async Task Given_CreateSaleRequest_When_CreatingSale_Then_Returns201AtGetSaleWithSale()
    {
        // Given
        var request = new CreateSaleRequest
        {
            SaleNumber = "S-000123",
            SaleDate = new DateTime(2026, 9, 24, 14, 30, 0, DateTimeKind.Utc),
            CustomerId = Guid.Parse("3f2b8c1e-1111-4a5b-9c2d-000000000001"),
            CustomerName = "Maria Silva",
            BranchId = Guid.Parse("3f2b8c1e-2222-4a5b-9c2d-000000000002"),
            BranchName = "Filial Centro",
            Items = [new SaleItemRequest { ProductId = Guid.Parse("3f2b8c1e-3333-4a5b-9c2d-000000000003"), ProductName = "Cerveja 350ml", Quantity = 5, UnitPrice = 4.50m }]
        };
        CreateSaleCommand? sent = null;
        _mediator.Send(Arg.Do<CreateSaleCommand>(command => sent = command), Arg.Any<CancellationToken>())
            .Returns(ExampleResult());

        // When
        var response = await _controller.CreateSale(request, CancellationToken.None);

        // Then
        sent.Should().BeEquivalentTo(request);
        var created = response.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(SalesController.GetSale));
        created.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(ExampleResult().Id);
        MvcJson.Serialize(created.Value).Should().Be(
            $$"""{"success":true,"message":"Sale created successfully","data":{{ExampleSaleJson}}}""");
    }

    /// <summary>
    /// Tests that getting a sale sends the query for the route id and returns 200 with the sale in the envelope.
    /// </summary>
    [Fact(DisplayName = "Given a sale id When getting the sale Then it sends the query and returns 200 with the sale")]
    public async Task Given_SaleId_When_GettingSale_Then_Returns200WithSale()
    {
        // Given
        var result = ExampleResult();
        _mediator.Send(new GetSaleQuery(result.Id), Arg.Any<CancellationToken>()).Returns(result);

        // When
        var response = await _controller.GetSale(result.Id, CancellationToken.None);

        // Then
        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        MvcJson.Serialize(ok.Value).Should().Be(
            $$"""{"success":true,"message":"Sale retrieved successfully","data":{{ExampleSaleJson}}}""");
    }

    private static SaleResult ExampleResult() => new()
    {
        Id = Guid.Parse("7f9c2a44-5555-4d1e-8a3b-000000000010"),
        SaleNumber = "S-000123",
        SaleDate = new DateTime(2026, 9, 24, 14, 30, 0, DateTimeKind.Utc),
        CustomerId = Guid.Parse("3f2b8c1e-1111-4a5b-9c2d-000000000001"),
        CustomerName = "Maria Silva",
        BranchId = Guid.Parse("3f2b8c1e-2222-4a5b-9c2d-000000000002"),
        BranchName = "Filial Centro",
        TotalAmount = 20.25m,
        IsCancelled = false,
        CreatedAt = new DateTime(2026, 9, 24, 14, 31, 2, DateTimeKind.Utc),
        UpdatedAt = null,
        Items =
        [
            new SaleItemResult
            {
                Id = Guid.Parse("7f9c2a44-6666-4d1e-8a3b-000000000011"),
                ProductId = Guid.Parse("3f2b8c1e-3333-4a5b-9c2d-000000000003"),
                ProductName = "Cerveja 350ml",
                Quantity = 5,
                UnitPrice = 4.50m,
                DiscountPercentage = 10m,
                DiscountAmount = 2.25m,
                TotalAmount = 20.25m,
                IsCancelled = false
            }
        ]
    };
}
```

Notes for reviewers:
- The expected body is spec §7.2's example, compacted. Comparing it whole proves the envelope is built once, the field names and their order, and that there's no `isDeleted` or `deletedAt`.
- The raw strings use `$$` and end in `}}}"""`: the hole closes with `}}`, and the last `}` is literal. That's allowed, unlike a literal `}}`, which ticket 04's Decision 8 hit.
- `new GetSaleQuery(result.Id)` matches the controller's query by record equality.

- [ ] **Step 2: Build and watch the unit tests fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected:

```
SalesControllerTests.cs(28,22): error CS0246: The type or namespace name 'SalesController' could not be found (are you missing a using directive or an assembly reference?)
SalesControllerTests.cs(4,49): error CS0234: The type or namespace name 'Sales' does not exist in the namespace 'Ambev.DeveloperEvaluation.WebApi.Features' (are you missing an assembly reference?)
SalesControllerTests.cs(5,49): error CS0234: The type or namespace name 'Sales' does not exist in the namespace 'Ambev.DeveloperEvaluation.WebApi.Features' (are you missing an assembly reference?)
SalesMappingTests.cs(3,49): error CS0234: The type or namespace name 'Sales' does not exist in the namespace 'Ambev.DeveloperEvaluation.WebApi.Features' (are you missing an assembly reference?)
SalesMappingTests.cs(4,49): error CS0234: The type or namespace name 'Sales' does not exist in the namespace 'Ambev.DeveloperEvaluation.WebApi.Features' (are you missing an assembly reference?)
```

- [ ] **Step 3: Add the functional test data**

Create `tests/Ambev.DeveloperEvaluation.Functional/TestData/SaleRequestBody.cs`:

```csharp
namespace Ambev.DeveloperEvaluation.Functional.TestData;

/// <summary>
/// The JSON body of <c>POST /api/sales</c> (spec §7.2), as a client writes it.
/// </summary>
/// <param name="SaleNumber">The sale number.</param>
/// <param name="SaleDate">The date and time of the sale.</param>
/// <param name="CustomerId">The id of the customer.</param>
/// <param name="CustomerName">The customer's name.</param>
/// <param name="BranchId">The id of the branch.</param>
/// <param name="BranchName">The branch's name.</param>
/// <param name="Items">The lines of the sale.</param>
public sealed record SaleRequestBody(
    string SaleNumber,
    DateTime SaleDate,
    Guid CustomerId,
    string CustomerName,
    Guid BranchId,
    string BranchName,
    IReadOnlyList<SaleItemRequestBody> Items);

/// <summary>
/// One line of a <see cref="SaleRequestBody"/>. Like the API contract, it has no discount fields.
/// </summary>
/// <param name="ProductId">The id of the product.</param>
/// <param name="ProductName">The product's name.</param>
/// <param name="Quantity">The quantity of identical items.</param>
/// <param name="UnitPrice">The price of one item.</param>
public sealed record SaleItemRequestBody(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);
```

Create `tests/Ambev.DeveloperEvaluation.Functional/TestData/SaleResponseBody.cs`:

```csharp
namespace Ambev.DeveloperEvaluation.Functional.TestData;

/// <summary>
/// A sale in the <c>data</c> of a Sales API success response (spec §7.2), read back for assertions.
/// </summary>
/// <param name="Id">The id of the sale.</param>
/// <param name="SaleNumber">The sale number.</param>
/// <param name="SaleDate">The date and time of the sale, in UTC.</param>
/// <param name="CustomerId">The id of the customer.</param>
/// <param name="CustomerName">The customer's name.</param>
/// <param name="BranchId">The id of the branch.</param>
/// <param name="BranchName">The branch's name.</param>
/// <param name="TotalAmount">The sale total.</param>
/// <param name="IsCancelled">Whether the sale was cancelled.</param>
/// <param name="CreatedAt">When the sale was created, in UTC.</param>
/// <param name="UpdatedAt">When the sale was last changed, in UTC, if ever.</param>
/// <param name="Items">The lines of the sale.</param>
public sealed record SaleResponseBody(
    Guid Id,
    string SaleNumber,
    DateTime SaleDate,
    Guid CustomerId,
    string CustomerName,
    Guid BranchId,
    string BranchName,
    decimal TotalAmount,
    bool IsCancelled,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<SaleItemResponseBody> Items);

/// <summary>
/// One line of a <see cref="SaleResponseBody"/>, with the discount and totals the domain computed.
/// </summary>
/// <param name="Id">The id of the line.</param>
/// <param name="ProductId">The id of the product.</param>
/// <param name="ProductName">The product's name.</param>
/// <param name="Quantity">The quantity of identical items.</param>
/// <param name="UnitPrice">The price of one item.</param>
/// <param name="DiscountPercentage">The discount percentage.</param>
/// <param name="DiscountAmount">The discount amount.</param>
/// <param name="TotalAmount">The line total, after the discount.</param>
/// <param name="IsCancelled">Whether the line was cancelled.</param>
public sealed record SaleItemResponseBody(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountPercentage,
    decimal DiscountAmount,
    decimal TotalAmount,
    bool IsCancelled);
```

Create `tests/Ambev.DeveloperEvaluation.Functional/TestData/SaleRequestBodyTestData.cs`:

```csharp
using Bogus;

namespace Ambev.DeveloperEvaluation.Functional.TestData;

/// <summary>
/// Generates create-sale bodies that pass every Sales rule, with Bogus.
/// </summary>
public static class SaleRequestBodyTestData
{
    /// <summary>
    /// Generates a valid body. The sale number is unique on every call, so tests that share the database never
    /// collide on its unique index, and the sale date has whole seconds, so it reads back exactly.
    /// </summary>
    /// <param name="items">The lines of the sale. With none, the body gets one random line.</param>
    /// <returns>A valid <see cref="SaleRequestBody"/>.</returns>
    public static SaleRequestBody GenerateValid(params SaleItemRequestBody[] items)
    {
        var faker = new Faker();
        var saleDate = faker.Date.Recent().ToUniversalTime();
        return new SaleRequestBody(
            SaleNumber: $"S-{Guid.NewGuid():N}",
            SaleDate: new DateTime(saleDate.Year, saleDate.Month, saleDate.Day, saleDate.Hour, saleDate.Minute, saleDate.Second, DateTimeKind.Utc),
            CustomerId: Guid.NewGuid(),
            CustomerName: faker.Name.FullName(),
            BranchId: Guid.NewGuid(),
            BranchName: $"Filial {faker.Address.City()}",
            Items: items.Length > 0 ? items : [GenerateItem(faker.Random.Int(1, 20), Math.Round(faker.Random.Decimal(0.01m, 500m), 2))]);
    }

    /// <summary>
    /// Generates a line for a new product.
    /// </summary>
    /// <param name="quantity">The quantity of identical items.</param>
    /// <param name="unitPrice">The price of one item.</param>
    /// <returns>The line.</returns>
    public static SaleItemRequestBody GenerateItem(int quantity, decimal unitPrice) =>
        new(Guid.NewGuid(), new Faker().Commerce.ProductName(), quantity, unitPrice);
}
```

- [ ] **Step 4: Add the HTTP helpers**

Read `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiHttpExtensions.cs` first (the Write tool needs that). It holds ticket 04's `SignUpAsync` and `ReadDataAsync`. Then, with the Write tool, replace the whole file with:

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;

namespace Ambev.DeveloperEvaluation.Functional.Fixtures;

/// <summary>
/// Helpers for calling the API the way a client does.
/// </summary>
public static class ApiHttpExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Signs up a user with <c>POST /api/users</c>, for tests whose subject is a later step, and fails the test if that doesn't return 201.
    /// </summary>
    /// <param name="client">The client of the API under test.</param>
    /// <param name="request">The sign-up request.</param>
    /// <returns>The new user's id.</returns>
    public static async Task<Guid> SignUpAsync(this HttpClient client, SignUpRequest request)
    {
        using var response = await client.PostAsJsonAsync("/api/users", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.ReadDataAsync()).GetProperty("id").GetGuid();
    }

    /// <summary>
    /// Signs up a new active user and logs in, for tests whose subject is a later step. The client then sends
    /// the JWT with every request, as <c>Authorization: Bearer &lt;token&gt;</c>. Fails the test if either call fails.
    /// </summary>
    /// <param name="client">The client of the API under test.</param>
    /// <returns>A task that completes when the client holds the token.</returns>
    public static async Task LogInAsNewUserAsync(this HttpClient client)
    {
        var user = SignUpRequestTestData.GenerateValid();
        await client.SignUpAsync(user);

        using var response = await client.PostAsJsonAsync("/api/auth", new { user.Email, user.Password });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = (await response.ReadDataAsync()).GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Creates a sale with <c>POST /api/sales</c>, for tests whose subject is a later step, and fails the test if that doesn't return 201.
    /// </summary>
    /// <param name="client">A client that holds a token.</param>
    /// <param name="sale">The sale to create.</param>
    /// <returns>The created sale, as the response returned it.</returns>
    public static async Task<SaleResponseBody> CreateSaleAsync(this HttpClient client, SaleRequestBody sale)
    {
        using var response = await client.PostAsJsonAsync("/api/sales", sale);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.ReadSaleAsync();
    }

    /// <summary>
    /// Reads the <c>data</c> property of a success envelope <c>{success, message, data}</c>.
    /// </summary>
    /// <param name="response">A success response of the API.</param>
    /// <returns>A copy of the <c>data</c> element that outlives the parsed document.</returns>
    public static async Task<JsonElement> ReadDataAsync(this HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("data").Clone();
    }

    /// <summary>
    /// Reads the sale in the <c>data</c> property of a Sales success envelope.
    /// </summary>
    /// <param name="response">A success response of the Sales API.</param>
    /// <returns>The sale.</returns>
    public static async Task<SaleResponseBody> ReadSaleAsync(this HttpResponseMessage response) =>
        (await response.ReadDataAsync()).Deserialize<SaleResponseBody>(JsonOptions)!;
}
```

`SignUpAsync` and `ReadDataAsync` are unchanged, and ticket 04's tests keep using them.

- [ ] **Step 5: Write the failing functional tests**

Create `tests/Ambev.DeveloperEvaluation.Functional/Sales/SaleJson.cs`:

```csharp
using System.Text.Json;
using FluentAssertions;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

/// <summary>
/// The JSON shape of a sale in the Sales API's success responses (spec §7.2).
/// </summary>
internal static class SaleJson
{
    private static readonly string[] SaleFields =
    [
        "id", "saleNumber", "saleDate", "customerId", "customerName", "branchId", "branchName",
        "totalAmount", "isCancelled", "createdAt", "updatedAt", "items"
    ];

    private static readonly string[] ItemFields =
    [
        "id", "productId", "productName", "quantity", "unitPrice", "discountPercentage", "discountAmount",
        "totalAmount", "isCancelled"
    ];

    /// <summary>
    /// Checks that the body is exactly <c>{success, message, data}</c>, built once, and that the sale in <c>data</c>
    /// and each of its lines have exactly the documented fields, in order: no <c>isDeleted</c>, no <c>deletedAt</c>.
    /// </summary>
    /// <param name="response">A success response of the Sales API.</param>
    /// <param name="message">The expected success message.</param>
    /// <returns>A task that completes when the body has been checked.</returns>
    public static async Task ShouldBeSaleEnvelopeAsync(HttpResponseMessage response, string message)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        root.EnumerateObject().Select(property => property.Name).Should().Equal("success", "message", "data");
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        root.GetProperty("message").GetString().Should().Be(message);

        var sale = root.GetProperty("data");
        sale.EnumerateObject().Select(property => property.Name).Should().Equal(SaleFields);
        sale.GetProperty("items").EnumerateArray().Should().NotBeEmpty()
            .And.AllSatisfy(item => item.EnumerateObject().Select(property => property.Name).Should().Equal(ItemFields));
    }
}
```

Create `tests/Ambev.DeveloperEvaluation.Functional/Sales/CreateSaleTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

/// <summary>
/// Contains functional tests for creating a sale with <c>POST /api/sales</c>.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class CreateSaleTests
{
    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSaleTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public CreateSaleTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// One line at 4.50 per row: quantity, then the expected discount percentage, discount amount and line total.
    /// </summary>
    public static TheoryData<int, decimal, decimal, decimal> DiscountTiers => new()
    {
        { 3, 0m, 0.00m, 13.50m },
        { 4, 10m, 1.80m, 16.20m },
        { 10, 20m, 9.00m, 36.00m }
    };

    /// <summary>
    /// Tests the whole create response: 201, the <c>Location</c> of the new sale, the envelope built once with the
    /// documented fields, and the sale as sent plus what the domain computed (5 × 4.50 gets 10%).
    /// </summary>
    [Fact(DisplayName = "Given a logged-in client When posting a valid sale Then returns 201 at /api/sales/{id} with the sale and its discount")]
    public async Task Given_LoggedInClient_When_PostingValidSale_Then_Returns201WithSaleAndDiscount()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var sale = SaleRequestBodyTestData.GenerateValid(SaleRequestBodyTestData.GenerateItem(5, 4.50m));

        // When
        using var response = await client.PostAsJsonAsync("/api/sales", sale);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        await SaleJson.ShouldBeSaleEnvelopeAsync(response, "Sale created successfully");
        var created = await response.ReadSaleAsync();
        response.Headers.Location!.AbsolutePath.Should().Be($"/api/sales/{created.Id}");
        created.Should().BeEquivalentTo(new
        {
            sale.SaleNumber,
            sale.SaleDate,
            sale.CustomerId,
            sale.CustomerName,
            sale.BranchId,
            sale.BranchName,
            TotalAmount = 20.25m,
            IsCancelled = false,
            UpdatedAt = (DateTime?)null,
            Items = new[]
            {
                new
                {
                    sale.Items[0].ProductId,
                    sale.Items[0].ProductName,
                    Quantity = 5,
                    UnitPrice = 4.50m,
                    DiscountPercentage = 10m,
                    DiscountAmount = 2.25m,
                    TotalAmount = 20.25m,
                    IsCancelled = false
                }
            }
        });
    }

    /// <summary>
    /// Tests rule R1 over HTTP at the three tiers: 3 gets nothing, 4 gets 10% (spec decision D3), 10 gets 20%.
    /// </summary>
    [Theory(DisplayName = "Given a line at a discount tier When posting the sale Then the response has the tier's discount and totals")]
    [MemberData(nameof(DiscountTiers))]
    public async Task Given_LineAtDiscountTier_When_PostingSale_Then_ResponseHasTierDiscount(
        int quantity, decimal discountPercentage, decimal discountAmount, decimal totalAmount)
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var sale = SaleRequestBodyTestData.GenerateValid(SaleRequestBodyTestData.GenerateItem(quantity, 4.50m));

        // When
        using var response = await client.PostAsJsonAsync("/api/sales", sale);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.ReadSaleAsync();
        created.TotalAmount.Should().Be(totalAmount);
        created.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            DiscountPercentage = discountPercentage,
            DiscountAmount = discountAmount,
            TotalAmount = totalAmount
        });
    }

    /// <summary>
    /// Tests rule R2: discount fields that a client sends are ignored, and the domain computes its own.
    /// </summary>
    [Fact(DisplayName = "Given a body with discount fields When posting the sale Then they are ignored and the domain's discount applies")]
    public async Task Given_BodyWithDiscountFields_When_PostingSale_Then_DomainDiscountApplies()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var body = JsonSerializer.SerializeToNode(
            SaleRequestBodyTestData.GenerateValid(SaleRequestBodyTestData.GenerateItem(3, 10.00m)),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();
        body["totalAmount"] = 15.00m;
        var line = body["items"]![0]!.AsObject();
        line["discountPercentage"] = 50m;
        line["discountAmount"] = 15.00m;
        line["totalAmount"] = 15.00m;

        // When
        using var response = await client.PostAsJsonAsync("/api/sales", body);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.ReadSaleAsync();
        created.TotalAmount.Should().Be(30.00m);
        created.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            DiscountPercentage = 0m,
            DiscountAmount = 0.00m,
            TotalAmount = 30.00m
        });
    }

    /// <summary>
    /// Tests rule R1's limit: 21 identical items are a validation error with R1's message.
    /// </summary>
    [Fact(DisplayName = "Given a line with 21 identical items When posting the sale Then returns 400 ValidationError with R1's message")]
    public async Task Given_LineWith21Items_When_PostingSale_Then_Returns400WithR1Message()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var sale = SaleRequestBodyTestData.GenerateValid(SaleRequestBodyTestData.GenerateItem(21, 4.50m));

        // When
        using var response = await client.PostAsJsonAsync("/api/sales", sale);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            """{"type":"ValidationError","error":"Invalid input data","detail":"Items[0].Quantity: It's not possible to sell above 20 identical items"}""");
    }

    /// <summary>
    /// Tests rule R4: a product that appears in two lines is a validation error.
    /// </summary>
    [Fact(DisplayName = "Given two lines for the same product When posting the sale Then returns 400 ValidationError")]
    public async Task Given_TwoLinesForSameProduct_When_PostingSale_Then_Returns400()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var line = SaleRequestBodyTestData.GenerateItem(2, 4.50m);
        var sale = SaleRequestBodyTestData.GenerateValid(line, line with { Quantity = 3 });

        // When
        using var response = await client.PostAsJsonAsync("/api/sales", sale);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            """{"type":"ValidationError","error":"Invalid input data","detail":"Items: Each product can appear only once in a sale"}""");
    }

    /// <summary>
    /// Tests rule R5: a sale without lines is a validation error.
    /// </summary>
    [Fact(DisplayName = "Given an empty item list When posting the sale Then returns 400 ValidationError")]
    public async Task Given_EmptyItemList_When_PostingSale_Then_Returns400()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var sale = SaleRequestBodyTestData.GenerateValid() with { Items = [] };

        // When
        using var response = await client.PostAsJsonAsync("/api/sales", sale);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            """{"type":"ValidationError","error":"Invalid input data","detail":"Items: A sale must have at least one item"}""");
    }
}
```

Create `tests/Ambev.DeveloperEvaluation.Functional/Sales/GetSaleTests.cs`:

```csharp
using System.Net;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

/// <summary>
/// Contains functional tests for reading a sale with <c>GET /api/sales/{id}</c>.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class GetSaleTests
{
    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSaleTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public GetSaleTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// Tests that a created sale reads back from the database as the create response showed it.
    /// The create response comes from memory, so its <c>createdAt</c> has 100 ns ticks where PostgreSQL keeps microseconds.
    /// </summary>
    [Fact(DisplayName = "Given a created sale When getting it by id Then returns 200 with the same sale as the create response")]
    public async Task Given_CreatedSale_When_GettingById_Then_Returns200WithSameSale()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var created = await client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid(
            SaleRequestBodyTestData.GenerateItem(4, 4.50m),
            SaleRequestBodyTestData.GenerateItem(12, 3.10m)));

        // When
        using var response = await client.GetAsync($"/api/sales/{created.Id}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await SaleJson.ShouldBeSaleEnvelopeAsync(response, "Sale retrieved successfully");
        (await response.ReadSaleAsync()).Should().BeEquivalentTo(created, options => options
            .ComparingRecordsByMembers()
            .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromMicroseconds(1)))
            .WhenTypeIs<DateTime>());
    }

    /// <summary>
    /// Tests that an unknown id is a 404 with the documented body.
    /// </summary>
    [Fact(DisplayName = "Given an unknown id When getting the sale Then returns 404 ResourceNotFound")]
    public async Task Given_UnknownId_When_GettingSale_Then_Returns404ResourceNotFound()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var id = Guid.NewGuid();

        // When
        using var response = await client.GetAsync($"/api/sales/{id}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"ResourceNotFound","error":"Resource not found","detail":"The sale with ID {{id}} does not exist"}""");
    }

    /// <summary>
    /// Tests that the query validator runs through the MediatR pipeline: the empty id is a validation error.
    /// </summary>
    [Fact(DisplayName = "Given the empty id When getting the sale Then returns 400 ValidationError")]
    public async Task Given_EmptyId_When_GettingSale_Then_Returns400ValidationError()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();

        // When
        using var response = await client.GetAsync($"/api/sales/{Guid.Empty}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            """{"type":"ValidationError","error":"Invalid input data","detail":"Id: 'Id' must not be empty."}""");
    }
}
```

Notes for reviewers:
- `ComparingRecordsByMembers()` matters in the read-back test. By default FluentAssertions compares records with `Equals`, which would compare the `Items` lists by reference and skip the 1 µs tolerance on `createdAt`.
- The discount-fields test builds the body as a `JsonObject`, adds `totalAmount`, `discountPercentage` and `discountAmount` values that would be wrong, and expects the domain's values back (rule R2). System.Text.Json ignores the unknown fields.
- The empty-id test proves `GetSaleQueryValidator` runs through the pipeline: the route binds `Guid.Empty`, and the validator turns it into a 400.

- [ ] **Step 6: Watch the functional tests fail (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Functional 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~Functional.Sales" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | sort | uniq -c | cut -c1-200
```

Expected: `0 Error(s)` and `Build succeeded.` The Functional project goes through HTTP only, so it compiles without the controller. Then **(probe)**:

```
      1    Expected (response.Content.ReadAsStringAsync()) to be "{"type":"ResourceNotFound","error":"Resource not found","detail":"The sale with ID … does not exist
      4    Expected response.StatusCode to be HttpStatusCode.BadRequest {value: 400}, but found HttpStatusCode.NotFound {value: 404}.
      6    Expected response.StatusCode to be HttpStatusCode.Created {value: 201}, but found HttpStatusCode.NotFound {value: 404}.
      1 Failed!  - Failed:    11, Passed:     0, Skipped:     0, Total:    11, Duration: … - Ambev.DeveloperEvaluation.Functional.dll (net8.0)
```

Every request to `/api/sales` gets the framework's 404, because no controller has that route yet.

- [ ] **Step 7: Write the contracts and profiles**

Create `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SaleItemRequest.cs`:

```csharp
namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales;

/// <summary>
/// One line of a sale in a request body. There are no discount fields: the domain computes them (rule R2).
/// </summary>
public sealed class SaleItemRequest
{
    /// <summary>
    /// Gets or sets the id of the product.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product's name, copied into the sale.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity of identical items, from 1 to 20.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the price of one item: above 0, with at most 2 decimal places.
    /// </summary>
    public decimal UnitPrice { get; set; }
}
```

Create `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SaleResponse.cs`:

```csharp
namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales;

/// <summary>
/// A sale in a response body (spec §7.2), with flat fields. It has no <c>isDeleted</c> or <c>deletedAt</c>:
/// deleted sales are never returned.
/// </summary>
public sealed class SaleResponse
{
    /// <summary>
    /// Gets or sets the id of the sale.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the sale number.
    /// </summary>
    public string SaleNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time of the sale, in UTC.
    /// </summary>
    public DateTime SaleDate { get; set; }

    /// <summary>
    /// Gets or sets the id of the customer.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the customer's name.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the id of the branch.
    /// </summary>
    public Guid BranchId { get; set; }

    /// <summary>
    /// Gets or sets the branch's name.
    /// </summary>
    public string BranchName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sale total: the sum of the totals of the lines that aren't cancelled.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the sale was cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// Gets or sets when the sale was created, in UTC.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the sale was last changed, in UTC, or <c>null</c> if it never was.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the lines of the sale, cancelled ones included.
    /// </summary>
    public IReadOnlyList<SaleItemResponse> Items { get; set; } = [];
}
```

Create `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SaleItemResponse.cs`:

```csharp
namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales;

/// <summary>
/// A line of a <see cref="SaleResponse"/>, with the discount and totals the domain computed.
/// </summary>
public sealed class SaleItemResponse
{
    /// <summary>
    /// Gets or sets the id of the line.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the id of the product.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product's name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity of identical items.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the price of one item.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets or sets the discount percentage: 0, 10 or 20.
    /// </summary>
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// Gets or sets the discount amount.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Gets or sets the line total, after the discount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the line was cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }
}
```

Create `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SaleContractProfile.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Sales;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales;

/// <summary>
/// Maps the Sales contracts that several endpoints share: a request line to the Application input,
/// and the Application result to the response.
/// </summary>
public sealed class SaleContractProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SaleContractProfile"/> class with the shared Sales maps.
    /// </summary>
    public SaleContractProfile()
    {
        CreateMap<SaleItemRequest, SaleItemInput>();
        CreateMap<SaleResult, SaleResponse>();
        CreateMap<SaleItemResult, SaleItemResponse>();
    }
}
```

Create `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/CreateSale/CreateSaleRequest.cs`:

```csharp
namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;

/// <summary>
/// The body of <c>POST /api/sales</c> (spec §7.2). The Application validator checks it, not this contract (spec decision D10).
/// </summary>
public sealed class CreateSaleRequest
{
    /// <summary>
    /// Gets or sets the sale number. Trimmed, it must have 1 to 50 characters.
    /// </summary>
    public string SaleNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time of the sale. A value without an offset is read as UTC.
    /// </summary>
    public DateTime SaleDate { get; set; }

    /// <summary>
    /// Gets or sets the id of the customer.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the customer's name.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the id of the branch.
    /// </summary>
    public Guid BranchId { get; set; }

    /// <summary>
    /// Gets or sets the branch's name.
    /// </summary>
    public string BranchName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the lines of the sale: at least one, and one per product.
    /// </summary>
    public List<SaleItemRequest> Items { get; set; } = [];
}
```

Create `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/CreateSale/CreateSaleProfile.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;

/// <summary>
/// Maps the create-sale request to its command. The lines use the map in <see cref="SaleContractProfile"/>.
/// </summary>
public sealed class CreateSaleProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSaleProfile"/> class with the request map.
    /// </summary>
    public CreateSaleProfile()
    {
        CreateMap<CreateSaleRequest, CreateSaleCommand>();
    }
}
```

`SaleNumber` stays `string` with an empty default: this ticket always requires it, and ticket 06 makes it `string?` with no default. Ticket 03's `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes` keeps MVC from rejecting a body before the validator sees it. `Program.cs` already registers every profile in the WebApi assembly.

- [ ] **Step 8: Write the controller**

Create `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales;

/// <summary>
/// The Sales API. Every endpoint needs a JWT from <c>POST /api/auth</c>; no role is checked (spec decision D13).
/// </summary>
[ApiController]
[Route("api/sales")]
[Authorize]
public sealed class SalesController : BaseController
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="SalesController"/> class.
    /// </summary>
    /// <param name="mediator">The mediator that runs the Sales use cases.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    public SalesController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    /// <summary>
    /// Creates a sale. The domain computes the discounts and totals.
    /// </summary>
    /// <param name="request">The sale to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 with the created sale, and its address in the <c>Location</c> header.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(_mapper.Map<CreateSaleCommand>(request), cancellationToken);

        return Created(nameof(GetSale), new { id = result.Id }, _mapper.Map<SaleResponse>(result), "Sale created successfully");
    }

    /// <summary>
    /// Gets one sale, with its items.
    /// </summary>
    /// <param name="id">The id of the sale.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 with the sale.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSale([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSaleQuery(id), cancellationToken);

        return Ok(_mapper.Map<SaleResponse>(result), "Sale retrieved successfully");
    }
}
```

Both actions use ticket 03's helpers, which take the message, so the envelope is built exactly once. A one-argument `Ok(dto)` would compile but skip the envelope (ticket 03, Task 7 Step 7). Ticket 04's global Swagger security requirement already covers these endpoints, and the rehearsal generated the document through `ISwaggerProvider` with the new paths `/api/sales` (POST) and `/api/sales/{id}` (GET).

- [ ] **Step 9: Verify (GREEN)**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~WebApi.Features.Sales" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~Functional.Sales|FullyQualifiedName~Functional.Users|FullyQualifiedName~Functional.Auth" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected: `0 Error(s)` and `Build succeeded.`; `Passed:     5`; `Passed:   172`. Then **(probe)** `Passed!  - Failed:     0, Passed:    18, …`: the 11 new Sales tests and ticket 04's 7 Users and Auth tests. That includes `Location` ending in `/api/sales/{id}`, the envelope and field names, the three tiers, the ignored discount fields, the exact 400 and 404 bodies, and the read-back. The run against the container is **(derived)**.

- [ ] **Step 10: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SaleItemRequest.cs src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SaleResponse.cs src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SaleItemResponse.cs src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SaleContractProfile.cs src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/CreateSale/CreateSaleRequest.cs src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/CreateSale/CreateSaleProfile.cs tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesMappingTests.cs tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesControllerTests.cs tests/Ambev.DeveloperEvaluation.Functional/TestData/SaleRequestBody.cs tests/Ambev.DeveloperEvaluation.Functional/TestData/SaleResponseBody.cs tests/Ambev.DeveloperEvaluation.Functional/TestData/SaleRequestBodyTestData.cs tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiHttpExtensions.cs tests/Ambev.DeveloperEvaluation.Functional/Sales/SaleJson.cs tests/Ambev.DeveloperEvaluation.Functional/Sales/CreateSaleTests.cs tests/Ambev.DeveloperEvaluation.Functional/Sales/GetSaleTests.cs
git commit -m "feat(sales): create and get sales over HTTP" -m "SalesController requires a JWT and serves POST /api/sales (201 with Location /api/sales/{id} and Sale created successfully) and GET /api/sales/{id} (200 with the sale). Both use the envelope helpers, so nothing is wrapped twice. The request contracts have no discount fields (rule R2), and the responses have no soft-delete fields. The functional tests sign up, log in and go through create, read-back, the discount tiers and the 400 and 404 bodies over HTTP."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `16 files changed, 1022 insertions(+)` and `Scan complete: 0 issue(s) found`.

---

### Task 15: Answer a missing or invalid JWT with the 401 error body

**Skills:** `dotnet-best-practices`, `dependency-injection-patterns` (why the wiring stays in `Program.cs`), `testcontainers-integration-tests`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Common/JwtBearerChallengeTests.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Functional/Sales/SalesAuthenticationTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.WebApi/Common/JwtBearerChallenge.cs`
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Program.cs` (one `using`, one line)

- [ ] **Step 1: Write the failing unit tests**

Create `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Common/JwtBearerChallengeTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.WebApi.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Common;

/// <summary>
/// Contains unit tests for <see cref="JwtBearerChallenge"/>, which answers a request without a valid JWT.
/// </summary>
public sealed class JwtBearerChallengeTests
{
    /// <summary>
    /// Tests that the challenge writes the 401 body of spec §7.4 instead of an empty response,
    /// keeps the <c>WWW-Authenticate: Bearer</c> header, and stops the handler from writing its own response.
    /// </summary>
    [Fact(DisplayName = "Given a JWT bearer challenge When it is answered Then it writes 401 AuthenticationError with the invalid-token body")]
    public async Task Given_JwtBearerChallenge_When_Answered_Then_Writes401WithInvalidTokenBody()
    {
        // Given
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        var scheme = new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler));
        var challenge = new JwtBearerChallengeContext(httpContext, scheme, new JwtBearerOptions(), new AuthenticationProperties());

        // When
        await JwtBearerChallenge.WriteResponseAsync(challenge);

        // Then
        challenge.Handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        httpContext.Response.Headers.WWWAuthenticate.ToString().Should().Be("Bearer");
        httpContext.Response.ContentType.Should().Be("application/json; charset=utf-8");
        httpContext.Response.Body.Position = 0;
        (await new StreamReader(httpContext.Response.Body).ReadToEndAsync()).Should().Be(
            """{"type":"AuthenticationError","error":"Invalid authentication token","detail":"The provided authentication token has expired or is invalid"}""");
    }

    /// <summary>
    /// Tests that configuring the JWT bearer options points the handler's challenge event at the response above.
    /// </summary>
    [Fact(DisplayName = "Given JWT bearer options When the challenge is configured Then the challenge event writes the invalid-token response")]
    public void Given_JwtBearerOptions_When_ChallengeConfigured_Then_ChallengeEventWritesInvalidTokenResponse()
    {
        // Given
        var options = new JwtBearerOptions();

        // When
        JwtBearerChallenge.Configure(options);

        // Then
        options.Events.OnChallenge.Should().Be(new Func<JwtBearerChallengeContext, Task>(JwtBearerChallenge.WriteResponseAsync));
    }
}
```

- [ ] **Step 2: Write the failing functional tests**

Create `tests/Ambev.DeveloperEvaluation.Functional/Sales/SalesAuthenticationTests.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

/// <summary>
/// Contains functional tests for the JWT that every Sales endpoint requires (spec §7.5).
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class SalesAuthenticationTests
{
    private const string InvalidTokenBody =
        """{"type":"AuthenticationError","error":"Invalid authentication token","detail":"The provided authentication token has expired or is invalid"}""";

    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="SalesAuthenticationTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public SalesAuthenticationTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// Tests that a request without a token gets the documented 401 body, not an empty response.
    /// </summary>
    [Fact(DisplayName = "Given no token When posting a sale Then returns 401 AuthenticationError with the invalid-token body")]
    public async Task Given_NoToken_When_PostingSale_Then_Returns401WithInvalidTokenBody()
    {
        // Given
        using var client = _api.CreateClient();

        // When
        using var response = await client.PostAsJsonAsync("/api/sales", SaleRequestBodyTestData.GenerateValid());

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ToString().Should().Be("Bearer");
        (await response.Content.ReadAsStringAsync()).Should().Be(InvalidTokenBody);
    }

    /// <summary>
    /// Tests that a well-formed token signed with another key is rejected with the documented 401 body.
    /// </summary>
    [Fact(DisplayName = "Given a token signed with another key When getting a sale Then returns 401 AuthenticationError with the invalid-token body")]
    public async Task Given_TokenSignedWithAnotherKey_When_GettingSale_Then_Returns401WithInvalidTokenBody()
    {
        // Given
        using var client = _api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            CreateToken("another-signing-key-that-is-at-least-32-bytes-long", DateTime.UtcNow.AddHours(1)));

        // When
        using var response = await client.GetAsync($"/api/sales/{Guid.NewGuid()}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).Should().Be(InvalidTokenBody);
    }

    /// <summary>
    /// Tests that a token signed with the API's own key but already expired is rejected with the documented 401 body.
    /// </summary>
    [Fact(DisplayName = "Given an expired token When getting a sale Then returns 401 AuthenticationError with the invalid-token body")]
    public async Task Given_ExpiredToken_When_GettingSale_Then_Returns401WithInvalidTokenBody()
    {
        // Given
        using var client = _api.CreateClient();
        var apiKey = _api.Services.GetRequiredService<IConfiguration>()["Jwt:SecretKey"]!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            CreateToken(apiKey, DateTime.UtcNow.AddMinutes(-5)));

        // When
        using var response = await client.GetAsync($"/api/sales/{Guid.NewGuid()}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).Should().Be(InvalidTokenBody);
    }

    private static string CreateToken(string signingKey, DateTime expires) =>
        new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            claims: [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
            notBefore: expires.AddHours(-2),
            expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.ASCII.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256)));
}
```

Notes for reviewers:
- The badly signed token is well formed, so the test exercises the signature check, not just parsing. The expired one is signed with the API's own key, which the test reads from the API's configuration, so only the lifetime check can reject it.
- `System.IdentityModel.Tokens.Jwt` reaches the Functional project through WebApi, as ticket 04 noted, so no package is added.

- [ ] **Step 3: Watch them fail (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
dotnet build tests/Ambev.DeveloperEvaluation.Functional 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~SalesAuthenticationTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-220
```

Expected: the unit build fails with

```
JwtBearerChallengeTests.cs(29,15): error CS0103: The name 'JwtBearerChallenge' does not exist in the current context
JwtBearerChallengeTests.cs(51,9): error CS0103: The name 'JwtBearerChallenge' does not exist in the current context
JwtBearerChallengeTests.cs(54,90): error CS0103: The name 'JwtBearerChallenge' does not exist in the current context
```

The Functional project builds (`0 Error(s)`, `Build succeeded.`). Then **(probe)**:

```
   Expected (response.Content.ReadAsStringAsync()) to be "{"type":"AuthenticationError","error":"Invalid authentication token","detail":"The provided authentication token has expired or is invalid"}" with a length of 140
   Expected (response.Content.ReadAsStringAsync()) to be "{"type":"AuthenticationError","error":"Invalid authentication token","detail":"The provided authentication token has expired or is invalid"}" with a length of 140
   Expected (response.Content.ReadAsStringAsync()) to be "{"type":"AuthenticationError","error":"Invalid authentication token","detail":"The provided authentication token has expired or is invalid"}" with a length of 140
Failed!  - Failed:     3, Passed:     0, Skipped:     0, Total:     3, Duration: … - Ambev.DeveloperEvaluation.Functional.dll (net8.0)
```

The JWT bearer handler answers 401 with an empty body. The no-token test gets past its header check, because the default handler already sends `WWW-Authenticate: Bearer`, and the challenge must keep sending it.

- [ ] **Step 4: Write the challenge response**

Create `src/Ambev.DeveloperEvaluation.WebApi/Common/JwtBearerChallenge.cs`:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Ambev.DeveloperEvaluation.WebApi.Common;

/// <summary>
/// Answers a request that reaches an <c>[Authorize]</c> endpoint without a valid JWT (missing, badly signed or expired)
/// with the 401 body of spec §7.4, instead of the JWT bearer handler's empty response.
/// </summary>
public static class JwtBearerChallenge
{
    private static readonly ApiErrorResponse InvalidTokenBody = new(
        "AuthenticationError",
        "Invalid authentication token",
        "The provided authentication token has expired or is invalid");

    /// <summary>
    /// Makes the JWT bearer handler answer its challenges with <see cref="WriteResponseAsync"/>.
    /// </summary>
    /// <param name="options">The options of the JWT bearer scheme.</param>
    public static void Configure(JwtBearerOptions options) =>
        options.Events = new JwtBearerEvents { OnChallenge = WriteResponseAsync };

    /// <summary>
    /// Writes 401 with the <c>AuthenticationError</c> body and a <c>WWW-Authenticate: Bearer</c> header,
    /// and marks the challenge handled so the handler doesn't write its own response.
    /// </summary>
    /// <param name="context">The challenge of the JWT bearer handler.</param>
    /// <returns>A task that completes when the response has been written.</returns>
    public static async Task WriteResponseAsync(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.WWWAuthenticate = JwtBearerDefaults.AuthenticationScheme;
        await context.Response.WriteAsJsonAsync(InvalidTokenBody);
    }
}
```

`HttpContext`, `StatusCodes` and `WriteAsJsonAsync` come from the Web SDK's implicit usings. The body needs no relaxed escaping, so the default JSON options serialize it, in camelCase (Decision 12).

- [ ] **Step 5: Wire it after the JWT registration**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.WebApi/Program.cs`, replace

```csharp
            builder.Services.AddJwtAuthentication(builder.Configuration);
```

with

```csharp
            builder.Services.AddJwtAuthentication(builder.Configuration);
            builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, JwtBearerChallenge.Configure);
```

Then replace

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
```

with

```csharp
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
```

The configure action runs after `AddJwtBearer`'s own, so it only adds the events. `AddJwtAuthentication` sets none, and its token validation stays as it is.

- [ ] **Step 6: Verify (GREEN)**

```bash
git diff --stat -- src | cat
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~JwtBearerChallengeTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~Functional.Sales|FullyQualifiedName~Functional.Users|FullyQualifiedName~Functional.Auth|FullyQualifiedName~StartupFailureTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected: `src/Ambev.DeveloperEvaluation.WebApi/Program.cs | 2 ++`; `0 Error(s)` and `Build succeeded.`; `Passed:     2`; `Passed:   174`. Then **(probe)** `Passed!  - Failed:     0, Passed:    22, …`: 14 Sales tests, ticket 04's 7 Users and Auth tests, and the startup test. The last one still gets its configuration error, since the new line comes after `AddJwtAuthentication`.

- [ ] **Step 7: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.WebApi/Common/JwtBearerChallenge.cs src/Ambev.DeveloperEvaluation.WebApi/Program.cs tests/Ambev.DeveloperEvaluation.Unit/WebApi/Common/JwtBearerChallengeTests.cs tests/Ambev.DeveloperEvaluation.Functional/Sales/SalesAuthenticationTests.cs
git commit -m "feat(auth): answer a missing or invalid JWT with the 401 error body" -m "The JWT bearer challenge answered with an empty 401. It now writes {type: AuthenticationError, error: Invalid authentication token, detail: The provided authentication token has expired or is invalid}, with WWW-Authenticate: Bearer (spec 7.4 and 7.5). The functional tests cover no token, a token signed with another key, and an expired token."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `4 files changed, 195 insertions(+)` and `Scan complete: 0 issue(s) found`.

---

### Task 16: Add the sales requests to the `.http` file and replay them against `docker compose up`

**Files:**
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http` (requests appended)

- [ ] **Step 1: Look at the file (RED)**

```bash
grep -c 'api/sales' src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
tail -3 src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
```

Expected: `0`, then ticket 04's last lines: `###`, the comment about the JWT, and `@token = {{login.response.body.$.data.token}}`.

- [ ] **Step 2: Append the create and get requests**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http`, replace

```http
###
# The JWT from the login above. Requests that need it send the header "Authorization: Bearer" plus this token.
@token = {{login.response.body.$.data.token}}
```

with

```http
###
# The JWT from the login above. Requests that need it send the header "Authorization: Bearer" plus this token.
@token = {{login.response.body.$.data.token}}

### Create a sale: 5 identical items get 10%, which the API computes (201 with the sale)
# The sale number is random, so the file can be re-run.
# @name createSale
POST {{baseUrl}}/api/sales
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "saleNumber": "S-{{$randomInt 100000 999999}}",
  "saleDate": "2026-09-24T14:30:00Z",
  "customerId": "3f2b8c1e-1111-4a5b-9c2d-000000000001",
  "customerName": "Maria Silva",
  "branchId": "3f2b8c1e-2222-4a5b-9c2d-000000000002",
  "branchName": "Filial Centro",
  "items": [
    { "productId": "3f2b8c1e-3333-4a5b-9c2d-000000000003", "productName": "Cerveja 350ml", "quantity": 5, "unitPrice": 4.50 }
  ]
}

###
# The id of the sale created above.
@saleId = {{createSale.response.body.$.data.id}}

### Get the sale back (200 with the same sale)
GET {{baseUrl}}/api/sales/{{saleId}}
Authorization: Bearer {{token}}
```

The comments avoid `{{…}}`, as ticket 04's do, so no editor tries to resolve a variable inside a comment. The customer, branch and product ids are the spec's example ids. They are external identities, with no foreign key, so any id works. Tickets 06–12 append their requests after the last line.

- [ ] **Step 3: Check the file (GREEN)**

```bash
git diff --stat | cat
grep -nE '^@|^(POST|GET) |# @name|randomInt|Authorization' src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
```

Expected: `1 file changed, 27 insertions(+)`, then:

```
10:@baseUrl = http://localhost:8080
11:@email = admin@developerstore.com
12:@password = Admin@123
15:POST {{baseUrl}}/api/users
28:# @name login
29:POST {{baseUrl}}/api/auth
38:# The JWT from the login above. Requests that need it send the header "Authorization: Bearer" plus this token.
39:@token = {{login.response.body.$.data.token}}
43:# @name createSale
44:POST {{baseUrl}}/api/sales
45:Authorization: Bearer {{token}}
49:  "saleNumber": "S-{{$randomInt 100000 999999}}",
62:@saleId = {{createSale.response.body.$.data.id}}
65:GET {{baseUrl}}/api/sales/{{saleId}}
66:Authorization: Bearer {{token}}
```

- [ ] **Step 4: Start the stack from nothing (derived)**

```bash
docker compose down -v --remove-orphans
docker compose up --build --wait --wait-timeout 300; echo "exit=$?"
```

Expected: the database reaches `Healthy` before the API starts, then `exit=0`. The build includes this branch's code, and the API applies all three migrations at startup. If a port is already allocated, stop and ask the user.

- [ ] **Step 5: Replay the file's requests with curl (derived)**

Keep this in one Bash call. The requests are exactly the `.http` file's, with its variables filled in. The loop creates two sales, the way running the file twice would, each with its own random number.

```bash
curl -s -o /dev/null -w "sign-up %{http_code}\n" -X POST http://localhost:8080/api/users -H 'Content-Type: application/json' --data-raw '{"username":"admin","password":"Admin@123","phone":"+5511999999999","email":"admin@developerstore.com","status":"Active","role":"Admin"}'
curl -s -o /tmp/ticket05-checks/login.json -X POST http://localhost:8080/api/auth -H 'Content-Type: application/json' --data-raw '{"email":"admin@developerstore.com","password":"Admin@123"}'
TOKEN=$(python3 -c "import json, sys; print(json.load(open(sys.argv[1]))['data']['token'])" /tmp/ticket05-checks/login.json)
for run in 1 2; do
  NUMBER="S-$(( RANDOM % 900000 + 100000 ))"
  curl -s -o /tmp/ticket05-checks/create.json -w "run $run create %{http_code} " -X POST http://localhost:8080/api/sales -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' --data-raw "{\"saleNumber\":\"$NUMBER\",\"saleDate\":\"2026-09-24T14:30:00Z\",\"customerId\":\"3f2b8c1e-1111-4a5b-9c2d-000000000001\",\"customerName\":\"Maria Silva\",\"branchId\":\"3f2b8c1e-2222-4a5b-9c2d-000000000002\",\"branchName\":\"Filial Centro\",\"items\":[{\"productId\":\"3f2b8c1e-3333-4a5b-9c2d-000000000003\",\"productName\":\"Cerveja 350ml\",\"quantity\":5,\"unitPrice\":4.50}]}"
  SALE_ID=$(python3 -c "import json, sys; d = json.load(open(sys.argv[1]))['data']; print(d['id']); print('number', d['saleNumber'], 'total', d['totalAmount'], 'discount', d['items'][0]['discountPercentage'], file=sys.stderr)" /tmp/ticket05-checks/create.json)
  curl -s -o /tmp/ticket05-checks/get.json -w "run $run get %{http_code} " "http://localhost:8080/api/sales/$SALE_ID" -H "Authorization: Bearer $TOKEN"
  python3 -c "import json, sys; d = json.load(open(sys.argv[1]))['data']; print('same id', d['id'] == sys.argv[2], 'total', d['totalAmount'], 'discount', d['items'][0]['discountPercentage'])" /tmp/ticket05-checks/get.json "$SALE_ID"
done
curl -s -w " %{http_code}\n" http://localhost:8080/api/sales/3f2b8c1e-5555-4a5b-9c2d-000000000009
```

Expected (the sale numbers are random):

```
sign-up 201
run 1 create 201 number S-… total 20.25 discount 10
run 1 get 200 same id True total 20.25 discount 10.00
run 2 create 201 number S-… total 20.25 discount 10
run 2 get 200 same id True total 20.25 discount 10.00
{"type":"AuthenticationError","error":"Invalid authentication token","detail":"The provided authentication token has expired or is invalid"} 401
```

`discount 10` after the create and `10.00` after the get is Decision 15. The last line is a request without a token.

- [ ] **Step 6: Check the event log and the schema on the stack (derived)**

```bash
docker compose logs ambev.developerevaluation.webapi 2>&1 | grep -o 'Sale event SaleCreatedEvent published {"SaleId": "[^"]*", "SaleNumber": "[^"]*"' | cut -c1-60
docker compose exec -T ambev.developerevaluation.database psql -U developer -d developer_evaluation -At -c 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY 1;'
docker compose exec -T ambev.developerevaluation.database psql -U developer -d developer_evaluation -At -c "SELECT (SELECT count(*) FROM \"Sales\"), (SELECT count(*) FROM \"SaleItems\"), (SELECT count(*) FROM pg_sequences WHERE sequencename = 'sale_number_seq');"
```

Expected:
- Two lines starting `Sale event SaleCreatedEvent published {"SaleId": "…`: one entry per create, written after the save.
- `20241014011203_InitialMigrations`, `<timestamp>_AddUserTimestamps` and `<timestamp>_AddSales`.
- `2|2|1`: two sales, two lines, and the sequence.

- [ ] **Step 7: Take the stack down**

```bash
docker compose down -v
docker compose ps -a
```

Expected: the containers and the network are removed, and `ps -a` lists nothing.

- [ ] **Step 8: Commit**

```bash
git add src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
git commit -m "docs(http): create a sale and read it back" -m "The .http file creates a sale with the token from the login request and a random sale number, so it can be re-run, keeps the new sale's id, and reads the sale back."
git show --stat --format= HEAD | tail -1
```

Expected: `1 file changed, 27 insertions(+)`. No slopwatch run: nothing C# changed.

---

### Task 17: Verify everything, review, then open a pull request into `develop`

**Skills:** superpowers:verification-before-completion, `dotnet-slopwatch`, `test-anti-patterns` (report only), `clean-code` and superpowers:requesting-code-review (both optional), superpowers:finishing-a-development-branch.

**Files:** none, unless Step 2 finds a real problem.

- [ ] **Step 1: Re-run every check fresh (superpowers:verification-before-completion)**

```bash
git status --short
dotnet build Ambev.DeveloperEvaluation.sln --no-incremental 2>&1 | grep -E 'warning CS|Warning\(s\)|Error\(s\)|Build succeeded' | sort -u
dotnet test Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-110
slopwatch analyze --fail-on warning 2>&1 | tail -1
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at ' | tail -1
ls src/Ambev.DeveloperEvaluation.ORM/Migrations/ | grep -c '_AddSales.cs$'
git grep -n -E 'AddScoped<ISaleRepository|JwtBearerChallenge.Configure|HasSequence' -- src | cut -c1-150
```

Expected:
- `git status --short` lists only `?? docs/superpowers/tickets/`, plus any other tickets' untracked plan files.
- `    0 Error(s)`, `    2 Warning(s)` and `Build succeeded.`, with no `warning CS` line.
- Three `Passed!` lines. Unit shows `Passed:   174`. **(derived)** Integration shows `Passed:     8` (ticket 02's 4, Task 12's 3, Task 13's 1), and Functional shows `Passed:    27` (ticket 04's 12, Task 13's 1, Task 14's 11, Task 15's 3).
- `Scan complete: 0 issue(s) found`.
- `No changes have been made to the model since the last migration.`
- `1`: one `AddSales` migration.
- Five wiring lines:
  - `InfrastructureModuleInitializer.cs` (`AddScoped<ISaleRepository, SaleRepository>`)
  - `DefaultContext.cs` (`HasSequence<long>(SaleConfiguration.SaleNumberSequence)`)
  - the generated `HasSequence("sale_number_seq")` in the `AddSales` designer file and in the snapshot
  - `Program.cs` (`JwtBearerChallenge.Configure`)

Each ticket criterion and its evidence:

| Ticket criterion | Evidence |
|---|---|
| `ExternalIdentity`: non-empty id, trimmed name of 1–100 characters, equality by value | `ExternalIdentityTests` (Task 2) |
| `DiscountPolicy`: 1–3 → 0, 4–9 → 10, 10–20 → 20; below 1 or above 20 throws, with R1's message above 20 | `DiscountPolicyTests` (Task 3) |
| `Sale.Create`: lines with R1 and R3, total of the active lines, R4, R5, sale number trimmed to 1–50, `SaleDate` in UTC, `CreatedAt`, `SaleCreatedEvent` with the §5.4 payload and `OccurredAt` | `SaleTests` (Tasks 4–6) |
| Ids created by the domain; `Items` read-only and backed by a private list | `SaleTests` (Task 4); `SaleConfiguration` maps `Items` through the field (Task 12) |
| Unit tests with Bogus `TestData` builders and `Given…When…Then` names | `SaleTestData`, `CreateSaleCommandTestData`, every new test class |
| Validators for `CreateSaleCommand` and `GetSaleQuery`, run by `ValidationBehavior`, with every listed rule | `CreateSaleCommandValidatorTests`, `GetSaleQueryValidatorTests`; over HTTP, the 400s in `CreateSaleTests` and the empty id in `GetSaleTests` |
| CreateSale saves, then publishes through `IPublisher` with a shared helper, then clears; nothing is published if the save throws | `CreateSaleHandlerTests` (`Received.InOrder`, and the save that throws); `SaleEventPublisher` |
| One notification handler logs `Sale event {EventName} published {@Event}` | `SaleEventLogHandlerTests` (the entry, and dispatch through MediatR); Task 16 Step 6 log lines |
| GetSale throws `KeyNotFoundException` ("The sale with ID … does not exist") | `GetSaleHandlerTests`; the exact 404 body in `GetSaleTests` |
| `Sale` → flat `SaleResult`; the Sales profiles pass `AssertConfigurationIsValid()` | `SaleProfileTests`, `SalesMappingTests` |
| Handler tests with NSubstitute: happy paths, 404, publish-after-save order, no publish on a failed save | `CreateSaleHandlerTests`, `GetSaleHandlerTests` |
| `AddSales` creates exactly §8.1's schema, with soft-delete columns, the unique number index, four more indexes, the `numeric` precisions and `xmin`; Sales schema only; no pending model changes | Task 12 Step 9 (count `11`, the `xmin` line, the SQL); Step 1 above |
| Owned identities in parent columns, `ValueGeneratedNever()`, `Items` through its backing field, `DomainEvents` not mapped | `ExternalIdentityMapping`, `SaleConfiguration`, `SaleItemConfiguration`; the SQL columns; `SaleRepositoryTests` |
| `SaleRepository.CreateAsync` and tracked `GetByIdAsync` with the items, registered in IoC | `SaleRepositoryTests` (the tracking test); the IoC line above |
| Saving and reloading keeps owned types, items, flags and decimal precision | `SaleRepositoryTests` |
| The data reset truncates `Sales` and `SaleItems` and restarts `sale_number_seq` | `DataResetTests` in both projects (Task 13) |
| `[Authorize]` controller; POST 201 with `Location: /api/sales/{id}` and "Sale created successfully"; GET 200; envelope built once | `SalesControllerTests`; `CreateSaleTests` (`Location`, `SaleJson`); `GetSaleTests` |
| No discount fields in requests (R2); no `isDeleted` or `deletedAt` in responses | the discount-fields test in `CreateSaleTests`; `SaleJson`'s exact field lists |
| Missing or invalid JWT → the documented 401 body | `JwtBearerChallengeTests`; `SalesAuthenticationTests` (no token, another key, expired); Task 16 Step 5's last line |
| Functional tests: read-back, the three tiers, 21 items, repeated product, empty list, unknown id, no or invalid token | `CreateSaleTests` (8), `GetSaleTests` (3), `SalesAuthenticationTests` (3) |
| `.http`: create and get with the captured token, a random `saleNumber`, the created id captured | Task 16 Steps 3 and 5 |
| `feature/create-sale`, pull request into `develop`, Conventional Commits, test-first, no attribution | every task's RED step; Steps 4–6 below |

- [ ] **Step 2: Audit the new tests (skill `test-anti-patterns`, report only)**

Run the skill on these new test classes:
- Unit: `ExternalIdentityTests`, `DiscountPolicyTests`, `SaleTests`, `SaleProfileTests`, `CreateSaleCommandValidatorTests`, `CreateSaleHandlerTests`, `GetSaleQueryValidatorTests`, `GetSaleHandlerTests`, `SaleEventLogHandlerTests`, `SalesMappingTests`, `SalesControllerTests` and `JwtBearerChallengeTests`.
- Integration: `SaleRepositoryTests`, and the changed `DataResetTests`.
- Functional: `CreateSaleTests`, `GetSaleTests`, `SalesAuthenticationTests`, and the changed `DataResetTests`.

Include the helpers:
- `SaleTestData` (Unit and Integration) and `CreateSaleCommandTestData`
- `SaleRequestBody`, `SaleResponseBody` and `SaleRequestBodyTestData`
- `SaleJson` and `ApiHttpExtensions`

Expected remarks that are deliberate; report them and change nothing:
- **Unseeded Bogus in the builders.** It's the template's convention (spec §10), and every generated value is valid by construction. The integration sale numbers are six random digits (Task 12 notes).
- **`Received.InOrder` in `CreateSaleHandlerTests`.** Call order is the behaviour under test (rule R14). The project note already covers it.
- **Tests that passed from the start** (the quantity cases and the `4.500` price in Task 5). They pin at the aggregate the rules that Task 4's lines enforce through `DiscountPolicy`.
- **Assertions inside `SignUpAsync`, `LogInAsNewUserAsync` and `CreateSaleAsync`.** They guard setup steps, so a broken sign-up, login or create fails loudly instead of confusing the test that depends on it.
- **`DateTime.UtcNow` bracketing in `SaleTests`** instead of a clock abstraction (Decision 6).
- **Many assertions in the whole-response create test, and `SaleJson`'s field-order checks.** They check one outcome, the create response, and the wire format is the behaviour (Decision 14).
- **`ReceivedCalls()` on the `ILogger` substitute in `SaleEventLogHandlerTests`.** `LogInformation` is an extension method, so the test reads the underlying `Log` call to check the level and the structured entry.
- **The fixed `S-RESET-PROBE` sale number in the functional `DataResetTests`.** The reset right after it empties the table (Task 13).
- **Delegate equality in `JwtBearerChallengeTests`.** It proves the wiring: the challenge event is `WriteResponseAsync`, which the other test covers.

If the skill finds a real problem, fix it in a separate `test(sales): …` commit, then re-run Step 1.

- [ ] **Step 3: Optional code review**

If you want a second look before the pull request, run superpowers:requesting-code-review against `develop..feature/create-sale`, using `clean-code`, `dotnet-best-practices` and `efcore-patterns` as the lenses. Anything it raises that belongs to tickets 06–12 (see the rules) goes into the report, not into this branch.

- [ ] **Step 4: Check the commit messages**

```bash
git log --format=%s develop..feature/create-sale
git log --format=%B develop..feature/create-sale | grep -n -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
```

Expected (plus any `test(sales):` commit from Step 2):

```
docs(http): create a sale and read it back
feat(auth): answer a missing or invalid JWT with the 401 error body
feat(sales): create and get sales over HTTP
test: reset the Sales tables and sale_number_seq between test classes
feat(sales): persist sales with EF Core and the AddSales migration
feat(sales): log published sale events
feat(sales): get a sale by id
feat(sales): create a sale and publish its events after the save
feat(sales): validate the create-sale command
feat(sales): map a sale to a flat SaleResult
feat(sales): record SaleCreatedEvent when a sale is created
feat(sales): reject invalid sales in the domain
feat(sales): create a sale with discounted lines and totals
feat(sales): add the quantity discount policy
feat(sales): add the ExternalIdentity value object
no attribution lines
```

- [ ] **Step 5: Push the branch and open a pull request into `develop`**

This is the superpowers:finishing-a-development-branch step, with the option already fixed: a pull request into `develop` for the user to review. Don't merge it yourself.

Write the pull request body with the Write tool to `/tmp/ticket05-checks/pr-body.md`: two or three sentences on what the ticket delivers (the Goal above), the evidence table from Step 1 with this run's results, and what Steps 2 and 3 reported. No `Co-Authored-By` trailer, no "Generated with" line and no session link. Then:

```bash
git push -u origin feature/create-sale
gh pr create --base develop --head feature/create-sale --title "Ticket 05: create a sale and read it back" --body-file /tmp/ticket05-checks/pr-body.md
```

Expected: the push prints `* [new branch]      feature/create-sale -> feature/create-sale`, and `gh pr create` prints the pull request URL, `https://github.com/PR001-git/developer-store-api/pull/<n>`. If `gh` fails (for example, it isn't logged in; check with `gh auth status`), stop and tell the user: the branch is pushed, so they can open the pull request on GitHub.

- [ ] **Step 6: Verify the pull request**

```bash
gh pr view feature/create-sale --json state,baseRefName,commits --jq '"\(.state) \(.baseRefName) \(.commits | length) commits"'
gh pr view feature/create-sale --json title,body --jq '.title + "\n" + .body' | grep -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
git diff --shortstat develop...feature/create-sale
git status -sb | head -1
git status --short
```

Expected:
- `OPEN develop 15 commits` (16 if Step 2 added a `test(sales):` commit).
- `no attribution lines`.
- ` 69 files changed, 4446 insertions(+), 22 deletions(-)`, or more with a `test(sales):` commit.
- `## feature/create-sale...origin/feature/create-sale` with no `ahead` or `behind`.
- Only `?? docs/superpowers/tickets/`, plus any other tickets' untracked plan files.

Give the user the pull request URL in the Step 8 report. Don't merge it: the user reviews it and merges it on GitHub with **Create a merge commit** (the `--no-ff` equivalent that the later plans' starting-point checks look for). Keep `feature/create-sale`, since the ticket doesn't ask to delete it.

- [ ] **Step 7: Clean up**

```bash
docker compose ps -a
tasklist | grep -i 'Ambev.DeveloperEvaluation.WebApi' || echo "no API process left"
rm -rf /tmp/ticket05-checks
```

Expected: `docker compose ps -a` lists nothing, `no API process left`, and `rm` prints nothing. The Testcontainers containers are gone too: the fixtures dispose of them when their test run ends.

- [ ] **Step 8: Report to the user**

Besides the results, tell the user:
1. **Try the `.http` file once in your editor** (Visual Studio 2022, or VS Code with REST Client) against `docker compose up`. Send the sign-up, the login, the create and the get. Check that `{{$randomInt 100000 999999}}` gives a new number each time, and that `{{saleId}}` resolves from the create response (Decision 17).
2. **POST and GET can differ in trailing zeros** (Decision 15): `10` and `0` after the create, `10.00` and `0.00` after the get, and `createdAt` in 100 ns ticks versus microseconds. They are the same values. Ticket 13's README can mention it.
3. **`dotnet ef` logs a `[FTL] … HostAbortedException` on every command** now that ticket 03 fixed the Serilog filter. It's harmless, but ticket 02's Decision 5 expected nothing extra to be logged. A later change could leave `HostAbortedException` out of the `Log.Fatal` in `Program.cs`; this ticket doesn't.
4. **The rehearsal found a 500:** `"items": [null]` crashed the repeated-product check. It's a 400 now (Decision 7), and a unit test covers it.
5. **`efcore-patterns` conflict, decided for the ticket** (Decision 9): `GetByIdAsync` is tracked, because tickets 07–09 load the aggregate with it, change it and save it.
6. **For the next tickets:**
   - 06: `SaleNumber` becomes `string?` in `CreateSaleRequest` and `CreateSaleCommand`. `sale_number_seq` and the unique index already exist, and both data resets already restart the sequence.
   - 07–09: reuse `ISaleRepository.GetByIdAsync` and `SaleEventPublisher.PublishDomainEventsAsync`, and add one `INotificationHandler<…>` interface per new event to `SaleEventLogHandler`.
   - 09: reuse `SaleValidationRules` and `SaleContractProfile`.
   - None of them adds a migration.
7. **Every (probe) or (derived) output that differed** in this run: say what differed and what superpowers:systematic-debugging found. Say so explicitly if none did.
