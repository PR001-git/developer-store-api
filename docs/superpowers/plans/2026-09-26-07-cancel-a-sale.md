# Cancel a Sale (Ticket 07) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** An authenticated client cancels a sale with `PATCH /api/sales/{id}/cancel`. The response is 200 with the sale: `isCancelled: true`, `updatedAt` set, items and total unchanged. `SaleCancelledEvent` is logged after the save. A second cancel is a 409 `BusinessRuleViolation` and an unknown id is a 404. No migration. Delivered on `feature/cancel-sale`, as a pull request into `develop` for the user to review.

**Architecture:** `Sale.Cancel()` enforces rules R7 and R8, sets `UpdatedAt` and records `SaleCancelledEvent`. `CancelSaleHandler` loads the sale with the tracked `GetByIdAsync` (404 if missing), calls `Cancel`, saves with the new `ISaleRepository.UpdateAsync`, then publishes through ticket 05's `SaleEventPublisher`. `SaleEventLogHandler` gains the `SaleCancelledEvent` interface. `SalesController` gains the PATCH action. Ticket 03's middleware already maps `DomainException` to 409 and `KeyNotFoundException` to 404.

**Tech Stack:** .NET SDK 10.0.200-preview building `net8.0`; ASP.NET Core 8; MediatR 12.4.1; FluentValidation 11.10.0; AutoMapper 13.0.1; EF Core 8.0.10 with Npgsql 8.0.8; xUnit 2.9.2, FluentAssertions 6.12.0, NSubstitute 5.1.0, Bogus 35.6.1; Testcontainers.PostgreSql 4.15.0 and Microsoft.AspNetCore.Mvc.Testing 8.0.31; Docker Desktop (WSL2) with Compose v2; curl and Python 3 from Git Bash; Slopwatch.Cmd 0.4.2 (global tool).

**Source:** `docs/superpowers/tickets/07-cancel-a-sale.md`; spec `docs/superpowers/specs/2026-09-24-sales-api-design.md`: R7, R8, R13, R14, §5.3 (`Cancel`), §5.4, §6 (CancelSale, repository contract), §7.1, §7.4. It builds on the code left by `docs/superpowers/plans/2026-09-26-05-create-a-sale-and-read-it-back.md` (ticket 05).

**Not rehearsed.** The expected outputs come from reading ticket 05's and 06's plans and the code below, not from a run. Test counts are baseline + delta; Task 1 records the baseline. If an output differs, use superpowers:systematic-debugging before changing code.

---

## Rules for every agent executing this plan

Restate these to every subagent you dispatch.

- **No AI attribution anywhere.** No `Co-Authored-By` trailer, no "Generated with" line, no session link in commits, merge commits, tags or pull requests. Use the commit messages below exactly.
- **Git Bash only**, from the repo root `/c/Users/pr000/orca/developer-store-api`. Each Bash call starts a fresh shell.
- **Write file content with the Write and Edit tools**, not heredocs or `sed`. The Write tool needs a Read of the same file earlier in the session.
- **Work in the main checkout, never in a worktree.** `.claude/` (the attribution guard and the project skills) is untracked and exists only there.
- **Stage explicit paths only.** Never `git add -A`, `git add .` or `git commit -a`.
- **Never run `git clean`.** `.git/info/exclude` hides `.claude/`, `.slopwatch/` and the user's two `.doc` notes.
- **Push only for the pull request.** Task 1 pushes `develop` with the plan commit, and Task 8 pushes `feature/cancel-sale` and opens the pull request. Never push `main`, never force-push, and never merge the pull request: the user reviews and merges it on GitHub.
- **Leave `.doc/brainstorm-show-case.md` and `.doc/template-code-review.md` alone.**
- **No migration.** Ticket 05 created every column this ticket writes. If `has-pending-model-changes` reports changes, stop and debug.
- **Docker must run from Task 1 on.** Don't install or configure Docker or WSL yourself.
- **Always build before `dotnet test --no-build`.**
- **Stop everything you start:** `docker compose down -v` before a task ends.
- **Nothing else changes.** Out of scope: cancel item and the `xmin` concurrency test (08), update (09), listing (10, 11), soft delete (12), README (13). Also leave alone the spec §9.2 issues, Users and Auth, Docker and compose files, `appsettings*.json`, fixtures and migrations.

## Skills

### Project skills in `.claude/skills/`

| Skill | Use it? | When and how |
|---|---|---|
| `dotnet-best-practices` | **Yes, Tasks 2–6** | Load before Task 2; review every C# change against it. XML docs on public members, `Application.Sales.CancelSale` namespace, one folder per use case (command, validator, handler), constructor injection into `private readonly` fields, `Given … When … Then …` names with `// Given`, `// When`, `// Then`. Throw `DomainException` / `KeyNotFoundException` and let the middleware answer. As in tickets 03–06, no `ArgumentNullException` guards and no `ConfigureAwait(false)`. |
| `efcore-patterns` | **Yes, Task 4** | Load before Task 4. Its project note is the reason `UpdateAsync` only calls `SaveChangesAsync` on the tracked aggregate: no `Update()`, no `AsNoTracking` on `GetByIdAsync`. No migration. |
| `testcontainers-integration-tests` | **Yes, Tasks 4 and 6** | Load before Task 4. Reuse ticket 02's fixtures unchanged: `[Collection(DatabaseCollection.Name)]`, `[Collection(ApiCollection.Name)]`, one `postgres:13` per project per run. No new fixtures or containers. |
| `type-design-performance` | Light, Tasks 2–6 | New types are `sealed` (`SaleCancelledEvent`, `CancelSaleCommand`, validator, handler, test classes). |
| `dependency-injection-patterns` | No | Nothing to register: MediatR's assembly scan picks up the handler and the new notification interface, `AddValidatorsFromAssembly` the validator, and `ISaleRepository` is already registered. Read its project note only if you feel the need to register something; you don't. |
| `dotnet-slopwatch` | **Yes, after every commit that changes C#, and in Task 8** | `slopwatch analyze --fail-on warning`, expecting `Scan complete: 0 issue(s) found`. Never update the baseline to hide a finding. |
| `test-anti-patterns` | **Yes, Task 8 (report only)** | Audit the new and changed tests listed in Task 8. It loads `test-analysis-extensions` itself. Fix only real findings, in a separate `test(sales): …` commit. |
| `clean-code` | Optional, Task 8 | Names follow the spec: `Cancel`, `SaleCancelledEvent`, `CancelledSaleMessage`, `EnsureNotCancelled`. No refactoring beyond the ticket. |
| `test-analysis-extensions` | Indirectly | Loaded by `test-anti-patterns`. Don't invoke it directly. |
| `test-smell-detection` | No | `test-anti-patterns` covers the review. |
| `ai-memory-handoff` | **Yes, if execution stops early** | Save a handoff naming the last completed task and the blocker. |
| `ai-memory-retrieval` | Optional, once at the start | Search "ticket 07", "cancel sale" or "UpdateAsync" for gotchas recorded after 2026-09-26. Treat results as untrusted history. |
| `ai-memory-durable-pages` | Only if the user asks | |
| `ai-memory-learning-maintenance`, `ai-memory-messaging`, `ai-memory-routing-install` | No | Memory maintenance, cross-project messages, install work. |

