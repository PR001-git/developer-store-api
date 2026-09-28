# Update a Sale (Ticket 09) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** An authenticated client replaces a sale with `PUT /api/sales/{id}`. The body is the create body without `saleNumber`, and a `saleNumber` sent anyway is ignored. The date, customer and branch are replaced, and the lines are reconciled by product (R10):
- a sent product updates its active line: new quantity, price and name, and the discount again
- a sent product with no active line gets a new line, even if its only line was cancelled
- an active line whose product isn't sent is cancelled, not deleted
- cancelled lines never change

The response is 200 with the sale. After the save, one `ItemCancelledEvent` per cancelled line and then `SaleModifiedEvent` are logged, even when nothing changed. An unknown id is a 404. Invalid input is a 400 with the create messages. A cancelled sale is a 409 `BusinessRuleViolation`. No migration. Delivered on `feature/update-sale` as a pull request into `develop`.

**Architecture:**
- **Domain.** `Sale.Update(date, customer, branch, items)` checks R7, R5 and R4. It then builds every candidate line, which checks R1 and the price rules, before it changes anything. Then it replaces the header, reconciles the lines, recalculates the total, sets `UpdatedAt` and records `SaleModifiedEvent` last. The private `ReconcileLines` does the reconciling:
  - it cancels a missing line through `CancelLine`, which it now shares with `CancelItem`
  - it updates a kept line through the new internal `SaleItem.UpdateFrom`
  - it adds the candidate as a new line
- **Application.** `UpdateSaleHandler` loads the sale with `GetByIdAsync` (404 if missing), calls `Update`, saves with ticket 07's `UpdateAsync` and publishes through `SaleEventPublisher`. The validator reuses ticket 05's `SaleValidationRules`. `SaleItemInput.ToItemData()` now converts the lines for both the create and the update handler. `SaleEventLogHandler` gains the `SaleModifiedEvent` interface.
- **WebApi.** `SalesController` gains the PUT action, with a new `UpdateSaleRequest` and `UpdateSaleProfile`.
- **Persistence.** EF Core change tracking inserts the new line, because its id is `ValueGeneratedNever`, and writes the replaced owned identities into their parent rows. Task 4 pins both against PostgreSQL.

**Tech Stack:** .NET SDK 10.0.200-preview building `net8.0`; ASP.NET Core 8; MediatR 12.4.1; FluentValidation 11.10.0; AutoMapper 13.0.1; EF Core 8.0.10 with Npgsql 8.0.8; xUnit 2.9.2, FluentAssertions 6.12.0, NSubstitute 5.1.0, Bogus 35.6.1; Testcontainers.PostgreSql 4.15.0 and Microsoft.AspNetCore.Mvc.Testing 8.0.31; Docker Desktop (WSL2) with Compose v2; curl and Python 3 from Git Bash; Slopwatch.Cmd 0.4.2 (global tool).

**Source:** `docs/superpowers/tickets/09-update-a-sale.md`. Spec `docs/superpowers/specs/2026-09-24-sales-api-design.md`: D6, R4, R5, R7, R10, R12, R13, R14, §5.3 (`Update`), §5.4, §6 (UpdateSale), §7.1, §7.2, §7.4 and §8.2. It builds on the code that these plans leave:
- `docs/superpowers/plans/2026-09-26-05-create-a-sale-and-read-it-back.md` (05)
- `docs/superpowers/plans/2026-09-26-07-cancel-a-sale.md` (07)
- `docs/superpowers/plans/2026-09-26-08-cancel-an-item.md` (08)

**Not rehearsed.** The expected outputs come from reading the plans for tickets 05–08. Test counts are the baseline plus a delta, and Task 1 records the baseline. If an output differs, use superpowers:systematic-debugging before changing code.

---

## Rules for every agent executing this plan

Restate these to every subagent you dispatch.

- **No AI attribution anywhere.** No `Co-Authored-By` trailer, "Generated with" line or session link in commits, merge commits, tags or pull requests. Use the commit messages below exactly.
- **Git Bash only**, from `/c/Users/pr000/orca/developer-store-api`. Each Bash call starts a fresh shell.
- **Write file content with the Write and Edit tools**, not heredocs or `sed`. The Write tool needs an earlier Read of the same file.
- **Work in the main checkout, never in a worktree.** `.claude/` (the attribution guard and the project skills) is untracked and exists only here.
- **Stage explicit paths only.** Never `git add -A`, `git add .` or `git commit -a`.
- **Never run `git clean`.** `.git/info/exclude` hides `.claude/`, `.slopwatch/` and the user's two `.doc` notes.
- **Push only for the pull request.** Task 1 pushes `develop` with the plan commit, and Task 8 pushes the branch and opens the pull request. Never push `main`, never force-push, and never merge the pull request.
- **Leave `.doc/brainstorm-show-case.md` and `.doc/template-code-review.md` alone.**
- **No migration.** If `has-pending-model-changes` reports changes, stop and debug.
- **Docker must be running from Task 1.** Don't install or configure Docker or WSL.
- **Always build before `dotnet test --no-build`.**
- **Stop everything you start:** run `docker compose down -v` before a task ends.
- **Nothing else changes.** These are out of scope:
  - listing (10, 11), soft delete (12) and the README (13)
  - the spec §9.2 issues, Users and Auth
  - Docker and compose files, and `appsettings*.json`
  - the fixture classes, including `ApiHttpExtensions`, which this ticket doesn't need to change
  - migrations, and `SaleItemConfiguration.cs`, which Task 4 changes for a moment and restores

## Skills

### Project skills in `.claude/skills/`

