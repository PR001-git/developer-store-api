# Cancel an Item (Ticket 08) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** An authenticated client cancels one line with `PATCH /api/sales/{id}/items/{itemId}/cancel`. The response is 200 with the sale: the line is cancelled, the total covers only the active lines, and `updatedAt` is set. `ItemCancelledEvent` is logged after the save. Cancelling the last active line also cancels the sale, with total 0, and `SaleCancelledEvent` follows `ItemCancelledEvent`. An unknown sale or item is a 404. A cancelled line or a cancelled sale is a 409 `BusinessRuleViolation`. Two concurrent changes to one sale can't both succeed: the second save fails on `xmin` and becomes 409 `ConcurrencyConflict`. No migration. Delivered on `feature/cancel-sale-item` as a pull request into `develop`.

**Architecture:** `Sale.CancelItem(itemId)` enforces R6, R7 and R9. It cancels the line through the new internal `SaleItem.Cancel()`, recalculates the total, sets `UpdatedAt` and records `ItemCancelledEvent`. When no active line is left, it also calls the private `MarkCancelled`, which it shares with `Cancel()`. `CancelSaleItemHandler` loads the sale with `GetByIdAsync` and checks that the item belongs to it (404 otherwise). It then calls `CancelItem`, saves with ticket 07's `UpdateAsync` and publishes through `SaleEventPublisher`. `SaleEventLogHandler` gains the `ItemCancelledEvent` interface, and `SalesController` gains the PATCH action. Every mutation writes the `Sales` row, so ticket 05's `xmin` token also guards item-only changes, and ticket 03's middleware maps `DbUpdateConcurrencyException` to 409.

**Tech Stack:** .NET SDK 10.0.200-preview building `net8.0`; ASP.NET Core 8; MediatR 12.4.1; FluentValidation 11.10.0; AutoMapper 13.0.1; EF Core 8.0.10 with Npgsql 8.0.8; xUnit 2.9.2, FluentAssertions 6.12.0, NSubstitute 5.1.0, Bogus 35.6.1; Testcontainers.PostgreSql 4.15.0 and Microsoft.AspNetCore.Mvc.Testing 8.0.31; Docker Desktop (WSL2) with Compose v2; curl and Python 3 from Git Bash; Slopwatch.Cmd 0.4.2 (global tool).

**Source:** `docs/superpowers/tickets/08-cancel-an-item.md`. Spec `docs/superpowers/specs/2026-09-24-sales-api-design.md`: D5, D9, R6, R7, R9, R13, R14, §5.3 (`CancelItem`), §5.4, §6 (CancelSaleItem), §7.1, §7.4 and §8.2. It builds on the code that `docs/superpowers/plans/2026-09-26-05-create-a-sale-and-read-it-back.md` (05) and `docs/superpowers/plans/2026-09-26-07-cancel-a-sale.md` (07) leave.

**Not rehearsed.** The expected outputs come from reading the plans for tickets 05, 06 and 07. Test counts are the baseline plus a delta, and Task 1 records the baseline. If an output differs, use superpowers:systematic-debugging before changing code.

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
- **Nothing else changes.** Update (09), listing (10, 11), soft delete (12) and the README (13) are out of scope, and so are the spec §9.2 issues, Users and Auth, Docker and compose files, `appsettings*.json`, the fixture classes (only `ApiHttpExtensions` gains a helper), migrations and `SaleConfiguration.cs` (Task 4 only touches it for a moment and restores it).

## Skills

### Project skills in `.claude/skills/`

| Skill | Use it? | When and how |
|---|---|---|
| `dotnet-best-practices` | **Yes, Tasks 2–6** | Load it before Task 2 and review every C# change against it. That means XML docs on public members, the `Application.Sales.CancelSaleItem` namespace, one folder per use case, constructor injection into `private readonly` fields, and `Given … When … Then …` names with `// Given`, `// When`, `// Then`. Throw `DomainException` or `KeyNotFoundException` and let the middleware answer. As in tickets 03–07, add no `ArgumentNullException` guards and no `ConfigureAwait(false)`. |
| `efcore-patterns` | **Yes, Task 4** | Load it before Task 4. Its project note is why `GetByIdAsync` stays tracked: change tracking writes the cancelled `SaleItem` and the `Sales` row, and the `xmin` token guards that row. No repository change and no migration. |
| `testcontainers-integration-tests` | **Yes, Tasks 4 and 6** | Load it before Task 4. Reuse ticket 02's fixtures unchanged: `[Collection(DatabaseCollection.Name)]`, `[Collection(ApiCollection.Name)]`, one `postgres:13` per project. The concurrency test opens two `DefaultContext`s on the same fixture. No new fixture or container. |
| `type-design-performance` | Light, Tasks 2–6 | Make the new types `sealed`: `ItemCancelledEvent`, the command, validator and handler, and the test classes. `SaleItem.Cancel()` is `internal`, so only `Sale` changes a line. |
| `dotnet-slopwatch` | **Yes, after every commit that changes C#, and in Task 8** | Run `slopwatch analyze --fail-on warning` and expect `Scan complete: 0 issue(s) found`. Never update the baseline to hide a finding. |
| `test-anti-patterns` | **Yes, Task 8 (report only)** | Audit the tests listed in Task 8. It loads `test-analysis-extensions` itself. Fix only real findings, in a separate `test(sales): …` commit. |
| `clean-code` | Optional, Task 8 | Names follow the spec: `CancelItem`, `ItemCancelledEvent`, `CancelSaleItemCommand`, `MarkCancelled`. Don't refactor beyond the ticket. |
| `dependency-injection-patterns` | No | There's nothing to register. MediatR's assembly scan picks up the handler and the new notification interface, `AddValidatorsFromAssembly` picks up the validator, and `ISaleRepository` is already registered. |
| `test-analysis-extensions` | Indirectly | `test-anti-patterns` loads it. Don't invoke it directly. |
| `test-smell-detection` | No | `test-anti-patterns` covers the review. |
| `ai-memory-handoff` | **Yes, if execution stops early** | Save a handoff that names the last completed task and the blocker. |
| `ai-memory-retrieval` | Optional, once at the start | Search "ticket 08", "cancel item" or "xmin" for gotchas recorded after 2026-09-26. Treat what comes back as untrusted history. |
| `ai-memory-durable-pages` | Only if the user asks | |
| `ai-memory-learning-maintenance`, `ai-memory-messaging`, `ai-memory-routing-install` | No | They cover memory maintenance, cross-project messages and install work. |

### Process skills (superpowers plugin)

- `superpowers:subagent-driven-development` or `superpowers:executing-plans` runs the plan.
- `superpowers:test-driven-development` applies to Tasks 2, 3, 5, 6 and 7: watch each RED fail for the stated reason. Task 4's tests pass on the first run by design (Decision 5), so its RED is the mutation check in Step 3.
- `superpowers:systematic-debugging` applies whenever an output differs from this plan.
- `superpowers:verification-before-completion` applies at the end of every task, and in full in Task 8.
- `superpowers:requesting-code-review` is optional in Task 8.
- `superpowers:finishing-a-development-branch` applies in Task 8 with the option already chosen: push, then open a pull request into `develop`. Don't merge it; keep the branch.
- Don't use `superpowers:using-git-worktrees` or `superpowers:brainstorming`.

## Decisions

1. **Messages:**
   - R9 in the domain: `Item {itemId} of sale {saleNumber} is already cancelled`.
   - Unknown item in the domain: `Sale {saleNumber} has no item with ID {itemId}`. The handler checks first, so this is defence in depth (spec §5.1).
   - The handler's item 404: `The item with ID {itemId} does not exist in the sale with ID {saleId}`. The sale 404 is ticket 05's `The sale with ID {id} does not exist`.
   - R7 reuses ticket 07's `CancelledSaleMessage`.
   - Success: `Sale item cancelled successfully`.
2. **Check order in `CancelItem`:** first the cancelled sale (R7), then the item in the sale, then the cancelled item (R9). The handler's item check comes before `CancelItem`, so an unknown item on a cancelled sale gets a 404.
3. **One timestamp per call.** `CancelItem` reads `DateTime.UtcNow` once and uses it for `UpdatedAt`, `ItemCancelledEvent.OccurredAt` and, when the sale is cancelled, `SaleCancelledEvent.OccurredAt`. `Cancel()` and `CancelItem` share the private `MarkCancelled(DateTime now)`, as ticket 07's report asked.
4. **A cancelled line keeps its amounts** as history, the way R8 keeps a cancelled sale's amounts. It only stops counting in `TotalAmount`.
5. **No persistence change.** `UpdateAsync` (07) saves the tracked aggregate, and `xmin` (05) guards the `Sales` row. `CancelItem` always changes `TotalAmount` and `UpdatedAt`, so the row is always written. Task 4's integration tests pin this and pass on the first run. A temporary mutation (removing `IsRowVersion()`) proves that the concurrency test can fail.
6. **Route:** `[HttpPatch("{id}/items/{itemId}/cancel")]` with no `:guid` constraint, like ticket 07. A malformed id gets a 400 from model binding.
7. **Functional tests** take lines by index from the POST response, which keeps the request order. After a PATCH the sale is reloaded, and item order isn't guaranteed, so assertions compare by id or with order-insensitive `BeEquivalentTo`.
8. **`.http`:** the new requests go after ticket 07's, and they create their own two-item sale, because ticket 07's requests have already cancelled `{{saleId}}`.
9. **Works whether or not ticket 06 is merged.** Every Edit anchor below is ticket 07 code, which ticket 06 doesn't touch. If an anchor isn't found, Read the file and make the same change around the current text.
10. **Delivery, as in tickets 05–07:** commit this plan on `develop` and push it, then open a pull request into `develop`. The user merges it on GitHub with **Create a merge commit**, which is the ticket's `--no-ff`. The branch is kept.