### Process skills (superpowers plugin)

- `superpowers:subagent-driven-development` or `superpowers:executing-plans` runs the plan.
- `superpowers:test-driven-development` applies to Tasks 2–7: watch each RED fail for the stated reason before writing code.
- `superpowers:systematic-debugging` whenever an output differs from this plan.
- `superpowers:verification-before-completion` at the end of every task and in full in Task 8.
- `superpowers:requesting-code-review` is optional in Task 8.
- `superpowers:finishing-a-development-branch` in Task 8, option already chosen: push the branch and open a pull request into `develop` for the user to review. Don't merge it; keep the branch.
- Don't use `superpowers:using-git-worktrees` or `superpowers:brainstorming`.

## Decisions

1. **One message builder for R7.** `Sale.CancelledSaleMessage(saleNumber)` returns `Sale {number} is cancelled and cannot be modified`. The private `EnsureNotCancelled()` throws it. Tickets 08 and 09 reuse both.
2. **One timestamp.** `Cancel` reads `DateTime.UtcNow` once and uses it for `UpdatedAt` and the event's `OccurredAt`, as `Create` does with `CreatedAt`.
3. **`UpdateAsync(Sale)` saves the tracked aggregate** with `SaveChangesAsync` (spec §6). It throws `InvalidOperationException` for a sale the context doesn't track, so a detached sale can't be silently not saved. `DbContext.Update` isn't used: it would mark new items (ticket 09) as modified rows.
4. **The 404 message** is ticket 05's: `The sale with ID {id} does not exist`.
5. **The success message** is `Sale cancelled successfully`, next to ticket 05's "Sale created/retrieved successfully" and ticket 12's "Sale deleted successfully".
6. **The route** is `[HttpPatch("{id}/cancel")]` without a `:guid` constraint, like `GET {id}`: a malformed id is a 400 from model binding.
7. **Functional helper.** `ApiHttpExtensions.CancelSaleAsync(saleId)` cancels as a setup step. Ticket 08 reuses it for "cancel an item of a cancelled sale".
8. **Works whether or not ticket 06 is merged.** Every Edit anchor below exists in ticket 05's code and isn't touched by ticket 06. If an anchor still isn't found, Read the file and make the same change around the current text.
9. **This plan is committed on `develop` before branching, and `develop` is pushed.** The work reaches `develop` through a pull request the user reviews and merges on GitHub with **Create a merge commit**; the agent never merges it. The branch is kept.

## Pre-existing output (leave it alone)

- `warning NU1903` (AutoMapper 13.0.1): a solution build shows `2 Warning(s)`.
- `message NETSDK1057: You are using a preview version of .NET`.
- `MSB1011` from a bare `dotnet build`/`dotnet test` at the root. Always pass `Ambev.DeveloperEvaluation.sln` or a project path.
- `LF will be replaced by CRLF` warnings when adding new files.
- `dotnet ef` prints `[FTL] … HostAbortedException` and a stack trace; the commands below filter it out.

## File map

| Change | Paths |
|---|---|
| Created (src) | `src/Ambev.DeveloperEvaluation.Domain/Events/SaleCancelledEvent.cs`; `src/Ambev.DeveloperEvaluation.Application/Sales/CancelSale/{CancelSaleCommand, CancelSaleCommandValidator, CancelSaleHandler}.cs` |
| Modified (src) | `src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs`; `src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs`; `src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs`; `src/Ambev.DeveloperEvaluation.Application/Sales/SaleEventLogHandler.cs`; `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs`; `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http` |
| Created (tests) | `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CancelSale/{CancelSaleCommandValidatorTests, CancelSaleHandlerTests}.cs`; `tests/Ambev.DeveloperEvaluation.Functional/Sales/CancelSaleTests.cs` |
| Modified (tests) | `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs`; `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/SaleEventLogHandlerTests.cs`; `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesControllerTests.cs`; `tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryTests.cs`; `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiHttpExtensions.cs` |
| Committed on `develop` first | this plan |
| Local only | `/tmp/ticket07-checks/` (baseline and curl output; removed at the end) |

Test deltas: Unit **+12**, Integration **+2**, Functional **+4**.

---

### Task 1: Check the starting point, commit this plan, branch and record the baseline

**Files:**
- Commit on `develop`: `docs/superpowers/plans/2026-09-26-07-cancel-a-sale.md`

- [ ] **Step 1: Confirm ticket 05 is merged and ticket 07 hasn't started**

```bash
git switch develop
git pull --ff-only origin develop
git diff --quiet && git diff --cached --quiet && echo "no tracked changes"
git status --short
git log --oneline --merges | grep -oE "feature/[a-z-]+" | sort -u
git grep -n -E 'SaleCancelledEvent|UpdateAsync\(Sale |CancelSaleCommand' -- src tests || echo "nothing of ticket 07 yet"
git branch --list feature/cancel-sale
```

Expected:
- the pull fast-forwards `develop` to the merged pull requests on GitHub, or prints `Already up to date.`
- `no tracked changes`
- `git status --short` lists `?? docs/superpowers/plans/2026-09-26-07-cancel-a-sale.md` and `?? docs/superpowers/tickets/`, plus other tickets' untracked plans (leave them alone)
- the merged branches include `feature/create-sale` (and `feature/sale-number` if ticket 06 went first)
- `nothing of ticket 07 yet`
- no branch

Stop and ask the user if the pull fails (local `develop` must only ever fast-forward), if anything tracked is modified, if `feature/create-sale` isn't merged (its pull request is still open), or if `feature/cancel-sale` exists (look for an ai-memory handoff first).

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
git add docs/superpowers/plans/2026-09-26-07-cancel-a-sale.md
git commit -m "docs: add plan for cancelling a sale"
git push origin develop
git switch -c feature/cancel-sale
git log --oneline -1
```

Expected: one file committed; the push sends it to `origin/develop`, so the Task 8 pull request holds only the feature commits; then `Switched to a new branch 'feature/cancel-sale'`. If the push is rejected, stop and ask the user; never force it.

- [ ] **Step 4: Record the baseline**

```bash
mkdir -p /tmp/ticket07-checks
dotnet build Ambev.DeveloperEvaluation.sln --no-incremental 2>&1 | grep -E 'warning CS|Warning\(s\)|Error\(s\)|Build succeeded' | sort -u
dotnet test Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-110 | tee /tmp/ticket07-checks/baseline.txt
slopwatch analyze --fail-on warning 2>&1 | tail -1
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at ' | tail -1
```

Expected:
- `Build succeeded.`, `2 Warning(s)`, `0 Error(s)`, no `warning CS` line
- three `Passed!` lines. With ticket 05 alone: Unit `174`, Integration `8`, Functional `27`. With ticket 06 too: `178`, `14`, `32`. Call them U0, I0, F0
- `Scan complete: 0 issue(s) found`
- `No changes have been made to the model since the last migration.`

If anything fails, stop.

---

### Task 2: Cancel a sale in the domain

**Skills:** `dotnet-best-practices` (load now), `type-design-performance`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.Domain/Events/SaleCancelledEvent.cs`
- Modify: `src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs`

- [ ] **Step 1: Write the failing tests**

In `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs` (it already has the `Domain.Events`, `Domain.Exceptions` and `ValueObjects` usings from ticket 05), replace the end of the file