| Skill | Use it? | When and how |
|---|---|---|
| `dotnet-best-practices` | **Yes, Tasks 2–6** | Load it before Task 2 and review every C# change against it:<br>• XML docs on public members<br>• the `Application.Sales.UpdateSale` and `WebApi.Features.Sales.UpdateSale` namespaces, one folder per use case<br>• constructor injection into `private readonly` fields<br>• `Given … When … Then …` names with `// Given`, `// When`, `// Then`<br><br>Throw `DomainException` or `KeyNotFoundException` and let the middleware answer. As in tickets 03–08, add no `ArgumentNullException` guards and no `ConfigureAwait(false)`. |
| `efcore-patterns` | **Yes, Task 4** | Load it before Task 4. Its project note describes this ticket's case: `SaveChanges` relies on change tracking to insert new `SaleItem`s and to check the `xmin` token. That's why `GetByIdAsync` stays tracked and `UpdateAsync` only calls `SaveChangesAsync`. `DbContext.Update` isn't used, because it would mark the new line as an existing row (ticket 07, Decision 3). No repository change and no migration. |
| `testcontainers-integration-tests` | **Yes, Tasks 4 and 6** | Load it before Task 4. Reuse ticket 02's fixtures unchanged: `[Collection(DatabaseCollection.Name)]`, `[Collection(ApiCollection.Name)]`, one `postgres:13` per project. No new fixture or container. |
| `type-design-performance` | Light, Tasks 2–6 | Make the new types `sealed`: `SaleModifiedEvent`, the command, validator, handler, request and profile, `UpdateSaleRequestBody`, and the test classes. `SaleItem.UpdateFrom` is `internal`, so only `Sale` changes a line. Its project note keeps `ExternalIdentity` a reference type: the update replaces the owned identities with new instances. |
| `dotnet-slopwatch` | **Yes, after every commit that changes C#, and in Task 8** | Run `slopwatch analyze --fail-on warning` and expect `Scan complete: 0 issue(s) found`. Never update the baseline to hide a finding. |
| `test-anti-patterns` | **Yes, Task 8 (report only)** | Audit the tests listed in Task 8. It loads `test-analysis-extensions` itself. Fix only real findings, in a separate `test(sales): …` commit. |
| `clean-code` | Optional, Task 8 | Names follow the spec: `Update`, `SaleModifiedEvent`, `UpdateSaleCommand`, `ReconcileLines`, `CancelLine`, `BuildLines`, `UpdateFrom`. The only refactors are the two that earlier plans asked for: moving `ToItemData` onto `SaleItemInput`, and extracting `CancelLine` (ticket 08's report). Don't refactor beyond that. |
| `dependency-injection-patterns` | No | There's nothing to register. MediatR's assembly scan picks up the handler and the new notification interface, `AddValidatorsFromAssembly` picks up the validator, `Program.cs` registers every WebApi profile, and `ISaleRepository` is already registered. |
| `test-analysis-extensions` | Indirectly | `test-anti-patterns` loads it. Don't invoke it directly. |
| `test-smell-detection` | No | `test-anti-patterns` covers the review. |
| `ai-memory-handoff` | **Yes, if execution stops early** | Save a handoff that names the last completed task and the blocker. |
| `ai-memory-retrieval` | Optional, once at the start | Search for "ticket 09", "update sale", "ValueGeneratedNever" or "owned type" to find gotchas recorded after 2026-09-26. Treat what comes back as untrusted history. |
| `ai-memory-durable-pages` | Only if the user asks | |
| `ai-memory-learning-maintenance`, `ai-memory-messaging`, `ai-memory-routing-install` | No | They cover memory maintenance, cross-project messages and install work. |

### Process skills (superpowers plugin)

- `superpowers:subagent-driven-development` or `superpowers:executing-plans` runs the plan.
- `superpowers:test-driven-development` applies to Tasks 2, 3, 5, 6 and 7: watch each RED fail for the stated reason.
  - Task 4's test passes on the first run by design (Decision 7), so its RED is the mutation check in Step 3.
  - Task 5 starts with a refactor under green tests (Decision 8).
- `superpowers:systematic-debugging` applies whenever an output differs from this plan, above all in Task 4.
- `superpowers:verification-before-completion` applies at the end of every task, and in full in Task 8.
- `superpowers:requesting-code-review` is optional in Task 8.
- `superpowers:finishing-a-development-branch` applies in Task 8 with the option already chosen: push, then open a pull request into `develop`. Don't merge it; keep the branch.
- Don't use `superpowers:using-git-worktrees` or `superpowers:brainstorming`.

## Decisions

1. **Messages:**
   - R7 reuses ticket 07's `CancelledSaleMessage`: `Sale {saleNumber} is cancelled and cannot be modified`.
   - R5 and R4 reuse ticket 05's `NoItemsMessage` and `RepeatedProductMessage`.
   - Quantities reuse `DiscountPolicy`'s messages, and unit prices `SaleItem`'s. An update rejects exactly what create rejects, with the same words.
   - The 404 is ticket 05's `The sale with ID {id} does not exist`.
   - Success: `Sale updated successfully`.
2. **Nothing changes until every check has passed.** `Update` runs its checks in this order:
   - the cancelled sale (R7)
   - `EnsureValidLines`: no lines, or a repeated product
   - `BuildLines`, which builds every candidate `SaleItem` into a list; each constructor checks the quantity (R1) and the unit price

   Only then does it touch the sale, so a rejected update leaves the header, the lines, the total, `UpdatedAt` and the events as they were. `Create` switches to `BuildLines` too, so both build lines one way.
3. **Reconciliation (R10) in `ReconcileLines`:**
   - It takes a snapshot of the active lines.
   - It cancels each active line whose product isn't sent, in line order, through `CancelLine`. `CancelLine` is extracted from `CancelItem`, as ticket 08's report asked: `item.Cancel()` plus `ItemCancelledEvent`.
   - For each candidate, an active line of the same product takes its values through `SaleItem.UpdateFrom`, which keeps the line's id. Otherwise the candidate is added as a new line, with its own new id.
   - Cancelled lines are never matched or changed. A product whose only line was cancelled therefore gets a new line, and the sale then has two lines for that product, one cancelled and one active. R4 counts active lines only.
   - The payload has at least one line, and every sent product ends with an active line. So an update always leaves an active line and never cancels the sale: R6 holds without `MarkCancelled`.
4. **One timestamp per call.** `Update` reads `DateTime.UtcNow` once and uses it for `UpdatedAt`, each `ItemCancelledEvent.OccurredAt` and `SaleModifiedEvent.OccurredAt`. `SaleModifiedEvent` is recorded last and always, even when nothing changed (§5.3). It carries the new total.
5. **A cancelled line keeps its amounts** as history, as in ticket 08 (its Decision 4). It only stops counting in `TotalAmount`.
6. **The header:**
   - `SaleDate` goes through `ToUtc` (R13).
   - `Customer` and `Branch` are replaced by the handler's new `ExternalIdentity` instances.
   - `Update` has no sale-number parameter, so the number never changes (R12). Ticket 06's Decision 11 left this test to ticket 09.
7. **No persistence change.** `UpdateAsync` (07) saves the tracked aggregate, and it depends on two EF Core behaviours:
   - **The new line is inserted.** Its id is already set, and since EF Core 3.0 an entity that `DetectChanges` finds with a set, generated key is tracked as an existing row. `SaleItemConfiguration`'s `ValueGeneratedNever()` is what makes it an `INSERT` (spec §8.2).
   - **Replaced owned identities are written.** That's the new `Customer` and `Branch`, and a kept line's new `Product`. EF Core turns the old and new owned instances that share one table row into an `UPDATE` of that row's columns.

   Task 4's integration test pins both and passes on the first run. Its mutation check removes `ValueGeneratedNever()` and watches the test fail. Every update writes `UpdatedAt`, so ticket 05's `xmin` token guards concurrent updates as it guards item cancellation (ticket 08, Decision 5), and ticket 03's middleware answers `DbUpdateConcurrencyException` with a 409.
8. **Application:**
   - `UpdateSaleCommand` is a class with setters, like `CreateSaleCommand`. AutoMapper builds it from the request, and the controller sets `Id` from the route.
   - Its validator has create's rules without `SaleNumber`, plus `Id`. `ValidSaleItems` and `MaximumTrimmedLength` come from `SaleValidationRules` (ticket 05, Decision 7), so the line rules aren't duplicated.
   - `SaleItemInput.ToItemData()` replaces `CreateSaleHandler`'s private `ToItemData`, so both handlers convert a line the same way. It's a separate `refactor(sales):` commit, made while ticket 05's create tests stay green.
9. **WebApi:**
   - `UpdateSaleRequest` in `Features/Sales/UpdateSale/` has no `SaleNumber`. System.Text.Json skips unknown properties, as ticket 05's discount-fields test shows, so a `saleNumber` in the body never reaches the command.
   - `UpdateSaleProfile` maps the request and ignores `Id`. The line map is `SaleContractProfile`'s (ticket 05, Decision 13).
   - The route is `[HttpPut("{id}")]` with no `:guid` constraint, like tickets 05–08, so a malformed id is a 400 from model binding.
10. **Functional tests:**
    - They take lines by index from the POST response, which keeps the request order.
    - A PUT response and a later GET come from a loaded sale, so item comparisons are order-insensitive (ticket 08, Decision 7).
    - Before the action exists, `PUT /api/sales/{id}` matches the GET route's path, so routing answers with an empty 405, not a 404.
11. **`.http`:**
    - Two PUTs go after "Get the sale back" and before ticket 07's cancel. The first changes the branch, moves the product to 10 items (20%) and adds a second product. The second drops the first product.
    - A third PUT on the now-cancelled sale (409) goes after ticket 07's "Cancel it again".
    - The curl replay expects this event log: `SaleCreated`, `SaleModified`, `ItemCancelled`, `SaleModified`, `SaleCancelled`.
12. **Works whether or not ticket 06 is merged.** Every Edit anchor below is either ticket 07 or 08 code, or ticket 05 code that ticket 06 leaves alone. `CreateSaleHandler`'s `ToItemData` is the same in both versions. If an anchor isn't found, Read the file and make the same change around the current text.
13. **Delivery, as in tickets 05–08:** commit this plan on `develop` and push it, then open a pull request into `develop`. The user merges it on GitHub with **Create a merge commit**, which is the ticket's `--no-ff`. The branch is kept.

## Pre-existing output (leave it alone)

- `warning NU1903` (AutoMapper 13.0.1): a solution build shows `2 Warning(s)`.
- `NETSDK1057`, because the SDK is a preview.
- `MSB1011` from a bare `dotnet build` at the root. Always pass the `.sln` or a project.
- `LF will be replaced by CRLF` when adding new files.
- `dotnet ef` prints `[FTL] … HostAbortedException` and a stack trace. The commands below filter it out.

## File map

| Change | Paths |
|---|---|
| Created (src) | `src/Ambev.DeveloperEvaluation.Domain/Events/SaleModifiedEvent.cs`; `src/Ambev.DeveloperEvaluation.Application/Sales/UpdateSale/{UpdateSaleCommand, UpdateSaleCommandValidator, UpdateSaleHandler}.cs`; `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/UpdateSale/{UpdateSaleRequest, UpdateSaleProfile}.cs` |
| Modified (src) | `src/Ambev.DeveloperEvaluation.Domain/Entities/{Sale, SaleItem}.cs`; `src/Ambev.DeveloperEvaluation.Application/Sales/{SaleItemInput, SaleEventLogHandler}.cs`; `src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleHandler.cs`; `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs`; `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http` |
| Created (tests) | `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/TestData/UpdateSaleCommandTestData.cs`; `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/UpdateSale/{UpdateSaleCommandValidatorTests, UpdateSaleHandlerTests}.cs`; `tests/Ambev.DeveloperEvaluation.Functional/TestData/UpdateSaleRequestBody.cs`; `tests/Ambev.DeveloperEvaluation.Functional/Sales/UpdateSaleTests.cs` |
| Modified (tests) | `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/{SaleTests, TestData/SaleTestData}.cs`; `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/SaleEventLogHandlerTests.cs`; `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/{SalesMappingTests, SalesControllerTests}.cs`; `tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryTests.cs`; `tests/Ambev.DeveloperEvaluation.Functional/TestData/SaleRequestBodyTestData.cs` |
| Touched and restored | `src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleItemConfiguration.cs` (Task 4, Step 3 only) |
| Committed on `develop` first | this plan |
| Local only | `/tmp/ticket09-checks/` (removed at the end) |

Test deltas: Unit **+31**, Integration **+1**, Functional **+5**.

---

### Task 1: Check the starting point, commit this plan, branch and record the baseline

**Files:**
- Commit on `develop`: `docs/superpowers/plans/2026-09-26-09-update-a-sale.md`

- [ ] **Step 1: Confirm ticket 08 is merged and ticket 09 hasn't started**

```bash
git switch develop
git pull --ff-only origin develop
git diff --quiet && git diff --cached --quiet && echo "no tracked changes"
git status --short
git log --oneline --merges | grep -oE "feature/[a-z-]+" | sort -u
git grep -n -E 'SaleModifiedEvent|UpdateSaleCommand|UpdateFrom\(' -- src tests || echo "nothing of ticket 09 yet"
git branch --list feature/update-sale
```

Expected:
- the pull fast-forwards `develop` or prints `Already up to date.`
- `no tracked changes`
- `?? docs/superpowers/plans/2026-09-26-09-update-a-sale.md` and `?? docs/superpowers/tickets/`, plus other untracked plans (leave them alone)
- the merged branches include `feature/create-sale`, `feature/cancel-sale` and `feature/cancel-sale-item`, plus `feature/sale-number` if ticket 06 is merged
- `nothing of ticket 09 yet`
- no branch listed

Stop and ask the user if:
- the pull fails
- anything tracked is modified
- `feature/cancel-sale-item` isn't merged
- `feature/update-sale` exists; look for an ai-memory handoff first

- [ ] **Step 2: Check the tools and Docker**

```bash
dotnet tool restore 2>&1 | tail -1
command -v slopwatch
docker version --format 'Docker server {{.Server.Version}}' 2>&1 | tail -1
docker info --format '{{.OSType}}' 2>&1 | tail -1
```

Expected: `Restore was successful.`, a slopwatch path, a `Docker server …` line and `linux`. If Docker fails, stop, save a handoff (`ai-memory-handoff`), and ask the user to start Docker Desktop.

- [ ] **Step 3: Commit this plan on `develop` and create the branch**

```bash
git add docs/superpowers/plans/2026-09-26-09-update-a-sale.md
git commit -m "docs: add plan for updating a sale"
git push origin develop
git switch -c feature/update-sale
git log --oneline -1
```

Expected: one file committed and pushed, then `Switched to a new branch 'feature/update-sale'`. If the push is rejected, stop and ask; never force it.

- [ ] **Step 4: Record the baseline**

```bash
mkdir -p /tmp/ticket09-checks
dotnet build Ambev.DeveloperEvaluation.sln --no-incremental 2>&1 | grep -E 'warning CS|Warning\(s\)|Error\(s\)|Build succeeded' | sort -u
dotnet test Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-110 | tee /tmp/ticket09-checks/baseline.txt
slopwatch analyze --fail-on warning 2>&1 | tail -1
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at ' | tail -1
```

Expected:
- `Build succeeded.`, `2 Warning(s)`, `0 Error(s)`, no `warning CS`
- three `Passed!` lines: Unit, Integration and Functional. Call their counts U0, I0 and F0:

  | Merged | Unit | Integration | Functional |
  |---|---|---|---|
  | tickets 05, 07 and 08 | 205 | 12 | 37 |
  | ticket 06 as well | 209 | 18 | 42 |

- `Scan complete: 0 issue(s) found`
- `No changes have been made to the model since the last migration.`

If anything fails, stop.

---

### Task 2: Update a sale in the domain

**Skills:** `dotnet-best-practices` (load it now), `type-design-performance`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/TestData/SaleTestData.cs`
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.Domain/Events/SaleModifiedEvent.cs`
- Modify: `src/Ambev.DeveloperEvaluation.Domain/Entities/SaleItem.cs`
- Modify: `src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs`

- [ ] **Step 1: Let the test builder make a line for a given product**

In `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/TestData/SaleTestData.cs`, replace

```csharp
    public static SaleItemData GenerateItem(int quantity, decimal unitPrice) =>
        new(new ExternalIdentity(Guid.NewGuid(), Faker.Commerce.ProductName()), quantity, unitPrice);
```

with

```csharp
    public static SaleItemData GenerateItem(int quantity, decimal unitPrice) => GenerateItem(Guid.NewGuid(), quantity, unitPrice);

    /// <summary>
    /// Generates a line for the given product, with a random product name. In an update, the id of an existing
    /// line's product changes that line (rule R10).
    /// </summary>
    /// <param name="productId">The id of the product.</param>
    /// <param name="quantity">The quantity of identical items.</param>
    /// <param name="unitPrice">The price of one item.</param>
    /// <returns>The line's input data.</returns>
    public static SaleItemData GenerateItem(Guid productId, int quantity, decimal unitPrice) =>
        new(new ExternalIdentity(productId, Faker.Commerce.ProductName()), quantity, unitPrice);
```

- [ ] **Step 2: Write the failing tests**

In `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs`, replace the end of the file (ticket 08's last test)

```csharp
        act.Should().Throw<DomainException>().WithMessage($"Sale {sale.SaleNumber} has no item with ID {itemId}");
        sale.Items.Should().OnlyContain(item => !item.IsCancelled);
        sale.UpdatedAt.Should().BeNull();
        sale.DomainEvents.Should().BeEmpty();
    }
}
```

with

```csharp
        act.Should().Throw<DomainException>().WithMessage($"Sale {sale.SaleNumber} has no item with ID {itemId}");
        sale.Items.Should().OnlyContain(item => !item.IsCancelled);
        sale.UpdatedAt.Should().BeNull();
        sale.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Tests rules R10 and R13: an update replaces the customer, the branch and the sale date, which is read as UTC
    /// when it has no kind. The sale number stays the same (rule R12).
    /// </summary>
    [Fact(DisplayName = "Given an open sale When updating it with a date without a kind, a new customer and a new branch Then the header is replaced, the date is in UTC and the number is kept")]
    public void Given_OpenSale_When_UpdatingHeader_Then_HeaderReplacedInUtcAndNumberKept()
    {
        // Given
        var sale = SaleTestData.CreateSale();
        var saleNumber = sale.SaleNumber;
        var customer = SaleTestData.GenerateCustomer();
        var branch = SaleTestData.GenerateBranch();

        // When
        sale.Update(new DateTime(2026, 9, 25, 10, 0, 0, DateTimeKind.Unspecified), customer, branch,
            [SaleTestData.GenerateItem(sale.Items.Single().Product.Id, 2, 8.00m)]);

        // Then
        sale.SaleDate.Should().Be(new DateTime(2026, 9, 25, 10, 0, 0, DateTimeKind.Utc));
        sale.SaleDate.Kind.Should().Be(DateTimeKind.Utc);
        sale.Customer.Should().Be(customer);
        sale.Branch.Should().Be(branch);
        sale.SaleNumber.Should().Be(saleNumber);
    }

    /// <summary>
    /// Tests rule R10: an active line whose product is sent keeps its id, and gets the new quantity, price and name
    /// and the discount of the new quantity.
    /// </summary>
    [Fact(DisplayName = "Given an active line of 4 items at 10% When updating its product to 10 items with a new price and name Then the line keeps its id and gets 20% and the new amounts")]
    public void Given_ActiveLineAt10Percent_When_UpdatingItsProductTo10Items_Then_LineKeepsIdAndGets20Percent()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m));
        var line = sale.Items.Single();
        var lineId = line.Id;
        var sent = SaleTestData.GenerateItem(line.Product.Id, 10, 5.00m);

        // When
        UpdateLines(sale, sent);

        // Then (10 × 5.00 = 50.00; 20% of it is 10.00)
        sale.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Id = lineId,
            sent.Product,
            Quantity = 10,
            UnitPrice = 5.00m,
            DiscountPercentage = 20m,
            DiscountAmount = 10.00m,
            TotalAmount = 40.00m,
            IsCancelled = false
        });
    }

    /// <summary>
    /// Tests rule R10: a sent product with no line gets a new active line, with its own new id and its discount.
    /// </summary>
    [Fact(DisplayName = "Given a sale with one line When updating it with that product and a new one Then the new product gets a new active line with its discount")]
    public void Given_SaleWithOneLine_When_UpdatingWithNewProduct_Then_NewProductGetsNewLine()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m));
        var existing = sale.Items.Single();
        var added = SaleTestData.GenerateItem(5, 4.45m);

        // When
        UpdateLines(sale, SaleTestData.GenerateItem(existing.Product.Id, 4, 4.50m), added);

        // Then (5 × 4.45 = 22.25; its 10% is 2.225, which rounds away from zero to 2.23)
        sale.Items.Should().HaveCount(2);
        var newLine = sale.Items.Should().ContainSingle(item => item.Product.Id == added.Product.Id).Subject;
        newLine.Id.Should().NotBe(Guid.Empty).And.NotBe(existing.Id);
        newLine.Should().BeEquivalentTo(new
        {
            added.Product,
            Quantity = 5,
            UnitPrice = 4.45m,
            DiscountPercentage = 10m,
            DiscountAmount = 2.23m,
            TotalAmount = 20.02m,
            IsCancelled = false
        });
    }

    /// <summary>
    /// Tests rule R10 and spec decision D6: an active line whose product isn't sent is cancelled, not removed. It keeps
    /// its amounts as history and no longer counts in the total.
    /// </summary>
    [Fact(DisplayName = "Given a sale with two active lines When updating it without the first product Then that line is cancelled with its amounts kept and leaves the total")]
    public void Given_SaleWithTwoActiveLines_When_UpdatingWithoutFirstProduct_Then_ThatLineIsCancelledAndKeepsAmounts()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m), SaleTestData.GenerateItem(3, 10.00m));
        var dropped = sale.Items.First();
        var kept = sale.Items.Last();

        // When
        UpdateLines(sale, SaleTestData.GenerateItem(kept.Product.Id, 3, 10.00m));

        // Then (the dropped line keeps its 16.20; 30.00 is left)
        sale.Items.Should().HaveCount(2).And.Contain(dropped);
        dropped.Should().BeEquivalentTo(new { Quantity = 4, UnitPrice = 4.50m, DiscountAmount = 1.80m, TotalAmount = 16.20m, IsCancelled = true });
        kept.IsCancelled.Should().BeFalse();
        sale.TotalAmount.Should().Be(30.00m);
    }

    /// <summary>
    /// Tests rule R10: a product whose only line was cancelled gets a new active line. The cancelled line stays as it
    /// was, because cancelled lines never change.
    /// </summary>
    [Fact(DisplayName = "Given a product whose only line was cancelled When updating the sale with it again Then it gets a new active line and the cancelled line stays unchanged")]
    public void Given_ProductWhoseLineWasCancelled_When_UpdatingWithItAgain_Then_GetsNewLineAndCancelledLineUnchanged()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m), SaleTestData.GenerateItem(3, 10.00m));
        var cancelled = sale.Items.First();
        var cancelledProduct = cancelled.Product;
        var active = sale.Items.Last();
        sale.CancelItem(cancelled.Id);

        // When
        UpdateLines(sale,
            SaleTestData.GenerateItem(cancelledProduct.Id, 10, 4.50m),
            SaleTestData.GenerateItem(active.Product.Id, 3, 10.00m));

        // Then (the new line is 10 × 4.50 at 20% = 36.00; with the other 30.00 the total is 66.00)
        sale.Items.Should().HaveCount(3);
        cancelled.Should().BeEquivalentTo(new { Product = cancelledProduct, Quantity = 4, TotalAmount = 16.20m, IsCancelled = true });
        sale.Items.Should().ContainSingle(item => item.Product.Id == cancelledProduct.Id && !item.IsCancelled)
            .Which.Should().BeEquivalentTo(new { Quantity = 10, DiscountPercentage = 20m, TotalAmount = 36.00m });
        sale.TotalAmount.Should().Be(66.00m);
    }

    /// <summary>
    /// Tests rules R3 and R13 after an update: the total is the sum of the active lines, and
    /// <see cref="Sale.UpdatedAt"/> is now, in UTC.
    /// </summary>
    [Fact(DisplayName = "Given an open sale When updating its lines Then the total is the sum of the active lines and UpdatedAt is now in UTC")]
    public void Given_OpenSale_When_UpdatingLines_Then_TotalOfActiveLinesAndUpdatedNowInUtc()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m), SaleTestData.GenerateItem(3, 10.00m));
        var before = DateTime.UtcNow;

        // When (the first line goes to 10 × 4.50 at 20%, the second is dropped, and 2 × 8.00 is added)
        UpdateLines(sale,
            SaleTestData.GenerateItem(sale.Items.First().Product.Id, 10, 4.50m),
            SaleTestData.GenerateItem(2, 8.00m));

        // Then (36.00 + 16.00; the dropped 30.00 doesn't count)
        var after = DateTime.UtcNow;
        sale.TotalAmount.Should().Be(52.00m);
        sale.UpdatedAt.Should().NotBeNull().And.BeOnOrAfter(before).And.BeOnOrBefore(after);
        sale.UpdatedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests the event sequence of §5.3: one <see cref="ItemCancelledEvent"/> per cancelled line, in line order, then
    /// <see cref="SaleModifiedEvent"/> with the new total, all stamped with <see cref="Sale.UpdatedAt"/>.
    /// </summary>
    [Fact(DisplayName = "Given a loaded sale with three lines When updating it with only the third product Then it records ItemCancelledEvent for the first two lines, then SaleModifiedEvent")]
    public void Given_LoadedSaleWithThreeLines_When_UpdatingWithOnlyThirdProduct_Then_RecordsItemCancelledTwiceThenSaleModified()
    {
        // Given (a loaded sale has no recorded events)
        var sale = SaleTestData.CreateSale(
            SaleTestData.GenerateItem(4, 4.50m), SaleTestData.GenerateItem(3, 10.00m), SaleTestData.GenerateItem(2, 8.00m));
        sale.ClearDomainEvents();
        var first = sale.Items.ElementAt(0);
        var second = sale.Items.ElementAt(1);
        var third = sale.Items.ElementAt(2);

        // When
        UpdateLines(sale, SaleTestData.GenerateItem(third.Product.Id, 2, 8.00m));

        // Then
        var updatedAt = sale.UpdatedAt!.Value;
        sale.DomainEvents.Should().Equal(
            new ItemCancelledEvent(sale.Id, sale.SaleNumber, first.Id, first.Product.Id, updatedAt),
            new ItemCancelledEvent(sale.Id, sale.SaleNumber, second.Id, second.Product.Id, updatedAt),
            new SaleModifiedEvent(sale.Id, sale.SaleNumber, 16.00m, updatedAt));
    }

    /// <summary>
    /// Tests §5.3: an update that changes nothing still records <see cref="SaleModifiedEvent"/>, and only that. The
    /// lines keep their ids and amounts, and none is cancelled.
    /// </summary>
    [Fact(DisplayName = "Given a loaded sale When updating it with the same header and lines Then only SaleModifiedEvent is recorded and the lines are unchanged")]
    public void Given_LoadedSale_When_UpdatingWithSameHeaderAndLines_Then_OnlySaleModifiedIsRecorded()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m), SaleTestData.GenerateItem(3, 10.00m));
        sale.ClearDomainEvents();
        var itemsBefore = sale.Items
            .Select(item => new
            {
                item.Id, item.Product, item.Quantity, item.UnitPrice,
                item.DiscountPercentage, item.DiscountAmount, item.TotalAmount, item.IsCancelled
            })
            .ToList();
        var sameLines = sale.Items.Select(item => new SaleItemData(item.Product, item.Quantity, item.UnitPrice)).ToArray();

        // When
        UpdateLines(sale, sameLines);

        // Then (16.20 + 30.00)
        sale.DomainEvents.Should().ContainSingle().Which.Should().Be(
            new SaleModifiedEvent(sale.Id, sale.SaleNumber, 46.20m, sale.UpdatedAt!.Value));
        sale.Items.Should().BeEquivalentTo(itemsBefore, options => options.WithStrictOrdering());
    }

    /// <summary>
    /// Tests rule R7: a cancelled sale is read-only, so an update is rejected and changes nothing.
    /// </summary>
    [Fact(DisplayName = "Given a cancelled sale When updating it Then it throws DomainException and changes nothing")]
    public void Given_CancelledSale_When_Updating_Then_ThrowsAndChangesNothing()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m));
        sale.Cancel();
        sale.ClearDomainEvents();
        var before = StateOf(sale);

        // When
        var act = () => sale.Update(DateTime.UtcNow, SaleTestData.GenerateCustomer(), SaleTestData.GenerateBranch(),
            [SaleTestData.GenerateItem(sale.Items.Single().Product.Id, 10, 4.50m)]);

        // Then
        act.Should().Throw<DomainException>().WithMessage($"Sale {sale.SaleNumber} is cancelled and cannot be modified");
        StateOf(sale).Should().BeEquivalentTo(before);
        sale.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R5 in an update: the lines can't be emptied, and a rejected update changes nothing.
    /// </summary>
    [Fact(DisplayName = "Given an open sale When updating it with no items Then it throws DomainException and changes nothing")]
    public void Given_OpenSale_When_UpdatingWithNoItems_Then_ThrowsAndChangesNothing()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m));
        sale.ClearDomainEvents();
        var before = StateOf(sale);

        // When
        var act = () => sale.Update(DateTime.UtcNow, SaleTestData.GenerateCustomer(), SaleTestData.GenerateBranch(), []);

        // Then
        act.Should().Throw<DomainException>().WithMessage("A sale must have at least one item");
        StateOf(sale).Should().BeEquivalentTo(before);
        sale.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R4 in an update: a product sent twice is rejected, and nothing changes.
    /// </summary>
    [Fact(DisplayName = "Given an open sale When updating it with the same product twice Then it throws DomainException and changes nothing")]
    public void Given_OpenSale_When_UpdatingWithSameProductTwice_Then_ThrowsAndChangesNothing()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m));
        sale.ClearDomainEvents();
        var productId = sale.Items.Single().Product.Id;
        var before = StateOf(sale);

        // When
        var act = () => sale.Update(DateTime.UtcNow, SaleTestData.GenerateCustomer(), SaleTestData.GenerateBranch(),
            [SaleTestData.GenerateItem(productId, 10, 4.50m), SaleTestData.GenerateItem(productId, 2, 4.50m)]);

        // Then
        act.Should().Throw<DomainException>().WithMessage("Each product can appear only once in a sale");
        StateOf(sale).Should().BeEquivalentTo(before);
        sale.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R1 in an update: a quantity outside 1 to 20 is rejected. The valid line sent before it changes
    /// nothing either, because every line is checked before anything changes.
    /// </summary>
    /// <param name="quantity">A quantity outside 1 to 20.</param>
    /// <param name="message">The expected message, the same as create's.</param>
    [Theory(DisplayName = "Given an open sale When updating it with a valid line and one with a quantity outside 1 to 20 Then it throws DomainException and changes nothing")]
    [InlineData(0, "Quantity must be at least 1")]
    [InlineData(21, "It's not possible to sell above 20 identical items")]
    public void Given_OpenSale_When_UpdatingWithQuantityOutside1To20_Then_ThrowsAndChangesNothing(int quantity, string message)
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m));
        sale.ClearDomainEvents();
        var before = StateOf(sale);

        // When
        var act = () => sale.Update(DateTime.UtcNow, SaleTestData.GenerateCustomer(), SaleTestData.GenerateBranch(),
            [SaleTestData.GenerateItem(sale.Items.Single().Product.Id, 10, 4.50m), SaleTestData.GenerateItem(quantity, 4.50m)]);

        // Then
        act.Should().Throw<DomainException>().WithMessage(message);
        StateOf(sale).Should().BeEquivalentTo(before);
        sale.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Updates the sale's lines and keeps its header, for the tests that are about the lines.
    /// </summary>
    private static void UpdateLines(Sale sale, params SaleItemData[] items) =>
        sale.Update(sale.SaleDate, sale.Customer, sale.Branch, items);

    /// <summary>
    /// Captures everything an update can change, so a test can check that a rejected update changed nothing.
    /// </summary>
    private static object StateOf(Sale sale) => new
    {
        sale.SaleDate,
        sale.Customer,
        sale.Branch,
        sale.TotalAmount,
        sale.IsCancelled,
        sale.UpdatedAt,
        Items = sale.Items
            .Select(item => new
            {
                item.Id, item.Product, item.Quantity, item.UnitPrice,
                item.DiscountPercentage, item.DiscountAmount, item.TotalAmount, item.IsCancelled
            })
            .ToList()
    };
}
```

Notes for reviewers:
- `StateOf` returns `object`. FluentAssertions 6 compares an `object` expectation by its runtime type, so both snapshots are compared member by member, lines included.
- The header in each rejection test is new (`DateTime.UtcNow`, a new customer and branch). `StateOf` therefore also proves that the header wasn't replaced.
- In the quantity theory, the first line is valid and would change the existing line. It must stay unchanged (Decision 2).

- [ ] **Step 3: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected: `CS1061: 'Sale' does not contain a definition for 'Update'` (several lines) and `CS0246: The type or namespace name 'SaleModifiedEvent' could not be found`.

- [ ] **Step 4: Write the event**

Create `src/Ambev.DeveloperEvaluation.Domain/Events/SaleModifiedEvent.cs`:

```csharp
namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>
/// A sale was updated: its header was replaced and its lines were reconciled (rule R10). It is recorded on every
/// update, after that update's <see cref="ItemCancelledEvent"/>s, even when nothing changed.
/// </summary>
/// <param name="SaleId">The id of the sale.</param>
/// <param name="SaleNumber">The sale number.</param>
/// <param name="TotalAmount">The sale total after the update.</param>
/// <param name="OccurredAt">When the sale was updated, in UTC.</param>
public sealed record SaleModifiedEvent(Guid SaleId, string SaleNumber, decimal TotalAmount, DateTime OccurredAt) : IDomainEvent;
```

- [ ] **Step 5: Let a line take the values of a new one**

In `src/Ambev.DeveloperEvaluation.Domain/Entities/SaleItem.cs`, replace

```csharp
    internal void Cancel() => IsCancelled = true;
}
```

with

```csharp
    internal void Cancel() => IsCancelled = true;

    /// <summary>
    /// Takes the product, quantity, price, discount and amounts of a line that the sale built from new input, and
    /// keeps its own id (rule R10). The sale calls it only on an active line of the same product.
    /// </summary>
    /// <param name="line">The line built from the new input.</param>
    internal void UpdateFrom(SaleItem line)
    {
        Product = line.Product;
        Quantity = line.Quantity;
        UnitPrice = line.UnitPrice;
        DiscountPercentage = line.DiscountPercentage;
        DiscountAmount = line.DiscountAmount;
        TotalAmount = line.TotalAmount;
    }
}
```

- [ ] **Step 6: Add `Update`, and share `BuildLines` with `Create` and `CancelLine` with `CancelItem`**

In `src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs`, replace

```csharp
        sale._items.AddRange(items.Select(item => new SaleItem(item.Product, item.Quantity, item.UnitPrice)));
```

with

```csharp
        sale._items.AddRange(BuildLines(items));
```

Then replace the end of `CancelItem` (ticket 08)

```csharp
        var now = DateTime.UtcNow;
        item.Cancel();
        RecalculateTotal();
        UpdatedAt = now;
        _domainEvents.Add(new ItemCancelledEvent(Id, SaleNumber, item.Id, item.Product.Id, now));

        if (_items.TrueForAll(line => line.IsCancelled))
            MarkCancelled(now);
    }
```

with

```csharp
        var now = DateTime.UtcNow;
        CancelLine(item, now);
        RecalculateTotal();
        UpdatedAt = now;

        if (_items.TrueForAll(line => line.IsCancelled))
            MarkCancelled(now);
    }

    /// <summary>
    /// Replaces the header and reconciles the lines by product (rule R10):
    /// <list type="bullet">
    /// <item><see cref="SaleDate"/> (in UTC, rule R13), <see cref="Customer"/> and <see cref="Branch"/> are replaced;
    /// the sale number never changes.</item>
    /// <item>An active line whose product is sent gets the new quantity, price and name, and its discount again.</item>
    /// <item>A sent product with no active line gets a new line, even if its only line was cancelled.</item>
    /// <item>An active line whose product isn't sent is cancelled, and an <see cref="ItemCancelledEvent"/> is recorded.</item>
    /// <item>Cancelled lines never change.</item>
    /// </list>
    /// Then the total is recalculated, <see cref="UpdatedAt"/> is set, and a <see cref="SaleModifiedEvent"/> is
    /// recorded last, even when nothing changed.
    /// </summary>
    /// <param name="saleDate">The date and time of the sale. A value without a kind is read as UTC; a local one is converted (rule R13).</param>
    /// <param name="customer">The customer who bought.</param>
    /// <param name="branch">The branch where the sale was made.</param>
    /// <param name="items">The lines the sale must have: at least one, and one per product.</param>
    /// <exception cref="DomainException">
    /// Thrown when the sale is cancelled (rule R7), there are no lines, a product repeats, or a line has a quantity
    /// outside 1 to 20 or an invalid unit price. Nothing changes then.
    /// </exception>
    public void Update(
        DateTime saleDate,
        ExternalIdentity customer,
        ExternalIdentity branch,
        IReadOnlyCollection<SaleItemData> items)
    {
        EnsureNotCancelled();
        EnsureValidLines(items);
        var lines = BuildLines(items);

        var now = DateTime.UtcNow;
        SaleDate = ToUtc(saleDate);
        Customer = customer;
        Branch = branch;
        ReconcileLines(lines, now);
        RecalculateTotal();
        UpdatedAt = now;
        _domainEvents.Add(new SaleModifiedEvent(Id, SaleNumber, TotalAmount, now));
    }
```

Then replace ticket 08's `MarkCancelled`

```csharp
    private void MarkCancelled(DateTime now)
    {
        IsCancelled = true;
        UpdatedAt = now;
        _domainEvents.Add(new SaleCancelledEvent(Id, SaleNumber, now));
    }
```

with

```csharp
    private void MarkCancelled(DateTime now)
    {
        IsCancelled = true;
        UpdatedAt = now;
        _domainEvents.Add(new SaleCancelledEvent(Id, SaleNumber, now));
    }

    private void CancelLine(SaleItem item, DateTime now)
    {
        item.Cancel();
        _domainEvents.Add(new ItemCancelledEvent(Id, SaleNumber, item.Id, item.Product.Id, now));
    }

    private void ReconcileLines(IReadOnlyCollection<SaleItem> candidates, DateTime now)
    {
        // Rule R10. Cancelled lines are history: they are never matched or changed.
        var activeLines = _items.Where(line => !line.IsCancelled).ToList();
        var sentProductIds = candidates.Select(candidate => candidate.Product.Id).ToHashSet();

        foreach (var line in activeLines.Where(line => !sentProductIds.Contains(line.Product.Id)))
            CancelLine(line, now);

        foreach (var candidate in candidates)
        {
            var activeLine = activeLines.Find(line => line.Product.Id == candidate.Product.Id);
            if (activeLine is null)
                _items.Add(candidate);
            else
                activeLine.UpdateFrom(candidate);
        }
    }
```

Then replace

```csharp
    private void RecalculateTotal() =>
```

with

```csharp
    private static List<SaleItem> BuildLines(IReadOnlyCollection<SaleItemData> items) =>
        items.Select(item => new SaleItem(item.Product, item.Quantity, item.UnitPrice)).ToList();

    private void RecalculateTotal() =>
```

`BuildLines` returns a list, so every line is built, and checked, before `Update` changes anything (Decision 2). `CancelItem` now records `ItemCancelledEvent` before it recalculates the total. The events and their order are the same, so ticket 08's tests still pass. A line that `ReconcileLines` just cancelled is never matched afterwards: its product isn't among the sent ones.

- [ ] **Step 7: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~Domain.Entities.SaleTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected:
- `0 Error(s)` and `Build succeeded.`
- SaleTests `Passed:    52`: ticket 08 left 39, and its `CancelItem` tests still pass after the refactor
- Unit U0 + 13

- [ ] **Step 8: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Domain/Events/SaleModifiedEvent.cs src/Ambev.DeveloperEvaluation.Domain/Entities/SaleItem.cs src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/TestData/SaleTestData.cs
git commit -m "feat(sales): update a sale in the domain" -m "Sale.Update replaces the sale date (in UTC), the customer and the branch, and reconciles the lines by product (rule R10). A sent product updates its active line, keeping the line's id, or gets a new line, even one whose only line was cancelled. An active line whose product isn't sent is cancelled and records ItemCancelledEvent, and cancelled lines never change. The total is recalculated, UpdatedAt is set and SaleModifiedEvent is recorded last, even when nothing changed. A cancelled sale, no lines, a repeated product and a quantity outside 1 to 20 are rejected with create's messages before anything changes. Create shares BuildLines, and CancelItem shares CancelLine."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `5 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 3: Log `SaleModifiedEvent`

**Skill:** `dotnet-best-practices`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/SaleEventLogHandlerTests.cs`
- Modify: `src/Ambev.DeveloperEvaluation.Application/Sales/SaleEventLogHandler.cs`

- [ ] **Step 1: Write the failing test**

In `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/SaleEventLogHandlerTests.cs`, replace the end of ticket 08's test

```csharp
            new KeyValuePair<string, object?>("@Event", itemCancelled),
            new KeyValuePair<string, object?>("{OriginalFormat}", "Sale event {EventName} published {@Event}"));
    }
```

with

```csharp
            new KeyValuePair<string, object?>("@Event", itemCancelled),
            new KeyValuePair<string, object?>("{OriginalFormat}", "Sale event {EventName} published {@Event}"));
    }

    /// <summary>
    /// Tests that a <see cref="SaleModifiedEvent"/> published as <see cref="IDomainEvent"/> reaches the log handler
    /// and becomes one structured Information entry.
    /// </summary>
    [Fact(DisplayName = "Given MediatR with the Application handlers When a SaleModifiedEvent is published as IDomainEvent Then the log handler writes one structured Information entry with it")]
    public async Task Given_MediatRWithApplicationHandlers_When_SaleModifiedEventPublished_Then_LogHandlerWritesIt()
    {
        // Given
        var services = new ServiceCollection();
        services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(ApplicationLayer).Assembly));
        services.AddSingleton(_logger);
        await using var provider = services.BuildServiceProvider();
        IDomainEvent saleModified = new SaleModifiedEvent(Guid.NewGuid(), "S-000123", 52.00m, DateTime.UtcNow);

        // When
        await provider.GetRequiredService<IPublisher>().Publish(saleModified);

        // Then
        var logCall = _logger.ReceivedCalls().Should().ContainSingle(call => call.GetMethodInfo().Name == nameof(ILogger.Log)).Subject;
        logCall.GetArguments()[0].Should().Be(LogLevel.Information);
        logCall.GetArguments()[2].Should().BeAssignableTo<IReadOnlyList<KeyValuePair<string, object?>>>().Which.Should().Equal(
            new KeyValuePair<string, object?>("EventName", "SaleModifiedEvent"),
            new KeyValuePair<string, object?>("@Event", saleModified),
            new KeyValuePair<string, object?>("{OriginalFormat}", "Sale event {EventName} published {@Event}"));
    }
```

- [ ] **Step 2: Run it and watch it fail (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~SaleEventLogHandlerTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: the build succeeds; `Failed:     1, Passed:     4`. No handler receives the event, so the logger gets no `Log` call.

- [ ] **Step 3: Handle the event**

In `src/Ambev.DeveloperEvaluation.Application/Sales/SaleEventLogHandler.cs`, replace

```csharp
    INotificationHandler<SaleCancelledEvent>,
    INotificationHandler<ItemCancelledEvent>
```

with

```csharp
    INotificationHandler<SaleCancelledEvent>,
    INotificationHandler<ItemCancelledEvent>,
    INotificationHandler<SaleModifiedEvent>
```

Then replace

```csharp
    public Task Handle(ItemCancelledEvent notification, CancellationToken cancellationToken) => Log(notification);
```

with

```csharp
    public Task Handle(ItemCancelledEvent notification, CancellationToken cancellationToken) => Log(notification);

    /// <summary>
    /// Logs a <see cref="SaleModifiedEvent"/>.
    /// </summary>
    /// <param name="notification">The event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    public Task Handle(SaleModifiedEvent notification, CancellationToken cancellationToken) => Log(notification);
```

- [ ] **Step 4: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~SaleEventLogHandlerTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)`; `Passed:     5`; Unit U0 + 14.

- [ ] **Step 5: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Application/Sales/SaleEventLogHandler.cs tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/SaleEventLogHandlerTests.cs
git commit -m "feat(sales): log SaleModifiedEvent" -m "SaleEventLogHandler also handles SaleModifiedEvent, writing the same structured entry as for the other sale events. It now handles all four events of spec 5.4."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `2 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 4: Pin a sale update against PostgreSQL

**Skills:** `efcore-patterns`, `testcontainers-integration-tests` (load both now).

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryTests.cs`
- Touch and restore: `src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleItemConfiguration.cs` (Step 3 only)

- [ ] **Step 1: Write the integration test**

In `tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryTests.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Integration.Fixtures;
```

with

```csharp
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Ambev.DeveloperEvaluation.Integration.Fixtures;
```

Then replace the end of ticket 08's concurrency test

```csharp
        saved!.IsCancelled.Should().BeFalse();
        saved.TotalAmount.Should().Be(secondItem.TotalAmount);
        saved.Items.Should().ContainSingle(item => item.IsCancelled).Which.Id.Should().Be(firstItemId);
    }
```

with

```csharp
        saved!.IsCancelled.Should().BeFalse();
        saved.TotalAmount.Should().Be(secondItem.TotalAmount);
        saved.Items.Should().ContainSingle(item => item.IsCancelled).Which.Id.Should().Be(firstItemId);
    }

    /// <summary>
    /// Tests rule R10 against PostgreSQL: an update of a loaded sale is saved whole. The new line is inserted with
    /// the id the domain gave it, which <c>ValueGeneratedNever</c> makes possible. The kept line gets its new
    /// quantity and renamed product, the dropped line is stored cancelled, and the owned customer and branch are
    /// replaced.
    /// </summary>
    [Fact(DisplayName = "Given a loaded sale updated with a new header, a changed line, a new line and a dropped line When updating it Then a new context reads every change")]
    public async Task Given_LoadedSaleUpdated_When_Updating_Then_NewContextReadsEveryChange()
    {
        // Given (kept and dropped are the lines as created; the update context loads its own instances)
        var sale = SaleTestData.GenerateValidSale(itemCount: 2);
        var kept = sale.Items.First();
        var dropped = sale.Items.Last();
        await using (var createContext = _database.CreateContext())
        {
            await new SaleRepository(createContext).CreateAsync(sale);
        }

        await using var updateContext = _database.CreateContext();
        var repository = new SaleRepository(updateContext);
        var loaded = await repository.GetByIdAsync(sale.Id);
        var saleDate = new DateTime(2026, 9, 25, 10, 0, 0, DateTimeKind.Utc);
        var customer = new ExternalIdentity(Guid.NewGuid(), "Updated customer");
        var branch = new ExternalIdentity(Guid.NewGuid(), "Updated branch");
        var renamed = new ExternalIdentity(kept.Product.Id, "Renamed product");
        var added = new SaleItemData(new ExternalIdentity(Guid.NewGuid(), "Added product"), 2, 8.00m);
        loaded!.Update(saleDate, customer, branch, [new SaleItemData(renamed, 10, 4.50m), added]);
        var addedId = loaded.Items.Single(item => item.Product.Id == added.Product.Id).Id;

        // When
        await repository.UpdateAsync(loaded);

        // Then (10 × 4.50 at 20% = 36.00, plus 2 × 8.00 = 16.00)
        await using var readContext = _database.CreateContext();
        var saved = await new SaleRepository(readContext).GetByIdAsync(sale.Id);
        saved.Should().NotBeNull();
        saved!.Should().BeEquivalentTo(new
        {
            sale.SaleNumber,
            SaleDate = saleDate,
            Customer = customer,
            Branch = branch,
            TotalAmount = 52.00m,
            IsCancelled = false
        });
        saved.UpdatedAt.Should().BeCloseTo(loaded.UpdatedAt!.Value, TimeSpan.FromMicroseconds(1));
        saved.Items.Should().BeEquivalentTo(new[]
        {
            new
            {
                kept.Id, Product = renamed, Quantity = 10, UnitPrice = 4.50m,
                DiscountPercentage = 20m, DiscountAmount = 9.00m, TotalAmount = 36.00m, IsCancelled = false
            },
            new
            {
                dropped.Id, dropped.Product, dropped.Quantity, dropped.UnitPrice,
                dropped.DiscountPercentage, dropped.DiscountAmount, dropped.TotalAmount, IsCancelled = true
            },
            new
            {
                Id = addedId, added.Product, added.Quantity, added.UnitPrice,
                DiscountPercentage = 0m, DiscountAmount = 0.00m, TotalAmount = 16.00m, IsCancelled = false
            }
        });
    }