## Pre-existing output (leave it alone)

- `warning NU1903` (AutoMapper 13.0.1): a solution build shows `2 Warning(s)`.
- `NETSDK1057` (preview SDK); `MSB1011` from a bare `dotnet build` at the root, so always pass the `.sln` or a project; `LF will be replaced by CRLF` on new files.
- `dotnet ef` prints `[FTL] … HostAbortedException` and a stack trace. The commands below filter it out.

## File map

| Change | Paths |
|---|---|
| Created (src) | `src/Ambev.DeveloperEvaluation.Domain/Events/ItemCancelledEvent.cs`; `src/Ambev.DeveloperEvaluation.Application/Sales/CancelSaleItem/{CancelSaleItemCommand, CancelSaleItemCommandValidator, CancelSaleItemHandler}.cs` |
| Modified (src) | `src/Ambev.DeveloperEvaluation.Domain/Entities/{Sale, SaleItem}.cs`; `src/Ambev.DeveloperEvaluation.Application/Sales/SaleEventLogHandler.cs`; `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs`; `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http` |
| Created (tests) | `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CancelSaleItem/{CancelSaleItemCommandValidatorTests, CancelSaleItemHandlerTests}.cs`; `tests/Ambev.DeveloperEvaluation.Functional/Sales/CancelSaleItemTests.cs` |
| Modified (tests) | `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs`; `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/SaleEventLogHandlerTests.cs`; `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesControllerTests.cs`; `tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryTests.cs`; `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiHttpExtensions.cs` |
| Committed on `develop` first | this plan |
| Local only | `/tmp/ticket08-checks/` (removed at the end) |

Test deltas: Unit **+19**, Integration **+2**, Functional **+6**.

---

### Task 1: Check the starting point, commit this plan, branch and record the baseline

**Files:**
- Commit on `develop`: `docs/superpowers/plans/2026-09-26-08-cancel-an-item.md`

- [ ] **Step 1: Confirm ticket 07 is merged and ticket 08 hasn't started**

```bash
git switch develop
git pull --ff-only origin develop
git diff --quiet && git diff --cached --quiet && echo "no tracked changes"
git status --short
git log --oneline --merges | grep -oE "feature/[a-z-]+" | sort -u
git grep -n -E 'ItemCancelledEvent|CancelItem\(|CancelSaleItemCommand' -- src tests || echo "nothing of ticket 08 yet"
git branch --list feature/cancel-sale-item
```

Expected:
- the pull fast-forwards `develop` or prints `Already up to date.`
- `no tracked changes`
- `?? docs/superpowers/plans/2026-09-26-08-cancel-an-item.md` and `?? docs/superpowers/tickets/`, plus other untracked plans (leave them alone)
- the merged branches include `feature/create-sale` and `feature/cancel-sale` (and `feature/sale-number` if ticket 06 went first)
- `nothing of ticket 08 yet`
- no branch listed

Stop and ask the user if the pull fails, anything tracked is modified, `feature/cancel-sale` isn't merged, or `feature/cancel-sale-item` exists. In that last case, look for an ai-memory handoff first.

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
git add docs/superpowers/plans/2026-09-26-08-cancel-an-item.md
git commit -m "docs: add plan for cancelling a sale item"
git push origin develop
git switch -c feature/cancel-sale-item
git log --oneline -1
```

Expected: one file committed and pushed, then `Switched to a new branch 'feature/cancel-sale-item'`. If the push is rejected, stop and ask; never force it.

- [ ] **Step 4: Record the baseline**

```bash
mkdir -p /tmp/ticket08-checks
dotnet build Ambev.DeveloperEvaluation.sln --no-incremental 2>&1 | grep -E 'warning CS|Warning\(s\)|Error\(s\)|Build succeeded' | sort -u
dotnet test Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-110 | tee /tmp/ticket08-checks/baseline.txt
slopwatch analyze --fail-on warning 2>&1 | tail -1
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at ' | tail -1
```

Expected:
- `Build succeeded.`, `2 Warning(s)`, `0 Error(s)`, no `warning CS`
- three `Passed!` lines: Unit, Integration and Functional. Call their counts U0, I0 and F0. With tickets 05 and 07 they are 186, 10 and 31; with ticket 06 as well, 190, 16 and 36
- `Scan complete: 0 issue(s) found`
- `No changes have been made to the model since the last migration.`

If anything fails, stop.

---

### Task 2: Cancel an item in the domain

**Skills:** `dotnet-best-practices` (load it now), `type-design-performance`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.Domain/Events/ItemCancelledEvent.cs`
- Modify: `src/Ambev.DeveloperEvaluation.Domain/Entities/SaleItem.cs`
- Modify: `src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs`

- [ ] **Step 1: Write the failing tests**