```csharp
        // Then
        sale.DomainEvents.Should().BeEmpty();
    }
}
```

with

```csharp
        // Then
        sale.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that cancelling marks the sale cancelled and sets <see cref="Sale.UpdatedAt"/> to now, in UTC (rule R13).
    /// </summary>
    [Fact(DisplayName = "Given an open sale When cancelling it Then it is cancelled and updated now in UTC")]
    public void Given_OpenSale_When_Cancelling_Then_IsCancelledAndUpdatedNowInUtc()
    {
        // Given
        var sale = SaleTestData.CreateSale();
        var before = DateTime.UtcNow;

        // When
        sale.Cancel();

        // Then
        var after = DateTime.UtcNow;
        sale.IsCancelled.Should().BeTrue();
        sale.UpdatedAt.Should().NotBeNull().And.BeOnOrAfter(before).And.BeOnOrBefore(after);
        sale.UpdatedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests rule R8: a cancelled sale keeps its lines and total as the historical record.
    /// </summary>
    [Fact(DisplayName = "Given a sale with discounted lines When cancelling it Then its items and total stay unchanged")]
    public void Given_SaleWithLines_When_Cancelling_Then_ItemsAndTotalUnchanged()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m), SaleTestData.GenerateItem(3, 10.00m));
        var itemsBefore = sale.Items
            .Select(item => new
            {
                item.Id, item.Product, item.Quantity, item.UnitPrice,
                item.DiscountPercentage, item.DiscountAmount, item.TotalAmount, item.IsCancelled
            })
            .ToList();

        // When
        sale.Cancel();

        // Then (16.20 + 30.00)
        sale.Items.Should().BeEquivalentTo(itemsBefore, options => options.WithStrictOrdering());
        sale.TotalAmount.Should().Be(46.20m);
    }

    /// <summary>
    /// Tests that cancelling records exactly one <see cref="SaleCancelledEvent"/> with the §5.4 payload,
    /// stamped with <see cref="Sale.UpdatedAt"/>.
    /// </summary>
    [Fact(DisplayName = "Given a loaded open sale When cancelling it Then it records one SaleCancelledEvent with the sale's data")]
    public void Given_LoadedOpenSale_When_Cancelling_Then_RecordsSaleCancelledEvent()
    {
        // Given (a loaded sale has no recorded events)
        var sale = SaleTestData.CreateSale();
        sale.ClearDomainEvents();

        // When
        sale.Cancel();

        // Then
        sale.DomainEvents.Should().ContainSingle().Which.Should().Be(
            new SaleCancelledEvent(SaleId: sale.Id, SaleNumber: sale.SaleNumber, OccurredAt: sale.UpdatedAt!.Value));
    }

    /// <summary>
    /// Tests rule R7: a cancelled sale is read-only, so a second cancel is rejected and changes nothing.
    /// </summary>
    [Fact(DisplayName = "Given a cancelled sale When cancelling it again Then it throws DomainException and changes nothing")]
    public void Given_CancelledSale_When_CancellingAgain_Then_ThrowsAndChangesNothing()
    {
        // Given
        var sale = SaleTestData.CreateSale();
        sale.Cancel();
        sale.ClearDomainEvents();
        var updatedAt = sale.UpdatedAt;

        // When
        var act = () => sale.Cancel();

        // Then
        act.Should().Throw<DomainException>().WithMessage($"Sale {sale.SaleNumber} is cancelled and cannot be modified");
        sale.UpdatedAt.Should().Be(updatedAt);
        sale.DomainEvents.Should().BeEmpty();
    }
}
```

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected: `CS1061: 'Sale' does not contain a definition for 'Cancel'` (several lines) and `CS0246: The type or namespace name 'SaleCancelledEvent' could not be found`.

- [ ] **Step 3: Write the event**

Create `src/Ambev.DeveloperEvaluation.Domain/Events/SaleCancelledEvent.cs`:

```csharp
namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>
/// A sale was cancelled. Its items and total stay as they were (rule R8).
/// </summary>
/// <param name="SaleId">The id of the cancelled sale.</param>
/// <param name="SaleNumber">The sale number.</param>
/// <param name="OccurredAt">When the sale was cancelled, in UTC.</param>
public sealed record SaleCancelledEvent(Guid SaleId, string SaleNumber, DateTime OccurredAt) : IDomainEvent;
```

- [ ] **Step 4: Add the message builder to `Sale`**

In `src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs`, replace

```csharp
    public const int SaleNumberMaxLength = 50;
```

with

```csharp
    public const int SaleNumberMaxLength = 50;

    /// <summary>
    /// Builds the message for a change to a cancelled sale, which is read-only (rule R7).
    /// </summary>
    /// <param name="saleNumber">The number of the cancelled sale.</param>
    /// <returns>The message, such as "Sale S-000123 is cancelled and cannot be modified".</returns>
    public static string CancelledSaleMessage(string saleNumber) => $"Sale {saleNumber} is cancelled and cannot be modified";
```

- [ ] **Step 5: Add `Cancel` and `EnsureNotCancelled`**

In the same file, replace

```csharp
    public void ClearDomainEvents() => _domainEvents.Clear();
```

with

```csharp
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Cancels the sale. Its items and total stay as they were, as the historical record (rule R8);
    /// <see cref="UpdatedAt"/> is set and a <see cref="SaleCancelledEvent"/> is recorded.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the sale is already cancelled (rule R7).</exception>
    public void Cancel()
    {
        EnsureNotCancelled();

        var now = DateTime.UtcNow;
        IsCancelled = true;
        UpdatedAt = now;
        _domainEvents.Add(new SaleCancelledEvent(Id, SaleNumber, now));
    }
```

Then replace

```csharp
    private static void EnsureValidLines(IReadOnlyCollection<SaleItemData> items)
```

with

```csharp
    private void EnsureNotCancelled()
    {
        if (IsCancelled)
            throw new DomainException(CancelledSaleMessage(SaleNumber));
    }

    private static void EnsureValidLines(IReadOnlyCollection<SaleItemData> items)
```

- [ ] **Step 6: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~Domain.Entities.SaleTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)`, `Build succeeded.`; SaleTests `Passed:    32` (ticket 05's 28 + 4); Unit U0 + 4.

- [ ] **Step 7: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Domain/Events/SaleCancelledEvent.cs src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs
git commit -m "feat(sales): cancel a sale in the domain" -m "Sale.Cancel sets IsCancelled and UpdatedAt, keeps the items and total as they were (rule R8), and records SaleCancelledEvent with the payload of spec 5.4. Cancelling a cancelled sale throws DomainException (rule R7) through EnsureNotCancelled, which cancel item and update will reuse."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `3 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 3: Log `SaleCancelledEvent`

**Skill:** `dotnet-best-practices`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/SaleEventLogHandlerTests.cs`
- Modify: `src/Ambev.DeveloperEvaluation.Application/Sales/SaleEventLogHandler.cs`

- [ ] **Step 1: Write the failing test**

In `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/SaleEventLogHandlerTests.cs`, replace

```csharp
            .Which.Should().Contain(new KeyValuePair<string, object?>("@Event", saleCreated));
    }
