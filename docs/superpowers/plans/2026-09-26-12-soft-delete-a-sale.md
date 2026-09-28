# Soft-Delete a Sale (Ticket 12) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** An authenticated client deletes a sale with `DELETE /api/sales/{id}` and gets 200 with exactly `{"success":true,"message":"Sale deleted successfully"}`. From then on, `GET` returns 404, the list omits the sale, and deleting it again returns 404. Its row and items stay in the database, and no event is published. Cancelled sales can be deleted too. A deleted sale's number stays taken, both for a client that sends it and for the generator. No migration. Delivered on `feature/delete-sale` as a pull request into `develop`.

**Architecture:**
- **Domain:** `Sale.Delete()` sets `IsDeleted`, `DeletedAt` and `UpdatedAt` and records no event.
- **ORM:**
  - `SaleConfiguration` adds the global query filter `!IsDeleted`, so `GetByIdAsync` and `ListAsync` don't change.
  - `ExistsBySaleNumberAsync` calls `IgnoreQueryFilters()`. The generator reaches the database only through it (ticket 06, Decision 3).
  - `DefaultContext.OnConfiguring` ignores `PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning`, with a comment saying why.
- **Application:** `DeleteSaleCommand : IRequest<Unit>`, its validator, and `DeleteSaleHandler`, which loads the sale (404), deletes it and saves it with ticket 07's `UpdateAsync`.
- **WebApi:** `SalesController.DeleteSale` answers through ticket 03's `Ok(message)`.

**Tech Stack:** .NET SDK 10.0.200-preview building `net8.0`; ASP.NET Core 8; MediatR 12.4.1; FluentValidation 11.10.0; AutoMapper 13.0.1; EF Core 8.0.10 with Npgsql 8.0.8; xUnit 2.9.2, FluentAssertions 6.12.0, NSubstitute 5.1.0, Bogus 35.6.1; Testcontainers.PostgreSql 4.15.0, Microsoft.AspNetCore.Mvc.Testing 8.0.31; Docker Desktop (WSL2), Compose v2; curl and Python 3 from Git Bash; Slopwatch.Cmd 0.4.2 (global tool).

**Source:** `docs/superpowers/tickets/12-soft-delete-a-sale.md`. The spec, `docs/superpowers/specs/2026-09-24-sales-api-design.md`, covers it in D7, R7, R11, R12, R13, §5.3 (`Delete`), §6 (DeleteSale and the repository contract) and §8.2 (query filter). The plan builds on the code the plans for tickets 02, 03, 05, 06, 07 and 10 leave behind.

**Not rehearsed.** The expected outputs come from reading the plans for tickets 02–11 and the EF Core 8.0.10 source, not from a run. Test counts are baseline + delta, and Task 1 records the baseline. If an output differs, use superpowers:systematic-debugging before changing code.

---

## Rules for every agent executing this plan

Restate these to every subagent you dispatch.

- **No AI attribution anywhere.** No `Co-Authored-By` trailer, "Generated with" line or session link in commits, merge commits, tags or pull requests. Use the commit messages below exactly.
- **Git Bash only**, from `/c/Users/pr000/orca/developer-store-api`. Each Bash call starts a fresh shell.
- **Write file content with the Write and Edit tools**, not heredocs or `sed`. Edit keeps the BOM and CRLF of template files (`DefaultContext.cs`, the `.http` file). Write needs an earlier Read of the same file.
- **Main checkout only, never a worktree.** `.claude/` (the attribution guard and the project skills) is untracked and exists only here.
- **Stage explicit paths only.** Never `git add -A`, `git add .` or `git commit -a`. Never run `git clean`.
- **Push only for the pull request:** push `develop` with the plan commit (Task 1), then the branch (Task 8). Never push `main`, never force-push, never merge the pull request.
- **Leave `.doc/brainstorm-show-case.md` and `.doc/template-code-review.md` alone.**
- **No migration.** Ticket 05 created `IsDeleted` and `DeletedAt`. If `has-pending-model-changes` reports changes, stop and debug.
- **Docker must be running from Task 1.** Don't install or configure Docker or WSL.
- **Always build before `dotnet test --no-build`.** If you started `docker compose`, run `docker compose down -v` before the task ends.
- **Edit anchors.** Every anchor below is code from ticket 05, 06, 07 or 10. If an anchor isn't found, Read the file and make the same change around the current text.
- **Nothing else changes.** Out of scope:
  - the README (13)
  - `SaleNumberGenerator.cs` (see Decision 3)
  - the spec §9.2 issues, Users and Auth
  - Docker and compose files, `appsettings*.json`, fixture classes, migrations

## Skills

### Project skills in `.claude/skills/`

| Skill | Use it? | When and how |
|---|---|---|
| `dotnet-best-practices` | **Yes, Tasks 2–6** | Load it before Task 2 and review every C# change against it: XML docs on public members, the `Application.Sales.DeleteSale` namespace, one folder per use case (command, validator, handler), constructor injection into `private readonly` fields, and `Given … When … Then …` names with `// Given`, `// When`, `// Then`. Throw `KeyNotFoundException` and let ticket 03's middleware answer. As in tickets 03–11: no `ArgumentNullException` guards and no `ConfigureAwait(false)`. `DefaultContext` keeps the template's style (no XML docs on its members). |
| `efcore-patterns` | **Yes, Tasks 3 and 4** | Load it before Task 3. Its project note keeps change tracking the default: the handler deletes through the tracked `GetByIdAsync` and `UpdateAsync`, with no `Update()` and no `Remove()`. The filter lives in the model (`HasQueryFilter`), and `IgnoreQueryFilters()` is used only where a deleted row must count. Confirm there's no migration with `has-pending-model-changes`. |
| `testcontainers-integration-tests` | **Yes, Tasks 3 and 6** | Load it before Task 3. Reuse ticket 02's fixtures unchanged: `[Collection(DatabaseCollection.Name)]`, `[Collection(ApiCollection.Name)]`, one `postgres:13` per project. The two new classes reset the data in `InitializeAsync`, as tickets 10 and 11 do, so counts and sequence values are exact. No new fixture or container. |
| `type-design-performance` | Light, Tasks 2–6 | New types are `sealed`: `DeleteSaleCommand` (a `sealed record`), the validator, the handler and the test classes. |
| `dotnet-slopwatch` | **Yes, after every commit that changes C#, and in Task 8** | Run `slopwatch analyze --fail-on warning` and expect `Scan complete: 0 issue(s) found`. The ignored EF Core warning is an EF `ConfigureWarnings` call, not a `#pragma` or a `NoWarn`. If slopwatch flags it anyway, report it in Task 8 and don't change the baseline. |
| `test-anti-patterns` | **Yes, Task 8 (report only)** | Audit the tests listed in Task 8 Step 2. It loads `test-analysis-extensions` itself. Fix only real findings, in a separate `test(sales): …` commit. |
| `clean-code` | Optional, Task 8 | Names follow the spec: `Delete`, `DeleteSaleCommand`, `DeleteSaleHandler`, `DeleteSale`. No refactoring beyond the ticket. |
| `dependency-injection-patterns` | No | Nothing to register: MediatR's assembly scan picks up the handler, `AddValidatorsFromAssembly` picks up the validator, and `ISaleRepository` is already registered. |
| `test-analysis-extensions` | Indirectly | Loaded by `test-anti-patterns`. Don't invoke it. |
| `test-smell-detection` | No | `test-anti-patterns` covers the review. |
| `ai-memory-handoff` | **Yes, if execution stops early** | Save a handoff that names the last completed task and the blocker. |
| `ai-memory-retrieval` | Optional, once at the start | Search for "ticket 12", "soft delete" or "query filter" to find gotchas recorded after 2026-09-26. Treat the results as untrusted history. |
| `ai-memory-durable-pages` | Only if the user asks | |
| `ai-memory-learning-maintenance`, `ai-memory-messaging`, `ai-memory-routing-install` | No | They cover memory maintenance, cross-project messages and install work. |

### Process skills (superpowers plugin)