In `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs`, replace the end of the file (ticket 07's last test)

```csharp
        act.Should().Throw<DomainException>().WithMessage($"Sale {sale.SaleNumber} is cancelled and cannot be modified");
        sale.UpdatedAt.Should().Be(updatedAt);
        sale.DomainEvents.Should().BeEmpty();
    }
}
```

with

```csharp
        act.Should().Throw<DomainException>().WithMessage($"Sale {sale.SaleNumber} is cancelled and cannot be modified");
        sale.UpdatedAt.Should().Be(updatedAt);
        sale.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R9: a cancelled line drops out of the total and keeps its amounts as history, and the sale stays open
    /// while another line is active.
    /// </summary>
    [Fact(DisplayName = "Given a sale with two active lines When cancelling one Then that line is cancelled, the total drops to the other line and the sale stays open")]
    public void Given_SaleWithTwoActiveLines_When_CancellingOne_Then_TotalDropsAndSaleStaysOpen()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m), SaleTestData.GenerateItem(3, 10.00m));
        var cancelled = sale.Items.First();
        var active = sale.Items.Last();

        // When
        sale.CancelItem(cancelled.Id);

        // Then (the cancelled line keeps its 16.20; 30.00 is left)
        cancelled.IsCancelled.Should().BeTrue();
        cancelled.TotalAmount.Should().Be(16.20m);
        active.IsCancelled.Should().BeFalse();
        sale.TotalAmount.Should().Be(30.00m);
        sale.IsCancelled.Should().BeFalse();
    }

    /// <summary>
    /// Tests rule R13: cancelling an item sets <see cref="Sale.UpdatedAt"/> to now, in UTC, so the <c>Sales</c> row
    /// is written and its concurrency token covers item-only changes.
    /// </summary>
    [Fact(DisplayName = "Given an open sale When cancelling an item Then UpdatedAt is set to now in UTC")]
    public void Given_OpenSale_When_CancellingItem_Then_UpdatedNowInUtc()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(), SaleTestData.GenerateItem());
        var before = DateTime.UtcNow;

        // When
        sale.CancelItem(sale.Items.First().Id);

        // Then
        var after = DateTime.UtcNow;
        sale.UpdatedAt.Should().NotBeNull().And.BeOnOrAfter(before).And.BeOnOrBefore(after);
        sale.UpdatedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests that cancelling a line records exactly one <see cref="ItemCancelledEvent"/> with the §5.4 payload,
    /// stamped with <see cref="Sale.UpdatedAt"/>.
    /// </summary>
    [Fact(DisplayName = "Given a loaded sale with two active lines When cancelling one Then it records one ItemCancelledEvent with the sale's and the line's data")]
    public void Given_LoadedSaleWithTwoActiveLines_When_CancellingOne_Then_RecordsItemCancelledEvent()
    {
        // Given (a loaded sale has no recorded events)
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(), SaleTestData.GenerateItem());
        sale.ClearDomainEvents();
        var item = sale.Items.First();

        // When
        sale.CancelItem(item.Id);

        // Then
        sale.DomainEvents.Should().ContainSingle().Which.Should().Be(new ItemCancelledEvent(
            SaleId: sale.Id,
            SaleNumber: sale.SaleNumber,
            ItemId: item.Id,
            ProductId: item.Product.Id,
            OccurredAt: sale.UpdatedAt!.Value));
    }

    /// <summary>
    /// Tests rule R6 and decision D5: cancelling the last active line cancels the sale too, the total becomes 0,
    /// and <see cref="ItemCancelledEvent"/> is recorded before <see cref="SaleCancelledEvent"/>, both with one timestamp.
    /// </summary>
    [Fact(DisplayName = "Given a sale with one active line left When cancelling it Then the sale is cancelled with total 0, and ItemCancelledEvent then SaleCancelledEvent are recorded")]
    public void Given_SaleWithOneActiveLineLeft_When_CancellingIt_Then_SaleCancelledWithZeroTotal()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m), SaleTestData.GenerateItem(3, 10.00m));
        sale.CancelItem(sale.Items.First().Id);
        sale.ClearDomainEvents();
        var last = sale.Items.Last();

        // When
        sale.CancelItem(last.Id);

        // Then
        sale.IsCancelled.Should().BeTrue();
        sale.TotalAmount.Should().Be(0m);
        sale.Items.Should().OnlyContain(item => item.IsCancelled);
        sale.DomainEvents.Should().Equal(
            new ItemCancelledEvent(sale.Id, sale.SaleNumber, last.Id, last.Product.Id, sale.UpdatedAt!.Value),
            new SaleCancelledEvent(sale.Id, sale.SaleNumber, sale.UpdatedAt!.Value));
    }

    /// <summary>
    /// Tests rule R7: a cancelled sale is read-only, so cancelling one of its lines is rejected and changes nothing.
    /// </summary>
    [Fact(DisplayName = "Given a cancelled sale When cancelling one of its lines Then it throws DomainException and changes nothing")]
    public void Given_CancelledSale_When_CancellingLine_Then_ThrowsAndChangesNothing()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m));
        sale.Cancel();
        sale.ClearDomainEvents();
        var updatedAt = sale.UpdatedAt;
        var item = sale.Items.Single();

        // When
        var act = () => sale.CancelItem(item.Id);

        // Then
        act.Should().Throw<DomainException>().WithMessage($"Sale {sale.SaleNumber} is cancelled and cannot be modified");
        item.IsCancelled.Should().BeFalse();
        sale.TotalAmount.Should().Be(16.20m);
        sale.UpdatedAt.Should().Be(updatedAt);
        sale.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R9: a line that is already cancelled can't be cancelled again, and nothing changes.
    /// </summary>
    [Fact(DisplayName = "Given a cancelled line When cancelling it again Then it throws DomainException and changes nothing")]
    public void Given_CancelledLine_When_CancellingAgain_Then_ThrowsAndChangesNothing()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m), SaleTestData.GenerateItem(3, 10.00m));
        var item = sale.Items.First();
        sale.CancelItem(item.Id);
        sale.ClearDomainEvents();
        var updatedAt = sale.UpdatedAt;

        // When
        var act = () => sale.CancelItem(item.Id);

        // Then
        act.Should().Throw<DomainException>().WithMessage($"Item {item.Id} of sale {sale.SaleNumber} is already cancelled");
        sale.TotalAmount.Should().Be(30.00m);
        sale.IsCancelled.Should().BeFalse();
        sale.UpdatedAt.Should().Be(updatedAt);
        sale.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Tests the domain's defence in depth: an id that isn't one of the sale's lines is rejected, and nothing changes.
    /// </summary>
    [Fact(DisplayName = "Given an item id that isn't in the sale When cancelling it Then it throws DomainException and changes nothing")]
    public void Given_ItemIdNotInSale_When_CancellingIt_Then_ThrowsAndChangesNothing()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m));
        sale.ClearDomainEvents();
        var itemId = Guid.NewGuid();

        // When
        var act = () => sale.CancelItem(itemId);

        // Then
        act.Should().Throw<DomainException>().WithMessage($"Sale {sale.SaleNumber} has no item with ID {itemId}");
        sale.Items.Should().OnlyContain(item => !item.IsCancelled);
        sale.UpdatedAt.Should().BeNull();
        sale.DomainEvents.Should().BeEmpty();
    }
}
```

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected: `CS1061: 'Sale' does not contain a definition for 'CancelItem'` (several lines) and `CS0246: The type or namespace name 'ItemCancelledEvent' could not be found`.

- [ ] **Step 3: Write the event**

Create `src/Ambev.DeveloperEvaluation.Domain/Events/ItemCancelledEvent.cs`:

```csharp
namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>
/// A line of a sale was cancelled. It no longer counts in the sale total (rule R9).
/// </summary>
/// <param name="SaleId">The id of the sale.</param>
/// <param name="SaleNumber">The sale number.</param>
/// <param name="ItemId">The id of the cancelled line.</param>
/// <param name="ProductId">The id of the line's product.</param>
/// <param name="OccurredAt">When the line was cancelled, in UTC.</param>
public sealed record ItemCancelledEvent(Guid SaleId, string SaleNumber, Guid ItemId, Guid ProductId, DateTime OccurredAt) : IDomainEvent;
```

- [ ] **Step 4: Let a line be cancelled by its sale**

In `src/Ambev.DeveloperEvaluation.Domain/Entities/SaleItem.cs`, replace

```csharp
    public bool IsCancelled { get; private set; }
}
```

with

```csharp
    public bool IsCancelled { get; private set; }

    /// <summary>
    /// Cancels the line. Its amounts stay as they were, as history; the sale stops counting it in its total.
    /// </summary>
    internal void Cancel() => IsCancelled = true;
}
```

- [ ] **Step 5: Add `CancelItem` and share `MarkCancelled` with `Cancel`**

In `src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs`, replace

```csharp
    public void Cancel()
    {
        EnsureNotCancelled();

        var now = DateTime.UtcNow;
        IsCancelled = true;
        UpdatedAt = now;
        _domainEvents.Add(new SaleCancelledEvent(Id, SaleNumber, now));
    }
```

with

```csharp
    public void Cancel()
    {
        EnsureNotCancelled();

        MarkCancelled(DateTime.UtcNow);
    }

    /// <summary>
    /// Cancels one line (rule R9): it drops out of the total and keeps its amounts as history, <see cref="UpdatedAt"/>
    /// is set and an <see cref="ItemCancelledEvent"/> is recorded. When no active line is left, the sale is cancelled
    /// too, its total is 0, and a <see cref="SaleCancelledEvent"/> follows (rule R6).
    /// </summary>
    /// <param name="itemId">The id of the line.</param>
    /// <exception cref="DomainException">
    /// Thrown when the sale is cancelled (rule R7), the line isn't in the sale, or the line is already cancelled (rule R9).
    /// </exception>
    public void CancelItem(Guid itemId)
    {
        EnsureNotCancelled();

        var item = _items.SingleOrDefault(line => line.Id == itemId)
            ?? throw new DomainException($"Sale {SaleNumber} has no item with ID {itemId}");
        if (item.IsCancelled)
            throw new DomainException($"Item {itemId} of sale {SaleNumber} is already cancelled");

        var now = DateTime.UtcNow;
        item.Cancel();
        RecalculateTotal();
        UpdatedAt = now;
        _domainEvents.Add(new ItemCancelledEvent(Id, SaleNumber, item.Id, item.Product.Id, now));

        if (_items.TrueForAll(line => line.IsCancelled))
            MarkCancelled(now);
    }
```

Then replace

```csharp
    private void EnsureNotCancelled()
    {
        if (IsCancelled)
            throw new DomainException(CancelledSaleMessage(SaleNumber));
    }
```

with

```csharp
    private void EnsureNotCancelled()
    {
        if (IsCancelled)
            throw new DomainException(CancelledSaleMessage(SaleNumber));
    }

    private void MarkCancelled(DateTime now)
    {
        IsCancelled = true;
        UpdatedAt = now;
        _domainEvents.Add(new SaleCancelledEvent(Id, SaleNumber, now));
    }
```

- [ ] **Step 6: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~Domain.Entities.SaleTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)`, `Build succeeded.`; SaleTests `Passed:    39` (ticket 07 left 32; ticket 07's four `Cancel` tests still pass after the refactor); Unit U0 + 7.

- [ ] **Step 7: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Domain/Events/ItemCancelledEvent.cs src/Ambev.DeveloperEvaluation.Domain/Entities/SaleItem.cs src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs
git commit -m "feat(sales): cancel an item in the domain" -m "Sale.CancelItem cancels one line, recalculates the total over the active lines, sets UpdatedAt and records ItemCancelledEvent. Cancelling the last active line also cancels the sale with total 0 and records SaleCancelledEvent after ItemCancelledEvent (rule R6). It rejects a cancelled sale (rule R7), an item that isn't in the sale and an item that is already cancelled (rule R9) with DomainException. Cancel and CancelItem share MarkCancelled."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `4 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 3: Log `ItemCancelledEvent`

**Skill:** `dotnet-best-practices`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/SaleEventLogHandlerTests.cs`
- Modify: `src/Ambev.DeveloperEvaluation.Application/Sales/SaleEventLogHandler.cs`

- [ ] **Step 1: Write the failing test**

In `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/SaleEventLogHandlerTests.cs`, replace the end of ticket 07's test

```csharp
            new KeyValuePair<string, object?>("@Event", saleCancelled),
            new KeyValuePair<string, object?>("{OriginalFormat}", "Sale event {EventName} published {@Event}"));
    }
```

with

```csharp
            new KeyValuePair<string, object?>("@Event", saleCancelled),
            new KeyValuePair<string, object?>("{OriginalFormat}", "Sale event {EventName} published {@Event}"));
    }

    /// <summary>
    /// Tests that an <see cref="ItemCancelledEvent"/> published as <see cref="IDomainEvent"/> reaches the log handler
    /// and becomes one structured Information entry.
    /// </summary>
    [Fact(DisplayName = "Given MediatR with the Application handlers When an ItemCancelledEvent is published as IDomainEvent Then the log handler writes one structured Information entry with it")]
    public async Task Given_MediatRWithApplicationHandlers_When_ItemCancelledEventPublished_Then_LogHandlerWritesIt()
    {
        // Given
        var services = new ServiceCollection();
        services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(ApplicationLayer).Assembly));
        services.AddSingleton(_logger);
        await using var provider = services.BuildServiceProvider();
        IDomainEvent itemCancelled = new ItemCancelledEvent(Guid.NewGuid(), "S-000123", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        // When
        await provider.GetRequiredService<IPublisher>().Publish(itemCancelled);

        // Then
        var logCall = _logger.ReceivedCalls().Should().ContainSingle(call => call.GetMethodInfo().Name == nameof(ILogger.Log)).Subject;
        logCall.GetArguments()[0].Should().Be(LogLevel.Information);
        logCall.GetArguments()[2].Should().BeAssignableTo<IReadOnlyList<KeyValuePair<string, object?>>>().Which.Should().Equal(
            new KeyValuePair<string, object?>("EventName", "ItemCancelledEvent"),
            new KeyValuePair<string, object?>("@Event", itemCancelled),
            new KeyValuePair<string, object?>("{OriginalFormat}", "Sale event {EventName} published {@Event}"));
    }
```

- [ ] **Step 2: Run it and watch it fail (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~SaleEventLogHandlerTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: the build succeeds; `Failed:     1, Passed:     3`. No handler receives the event, so the logger gets no `Log` call.

- [ ] **Step 3: Handle the event**

In `src/Ambev.DeveloperEvaluation.Application/Sales/SaleEventLogHandler.cs`, replace

```csharp
    INotificationHandler<SaleCreatedEvent>,
    INotificationHandler<SaleCancelledEvent>