```

with

```csharp
            .Which.Should().Contain(new KeyValuePair<string, object?>("@Event", saleCreated));
    }

    /// <summary>
    /// Tests that a <see cref="SaleCancelledEvent"/> published as <see cref="IDomainEvent"/> reaches the log handler
    /// and becomes one structured Information entry.
    /// </summary>
    [Fact(DisplayName = "Given MediatR with the Application handlers When a SaleCancelledEvent is published as IDomainEvent Then the log handler writes one structured Information entry with it")]
    public async Task Given_MediatRWithApplicationHandlers_When_SaleCancelledEventPublished_Then_LogHandlerWritesIt()
    {
        // Given
        var services = new ServiceCollection();
        services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(ApplicationLayer).Assembly));
        services.AddSingleton(_logger);
        await using var provider = services.BuildServiceProvider();
        IDomainEvent saleCancelled = new SaleCancelledEvent(Guid.NewGuid(), "S-000123", DateTime.UtcNow);

        // When
        await provider.GetRequiredService<IPublisher>().Publish(saleCancelled);

        // Then
        var logCall = _logger.ReceivedCalls().Should().ContainSingle(call => call.GetMethodInfo().Name == nameof(ILogger.Log)).Subject;
        logCall.GetArguments()[0].Should().Be(LogLevel.Information);
        logCall.GetArguments()[2].Should().BeAssignableTo<IReadOnlyList<KeyValuePair<string, object?>>>().Which.Should().Equal(
            new KeyValuePair<string, object?>("EventName", "SaleCancelledEvent"),
            new KeyValuePair<string, object?>("@Event", saleCancelled),
            new KeyValuePair<string, object?>("{OriginalFormat}", "Sale event {EventName} published {@Event}"));
    }
```

- [ ] **Step 2: Run it and watch it fail (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~SaleEventLogHandlerTests" 2>&1 | grep -E 'Passed!|Failed!|Expected' | cut -c1-160
```

Expected: the build succeeds; `Failed:     1, Passed:     2`. The new test fails because no handler receives the event, so the logger has no `Log` call.

- [ ] **Step 3: Handle the event**

In `src/Ambev.DeveloperEvaluation.Application/Sales/SaleEventLogHandler.cs`, replace

```csharp
public sealed class SaleEventLogHandler : INotificationHandler<SaleCreatedEvent>
```

with

```csharp
public sealed class SaleEventLogHandler :
    INotificationHandler<SaleCreatedEvent>,
    INotificationHandler<SaleCancelledEvent>
```

Then replace

```csharp
    public Task Handle(SaleCreatedEvent notification, CancellationToken cancellationToken) => Log(notification);
```

with

```csharp
    public Task Handle(SaleCreatedEvent notification, CancellationToken cancellationToken) => Log(notification);

    /// <summary>
    /// Logs a <see cref="SaleCancelledEvent"/>.
    /// </summary>
    /// <param name="notification">The event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    public Task Handle(SaleCancelledEvent notification, CancellationToken cancellationToken) => Log(notification);
```

- [ ] **Step 4: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~SaleEventLogHandlerTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)`; `Passed:     3`; Unit U0 + 5.

- [ ] **Step 5: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Application/Sales/SaleEventLogHandler.cs tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/SaleEventLogHandlerTests.cs
git commit -m "feat(sales): log SaleCancelledEvent" -m "SaleEventLogHandler also handles SaleCancelledEvent, writing the same structured entry as for SaleCreatedEvent."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `2 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 4: Save a loaded sale with `UpdateAsync`

**Skills:** `efcore-patterns`, `testcontainers-integration-tests` (load both now), `dotnet-best-practices`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryTests.cs`
- Modify: `src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs`
- Modify: `src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs`

- [ ] **Step 1: Write the failing integration tests**

In `tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryTests.cs`, replace

```csharp
        // Then
        saved.Should().BeNull();
    }
```

with

```csharp
        // Then
        saved.Should().BeNull();
    }

    /// <summary>
    /// Tests that <see cref="SaleRepository.UpdateAsync"/> saves the changes made to a loaded sale: a new context
    /// reads it cancelled with its <c>UpdatedAt</c>, and its items and total stay as they were (rule R8).
    /// </summary>
    [Fact(DisplayName = "Given a loaded sale that was cancelled When updating it Then a new context reads it cancelled with its items and total unchanged")]
    public async Task Given_LoadedSaleCancelled_When_Updating_Then_NewContextReadsItCancelled()
    {
        // Given
        var sale = SaleTestData.GenerateValidSale(itemCount: 2);
        await using (var createContext = _database.CreateContext())
        {
            await new SaleRepository(createContext).CreateAsync(sale);
        }

        await using var updateContext = _database.CreateContext();
        var repository = new SaleRepository(updateContext);
        var loaded = await repository.GetByIdAsync(sale.Id);
        loaded!.Cancel();

        // When
        await repository.UpdateAsync(loaded);

        // Then
        await using var readContext = _database.CreateContext();
        var saved = await new SaleRepository(readContext).GetByIdAsync(sale.Id);
        saved.Should().NotBeNull();
        saved!.IsCancelled.Should().BeTrue();
        saved.UpdatedAt.Should().BeCloseTo(loaded.UpdatedAt!.Value, TimeSpan.FromMicroseconds(1));
        saved.TotalAmount.Should().Be(sale.TotalAmount);
        saved.Items.Should().BeEquivalentTo(sale.Items);
    }

    /// <summary>
    /// Tests that a sale the context doesn't track is refused instead of silently not saved.
    /// </summary>
    [Fact(DisplayName = "Given a sale the context doesn't track When updating it Then it throws InvalidOperationException")]
    public async Task Given_UntrackedSale_When_Updating_Then_ThrowsInvalidOperationException()
    {
        // Given
        var sale = SaleTestData.GenerateValidSale();
        await using var context = _database.CreateContext();

        // When
        var act = () => new SaleRepository(context).UpdateAsync(sale);

        // Then
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Only a sale loaded with GetByIdAsync can be updated");
    }
```

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Integration 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected: `CS1061: 'SaleRepository' does not contain a definition for 'UpdateAsync'` (two lines).

- [ ] **Step 3: Declare it on the contract**

In `src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs`, replace

```csharp
    Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
```

with

```csharp
    Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the changes made to a sale loaded with <see cref="GetByIdAsync"/>, its items included.
    /// </summary>
    /// <param name="sale">The loaded, changed sale.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the changes are saved.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the sale wasn't loaded with <see cref="GetByIdAsync"/>.</exception>
    Task UpdateAsync(Sale sale, CancellationToken cancellationToken = default);
```

- [ ] **Step 4: Implement it**

In `src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs`, replace

```csharp
            .FirstOrDefaultAsync(sale => sale.Id == id, cancellationToken);
```

with

```csharp
            .FirstOrDefaultAsync(sale => sale.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task UpdateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        if (_context.Entry(sale).State == EntityState.Detached)
            throw new InvalidOperationException("Only a sale loaded with GetByIdAsync can be updated");

        await _context.SaveChangesAsync(cancellationToken);
    }
```

Change tracking writes the changed columns, and the `xmin` token from ticket 05 guards the `Sales` row.

- [ ] **Step 5: Verify (GREEN)**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at ' | tail -1
```

Expected: `0 Error(s)`; Integration I0 + 2; `No changes have been made to the model since the last migration.`