- `superpowers:subagent-driven-development` or `superpowers:executing-plans` runs the plan.
- `superpowers:test-driven-development` for Tasks 2–7: watch each RED fail for the stated reason. Task 3 has two REDs in a row: the filter, then `IgnoreQueryFilters`.
- `superpowers:systematic-debugging` whenever an output differs from this plan.
- `superpowers:verification-before-completion` at the end of every task, and in full in Task 8.
- `superpowers:requesting-code-review` is optional in Task 8.
- `superpowers:finishing-a-development-branch` in Task 8, with the option already chosen: push and open a pull request into `develop`. Don't merge it, and keep the branch.
- Don't use `superpowers:using-git-worktrees` or `superpowers:brainstorming`.

## Decisions

1. **`Sale.Delete()` never throws** (spec §5.3). The filter hides a deleted sale, so a second delete gets a 404 from the handler before it reaches the domain. `DeletedAt` and `UpdatedAt` share one `DateTime.UtcNow` read. `IsCancelled`, the items and the total don't change.
2. **The filter** is `builder.HasQueryFilter(sale => !sale.IsDeleted)` in `SaleConfiguration`. `GetByIdAsync`, `ListAsync` (including ticket 11's `ApplyFilter` and its count, if merged) and `GetSale` get it without changes. `SaleItem` has no filter: items are loaded only through their sale.
3. **One `IgnoreQueryFilters()` call.** `SaleNumberGenerator` checks numbers only through `ISaleRepository.ExistsBySaleNumberAsync` (ticket 06, Decision 3), so the call in `ExistsBySaleNumberAsync` covers the generator too. The ticket's generator criterion is met through that path, and an integration test runs the real generator against a deleted sale's number.
4. **The EF Core warning.** In EF Core 8.0.10, `ModelValidator.ValidateQueryFilters` logs `PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning` only for a required reference navigation on the dependent side. `SaleItem` has none: ticket 05 maps `HasMany(sale => sale.Items).WithOne()`. So today's model never logs the warning.
   - The ticket and spec §8.2 still ask for the setting. It records the reason, and it keeps a future `SaleItem.Sale` navigation from logging a false alarm.
   - The setting goes in `DefaultContext.OnConfiguring`, so every way of creating the context gets it: `AddDbContext`, the design-time factory and the test fixtures.
   - The unit test reads the setting from the context's options, because there's no warning to observe.
5. **`DeleteSaleCommand : IRequest<Unit>`, not `IRequest`.** In MediatR 12, a void `IRequest` doesn't implement `IRequest<Unit>`. The template's `ValidationBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>` would then never wrap it, and the validator would never run. The functional empty-id test (400) proves the pipeline runs it.
6. **The handler** depends on `ISaleRepository` only: there's no event to publish (R11) and no result to map. Its 404 message is ticket 05's: `The sale with ID {id} does not exist`.
7. **The controller** uses `[HttpDelete("{id}")]` without a `:guid` constraint, like `GET {id}`, so a malformed id is a 400 from model binding. It returns `Ok("Sale deleted successfully")`, ticket 03's helper for responses without data. `[ProducesResponseType(typeof(ApiResponse), 200)]` matches the template's `DeleteUser`.
8. **Two guards for a deleted sale's number.** Without `IgnoreQueryFilters`, `POST` with that number would still end in 409, through ticket 06's unique-index translation. So the functional 409 test pins the HTTP contract, and the integration tests pin the mechanism: `ExistsBySaleNumberAsync` returns `true`, and the generator skips the number.
9. **Test data:**
   - **Integration (`SaleSoftDeleteTests`):** resets in `InitializeAsync`, then deletes the way the handler does: `GetByIdAsync`, `Delete`, `UpdateAsync`.
   - **Functional (`DeleteSaleTests`):** resets and logs in, in `InitializeAsync`. A private `CreateDeletedSaleAsync` helper asserts the setup delete returns 200.
10. **`.http`:** three requests at the end of the file: delete `{{saleId}}` (200), get it (404), delete it again (404). By then the requests above have cancelled `{{saleId}}` (ticket 07), so the file also shows that a cancelled sale can be deleted. Nothing after them uses `{{saleId}}`.
11. **Delivery, as in tickets 05–11:** commit this plan on `develop` and push it, then open a pull request into `develop`. The user merges it on GitHub with **Create a merge commit** (the ticket's `--no-ff`).

## Order with other tickets

- **Blocked by 06 and 10.** Ticket 10 is blocked by 07, so `UpdateAsync` and `Sale.Cancel()` exist.
- Tickets 08, 09 and 11 may already be merged. The anchors avoid their code, the baseline in Task 1 includes their tests, and only the end of the `.http` file depends on 08 (Task 7, Step 1).
- **Ticket 13** documents soft delete and the kept numbers in the README (Task 8 report).

## Pre-existing output (leave it alone)

- `warning NU1903` (AutoMapper 13.0.1): a solution build shows `2 Warning(s)`.
- `NETSDK1057` (preview SDK). A bare `dotnet build` at the root gives `MSB1011`, so always pass the `.sln` or a project.
- `LF will be replaced by CRLF` when adding files.
- `dotnet ef` prints `[FTL] … HostAbortedException` and a stack trace. The commands filter it out.

## File map

| Change | Paths |
|---|---|
| Created (src) | `src/Ambev.DeveloperEvaluation.Application/Sales/DeleteSale/{DeleteSaleCommand, DeleteSaleCommandValidator, DeleteSaleHandler}.cs` |
| Modified (src) | `src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs`; `src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs`; `src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleConfiguration.cs`; `src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs`; `src/Ambev.DeveloperEvaluation.ORM/DefaultContext.cs`; `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs`; `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http` |
| Created (tests) | `tests/Ambev.DeveloperEvaluation.Unit/ORM/DefaultContextTests.cs`; `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/DeleteSale/{DeleteSaleCommandValidatorTests, DeleteSaleHandlerTests}.cs`; `tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleSoftDeleteTests.cs`; `tests/Ambev.DeveloperEvaluation.Functional/Sales/DeleteSaleTests.cs` |
| Modified (tests) | `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs`; `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesControllerTests.cs` |
| Committed on `develop` first | this plan |
| Local only | `/tmp/ticket12-checks/` (removed at the end) |

Test deltas: Unit **+9**, Integration **+4**, Functional **+8**.

---

### Task 1: Check the starting point, commit this plan, branch and record the baseline

**Files:**
- Commit on `develop`: `docs/superpowers/plans/2026-09-26-12-soft-delete-a-sale.md`

- [ ] **Step 1: Confirm tickets 06 and 10 are merged and ticket 12 hasn't started**

```bash
git switch develop
git pull --ff-only origin develop
git diff --quiet && git diff --cached --quiet && echo "no tracked changes"
git status --short
git log --oneline --merges | grep -oE "feature/[a-z-]+" | sort -u
git grep -n -E 'DeleteSaleCommand|HasQueryFilter|IgnoreQueryFilters|void Delete\(' -- src tests || echo "nothing of ticket 12 yet"
git branch --list feature/delete-sale
```

Expected:
- the pull fast-forwards or prints `Already up to date.`
- `no tracked changes`
- untracked `?? docs/superpowers/plans/2026-09-26-12-soft-delete-a-sale.md` and `?? docs/superpowers/tickets/`, possibly with other untracked plans
- the merged branches include `feature/sale-number`, `feature/cancel-sale` and `feature/list-sales`
- `nothing of ticket 12 yet`
- no `feature/delete-sale` branch

Stop and ask the user if the pull fails, anything tracked is modified, one of those three branches isn't merged, or `feature/delete-sale` exists. In that last case, look for an ai-memory handoff first.

- [ ] **Step 2: Check the tools and Docker**

```bash
dotnet tool restore 2>&1 | tail -1
command -v slopwatch
docker version --format 'Docker server {{.Server.Version}}' 2>&1 | tail -1
docker info --format '{{.OSType}}' 2>&1 | tail -1
```

Expected: `Restore was successful.`, a slopwatch path, `Docker server …` and `linux`. If Docker fails, stop, save a handoff (`ai-memory-handoff`) and ask the user to start Docker Desktop.

- [ ] **Step 3: Commit this plan on `develop`, push, branch**

```bash
git add docs/superpowers/plans/2026-09-26-12-soft-delete-a-sale.md
git commit -m "docs: add plan for soft-deleting a sale"
git push origin develop
git switch -c feature/delete-sale
```

Expected: one file committed and pushed, then `Switched to a new branch 'feature/delete-sale'`. If the push is rejected, stop and ask. Never force it.

- [ ] **Step 4: Record the baseline**

```bash
mkdir -p /tmp/ticket12-checks
dotnet build Ambev.DeveloperEvaluation.sln --no-incremental 2>&1 | grep -E 'warning CS|Warning\(s\)|Error\(s\)|Build succeeded' | sort -u
dotnet test Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-110 | tee /tmp/ticket12-checks/baseline.txt
slopwatch analyze --fail-on warning 2>&1 | tail -1
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at ' | tail -1
```

Expected:
- `Build succeeded.`, `2 Warning(s)`, `0 Error(s)` and no `warning CS`
- three `Passed!` lines (Unit, Integration, Functional). Record them as U0, I0 and F0.
- `Scan complete: 0 issue(s) found`
- `No changes have been made to the model since the last migration.`

If anything fails, stop.

---

### Task 2: Soft-delete a sale in the domain

**Skills:** `dotnet-best-practices` (load it now), `type-design-performance`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs`
- Modify: `src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs`

- [ ] **Step 1: Write the failing tests**

In `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs`, replace

```csharp
        sale.Should().BeEquivalentTo(new { IsCancelled = false, IsDeleted = false, DeletedAt = (DateTime?)null, UpdatedAt = (DateTime?)null });
    }
```

with

```csharp
        sale.Should().BeEquivalentTo(new { IsCancelled = false, IsDeleted = false, DeletedAt = (DateTime?)null, UpdatedAt = (DateTime?)null });
    }

    /// <summary>
    /// Tests rules R11 and R13: deleting marks the sale deleted, and sets <see cref="Sale.DeletedAt"/> and
    /// <see cref="Sale.UpdatedAt"/> to the same time, now, in UTC.
    /// </summary>
    [Fact(DisplayName = "Given an open sale When deleting it Then it is deleted, with DeletedAt and UpdatedAt set to now in UTC")]
    public void Given_OpenSale_When_Deleting_Then_IsDeletedWithTimestampsNowInUtc()
    {
        // Given
        var sale = SaleTestData.CreateSale();
        var before = DateTime.UtcNow;

        // When
        sale.Delete();

        // Then
        var after = DateTime.UtcNow;
        sale.IsDeleted.Should().BeTrue();
        sale.DeletedAt.Should().NotBeNull().And.BeOnOrAfter(before).And.BeOnOrBefore(after);
        sale.DeletedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
        sale.UpdatedAt.Should().Be(sale.DeletedAt);
    }

    /// <summary>
    /// Tests rule R11: a delete records no event, so nothing is published.
    /// </summary>
    [Fact(DisplayName = "Given a loaded sale When deleting it Then it records no event")]
    public void Given_LoadedSale_When_Deleting_Then_RecordsNoEvent()
    {
        // Given (a loaded sale has no recorded events)
        var sale = SaleTestData.CreateSale();
        sale.ClearDomainEvents();

        // When
        sale.Delete();

        // Then
        sale.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R7: a cancelled sale is read-only, but it can still be deleted, and it stays cancelled.
    /// </summary>
    [Fact(DisplayName = "Given a cancelled sale When deleting it Then it is deleted and stays cancelled")]
    public void Given_CancelledSale_When_Deleting_Then_IsDeletedAndStaysCancelled()
    {
        // Given
        var sale = SaleTestData.CreateSale();
        sale.Cancel();

        // When
        var act = () => sale.Delete();

        // Then
        act.Should().NotThrow();
        sale.IsDeleted.Should().BeTrue();
        sale.IsCancelled.Should().BeTrue();
    }
```

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected: three `SaleTests.cs(…): error CS1061: 'Sale' does not contain a definition for 'Delete' …` lines.

- [ ] **Step 3: Add `Delete`**

In `src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs`, replace

```csharp
    public void ClearDomainEvents() => _domainEvents.Clear();
```

with

```csharp
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Soft-deletes the sale (rule R11): <see cref="IsDeleted"/> is set, and <see cref="DeletedAt"/> and
    /// <see cref="UpdatedAt"/> get the same time. The items stay as the history, and no event is recorded.
    /// A cancelled sale can be deleted too (rule R7).
    /// </summary>
    public void Delete()
    {
        var now = DateTime.UtcNow;
        IsDeleted = true;
        DeletedAt = now;
        UpdatedAt = now;
    }
```

- [ ] **Step 4: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~Domain.Entities.SaleTests&FullyQualifiedName~When_Deleting" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)`, `Build succeeded.`; `Passed:     3`; Unit U0 + 3.

- [ ] **Step 5: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs
git commit -m "feat(sales): soft-delete a sale in the domain" -m "Sale.Delete sets IsDeleted, and DeletedAt and UpdatedAt to the same UTC time (rules R11 and R13). It records no event and is allowed on a cancelled sale (rule R7)."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `2 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 3: Hide deleted sales with a global query filter, and keep their numbers taken

**Skills:** `efcore-patterns`, `testcontainers-integration-tests` (load both now), `dotnet-best-practices`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleSoftDeleteTests.cs`
- Modify: `src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleConfiguration.cs`
- Modify: `src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs`
- Modify: `src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs`

- [ ] **Step 1: Write the integration tests**

Create `tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleSoftDeleteTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Integration.Fixtures;
using Ambev.DeveloperEvaluation.Integration.TestData;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Ambev.DeveloperEvaluation.ORM.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.ORM;

/// <summary>
/// Contains integration tests for soft delete against PostgreSQL (spec §8.2). The global query filter hides a deleted
/// sale from <see cref="SaleRepository.GetByIdAsync"/> and <see cref="SaleRepository.ListAsync"/>, its rows stay, and
/// its number stays taken (rule R12). Each test starts from empty tables and a restarted <c>sale_number_seq</c>.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class SaleSoftDeleteTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaleSoftDeleteTests"/> class.
    /// </summary>
    /// <param name="database">The collection's database, injected by xUnit.</param>
    public SaleSoftDeleteTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    /// <summary>
    /// Empties the tables and restarts the sequence, so counts and generated numbers are exact.
    /// </summary>
    public Task InitializeAsync() => _database.ResetDataAsync(DataResetFixture.Tables, DataResetFixture.Sequences);

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Tests rule R11: the filter hides a deleted sale from <see cref="SaleRepository.GetByIdAsync"/>, while its row
    /// and its items stay in the database.
    /// </summary>
    [Fact(DisplayName = "Given a deleted sale When loading it by id Then it returns null, while IgnoreQueryFilters still finds the row and its items")]
    public async Task Given_DeletedSale_When_LoadingById_Then_ReturnsNullButRowAndItemsRemain()
    {
        // Given
        var deleted = await SaveDeletedSaleAsync(SaleTestData.GenerateValidSale(itemCount: 2));

        // When
        await using var context = _database.CreateContext();
        var loaded = await new SaleRepository(context).GetByIdAsync(deleted.Id);

        // Then
        loaded.Should().BeNull();
        var row = await context.Sales
            .IgnoreQueryFilters()
            .Include(sale => sale.Items)
            .SingleAsync(sale => sale.Id == deleted.Id);
        row.IsDeleted.Should().BeTrue();
        row.DeletedAt.Should().BeCloseTo(deleted.DeletedAt!.Value, TimeSpan.FromMicroseconds(1));
        row.Items.Select(item => item.Id).Should().BeEquivalentTo(deleted.Items.Select(item => item.Id));
    }

    /// <summary>
    /// Tests rule R11: a deleted sale is neither on the page nor in the count.
    /// </summary>
    [Fact(DisplayName = "Given a deleted sale and a kept one When listing Then only the kept sale is on the page and in the count")]
    public async Task Given_DeletedAndKeptSales_When_Listing_Then_OnlyKeptSaleIsListedAndCounted()
    {
        // Given
        var kept = SaleTestData.GenerateValidSale();
        await using (var createContext = _database.CreateContext())
        {
            await new SaleRepository(createContext).CreateAsync(kept);
        }

        await SaveDeletedSaleAsync(SaleTestData.GenerateValidSale());

        // When
        await using var context = _database.CreateContext();
        var page = await new SaleRepository(context).ListAsync(new SaleListQuery(1, 10, []));

        // Then
        page.Sales.Select(sale => sale.Id).Should().Equal(kept.Id);
        page.TotalCount.Should().Be(1);
    }

    /// <summary>
    /// Tests rule R12: a deleted sale's number still counts as taken.
    /// </summary>
    [Fact(DisplayName = "Given a deleted sale When checking its number Then the number still exists")]
    public async Task Given_DeletedSale_When_CheckingItsNumber_Then_NumberStillExists()
    {
        // Given
        var deleted = await SaveDeletedSaleAsync(SaleTestData.GenerateValidSale());

        // When
        await using var context = _database.CreateContext();
        var exists = await new SaleRepository(context).ExistsBySaleNumberAsync(deleted.SaleNumber);

        // Then
        exists.Should().BeTrue();
    }

    /// <summary>
    /// Tests step 3 of spec §8.3 with a deleted sale: the generator skips its number instead of reusing it.
    /// </summary>
    [Fact(DisplayName = "Given a deleted sale numbered S-000001 When generating a number Then it skips S-000001 and returns S-000002")]
    public async Task Given_DeletedSaleHoldsFirstNumber_When_Generating_Then_SkipsIt()
    {
        // Given
        await SaveDeletedSaleAsync(SaleTestData.GenerateValidSale(saleNumber: "S-000001"));

        // When
        await using var context = _database.CreateContext();
        var number = await new SaleNumberGenerator(context, new SaleRepository(context)).NextAsync();

        // Then
        number.Should().Be("S-000002");
    }

    /// <summary>
    /// Saves the sale, then deletes it the way the delete handler does: load it, <see cref="Sale.Delete"/>, update it.
    /// </summary>
    /// <param name="sale">A new sale.</param>
    /// <returns>The deleted sale, as it was saved.</returns>
    private async Task<Sale> SaveDeletedSaleAsync(Sale sale)
    {
        await using (var createContext = _database.CreateContext())
        {
            await new SaleRepository(createContext).CreateAsync(sale);
        }

        await using var deleteContext = _database.CreateContext();
        var repository = new SaleRepository(deleteContext);
        var loaded = await repository.GetByIdAsync(sale.Id);
        loaded!.Delete();
        await repository.UpdateAsync(loaded);
        return loaded;
    }
}
```

- [ ] **Step 2: Run them without the filter (first RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Integration 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build --filter "FullyQualifiedName~SaleSoftDeleteTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected: `Build succeeded.`; `Failed:     2, Passed:     2`. The by-id test gets the sale back (`Expected loaded to be <null>, but found …Sale…`), and the list test gets both sales. The number and generator tests pass for now, because nothing hides deleted rows yet.

- [ ] **Step 3: Add the query filter**

In `src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleConfiguration.cs`, replace

```csharp
        builder.Ignore(sale => sale.DomainEvents);
```

with

```csharp
        builder.Ignore(sale => sale.DomainEvents);

        // Soft delete (spec decision D7): every query hides deleted sales, so no query has to remember a filter.
        // SaleRepository.ExistsBySaleNumberAsync opts out, because a deleted sale keeps its number (rule R12).
        builder.HasQueryFilter(sale => !sale.IsDeleted);
```

- [ ] **Step 4: Run them with the filter (second RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Integration 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build --filter "FullyQualifiedName~SaleSoftDeleteTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected: `Failed:     2, Passed:     2`, but now the other two fail. `Expected exists to be True, but found False.` The generator returns `"S-000001"`, the deleted sale's number, where `"S-000002"` is expected.

- [ ] **Step 5: Ignore the filter in the number check**

In `src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs`, replace

```csharp
    public Task<bool> ExistsBySaleNumberAsync(string saleNumber, CancellationToken cancellationToken = default) =>
        _context.Sales.AnyAsync(sale => sale.SaleNumber == saleNumber, cancellationToken);
```

with

```csharp
    public Task<bool> ExistsBySaleNumberAsync(string saleNumber, CancellationToken cancellationToken = default) =>
        // A soft-deleted sale keeps its number (rule R12), so this check ignores the soft-delete filter.
        _context.Sales
            .IgnoreQueryFilters()
            .AnyAsync(sale => sale.SaleNumber == saleNumber, cancellationToken);
```

In `src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs`, replace

```csharp
    /// <returns>The sale, or <c>null</c> if there is none with that id.</returns>
```

with

```csharp
    /// <returns>The sale, or <c>null</c> if there is none with that id or it was soft-deleted.</returns>
```

Then replace

```csharp
    /// Tells whether a sale already has this sale number. The comparison is exact: case counts.
```

with

```csharp
    /// Tells whether a sale already has this sale number, soft-deleted sales included (rule R12).
    /// The comparison is exact: case counts.
```

- [ ] **Step 6: Verify (GREEN), and that the model didn't change**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build --filter "FullyQualifiedName~SaleSoftDeleteTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at ' | tail -1
git status --short src/Ambev.DeveloperEvaluation.ORM/Migrations
```

Expected:
- `0 Error(s)`, no `warning CS`
- `Passed:     4`; Integration I0 + 4
- Functional F0: the filter changes nothing for sales that aren't deleted
- `No changes have been made to the model since the last migration.` Query filters aren't part of the migration model.
- no migration file changed

- [ ] **Step 7: Commit and scan**

```bash
git add tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleSoftDeleteTests.cs src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleConfiguration.cs src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs
git commit -m "feat(sales): hide soft-deleted sales with a global query filter" -m "Sale has the EF Core query filter !IsDeleted, so GetByIdAsync and ListAsync never return a deleted sale, while its row and items stay. ExistsBySaleNumberAsync calls IgnoreQueryFilters, so a deleted sale's number stays taken for a client-sent number and for the generator, which checks through it (rule R12). No migration."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `4 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 4: Ignore EF Core's required-navigation query-filter warning

**Skills:** `efcore-patterns`, `dotnet-best-practices`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Unit/ORM/DefaultContextTests.cs`
- Modify: `src/Ambev.DeveloperEvaluation.ORM/DefaultContext.cs`

- [ ] **Step 1: Write the failing test**

Create `tests/Ambev.DeveloperEvaluation.Unit/ORM/DefaultContextTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.ORM;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.ORM;

/// <summary>
/// Contains unit tests for the options that <see cref="DefaultContext"/> sets on itself. No database is opened.
/// </summary>
public sealed class DefaultContextTests
{
    /// <summary>
    /// Tests spec §8.2: <c>Sale</c> has the soft-delete filter and <c>SaleItem</c> doesn't, and the warning about that
    /// is ignored, because items are only ever loaded through their sale.
    /// </summary>
    [Fact(DisplayName = "Given a DefaultContext When reading its warning settings Then the required-navigation query-filter warning is ignored")]
    public void Given_DefaultContext_When_ReadingWarningSettings_Then_QueryFilterNavigationWarningIsIgnored()
    {
        // Given
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseNpgsql("Host=localhost;Database=unused")
            .Options;
        using var context = new DefaultContext(options);

        // When
        var warnings = context.GetService<IDbContextOptions>().FindExtension<CoreOptionsExtension>()!.WarningsConfiguration;

        // Then
        warnings.GetBehavior(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning)
            .Should().Be(WarningBehavior.Ignore);
    }
}
```

- [ ] **Step 2: Run it and watch it fail (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~DefaultContextTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected: `Build succeeded.`; `Failed:     1`. The message says it expected `WarningBehavior.Ignore` but found `<null>`: no behaviour is set for that event.

- [ ] **Step 3: Ignore the warning**

With the Edit tool (the file has a BOM and CRLF endings), in `src/Ambev.DeveloperEvaluation.ORM/DefaultContext.cs`, replace

```csharp
using Microsoft.EntityFrameworkCore;
using System.Reflection;
```

with

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Reflection;
```

Then replace

```csharp
    public DefaultContext(DbContextOptions<DefaultContext> options) : base(options)
    {
    }
```

with

```csharp
    public DefaultContext(DbContextOptions<DefaultContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Sale has the soft-delete query filter and SaleItem has none. EF Core warns that such a relationship can
        // load a dependent whose principal is filtered out. Items are only ever loaded through their sale
        // (GetByIdAsync, ListAsync), so a deleted sale's items are hidden with it and the warning doesn't apply.
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
    }
```

- [ ] **Step 4: Verify (GREEN)**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at ' | tail -1
```

Expected: `0 Error(s)`, no `warning CS`; Unit U0 + 4; Integration I0 + 4; `No changes have been made to the model since the last migration.`

- [ ] **Step 5: Commit and scan**

```bash
git add tests/Ambev.DeveloperEvaluation.Unit/ORM/DefaultContextTests.cs src/Ambev.DeveloperEvaluation.ORM/DefaultContext.cs
git commit -m "chore(orm): ignore the required-navigation query-filter warning" -m "DefaultContext ignores PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning, with a comment saying why: Sale has the soft-delete filter, SaleItem has none, and items are only ever loaded through their sale (spec 8.2)."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `2 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 5: Delete a sale in the Application layer

**Skills:** `dotnet-best-practices`, `type-design-performance`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/DeleteSale/DeleteSaleCommandValidatorTests.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/DeleteSale/DeleteSaleHandlerTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.Application/Sales/DeleteSale/DeleteSaleCommand.cs`, `DeleteSaleCommandValidator.cs`, `DeleteSaleHandler.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/DeleteSale/DeleteSaleCommandValidatorTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;
using FluentValidation.TestHelper;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.DeleteSale;

/// <summary>
/// Contains unit tests for <see cref="DeleteSaleCommandValidator"/>.
/// </summary>
public sealed class DeleteSaleCommandValidatorTests
{
    private readonly DeleteSaleCommandValidator _validator = new();

    /// <summary>
    /// Tests that an id is required.
    /// </summary>
    [Fact(DisplayName = "Given an empty id When validating Then Id fails as empty")]
    public void Given_EmptyId_When_Validating_Then_IdFails()
    {
        // When
        var result = _validator.TestValidate(new DeleteSaleCommand(Guid.Empty));

        // Then
        result.ShouldHaveValidationErrorFor(command => command.Id).WithErrorMessage("'Id' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that any other id passes.
    /// </summary>
    [Fact(DisplayName = "Given an id When validating Then there are no failures")]
    public void Given_Id_When_Validating_Then_NoFailures()
    {
        // When
        var result = _validator.TestValidate(new DeleteSaleCommand(Guid.NewGuid()));

        // Then
        result.ShouldNotHaveAnyValidationErrors();
    }
}
```

Create `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/DeleteSale/DeleteSaleHandlerTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.DeleteSale;

/// <summary>
/// Contains unit tests for <see cref="DeleteSaleHandler"/>.
/// </summary>
public sealed class DeleteSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly DeleteSaleHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteSaleHandlerTests"/> class.
    /// </summary>
    public DeleteSaleHandlerTests()
    {
        _handler = new DeleteSaleHandler(_saleRepository);
    }

    /// <summary>
    /// Tests that the handler deletes the loaded sale before it saves it.
    /// </summary>
    [Fact(DisplayName = "Given a sale When deleting it Then it saves the sale already marked deleted")]
    public async Task Given_Sale_When_Deleting_Then_SavesItMarkedDeleted()
    {
        // Given
        var sale = SaleTestData.CreateSale();
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        var deletedWhenSaved = false;
        _saleRepository
            .When(repository => repository.UpdateAsync(sale, Arg.Any<CancellationToken>()))
            .Do(_ => deletedWhenSaved = sale.IsDeleted);

        // When
        await _handler.Handle(new DeleteSaleCommand(sale.Id), CancellationToken.None);

        // Then
        await _saleRepository.Received(1).UpdateAsync(sale, Arg.Any<CancellationToken>());
        deletedWhenSaved.Should().BeTrue();
    }

    /// <summary>
    /// Tests that a missing sale is a not-found error and nothing is saved. A deleted sale takes the same path,
    /// because the query filter makes <see cref="ISaleRepository.GetByIdAsync"/> return <c>null</c> for it.
    /// </summary>
    [Fact(DisplayName = "Given no sale with the id When deleting Then it throws KeyNotFoundException and saves nothing")]
    public async Task Given_NoSaleWithId_When_Deleting_Then_ThrowsKeyNotFoundException()
    {
        // Given
        var id = Guid.NewGuid();
        _saleRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Sale?)null);

        // When
        var act = () => _handler.Handle(new DeleteSaleCommand(id), CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"The sale with ID {id} does not exist");
        await _saleRepository.DidNotReceive().UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }
}
```

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected: `CS0234: The type or namespace name 'DeleteSale' does not exist in the namespace 'Ambev.DeveloperEvaluation.Application.Sales'` in both files, and `CS0246` for `DeleteSaleCommandValidator` and `DeleteSaleHandler`.

- [ ] **Step 3: Write the command, its validator and the handler**

Create `src/Ambev.DeveloperEvaluation.Application/Sales/DeleteSale/DeleteSaleCommand.cs`:

```csharp
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;

/// <summary>
/// Soft-deletes a sale. It returns no data; <see cref="Unit"/> keeps it an <see cref="IRequest{TResponse}"/>,
/// so the MediatR <c>ValidationBehavior</c> runs <see cref="DeleteSaleCommandValidator"/> first.
/// </summary>
/// <param name="Id">The id of the sale.</param>
public sealed record DeleteSaleCommand(Guid Id) : IRequest<Unit>;
```

Create `src/Ambev.DeveloperEvaluation.Application/Sales/DeleteSale/DeleteSaleCommandValidator.cs`:

```csharp
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;

/// <summary>
/// Validates <see cref="DeleteSaleCommand"/> before its handler runs.
/// </summary>
public sealed class DeleteSaleCommandValidator : AbstractValidator<DeleteSaleCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteSaleCommandValidator"/> class: the id is required.
    /// </summary>
    public DeleteSaleCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
```

Create `src/Ambev.DeveloperEvaluation.Application/Sales/DeleteSale/DeleteSaleHandler.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Repositories;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;

/// <summary>
/// Handles <see cref="DeleteSaleCommand"/>: the sale marks itself deleted and the repository saves it.
/// Nothing is published (rule R11).
/// </summary>
public sealed class DeleteSaleHandler : IRequestHandler<DeleteSaleCommand, Unit>
{
    private readonly ISaleRepository _saleRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteSaleHandler"/> class.
    /// </summary>
    /// <param name="saleRepository">The sale repository.</param>
    public DeleteSaleHandler(ISaleRepository saleRepository)
    {
        _saleRepository = saleRepository;
    }

    /// <summary>
    /// Loads the sale, deletes it and saves it.
    /// </summary>
    /// <param name="command">The validated command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when there is no sale with the id, or it was already deleted.</exception>
    public async Task<Unit> Handle(DeleteSaleCommand command, CancellationToken cancellationToken)
    {
        var sale = await _saleRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"The sale with ID {command.Id} does not exist");

        sale.Delete();

        await _saleRepository.UpdateAsync(sale, cancellationToken);

        return Unit.Value;
    }
}
```

- [ ] **Step 4: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~Sales.DeleteSale" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)`; `Passed:     4`; Unit U0 + 8.

- [ ] **Step 5: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Application/Sales/DeleteSale/DeleteSaleCommand.cs src/Ambev.DeveloperEvaluation.Application/Sales/DeleteSale/DeleteSaleCommandValidator.cs src/Ambev.DeveloperEvaluation.Application/Sales/DeleteSale/DeleteSaleHandler.cs tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/DeleteSale/DeleteSaleCommandValidatorTests.cs tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/DeleteSale/DeleteSaleHandlerTests.cs
git commit -m "feat(sales): delete a sale in the application layer" -m "DeleteSaleCommand is an IRequest<Unit>, so ValidationBehavior runs its validator, which requires the id. DeleteSaleHandler loads the sale (KeyNotFoundException if it's missing or already deleted), deletes it and saves it with UpdateAsync. It publishes nothing (rule R11)."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `5 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 6: Soft-delete a sale over HTTP

**Skills:** `dotnet-best-practices`, `testcontainers-integration-tests`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesControllerTests.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Functional/Sales/DeleteSaleTests.cs`
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs`

- [ ] **Step 1: Write the failing controller test**

In `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesControllerTests.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
```

with

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;
```

Then replace

```csharp
            $$"""{"success":true,"message":"Sale retrieved successfully","data":{{ExampleSaleJson}}}""");
    }
```

with

```csharp
            $$"""{"success":true,"message":"Sale retrieved successfully","data":{{ExampleSaleJson}}}""");
    }

    /// <summary>
    /// Tests that deleting a sale sends the command for the route id and returns 200 with exactly
    /// <c>{success, message}</c>: no <c>data</c>, and the envelope built once.
    /// </summary>
    [Fact(DisplayName = "Given a sale id When deleting the sale Then it sends the command and returns 200 with only success and message")]
    public async Task Given_SaleId_When_DeletingSale_Then_Returns200WithSuccessAndMessageOnly()
    {
        // Given
        var id = Guid.NewGuid();

        // When
        var response = await _controller.DeleteSale(id, CancellationToken.None);

        // Then
        await _mediator.Received(1).Send(new DeleteSaleCommand(id), Arg.Any<CancellationToken>());
        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        MvcJson.Serialize(ok.Value).Should().Be("""{"success":true,"message":"Sale deleted successfully"}""");
    }
```

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected: `SalesControllerTests.cs(…): error CS1061: 'SalesController' does not contain a definition for 'DeleteSale' …`.

- [ ] **Step 3: Write the failing functional tests**

Create `tests/Ambev.DeveloperEvaluation.Functional/Sales/DeleteSaleTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

/// <summary>
/// Contains functional tests for soft-deleting a sale with <c>DELETE /api/sales/{id}</c>. Each test starts from
/// empty tables and logs in, so the list totals are exact.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class DeleteSaleTests : IAsyncLifetime
{
    private const string DeletedBody = """{"success":true,"message":"Sale deleted successfully"}""";

    private readonly ApiFixture _api;
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteSaleTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public DeleteSaleTests(ApiFixture api)
    {
        _api = api;
        _client = api.CreateClient();
    }

    /// <summary>
    /// Empties the tables, then logs in.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _api.ResetDataAsync(DataResetFixture.Tables, DataResetFixture.Sequences);
        await _client.LogInAsNewUserAsync();
    }

    /// <inheritdoc />
    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Tests the delete response: 200 with exactly <c>{success, message}</c>, built once.
    /// </summary>
    [Fact(DisplayName = "Given a sale When deleting it Then returns 200 with exactly success and message")]
    public async Task Given_Sale_When_Deleting_Then_Returns200WithSuccessAndMessageOnly()
    {
        // Given
        var sale = await _client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid());

        // When
        using var response = await _client.DeleteAsync($"/api/sales/{sale.Id}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be(DeletedBody);
    }

    /// <summary>
    /// Tests rule R11: a deleted sale is not found.
    /// </summary>
    [Fact(DisplayName = "Given a deleted sale When getting it by id Then returns 404 ResourceNotFound")]
    public async Task Given_DeletedSale_When_GettingIt_Then_Returns404ResourceNotFound()
    {
        // Given
        var sale = await CreateDeletedSaleAsync();

        // When
        using var response = await _client.GetAsync($"/api/sales/{sale.Id}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"ResourceNotFound","error":"Resource not found","detail":"The sale with ID {{sale.Id}} does not exist"}""");
    }

    /// <summary>
    /// Tests rule R11: the list omits a deleted sale, and <c>totalItems</c> doesn't count it.
    /// </summary>
    [Fact(DisplayName = "Given a deleted sale and a kept one When listing sales Then only the kept sale is listed and counted")]
    public async Task Given_DeletedAndKeptSales_When_Listing_Then_OnlyKeptSaleIsListedAndCounted()
    {
        // Given
        var kept = await _client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid());
        await CreateDeletedSaleAsync();

        // When
        using var response = await _client.GetAsync("/api/sales");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = (await response.Content.ReadFromJsonAsync<SalePageResponseBody>())!;
        page.Data.Select(sale => sale.Id).Should().Equal(kept.Id);
        page.TotalItems.Should().Be(1);
    }

    /// <summary>
    /// Tests rule R7: a cancelled sale is read-only, but it can still be deleted.
    /// </summary>
    [Fact(DisplayName = "Given a cancelled sale When deleting it Then returns 200")]
    public async Task Given_CancelledSale_When_Deleting_Then_Returns200()
    {
        // Given
        var sale = await _client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid());
        await _client.CancelSaleAsync(sale.Id);

        // When
        using var response = await _client.DeleteAsync($"/api/sales/{sale.Id}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be(DeletedBody);
    }

    /// <summary>
    /// Tests that a second delete finds nothing to delete.
    /// </summary>
    [Fact(DisplayName = "Given a deleted sale When deleting it again Then returns 404 ResourceNotFound")]
    public async Task Given_DeletedSale_When_DeletingAgain_Then_Returns404ResourceNotFound()
    {
        // Given
        var sale = await CreateDeletedSaleAsync();

        // When
        using var response = await _client.DeleteAsync($"/api/sales/{sale.Id}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"ResourceNotFound","error":"Resource not found","detail":"The sale with ID {{sale.Id}} does not exist"}""");
    }

    /// <summary>
    /// Tests rule R12: a deleted sale's number stays taken.
    /// </summary>
    [Fact(DisplayName = "Given a deleted sale When creating a sale with its number Then returns 409 BusinessRuleViolation")]
    public async Task Given_DeletedSale_When_CreatingSaleWithItsNumber_Then_Returns409BusinessRuleViolation()
    {
        // Given
        var deleted = await CreateDeletedSaleAsync();
        var sale = SaleRequestBodyTestData.GenerateValid() with { SaleNumber = deleted.SaleNumber };

        // When
        using var response = await _client.PostAsJsonAsync("/api/sales", sale);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"BusinessRuleViolation","error":"Business rule violation","detail":"Sale number {{deleted.SaleNumber}} already exists"}""");
    }

    /// <summary>
    /// Tests that the validator runs through the MediatR pipeline (Decision 5): the empty id is a 400, not a 404.
    /// </summary>
    [Fact(DisplayName = "Given the empty id When deleting the sale Then returns 400 ValidationError")]
    public async Task Given_EmptyId_When_DeletingSale_Then_Returns400ValidationError()
    {
        // When
        using var response = await _client.DeleteAsync($"/api/sales/{Guid.Empty}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            """{"type":"ValidationError","error":"Invalid input data","detail":"Id: 'Id' must not be empty."}""");
    }

    /// <summary>
    /// Tests that the endpoint requires a JWT and answers without one with the documented 401 body.
    /// </summary>
    [Fact(DisplayName = "Given no token When deleting a sale Then returns 401 AuthenticationError with the invalid-token body")]
    public async Task Given_NoToken_When_DeletingSale_Then_Returns401WithInvalidTokenBody()
    {
        // Given
        using var client = _api.CreateClient();

        // When
        using var response = await client.DeleteAsync($"/api/sales/{Guid.NewGuid()}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ToString().Should().Be("Bearer");
        (await response.Content.ReadAsStringAsync()).Should().Be(
            """{"type":"AuthenticationError","error":"Invalid authentication token","detail":"The provided authentication token has expired or is invalid"}""");
    }

    /// <summary>
    /// Creates a sale and deletes it, for tests whose subject is a later step, and fails the test if the delete
    /// doesn't return 200.
    /// </summary>
    /// <returns>The sale as the create response returned it.</returns>
    private async Task<SaleResponseBody> CreateDeletedSaleAsync()
    {
        var sale = await _client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid());
        using var response = await _client.DeleteAsync($"/api/sales/{sale.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return sale;
    }
}
```

- [ ] **Step 4: Watch the functional tests fail (RED)**

The Unit project still doesn't compile (Step 2), so build only the Functional project:

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Functional 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~DeleteSaleTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected: `Build succeeded.`; `Failed:     8, Passed:     0`. `DELETE /api/sales/{id}` matches the `GET {id}` route's path, so every delete gets 405 Method Not Allowed. The status checks fail, and so does the check inside `CreateDeletedSaleAsync`.

- [ ] **Step 5: Add the action**

In `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
```

with

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;
```

Then replace

```csharp
        return Ok(_mapper.Map<SaleResponse>(result), "Sale retrieved successfully");
    }
```

with

```csharp
        return Ok(_mapper.Map<SaleResponse>(result), "Sale retrieved successfully");
    }

    /// <summary>
    /// Soft-deletes a sale. It disappears from every read afterwards, its number stays taken, and nothing is published.
    /// A cancelled sale can be deleted too.
    /// </summary>
    /// <param name="id">The id of the sale.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 with <c>{success, message}</c>.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSale([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteSaleCommand(id), cancellationToken);

        return Ok("Sale deleted successfully");
    }