```

with

```csharp
    INotificationHandler<SaleCreatedEvent>,
    INotificationHandler<SaleCancelledEvent>,
    INotificationHandler<ItemCancelledEvent>
```

Then replace

```csharp
    public Task Handle(SaleCancelledEvent notification, CancellationToken cancellationToken) => Log(notification);
```

with

```csharp
    public Task Handle(SaleCancelledEvent notification, CancellationToken cancellationToken) => Log(notification);

    /// <summary>
    /// Logs an <see cref="ItemCancelledEvent"/>.
    /// </summary>
    /// <param name="notification">The event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    public Task Handle(ItemCancelledEvent notification, CancellationToken cancellationToken) => Log(notification);
```

- [ ] **Step 4: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~SaleEventLogHandlerTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)`; `Passed:     4`; Unit U0 + 8.

- [ ] **Step 5: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Application/Sales/SaleEventLogHandler.cs tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/SaleEventLogHandlerTests.cs
git commit -m "feat(sales): log ItemCancelledEvent" -m "SaleEventLogHandler also handles ItemCancelledEvent, writing the same structured entry as for the other sale events."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `2 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 4: Pin item cancellation and the `xmin` check against PostgreSQL

**Skills:** `efcore-patterns`, `testcontainers-integration-tests` (load both now).

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryTests.cs`
- Touch and restore: `src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleConfiguration.cs` (Step 3 only)

- [ ] **Step 1: Write the integration tests**

In `tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryTests.cs`, replace the end of ticket 07's last test

```csharp
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Only a sale loaded with GetByIdAsync can be updated");
    }
```

with

```csharp
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Only a sale loaded with GetByIdAsync can be updated");
    }

    /// <summary>
    /// Tests that cancelling an item of a loaded sale is saved: a new context reads that item cancelled,
    /// the total over the active item, and <c>UpdatedAt</c> set.
    /// </summary>
    [Fact(DisplayName = "Given a loaded sale with an item cancelled When updating it Then a new context reads the item cancelled and the total dropped")]
    public async Task Given_LoadedSaleWithItemCancelled_When_Updating_Then_NewContextReadsItemCancelled()
    {
        // Given
        var sale = SaleTestData.GenerateValidSale(itemCount: 2);
        var cancelledId = sale.Items.First().Id;
        var active = sale.Items.Last();
        await using (var createContext = _database.CreateContext())
        {
            await new SaleRepository(createContext).CreateAsync(sale);
        }

        await using var updateContext = _database.CreateContext();
        var repository = new SaleRepository(updateContext);
        var loaded = await repository.GetByIdAsync(sale.Id);
        loaded!.CancelItem(cancelledId);

        // When
        await repository.UpdateAsync(loaded);

        // Then
        await using var readContext = _database.CreateContext();
        var saved = await new SaleRepository(readContext).GetByIdAsync(sale.Id);
        saved.Should().NotBeNull();
        saved!.IsCancelled.Should().BeFalse();
        saved.TotalAmount.Should().Be(active.TotalAmount);
        saved.UpdatedAt.Should().BeCloseTo(loaded.UpdatedAt!.Value, TimeSpan.FromMicroseconds(1));
        saved.Items.Should().ContainSingle(item => item.IsCancelled).Which.Id.Should().Be(cancelledId);
    }

    /// <summary>
    /// Tests spec decision D9: two requests load the same sale and each cancel a different item. The first save wins.
    /// The second is refused by the <c>xmin</c> token on the <c>Sales</c> row, which every mutation writes, and rolls
    /// back whole. Without the token both lines would be cancelled while the sale stayed open, breaking rule R6.
    /// </summary>
    [Fact(DisplayName = "Given two contexts that loaded the same sale When each cancels a different item and saves Then the second save throws DbUpdateConcurrencyException and only the first change is stored")]
    public async Task Given_TwoContextsLoadedSameSale_When_EachCancelsDifferentItem_Then_SecondSaveThrowsConcurrencyException()
    {
        // Given
        var sale = SaleTestData.GenerateValidSale(itemCount: 2);
        var firstItemId = sale.Items.First().Id;
        var secondItem = sale.Items.Last();
        await using (var createContext = _database.CreateContext())
        {
            await new SaleRepository(createContext).CreateAsync(sale);
        }

        await using var firstContext = _database.CreateContext();
        await using var secondContext = _database.CreateContext();
        var firstRepository = new SaleRepository(firstContext);
        var secondRepository = new SaleRepository(secondContext);
        var firstLoad = await firstRepository.GetByIdAsync(sale.Id);
        var secondLoad = await secondRepository.GetByIdAsync(sale.Id);
        firstLoad!.CancelItem(firstItemId);
        secondLoad!.CancelItem(secondItem.Id);
        await firstRepository.UpdateAsync(firstLoad);

        // When
        var act = () => secondRepository.UpdateAsync(secondLoad);

        // Then
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
        await using var readContext = _database.CreateContext();
        var saved = await new SaleRepository(readContext).GetByIdAsync(sale.Id);
        saved!.IsCancelled.Should().BeFalse();
        saved.TotalAmount.Should().Be(secondItem.TotalAmount);
        saved.Items.Should().ContainSingle(item => item.IsCancelled).Which.Id.Should().Be(firstItemId);
    }
```

The file already has `using Microsoft.EntityFrameworkCore;` (ticket 05), which provides `DbUpdateConcurrencyException`.

- [ ] **Step 2: Run them (they pass on the first run)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Integration 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build --filter "FullyQualifiedName~SaleRepositoryTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected: `0 Error(s)`; no failures. Tickets 05 and 07 already provide this behaviour (Decision 5).

- [ ] **Step 3: Prove the concurrency test can fail (mutation check)**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleConfiguration.cs`, replace

```csharp
        builder.Property<uint>(RowVersion).IsRowVersion();
```

with

```csharp
        // builder.Property<uint>(RowVersion).IsRowVersion();
```

Then:

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Integration 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build --filter "FullyQualifiedName~SecondSaveThrowsConcurrencyException" 2>&1 | grep -E 'Passed!|Failed!|Expected a' | cut -c1-160
git restore src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleConfiguration.cs
git status --short src
```

Expected: `Failed:     1`, with `Expected a <Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException> to be thrown, but no exception was thrown.` After the restore, `git status --short src` prints nothing.

- [ ] **Step 4: Verify (GREEN)**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-110
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at ' | tail -1
```

Expected: `0 Error(s)`; Integration I0 + 2; `No changes have been made to the model since the last migration.`

- [ ] **Step 5: Commit and scan**

```bash
git add tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryTests.cs
git commit -m "test(sales): pin item cancellation and the xmin concurrency check" -m "A cancelled item is saved through UpdateAsync with the new total and UpdatedAt. Two contexts that cancel different items of the same sale can't both save: the second gets DbUpdateConcurrencyException from the xmin token on the Sales row, which every mutation writes, and nothing of it is stored."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `1 file changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 5: Cancel a sale item and publish its events after the save

**Skills:** `dotnet-best-practices`; `test-anti-patterns` project note (`Received.InOrder` is the behaviour under test).

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CancelSaleItem/CancelSaleItemCommandValidatorTests.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CancelSaleItem/CancelSaleItemHandlerTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.Application/Sales/CancelSaleItem/{CancelSaleItemCommand, CancelSaleItemCommandValidator, CancelSaleItemHandler}.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CancelSaleItem/CancelSaleItemCommandValidatorTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;
using FluentValidation.TestHelper;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.CancelSaleItem;

/// <summary>
/// Contains unit tests for <see cref="CancelSaleItemCommandValidator"/>.
/// </summary>
public sealed class CancelSaleItemCommandValidatorTests
{
    private readonly CancelSaleItemCommandValidator _validator = new();

    /// <summary>
    /// Tests that the sale id is required.
    /// </summary>
    [Fact(DisplayName = "Given an empty sale id When validating Then SaleId fails as empty")]
    public void Given_EmptySaleId_When_Validating_Then_SaleIdFails()
    {
        // When
        var result = _validator.TestValidate(new CancelSaleItemCommand(Guid.Empty, Guid.NewGuid()));

        // Then
        result.ShouldHaveValidationErrorFor(command => command.SaleId).WithErrorMessage("'Sale Id' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that the item id is required.
    /// </summary>
    [Fact(DisplayName = "Given an empty item id When validating Then ItemId fails as empty")]
    public void Given_EmptyItemId_When_Validating_Then_ItemIdFails()
    {
        // When
        var result = _validator.TestValidate(new CancelSaleItemCommand(Guid.NewGuid(), Guid.Empty));

        // Then
        result.ShouldHaveValidationErrorFor(command => command.ItemId).WithErrorMessage("'Item Id' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that any other pair of ids passes.
    /// </summary>
    [Fact(DisplayName = "Given a sale id and an item id When validating Then there are no failures")]
    public void Given_SaleIdAndItemId_When_Validating_Then_NoFailures()
    {
        // When
        var result = _validator.TestValidate(new CancelSaleItemCommand(Guid.NewGuid(), Guid.NewGuid()));

        // Then
        result.ShouldNotHaveAnyValidationErrors();
    }
}
```