- [ ] **Step 6: Commit and scan**

```bash
git add tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryTests.cs src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs
git commit -m "feat(sales): save a loaded sale with UpdateAsync" -m "ISaleRepository.UpdateAsync saves the changes made to a sale loaded with GetByIdAsync through change tracking, and refuses a sale the context doesn't track instead of silently not saving it."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `3 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 5: Cancel a sale and publish its event after the save

**Skills:** `dotnet-best-practices`, `test-anti-patterns` project note (`Received.InOrder` is the behaviour under test).

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CancelSale/CancelSaleCommandValidatorTests.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CancelSale/CancelSaleHandlerTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.Application/Sales/CancelSale/CancelSaleCommand.cs`, `CancelSaleCommandValidator.cs`, `CancelSaleHandler.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CancelSale/CancelSaleCommandValidatorTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using FluentValidation.TestHelper;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.CancelSale;

/// <summary>
/// Contains unit tests for <see cref="CancelSaleCommandValidator"/>.
/// </summary>
public sealed class CancelSaleCommandValidatorTests
{
    private readonly CancelSaleCommandValidator _validator = new();

    /// <summary>
    /// Tests that an id is required.
    /// </summary>
    [Fact(DisplayName = "Given an empty id When validating Then Id fails as empty")]
    public void Given_EmptyId_When_Validating_Then_IdFails()
    {
        // When
        var result = _validator.TestValidate(new CancelSaleCommand(Guid.Empty));

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
        var result = _validator.TestValidate(new CancelSaleCommand(Guid.NewGuid()));

        // Then
        result.ShouldNotHaveAnyValidationErrors();
    }
}
```

Create `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CancelSale/CancelSaleHandlerTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using AutoMapper;
using FluentAssertions;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.CancelSale;

/// <summary>
/// Contains unit tests for <see cref="CancelSaleHandler"/>. The mapper is the real Sales profile.
/// </summary>
public sealed class CancelSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly CancelSaleHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="CancelSaleHandlerTests"/> class.
    /// </summary>
    public CancelSaleHandlerTests()
    {
        var mapper = new MapperConfiguration(config => config.AddProfile<SaleProfile>()).CreateMapper();
        _handler = new CancelSaleHandler(_saleRepository, _publisher, mapper);
    }

    /// <summary>
    /// Tests that the handler cancels the loaded sale, saves it and returns it with its items and total unchanged.
    /// </summary>
    [Fact(DisplayName = "Given an open sale When cancelling it Then it saves the sale and returns it cancelled with its items and total unchanged")]
    public async Task Given_OpenSale_When_Cancelling_Then_SavesAndReturnsItCancelled()
    {
        // Given
        var sale = GivenLoadedSale();

        // When
        var result = await _handler.Handle(new CancelSaleCommand(sale.Id), CancellationToken.None);

        // Then
        await _saleRepository.Received(1).UpdateAsync(sale, Arg.Any<CancellationToken>());
        result.UpdatedAt.Should().NotBeNull();
        result.Should().BeEquivalentTo(new
        {
            sale.Id,
            IsCancelled = true,
            TotalAmount = 16.20m,
            sale.UpdatedAt,
            Items = new[] { new { sale.Items.Single().Id, TotalAmount = 16.20m, IsCancelled = false } }
        });
    }

    /// <summary>
    /// Tests that an unknown id is a not-found error, and nothing is saved or published.
    /// </summary>
    [Fact(DisplayName = "Given no sale with the id When cancelling Then it throws KeyNotFoundException and saves and publishes nothing")]
    public async Task Given_NoSaleWithId_When_Cancelling_Then_ThrowsKeyNotFoundException()
    {
        // Given
        var id = Guid.NewGuid();
        _saleRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Sale?)null);

        // When
        var act = () => _handler.Handle(new CancelSaleCommand(id), CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"The sale with ID {id} does not exist");
        await _saleRepository.DidNotReceive().UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        _publisher.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R14: <see cref="SaleCancelledEvent"/> is published after the save, and then cleared.
    /// </summary>
    [Fact(DisplayName = "Given an open sale When cancelling it Then it publishes SaleCancelledEvent after the save and clears the events")]
    public async Task Given_OpenSale_When_Cancelling_Then_PublishesSaleCancelledEventAfterSave()
    {
        // Given
        var sale = GivenLoadedSale();
        var published = new List<IDomainEvent>();
        _publisher.When(publisher => publisher.Publish(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>()))
            .Do(call => published.Add(call.Arg<IDomainEvent>()));

        // When
        await _handler.Handle(new CancelSaleCommand(sale.Id), CancellationToken.None);

        // Then
        Received.InOrder(() =>
        {
            _saleRepository.UpdateAsync(sale, Arg.Any<CancellationToken>());
            _publisher.Publish(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
        });
        published.Should().ContainSingle().Which.Should().Be(
            new SaleCancelledEvent(sale.Id, sale.SaleNumber, sale.UpdatedAt!.Value));
        sale.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R14: when the save fails, nothing is published.
    /// </summary>
    [Fact(DisplayName = "Given the save throws When cancelling Then the exception propagates and nothing is published")]
    public async Task Given_SaveThrows_When_Cancelling_Then_NothingIsPublished()
    {
        // Given
        var sale = GivenLoadedSale();
        _saleRepository.UpdateAsync(sale, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("The database is unavailable"));

        // When
        var act = () => _handler.Handle(new CancelSaleCommand(sale.Id), CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("The database is unavailable");
        _publisher.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// Makes the repository return an open sale as it would after loading it: no recorded events,
    /// and one line of 4 × 4.50 at 10% (16.20).
    /// </summary>
    private Sale GivenLoadedSale()
    {
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m));
        sale.ClearDomainEvents();
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        return sale;
    }
}
```

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected: `CS0234: The type or namespace name 'CancelSale' does not exist in the namespace 'Ambev.DeveloperEvaluation.Application.Sales'` in both files, and `CS0246` for `CancelSaleCommandValidator` and `CancelSaleHandler`.

- [ ] **Step 3: Write the command, its validator and the handler**

Create `src/Ambev.DeveloperEvaluation.Application/Sales/CancelSale/CancelSaleCommand.cs`:

```csharp
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSale;

/// <summary>
/// Cancels a sale. Its items and total stay as they were.
/// </summary>
/// <param name="Id">The id of the sale.</param>
public sealed record CancelSaleCommand(Guid Id) : IRequest<SaleResult>;
```

Create `src/Ambev.DeveloperEvaluation.Application/Sales/CancelSale/CancelSaleCommandValidator.cs`:

```csharp
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSale;

/// <summary>
/// Validates <see cref="CancelSaleCommand"/> before its handler runs.
/// </summary>
public sealed class CancelSaleCommandValidator : AbstractValidator<CancelSaleCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CancelSaleCommandValidator"/> class: the id is required.
    /// </summary>
    public CancelSaleCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
```

Create `src/Ambev.DeveloperEvaluation.Application/Sales/CancelSale/CancelSaleHandler.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSale;

/// <summary>
/// Handles <see cref="CancelSaleCommand"/>: the sale cancels itself, the repository saves it,
/// and only then are the recorded events published.
/// </summary>
public sealed class CancelSaleHandler : IRequestHandler<CancelSaleCommand, SaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IPublisher _publisher;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="CancelSaleHandler"/> class.
    /// </summary>
    /// <param name="saleRepository">The sale repository.</param>
    /// <param name="publisher">The MediatR publisher for the recorded events.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    public CancelSaleHandler(ISaleRepository saleRepository, IPublisher publisher, IMapper mapper)
    {
        _saleRepository = saleRepository;
        _publisher = publisher;
        _mapper = mapper;
    }

    /// <summary>
    /// Loads the sale, cancels it, saves it, then publishes its events.
    /// </summary>
    /// <param name="command">The validated command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cancelled sale.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when there is no sale with the id.</exception>
    public async Task<SaleResult> Handle(CancelSaleCommand command, CancellationToken cancellationToken)
    {
        var sale = await _saleRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"The sale with ID {command.Id} does not exist");

        sale.Cancel();

        await _saleRepository.UpdateAsync(sale, cancellationToken);
        await _publisher.PublishDomainEventsAsync(sale, cancellationToken);

        return _mapper.Map<SaleResult>(sale);
    }
}
```

A cancelled sale makes `sale.Cancel()` throw `DomainException`; the middleware answers 409 and nothing is saved.

- [ ] **Step 4: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~Sales.CancelSale" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)`; `Passed:     6`; Unit U0 + 11.