```

- [ ] **Step 6: Verify (GREEN)**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected: `0 Error(s)`, no `warning CS`; Unit U0 + 9; Functional F0 + 8.

- [ ] **Step 7: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesControllerTests.cs tests/Ambev.DeveloperEvaluation.Functional/Sales/DeleteSaleTests.cs
git commit -m "feat(sales): soft-delete a sale over HTTP" -m "DELETE /api/sales/{id} returns 200 with exactly {success, message} (Sale deleted successfully), built once by the Ok(message) helper. Afterwards GET returns 404, the list omits the sale, a second delete returns 404 and its number is a 409. A cancelled sale can be deleted, the empty id is a 400 and a request without a token is a 401."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `3 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 7: Add the delete requests to the `.http` file and replay them against `docker compose up`

**Files:**
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http`

- [ ] **Step 1: Look at the end of the file (RED)**

```bash
grep -c 'DELETE {{baseUrl}}/api/sales' src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
tail -n 3 src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
```

Expected: `0`, then the file's last request. With ticket 08 merged, that's

```http
### Cancel the last active item: the sale is cancelled too (200; isCancelled true, totalAmount 0)
PATCH {{baseUrl}}/api/sales/{{twoItemSaleId}}/items/{{secondItemId}}/cancel
Authorization: Bearer {{token}}
```

and without it, ticket 07's

```http
### Cancel it again: a cancelled sale is read-only (409 BusinessRuleViolation)
PATCH {{baseUrl}}/api/sales/{{saleId}}/cancel
Authorization: Bearer {{token}}
```

- [ ] **Step 2: Append the requests**

With the Edit tool, replace the three lines that `tail` printed with the same three lines followed by:

```http