Create `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CancelSaleItem/CancelSaleItemHandlerTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using AutoMapper;
using FluentAssertions;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.CancelSaleItem;

/// <summary>
/// Contains unit tests for <see cref="CancelSaleItemHandler"/>. The mapper is the real Sales profile.
/// </summary>
public sealed class CancelSaleItemHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly CancelSaleItemHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="CancelSaleItemHandlerTests"/> class.
    /// </summary>
    public CancelSaleItemHandlerTests()
    {
        var mapper = new MapperConfiguration(config => config.AddProfile<SaleProfile>()).CreateMapper();
        _handler = new CancelSaleItemHandler(_saleRepository, _publisher, mapper);
    }

    /// <summary>
    /// Tests that the handler cancels the line, saves the sale and returns it open, with the total over the other line.
    /// </summary>
    [Fact(DisplayName = "Given a sale with two active lines When cancelling one Then it saves the sale and returns it open with that line cancelled and the total dropped")]
    public async Task Given_SaleWithTwoActiveLines_When_CancellingOne_Then_SavesAndReturnsTotalDropped()
    {
        // Given
        var sale = GivenLoadedSale();
        var cancelled = sale.Items.First();
        var active = sale.Items.Last();

        // When
        var result = await _handler.Handle(new CancelSaleItemCommand(sale.Id, cancelled.Id), CancellationToken.None);

        // Then
        await _saleRepository.Received(1).UpdateAsync(sale, Arg.Any<CancellationToken>());
        result.UpdatedAt.Should().NotBeNull();
        result.Should().BeEquivalentTo(new
        {
            sale.Id,
            IsCancelled = false,
            TotalAmount = 30.00m,
            sale.UpdatedAt,
            Items = new[]
            {
                new { cancelled.Id, TotalAmount = 16.20m, IsCancelled = true },
                new { active.Id, TotalAmount = 30.00m, IsCancelled = false }
            }
        });
    }

    /// <summary>
    /// Tests that an unknown sale is a not-found error, and nothing is saved or published.
    /// </summary>
    [Fact(DisplayName = "Given no sale with the id When cancelling an item Then it throws KeyNotFoundException and saves and publishes nothing")]
    public async Task Given_NoSaleWithId_When_CancellingItem_Then_ThrowsKeyNotFoundException()
    {
        // Given
        var saleId = Guid.NewGuid();
        _saleRepository.GetByIdAsync(saleId, Arg.Any<CancellationToken>()).Returns((Sale?)null);

        // When
        var act = () => _handler.Handle(new CancelSaleItemCommand(saleId, Guid.NewGuid()), CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"The sale with ID {saleId} does not exist");
        await _saleRepository.DidNotReceive().UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        _publisher.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R9: an item that isn't in the sale is a not-found error, and nothing is saved or published.
    /// </summary>
    [Fact(DisplayName = "Given an item id that isn't in the sale When cancelling it Then it throws KeyNotFoundException and saves and publishes nothing")]
    public async Task Given_ItemIdNotInSale_When_CancellingIt_Then_ThrowsKeyNotFoundException()
    {
        // Given
        var sale = GivenLoadedSale();
        var itemId = Guid.NewGuid();

        // When
        var act = () => _handler.Handle(new CancelSaleItemCommand(sale.Id, itemId), CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"The item with ID {itemId} does not exist in the sale with ID {sale.Id}");
        await _saleRepository.DidNotReceive().UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        _publisher.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// Tests that a domain rejection (rule R9) propagates, and nothing is saved or published.
    /// </summary>
    [Fact(DisplayName = "Given a cancelled line When cancelling it again Then DomainException propagates and nothing is saved or published")]
    public async Task Given_CancelledLine_When_CancellingAgain_Then_DomainExceptionAndNothingSaved()
    {
        // Given
        var sale = GivenLoadedSale();
        var item = sale.Items.First();
        sale.CancelItem(item.Id);
        sale.ClearDomainEvents();

        // When
        var act = () => _handler.Handle(new CancelSaleItemCommand(sale.Id, item.Id), CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<DomainException>().WithMessage($"Item {item.Id} of sale {sale.SaleNumber} is already cancelled");
        await _saleRepository.DidNotReceive().UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        _publisher.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R14: <see cref="ItemCancelledEvent"/> is published after the save, and then the events are cleared.
    /// </summary>
    [Fact(DisplayName = "Given a sale with two active lines When cancelling one Then it publishes ItemCancelledEvent after the save and clears the events")]
    public async Task Given_SaleWithTwoActiveLines_When_CancellingOne_Then_PublishesItemCancelledEventAfterSave()
    {
        // Given
        var sale = GivenLoadedSale();
        var item = sale.Items.First();
        var published = CapturePublishedEvents();

        // When
        await _handler.Handle(new CancelSaleItemCommand(sale.Id, item.Id), CancellationToken.None);

        // Then
        Received.InOrder(() =>
        {
            _saleRepository.UpdateAsync(sale, Arg.Any<CancellationToken>());
            _publisher.Publish(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
        });
        published.Should().ContainSingle().Which.Should().Be(
            new ItemCancelledEvent(sale.Id, sale.SaleNumber, item.Id, item.Product.Id, sale.UpdatedAt!.Value));
        sale.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Tests rules R6 and R14: cancelling the last active line returns the sale cancelled with total 0, and publishes
    /// <see cref="ItemCancelledEvent"/> then <see cref="SaleCancelledEvent"/>, both after the save.
    /// </summary>
    [Fact(DisplayName = "Given a sale with one active line left When cancelling it Then it returns the sale cancelled and publishes ItemCancelledEvent then SaleCancelledEvent after the save")]
    public async Task Given_SaleWithOneActiveLineLeft_When_CancellingIt_Then_PublishesBothEventsInOrderAfterSave()
    {
        // Given
        var sale = GivenLoadedSale();
        sale.CancelItem(sale.Items.First().Id);
        sale.ClearDomainEvents();
        var last = sale.Items.Last();
        var published = CapturePublishedEvents();

        // When
        var result = await _handler.Handle(new CancelSaleItemCommand(sale.Id, last.Id), CancellationToken.None);

        // Then
        Received.InOrder(() =>
        {
            _saleRepository.UpdateAsync(sale, Arg.Any<CancellationToken>());
            _publisher.Publish(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
            _publisher.Publish(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
        });
        published.Should().Equal(
            new ItemCancelledEvent(sale.Id, sale.SaleNumber, last.Id, last.Product.Id, sale.UpdatedAt!.Value),
            new SaleCancelledEvent(sale.Id, sale.SaleNumber, sale.UpdatedAt!.Value));
        result.IsCancelled.Should().BeTrue();
        result.TotalAmount.Should().Be(0m);
    }

    /// <summary>
    /// Tests rule R14: when the save fails, nothing is published.
    /// </summary>
    [Fact(DisplayName = "Given the save throws When cancelling an item Then the exception propagates and nothing is published")]
    public async Task Given_SaveThrows_When_CancellingItem_Then_NothingIsPublished()
    {
        // Given
        var sale = GivenLoadedSale();
        _saleRepository.UpdateAsync(sale, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("The database is unavailable"));

        // When
        var act = () => _handler.Handle(new CancelSaleItemCommand(sale.Id, sale.Items.First().Id), CancellationToken.None);

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

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected: `CS0234: The type or namespace name 'CancelSaleItem' does not exist in the namespace 'Ambev.DeveloperEvaluation.Application.Sales'` in both files, and `CS0246` for `CancelSaleItemCommandValidator`, `CancelSaleItemHandler` and `CancelSaleItemCommand`.

- [ ] **Step 3: Write the command, its validator and the handler**

Create `src/Ambev.DeveloperEvaluation.Application/Sales/CancelSaleItem/CancelSaleItemCommand.cs`:

```csharp
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;

/// <summary>
/// Cancels one line of a sale. Cancelling the last active line also cancels the sale.
/// </summary>
/// <param name="SaleId">The id of the sale.</param>
/// <param name="ItemId">The id of the line.</param>
public sealed record CancelSaleItemCommand(Guid SaleId, Guid ItemId) : IRequest<SaleResult>;
```

Create `src/Ambev.DeveloperEvaluation.Application/Sales/CancelSaleItem/CancelSaleItemCommandValidator.cs`:

```csharp
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;

/// <summary>
/// Validates <see cref="CancelSaleItemCommand"/> before its handler runs.
/// </summary>
public sealed class CancelSaleItemCommandValidator : AbstractValidator<CancelSaleItemCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CancelSaleItemCommandValidator"/> class: both ids are required.
    /// </summary>
    public CancelSaleItemCommandValidator()
    {
        RuleFor(command => command.SaleId).NotEmpty();
        RuleFor(command => command.ItemId).NotEmpty();
    }
}
```

Create `src/Ambev.DeveloperEvaluation.Application/Sales/CancelSaleItem/CancelSaleItemHandler.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;

/// <summary>
/// Handles <see cref="CancelSaleItemCommand"/>: the sale cancels its line, the repository saves it,
/// and only then are the recorded events published.
/// </summary>
public sealed class CancelSaleItemHandler : IRequestHandler<CancelSaleItemCommand, SaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IPublisher _publisher;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="CancelSaleItemHandler"/> class.
    /// </summary>
    /// <param name="saleRepository">The sale repository.</param>
    /// <param name="publisher">The MediatR publisher for the recorded events.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    public CancelSaleItemHandler(ISaleRepository saleRepository, IPublisher publisher, IMapper mapper)
    {
        _saleRepository = saleRepository;
        _publisher = publisher;
        _mapper = mapper;
    }

    /// <summary>
    /// Loads the sale, cancels the line, saves the sale, then publishes its events.
    /// </summary>
    /// <param name="command">The validated command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The sale after the change.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when there is no sale with the id, or the item isn't in it.</exception>
    public async Task<SaleResult> Handle(CancelSaleItemCommand command, CancellationToken cancellationToken)
    {
        var sale = await _saleRepository.GetByIdAsync(command.SaleId, cancellationToken)
            ?? throw new KeyNotFoundException($"The sale with ID {command.SaleId} does not exist");

        if (!sale.Items.Any(item => item.Id == command.ItemId))
            throw new KeyNotFoundException($"The item with ID {command.ItemId} does not exist in the sale with ID {command.SaleId}");

        sale.CancelItem(command.ItemId);

        await _saleRepository.UpdateAsync(sale, cancellationToken);
        await _publisher.PublishDomainEventsAsync(sale, cancellationToken);

        return _mapper.Map<SaleResult>(sale);
    }
}
```

When the sale or the item is already cancelled, `CancelItem` throws `DomainException` and nothing is saved; the middleware answers 409. When a concurrent save wins, `UpdateAsync` throws `DbUpdateConcurrencyException`, nothing is published, and the middleware answers 409 `ConcurrencyConflict`.

- [ ] **Step 4: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~Sales.CancelSaleItem" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)`; `Passed:    10`; Unit U0 + 18.