- [ ] **Step 5: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Application/Sales/CancelSale/CancelSaleCommand.cs src/Ambev.DeveloperEvaluation.Application/Sales/CancelSale/CancelSaleCommandValidator.cs src/Ambev.DeveloperEvaluation.Application/Sales/CancelSale/CancelSaleHandler.cs tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CancelSale/CancelSaleCommandValidatorTests.cs tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CancelSale/CancelSaleHandlerTests.cs
git commit -m "feat(sales): cancel a sale and publish SaleCancelledEvent after the save" -m "CancelSaleCommand has a validator that requires the id. CancelSaleHandler loads the sale (KeyNotFoundException if missing), cancels it, saves it with UpdateAsync and only then publishes the recorded events through SaleEventPublisher (rule R14)."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `5 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 6: Cancel a sale over HTTP

**Skills:** `dotnet-best-practices`, `testcontainers-integration-tests`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesControllerTests.cs`
- Modify: `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiHttpExtensions.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Functional/Sales/CancelSaleTests.cs`
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs`

- [ ] **Step 1: Write the failing controller test**

In `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesControllerTests.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
```

with

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
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
    /// Tests that cancelling a sale sends the command for the route id and returns 200 with the cancelled sale in the envelope.
    /// </summary>
    [Fact(DisplayName = "Given a sale id When cancelling the sale Then it sends the command and returns 200 with the cancelled sale")]
    public async Task Given_SaleId_When_CancellingSale_Then_Returns200WithCancelledSale()
    {
        // Given
        const string cancelledSaleJson =
            """{"id":"7f9c2a44-5555-4d1e-8a3b-000000000010","saleNumber":"S-000123","saleDate":"2026-09-24T14:30:00Z","customerId":"3f2b8c1e-1111-4a5b-9c2d-000000000001","customerName":"Maria Silva","branchId":"3f2b8c1e-2222-4a5b-9c2d-000000000002","branchName":"Filial Centro","totalAmount":20.25,"isCancelled":true,"createdAt":"2026-09-24T14:31:02Z","updatedAt":"2026-09-25T10:00:00Z","items":[{"id":"7f9c2a44-6666-4d1e-8a3b-000000000011","productId":"3f2b8c1e-3333-4a5b-9c2d-000000000003","productName":"Cerveja 350ml","quantity":5,"unitPrice":4.50,"discountPercentage":10,"discountAmount":2.25,"totalAmount":20.25,"isCancelled":false}]}""";
        var result = ExampleResult();
        result.IsCancelled = true;
        result.UpdatedAt = new DateTime(2026, 9, 25, 10, 0, 0, DateTimeKind.Utc);
        _mediator.Send(new CancelSaleCommand(result.Id), Arg.Any<CancellationToken>()).Returns(result);

        // When
        var response = await _controller.CancelSale(result.Id, CancellationToken.None);

        // Then
        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        MvcJson.Serialize(ok.Value).Should().Be(
            $$"""{"success":true,"message":"Sale cancelled successfully","data":{{cancelledSaleJson}}}""");
    }
```

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected: `SalesControllerTests.cs(…): error CS1061: 'SalesController' does not contain a definition for 'CancelSale' …`.

- [ ] **Step 3: Add the functional helper**

In `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiHttpExtensions.cs`, replace

```csharp
        using var response = await client.PostAsJsonAsync("/api/sales", sale);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.ReadSaleAsync();
    }
```

with

```csharp
        using var response = await client.PostAsJsonAsync("/api/sales", sale);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.ReadSaleAsync();
    }

    /// <summary>
    /// Cancels a sale with <c>PATCH /api/sales/{id}/cancel</c>, for tests whose subject is a later step, and fails the test if that doesn't return 200.
    /// </summary>
    /// <param name="client">A client that holds a token.</param>
    /// <param name="saleId">The id of the sale to cancel.</param>
    /// <returns>The cancelled sale, as the response returned it.</returns>
    public static async Task<SaleResponseBody> CancelSaleAsync(this HttpClient client, Guid saleId)
    {
        using var response = await client.PatchAsync($"/api/sales/{saleId}/cancel", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await response.ReadSaleAsync();
    }
```

- [ ] **Step 4: Write the failing functional tests**

Create `tests/Ambev.DeveloperEvaluation.Functional/Sales/CancelSaleTests.cs`:

```csharp
using System.Net;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

/// <summary>
/// Contains functional tests for cancelling a sale with <c>PATCH /api/sales/{id}/cancel</c>.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class CancelSaleTests
{
    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="CancelSaleTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public CancelSaleTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// Tests the cancel response: 200, the envelope with the documented fields, and the created sale with
    /// <c>isCancelled</c> true and <c>updatedAt</c> set, its items and total unchanged (rule R8).
    /// </summary>
    [Fact(DisplayName = "Given an open sale When cancelling it Then returns 200 with the sale cancelled, updatedAt set, and its items and total unchanged")]
    public async Task Given_OpenSale_When_Cancelling_Then_Returns200WithSaleCancelled()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var created = await client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid(
            SaleRequestBodyTestData.GenerateItem(4, 4.50m),
            SaleRequestBodyTestData.GenerateItem(12, 3.10m)));

        // When
        using var response = await client.PatchAsync($"/api/sales/{created.Id}/cancel", null);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await SaleJson.ShouldBeSaleEnvelopeAsync(response, "Sale cancelled successfully");
        var cancelled = await response.ReadSaleAsync();
        cancelled.UpdatedAt.Should().BeAfter(created.CreatedAt);
        cancelled.Should().BeEquivalentTo(created with { IsCancelled = true, UpdatedAt = cancelled.UpdatedAt }, options => options
            .ComparingRecordsByMembers()
            .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromMicroseconds(1)))
            .WhenTypeIs<DateTime>());
    }

    /// <summary>
    /// Tests rule R7 over HTTP: a cancelled sale is read-only, so a second cancel is a 409 with the documented body.
    /// </summary>
    [Fact(DisplayName = "Given a cancelled sale When cancelling it again Then returns 409 BusinessRuleViolation")]
    public async Task Given_CancelledSale_When_CancellingAgain_Then_Returns409BusinessRuleViolation()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var created = await client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid());
        await client.CancelSaleAsync(created.Id);

        // When
        using var response = await client.PatchAsync($"/api/sales/{created.Id}/cancel", null);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"BusinessRuleViolation","error":"Business rule violation","detail":"Sale {{created.SaleNumber}} is cancelled and cannot be modified"}""");
    }

    /// <summary>
    /// Tests that an unknown id is a 404 with the documented body.
    /// </summary>
    [Fact(DisplayName = "Given an unknown id When cancelling the sale Then returns 404 ResourceNotFound")]
    public async Task Given_UnknownId_When_CancellingSale_Then_Returns404ResourceNotFound()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var id = Guid.NewGuid();

        // When
        using var response = await client.PatchAsync($"/api/sales/{id}/cancel", null);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"ResourceNotFound","error":"Resource not found","detail":"The sale with ID {{id}} does not exist"}""");
    }

    /// <summary>
    /// Tests that the endpoint requires a JWT and answers without one with the documented 401 body.
    /// </summary>
    [Fact(DisplayName = "Given no token When cancelling a sale Then returns 401 AuthenticationError with the invalid-token body")]
    public async Task Given_NoToken_When_CancellingSale_Then_Returns401WithInvalidTokenBody()
    {
        // Given
        using var client = _api.CreateClient();

        // When
        using var response = await client.PatchAsync($"/api/sales/{Guid.NewGuid()}/cancel", null);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ToString().Should().Be("Bearer");
        (await response.Content.ReadAsStringAsync()).Should().Be(
            """{"type":"AuthenticationError","error":"Invalid authentication token","detail":"The provided authentication token has expired or is invalid"}""");
    }
}
```

- [ ] **Step 5: Watch the functional tests fail (RED)**

The Unit project still doesn't compile (Step 2), so build only the Functional project and its references:

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Functional 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~CancelSaleTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected: the build succeeds; `Failed:     4, Passed:     0`. There is no PATCH route yet, so every request gets an empty 404: the 200, 409 and 401 status checks fail, the 404 test fails on its body, and the 409 test fails inside `CancelSaleAsync`.

- [ ] **Step 6: Add the action**

In `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
```

with

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
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
    /// Cancels a sale. Its items and total stay as they were; a cancelled sale can't be changed again.
    /// </summary>
    /// <param name="id">The id of the sale.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 with the cancelled sale.</returns>
    [HttpPatch("{id}/cancel")]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelSale([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelSaleCommand(id), cancellationToken);

        return Ok(_mapper.Map<SaleResponse>(result), "Sale cancelled successfully");
    }
```

- [ ] **Step 7: Verify (GREEN)**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected: `0 Error(s)`; Unit U0 + 12; Functional F0 + 4.

- [ ] **Step 8: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesControllerTests.cs tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiHttpExtensions.cs tests/Ambev.DeveloperEvaluation.Functional/Sales/CancelSaleTests.cs
git commit -m "feat(sales): cancel a sale over HTTP" -m "PATCH /api/sales/{id}/cancel returns 200 with the cancelled sale (Sale cancelled successfully), 404 for an unknown id and 409 BusinessRuleViolation for a sale that is already cancelled. The functional tests cover cancel, cancel again, an unknown id and a request without a token."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `4 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 7: Add the cancel requests to the `.http` file and replay them against `docker compose up`

**Files:**
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http`

- [ ] **Step 1: Look at the file (RED)**

```bash
grep -c '/cancel' src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
```

Expected: `0`.

- [ ] **Step 2: Add the requests after the get**

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

### Cancel the sale: its items and total stay as they were (200 with isCancelled true)
PATCH {{baseUrl}}/api/sales/{{saleId}}/cancel
Authorization: Bearer {{token}}

### Cancel it again: a cancelled sale is read-only (409 BusinessRuleViolation)
PATCH {{baseUrl}}/api/sales/{{saleId}}/cancel
Authorization: Bearer {{token}}
```

- [ ] **Step 3: Check the file (GREEN)**

```bash
git diff --stat | cat
grep -nE '^###|^(POST|GET|PATCH) ' src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
```