### Delete the sale created above. The requests above cancelled it, and a cancelled sale can be deleted too (200 with only success and message)
DELETE {{baseUrl}}/api/sales/{{saleId}}
Authorization: Bearer {{token}}

### Get the deleted sale (404 ResourceNotFound)
GET {{baseUrl}}/api/sales/{{saleId}}
Authorization: Bearer {{token}}

### Delete it again (404 ResourceNotFound)
DELETE {{baseUrl}}/api/sales/{{saleId}}
Authorization: Bearer {{token}}
```

- [ ] **Step 3: Check the file (GREEN)**

```bash
git diff --stat | cat
tail -n 11 src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http | grep -E '^(DELETE|GET) '
```

Expected: `1 file changed, 12 insertions(+)`; then `DELETE`, `GET` and `DELETE` on `{{baseUrl}}/api/sales/{{saleId}}`, in that order.

- [ ] **Step 4: Start the stack from nothing**

```bash
docker compose down -v --remove-orphans
docker compose up --build --wait --wait-timeout 300; echo "exit=$?"
```

Expected: `exit=0`. If a port is already allocated, stop and ask the user.

- [ ] **Step 5: Replay with curl**

Keep this in one Bash call.

```bash
BASE=http://localhost:8080
CHECKS=/tmp/ticket12-checks
sale_body() { echo "{\"saleNumber\":\"$1\",\"saleDate\":\"2026-09-24T14:30:00Z\",\"customerId\":\"3f2b8c1e-1111-4a5b-9c2d-000000000001\",\"customerName\":\"Maria Silva\",\"branchId\":\"3f2b8c1e-2222-4a5b-9c2d-000000000002\",\"branchName\":\"Filial Centro\",\"items\":[{\"productId\":\"3f2b8c1e-3333-4a5b-9c2d-000000000003\",\"productName\":\"Cerveja 350ml\",\"quantity\":5,\"unitPrice\":4.50}]}"; }
id_of() { python3 -c "import json, sys; print(json.load(open(sys.argv[1]))['data']['id'])" "$1"; }
curl -s -o /dev/null -w "sign-up %{http_code}\n" -X POST $BASE/api/users -H 'Content-Type: application/json' --data-raw '{"username":"admin","password":"Admin@123","phone":"+5511999999999","email":"admin@developerstore.com","status":"Active","role":"Admin"}'
curl -s -o $CHECKS/login.json -X POST $BASE/api/auth -H 'Content-Type: application/json' --data-raw '{"email":"admin@developerstore.com","password":"Admin@123"}'
TOKEN=$(python3 -c "import json, sys; print(json.load(open(sys.argv[1]))['data']['token'])" $CHECKS/login.json)
OPEN_NUMBER="S-1$(( RANDOM % 90000 + 10000 ))"
curl -s -o $CHECKS/open.json -w "create open %{http_code}\n" -X POST $BASE/api/sales -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' --data-raw "$(sale_body $OPEN_NUMBER)"
curl -s -o $CHECKS/cancelled.json -w "create to cancel %{http_code}\n" -X POST $BASE/api/sales -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' --data-raw "$(sale_body S-2$(( RANDOM % 90000 + 10000 )))"
OPEN_ID=$(id_of $CHECKS/open.json)
CANCELLED_ID=$(id_of $CHECKS/cancelled.json)
curl -s -o /dev/null -w "cancel %{http_code}\n" -X PATCH "$BASE/api/sales/$CANCELLED_ID/cancel" -H "Authorization: Bearer $TOKEN"
curl -s -w " %{http_code}\n" -X DELETE "$BASE/api/sales/$OPEN_ID" -H "Authorization: Bearer $TOKEN"
curl -s -w " %{http_code}\n" "$BASE/api/sales/$OPEN_ID" -H "Authorization: Bearer $TOKEN"
curl -s -w " %{http_code}\n" -X DELETE "$BASE/api/sales/$OPEN_ID" -H "Authorization: Bearer $TOKEN"
curl -s -o /dev/null -w "delete cancelled %{http_code}\n" -X DELETE "$BASE/api/sales/$CANCELLED_ID" -H "Authorization: Bearer $TOKEN"
curl -s -o $CHECKS/list.json -w "list %{http_code} " "$BASE/api/sales" -H "Authorization: Bearer $TOKEN"
python3 -c "import json, sys; b = json.load(open(sys.argv[1])); print('totalItems', b['totalItems'], 'data', b['data'])" $CHECKS/list.json
curl -s -w " %{http_code}\n" -X POST $BASE/api/sales -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' --data-raw "$(sale_body $OPEN_NUMBER)"
```

Expected (the ids and numbers are random):

```
sign-up 201
create open 201
create to cancel 201
cancel 200
{"success":true,"message":"Sale deleted successfully"} 200
{"type":"ResourceNotFound","error":"Resource not found","detail":"The sale with ID … does not exist"} 404
{"type":"ResourceNotFound","error":"Resource not found","detail":"The sale with ID … does not exist"} 404
delete cancelled 200
list 200 totalItems 0 data []
{"type":"BusinessRuleViolation","error":"Business rule violation","detail":"Sale number S-1… already exists"} 409
```

- [ ] **Step 6: Check the rows and the event log**

```bash
docker compose exec -T ambev.developerevaluation.database psql -U developer -d developer_evaluation -At -c 'SELECT s."IsDeleted", s."DeletedAt" IS NOT NULL, s."UpdatedAt" = s."DeletedAt", s."IsCancelled", (SELECT count(*) FROM "SaleItems" i WHERE i."SaleId" = s."Id") FROM "Sales" s ORDER BY s."SaleNumber";'
docker compose logs ambev.developerevaluation.webapi 2>&1 | grep -oE 'Sale event [A-Za-z]+ published' | sort | uniq -c
```

Expected:

```
t|t|t|f|1
t|t|t|t|1
```

Both rows and their items are still there, and `UpdatedAt` equals `DeletedAt`. Then `2 Sale event SaleCreatedEvent published` and `1 Sale event SaleCancelledEvent published`, and no other event: the deletes and the rejected create published nothing.

- [ ] **Step 7: Take the stack down**

```bash
docker compose down -v
docker compose ps -a
```

Expected: `ps -a` lists nothing.

- [ ] **Step 8: Commit**

```bash
git add src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
git commit -m "docs(http): soft-delete a sale" -m "The .http file deletes the created sale, which the requests above cancelled, then shows the 404 for getting it and for deleting it again."
git show --stat --format= HEAD | tail -1
```

Expected: `1 file changed, 12 insertions(+)`. No slopwatch run, because no C# changed.

---

### Task 8: Verify everything, review, then open a pull request into `develop`

**Skills:** superpowers:verification-before-completion, `dotnet-slopwatch`, `test-anti-patterns` (report only), `clean-code` and superpowers:requesting-code-review (optional), superpowers:finishing-a-development-branch.

**Files:** none, unless Step 2 finds a real problem.

- [ ] **Step 1: Re-run every check fresh**

```bash
git status --short
dotnet build Ambev.DeveloperEvaluation.sln --no-incremental 2>&1 | grep -E 'warning CS|Warning\(s\)|Error\(s\)|Build succeeded' | sort -u
dotnet test Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-110
cat /tmp/ticket12-checks/baseline.txt
slopwatch analyze --fail-on warning 2>&1 | tail -1
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at ' | tail -1
git diff --stat develop..HEAD -- src/Ambev.DeveloperEvaluation.ORM/Migrations | tail -1
```

Expected:
- only the untracked tickets and plans
- `0 Error(s)`, `2 Warning(s)`, `Build succeeded.` and no `warning CS`
- Unit U0 + 9, Integration I0 + 4 and Functional F0 + 8, compared against the baseline file
- `Scan complete: 0 issue(s) found`
- `No changes have been made to the model since the last migration.`
- no output from the migrations diff

| Ticket criterion | Evidence |
|---|---|
| `Sale.Delete()` sets `IsDeleted`, `DeletedAt` and `UpdatedAt`, records no event, and is allowed on a cancelled sale | `SaleTests` (Task 2) |
| Global query filter `!IsDeleted`: `GetByIdAsync` and `ListAsync` never return deleted sales | `SaleSoftDeleteTests` by-id and list tests (Task 3) |
| `ExistsBySaleNumberAsync` and the generator ignore the filter | `IgnoreQueryFilters()` in `ExistsBySaleNumberAsync`, which the generator calls (Decision 3); `SaleSoftDeleteTests` number and generator tests (Task 3) |
| The warning is ignored in `DefaultContext`, with a comment | `DefaultContextTests` (Task 4); Decision 4 on why it doesn't fire today |
| Validator (non-empty id); handler loads (404), deletes, saves | `DeleteSaleCommandValidatorTests`, `DeleteSaleHandlerTests` (Task 5); the functional empty-id 400 |
| `DELETE` 200 with exactly `{success, message}` through `Ok(message)`; deleting again is 404 | `SalesControllerTests`, `DeleteSaleTests` (Task 6); Task 7 Step 5 |
| Integration tests: filter and rows, `ExistsBySaleNumberAsync`, generator | `SaleSoftDeleteTests` |
| Unit tests: `Delete` fields, no event, cancelled; handler happy path and 404 | Tasks 2 and 5 |
| Functional tests: GET 404 and list omits; cancelled 200; reused number 409 | `DeleteSaleTests` |
| Row and items stay; no event published | `SaleSoftDeleteTests`; Task 7 Step 6 |
| `.http` gains the delete request | Task 7 |
| `feature/delete-sale`, pull request into `develop`, Conventional Commits, no attribution | Steps 4–6 below |

- [ ] **Step 2: Audit the tests (skill `test-anti-patterns`, report only)**

Run it on `SaleSoftDeleteTests`, `DefaultContextTests`, `DeleteSaleCommandValidatorTests`, `DeleteSaleHandlerTests`, `DeleteSaleTests`, and the new tests in `SaleTests` and `SalesControllerTests`.

These are deliberate. Report them and leave them:
- The `DateTime.UtcNow` bracketing in `SaleTests`: there's no clock abstraction (ticket 05, Decision 6).
- The `When … Do` capture in `DeleteSaleHandlerTests`: it checks the sale's state at the moment of the save.
- `DefaultContextTests` reads EF Core's options: the setting is the ticket's criterion, and the warning can't fire on this model (Decision 4).
- The status check inside `CreateDeletedSaleAsync` and the helper in `SaleSoftDeleteTests` guard setup steps.

Fix a real finding in a separate `test(sales): …` commit, then re-run Step 1.

- [ ] **Step 3: Optional code review**

Run superpowers:requesting-code-review on `develop..feature/delete-sale`, with `clean-code`, `dotnet-best-practices` and `efcore-patterns` as lenses. Findings that belong to ticket 13 go into the report, not this branch.

- [ ] **Step 4: Check the commit messages**

```bash
git log --format=%s develop..feature/delete-sale
git log --format=%B develop..feature/delete-sale | grep -n -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
```

Expected (plus any `test(sales):` commit from Step 2):

```
docs(http): soft-delete a sale
feat(sales): soft-delete a sale over HTTP
feat(sales): delete a sale in the application layer
chore(orm): ignore the required-navigation query-filter warning
feat(sales): hide soft-deleted sales with a global query filter
feat(sales): soft-delete a sale in the domain
no attribution lines
```

- [ ] **Step 5: Push the branch and open a pull request into `develop`**

This is the superpowers:finishing-a-development-branch step: open a pull request into `develop` for the user to review. Don't merge it yourself.

Write the pull request body with the Write tool to `/tmp/ticket12-checks/pr-body.md`. It holds:
- two or three sentences on what the ticket delivers (the Goal above)
- the evidence table from Step 1, with this run's results
- Decision 4 (the EF Core warning) and what Steps 2 and 3 reported

No `Co-Authored-By` trailer, no "Generated with" line and no session link. Then:

```bash
git push -u origin feature/delete-sale
gh pr create --base develop --head feature/delete-sale --title "Ticket 12: soft-delete a sale" --body-file /tmp/ticket12-checks/pr-body.md
```

Expected: the push creates `origin/feature/delete-sale`, and `gh pr create` prints the pull request URL. If `gh` fails (check `gh auth status`), stop and tell the user: the branch is pushed, so they can open the pull request on GitHub.

- [ ] **Step 6: Verify the pull request**

```bash
gh pr view feature/delete-sale --json state,baseRefName,commits --jq '"\(.state) \(.baseRefName) \(.commits | length) commits"'
gh pr view feature/delete-sale --json title,body --jq '.title + "\n" + .body' | grep -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
git status -sb | head -1
git status --short
```

Expected:
- `OPEN develop 6 commits`, or 7 with a `test(sales):` commit from Step 2
- `no attribution lines`
- `## feature/delete-sale...origin/feature/delete-sale`, with no `ahead` or `behind`
- only untracked tickets and plans

Don't merge the pull request. The user merges it on GitHub with **Create a merge commit**. Keep `feature/delete-sale`.

- [ ] **Step 7: Clean up**

```bash
docker compose ps -a
tasklist | grep -i 'Ambev.DeveloperEvaluation.WebApi' || echo "no API process left"
rm -rf /tmp/ticket12-checks
```

Expected: nothing listed, and `no API process left`.

- [ ] **Step 8: Report to the user**

1. The results of Step 1, the pull request URL, and any output that differed from this plan, with what systematic-debugging found. If nothing differed, say so.
2. Ask the user to send the three new requests once from their editor's `.http` runner.
3. Decision 4: spec §8.2 assumes EF Core warns about the `Sale`/`SaleItem` filter, but EF Core 8.0.10 doesn't warn on this model. The setting is in place anyway, as the ticket asks.
4. For ticket 13's README:
   - Delete is soft: the rows stay, `GET` and the list hide the sale, and nothing is published.
   - A deleted sale's number stays taken, both for clients and for the generator.
   - A cancelled sale can be deleted.