- [ ] **Step 5: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Application/Sales/CancelSaleItem/CancelSaleItemCommand.cs src/Ambev.DeveloperEvaluation.Application/Sales/CancelSaleItem/CancelSaleItemCommandValidator.cs src/Ambev.DeveloperEvaluation.Application/Sales/CancelSaleItem/CancelSaleItemHandler.cs tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CancelSaleItem/CancelSaleItemCommandValidatorTests.cs tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CancelSaleItem/CancelSaleItemHandlerTests.cs
git commit -m "feat(sales): cancel a sale item and publish its events after the save" -m "CancelSaleItemCommand has a validator that requires both ids. CancelSaleItemHandler throws KeyNotFoundException for an unknown sale or an item that isn't in the sale, cancels the item, saves with UpdateAsync and only then publishes ItemCancelledEvent, followed by SaleCancelledEvent when the last active item was cancelled (rules R6 and R14)."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `5 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 6: Cancel a sale item over HTTP

**Skills:** `dotnet-best-practices`, `testcontainers-integration-tests`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesControllerTests.cs`
- Modify: `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiHttpExtensions.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Functional/Sales/CancelSaleItemTests.cs`
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs`

- [ ] **Step 1: Write the failing controller test**

In `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesControllerTests.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
```

with

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;
```

Then replace

```csharp
            $$"""{"success":true,"message":"Sale cancelled successfully","data":{{cancelledSaleJson}}}""");
    }
```

with

```csharp
            $$"""{"success":true,"message":"Sale cancelled successfully","data":{{cancelledSaleJson}}}""");
    }

    /// <summary>
    /// Tests that cancelling an item sends the command for the route ids and returns 200 with the sale in the envelope.
    /// </summary>
    [Fact(DisplayName = "Given a sale id and an item id When cancelling the item Then it sends the command and returns 200 with the sale")]
    public async Task Given_SaleIdAndItemId_When_CancellingSaleItem_Then_Returns200WithSale()
    {
        // Given (the example sale's only line was cancelled, so the sale was cancelled too)
        const string saleJson =
            """{"id":"7f9c2a44-5555-4d1e-8a3b-000000000010","saleNumber":"S-000123","saleDate":"2026-09-24T14:30:00Z","customerId":"3f2b8c1e-1111-4a5b-9c2d-000000000001","customerName":"Maria Silva","branchId":"3f2b8c1e-2222-4a5b-9c2d-000000000002","branchName":"Filial Centro","totalAmount":0,"isCancelled":true,"createdAt":"2026-09-24T14:31:02Z","updatedAt":"2026-09-25T10:00:00Z","items":[{"id":"7f9c2a44-6666-4d1e-8a3b-000000000011","productId":"3f2b8c1e-3333-4a5b-9c2d-000000000003","productName":"Cerveja 350ml","quantity":5,"unitPrice":4.50,"discountPercentage":10,"discountAmount":2.25,"totalAmount":20.25,"isCancelled":true}]}""";
        var result = ExampleResult();
        result.TotalAmount = 0m;
        result.IsCancelled = true;
        result.UpdatedAt = new DateTime(2026, 9, 25, 10, 0, 0, DateTimeKind.Utc);
        result.Items[0].IsCancelled = true;
        var itemId = result.Items[0].Id;
        _mediator.Send(new CancelSaleItemCommand(result.Id, itemId), Arg.Any<CancellationToken>()).Returns(result);

        // When
        var response = await _controller.CancelSaleItem(result.Id, itemId, CancellationToken.None);

        // Then
        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        MvcJson.Serialize(ok.Value).Should().Be(
            $$"""{"success":true,"message":"Sale item cancelled successfully","data":{{saleJson}}}""");
    }
```

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected: `SalesControllerTests.cs(…): error CS1061: 'SalesController' does not contain a definition for 'CancelSaleItem' …`.

- [ ] **Step 3: Add the functional helper**

In `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiHttpExtensions.cs`, replace ticket 07's helper body end

```csharp
        using var response = await client.PatchAsync($"/api/sales/{saleId}/cancel", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await response.ReadSaleAsync();
    }
```

with

```csharp
        using var response = await client.PatchAsync($"/api/sales/{saleId}/cancel", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await response.ReadSaleAsync();
    }

    /// <summary>
    /// Cancels one line with <c>PATCH /api/sales/{id}/items/{itemId}/cancel</c>, for tests whose subject is a later step,
    /// and fails the test if that doesn't return 200.
    /// </summary>
    /// <param name="client">A client that holds a token.</param>
    /// <param name="saleId">The id of the sale.</param>
    /// <param name="itemId">The id of the line to cancel.</param>
    /// <returns>The sale after the change, as the response returned it.</returns>
    public static async Task<SaleResponseBody> CancelSaleItemAsync(this HttpClient client, Guid saleId, Guid itemId)
    {
        using var response = await client.PatchAsync($"/api/sales/{saleId}/items/{itemId}/cancel", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await response.ReadSaleAsync();
    }
```

- [ ] **Step 4: Write the failing functional tests**

Create `tests/Ambev.DeveloperEvaluation.Functional/Sales/CancelSaleItemTests.cs`:

```csharp
using System.Net;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

/// <summary>
/// Contains functional tests for cancelling one line with <c>PATCH /api/sales/{id}/items/{itemId}/cancel</c>.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class CancelSaleItemTests
{
    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="CancelSaleItemTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public CancelSaleItemTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// Tests the response for one line of two: 200, the documented envelope, the line cancelled, the total over the
    /// other line, and the sale still open with <c>updatedAt</c> set.
    /// </summary>
    [Fact(DisplayName = "Given a sale with two active items When cancelling one Then returns 200 with that item cancelled, the total dropped and the sale open")]
    public async Task Given_SaleWithTwoActiveItems_When_CancellingOne_Then_Returns200WithTotalDroppedAndSaleOpen()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var created = await client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid(
            SaleRequestBodyTestData.GenerateItem(4, 4.50m),
            SaleRequestBodyTestData.GenerateItem(12, 3.10m)));
        var cancelledItem = created.Items[0];
        var activeItem = created.Items[1];

        // When
        using var response = await client.PatchAsync($"/api/sales/{created.Id}/items/{cancelledItem.Id}/cancel", null);

        // Then (16.20 leaves; 12 × 3.10 at 20% = 29.76 stays)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await SaleJson.ShouldBeSaleEnvelopeAsync(response, "Sale item cancelled successfully");
        var sale = await response.ReadSaleAsync();
        sale.IsCancelled.Should().BeFalse();
        sale.TotalAmount.Should().Be(29.76m);
        sale.UpdatedAt.Should().BeAfter(created.CreatedAt);
        sale.Items.Should().BeEquivalentTo(new[] { cancelledItem with { IsCancelled = true }, activeItem });
    }

    /// <summary>
    /// Tests rule R6 over HTTP: cancelling the last active line cancels the sale with total 0, and a read shows the same.
    /// </summary>
    [Fact(DisplayName = "Given a sale with one active item left When cancelling it Then returns 200 with the sale cancelled and total 0, and a read shows the same")]
    public async Task Given_SaleWithOneActiveItemLeft_When_CancellingIt_Then_Returns200WithSaleCancelledAndZeroTotal()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var created = await client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid(
            SaleRequestBodyTestData.GenerateItem(4, 4.50m),
            SaleRequestBodyTestData.GenerateItem(12, 3.10m)));
        await client.CancelSaleItemAsync(created.Id, created.Items[0].Id);

        // When
        using var response = await client.PatchAsync($"/api/sales/{created.Id}/items/{created.Items[1].Id}/cancel", null);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var sale = await response.ReadSaleAsync();
        sale.IsCancelled.Should().BeTrue();
        sale.TotalAmount.Should().Be(0m);
        sale.Items.Should().OnlyContain(item => item.IsCancelled);

        using var read = await client.GetAsync($"/api/sales/{created.Id}");
        read.StatusCode.Should().Be(HttpStatusCode.OK);
        var saved = await read.ReadSaleAsync();
        saved.IsCancelled.Should().BeTrue();
        saved.TotalAmount.Should().Be(0m);
    }

    /// <summary>
    /// Tests rule R9 over HTTP: cancelling the same line again is a 409 with the documented body.
    /// </summary>
    [Fact(DisplayName = "Given a cancelled item When cancelling it again Then returns 409 BusinessRuleViolation")]
    public async Task Given_CancelledItem_When_CancellingAgain_Then_Returns409BusinessRuleViolation()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var created = await client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid(
            SaleRequestBodyTestData.GenerateItem(4, 4.50m),
            SaleRequestBodyTestData.GenerateItem(12, 3.10m)));
        var item = created.Items[0];
        await client.CancelSaleItemAsync(created.Id, item.Id);

        // When
        using var response = await client.PatchAsync($"/api/sales/{created.Id}/items/{item.Id}/cancel", null);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"BusinessRuleViolation","error":"Business rule violation","detail":"Item {{item.Id}} of sale {{created.SaleNumber}} is already cancelled"}""");
    }

    /// <summary>
    /// Tests rule R9 over HTTP: an item that isn't in the sale is a 404 with the documented body.
    /// </summary>
    [Fact(DisplayName = "Given an item id that isn't in the sale When cancelling it Then returns 404 ResourceNotFound")]
    public async Task Given_ItemIdNotInSale_When_CancellingIt_Then_Returns404ResourceNotFound()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var created = await client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid());
        var itemId = Guid.NewGuid();

        // When
        using var response = await client.PatchAsync($"/api/sales/{created.Id}/items/{itemId}/cancel", null);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"ResourceNotFound","error":"Resource not found","detail":"The item with ID {{itemId}} does not exist in the sale with ID {{created.Id}}"}""");
    }

    /// <summary>
    /// Tests that an unknown sale is a 404 with the documented body.
    /// </summary>
    [Fact(DisplayName = "Given an unknown sale id When cancelling an item Then returns 404 ResourceNotFound")]
    public async Task Given_UnknownSaleId_When_CancellingItem_Then_Returns404ResourceNotFound()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var saleId = Guid.NewGuid();

        // When
        using var response = await client.PatchAsync($"/api/sales/{saleId}/items/{Guid.NewGuid()}/cancel", null);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"ResourceNotFound","error":"Resource not found","detail":"The sale with ID {{saleId}} does not exist"}""");
    }

    /// <summary>
    /// Tests rule R7 over HTTP: an item of a cancelled sale can't be cancelled, a 409 with the documented body.
    /// </summary>
    [Fact(DisplayName = "Given a cancelled sale When cancelling one of its items Then returns 409 BusinessRuleViolation")]
    public async Task Given_CancelledSale_When_CancellingItem_Then_Returns409BusinessRuleViolation()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var created = await client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid());
        await client.CancelSaleAsync(created.Id);

        // When
        using var response = await client.PatchAsync($"/api/sales/{created.Id}/items/{created.Items[0].Id}/cancel", null);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"BusinessRuleViolation","error":"Business rule violation","detail":"Sale {{created.SaleNumber}} is cancelled and cannot be modified"}""");
    }
}
```