```

Notes for reviewers:
- The three anonymous line types have the same members in the same order, so `new[]` gets one element type.
- FluentAssertions compares the `ExternalIdentity` records by value, and decimals by value: `20m` equals the `20.00m` that `numeric(5,2)` gives back.
- The items are compared without order, because a reload doesn't guarantee it.

- [ ] **Step 2: Run it (it passes on the first run)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Integration 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build --filter "FullyQualifiedName~SaleRepositoryTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected: `0 Error(s)`; no failures. Tickets 05 and 07 already provide this behaviour (Decision 7).

If it fails, that's the open EF Core risk of Decision 7. Use superpowers:systematic-debugging to find out which of the two behaviours differs: the new line's `INSERT`, or the replaced owned identities. Don't change the domain to work around it before that.

- [ ] **Step 3: Prove the test can fail (mutation check)**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleItemConfiguration.cs`, replace

```csharp
        builder.Property(item => item.Id).ValueGeneratedNever();
```

with

```csharp
        // builder.Property(item => item.Id).ValueGeneratedNever();
```

Then:

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Integration 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build --filter "FullyQualifiedName~NewContextReadsEveryChange" 2>&1 | grep -E 'Passed!|Failed!|DbUpdateConcurrencyException' | cut -c1-200
git restore src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleItemConfiguration.cs
git status --short src
```

Expected: `Failed:     1`, with a `Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException` line saying the operation `was expected to affect 1 row(s), but actually affected 0 row(s)`. With a generated key already set, EF Core takes the new line for an existing row and sends an `UPDATE` that matches nothing. Another failure, such as the new line missing on reload, also proves the point; a pass doesn't, so stop and debug. After the restore, `git status --short src` prints nothing.

- [ ] **Step 4: Verify (GREEN)**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-110
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at ' | tail -1
```