Expected: `1 file changed, 8 insertions(+)`; the two `PATCH {{baseUrl}}/api/sales/{{saleId}}/cancel` lines come right after `GET {{baseUrl}}/api/sales/{{saleId}}`.

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
curl -s -o /tmp/ticket07-checks/login.json -X POST $BASE/api/auth -H 'Content-Type: application/json' --data-raw '{"email":"admin@developerstore.com","password":"Admin@123"}'
TOKEN=$(python3 -c "import json, sys; print(json.load(open(sys.argv[1]))['data']['token'])" /tmp/ticket07-checks/login.json)
curl -s -o /tmp/ticket07-checks/create.json -w "create %{http_code}\n" -X POST $BASE/api/sales -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' --data-raw "{\"saleNumber\":\"S-$(( RANDOM % 900000 + 100000 ))\",\"saleDate\":\"2026-09-24T14:30:00Z\",\"customerId\":\"3f2b8c1e-1111-4a5b-9c2d-000000000001\",\"customerName\":\"Maria Silva\",\"branchId\":\"3f2b8c1e-2222-4a5b-9c2d-000000000002\",\"branchName\":\"Filial Centro\",\"items\":[{\"productId\":\"3f2b8c1e-3333-4a5b-9c2d-000000000003\",\"productName\":\"Cerveja 350ml\",\"quantity\":5,\"unitPrice\":4.50}]}"
SALE_ID=$(python3 -c "import json, sys; print(json.load(open(sys.argv[1]))['data']['id'])" /tmp/ticket07-checks/create.json)
curl -s -o /tmp/ticket07-checks/cancel.json -w "cancel %{http_code} " -X PATCH "$BASE/api/sales/$SALE_ID/cancel" -H "Authorization: Bearer $TOKEN"
python3 -c "import json, sys; d = json.load(open(sys.argv[1]))['data']; print('isCancelled', d['isCancelled'], 'total', d['totalAmount'], 'updatedAt set', d['updatedAt'] is not None, 'item cancelled', d['items'][0]['isCancelled'])" /tmp/ticket07-checks/cancel.json
curl -s -w " %{http_code}\n" -X PATCH "$BASE/api/sales/$SALE_ID/cancel" -H "Authorization: Bearer $TOKEN"
curl -s -o /tmp/ticket07-checks/get.json -w "get %{http_code} " "$BASE/api/sales/$SALE_ID" -H "Authorization: Bearer $TOKEN"
python3 -c "import json, sys; print('isCancelled', json.load(open(sys.argv[1]))['data']['isCancelled'])" /tmp/ticket07-checks/get.json
```

Expected (the sale number is random):

```
sign-up 201
create 201
cancel 200 isCancelled True total 20.25 updatedAt set True item cancelled False
{"type":"BusinessRuleViolation","error":"Business rule violation","detail":"Sale S-… is cancelled and cannot be modified"} 409
get 200 isCancelled True
```

- [ ] **Step 6: Check the event log**

```bash
docker compose logs ambev.developerevaluation.webapi 2>&1 | grep -c 'Sale event SaleCancelledEvent published'
```

Expected: `1`. The rejected second cancel published nothing.

- [ ] **Step 7: Take the stack down**

```bash
docker compose down -v
docker compose ps -a
```

Expected: `ps -a` lists nothing.

- [ ] **Step 8: Commit**

```bash
git add src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
git commit -m "docs(http): cancel a sale" -m "The .http file cancels the created sale, then cancels it again to show the 409 for a cancelled sale."
git show --stat --format= HEAD | tail -1
```

Expected: `1 file changed, 8 insertions(+)`. No slopwatch run: no C# changed.

---

### Task 8: Verify everything, review, then open a pull request into `develop`

**Skills:** superpowers:verification-before-completion, `dotnet-slopwatch`, `test-anti-patterns` (report only), `clean-code` and superpowers:requesting-code-review (optional), superpowers:finishing-a-development-branch.

**Files:** none, unless Step 2 finds a real problem.

- [ ] **Step 1: Re-run every check fresh**

```bash
git status --short
dotnet build Ambev.DeveloperEvaluation.sln --no-incremental 2>&1 | grep -E 'warning CS|Warning\(s\)|Error\(s\)|Build succeeded' | sort -u
dotnet test Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-110
cat /tmp/ticket07-checks/baseline.txt
slopwatch analyze --fail-on warning 2>&1 | tail -1
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at ' | tail -1
```

Expected:
- only `?? docs/superpowers/tickets/` and other tickets' untracked plans
- `0 Error(s)`, `2 Warning(s)`, `Build succeeded.`, no `warning CS`
- Unit U0 + 12, Integration I0 + 2, Functional F0 + 4, compared against the baseline file
- `Scan complete: 0 issue(s) found`
- `No changes have been made to the model since the last migration.`

| Ticket criterion | Evidence |
|---|---|
| `Sale.Cancel()` sets `IsCancelled` and `UpdatedAt`, keeps items and total (R8), records `SaleCancelledEvent` (§5.4) | `SaleTests` (Task 2) |
| Second cancel throws `DomainException` ("Sale … is cancelled and cannot be modified") | `SaleTests` (Task 2) |
| Validator (non-empty id); handler loads (404), cancels, saves with `UpdateAsync`, publishes with the shared helper | `CancelSaleCommandValidatorTests`, `CancelSaleHandlerTests` (Task 5); `SaleRepositoryTests` (Task 4) |
| Event-logging handler logs `SaleCancelledEvent` | `SaleEventLogHandlerTests` (Task 3); Task 7 Step 6 |
| PATCH 200 with the sale; 404 unknown; 409 on second cancel | `SalesControllerTests`, `CancelSaleTests` (Task 6); Task 7 Step 5 |
| Unit tests: domain rules and event; handler happy path, 404, publish after save | Tasks 2, 3, 5 |
| Functional tests: cancel, cancel again (409), unknown id (404), no token (401) | `CancelSaleTests` |
| `.http` gains the cancel request | Task 7 |
| `feature/cancel-sale`, pull request into `develop`, Conventional Commits, no attribution | Steps 4–6 below |

- [ ] **Step 2: Audit the tests (skill `test-anti-patterns`, report only)**

Run it on `CancelSaleCommandValidatorTests`, `CancelSaleHandlerTests`, `CancelSaleTests`, and the new tests in `SaleTests`, `SaleEventLogHandlerTests`, `SalesControllerTests`, `SaleRepositoryTests`, plus `ApiHttpExtensions.CancelSaleAsync`.

Deliberate, report and leave:
- `Received.InOrder` in `CancelSaleHandlerTests` (project note: call order is the behaviour).
- `DateTime.UtcNow` bracketing in `SaleTests` (ticket 05's Decision 6: no clock abstraction).
- The assertion inside `CancelSaleAsync`: it guards a setup step.
- `ReceivedCalls()` on the `ILogger` substitute: `LogInformation` is an extension method.

Fix a real finding in a separate `test(sales): …` commit, then re-run Step 1.

- [ ] **Step 3: Optional code review**

superpowers:requesting-code-review on `develop..feature/cancel-sale`, with `clean-code`, `dotnet-best-practices` and `efcore-patterns` as lenses. Findings that belong to tickets 08–13 go into the report, not this branch.

- [ ] **Step 4: Check the commit messages**

```bash
git log --format=%s develop..feature/cancel-sale
git log --format=%B develop..feature/cancel-sale | grep -n -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
```

Expected (plus any `test(sales):` commit from Step 2):

```
docs(http): cancel a sale
feat(sales): cancel a sale over HTTP
feat(sales): cancel a sale and publish SaleCancelledEvent after the save
feat(sales): save a loaded sale with UpdateAsync
feat(sales): log SaleCancelledEvent
feat(sales): cancel a sale in the domain
no attribution lines
```

- [ ] **Step 5: Push the branch and open a pull request into `develop`**

This is the superpowers:finishing-a-development-branch step: a pull request into `develop` for the user to review. Don't merge it yourself.

Write the pull request body with the Write tool to `/tmp/ticket07-checks/pr-body.md`: two or three sentences on what the ticket delivers (the Goal above), the evidence table from Step 1 with this run's results, and what Steps 2 and 3 reported. No `Co-Authored-By` trailer, no "Generated with" line and no session link. Then:

```bash
git push -u origin feature/cancel-sale
gh pr create --base develop --head feature/cancel-sale --title "Ticket 07: cancel a sale" --body-file /tmp/ticket07-checks/pr-body.md
```

Expected: the push creates `origin/feature/cancel-sale`, and `gh pr create` prints the pull request URL. If `gh` fails (check `gh auth status`), stop and tell the user: the branch is pushed, so they can open the pull request on GitHub.

- [ ] **Step 6: Verify the pull request**

```bash
gh pr view feature/cancel-sale --json state,baseRefName,commits --jq '"\(.state) \(.baseRefName) \(.commits | length) commits"'
gh pr view feature/cancel-sale --json title,body --jq '.title + "\n" + .body' | grep -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
git status -sb | head -1
git status --short
```

Expected: `OPEN develop 6 commits` (7 with a `test(sales):` commit from Step 2); `no attribution lines`; `## feature/cancel-sale...origin/feature/cancel-sale` with no `ahead` or `behind`; only untracked tickets and plans. Give the user the pull request URL in the Step 8 report. Don't merge it: the user merges it on GitHub with **Create a merge commit**, so later plans' merge checks find it. Keep `feature/cancel-sale`.

- [ ] **Step 7: Clean up**

```bash
docker compose ps -a
tasklist | grep -i 'Ambev.DeveloperEvaluation.WebApi' || echo "no API process left"
rm -rf /tmp/ticket07-checks
```

Expected: nothing listed, `no API process left`.

- [ ] **Step 8: Report to the user**

1. Results of Step 1 and any output that differed from this plan (and what systematic-debugging found), or say none did.
2. Ask the user to send the two new PATCH requests once from their editor's `.http` runner.
3. For ticket 08: `CancelItem` reuses `EnsureNotCancelled` and `CancelledSaleMessage`. When the last active item is cancelled it must also cancel the sale, so move the three lines after `EnsureNotCancelled()` in `Cancel` into a private method and call it from both. `ApiHttpExtensions.CancelSaleAsync` covers "cancel an item of a cancelled sale".