- [ ] **Step 5: Watch the functional tests fail (RED)**

The Unit project still doesn't compile (Step 2), so build only the Functional project:

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Functional 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~CancelSaleItemTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-110
```

Expected: the build succeeds; `Failed:     6, Passed:     0`. With no route, every PATCH gets an empty 404. The 200 and 409 status checks fail, the two 404 tests fail on the body, and two tests fail inside `CancelSaleItemAsync`.

- [ ] **Step 6: Add the action**

In `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
```

with

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;
```

Then replace

```csharp
        return Ok(_mapper.Map<SaleResponse>(result), "Sale cancelled successfully");
    }
```

with

```csharp
        return Ok(_mapper.Map<SaleResponse>(result), "Sale cancelled successfully");
    }

    /// <summary>
    /// Cancels one line of a sale. It drops out of the total; cancelling the last active line also cancels the sale.
    /// </summary>
    /// <param name="id">The id of the sale.</param>
    /// <param name="itemId">The id of the line.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 with the sale after the change.</returns>
    [HttpPatch("{id}/items/{itemId}/cancel")]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelSaleItem([FromRoute] Guid id, [FromRoute] Guid itemId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelSaleItemCommand(id, itemId), cancellationToken);

        return Ok(_mapper.Map<SaleResponse>(result), "Sale item cancelled successfully");
    }
```

- [ ] **Step 7: Verify (GREEN)**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected: `0 Error(s)`; Unit U0 + 19; Functional F0 + 6.

- [ ] **Step 8: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesControllerTests.cs tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiHttpExtensions.cs tests/Ambev.DeveloperEvaluation.Functional/Sales/CancelSaleItemTests.cs
git commit -m "feat(sales): cancel a sale item over HTTP" -m "PATCH /api/sales/{id}/items/{itemId}/cancel returns 200 with the sale (Sale item cancelled successfully), 404 for an unknown sale or item, and 409 BusinessRuleViolation for an item that is already cancelled or a cancelled sale. The functional tests cover one item of two, the last active item cancelling the sale, a repeat, an unknown item, an unknown sale and a cancelled sale."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `4 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 7: Add the cancel-item requests to the `.http` file and replay them against `docker compose up`

**Files:**
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http`

- [ ] **Step 1: Look at the file (RED)**

```bash
grep -c '/items/' src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
```

Expected: `0`.

- [ ] **Step 2: Append the requests after ticket 07's**

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

### Create a sale with two items, to cancel them one by one (201; 5 items get 10%, 2 items get none)
# @name createTwoItemSale
POST {{baseUrl}}/api/sales
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "saleNumber": "S-{{$randomInt 100000 999999}}",
  "saleDate": "2026-09-24T16:00:00Z",
  "customerId": "3f2b8c1e-1111-4a5b-9c2d-000000000001",
  "customerName": "Maria Silva",
  "branchId": "3f2b8c1e-2222-4a5b-9c2d-000000000002",
  "branchName": "Filial Centro",
  "items": [
    { "productId": "3f2b8c1e-3333-4a5b-9c2d-000000000003", "productName": "Cerveja 350ml", "quantity": 5, "unitPrice": 4.50 },
    { "productId": "3f2b8c1e-4444-4a5b-9c2d-000000000004", "productName": "Refrigerante 2L", "quantity": 2, "unitPrice": 8.00 }
  ]
}

###
# The sale created above and its two items, in the order they were sent.
@twoItemSaleId = {{createTwoItemSale.response.body.$.data.id}}
@firstItemId = {{createTwoItemSale.response.body.$.data.items[0].id}}
@secondItemId = {{createTwoItemSale.response.body.$.data.items[1].id}}

### Cancel the first item: it leaves the total and the sale stays open (200; totalAmount 16.00)
PATCH {{baseUrl}}/api/sales/{{twoItemSaleId}}/items/{{firstItemId}}/cancel
Authorization: Bearer {{token}}

### Cancel it again: the item is already cancelled (409 BusinessRuleViolation)
PATCH {{baseUrl}}/api/sales/{{twoItemSaleId}}/items/{{firstItemId}}/cancel
Authorization: Bearer {{token}}