Expected: `0 Error(s)`; Integration I0 + 1; `No changes have been made to the model since the last migration.`

- [ ] **Step 5: Commit and scan**

```bash
git add tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryTests.cs
git commit -m "test(sales): pin a sale update against PostgreSQL" -m "An updated sale is saved through UpdateAsync: a new context reads the replaced sale date, customer and branch, the kept line with its new quantity, discount and renamed product, the dropped line cancelled with its amounts, and the new line with the id the domain gave it. The new line is inserted only because SaleItem ids are ValueGeneratedNever; without it the save fails."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `1 file changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 5: Update a sale and publish its events after the save

**Skills:** `dotnet-best-practices`; the `test-anti-patterns` project note (`Received.InOrder` is the behaviour under test).

**Files:**
- Modify: `src/Ambev.DeveloperEvaluation.Application/Sales/SaleItemInput.cs`
- Modify: `src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleHandler.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/TestData/UpdateSaleCommandTestData.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/UpdateSale/UpdateSaleCommandValidatorTests.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/UpdateSale/UpdateSaleHandlerTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.Application/Sales/UpdateSale/{UpdateSaleCommand, UpdateSaleCommandValidator, UpdateSaleHandler}.cs`

This task has two commits. First a refactor, with ticket 05's create tests green before and after. Then the use case, test-first.

- [ ] **Step 1: Give `SaleItemInput` its conversion (refactor)**

In `src/Ambev.DeveloperEvaluation.Application/Sales/SaleItemInput.cs`, replace

```csharp
namespace Ambev.DeveloperEvaluation.Application.Sales;
```

with

```csharp
using Ambev.DeveloperEvaluation.Domain.ValueObjects;

namespace Ambev.DeveloperEvaluation.Application.Sales;
```

Then replace

```csharp
    public decimal UnitPrice { get; set; }
}
```

with

```csharp
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Converts the line into the domain's input, which the create and update handlers pass to <c>Sale</c>.
    /// </summary>
    /// <returns>The line's input data.</returns>
    public SaleItemData ToItemData() => new(new ExternalIdentity(ProductId, ProductName), Quantity, UnitPrice);
}
```

- [ ] **Step 2: Use it in `CreateSaleHandler` (refactor)**

In `src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleHandler.cs`, replace

```csharp
            command.Items.Select(ToItemData).ToList());
```

with

```csharp
            command.Items.Select(item => item.ToItemData()).ToList());
```

Then replace the private method at the end of the class