### Cancel the last active item: the sale is cancelled too (200; isCancelled true, totalAmount 0)
PATCH {{baseUrl}}/api/sales/{{twoItemSaleId}}/items/{{secondItemId}}/cancel
Authorization: Bearer {{token}}
```

- [ ] **Step 3: Check the file (GREEN)**

```bash
git diff --stat | cat
grep -nE '^###|^@|^(POST|GET|PATCH) ' src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http | tail -16
```

Expected: `1 file changed, 37 insertions(+)`. The listing ends with the new create, the three `@…Id` captures and the three `PATCH …/items/…/cancel` lines, after ticket 07's two `PATCH …/cancel` lines.

- [ ] **Step 4: Start the stack from nothing**

```bash
docker compose down -v --remove-orphans
docker compose up --build --wait --wait-timeout 300; echo "exit=$?"
```

Expected: `exit=0`. If a port is already allocated, stop and ask the user.

- [ ] **Step 5: Replay the requests with curl**

Keep this in one Bash call.

```bash
BASE=http://localhost:8080
curl -s -o /dev/null -w "sign-up %{http_code}\n" -X POST $BASE/api/users -H 'Content-Type: application/json' --data-raw '{"username":"admin","password":"Admin@123","phone":"+5511999999999","email":"admin@developerstore.com","status":"Active","role":"Admin"}'
curl -s -o /tmp/ticket08-checks/login.json -X POST $BASE/api/auth -H 'Content-Type: application/json' --data-raw '{"email":"admin@developerstore.com","password":"Admin@123"}'
TOKEN=$(python3 -c "import json, sys; print(json.load(open(sys.argv[1]))['data']['token'])" /tmp/ticket08-checks/login.json)
curl -s -o /tmp/ticket08-checks/create.json -w "create %{http_code}\n" -X POST $BASE/api/sales -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' --data-raw "{\"saleNumber\":\"S-$(( RANDOM % 900000 + 100000 ))\",\"saleDate\":\"2026-09-24T16:00:00Z\",\"customerId\":\"3f2b8c1e-1111-4a5b-9c2d-000000000001\",\"customerName\":\"Maria Silva\",\"branchId\":\"3f2b8c1e-2222-4a5b-9c2d-000000000002\",\"branchName\":\"Filial Centro\",\"items\":[{\"productId\":\"3f2b8c1e-3333-4a5b-9c2d-000000000003\",\"productName\":\"Cerveja 350ml\",\"quantity\":5,\"unitPrice\":4.50},{\"productId\":\"3f2b8c1e-4444-4a5b-9c2d-000000000004\",\"productName\":\"Refrigerante 2L\",\"quantity\":2,\"unitPrice\":8.00}]}"
SALE_ID=$(python3 -c "import json, sys; print(json.load(open(sys.argv[1]))['data']['id'])" /tmp/ticket08-checks/create.json)
FIRST_ID=$(python3 -c "import json, sys; print(json.load(open(sys.argv[1]))['data']['items'][0]['id'])" /tmp/ticket08-checks/create.json)
SECOND_ID=$(python3 -c "import json, sys; print(json.load(open(sys.argv[1]))['data']['items'][1]['id'])" /tmp/ticket08-checks/create.json)
SUMMARY="import json, sys; d = json.load(open(sys.argv[1]))['data']; print('isCancelled', d['isCancelled'], 'total', d['totalAmount'], 'cancelled items', sum(i['isCancelled'] for i in d['items']))"
curl -s -o /tmp/ticket08-checks/first.json -w "cancel first %{http_code} " -X PATCH "$BASE/api/sales/$SALE_ID/items/$FIRST_ID/cancel" -H "Authorization: Bearer $TOKEN"
python3 -c "$SUMMARY" /tmp/ticket08-checks/first.json
curl -s -w " %{http_code}\n" -X PATCH "$BASE/api/sales/$SALE_ID/items/$FIRST_ID/cancel" -H "Authorization: Bearer $TOKEN"
curl -s -w " %{http_code}\n" -X PATCH "$BASE/api/sales/$SALE_ID/items/3f2b8c1e-9999-4a5b-9c2d-000000000099/cancel" -H "Authorization: Bearer $TOKEN"
curl -s -o /tmp/ticket08-checks/last.json -w "cancel last %{http_code} " -X PATCH "$BASE/api/sales/$SALE_ID/items/$SECOND_ID/cancel" -H "Authorization: Bearer $TOKEN"
python3 -c "$SUMMARY" /tmp/ticket08-checks/last.json
curl -s -o /tmp/ticket08-checks/get.json -w "get %{http_code} " "$BASE/api/sales/$SALE_ID" -H "Authorization: Bearer $TOKEN"
python3 -c "$SUMMARY" /tmp/ticket08-checks/get.json
```

Expected (the ids and the sale number vary; totals may print as `16.0`, `0` or `0.0`):

```
sign-up 201
create 201
cancel first 200 isCancelled False total 16.0 cancelled items 1
{"type":"BusinessRuleViolation","error":"Business rule violation","detail":"Item <FIRST_ID> of sale S-… is already cancelled"} 409
{"type":"ResourceNotFound","error":"Resource not found","detail":"The item with ID 3f2b8c1e-9999-4a5b-9c2d-000000000099 does not exist in the sale with ID <SALE_ID>"} 404
cancel last 200 isCancelled True total 0 cancelled items 2
get 200 isCancelled True total 0.0 cancelled items 2
```

- [ ] **Step 6: Check the event log and its order**

```bash
docker compose logs --no-log-prefix ambev.developerevaluation.webapi 2>&1 | grep -oE 'Sale event [A-Za-z]+ published'
```

Expected:

```
Sale event SaleCreatedEvent published
Sale event ItemCancelledEvent published
Sale event ItemCancelledEvent published
Sale event SaleCancelledEvent published
```

The rejected requests published nothing.

- [ ] **Step 7: Take the stack down**

```bash
docker compose down -v
docker compose ps -a
```

Expected: `ps -a` lists nothing.

- [ ] **Step 8: Commit**

```bash
git add src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
git commit -m "docs(http): cancel sale items" -m "The .http file creates a sale with two items, cancels the first, repeats it to show the 409, and cancels the last active item, which cancels the sale."
git show --stat --format= HEAD | tail -1
```

Expected: `1 file changed, 37 insertions(+)`. No slopwatch run, because no C# changed.

---

### Task 8: Verify everything, review, then open a pull request into `develop`

**Skills:** superpowers:verification-before-completion, `dotnet-slopwatch`, `test-anti-patterns` (report only), `clean-code` and superpowers:requesting-code-review (optional), superpowers:finishing-a-development-branch.

**Files:** none, unless Step 2 finds a real problem.

- [ ] **Step 1: Re-run every check fresh**

```bash
git status --short
dotnet build Ambev.DeveloperEvaluation.sln --no-incremental 2>&1 | grep -E 'warning CS|Warning\(s\)|Error\(s\)|Build succeeded' | sort -u
dotnet test Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-110
cat /tmp/ticket08-checks/baseline.txt
slopwatch analyze --fail-on warning 2>&1 | tail -1
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at ' | tail -1
```

Expected:
- only `?? docs/superpowers/tickets/` and other untracked plans
- `0 Error(s)`, `2 Warning(s)`, `Build succeeded.`, no `warning CS`
- Unit U0 + 19, Integration I0 + 2, Functional F0 + 6, compared against the baseline file
- `Scan complete: 0 issue(s) found`
- `No changes have been made to the model since the last migration.`

| Ticket criterion | Evidence |
|---|---|
| `CancelItem` marks the item cancelled, recomputes the total, sets `UpdatedAt` and records `ItemCancelledEvent` | `SaleTests` (Task 2) |
| Last active item cancels the sale, total 0, `SaleCancelledEvent` after `ItemCancelledEvent` (R6) | `SaleTests`, `CancelSaleItemHandlerTests`, `CancelSaleItemTests`; Task 7 Step 6 |
| `DomainException` for a cancelled sale (R7) and a cancelled item (R9) | `SaleTests` (Task 2) |
| Validator (non-empty ids); `KeyNotFoundException` for an unknown sale or item; save, then publish in order | `CancelSaleItemCommandValidatorTests`, `CancelSaleItemHandlerTests` (Task 5) |
| Event-logging handler logs `ItemCancelledEvent` | `SaleEventLogHandlerTests` (Task 3); Task 7 Step 6 |
| Endpoint returns 200, 404 (sale or item) and 409 (cancelled item or sale) | `SalesControllerTests`, `CancelSaleItemTests` (Task 6); Task 7 Step 5 |
| Every mutation writes the `Sales` row, so `xmin` covers item-only changes | `UpdatedAt` test in `SaleTests`; concurrency test and mutation check (Task 4) |
| Two contexts cancel different items: the second save raises `DbUpdateConcurrencyException` (mapped to 409 by ticket 03) | `SaleRepositoryTests` (Task 4) |
| Functional tests: one of two, the last one, a repeat (409), not in the sale (404), a cancelled sale (409) | `CancelSaleItemTests` |
| `.http` gains the cancel-item request | Task 7 |
| `feature/cancel-sale-item`, a pull request into `develop`, Conventional Commits, no attribution | Steps 4–6 below |

- [ ] **Step 2: Audit the tests (skill `test-anti-patterns`, report only)**

Run it on `CancelSaleItemCommandValidatorTests`, `CancelSaleItemHandlerTests` and `CancelSaleItemTests`, and on the new tests in `SaleTests`, `SaleEventLogHandlerTests`, `SalesControllerTests` and `SaleRepositoryTests`, plus `ApiHttpExtensions.CancelSaleItemAsync`.

These findings are deliberate; report them and leave them:
- `Received.InOrder` in `CancelSaleItemHandlerTests`: the project note says call order is the behaviour.
- The `DateTime.UtcNow` bracketing in `SaleTests`: there's no clock abstraction (ticket 05, Decision 6).
- The assertion inside `CancelSaleItemAsync`, which guards a setup step.
- The log-handler test repeats ticket 07's shape for another event type.
- Several assertions in the concurrency test: they check a single outcome, the stored state after the conflict.

Fix a real finding in a separate `test(sales): …` commit, then re-run Step 1.

- [ ] **Step 3: Optional code review**

Run superpowers:requesting-code-review on `develop..feature/cancel-sale-item`, with `clean-code`, `dotnet-best-practices` and `efcore-patterns` as lenses. Findings that belong to tickets 09–13 go into the report, not this branch.

- [ ] **Step 4: Check the commit messages**

```bash
git log --format=%s develop..feature/cancel-sale-item
git log --format=%B develop..feature/cancel-sale-item | grep -n -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
```

Expected (plus any `test(sales):` commit from Step 2):

```
docs(http): cancel sale items
feat(sales): cancel a sale item over HTTP
feat(sales): cancel a sale item and publish its events after the save
test(sales): pin item cancellation and the xmin concurrency check
feat(sales): log ItemCancelledEvent
feat(sales): cancel an item in the domain
no attribution lines
```

- [ ] **Step 5: Push the branch and open a pull request into `develop`**

Write the pull request body with the Write tool to `/tmp/ticket08-checks/pr-body.md`. It holds the Goal in two or three sentences, the evidence table from Step 1 with this run's results, and what Steps 2 and 3 reported. No attribution lines. Then:

```bash
git push -u origin feature/cancel-sale-item
gh pr create --base develop --head feature/cancel-sale-item --title "Ticket 08: cancel an item" --body-file /tmp/ticket08-checks/pr-body.md
```

Expected: the push creates `origin/feature/cancel-sale-item`, and `gh pr create` prints the pull request URL. If `gh` fails, check `gh auth status`, then stop and tell the user that the branch is pushed so they can open the pull request on GitHub.

- [ ] **Step 6: Verify the pull request**

```bash
gh pr view feature/cancel-sale-item --json state,baseRefName,commits --jq '"\(.state) \(.baseRefName) \(.commits | length) commits"'
gh pr view feature/cancel-sale-item --json title,body --jq '.title + "\n" + .body' | grep -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
git status -sb | head -1
```

Expected: `OPEN develop 6 commits` (7 with a Step 2 fix); `no attribution lines`; `## feature/cancel-sale-item...origin/feature/cancel-sale-item` with no ahead or behind. Don't merge it, and keep the branch.

- [ ] **Step 7: Clean up**

```bash
docker compose ps -a
tasklist | grep -i 'Ambev.DeveloperEvaluation.WebApi' || echo "no API process left"
rm -rf /tmp/ticket08-checks
```

Expected: nothing listed, then `no API process left`.

- [ ] **Step 8: Report to the user**

1. The results of Step 1 and every output that differed from this plan, with what systematic-debugging found (or say that none differed). Include the pull request URL.
2. Ask the user to run the new `.http` requests once from their editor.
3. A note for ticket 09: `Update` cancels lines that are missing from the payload. It should call `SaleItem.Cancel()` and record `ItemCancelledEvent` the way `CancelItem` does, so extract a private `CancelLine(SaleItem item, DateTime now)` from `CancelItem` at that point. `Update` must also call `EnsureNotCancelled()` and `RecalculateTotal()`.