```csharp
    }

    private static SaleItemData ToItemData(SaleItemInput item) =>
        new(new ExternalIdentity(item.ProductId, item.ProductName), item.Quantity, item.UnitPrice);
}
```

with

```csharp
    }
}
```

The file keeps its `Domain.ValueObjects` `using` for `ExternalIdentity`.

- [ ] **Step 3: Verify the refactor changed nothing (still GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~Sales.CreateSale" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)`; `Passed:    27`, or `Passed:    30` with ticket 06 (the validator's 24 and the handler's 3 or 6); Unit U0 + 14.

- [ ] **Step 4: Commit the refactor and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Application/Sales/SaleItemInput.cs src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleHandler.cs
git commit -m "refactor(sales): move ToItemData onto SaleItemInput" -m "SaleItemInput.ToItemData replaces CreateSaleHandler's private ToItemData, so the create and update handlers turn a line into SaleItemData the same way. No behaviour changes."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `2 files changed, …` and `Scan complete: 0 issue(s) found`.

- [ ] **Step 5: Write the command builder and the failing tests**

Create `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/TestData/UpdateSaleCommandTestData.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Bogus;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.TestData;

/// <summary>
/// Generates valid <see cref="UpdateSaleCommand"/> instances with Bogus. Every generated value passes the validator
/// and the domain rules, so a test changes only the value it is about.
/// </summary>
public static class UpdateSaleCommandTestData
{
    private static readonly Faker Faker = new();

    /// <summary>
    /// Generates a valid command for the sale with the given id, with a new header.
    /// </summary>
    /// <param name="id">The id of the sale to update.</param>
    /// <param name="items">The lines the sale must have. With none, the command gets two lines for new products.</param>
    /// <returns>A valid command.</returns>
    public static UpdateSaleCommand GenerateValidCommand(Guid id, params SaleItemInput[] items) => new()
    {
        Id = id,
        SaleDate = Faker.Date.Recent().ToUniversalTime(),
        CustomerId = Guid.NewGuid(),
        CustomerName = Faker.Name.FullName(),
        BranchId = Guid.NewGuid(),
        BranchName = $"Filial {Faker.Address.City()}",
        Items = items.Length > 0 ? items : [CreateSaleCommandTestData.GenerateValidItem(), CreateSaleCommandTestData.GenerateValidItem()]
    };

    /// <summary>
    /// Generates a line for the given product, with a random product name. The id of an existing line's product
    /// changes that line (rule R10).
    /// </summary>
    /// <param name="productId">The id of the product.</param>
    /// <param name="quantity">The quantity of identical items.</param>
    /// <param name="unitPrice">The price of one item.</param>
    /// <returns>The line.</returns>
    public static SaleItemInput GenerateItem(Guid productId, int quantity, decimal unitPrice) => new()
    {
        ProductId = productId,
        ProductName = Faker.Commerce.ProductName(),
        Quantity = quantity,
        UnitPrice = unitPrice
    };
}
```

Create `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/UpdateSale/UpdateSaleCommandValidatorTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.Unit.Application.Sales.TestData;
using FluentValidation.TestHelper;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.UpdateSale;

/// <summary>
/// Contains unit tests for <see cref="UpdateSaleCommandValidator"/>: the id, each header rule, and the shared line
/// rules, which <c>CreateSaleCommandValidatorTests</c> covers one by one. Each invalid case checks that its failure
/// is the only one.
/// </summary>
public sealed class UpdateSaleCommandValidatorTests
{
    private readonly UpdateSaleCommandValidator _validator = new();

    /// <summary>
    /// Tests that a command with valid data has no failures.
    /// </summary>
    [Fact(DisplayName = "Given a valid command When validating Then there are no failures")]
    public void Given_ValidCommand_When_Validating_Then_NoFailures()
    {
        // When
        var result = _validator.TestValidate(UpdateSaleCommandTestData.GenerateValidCommand(Guid.NewGuid()));

        // Then
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Tests that the sale id is required.
    /// </summary>
    [Fact(DisplayName = "Given an empty id When validating Then Id fails as empty")]
    public void Given_EmptyId_When_Validating_Then_IdFails()
    {
        // When
        var result = _validator.TestValidate(UpdateSaleCommandTestData.GenerateValidCommand(Guid.Empty));

        // Then
        result.ShouldHaveValidationErrorFor(c => c.Id).WithErrorMessage("'Id' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that the sale date is required. A missing <c>saleDate</c> in the JSON arrives as <see cref="DateTime.MinValue"/>.
    /// </summary>
    [Fact(DisplayName = "Given no sale date When validating Then SaleDate fails as empty")]
    public void Given_NoSaleDate_When_Validating_Then_SaleDateFails()
    {
        // Given
        var command = UpdateSaleCommandTestData.GenerateValidCommand(Guid.NewGuid());
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
        var command = UpdateSaleCommandTestData.GenerateValidCommand(Guid.NewGuid());
        command.CustomerId = Guid.Empty;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.CustomerId).WithErrorMessage("'Customer Id' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that the customer name has at most 100 characters after trimming.
    /// </summary>
    [Fact(DisplayName = "Given a customer name of 101 characters When validating Then CustomerName fails as too long")]
    public void Given_CustomerNameOf101Characters_When_Validating_Then_CustomerNameFails()
    {
        // Given
        var command = UpdateSaleCommandTestData.GenerateValidCommand(Guid.NewGuid());
        command.CustomerName = new string('c', 101);

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.CustomerName)
            .WithErrorMessage("'Customer Name' must have at most 100 characters after trimming.").Only();
    }

    /// <summary>
    /// Tests that the branch id is required.
    /// </summary>
    [Fact(DisplayName = "Given an empty branch id When validating Then BranchId fails as empty")]
    public void Given_EmptyBranchId_When_Validating_Then_BranchIdFails()
    {
        // Given
        var command = UpdateSaleCommandTestData.GenerateValidCommand(Guid.NewGuid());
        command.BranchId = Guid.Empty;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.BranchId).WithErrorMessage("'Branch Id' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that the branch name is required.
    /// </summary>
    [Fact(DisplayName = "Given a blank branch name When validating Then BranchName fails as empty")]
    public void Given_BlankBranchName_When_Validating_Then_BranchNameFails()
    {
        // Given
        var command = UpdateSaleCommandTestData.GenerateValidCommand(Guid.NewGuid());
        command.BranchName = "   ";

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.BranchName).WithErrorMessage("'Branch Name' must not be empty.").Only();
    }

    /// <summary>
    /// Tests rule R5 through the shared line rules: an update can't empty the sale.
    /// </summary>
    [Fact(DisplayName = "Given no items When validating Then Items fails with the no-items message")]
    public void Given_NoItems_When_Validating_Then_ItemsFails()
    {
        // Given
        var command = UpdateSaleCommandTestData.GenerateValidCommand(Guid.NewGuid());
        command.Items = [];

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.Items).WithErrorMessage("A sale must have at least one item").Only();
    }

    /// <summary>
    /// Tests rule R4 through the shared line rules: a product can be sent only once.
    /// </summary>
    [Fact(DisplayName = "Given two items for the same product When validating Then Items fails with the repeated-product message")]
    public void Given_TwoItemsForSameProduct_When_Validating_Then_ItemsFails()
    {
        // Given
        var command = UpdateSaleCommandTestData.GenerateValidCommand(Guid.NewGuid());
        command.Items[1].ProductId = command.Items[0].ProductId;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.Items).WithErrorMessage("Each product can appear only once in a sale").Only();
    }

    /// <summary>
    /// Tests rule R1's limit through the shared per-line rules, with R1's own message.
    /// </summary>
    [Fact(DisplayName = "Given an item with 21 identical items When validating Then Items[0].Quantity fails with R1's message")]
    public void Given_ItemWith21Items_When_Validating_Then_QuantityFails()
    {
        // Given
        var command = UpdateSaleCommandTestData.GenerateValidCommand(Guid.NewGuid());
        command.Items[0].Quantity = 21;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor("Items[0].Quantity")
            .WithErrorMessage("It's not possible to sell above 20 identical items").Only();
    }
}
```

Create `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/UpdateSale/UpdateSaleHandlerTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Application.Sales.TestData;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using AutoMapper;
using FluentAssertions;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.UpdateSale;

/// <summary>
/// Contains unit tests for <see cref="UpdateSaleHandler"/>. The mapper is the real Sales profile.
/// </summary>
public sealed class UpdateSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly UpdateSaleHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSaleHandlerTests"/> class.
    /// </summary>
    public UpdateSaleHandlerTests()
    {
        var mapper = new MapperConfiguration(config => config.AddProfile<SaleProfile>()).CreateMapper();
        _handler = new UpdateSaleHandler(_saleRepository, _publisher, mapper);
    }

    /// <summary>
    /// Tests that the handler updates the loaded sale with the command's header and lines, saves it, and returns it
    /// reconciled: the changed line at its new tier, the new line, and the dropped line cancelled.
    /// </summary>
    [Fact(DisplayName = "Given a loaded sale When updating it with a new header, a changed line and a new line Then it saves the sale and returns it with the dropped line cancelled")]
    public async Task Given_LoadedSale_When_Updating_Then_SavesAndReturnsReconciledSale()
    {
        // Given (the first line goes to 10 × 4.50 at 20%, the second is dropped, and 2 × 8.00 is added)
        var sale = GivenLoadedSale();
        var changed = sale.Items.First();
        var dropped = sale.Items.Last();
        var added = UpdateSaleCommandTestData.GenerateItem(Guid.NewGuid(), 2, 8.00m);
        var command = UpdateSaleCommandTestData.GenerateValidCommand(sale.Id,
            UpdateSaleCommandTestData.GenerateItem(changed.Product.Id, 10, 4.50m), added);

        // When
        var result = await _handler.Handle(command, CancellationToken.None);

        // Then (36.00 + 16.00; the dropped line keeps its 30.00 but doesn't count)
        await _saleRepository.Received(1).UpdateAsync(sale, Arg.Any<CancellationToken>());
        result.UpdatedAt.Should().NotBeNull();
        result.Should().BeEquivalentTo(new
        {
            sale.Id,
            sale.SaleNumber,
            command.SaleDate,
            command.CustomerId,
            command.CustomerName,
            command.BranchId,
            command.BranchName,
            TotalAmount = 52.00m,
            IsCancelled = false,
            sale.UpdatedAt,
            Items = new[]
            {
                new { ProductId = changed.Product.Id, Quantity = 10, TotalAmount = 36.00m, IsCancelled = false },
                new { ProductId = dropped.Product.Id, Quantity = 3, TotalAmount = 30.00m, IsCancelled = true },
                new { added.ProductId, Quantity = 2, TotalAmount = 16.00m, IsCancelled = false }
            }
        });
    }

    /// <summary>
    /// Tests that an unknown sale is a not-found error, and nothing is saved or published.
    /// </summary>
    [Fact(DisplayName = "Given no sale with the id When updating it Then it throws KeyNotFoundException and saves and publishes nothing")]
    public async Task Given_NoSaleWithId_When_Updating_Then_ThrowsKeyNotFoundException()
    {
        // Given
        var saleId = Guid.NewGuid();
        _saleRepository.GetByIdAsync(saleId, Arg.Any<CancellationToken>()).Returns((Sale?)null);

        // When
        var act = () => _handler.Handle(UpdateSaleCommandTestData.GenerateValidCommand(saleId), CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"The sale with ID {saleId} does not exist");
        await _saleRepository.DidNotReceive().UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        _publisher.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R7: the domain's rejection of a cancelled sale propagates, and nothing is saved or published.
    /// </summary>
    [Fact(DisplayName = "Given a cancelled sale When updating it Then DomainException propagates and nothing is saved or published")]
    public async Task Given_CancelledSale_When_Updating_Then_DomainExceptionAndNothingSaved()
    {
        // Given
        var sale = GivenLoadedSale();
        sale.Cancel();
        sale.ClearDomainEvents();

        // When
        var act = () => _handler.Handle(UpdateSaleCommandTestData.GenerateValidCommand(sale.Id), CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<DomainException>().WithMessage($"Sale {sale.SaleNumber} is cancelled and cannot be modified");
        await _saleRepository.DidNotReceive().UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        _publisher.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R14 and §5.3: <see cref="ItemCancelledEvent"/> for the dropped line, then
    /// <see cref="SaleModifiedEvent"/>, are published after the save, and then the events are cleared.
    /// </summary>
    [Fact(DisplayName = "Given a loaded sale When updating it without its first product Then it publishes ItemCancelledEvent then SaleModifiedEvent after the save and clears the events")]
    public async Task Given_LoadedSale_When_UpdatingWithoutFirstProduct_Then_PublishesBothEventsInOrderAfterSave()
    {
        // Given
        var sale = GivenLoadedSale();
        var dropped = sale.Items.First();
        var kept = sale.Items.Last();
        var command = UpdateSaleCommandTestData.GenerateValidCommand(sale.Id,
            UpdateSaleCommandTestData.GenerateItem(kept.Product.Id, 3, 10.00m));
        var published = CapturePublishedEvents();

        // When
        await _handler.Handle(command, CancellationToken.None);

        // Then
        Received.InOrder(() =>
        {
            _saleRepository.UpdateAsync(sale, Arg.Any<CancellationToken>());
            _publisher.Publish(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
            _publisher.Publish(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
        });
        published.Should().Equal(
            new ItemCancelledEvent(sale.Id, sale.SaleNumber, dropped.Id, dropped.Product.Id, sale.UpdatedAt!.Value),
            new SaleModifiedEvent(sale.Id, sale.SaleNumber, 30.00m, sale.UpdatedAt!.Value));
        sale.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R14: when the save fails, nothing is published.
    /// </summary>
    [Fact(DisplayName = "Given the save throws When updating a sale Then the exception propagates and nothing is published")]
    public async Task Given_SaveThrows_When_Updating_Then_NothingIsPublished()
    {
        // Given
        var sale = GivenLoadedSale();
        _saleRepository.UpdateAsync(sale, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("The database is unavailable"));

        // When
        var act = () => _handler.Handle(UpdateSaleCommandTestData.GenerateValidCommand(sale.Id), CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("The database is unavailable");
        _publisher.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// Makes the repository return an open sale as it would after loading it: no recorded events, and two lines,
    /// 4 × 4.50 at 10% (16.20) and 3 × 10.00 (30.00).
    /// </summary>
    private Sale GivenLoadedSale()
    {
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m), SaleTestData.GenerateItem(3, 10.00m));
        sale.ClearDomainEvents();
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        return sale;
    }

    /// <summary>
    /// Records every event the handler publishes, in order.
    /// </summary>
    private List<IDomainEvent> CapturePublishedEvents()
    {
        var published = new List<IDomainEvent>();
        _publisher.When(publisher => publisher.Publish(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>()))
            .Do(call => published.Add(call.Arg<IDomainEvent>()));
        return published;
    }
}
```

- [ ] **Step 6: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected:
- `CS0234: The type or namespace name 'UpdateSale' does not exist in the namespace 'Ambev.DeveloperEvaluation.Application.Sales'`, in the three new files
- `CS0246` for `UpdateSaleCommand`, `UpdateSaleCommandValidator` and `UpdateSaleHandler`

- [ ] **Step 7: Write the command, its validator and the handler**

Create `src/Ambev.DeveloperEvaluation.Application/Sales/UpdateSale/UpdateSaleCommand.cs`:

```csharp
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

/// <summary>
/// Replaces a sale's date, customer and branch, and reconciles its lines by product (rule R10). It has no sale
/// number, because the number never changes.
/// </summary>
/// <remarks>
/// <see cref="UpdateSaleCommandValidator"/> checks it before the handler runs, through the MediatR <c>ValidationBehavior</c>.
/// </remarks>
public sealed class UpdateSaleCommand : IRequest<SaleResult>
{
    /// <summary>
    /// Gets or sets the id of the sale. The API takes it from the route.
    /// </summary>
    public Guid Id { get; set; }

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
    /// Gets or sets the lines the sale must have: at least one, and one per product. An active line whose product
    /// isn't here is cancelled.
    /// </summary>
    public IReadOnlyList<SaleItemInput> Items { get; set; } = [];
}
```

Create `src/Ambev.DeveloperEvaluation.Application/Sales/UpdateSale/UpdateSaleCommandValidator.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

/// <summary>
/// Validates <see cref="UpdateSaleCommand"/> before its handler runs. It has the create rules without the sale number,
/// plus the sale id. The line rules come from <see cref="SaleValidationRules"/>, shared with create. A failure throws
/// <see cref="ValidationException"/>, which the API returns as 400 <c>ValidationError</c>.
/// </summary>
public sealed class UpdateSaleCommandValidator : AbstractValidator<UpdateSaleCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSaleCommandValidator"/> class with the update rules.
    /// </summary>
    public UpdateSaleCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.SaleDate).NotEmpty();
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.CustomerName).NotEmpty().MaximumTrimmedLength(ExternalIdentity.NameMaxLength);
        RuleFor(command => command.BranchId).NotEmpty();
        RuleFor(command => command.BranchName).NotEmpty().MaximumTrimmedLength(ExternalIdentity.NameMaxLength);
        RuleFor(command => command.Items).ValidSaleItems();
    }
}
```

Create `src/Ambev.DeveloperEvaluation.Application/Sales/UpdateSale/UpdateSaleHandler.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using AutoMapper;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

/// <summary>
/// Handles <see cref="UpdateSaleCommand"/>: the sale replaces its header and reconciles its lines, the repository
/// saves it, and only then are the recorded events published.
/// </summary>
public sealed class UpdateSaleHandler : IRequestHandler<UpdateSaleCommand, SaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IPublisher _publisher;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSaleHandler"/> class.
    /// </summary>
    /// <param name="saleRepository">The sale repository.</param>
    /// <param name="publisher">The MediatR publisher for the recorded events.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    public UpdateSaleHandler(ISaleRepository saleRepository, IPublisher publisher, IMapper mapper)
    {
        _saleRepository = saleRepository;
        _publisher = publisher;
        _mapper = mapper;
    }

    /// <summary>
    /// Loads the sale, updates it, saves it, then publishes its events.
    /// </summary>
    /// <param name="command">The validated command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The sale after the update.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when there is no sale with the id.</exception>
    public async Task<SaleResult> Handle(UpdateSaleCommand command, CancellationToken cancellationToken)
    {
        var sale = await _saleRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"The sale with ID {command.Id} does not exist");

        sale.Update(
            command.SaleDate,
            new ExternalIdentity(command.CustomerId, command.CustomerName),
            new ExternalIdentity(command.BranchId, command.BranchName),
            command.Items.Select(item => item.ToItemData()).ToList());

        await _saleRepository.UpdateAsync(sale, cancellationToken);
        await _publisher.PublishDomainEventsAsync(sale, cancellationToken);

        return _mapper.Map<SaleResult>(sale);
    }
}
```

What the middleware answers when something fails:
- A cancelled sale: `Update` throws `DomainException`, nothing is saved, and the answer is 409 `BusinessRuleViolation`.
- A concurrent save that wins: `UpdateAsync` throws `DbUpdateConcurrencyException`, nothing is published, and the answer is 409 `ConcurrencyConflict`.

- [ ] **Step 8: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~Sales.UpdateSale" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)`; `Passed:    15` (the validator's 10 and the handler's 5); Unit U0 + 29.

- [ ] **Step 9: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Application/Sales/UpdateSale/UpdateSaleCommand.cs src/Ambev.DeveloperEvaluation.Application/Sales/UpdateSale/UpdateSaleCommandValidator.cs src/Ambev.DeveloperEvaluation.Application/Sales/UpdateSale/UpdateSaleHandler.cs tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/TestData/UpdateSaleCommandTestData.cs tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/UpdateSale/UpdateSaleCommandValidatorTests.cs tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/UpdateSale/UpdateSaleHandlerTests.cs
git commit -m "feat(sales): update a sale and publish its events after the save" -m "UpdateSaleCommand has a validator with the create rules minus the sale number, plus the sale id, reusing the shared line rules of SaleValidationRules. UpdateSaleHandler throws KeyNotFoundException for an unknown sale, updates it, saves with UpdateAsync and only then publishes one ItemCancelledEvent per dropped line and SaleModifiedEvent (rule R14)."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `6 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 6: Update a sale over HTTP

**Skills:** `dotnet-best-practices`, `testcontainers-integration-tests`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesMappingTests.cs`
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesControllerTests.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Functional/TestData/UpdateSaleRequestBody.cs`
- Modify: `tests/Ambev.DeveloperEvaluation.Functional/TestData/SaleRequestBodyTestData.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Functional/Sales/UpdateSaleTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/UpdateSale/{UpdateSaleRequest, UpdateSaleProfile}.cs`
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs`

- [ ] **Step 1: Write the failing unit tests**

In `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesMappingTests.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
```

with

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
```

Then replace

```csharp
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
```

with

```csharp
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;
```

Then replace

```csharp
/// Contains unit tests for the WebApi Sales profiles, <see cref="CreateSaleProfile"/> and <see cref="SaleContractProfile"/>.
```

with

```csharp
/// Contains unit tests for the WebApi Sales profiles, <see cref="CreateSaleProfile"/>, <see cref="UpdateSaleProfile"/>
/// and <see cref="SaleContractProfile"/>.
```

Then replace

```csharp
        config.AddProfile<CreateSaleProfile>();
        config.AddProfile<SaleContractProfile>();
    });
```

with

```csharp
        config.AddProfile<CreateSaleProfile>();
        config.AddProfile<UpdateSaleProfile>();
        config.AddProfile<SaleContractProfile>();
    });
```

Then replace the end of ticket 05's create-request test

```csharp
        // Then
        command.Should().BeEquivalentTo(request, options => options.WithStrictOrdering());
    }
```

with

```csharp
        // Then
        command.Should().BeEquivalentTo(request, options => options.WithStrictOrdering());
    }

    /// <summary>
    /// Tests that the update request becomes a command with the same header and lines. The id stays empty: the
    /// controller takes it from the route.
    /// </summary>
    [Fact(DisplayName = "Given an update-sale request When mapping it to UpdateSaleCommand Then the header and every line are copied and the id is left empty")]
    public void Given_UpdateSaleRequest_When_MappingToCommand_Then_HeaderAndLinesAreCopied()
    {
        // Given
        var request = new UpdateSaleRequest
        {
            SaleDate = new DateTime(2026, 9, 25, 10, 0, 0, DateTimeKind.Utc),
            CustomerId = Guid.NewGuid(),
            CustomerName = "Maria Silva",
            BranchId = Guid.NewGuid(),
            BranchName = "Filial Norte",
            Items =
            [
                new SaleItemRequest { ProductId = Guid.NewGuid(), ProductName = "Cerveja 350ml", Quantity = 10, UnitPrice = 4.50m },
                new SaleItemRequest { ProductId = Guid.NewGuid(), ProductName = "Refrigerante 2L", Quantity = 2, UnitPrice = 8.00m }
            ]
        };

        // When
        var command = _configuration.CreateMapper().Map<UpdateSaleCommand>(request);

        // Then
        command.Should().BeEquivalentTo(request, options => options.WithStrictOrdering());
        command.Id.Should().BeEmpty();
    }
```

The configuration test that already exists now covers `UpdateSaleProfile` too: without the `Ignore` of Step 6, `AssertConfigurationIsValid()` would report `Id` as unmapped.

In `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesControllerTests.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
```

with

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
```

Then replace

```csharp
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
```

with

```csharp
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;
```

Then replace

```csharp
            config.AddProfile<CreateSaleProfile>();
            config.AddProfile<SaleContractProfile>();
        }).CreateMapper();
```

with

```csharp
            config.AddProfile<CreateSaleProfile>();
            config.AddProfile<UpdateSaleProfile>();
            config.AddProfile<SaleContractProfile>();
        }).CreateMapper();
```

Then replace the end of ticket 08's controller test

```csharp
            $$"""{"success":true,"message":"Sale item cancelled successfully","data":{{saleJson}}}""");
    }
```

with

```csharp
            $$"""{"success":true,"message":"Sale item cancelled successfully","data":{{saleJson}}}""");
    }

    /// <summary>
    /// Tests that updating a sale sends the command built from the request, with the id from the route, and returns
    /// 200 with the sale in the envelope.
    /// </summary>
    [Fact(DisplayName = "Given a sale id and an update request When updating the sale Then it sends the command with the route id and returns 200 with the sale")]
    public async Task Given_SaleIdAndUpdateRequest_When_UpdatingSale_Then_SendsCommandAndReturns200WithSale()
    {
        // Given (the example sale after an update that moved its line to 10 items at 20%)
        const string updatedSaleJson =
            """{"id":"7f9c2a44-5555-4d1e-8a3b-000000000010","saleNumber":"S-000123","saleDate":"2026-09-24T14:30:00Z","customerId":"3f2b8c1e-1111-4a5b-9c2d-000000000001","customerName":"Maria Silva","branchId":"3f2b8c1e-2222-4a5b-9c2d-000000000002","branchName":"Filial Centro","totalAmount":36.00,"isCancelled":false,"createdAt":"2026-09-24T14:31:02Z","updatedAt":"2026-09-25T10:00:00Z","items":[{"id":"7f9c2a44-6666-4d1e-8a3b-000000000011","productId":"3f2b8c1e-3333-4a5b-9c2d-000000000003","productName":"Cerveja 350ml","quantity":10,"unitPrice":4.50,"discountPercentage":20,"discountAmount":9.00,"totalAmount":36.00,"isCancelled":false}]}""";
        var request = new UpdateSaleRequest
        {
            SaleDate = new DateTime(2026, 9, 24, 14, 30, 0, DateTimeKind.Utc),
            CustomerId = Guid.Parse("3f2b8c1e-1111-4a5b-9c2d-000000000001"),
            CustomerName = "Maria Silva",
            BranchId = Guid.Parse("3f2b8c1e-2222-4a5b-9c2d-000000000002"),
            BranchName = "Filial Centro",
            Items = [new SaleItemRequest { ProductId = Guid.Parse("3f2b8c1e-3333-4a5b-9c2d-000000000003"), ProductName = "Cerveja 350ml", Quantity = 10, UnitPrice = 4.50m }]
        };
        var result = ExampleResult();
        result.TotalAmount = 36.00m;
        result.UpdatedAt = new DateTime(2026, 9, 25, 10, 0, 0, DateTimeKind.Utc);
        result.Items[0].Quantity = 10;
        result.Items[0].DiscountPercentage = 20m;
        result.Items[0].DiscountAmount = 9.00m;
        result.Items[0].TotalAmount = 36.00m;
        UpdateSaleCommand? sent = null;
        _mediator.Send(Arg.Do<UpdateSaleCommand>(command => sent = command), Arg.Any<CancellationToken>())
            .Returns(result);

        // When
        var response = await _controller.UpdateSale(result.Id, request, CancellationToken.None);

        // Then
        sent.Should().BeEquivalentTo(request);
        sent!.Id.Should().Be(result.Id);
        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        MvcJson.Serialize(ok.Value).Should().Be(
            $$"""{"success":true,"message":"Sale updated successfully","data":{{updatedSaleJson}}}""");
    }
```

System.Text.Json writes a `decimal` with its scale, so `36.00m`, `9.00m` and `20m` come out as `36.00`, `9.00` and `20`.

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected:
- `CS0234: The type or namespace name 'UpdateSale' does not exist in the namespace 'Ambev.DeveloperEvaluation.WebApi.Features.Sales'`, in both files
- `CS0246` for `UpdateSaleProfile` and `UpdateSaleRequest`

- [ ] **Step 3: Add the functional test data**

Create `tests/Ambev.DeveloperEvaluation.Functional/TestData/UpdateSaleRequestBody.cs`:

```csharp
namespace Ambev.DeveloperEvaluation.Functional.TestData;

/// <summary>
/// The JSON body of <c>PUT /api/sales/{id}</c> (spec §7.2), as a client writes it: the create body without
/// <c>saleNumber</c>.
/// </summary>
/// <param name="SaleDate">The date and time of the sale.</param>
/// <param name="CustomerId">The id of the customer.</param>
/// <param name="CustomerName">The customer's name.</param>
/// <param name="BranchId">The id of the branch.</param>
/// <param name="BranchName">The branch's name.</param>
/// <param name="Items">The lines the sale must have.</param>
public sealed record UpdateSaleRequestBody(
    DateTime SaleDate,
    Guid CustomerId,
    string CustomerName,
    Guid BranchId,
    string BranchName,
    IReadOnlyList<SaleItemRequestBody> Items);
```

In `tests/Ambev.DeveloperEvaluation.Functional/TestData/SaleRequestBodyTestData.cs`, replace

```csharp
/// Generates create-sale bodies that pass every Sales rule, with Bogus.
```

with

```csharp
/// Generates create-sale and update-sale bodies that pass every Sales rule, with Bogus.
```

Then replace the end of `GenerateValid`

```csharp
            Items: items.Length > 0 ? items : [GenerateItem(faker.Random.Int(1, 20), Math.Round(faker.Random.Decimal(0.01m, 500m), 2))]);
    }
```

with

```csharp
            Items: items.Length > 0 ? items : [GenerateItem(faker.Random.Int(1, 20), Math.Round(faker.Random.Decimal(0.01m, 500m), 2))]);
    }

    /// <summary>
    /// Generates a valid update body: a new header, as <see cref="GenerateValid"/> makes it but without a sale number,
    /// and the given lines. A line with an existing line's product changes that line, and an active line whose product
    /// is left out is cancelled (rule R10).
    /// </summary>
    /// <param name="items">The lines the sale must have. With none, the body gets one random line.</param>
    /// <returns>A valid <see cref="UpdateSaleRequestBody"/>.</returns>
    public static UpdateSaleRequestBody GenerateValidUpdate(params SaleItemRequestBody[] items)
    {
        var sale = GenerateValid(items);
        return new UpdateSaleRequestBody(sale.SaleDate, sale.CustomerId, sale.CustomerName, sale.BranchId, sale.BranchName, sale.Items);
    }
```

- [ ] **Step 4: Write the failing functional tests**

Create `tests/Ambev.DeveloperEvaluation.Functional/Sales/UpdateSaleTests.cs`:

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
/// Contains functional tests for updating a sale with <c>PUT /api/sales/{id}</c>.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class UpdateSaleTests
{
    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSaleTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public UpdateSaleTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// Tests rule R10 over HTTP. One PUT moves a line across a discount tier (4 to 10 items, 10% to 20%), renames its
    /// product, adds a product and drops another. The response is 200 with the new header and the three lines,
    /// the dropped one cancelled, and a read shows the same sale.
    /// </summary>
    [Fact(DisplayName = "Given a sale with two items When putting 10 of the first product, a new product and not the second Then returns 200 with the first at 20%, the new line, the second cancelled and total 52.00, and a read shows the same")]
    public async Task Given_SaleWithTwoItems_When_PuttingReconciledLines_Then_Returns200WithLinesReconciled()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var created = await client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid(
            SaleRequestBodyTestData.GenerateItem(4, 4.50m),
            SaleRequestBodyTestData.GenerateItem(12, 3.10m)));
        var changedItem = created.Items[0];
        var droppedItem = created.Items[1];
        var addedLine = SaleRequestBodyTestData.GenerateItem(2, 8.00m);
        var body = SaleRequestBodyTestData.GenerateValidUpdate(
            SaleRequestBodyTestData.GenerateItem(10, 4.50m) with { ProductId = changedItem.ProductId },
            addedLine);

        // When
        using var response = await client.PutAsJsonAsync($"/api/sales/{created.Id}", body);

        // Then (10 × 4.50 at 20% = 36.00, plus 2 × 8.00 = 16.00; the dropped line keeps its 29.76 but doesn't count)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await SaleJson.ShouldBeSaleEnvelopeAsync(response, "Sale updated successfully");
        var updated = await response.ReadSaleAsync();
        updated.Should().BeEquivalentTo(new
        {
            created.Id,
            created.SaleNumber,
            body.SaleDate,
            body.CustomerId,
            body.CustomerName,
            body.BranchId,
            body.BranchName,
            TotalAmount = 52.00m,
            IsCancelled = false
        });
        updated.UpdatedAt.Should().BeAfter(created.CreatedAt);
        var addedId = updated.Items.Should().ContainSingle(item => item.ProductId == addedLine.ProductId).Subject.Id;
        updated.Items.Should().BeEquivalentTo(new[]
        {
            changedItem with
            {
                ProductName = body.Items[0].ProductName, Quantity = 10,
                DiscountPercentage = 20m, DiscountAmount = 9.00m, TotalAmount = 36.00m
            },
            droppedItem with { IsCancelled = true },
            new SaleItemResponseBody(addedId, addedLine.ProductId, addedLine.ProductName, 2, 8.00m, 0m, 0.00m, 16.00m, false)
        });

        using var read = await client.GetAsync($"/api/sales/{created.Id}");
        read.StatusCode.Should().Be(HttpStatusCode.OK);
        var saved = await read.ReadSaleAsync();
        saved.Should().BeEquivalentTo(updated, options => options.ComparingRecordsByMembers().Excluding(sale => sale.UpdatedAt));
        saved.UpdatedAt.Should().BeCloseTo(updated.UpdatedAt!.Value, TimeSpan.FromMicroseconds(1));
    }

    /// <summary>
    /// Tests rule R7 over HTTP: a cancelled sale is read-only, so an update is a 409 with the documented body.
    /// </summary>
    [Fact(DisplayName = "Given a cancelled sale When putting an update Then returns 409 BusinessRuleViolation")]
    public async Task Given_CancelledSale_When_PuttingUpdate_Then_Returns409BusinessRuleViolation()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var created = await client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid());
        await client.CancelSaleAsync(created.Id);

        // When
        using var response = await client.PutAsJsonAsync($"/api/sales/{created.Id}", SaleRequestBodyTestData.GenerateValidUpdate());

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"BusinessRuleViolation","error":"Business rule violation","detail":"Sale {{created.SaleNumber}} is cancelled and cannot be modified"}""");
    }

    /// <summary>
    /// Tests rule R1's limit over HTTP: 21 identical items are a validation error with R1's message, as on create.
    /// </summary>
    [Fact(DisplayName = "Given a line with 21 identical items When putting the update Then returns 400 ValidationError with R1's message")]
    public async Task Given_LineWith21Items_When_PuttingUpdate_Then_Returns400WithR1Message()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var created = await client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid());
        var body = SaleRequestBodyTestData.GenerateValidUpdate(SaleRequestBodyTestData.GenerateItem(21, 4.50m));

        // When
        using var response = await client.PutAsJsonAsync($"/api/sales/{created.Id}", body);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            """{"type":"ValidationError","error":"Invalid input data","detail":"Items[0].Quantity: It's not possible to sell above 20 identical items"}""");
    }

    /// <summary>
    /// Tests that an unknown id is a 404 with the documented body.
    /// </summary>
    [Fact(DisplayName = "Given an unknown id When putting an update Then returns 404 ResourceNotFound")]
    public async Task Given_UnknownId_When_PuttingUpdate_Then_Returns404ResourceNotFound()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var id = Guid.NewGuid();

        // When
        using var response = await client.PutAsJsonAsync($"/api/sales/{id}", SaleRequestBodyTestData.GenerateValidUpdate());

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"ResourceNotFound","error":"Resource not found","detail":"The sale with ID {{id}} does not exist"}""");
    }

    /// <summary>
    /// Tests rule R12 over HTTP: a <c>saleNumber</c> sent on PUT is ignored, and the sale keeps its number.
    /// </summary>
    [Fact(DisplayName = "Given a body with a saleNumber When putting the update Then returns 200 and the sale keeps its number, and a read shows the same")]
    public async Task Given_BodyWithSaleNumber_When_PuttingUpdate_Then_SaleKeepsItsNumber()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var created = await client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid());
        var body = JsonSerializer.SerializeToNode(
            SaleRequestBodyTestData.GenerateValidUpdate(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();
        body["saleNumber"] = $"S-{Guid.NewGuid():N}";

        // When
        using var response = await client.PutAsJsonAsync($"/api/sales/{created.Id}", body);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.ReadSaleAsync()).SaleNumber.Should().Be(created.SaleNumber);
        using var read = await client.GetAsync($"/api/sales/{created.Id}");
        (await read.ReadSaleAsync()).SaleNumber.Should().Be(created.SaleNumber);
    }
}
```

Notes for reviewers:
- The added line's id is read from the response, because the domain creates it. `ContainSingle` also proves that there's exactly one line for that product.
- The PUT response and the GET both come from the loaded sale, so everything except `updatedAt` is equal. `updatedAt` has 100 ns ticks after the PUT and microseconds after the GET, so it's compared to within 1 µs.

- [ ] **Step 5: Watch the functional tests fail (RED)**

The Unit project still doesn't compile (Step 2), so build only the Functional project:

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Functional 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~UpdateSaleTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | sort | uniq -c | cut -c1-200
```

Expected: the build succeeds; `Failed:     5, Passed:     0`. Each failure is a status check that `found HttpStatusCode.MethodNotAllowed {value: 405}`. The path matches the GET route, but nothing handles PUT yet, so routing answers every request with an empty 405 (Decision 10).

- [ ] **Step 6: Write the request and its profile**

Create `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/UpdateSale/UpdateSaleRequest.cs`:

```csharp
namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;

/// <summary>
/// The body of <c>PUT /api/sales/{id}</c> (spec §7.2): the create body without <c>saleNumber</c>. A
/// <c>saleNumber</c> sent anyway is ignored, like any unknown property, so the number never changes. The Application
/// validator checks the body, not this contract (spec decision D10).
/// </summary>
public sealed class UpdateSaleRequest
{
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
    /// Gets or sets the lines the sale must have: at least one, and one per product. An active line whose product
    /// isn't here is cancelled.
    /// </summary>
    public List<SaleItemRequest> Items { get; set; } = [];
}
```

Create `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/UpdateSale/UpdateSaleProfile.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;

/// <summary>
/// Maps the update-sale request to its command. The id comes from the route, so the controller sets it. The lines use
/// the map in <see cref="SaleContractProfile"/>.
/// </summary>
public sealed class UpdateSaleProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSaleProfile"/> class with the request map.
    /// </summary>
    public UpdateSaleProfile()
    {
        CreateMap<UpdateSaleRequest, UpdateSaleCommand>()
            .ForMember(command => command.Id, options => options.Ignore());
    }
}
```

- [ ] **Step 7: Add the action**

In `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
```

with

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
```

Then replace

```csharp
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
```

with

```csharp
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;
```

Then add the action right after `GetSale` by replacing

```csharp
        return Ok(_mapper.Map<SaleResponse>(result), "Sale retrieved successfully");
    }
```

with

```csharp
        return Ok(_mapper.Map<SaleResponse>(result), "Sale retrieved successfully");
    }

    /// <summary>
    /// Replaces a sale's date, customer and branch, and reconciles its lines by product. A sent product updates its
    /// active line or gets a new one, and an active line whose product isn't sent is cancelled. A <c>saleNumber</c>
    /// in the body is ignored: the number never changes.
    /// </summary>
    /// <param name="id">The id of the sale.</param>
    /// <param name="request">The new header and lines.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 with the sale after the update.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateSale([FromRoute] Guid id, [FromBody] UpdateSaleRequest request, CancellationToken cancellationToken)
    {
        var command = _mapper.Map<UpdateSaleCommand>(request);
        command.Id = id;

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(_mapper.Map<SaleResponse>(result), "Sale updated successfully");
    }
```

- [ ] **Step 8: Verify (GREEN)**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected: `0 Error(s)`; Unit U0 + 31; Functional F0 + 5.

- [ ] **Step 9: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/UpdateSale/UpdateSaleRequest.cs src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/UpdateSale/UpdateSaleProfile.cs src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesMappingTests.cs tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesControllerTests.cs tests/Ambev.DeveloperEvaluation.Functional/TestData/UpdateSaleRequestBody.cs tests/Ambev.DeveloperEvaluation.Functional/TestData/SaleRequestBodyTestData.cs tests/Ambev.DeveloperEvaluation.Functional/Sales/UpdateSaleTests.cs
git commit -m "feat(sales): update a sale over HTTP" -m "PUT /api/sales/{id} takes the create body without saleNumber and returns 200 with the sale (Sale updated successfully). A saleNumber in the body is ignored. An unknown sale is a 404, invalid input a 400 with the create messages, and a cancelled sale a 409 BusinessRuleViolation. The functional tests cover a PUT that moves a line across a discount tier, adds a product and drops another (the dropped line comes back cancelled), and a cancelled sale, 21 items, an unknown id and a sent saleNumber."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `8 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 7: Add the update requests to the `.http` file and replay them against `docker compose up`

**Files:**
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http`

- [ ] **Step 1: Look at the file (RED)**

```bash
grep -c '^PUT ' src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
```

Expected: `0`.

- [ ] **Step 2: Add the two updates after the get**

With the Edit tool, replace

```http
### Get the sale back (200 with the same sale)
GET {{baseUrl}}/api/sales/{{saleId}}
Authorization: Bearer {{token}}
```

with

```http
### Get the sale back (200 with the same sale)
GET {{baseUrl}}/api/sales/{{saleId}}
Authorization: Bearer {{token}}

### Update the sale: a new branch, the first product at 10 items (20%) and a second product (200; totalAmount 52.00)
PUT {{baseUrl}}/api/sales/{{saleId}}
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "saleDate": "2026-09-24T15:00:00Z",
  "customerId": "3f2b8c1e-1111-4a5b-9c2d-000000000001",
  "customerName": "Maria Silva",
  "branchId": "3f2b8c1e-5555-4a5b-9c2d-000000000005",
  "branchName": "Filial Norte",
  "items": [
    { "productId": "3f2b8c1e-3333-4a5b-9c2d-000000000003", "productName": "Cerveja 350ml", "quantity": 10, "unitPrice": 4.50 },
    { "productId": "3f2b8c1e-4444-4a5b-9c2d-000000000004", "productName": "Refrigerante 2L", "quantity": 2, "unitPrice": 8.00 }
  ]
}

### Update it without the first product: its line is cancelled, not deleted (200; totalAmount 16.00)
PUT {{baseUrl}}/api/sales/{{saleId}}
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "saleDate": "2026-09-24T15:00:00Z",
  "customerId": "3f2b8c1e-1111-4a5b-9c2d-000000000001",
  "customerName": "Maria Silva",
  "branchId": "3f2b8c1e-5555-4a5b-9c2d-000000000005",
  "branchName": "Filial Norte",
  "items": [
    { "productId": "3f2b8c1e-4444-4a5b-9c2d-000000000004", "productName": "Refrigerante 2L", "quantity": 2, "unitPrice": 8.00 }
  ]
}
```

- [ ] **Step 3: Add the update of the cancelled sale after ticket 07's second cancel**

With the Edit tool, replace

```http
### Cancel it again: a cancelled sale is read-only (409 BusinessRuleViolation)
PATCH {{baseUrl}}/api/sales/{{saleId}}/cancel
Authorization: Bearer {{token}}
```

with

```http
### Cancel it again: a cancelled sale is read-only (409 BusinessRuleViolation)
PATCH {{baseUrl}}/api/sales/{{saleId}}/cancel
Authorization: Bearer {{token}}

### Update the cancelled sale: a cancelled sale is read-only (409 BusinessRuleViolation)
PUT {{baseUrl}}/api/sales/{{saleId}}
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "saleDate": "2026-09-24T15:00:00Z",
  "customerId": "3f2b8c1e-1111-4a5b-9c2d-000000000001",
  "customerName": "Maria Silva",
  "branchId": "3f2b8c1e-5555-4a5b-9c2d-000000000005",
  "branchName": "Filial Norte",
  "items": [
    { "productId": "3f2b8c1e-4444-4a5b-9c2d-000000000004", "productName": "Refrigerante 2L", "quantity": 2, "unitPrice": 8.00 }
  ]
}
```

Ticket 08's two-item sale follows it unchanged.

- [ ] **Step 4: Check the file (GREEN)**

```bash
git diff --stat | cat
grep -nE '^(GET|PATCH|PUT) \{\{baseUrl\}\}/api/sales/\{\{saleId\}\}' src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http | cut -d: -f2-
```

Expected: `1 file changed, 49 insertions(+)`, then:

```
GET {{baseUrl}}/api/sales/{{saleId}}
PUT {{baseUrl}}/api/sales/{{saleId}}
PUT {{baseUrl}}/api/sales/{{saleId}}
PATCH {{baseUrl}}/api/sales/{{saleId}}/cancel
PATCH {{baseUrl}}/api/sales/{{saleId}}/cancel
PUT {{baseUrl}}/api/sales/{{saleId}}
```

- [ ] **Step 5: Start the stack from nothing**

```bash
docker compose down -v --remove-orphans
docker compose up --build --wait --wait-timeout 300; echo "exit=$?"
```

Expected: `exit=0`. If a port is already allocated, stop and ask the user.

- [ ] **Step 6: Replay the requests with curl**

Keep this in one Bash call. The PUT bodies are the `.http` file's.

```bash
BASE=http://localhost:8080
CHECKS=/tmp/ticket09-checks
curl -s -o /dev/null -w "sign-up %{http_code}\n" -X POST $BASE/api/users -H 'Content-Type: application/json' --data-raw '{"username":"admin","password":"Admin@123","phone":"+5511999999999","email":"admin@developerstore.com","status":"Active","role":"Admin"}'
curl -s -o $CHECKS/login.json -X POST $BASE/api/auth -H 'Content-Type: application/json' --data-raw '{"email":"admin@developerstore.com","password":"Admin@123"}'
TOKEN=$(python3 -c "import json, sys; print(json.load(open(sys.argv[1]))['data']['token'])" $CHECKS/login.json)
NUMBER="S-$(( RANDOM % 900000 + 100000 ))"
curl -s -o $CHECKS/create.json -w "create %{http_code}\n" -X POST $BASE/api/sales -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' --data-raw "{\"saleNumber\":\"$NUMBER\",\"saleDate\":\"2026-09-24T14:30:00Z\",\"customerId\":\"3f2b8c1e-1111-4a5b-9c2d-000000000001\",\"customerName\":\"Maria Silva\",\"branchId\":\"3f2b8c1e-2222-4a5b-9c2d-000000000002\",\"branchName\":\"Filial Centro\",\"items\":[{\"productId\":\"3f2b8c1e-3333-4a5b-9c2d-000000000003\",\"productName\":\"Cerveja 350ml\",\"quantity\":5,\"unitPrice\":4.50}]}"
SALE_ID=$(python3 -c "import json, sys; print(json.load(open(sys.argv[1]))['data']['id'])" $CHECKS/create.json)
HEADER='"saleDate":"2026-09-24T15:00:00Z","customerId":"3f2b8c1e-1111-4a5b-9c2d-000000000001","customerName":"Maria Silva","branchId":"3f2b8c1e-5555-4a5b-9c2d-000000000005","branchName":"Filial Norte"'
BEER='{"productId":"3f2b8c1e-3333-4a5b-9c2d-000000000003","productName":"Cerveja 350ml","quantity":10,"unitPrice":4.50}'
SODA='{"productId":"3f2b8c1e-4444-4a5b-9c2d-000000000004","productName":"Refrigerante 2L","quantity":2,"unitPrice":8.00}'
TOO_MANY='{"productId":"3f2b8c1e-4444-4a5b-9c2d-000000000004","productName":"Refrigerante 2L","quantity":21,"unitPrice":8.00}'
SUMMARY="import json, sys; d = json.load(open(sys.argv[1]))['data']; print('number kept', d['saleNumber'] == sys.argv[2], 'branch', d['branchName'], 'total', d['totalAmount'], 'lines', len(d['items']), 'cancelled lines', sum(i['isCancelled'] for i in d['items']))"
curl -s -o $CHECKS/update.json -w "update %{http_code} " -X PUT "$BASE/api/sales/$SALE_ID" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' --data-raw "{$HEADER,\"items\":[$BEER,$SODA]}"
python3 -c "$SUMMARY" $CHECKS/update.json "$NUMBER"
curl -s -o $CHECKS/drop.json -w "drop the first product %{http_code} " -X PUT "$BASE/api/sales/$SALE_ID" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' --data-raw "{$HEADER,\"items\":[$SODA]}"
python3 -c "$SUMMARY" $CHECKS/drop.json "$NUMBER"
curl -s -o $CHECKS/get.json -w "get %{http_code} " "$BASE/api/sales/$SALE_ID" -H "Authorization: Bearer $TOKEN"
python3 -c "$SUMMARY" $CHECKS/get.json "$NUMBER"
curl -s -w " %{http_code}\n" -X PUT "$BASE/api/sales/$SALE_ID" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' --data-raw "{$HEADER,\"items\":[$TOO_MANY]}"
curl -s -w " %{http_code}\n" -X PUT "$BASE/api/sales/3f2b8c1e-9999-4a5b-9c2d-000000000099" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' --data-raw "{$HEADER,\"items\":[$SODA]}"
curl -s -o /dev/null -w "cancel %{http_code}\n" -X PATCH "$BASE/api/sales/$SALE_ID/cancel" -H "Authorization: Bearer $TOKEN"
curl -s -w " %{http_code}\n" -X PUT "$BASE/api/sales/$SALE_ID" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' --data-raw "{$HEADER,\"items\":[$SODA]}"
```

Expected (the sale number varies; Python prints the totals as `52.0` and `16.0`):

```
sign-up 201
create 201
update 200 number kept True branch Filial Norte total 52.0 lines 2 cancelled lines 0
drop the first product 200 number kept True branch Filial Norte total 16.0 lines 2 cancelled lines 1
get 200 number kept True branch Filial Norte total 16.0 lines 2 cancelled lines 1
{"type":"ValidationError","error":"Invalid input data","detail":"Items[0].Quantity: It's not possible to sell above 20 identical items"} 400
{"type":"ResourceNotFound","error":"Resource not found","detail":"The sale with ID 3f2b8c1e-9999-4a5b-9c2d-000000000099 does not exist"} 404
cancel 200
{"type":"BusinessRuleViolation","error":"Business rule violation","detail":"Sale S-… is cancelled and cannot be modified"} 409
```

- [ ] **Step 7: Check the event log and its order**

```bash
docker compose logs --no-log-prefix ambev.developerevaluation.webapi 2>&1 | grep -oE 'Sale event [A-Za-z]+ published'
```

Expected:

```
Sale event SaleCreatedEvent published
Sale event SaleModifiedEvent published
Sale event ItemCancelledEvent published
Sale event SaleModifiedEvent published
Sale event SaleCancelledEvent published
```

The first update cancels no line, so it logs only `SaleModifiedEvent`. The second logs `ItemCancelledEvent`, then `SaleModifiedEvent`. The rejected requests (400, 404, 409) published nothing.

- [ ] **Step 8: Take the stack down**

```bash
docker compose down -v
docker compose ps -a
```

Expected: `ps -a` lists nothing.

- [ ] **Step 9: Commit**

```bash
git add src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
git commit -m "docs(http): update a sale" -m "The .http file updates the created sale twice: first with a new branch, the product at 10 items and a second product, then without the first product, whose line is cancelled. After the sale is cancelled, a third update shows the 409."
git show --stat --format= HEAD | tail -1
```

Expected: `1 file changed, 49 insertions(+)`. No slopwatch run, because no C# changed.

---

### Task 8: Verify everything, review, then open a pull request into `develop`

**Skills:** superpowers:verification-before-completion, `dotnet-slopwatch`, `test-anti-patterns` (report only), `clean-code` and superpowers:requesting-code-review (optional), superpowers:finishing-a-development-branch.

**Files:** none, unless Step 2 finds a real problem.

- [ ] **Step 1: Re-run every check fresh**

```bash
git status --short
dotnet build Ambev.DeveloperEvaluation.sln --no-incremental 2>&1 | grep -E 'warning CS|Warning\(s\)|Error\(s\)|Build succeeded' | sort -u
dotnet test Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-110
cat /tmp/ticket09-checks/baseline.txt
slopwatch analyze --fail-on warning 2>&1 | tail -1
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at ' | tail -1
```

Expected:
- only `?? docs/superpowers/tickets/` and other untracked plans
- `0 Error(s)`, `2 Warning(s)`, `Build succeeded.`, no `warning CS`
- Unit U0 + 31, Integration I0 + 1, Functional F0 + 5, compared against the baseline file. That's 236, 13 and 42 with tickets 05, 07 and 08; 240, 19 and 47 with ticket 06 as well
- `Scan complete: 0 issue(s) found`
- `No changes have been made to the model since the last migration.`

| Ticket criterion | Evidence |
|---|---|
| `Sale.Update` replaces `SaleDate` (in UTC), `Customer` and `Branch` | `SaleTests` (Task 2); `SaleRepositoryTests` (Task 4) |
| Reconciliation (R10): update a line, add a line (including a re-added product), cancel a missing line, never change a cancelled line | `SaleTests` (Task 2); `SaleRepositoryTests` (Task 4); `UpdateSaleTests` (Task 6) |
| Totals recomputed and `UpdatedAt` set; one `ItemCancelledEvent` per cancelled line, then `SaleModifiedEvent` | `SaleTests`, `UpdateSaleHandlerTests`; Task 7 Step 7 |
| `DomainException` for a cancelled sale, no items, a repeated product and a quantity outside 1–20, with create's messages | `SaleTests` (Task 2) |
| Validator with create's rules minus `SaleNumber`, sharing the line rules; the handler loads (404), updates, saves, then publishes | `UpdateSaleCommandValidatorTests`, `UpdateSaleHandlerTests` (Task 5) |
| The event-logging handler logs `SaleModifiedEvent` | `SaleEventLogHandlerTests` (Task 3); Task 7 Step 7 |
| PUT returns 200 with the sale, 404 for an unknown id, 400 for invalid input, 409 for a cancelled sale; a `saleNumber` in the body doesn't change the number | `SalesControllerTests`, `SalesMappingTests`, `UpdateSaleTests` (Task 6); Task 7 Step 6 |
| Unit tests: each reconciliation case, the event sequence, a no-change PUT still emitting `SaleModified`, the cancelled-sale rejection | `SaleTests` (Task 2) |
| Functional tests: a quantity across a discount tier, an added product and a dropped one shown cancelled; a cancelled sale (409); 21 items (400) | `UpdateSaleTests` (Task 6) |
| The `.http` file gains the update request | Task 7 |
| `feature/update-sale`, a pull request into `develop`, Conventional Commits, no attribution | Steps 4–6 below |

- [ ] **Step 2: Audit the tests (skill `test-anti-patterns`, report only)**

Run it on:
- the new classes: `UpdateSaleCommandValidatorTests`, `UpdateSaleHandlerTests` and `UpdateSaleTests`
- the new tests and helpers in `SaleTests`, including `UpdateLines` and `StateOf`
- the new tests in `SaleEventLogHandlerTests`, `SalesMappingTests`, `SalesControllerTests` and `SaleRepositoryTests`
- the test data: `SaleTestData.GenerateItem(Guid, …)`, `UpdateSaleCommandTestData`, `SaleRequestBodyTestData.GenerateValidUpdate` and `UpdateSaleRequestBody`

These findings are deliberate; report them and leave them:
- `Received.InOrder` in `UpdateSaleHandlerTests`: the project note says call order is the behaviour.
- The `DateTime.UtcNow` bracketing in `SaleTests`: there's no clock abstraction (ticket 05, Decision 6).
- `StateOf` compared with `BeEquivalentTo`: one snapshot checks the whole "changes nothing" outcome of a rejected update.
- The log-handler test repeats ticket 07's shape for another event type.
- Several assertions in the functional reconciliation test and the integration test: each checks one outcome, the reconciled sale.
- The functional test reads the new line's id from the response: the domain creates it, and `ContainSingle` asserts there's exactly one.

Fix a real finding in a separate `test(sales): …` commit, then re-run Step 1.

- [ ] **Step 3: Optional code review**

Run superpowers:requesting-code-review on `develop..feature/update-sale`, with `clean-code`, `dotnet-best-practices` and `efcore-patterns` as lenses. Findings that belong to tickets 10–13 go into the report, not this branch.

- [ ] **Step 4: Check the commit messages**

```bash
git log --format=%s develop..feature/update-sale
git log --format=%B develop..feature/update-sale | grep -n -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
```

Expected (plus any `test(sales):` commit from Step 2):

```
docs(http): update a sale
feat(sales): update a sale over HTTP
feat(sales): update a sale and publish its events after the save
refactor(sales): move ToItemData onto SaleItemInput
test(sales): pin a sale update against PostgreSQL
feat(sales): log SaleModifiedEvent
feat(sales): update a sale in the domain
no attribution lines
```

- [ ] **Step 5: Push the branch and open a pull request into `develop`**

Write the pull request body with the Write tool to `/tmp/ticket09-checks/pr-body.md`. It holds:
- the Goal in two or three sentences
- the evidence table from Step 1, with this run's results
- what Steps 2 and 3 reported

No attribution lines. Then:

```bash
git push -u origin feature/update-sale
gh pr create --base develop --head feature/update-sale --title "Ticket 09: update a sale" --body-file /tmp/ticket09-checks/pr-body.md
```

Expected: the push creates `origin/feature/update-sale`, and `gh pr create` prints the pull request URL. If `gh` fails, check `gh auth status`, then stop and tell the user that the branch is pushed so they can open the pull request on GitHub.

- [ ] **Step 6: Verify the pull request**

```bash
gh pr view feature/update-sale --json state,baseRefName,commits --jq '"\(.state) \(.baseRefName) \(.commits | length) commits"'
gh pr view feature/update-sale --json title,body --jq '.title + "\n" + .body' | grep -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
git status -sb | head -1
```

Expected:
- `OPEN develop 7 commits`, or 8 with a Step 2 fix
- `no attribution lines`
- `## feature/update-sale...origin/feature/update-sale`, with no ahead or behind

Don't merge it, and keep the branch.

- [ ] **Step 7: Clean up**

```bash
docker compose ps -a
tasklist | grep -i 'Ambev.DeveloperEvaluation.WebApi' || echo "no API process left"
rm -rf /tmp/ticket09-checks
```

Expected: nothing listed, then `no API process left`.

- [ ] **Step 8: Report to the user**

1. The results of Step 1 and the pull request URL. List every output that differed from this plan, with what systematic-debugging found, or say that none differed. Say explicitly how Task 4 went: whether the integration test passed on the first run, and whether the mutation check failed with `DbUpdateConcurrencyException`. Those confirm the two EF Core behaviours of Decision 7.
2. Ask the user to run the new `.http` requests once from their editor. The first PUT should show `totalAmount` 52.00, the second 16.00 with the first line cancelled, and the third a 409.
3. A note for tickets 12 and 13: an update never cancels the sale, because R5 keeps at least one line and every sent product ends with an active line (Decision 3). After a re-added product, a sale can hold two lines for one product, one cancelled and one active. The README should say so next to R4, which counts active lines only.
