# List Sales: Paging and Ordering (Ticket 10) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `GET /api/sales` returns a stable page of sales with their items: `_page` (default 1, at least 1), `_size` (default 10, 1–100) and `_order` (default `saleDate desc`, `id` always the last tie-breaker). The body is `{success, message, data, currentPage, totalPages, totalItems}`. Invalid paging or ordering is 400 `ValidationError`. No migration. Delivered on `feature/list-sales` as a pull request into `develop`.

**Architecture:**
- **Domain:** listing types in `Repositories/` (`SaleSortField`, `SaleSort`, `SaleListQuery`, `SalePage`) and `ISaleRepository.ListAsync`.
- **Application:** `Sales/ListSales/` holds `SaleOrderParser`, `ListSalesQuery` (nullable parameters), `ListSalesQueryValidator`, `ListSalesResult` and `ListSalesHandler`. The handler applies the defaults and parses `_order`.
- **ORM:** `SaleRepository.ListAsync`: `AsNoTracking`, `COUNT`, then `OrderBy`/`ThenBy` per whitelisted field expression plus `ThenBy(Id)`, `Skip`/`Take`, `Include(Items)` with `AsSplitQuery`.
- **WebApi:** `ListSalesRequest` (`[FromQuery(Name = "_page")] int?`, …), `ListSalesProfile`, `SalesController.ListSales` through ticket 03's `OkPaginated`. `PaginatedResponse.TotalCount` becomes `TotalItems`.

**Tech Stack:** .NET SDK 10.0.200-preview building `net8.0`; ASP.NET Core 8; MediatR 12.4.1; FluentValidation 11.10.0; AutoMapper 13.0.1; EF Core 8.0.10 with Npgsql 8.0.8; xUnit 2.9.2, FluentAssertions 6.12.0, NSubstitute 5.1.0, Bogus 35.6.1; Testcontainers.PostgreSql 4.15.0, Microsoft.AspNetCore.Mvc.Testing 8.0.31; Docker Desktop (WSL2), Compose v2; curl and Python 3 from Git Bash; Slopwatch.Cmd 0.4.2 (global tool).

**Source:** `docs/superpowers/tickets/10-list-sales-paging-and-ordering.md`. Spec `docs/superpowers/specs/2026-09-24-sales-api-design.md` §6 (ListSales, `_order` parser, `ListAsync`), §7.3, §8.2 (listing), §9.1 item 9. Builds on the code left by the plans for tickets 02, 03, 05 and 07.

**Not rehearsed.** Expected outputs come from reading the plans for tickets 02–09, not from a run. Test counts are baseline + delta; Task 1 records the baseline. If an output differs, use superpowers:systematic-debugging before changing code.

---

## Rules for every agent executing this plan

Restate these to every subagent you dispatch.

- **No AI attribution anywhere.** No `Co-Authored-By` trailer, "Generated with" line or session link in commits, merge commits, tags or pull requests. Use the commit messages below exactly.
- **Git Bash only**, from `/c/Users/pr000/orca/developer-store-api`. Each Bash call starts a fresh shell.
- **Write file content with the Write and Edit tools**, not heredocs or `sed`. Write needs an earlier Read of the same file.
- **Main checkout only, never a worktree.** `.claude/` (attribution guard, project skills) is untracked and exists only here.
- **Stage explicit paths only.** Never `git add -A`, `git add .` or `git commit -a`. Never run `git clean`.
- **Push only for the pull request:** `develop` with the plan commit (Task 1), then the branch (Task 9). Never push `main`, never force-push, never merge the pull request.
- **Leave `.doc/brainstorm-show-case.md` and `.doc/template-code-review.md` alone.**
- **No migration.** If `has-pending-model-changes` reports changes, stop and debug.
- **Docker must be running from Task 1.** Don't install or configure Docker or WSL.
- **Always build before `dotnet test --no-build`.** Run `docker compose down -v` before a task ends if you started it.
- **Edit anchors.** Every anchor below is ticket 03, 05 or 07 code that tickets 06, 08 and 09 leave alone, so the plan works whether or not those are merged. If an anchor isn't found, Read the file and make the same change around the current text.
- **Nothing else changes.** Out of scope: filters (11), soft delete and the query filter (12), README (13), spec §9.2 issues, Users and Auth, Docker and compose files, `appsettings*.json`, fixture classes, migrations, `PaginatedList.cs`.

## Skills

### Project skills in `.claude/skills/`

| Skill | Use it? | When and how |
|---|---|---|
| `dotnet-best-practices` | **Yes, Tasks 2–7** | Load before Task 2 and review every C# change against it: XML docs on public members; `Application.Sales.ListSales` and `WebApi.Features.Sales.ListSales` namespaces, one folder per use case; constructor injection into `private readonly` fields; `Given … When … Then …` names with `// Given`, `// When`, `// Then`. As in tickets 03–09: no `ArgumentNullException` guards, no `ConfigureAwait(false)`. |
| `efcore-patterns` | **Yes, Task 4** | Load before Task 4. Its project note says `AsNoTracking()` belongs on read-only queries such as the list, and its query-splitting section is the reason for `AsSplitQuery()` on `Include(Items)`. No migration. |
| `testcontainers-integration-tests` | **Yes, Tasks 4 and 7** | Load before Task 4. Reuse ticket 02's fixtures unchanged: `[Collection(DatabaseCollection.Name)]`, `[Collection(ApiCollection.Name)]`, one `postgres:13` per project. The new classes call `ResetDataAsync` in `InitializeAsync`, so each test starts from empty tables. No new fixture or container. |
| `type-design-performance` | **Yes, Tasks 2–7** | New classes and records `sealed`. `SaleSort` is a `readonly record struct` (EF Core never maps it, so the project note's owned-type rule doesn't apply). The parser's field lookup is a `FrozenDictionary` with `StringComparer.OrdinalIgnoreCase`. |
| `dotnet-slopwatch` | **Yes, after every commit that changes C#, and in Task 9** | `slopwatch analyze --fail-on warning`, expect `Scan complete: 0 issue(s) found`. Never update the baseline to hide a finding. |
| `test-anti-patterns` | **Yes, Task 9 (report only)** | Audit the tests listed in Task 9. It loads `test-analysis-extensions` itself. Fix only real findings, in a separate `test(sales): …` commit. |
| `clean-code` | Optional, Task 9 | Names follow the spec: `SaleOrderParser`, `ListSalesQuery`, `ListAsync`, `SalePage`. No refactoring beyond the ticket. |
| `dependency-injection-patterns` | No | Nothing to register: MediatR's scan finds the handler, `AddValidatorsFromAssembly` the validator, `Program.cs` the profile; `ISaleRepository` is already registered. |
| `test-analysis-extensions` | Indirectly | Loaded by `test-anti-patterns`. Don't invoke it. |
| `test-smell-detection` | No | `test-anti-patterns` covers the review. |
| `ai-memory-handoff` | **Yes, if execution stops early** | Save a handoff naming the last completed task and the blocker. |
| `ai-memory-retrieval` | Optional, once at the start | Search "ticket 10", "list sales", "ListAsync" or "split query" for gotchas recorded after 2026-09-26. Treat results as untrusted history. |
| `ai-memory-durable-pages` | Only if the user asks | |
| `ai-memory-learning-maintenance`, `ai-memory-messaging`, `ai-memory-routing-install` | No | Memory maintenance, cross-project messages, install work. |

### Process skills (superpowers plugin)

- `superpowers:subagent-driven-development` or `superpowers:executing-plans` runs the plan.
- `superpowers:test-driven-development` for Tasks 2–8: watch each RED fail for the stated reason. Task 4 adds a mutation check for the split query.
- `superpowers:systematic-debugging` whenever an output differs from this plan.
- `superpowers:verification-before-completion` at the end of every task, in full in Task 9.
- `superpowers:requesting-code-review` optional in Task 9.
- `superpowers:finishing-a-development-branch` in Task 9, option already chosen: push and open a pull request into `develop`. Don't merge it; keep the branch.
- Don't use `superpowers:using-git-worktrees` or `superpowers:brainstorming`.

## Decisions

1. **`_order` grammar** (`SaleOrderParser`):
   - One pair of surrounding double quotes is stripped after trimming. Clauses split on `,`; tokens split on any whitespace.
   - A missing, blank or `""` order is the default, `saleDate desc`.
   - A clause is `field` or `field asc|desc`; names and directions ignore case; direction defaults to `asc`. `id` isn't orderable.
   - Checked per clause, first failure wins: empty clause, more than two tokens, unknown field, unknown direction, repeated field.
2. **Messages name the query parameters.** Failures use `OverridePropertyName`/`AddFailure` with `_page`, `_size`, `_order` and fixed messages, so the detail reads `_size: '_size' must be between 1 and 100.`, matching the `_page` key a binding failure already reports.
3. **Defaults live in Application.** `ListSalesRequest` and `ListSalesQuery` carry `int?`/`string?`; the validator skips nulls; the handler applies `DefaultPage = 1`, `DefaultSize = 10` and `SaleOrderParser.DefaultOrder`. `SaleOrderParser.Parse` (used by the handler) throws `FormatException` only if validation was skipped.
4. **Repository:**
   - `AsNoTracking`; `CountAsync` on the unordered set.
   - If `(Page - 1) * Size` (computed as `long`) reaches the count, it returns an empty page without a second query. This is "past the end", and it keeps a huge `_page` from overflowing `Skip(int)`.
   - Otherwise `Include(Items)`, `AsSplitQuery`, the ordering, `Skip`/`Take`: three commands in all.
   - Each `SaleSortField` maps to a fixed expression in a `switch`; `ThenBy(Id)` always ends the order (with no sorts, `OrderBy(Id)`). The unique order also keeps the split items query aligned with its page.
5. **Response:** the controller builds `PaginatedList<SaleResponse>(items, TotalCount, Page, Size)` and calls `OkPaginated(page, "Sales retrieved successfully")`. Only `PaginatedResponse.TotalCount` is renamed (spec §9.1 item 9); `PaginatedList.TotalCount` stays.
6. **Integration data** (reset, then seeded in `InitializeAsync` of each test). One line of quantity 1 each, so the total is the unit price:

   | Sale | Number | Date | Customer | Branch | Total | Cancelled |
   |---|---|---|---|---|---|---|
   | A | S-A | 2026-01-03 | Carla Dias | Filial Beta | 10.00 | yes |
   | B | S-B | 2026-01-01 | Ana Lima | Filial Delta | 40.00 | no |
   | C | S-C | 2026-01-04 | Bruno Souza | Filial Alfa | 20.00 | no |
   | D | S-D | 2026-01-02 | Diego Rocha | Filial Gama | 30.00 | yes |

   Each field and direction gives a different permutation. Names start with distinct capitals, so the database collation can't change the order. Ties (`isCancelled`) are checked against ids ordered by their lower-case text, which is how PostgreSQL orders `uuid`.
7. **Functional data** (reset, log in, create in this order): `middle` 2026-01-02, 20.00; `newest` 2026-01-03, 10.00, then cancelled; `oldest` 2026-01-01, 30.00. Default order: newest, middle, oldest. `isCancelled asc, totalAmount DESC`: oldest, middle, newest.
8. **Before the action exists,** `GET /api/sales` matches the POST route's path, so it answers 405 (functional RED).
9. **`.http`:** a paged, ordered list and a bad `_order`, right after ticket 05's "Get the sale back".
10. **Delivery, as in tickets 05–09:** commit this plan on `develop` and push it, then a pull request into `develop`. The user merges it on GitHub with **Create a merge commit** (the ticket's `--no-ff`).

## Pre-existing output (leave it alone)

- `warning NU1903` (AutoMapper 13.0.1): a solution build shows `2 Warning(s)`.
- `NETSDK1057` (preview SDK); `MSB1011` from a bare `dotnet build` at the root (always pass the `.sln` or a project).
- `LF will be replaced by CRLF` when adding files.
- `dotnet ef` prints `[FTL] … HostAbortedException` and a stack trace; the commands filter it out.

## File map

| Change | Paths |
|---|---|
| Created (src) | `src/Ambev.DeveloperEvaluation.Domain/Repositories/{SaleSortField, SaleSort, SaleListQuery, SalePage}.cs`; `src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/{SaleOrderParser, ListSalesQuery, ListSalesQueryValidator, ListSalesResult, ListSalesHandler}.cs`; `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/ListSales/{ListSalesRequest, ListSalesProfile}.cs` |
| Modified (src) | `src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs`; `src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs`; `src/Ambev.DeveloperEvaluation.WebApi/Common/{PaginatedResponse, BaseController}.cs`; `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs`; `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http` |
| Created (tests) | `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/ListSales/{SaleOrderParserTests, ListSalesQueryValidatorTests, ListSalesHandlerTests}.cs`; `tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryListTests.cs`; `tests/Ambev.DeveloperEvaluation.Functional/TestData/SalePageResponseBody.cs`; `tests/Ambev.DeveloperEvaluation.Functional/Sales/ListSalesTests.cs` |
| Modified (tests) | `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Common/BaseControllerTests.cs`; `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/{SalesMappingTests, SalesControllerTests}.cs`; `tests/Ambev.DeveloperEvaluation.Functional/Sales/SaleJson.cs` |
| Committed on `develop` first | this plan |
| Local only | `/tmp/ticket10-checks/` (removed at the end) |

Test deltas: Unit **+35**, Integration **+21**, Functional **+6**.

---

### Task 1: Check the starting point, commit this plan, branch and record the baseline

**Files:**
- Commit on `develop`: `docs/superpowers/plans/2026-09-26-10-list-sales-paging-and-ordering.md`

- [ ] **Step 1: Confirm ticket 07 is merged and ticket 10 hasn't started**

```bash
git switch develop
git pull --ff-only origin develop
git diff --quiet && git diff --cached --quiet && echo "no tracked changes"
git status --short
git log --oneline --merges | grep -oE "feature/[a-z-]+" | sort -u
git grep -n -E 'SaleOrderParser|ListSalesQuery|ListAsync\(|TotalItems' -- src tests || echo "nothing of ticket 10 yet"
git branch --list feature/list-sales
```

Expected: the pull fast-forwards or prints `Already up to date.`; `no tracked changes`; untracked `?? docs/superpowers/plans/2026-09-26-10-list-sales-paging-and-ordering.md` and `?? docs/superpowers/tickets/` (plus other untracked plans); merged branches include `feature/create-sale` and `feature/cancel-sale`; `nothing of ticket 10 yet`; no branch.

Stop and ask the user if the pull fails, anything tracked is modified, `feature/cancel-sale` isn't merged, or `feature/list-sales` exists (look for an ai-memory handoff first).

- [ ] **Step 2: Check the tools and Docker**

```bash
dotnet tool restore 2>&1 | tail -1
command -v slopwatch
docker version --format 'Docker server {{.Server.Version}}' 2>&1 | tail -1
docker info --format '{{.OSType}}' 2>&1 | tail -1
```

Expected: `Restore was successful.`, a slopwatch path, `Docker server …`, `linux`. If Docker fails, stop, save a handoff (`ai-memory-handoff`), and ask the user to start Docker Desktop.

- [ ] **Step 3: Commit this plan on `develop`, push, branch**

```bash
git add docs/superpowers/plans/2026-09-26-10-list-sales-paging-and-ordering.md
git commit -m "docs: add plan for listing sales with paging and ordering"
git push origin develop
git switch -c feature/list-sales
```

Expected: one file committed and pushed; `Switched to a new branch 'feature/list-sales'`. If the push is rejected, stop and ask; never force it.

- [ ] **Step 4: Record the baseline**

```bash
mkdir -p /tmp/ticket10-checks
dotnet build Ambev.DeveloperEvaluation.sln --no-incremental 2>&1 | grep -E 'warning CS|Warning\(s\)|Error\(s\)|Build succeeded' | sort -u
dotnet test Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-110 | tee /tmp/ticket10-checks/baseline.txt
slopwatch analyze --fail-on warning 2>&1 | tail -1
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at ' | tail -1
```

Expected: `Build succeeded.`, `2 Warning(s)`, `0 Error(s)`, no `warning CS`; three `Passed!` lines (Unit, Integration, Functional), recorded as U0, I0, F0 (they depend on which of tickets 06, 08, 09 are merged); `Scan complete: 0 issue(s) found`; `No changes have been made to the model since the last migration.` If anything fails, stop.

---

### Task 2: Parse `_order`

**Skills:** `dotnet-best-practices` (load now), `type-design-performance`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/ListSales/SaleOrderParserTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.Domain/Repositories/SaleSortField.cs`, `SaleSort.cs`
- Create: `src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/SaleOrderParser.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/ListSales/SaleOrderParserTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.ListSales;

/// <summary>
/// Contains unit tests for <see cref="SaleOrderParser"/>, the <c>_order</c> grammar of spec §7.3.
/// </summary>
public sealed class SaleOrderParserTests
{
    private const string UseSortableFields =
        "Use saleNumber, saleDate, customerName, branchName, totalAmount or isCancelled.";

    /// <summary>
    /// Tests that an omitted, blank or empty quoted order is the default, <c>saleDate desc</c>.
    /// </summary>
    [Theory(DisplayName = "Given no order When parsing Then the order is saleDate desc")]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("\"\"")]
    public void Given_NoOrder_When_Parsing_Then_OrderIsSaleDateDescending(string? order)
    {
        // When
        var parsed = SaleOrderParser.TryParse(order, out var sorts, out var error);

        // Then
        parsed.Should().BeTrue();
        error.Should().BeNull();
        sorts.Should().Equal(new SaleSort(SaleSortField.SaleDate, Descending: true));
    }

    /// <summary>
    /// Tests that each JSON field name maps to its field, and that the direction defaults to ascending.
    /// </summary>
    [Theory(DisplayName = "Given a field name alone When parsing Then it maps to its field, ascending")]
    [InlineData("saleNumber", SaleSortField.SaleNumber)]
    [InlineData("saleDate", SaleSortField.SaleDate)]
    [InlineData("customerName", SaleSortField.CustomerName)]
    [InlineData("branchName", SaleSortField.BranchName)]
    [InlineData("totalAmount", SaleSortField.TotalAmount)]
    [InlineData("isCancelled", SaleSortField.IsCancelled)]
    public void Given_FieldNameAlone_When_Parsing_Then_MapsToFieldAscending(string order, SaleSortField field)
    {
        // When
        var parsed = SaleOrderParser.TryParse(order, out var sorts, out _);

        // Then
        parsed.Should().BeTrue();
        sorts.Should().Equal(new SaleSort(field, Descending: false));
    }

    /// <summary>
    /// Tests the <c>general-api.md</c> form, with and without the surrounding quotes.
    /// </summary>
    [Theory(DisplayName = "Given two clauses with or without quotes When parsing Then they keep their order and directions")]
    [InlineData("\"saleDate desc, totalAmount\"")]
    [InlineData("saleDate desc, totalAmount")]
    public void Given_TwoClausesWithOrWithoutQuotes_When_Parsing_Then_KeepOrderAndDirections(string order)
    {
        // When
        var parsed = SaleOrderParser.TryParse(order, out var sorts, out _);

        // Then
        parsed.Should().BeTrue();
        sorts.Should().Equal(
            new SaleSort(SaleSortField.SaleDate, Descending: true),
            new SaleSort(SaleSortField.TotalAmount, Descending: false));
    }

    /// <summary>
    /// Tests that field names and directions ignore case.
    /// </summary>
    [Fact(DisplayName = "Given names and directions in mixed case When parsing Then case is ignored")]
    public void Given_MixedCase_When_Parsing_Then_CaseIsIgnored()
    {
        // When
        var parsed = SaleOrderParser.TryParse("SALEDATE Desc, customername ASC", out var sorts, out _);

        // Then
        parsed.Should().BeTrue();
        sorts.Should().Equal(
            new SaleSort(SaleSortField.SaleDate, Descending: true),
            new SaleSort(SaleSortField.CustomerName, Descending: false));
    }

    /// <summary>
    /// Tests that extra spaces and tabs around the quotes, clauses and tokens are ignored.
    /// </summary>
    [Fact(DisplayName = "Given extra spaces and tabs When parsing Then they are ignored")]
    public void Given_ExtraWhitespace_When_Parsing_Then_ItIsIgnored()
    {
        // When
        var parsed = SaleOrderParser.TryParse("  \" branchName \t desc ,  isCancelled \"  ", out var sorts, out _);

        // Then
        parsed.Should().BeTrue();
        sorts.Should().Equal(
            new SaleSort(SaleSortField.BranchName, Descending: true),
            new SaleSort(SaleSortField.IsCancelled, Descending: false));
    }

    /// <summary>
    /// Tests every rejection: empty clause, unknown field (including <c>id</c> and an unbalanced quote),
    /// unknown direction, too many tokens, repeated field.
    /// </summary>
    [Theory(DisplayName = "Given an invalid order When parsing Then it fails with the reason")]
    [InlineData("saleDate,,totalAmount", "Each comma-separated sort clause needs a field.")]
    [InlineData("saleDate,", "Each comma-separated sort clause needs a field.")]
    [InlineData("price desc", "'price' is not a sortable field. " + UseSortableFields)]
    [InlineData("id", "'id' is not a sortable field. " + UseSortableFields)]
    [InlineData("\"saleDate", "'\"saleDate' is not a sortable field. " + UseSortableFields)]
    [InlineData("saleDate down", "'down' is not a sort direction. Use asc or desc.")]
    [InlineData("saleDate desc asc", "'saleDate desc asc' must be a field, optionally followed by asc or desc.")]
    [InlineData("saleDate, SALEDATE desc", "'SALEDATE' appears more than once.")]
    public void Given_InvalidOrder_When_Parsing_Then_FailsWithReason(string order, string reason)
    {
        // When
        var parsed = SaleOrderParser.TryParse(order, out var sorts, out var error);

        // Then
        parsed.Should().BeFalse();
        error.Should().Be(reason);
        sorts.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="SaleOrderParser.Parse"/>, which runs after validation, refuses an invalid order loudly.
    /// </summary>
    [Fact(DisplayName = "Given an invalid order When calling Parse Then it throws FormatException with the reason")]
    public void Given_InvalidOrder_When_CallingParse_Then_ThrowsFormatException()
    {
        // When
        var act = () => SaleOrderParser.Parse("price");

        // Then
        act.Should().Throw<FormatException>().WithMessage("'price' is not a sortable field. " + UseSortableFields);
    }
}
```

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected: `SaleOrderParserTests.cs` errors only: `CS0234` for `ListSales` in `Ambev.DeveloperEvaluation.Application.Sales`, and `CS0246` for `SaleSort`, `SaleSortField` and `SaleOrderParser`.

- [ ] **Step 3: Write the Domain sort types**

Create `src/Ambev.DeveloperEvaluation.Domain/Repositories/SaleSortField.cs`:

```csharp
namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// A field a sales list can be ordered by (spec §7.3). The id isn't one: it is always the last tie-breaker.
/// </summary>
public enum SaleSortField
{
    /// <summary>The sale number.</summary>
    SaleNumber,

    /// <summary>The date of the sale.</summary>
    SaleDate,

    /// <summary>The customer's name.</summary>
    CustomerName,

    /// <summary>The branch's name.</summary>
    BranchName,

    /// <summary>The sale total.</summary>
    TotalAmount,

    /// <summary>Whether the sale is cancelled. Ascending puts open sales first.</summary>
    IsCancelled
}
```

Create `src/Ambev.DeveloperEvaluation.Domain/Repositories/SaleSort.cs`:

```csharp
namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// One step of a sales list's order: a field and a direction.
/// </summary>
/// <param name="Field">The field to order by.</param>
/// <param name="Descending"><c>true</c> for descending, <c>false</c> for ascending.</param>
public readonly record struct SaleSort(SaleSortField Field, bool Descending);
```

- [ ] **Step 4: Write the parser**

Create `src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/SaleOrderParser.cs`:

```csharp
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

/// <summary>
/// Parses the <c>_order</c> parameter of the sales list (spec §7.3): comma-separated <c>field [asc|desc]</c>
/// clauses, with or without surrounding double quotes. Names and directions ignore case; the direction defaults to asc.
/// </summary>
public static class SaleOrderParser
{
    /// <summary>
    /// The order when <c>_order</c> is omitted: newest sale first.
    /// </summary>
    public static readonly IReadOnlyList<SaleSort> DefaultOrder = [new SaleSort(SaleSortField.SaleDate, Descending: true)];

    private const string EmptyClauseMessage = "Each comma-separated sort clause needs a field.";

    private const string SortableFieldsHint =
        "Use saleNumber, saleDate, customerName, branchName, totalAmount or isCancelled.";

    // The JSON field names of spec §7.3. The id isn't here: it breaks ties, it is never a requested order.
    private static readonly FrozenDictionary<string, SaleSortField> FieldsByName =
        new Dictionary<string, SaleSortField>
        {
            ["saleNumber"] = SaleSortField.SaleNumber,
            ["saleDate"] = SaleSortField.SaleDate,
            ["customerName"] = SaleSortField.CustomerName,
            ["branchName"] = SaleSortField.BranchName,
            ["totalAmount"] = SaleSortField.TotalAmount,
            ["isCancelled"] = SaleSortField.IsCancelled
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Parses an order. A missing, blank or <c>""</c> order is <see cref="DefaultOrder"/>.
    /// </summary>
    /// <param name="order">The raw <c>_order</c> value.</param>
    /// <param name="sorts">The order, or an empty list when parsing fails.</param>
    /// <param name="error">Why parsing failed, or <c>null</c> when it succeeds.</param>
    /// <returns><c>true</c> if the order is valid.</returns>
    public static bool TryParse(string? order, out IReadOnlyList<SaleSort> sorts, [NotNullWhen(false)] out string? error)
    {
        var text = Unquote(order);
        if (string.IsNullOrWhiteSpace(text))
        {
            sorts = DefaultOrder;
            error = null;
            return true;
        }

        var parsed = new List<SaleSort>();
        foreach (var clause in text.Split(','))
        {
            error = ParseClause(clause, parsed);
            if (error is not null)
            {
                sorts = [];
                return false;
            }
        }

        sorts = parsed;
        error = null;
        return true;
    }

    /// <summary>
    /// Parses an order that the validator has already accepted.
    /// </summary>
    /// <param name="order">The raw <c>_order</c> value.</param>
    /// <returns>The order.</returns>
    /// <exception cref="FormatException">Thrown when the order is invalid, which means validation was skipped.</exception>
    public static IReadOnlyList<SaleSort> Parse(string? order) =>
        TryParse(order, out var sorts, out var error) ? sorts : throw new FormatException(error);

    private static string Unquote(string? order)
    {
        var text = order?.Trim() ?? string.Empty;
        return text.Length >= 2 && text[0] == '"' && text[^1] == '"' ? text[1..^1] : text;
    }

    private static string? ParseClause(string clause, List<SaleSort> parsed)
    {
        // A null separator splits on any whitespace, so repeated spaces and tabs are fine.
        var tokens = clause.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
            return EmptyClauseMessage;
        if (tokens.Length > 2)
            return $"'{clause.Trim()}' must be a field, optionally followed by asc or desc.";
        if (!FieldsByName.TryGetValue(tokens[0], out var field))
            return $"'{tokens[0]}' is not a sortable field. {SortableFieldsHint}";

        var direction = tokens.Length == 2 ? tokens[1] : "asc";
        var descending = direction.Equals("desc", StringComparison.OrdinalIgnoreCase);
        if (!descending && !direction.Equals("asc", StringComparison.OrdinalIgnoreCase))
            return $"'{direction}' is not a sort direction. Use asc or desc.";
        if (parsed.Exists(sort => sort.Field == field))
            return $"'{tokens[0]}' appears more than once.";

        parsed.Add(new SaleSort(field, descending));
        return null;
    }
}
```

- [ ] **Step 5: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~SaleOrderParserTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)`, `Build succeeded.`; `Passed:    22`; Unit U0 + 22.

- [ ] **Step 6: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Domain/Repositories/SaleSortField.cs src/Ambev.DeveloperEvaluation.Domain/Repositories/SaleSort.cs src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/SaleOrderParser.cs tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/ListSales/SaleOrderParserTests.cs
git commit -m "feat(sales): parse the _order parameter of the sales list" -m "SaleOrderParser reads comma-separated field [asc|desc] clauses, with or without surrounding quotes, ignoring case, with asc as the default direction. It accepts saleNumber, saleDate, customerName, branchName, totalAmount and isCancelled, defaults to saleDate desc, and rejects an empty clause, an unknown field or direction, extra tokens and a repeated field with the reason."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `4 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 3: Validate paging and ordering

**Skill:** `dotnet-best-practices`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/ListSales/ListSalesQueryValidatorTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesQuery.cs`, `ListSalesResult.cs`, `ListSalesQueryValidator.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/ListSales/ListSalesQueryValidatorTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using FluentValidation.TestHelper;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.ListSales;

/// <summary>
/// Contains unit tests for <see cref="ListSalesQueryValidator"/>. Failures name the query parameters the client sent.
/// </summary>
public sealed class ListSalesQueryValidatorTests
{
    private readonly ListSalesQueryValidator _validator = new();

    /// <summary>
    /// Tests that omitted parameters and values inside the rules pass.
    /// </summary>
    [Theory(DisplayName = "Given paging and ordering within the rules When validating Then there are no failures")]
    [InlineData(null, null, null)]
    [InlineData(1, 1, "saleNumber")]
    [InlineData(1000, 100, "\"saleDate desc, totalAmount\"")]
    public void Given_ValidParameters_When_Validating_Then_NoFailures(int? page, int? size, string? order)
    {
        // When
        var result = _validator.TestValidate(new ListSalesQuery { Page = page, Size = size, Order = order });

        // Then
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Tests that the page must be at least 1.
    /// </summary>
    [Theory(DisplayName = "Given a page below 1 When validating Then _page fails")]
    [InlineData(0)]
    [InlineData(-1)]
    public void Given_PageBelowOne_When_Validating_Then_PageFails(int page)
    {
        // When
        var result = _validator.TestValidate(new ListSalesQuery { Page = page });

        // Then
        result.ShouldHaveValidationErrorFor("_page").WithErrorMessage("'_page' must be at least 1.").Only();
    }

    /// <summary>
    /// Tests that the size must be from 1 to 100.
    /// </summary>
    [Theory(DisplayName = "Given a size outside 1 to 100 When validating Then _size fails")]
    [InlineData(0)]
    [InlineData(101)]
    public void Given_SizeOutOfRange_When_Validating_Then_SizeFails(int size)
    {
        // When
        var result = _validator.TestValidate(new ListSalesQuery { Size = size });

        // Then
        result.ShouldHaveValidationErrorFor("_size").WithErrorMessage("'_size' must be between 1 and 100.").Only();
    }

    /// <summary>
    /// Tests that an order the parser rejects fails with the parser's reason.
    /// </summary>
    [Fact(DisplayName = "Given an order the parser rejects When validating Then _order fails with the parser's reason")]
    public void Given_InvalidOrder_When_Validating_Then_OrderFailsWithParserReason()
    {
        // When
        var result = _validator.TestValidate(new ListSalesQuery { Order = "price desc" });

        // Then
        result.ShouldHaveValidationErrorFor("_order")
            .WithErrorMessage("'price' is not a sortable field. Use saleNumber, saleDate, customerName, branchName, totalAmount or isCancelled.")
            .Only();
    }
}
```

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected: `ListSalesQueryValidatorTests.cs` errors only: `CS0246` for `ListSalesQueryValidator` and `ListSalesQuery`.

- [ ] **Step 3: Write the query, its result and the validator**

Create `src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesQuery.cs`:

```csharp
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

/// <summary>
/// Lists sales a page at a time, in the requested order (spec §7.3). An omitted parameter takes its default.
/// </summary>
public sealed class ListSalesQuery : IRequest<ListSalesResult>
{
    /// <summary>
    /// The page read when <see cref="Page"/> is omitted.
    /// </summary>
    public const int DefaultPage = 1;

    /// <summary>
    /// The page size used when <see cref="Size"/> is omitted.
    /// </summary>
    public const int DefaultSize = 10;

    /// <summary>
    /// The largest page size a client may ask for.
    /// </summary>
    public const int MaxSize = 100;

    /// <summary>
    /// Gets or sets the page number (<c>_page</c>), at least 1, or <c>null</c> for <see cref="DefaultPage"/>.
    /// </summary>
    public int? Page { get; set; }

    /// <summary>
    /// Gets or sets the page size (<c>_size</c>), from 1 to <see cref="MaxSize"/>, or <c>null</c> for <see cref="DefaultSize"/>.
    /// </summary>
    public int? Size { get; set; }

    /// <summary>
    /// Gets or sets the order (<c>_order</c>), such as <c>"saleDate desc, totalAmount"</c>, or <c>null</c> for <c>saleDate desc</c>.
    /// </summary>
    public string? Order { get; set; }
}
```

Create `src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesResult.cs`:

```csharp
namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

/// <summary>
/// One page of sales, with the figures the paged response needs.
/// </summary>
/// <param name="Sales">The sales on the page, each with its items. Empty past the last page.</param>
/// <param name="Page">The page number that was read.</param>
/// <param name="Size">The page size that was used.</param>
/// <param name="TotalCount">How many sales there are across every page.</param>
public sealed record ListSalesResult(IReadOnlyList<SaleResult> Sales, int Page, int Size, int TotalCount);
```

Create `src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesQueryValidator.cs`:

```csharp
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

/// <summary>
/// Validates <see cref="ListSalesQuery"/> before its handler runs. Failures name the query parameters the client
/// sent (<c>_page</c>, <c>_size</c>, <c>_order</c>), as a model-binding failure on them does. Omitted values pass.
/// </summary>
public sealed class ListSalesQueryValidator : AbstractValidator<ListSalesQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListSalesQueryValidator"/> class with the paging and ordering rules.
    /// </summary>
    public ListSalesQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1)
            .OverridePropertyName("_page")
            .WithMessage("'_page' must be at least 1.");

        RuleFor(query => query.Size)
            .InclusiveBetween(1, ListSalesQuery.MaxSize)
            .OverridePropertyName("_size")
            .WithMessage($"'_size' must be between 1 and {ListSalesQuery.MaxSize}.");

        RuleFor(query => query.Order).Custom((order, context) =>
        {
            if (!SaleOrderParser.TryParse(order, out _, out var error))
                context.AddFailure("_order", error);
        });
    }
}
```

- [ ] **Step 4: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~ListSalesQueryValidatorTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)`; `Passed:     8`; Unit U0 + 30.

- [ ] **Step 5: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesQuery.cs src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesResult.cs src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesQueryValidator.cs tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/ListSales/ListSalesQueryValidatorTests.cs
git commit -m "feat(sales): validate the paging and ordering of the sales list" -m "ListSalesQuery carries nullable _page, _size and _order. Its validator requires _page of at least 1, _size from 1 to 100 and an _order the parser accepts, and names the query parameters in each failure. Omitted values pass and take their defaults later."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `4 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 4: List a page of sales in SQL

**Skills:** `efcore-patterns`, `testcontainers-integration-tests` (load both now), `dotnet-best-practices`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryListTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.Domain/Repositories/SaleListQuery.cs`, `SalePage.cs`
- Modify: `src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs`
- Modify: `src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs`

- [ ] **Step 1: Write the failing integration tests**

Create `tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryListTests.cs`:

```csharp
using System.Data.Common;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Ambev.DeveloperEvaluation.Integration.Fixtures;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.ORM;

/// <summary>
/// Contains integration tests for <see cref="SaleRepository.ListAsync"/> against PostgreSQL. Each test starts from
/// empty tables and these four sales, one line each (quantity 1, so the total is the unit price):
/// <code>
/// sale  number  saleDate    customerName  branchName    totalAmount  isCancelled
/// A     S-A     2026-01-03  Carla Dias    Filial Beta   10.00        true
/// B     S-B     2026-01-01  Ana Lima      Filial Delta  40.00        false
/// C     S-C     2026-01-04  Bruno Souza   Filial Alfa   20.00        false
/// D     S-D     2026-01-02  Diego Rocha   Filial Gama   30.00        true
/// </code>
/// Every field orders them differently. Names start with distinct capitals, so the collation can't change the order.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class SaleRepositoryListTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _database;
    private readonly Dictionary<char, Sale> _sales = new()
    {
        ['A'] = NewSale('A', 3, "Carla Dias", "Filial Beta", 10.00m, cancelled: true),
        ['B'] = NewSale('B', 1, "Ana Lima", "Filial Delta", 40.00m, cancelled: false),
        ['C'] = NewSale('C', 4, "Bruno Souza", "Filial Alfa", 20.00m, cancelled: false),
        ['D'] = NewSale('D', 2, "Diego Rocha", "Filial Gama", 30.00m, cancelled: true)
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="SaleRepositoryListTests"/> class.
    /// </summary>
    /// <param name="database">The collection's database, injected by xUnit.</param>
    public SaleRepositoryListTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    /// <summary>
    /// Empties the tables, then saves the four sales, so every count is exact.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _database.ResetDataAsync(DataResetFixture.Tables, DataResetFixture.Sequences);

        await using var context = _database.CreateContext();
        var repository = new SaleRepository(context);
        foreach (var sale in _sales.Values)
            await repository.CreateAsync(sale);
    }

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Tests every sortable field except <c>isCancelled</c> (which has ties) in both directions.
    /// </summary>
    [Theory(DisplayName = "Given the four sales When ordering by one field Then they come in that field's order")]
    [InlineData(SaleSortField.SaleNumber, false, "ABCD")]
    [InlineData(SaleSortField.SaleNumber, true, "DCBA")]
    [InlineData(SaleSortField.SaleDate, false, "BDAC")]
    [InlineData(SaleSortField.SaleDate, true, "CADB")]
    [InlineData(SaleSortField.CustomerName, false, "BCAD")]
    [InlineData(SaleSortField.CustomerName, true, "DACB")]
    [InlineData(SaleSortField.BranchName, false, "CABD")]
    [InlineData(SaleSortField.BranchName, true, "DBAC")]
    [InlineData(SaleSortField.TotalAmount, false, "ACDB")]
    [InlineData(SaleSortField.TotalAmount, true, "BDCA")]
    public async Task Given_FourSales_When_OrderingByOneField_Then_TheyComeInThatOrder(SaleSortField field, bool descending, string expected)
    {
        // When
        var page = await ListAsync(1, 10, new SaleSort(field, descending));

        // Then
        Letters(page).Should().Be(expected);
    }

    /// <summary>
    /// Tests <c>isCancelled</c> in both directions with open and cancelled sales, each group ordered by the second field.
    /// </summary>
    [Theory(DisplayName = "Given open and cancelled sales When ordering by isCancelled then totalAmount Then each group follows its total")]
    [InlineData(false, true, "BCDA")]
    [InlineData(true, false, "ADCB")]
    public async Task Given_OpenAndCancelledSales_When_OrderingByIsCancelledThenTotal_Then_EachGroupFollowsItsTotal(
        bool cancelledDescending, bool totalDescending, string expected)
    {
        // When
        var page = await ListAsync(1, 10,
            new SaleSort(SaleSortField.IsCancelled, cancelledDescending),
            new SaleSort(SaleSortField.TotalAmount, totalDescending));

        // Then
        Letters(page).Should().Be(expected);
    }

    /// <summary>
    /// Tests the tie-breaker: sales with equal keys follow their id, so reading one sale per page returns each once.
    /// </summary>
    [Theory(DisplayName = "Given equal isCancelled keys When reading one sale per page Then the id breaks the ties across pages")]
    [InlineData(false, "BC", "AD")]
    [InlineData(true, "AD", "BC")]
    public async Task Given_EqualKeys_When_ReadingOneSalePerPage_Then_IdBreaksTiesAcrossPages(
        bool descending, string firstGroup, string secondGroup)
    {
        // Given
        var expected = InIdOrder(firstGroup).Concat(InIdOrder(secondGroup));

        // When
        var read = new List<Guid>();
        for (var number = 1; number <= 4; number++)
            read.AddRange((await ListAsync(number, 1, new SaleSort(SaleSortField.IsCancelled, descending))).Sales.Select(sale => sale.Id));

        // Then
        read.Should().Equal(expected);
    }

    /// <summary>
    /// Tests that with no sort the id alone orders the sales.
    /// </summary>
    [Fact(DisplayName = "Given the four sales When listing with no sort Then they come in id order")]
    public async Task Given_FourSales_When_ListingWithNoSort_Then_TheyComeInIdOrder()
    {
        // When
        var page = await ListAsync(1, 10);

        // Then
        page.Sales.Select(sale => sale.Id).Should().Equal(InIdOrder("ABCD"));
    }

    /// <summary>
    /// Tests paging: each page holds its slice, a page past the end is empty, and the total is always every sale.
    /// </summary>
    [Theory(DisplayName = "Given the four sales When reading a page by sale number Then it holds its slice and the total is 4")]
    [InlineData(1, 3, "ABC")]
    [InlineData(2, 3, "D")]
    [InlineData(3, 3, "")]
    [InlineData(int.MaxValue, 100, "")]
    public async Task Given_FourSales_When_ReadingPage_Then_ItHoldsItsSliceAndTotalIsFour(int number, int size, string expected)
    {
        // When
        var page = await ListAsync(number, size, new SaleSort(SaleSortField.SaleNumber, Descending: false));

        // Then
        Letters(page).Should().Be(expected);
        page.TotalCount.Should().Be(4);
    }

    /// <summary>
    /// Tests that each listed sale comes with its items and the values it was saved with.
    /// Timestamps are left out: ticket 05's round-trip test covers their precision.
    /// </summary>
    [Fact(DisplayName = "Given the four sales When listing them Then each comes with its items and saved values")]
    public async Task Given_FourSales_When_Listing_Then_EachComesWithItsItems()
    {
        // When
        var page = await ListAsync(1, 10, new SaleSort(SaleSortField.SaleNumber, Descending: false));

        // Then
        page.Sales.Should().BeEquivalentTo(_sales.Values, options => options
            .Excluding(sale => sale.DomainEvents)
            .Excluding(sale => sale.CreatedAt)
            .Excluding(sale => sale.UpdatedAt));
    }

    /// <summary>
    /// Tests the query shape: a count, the page with LIMIT and OFFSET, then the items in a separate query
    /// (split query), with nothing tracked.
    /// </summary>
    [Fact(DisplayName = "Given the four sales When reading page 2 of 2 Then it counts, reads the page, reads its items separately and tracks nothing")]
    public async Task Given_FourSales_When_ReadingPage_Then_UsesSplitQueryWithoutTracking()
    {
        // Given
        var recorder = new CommandRecorder();
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseNpgsql(_database.ConnectionString)
            .AddInterceptors(recorder)
            .Options;
        await using var context = new DefaultContext(options);

        // When
        var page = await new SaleRepository(context).ListAsync(
            new SaleListQuery(2, 2, [new SaleSort(SaleSortField.SaleNumber, Descending: false)]));

        // Then
        Letters(page).Should().Be("CD");
        recorder.CommandTexts.Should().HaveCount(3);
        recorder.CommandTexts[0].Should().Contain("count(*)");
        recorder.CommandTexts[1].Should().Contain("LIMIT").And.Contain("OFFSET").And.NotContain("\"SaleItems\"");
        recorder.CommandTexts[2].Should().Contain("\"SaleItems\"");
        context.ChangeTracker.Entries().Should().BeEmpty();
    }

    private static Sale NewSale(char letter, int day, string customerName, string branchName, decimal total, bool cancelled)
    {
        var sale = Sale.Create(
            $"S-{letter}",
            new DateTime(2026, 1, day, 12, 0, 0, DateTimeKind.Utc),
            new ExternalIdentity(Guid.NewGuid(), customerName),
            new ExternalIdentity(Guid.NewGuid(), branchName),
            [new SaleItemData(new ExternalIdentity(Guid.NewGuid(), $"Product {letter}"), 1, total)]);
        if (cancelled)
            sale.Cancel();
        return sale;
    }

    private static string Letters(SalePage page) => string.Concat(page.Sales.Select(sale => sale.SaleNumber[^1]));

    // PostgreSQL orders uuid values like their lower-case text.
    private IEnumerable<Guid> InIdOrder(string letters) =>
        letters.Select(letter => _sales[letter].Id).OrderBy(id => id.ToString(), StringComparer.Ordinal);

    private async Task<SalePage> ListAsync(int number, int size, params SaleSort[] sorts)
    {
        await using var context = _database.CreateContext();
        return await new SaleRepository(context).ListAsync(new SaleListQuery(number, size, sorts));
    }

    /// <summary>
    /// Records the text of every command EF Core sends, so a test can see the round trips.
    /// </summary>
    private sealed class CommandRecorder : DbCommandInterceptor
    {
        public List<string> CommandTexts { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            CommandTexts.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            CommandTexts.Add(command.CommandText);
            return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
```

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Integration 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected: `SaleRepositoryListTests.cs` errors only: `CS0246` for `SalePage` and `SaleListQuery`, `CS1061` for `ListAsync` on `SaleRepository`.

- [ ] **Step 3: Write the query and page types**

Create `src/Ambev.DeveloperEvaluation.Domain/Repositories/SaleListQuery.cs`:

```csharp
namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// Which page of sales <see cref="ISaleRepository.ListAsync"/> reads, and in what order.
/// </summary>
/// <param name="Page">The page number, from 1.</param>
/// <param name="Size">The number of sales per page, from 1.</param>
/// <param name="Sorts">The order, applied field by field. The id always breaks the remaining ties.</param>
public sealed record SaleListQuery(int Page, int Size, IReadOnlyList<SaleSort> Sorts);
```

Create `src/Ambev.DeveloperEvaluation.Domain/Repositories/SalePage.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// One page of sales, and how many sales there are across every page.
/// </summary>
/// <param name="Sales">The sales on the page, each with its items. Empty past the last page.</param>
/// <param name="TotalCount">How many sales there are in all.</param>
public sealed record SalePage(IReadOnlyList<Sale> Sales, int TotalCount);
```

- [ ] **Step 4: Declare `ListAsync`**

In `src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs`, replace

```csharp
    Task UpdateAsync(Sale sale, CancellationToken cancellationToken = default);
```

with

```csharp
    Task UpdateAsync(Sale sale, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads one page of sales with their items, untracked. The sales follow <see cref="SaleListQuery.Sorts"/>,
    /// then their id, so sales with equal keys always come in one order and pages never overlap.
    /// </summary>
    /// <param name="query">The page, its size and the order.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The page, and the number of sales in all.</returns>
    Task<SalePage> ListAsync(SaleListQuery query, CancellationToken cancellationToken = default);
```

- [ ] **Step 5: Implement it**

In `src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
```

with

```csharp
using System.Linq.Expressions;
using Ambev.DeveloperEvaluation.Domain.Entities;
```

Then replace

```csharp
            throw new InvalidOperationException("Only a sale loaded with GetByIdAsync can be updated");

        await _context.SaveChangesAsync(cancellationToken);
    }
```

with

```csharp
            throw new InvalidOperationException("Only a sale loaded with GetByIdAsync can be updated");

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SalePage> ListAsync(SaleListQuery query, CancellationToken cancellationToken = default)
    {
        var sales = _context.Sales.AsNoTracking();

        var totalCount = await sales.CountAsync(cancellationToken);

        // Past the last sale there is nothing to read. Computing in long also keeps a huge page from overflowing Skip.
        var skip = (long)(query.Page - 1) * query.Size;
        if (skip >= totalCount)
            return new SalePage([], totalCount);

        var page = await ApplyOrder(sales.Include(sale => sale.Items).AsSplitQuery(), query.Sorts)
            .Skip((int)skip)
            .Take(query.Size)
            .ToListAsync(cancellationToken);

        return new SalePage(page, totalCount);
    }

    /// <summary>
    /// Orders by each sort in turn, then by id. Each field maps to a fixed expression; nothing is looked up by name.
    /// </summary>
    private static IOrderedQueryable<Sale> ApplyOrder(IQueryable<Sale> sales, IReadOnlyList<SaleSort> sorts)
    {
        IOrderedQueryable<Sale>? ordered = null;
        foreach (var sort in sorts)
        {
            ordered = sort.Field switch
            {
                SaleSortField.SaleNumber => AppendOrdering(sales, ordered, sale => sale.SaleNumber, sort.Descending),
                SaleSortField.SaleDate => AppendOrdering(sales, ordered, sale => sale.SaleDate, sort.Descending),
                SaleSortField.CustomerName => AppendOrdering(sales, ordered, sale => sale.Customer.Name, sort.Descending),
                SaleSortField.BranchName => AppendOrdering(sales, ordered, sale => sale.Branch.Name, sort.Descending),
                SaleSortField.TotalAmount => AppendOrdering(sales, ordered, sale => sale.TotalAmount, sort.Descending),
                SaleSortField.IsCancelled => AppendOrdering(sales, ordered, sale => sale.IsCancelled, sort.Descending),
                _ => throw new ArgumentOutOfRangeException(nameof(sorts), sort.Field, "The sort field has no ordering expression.")
            };
        }

        return ordered is null ? sales.OrderBy(sale => sale.Id) : ordered.ThenBy(sale => sale.Id);
    }

    private static IOrderedQueryable<Sale> AppendOrdering<TKey>(
        IQueryable<Sale> sales, IOrderedQueryable<Sale>? ordered, Expression<Func<Sale, TKey>> key, bool descending) =>
        (ordered, descending) switch
        {
            (null, false) => sales.OrderBy(key),
            (null, true) => sales.OrderByDescending(key),
            ({ } current, false) => current.ThenBy(key),
            ({ } current, true) => current.ThenByDescending(key)
        };
```

- [ ] **Step 6: Verify (GREEN)**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build --filter "FullyQualifiedName~SaleRepositoryListTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at ' | tail -1
```

Expected: `0 Error(s)`, no `warning CS`; `Passed:    21`; Integration I0 + 21; `No changes have been made to the model since the last migration.`

- [ ] **Step 7: Prove the split-query test bites (mutation check)**

Remove `.AsSplitQuery()` from `ListAsync` with the Edit tool, then:

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Integration 2>&1 | grep -E 'Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build --filter "FullyQualifiedName~UsesSplitQueryWithoutTracking" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected: `Failed:     1`, with `Expected recorder.CommandTexts to contain 3 item(s), but found 2`. Restore `.AsSplitQuery()`, rebuild, rerun the filter: `Passed:     1`. `git diff --stat` must show the same changes as before the check.

- [ ] **Step 8: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Domain/Repositories/SaleListQuery.cs src/Ambev.DeveloperEvaluation.Domain/Repositories/SalePage.cs src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryListTests.cs
git commit -m "feat(sales): list a page of sales ordered in SQL" -m "ISaleRepository.ListAsync reads one page of sales with their items, untracked: a COUNT, then the ordered page with Skip and Take, then the items in a split query. Each whitelisted field maps to a fixed expression, and the id always breaks ties so pages are stable. A page past the end returns no sales without a second query."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `5 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 5: List sales in the Application layer

**Skill:** `dotnet-best-practices`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/ListSales/ListSalesHandlerTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesHandler.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/ListSales/ListSalesHandlerTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.ListSales;

/// <summary>
/// Contains unit tests for <see cref="ListSalesHandler"/>. The mapper is the real Sales profile.
/// </summary>
public sealed class ListSalesHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly ListSalesHandler _handler;
    private SaleListQuery? _sent;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListSalesHandlerTests"/> class. The repository records the query
    /// it gets and returns an empty page.
    /// </summary>
    public ListSalesHandlerTests()
    {
        var mapper = new MapperConfiguration(config => config.AddProfile<SaleProfile>()).CreateMapper();
        _handler = new ListSalesHandler(_saleRepository, mapper);
        _saleRepository.ListAsync(Arg.Do<SaleListQuery>(query => _sent = query), Arg.Any<CancellationToken>())
            .Returns(new SalePage([], 0));
    }

    /// <summary>
    /// Tests the defaults: page 1, size 10, <c>saleDate desc</c>.
    /// </summary>
    [Fact(DisplayName = "Given no paging or ordering When listing Then it asks the repository for page 1 of 10, newest first")]
    public async Task Given_NoParameters_When_Listing_Then_AsksForFirstPageOfTenNewestFirst()
    {
        // When
        await _handler.Handle(new ListSalesQuery(), CancellationToken.None);

        // Then
        _sent.Should().NotBeNull();
        _sent!.Page.Should().Be(1);
        _sent.Size.Should().Be(10);
        _sent.Sorts.Should().Equal(new SaleSort(SaleSortField.SaleDate, Descending: true));
    }

    /// <summary>
    /// Tests that the requested page, size and parsed order reach the repository.
    /// </summary>
    [Fact(DisplayName = "Given a page, a size and an order When listing Then it asks the repository for exactly those")]
    public async Task Given_PageSizeAndOrder_When_Listing_Then_AsksForExactlyThose()
    {
        // When
        await _handler.Handle(
            new ListSalesQuery { Page = 3, Size = 5, Order = "\"customerName, totalAmount desc\"" }, CancellationToken.None);

        // Then
        _sent.Should().NotBeNull();
        _sent!.Page.Should().Be(3);
        _sent.Size.Should().Be(5);
        _sent.Sorts.Should().Equal(
            new SaleSort(SaleSortField.CustomerName, Descending: false),
            new SaleSort(SaleSortField.TotalAmount, Descending: true));
    }

    /// <summary>
    /// Tests that the page comes back mapped, with the page, size and total count.
    /// </summary>
    [Fact(DisplayName = "Given a page of sales from the repository When listing Then it returns them mapped with the page, size and total")]
    public async Task Given_PageFromRepository_When_Listing_Then_ReturnsItMapped()
    {
        // Given (4 × 4.50 at 10% = 16.20)
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m));
        _saleRepository.ListAsync(Arg.Any<SaleListQuery>(), Arg.Any<CancellationToken>()).Returns(new SalePage([sale], 7));

        // When
        var result = await _handler.Handle(new ListSalesQuery { Page = 2, Size = 5 }, CancellationToken.None);

        // Then
        result.Page.Should().Be(2);
        result.Size.Should().Be(5);
        result.TotalCount.Should().Be(7);
        result.Sales.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            sale.Id,
            sale.SaleNumber,
            TotalAmount = 16.20m,
            Items = new[] { new { sale.Items.Single().Id, TotalAmount = 16.20m } }
        });
    }
}
```

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected: `ListSalesHandlerTests.cs` errors only: `CS0246` for `ListSalesHandler`.

- [ ] **Step 3: Write the handler**

Create `src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesHandler.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

/// <summary>
/// Handles <see cref="ListSalesQuery"/>: applies the defaults, parses the order and reads the page.
/// </summary>
public sealed class ListSalesHandler : IRequestHandler<ListSalesQuery, ListSalesResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListSalesHandler"/> class.
    /// </summary>
    /// <param name="saleRepository">The sale repository.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    public ListSalesHandler(ISaleRepository saleRepository, IMapper mapper)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// Reads the requested page of sales.
    /// </summary>
    /// <param name="query">The validated query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The page of sales, with the page, size and total count.</returns>
    public async Task<ListSalesResult> Handle(ListSalesQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page ?? ListSalesQuery.DefaultPage;
        var size = query.Size ?? ListSalesQuery.DefaultSize;

        var sales = await _saleRepository.ListAsync(
            new SaleListQuery(page, size, SaleOrderParser.Parse(query.Order)), cancellationToken);

        return new ListSalesResult(_mapper.Map<List<SaleResult>>(sales.Sales), page, size, sales.TotalCount);
    }
}
```

- [ ] **Step 4: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~Sales.ListSales" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)`; `Passed:    33`; Unit U0 + 33.

- [ ] **Step 5: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesHandler.cs tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/ListSales/ListSalesHandlerTests.cs
git commit -m "feat(sales): list sales through the application layer" -m "ListSalesHandler applies the defaults (page 1, size 10, saleDate desc), parses _order, reads the page through ISaleRepository.ListAsync and maps it to SaleResult with the page, size and total count."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `2 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 6: Rename the paged total to `totalItems`

**Skill:** `dotnet-best-practices`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Common/BaseControllerTests.cs`
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Common/PaginatedResponse.cs`
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Common/BaseController.cs`

- [ ] **Step 1: Change the expected body**

In `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Common/BaseControllerTests.cs`, replace

```csharp
"currentPage":2,"totalPages":3,"totalCount":5}""");
```

with

```csharp
"currentPage":2,"totalPages":3,"totalItems":5}""");
```

- [ ] **Step 2: Watch it fail (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~BaseControllerTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `Build succeeded.`; `Failed:     1, Passed:     3`: the body still ends in `"totalCount":5}`.

- [ ] **Step 3: Rename the property**

In `src/Ambev.DeveloperEvaluation.WebApi/Common/PaginatedResponse.cs`, replace

```csharp
    public int TotalCount { get; set; }
```

with

```csharp
    public int TotalItems { get; set; }
```

In `src/Ambev.DeveloperEvaluation.WebApi/Common/BaseController.cs`, replace

```csharp
    /// Returns 200 with the paged body: <c>{success, message, data}</c> plus <c>currentPage</c>, <c>totalPages</c> and <c>totalCount</c>.
```

with

```csharp
    /// Returns 200 with the paged body: <c>{success, message, data}</c> plus <c>currentPage</c>, <c>totalPages</c> and <c>totalItems</c>.
```

and replace

```csharp
            TotalCount = pagedList.TotalCount
```

with

```csharp
            TotalItems = pagedList.TotalCount
```

- [ ] **Step 4: Verify (GREEN)**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
git grep -n 'TotalCount' -- src/Ambev.DeveloperEvaluation.WebApi
```

Expected: `0 Error(s)`; Unit U0 + 33, no failure; `TotalCount` appears only in `PaginatedList.cs` and in `BaseController.cs` as `pagedList.TotalCount`.

- [ ] **Step 5: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.WebApi/Common/PaginatedResponse.cs src/Ambev.DeveloperEvaluation.WebApi/Common/BaseController.cs tests/Ambev.DeveloperEvaluation.Unit/WebApi/Common/BaseControllerTests.cs
git commit -m "fix(api): name the paged response total totalItems" -m "PaginatedResponse.TotalCount becomes TotalItems, so a paged body reads {success, message, data, currentPage, totalPages, totalItems} as spec 7.3 documents."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `3 files changed, 4 insertions(+), 4 deletions(-)` and `Scan complete: 0 issue(s) found`.

---

### Task 7: List sales over HTTP

**Skills:** `dotnet-best-practices`, `testcontainers-integration-tests`, `type-design-performance`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesMappingTests.cs`, `SalesControllerTests.cs`
- Modify: `tests/Ambev.DeveloperEvaluation.Functional/Sales/SaleJson.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Functional/TestData/SalePageResponseBody.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Functional/Sales/ListSalesTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/ListSales/ListSalesRequest.cs`, `ListSalesProfile.cs`
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs`

- [ ] **Step 1: Write the failing mapping test**

In `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesMappingTests.cs`, replace `using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;` with

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
```

replace `using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;` with

```csharp
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.ListSales;
```

replace `        config.AddProfile<CreateSaleProfile>();` with

```csharp
        config.AddProfile<CreateSaleProfile>();
        config.AddProfile<ListSalesProfile>();
```

and replace

```csharp
        // Then
        response.Should().BeEquivalentTo(result);
    }
```

with

```csharp
        // Then
        response.Should().BeEquivalentTo(result);
    }

    /// <summary>
    /// Tests that the list request becomes a query with the same paging and ordering, an omitted one staying null.
    /// </summary>
    [Fact(DisplayName = "Given a list-sales request When mapping it to ListSalesQuery Then page, size and order are copied and an omitted one stays null")]
    public void Given_ListSalesRequest_When_MappingToQuery_Then_ParametersAreCopied()
    {
        // Given
        var request = new ListSalesRequest { Page = 2, Order = "saleDate desc" };

        // When
        var query = _configuration.CreateMapper().Map<ListSalesQuery>(request);

        // Then
        query.Should().BeEquivalentTo(new { Page = 2, Size = (int?)null, Order = "saleDate desc" });
    }
```

- [ ] **Step 2: Write the failing controller test**

In `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesControllerTests.cs`, replace `using Ambev.DeveloperEvaluation.Application.Sales.GetSale;` with

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
```

replace `using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;` with

```csharp
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.ListSales;
```

replace `            config.AddProfile<CreateSaleProfile>();` with

```csharp
            config.AddProfile<CreateSaleProfile>();
            config.AddProfile<ListSalesProfile>();
```

and replace

```csharp
            $$"""{"success":true,"message":"Sale retrieved successfully","data":{{ExampleSaleJson}}}""");
    }
```

with

```csharp
            $$"""{"success":true,"message":"Sale retrieved successfully","data":{{ExampleSaleJson}}}""");
    }

    /// <summary>
    /// Tests that listing sends the query built from the request and returns 200 with the paged envelope, built once.
    /// </summary>
    [Fact(DisplayName = "Given paging and ordering parameters When listing sales Then it sends the query and returns 200 with the paged envelope")]
    public async Task Given_PagingAndOrdering_When_ListingSales_Then_Returns200WithPagedEnvelope()
    {
        // Given
        var request = new ListSalesRequest { Page = 2, Size = 1, Order = "\"saleDate desc\"" };
        ListSalesQuery? sent = null;
        _mediator.Send(Arg.Do<ListSalesQuery>(query => sent = query), Arg.Any<CancellationToken>())
            .Returns(new ListSalesResult([ExampleResult()], Page: 2, Size: 1, TotalCount: 3));

        // When
        var response = await _controller.ListSales(request, CancellationToken.None);

        // Then
        sent.Should().BeEquivalentTo(request);
        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        MvcJson.Serialize(ok.Value).Should().Be(
            $$"""{"success":true,"message":"Sales retrieved successfully","data":[{{ExampleSaleJson}}],"currentPage":2,"totalPages":3,"totalItems":3}""");
    }
```

- [ ] **Step 3: Build and watch the unit tests fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected: errors in `SalesMappingTests.cs` and `SalesControllerTests.cs`: `CS0234` for `ListSales` in `Ambev.DeveloperEvaluation.WebApi.Features.Sales`, `CS0246` for `ListSalesProfile` and `ListSalesRequest`, `CS1061` for `ListSales` on `SalesController`.

- [ ] **Step 4: Add the paged-envelope check and the page body**

In `tests/Ambev.DeveloperEvaluation.Functional/Sales/SaleJson.cs`, replace

```csharp
        var sale = root.GetProperty("data");
        sale.EnumerateObject().Select(property => property.Name).Should().Equal(SaleFields);
        sale.GetProperty("items").EnumerateArray().Should().NotBeEmpty()
            .And.AllSatisfy(item => item.EnumerateObject().Select(property => property.Name).Should().Equal(ItemFields));
    }
```

with

```csharp
        ShouldBeSale(root.GetProperty("data"));
    }

    /// <summary>
    /// Checks that the body is exactly <c>{success, message, data, currentPage, totalPages, totalItems}</c>, built once,
    /// and that every sale in <c>data</c> (at least one) and each of its lines have exactly the documented fields.
    /// </summary>
    /// <param name="response">A paged success response of the Sales API.</param>
    /// <param name="message">The expected success message.</param>
    /// <returns>A task that completes when the body has been checked.</returns>
    public static async Task ShouldBeSalePageEnvelopeAsync(HttpResponseMessage response, string message)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        root.EnumerateObject().Select(property => property.Name).Should()
            .Equal("success", "message", "data", "currentPage", "totalPages", "totalItems");
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        root.GetProperty("message").GetString().Should().Be(message);
        root.GetProperty("data").EnumerateArray().Should().NotBeEmpty().And.AllSatisfy(ShouldBeSale);
    }

    private static void ShouldBeSale(JsonElement sale)
    {
        sale.EnumerateObject().Select(property => property.Name).Should().Equal(SaleFields);
        sale.GetProperty("items").EnumerateArray().Should().NotBeEmpty()
            .And.AllSatisfy(item => item.EnumerateObject().Select(property => property.Name).Should().Equal(ItemFields));
    }
```

Create `tests/Ambev.DeveloperEvaluation.Functional/TestData/SalePageResponseBody.cs`:

```csharp
namespace Ambev.DeveloperEvaluation.Functional.TestData;

/// <summary>
/// The paged body of <c>GET /api/sales</c> (spec §7.3), read back for assertions. <c>SaleJson</c> checks
/// <c>success</c>, <c>message</c> and the field names.
/// </summary>
/// <param name="Data">The sales on the page.</param>
/// <param name="CurrentPage">The page number.</param>
/// <param name="TotalPages">The number of pages.</param>
/// <param name="TotalItems">The number of sales across every page.</param>
public sealed record SalePageResponseBody(
    IReadOnlyList<SaleResponseBody> Data,
    int CurrentPage,
    int TotalPages,
    int TotalItems);
```

- [ ] **Step 5: Write the failing functional tests**

Create `tests/Ambev.DeveloperEvaluation.Functional/Sales/ListSalesTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

/// <summary>
/// Contains functional tests for listing sales with <c>GET /api/sales</c>: paging and ordering. Each test starts
/// from empty tables, logs in and creates three sales with one line of quantity 1 (no discount), in this order:
/// <c>middle</c> (2026-01-02, 20.00), <c>newest</c> (2026-01-03, 10.00, then cancelled), <c>oldest</c> (2026-01-01, 30.00).
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class ListSalesTests : IAsyncLifetime
{
    private readonly ApiFixture _api;
    private readonly HttpClient _client;
    private SaleResponseBody _middle = null!;
    private SaleResponseBody _newest = null!;
    private SaleResponseBody _oldest = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListSalesTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public ListSalesTests(ApiFixture api)
    {
        _api = api;
        _client = api.CreateClient();
    }

    /// <summary>
    /// Empties the tables, then logs in and creates the three sales, so the totals are exact.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _api.ResetDataAsync(DataResetFixture.Tables, DataResetFixture.Sequences);
        await _client.LogInAsNewUserAsync();

        _middle = await CreateSaleAsync(day: 2, total: 20.00m);
        var newest = await CreateSaleAsync(day: 3, total: 10.00m);
        _newest = await _client.CancelSaleAsync(newest.Id);
        _oldest = await CreateSaleAsync(day: 1, total: 30.00m);
    }

    /// <inheritdoc />
    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Tests the defaults: page 1 of 10 ordered by <c>saleDate desc</c>, the paged envelope built once, and each sale with its items.
    /// </summary>
    [Fact(DisplayName = "Given three sales When listing without parameters Then returns 200 with page 1, newest first, each sale with its items")]
    public async Task Given_ThreeSales_When_ListingWithoutParameters_Then_ReturnsNewestFirstWithItems()
    {
        // When
        using var response = await _client.GetAsync("/api/sales");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await SaleJson.ShouldBeSalePageEnvelopeAsync(response, "Sales retrieved successfully");
        var page = await ReadPageAsync(response);
        new { page.CurrentPage, page.TotalPages, page.TotalItems }.Should().Be(new { CurrentPage = 1, TotalPages = 1, TotalItems = 3 });
        page.Data.Select(sale => sale.Id).Should().Equal(_newest.Id, _middle.Id, _oldest.Id);
        page.Data.Should().BeEquivalentTo(new[] { _newest, _middle, _oldest }, options => options
            .ComparingRecordsByMembers()
            .Excluding(sale => sale.CreatedAt)
            .Excluding(sale => sale.UpdatedAt));
    }

    /// <summary>
    /// Tests a quoted, multi-field order in mixed case over HTTP.
    /// </summary>
    [Fact(DisplayName = "Given open and cancelled sales When ordering by isCancelled asc then totalAmount DESC Then open sales come first, highest total first")]
    public async Task Given_OpenAndCancelledSales_When_OrderingByTwoFields_Then_OpenSalesFirstHighestTotalFirst()
    {
        // When
        using var response = await _client.GetAsync(
            $"/api/sales?_order={Uri.EscapeDataString("\"isCancelled asc, totalAmount DESC\"")}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await ReadPageAsync(response);
        page.Data.Select(sale => sale.Id).Should().Equal(_oldest.Id, _middle.Id, _newest.Id);
    }

    /// <summary>
    /// Tests that an order by a field that can't be sorted is a 400 with the documented body.
    /// </summary>
    [Fact(DisplayName = "Given an order by a field that can't be sorted When listing Then returns 400 ValidationError for _order")]
    public async Task Given_UnsortableField_When_Listing_Then_Returns400ValidationError()
    {
        // When
        using var response = await _client.GetAsync("/api/sales?_order=price%20desc");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            """{"type":"ValidationError","error":"Invalid input data","detail":"_order: 'price' is not a sortable field. Use saleNumber, saleDate, customerName, branchName, totalAmount or isCancelled."}""");
    }

    /// <summary>
    /// Tests that a page size above 100 is a 400 with the documented body.
    /// </summary>
    [Fact(DisplayName = "Given _size=101 When listing Then returns 400 ValidationError for _size")]
    public async Task Given_SizeAbove100_When_Listing_Then_Returns400ValidationError()
    {
        // When
        using var response = await _client.GetAsync("/api/sales?_size=101");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            """{"type":"ValidationError","error":"Invalid input data","detail":"_size: '_size' must be between 1 and 100."}""");
    }

    /// <summary>
    /// Tests the paging metadata of a last, partial page.
    /// </summary>
    [Fact(DisplayName = "Given three sales When listing page 2 of size 2 Then returns the oldest sale with currentPage 2, totalPages 2 and totalItems 3")]
    public async Task Given_ThreeSales_When_ListingSecondPageOfTwo_Then_ReturnsOldestWithPagingMetadata()
    {
        // When
        using var response = await _client.GetAsync("/api/sales?_page=2&_size=2");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await ReadPageAsync(response);
        page.Data.Select(sale => sale.Id).Should().Equal(_oldest.Id);
        new { page.CurrentPage, page.TotalPages, page.TotalItems }.Should().Be(new { CurrentPage = 2, TotalPages = 2, TotalItems = 3 });
    }

    /// <summary>
    /// Tests that a page past the end is empty and still reports the totals.
    /// </summary>
    [Fact(DisplayName = "Given three sales When listing a page past the end Then returns 200 with no sales and the same totals")]
    public async Task Given_ThreeSales_When_ListingPagePastTheEnd_Then_ReturnsNoSalesWithTotals()
    {
        // When
        using var response = await _client.GetAsync("/api/sales?_page=3&_size=2");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await ReadPageAsync(response);
        page.Data.Should().BeEmpty();
        new { page.CurrentPage, page.TotalPages, page.TotalItems }.Should().Be(new { CurrentPage = 3, TotalPages = 2, TotalItems = 3 });
    }

    private static async Task<SalePageResponseBody> ReadPageAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<SalePageResponseBody>())!;

    private Task<SaleResponseBody> CreateSaleAsync(int day, decimal total) =>
        _client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid(SaleRequestBodyTestData.GenerateItem(1, total)) with
        {
            SaleDate = new DateTime(2026, 1, day, 12, 0, 0, DateTimeKind.Utc)
        });
}
```

- [ ] **Step 6: Watch the functional tests fail (RED)**

The Unit project doesn't compile yet, so build only the Functional project:

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Functional 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~ListSalesTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected: the build succeeds; `Failed:     6, Passed:     0`. Each status check finds 405 Method Not Allowed (Decision 8).

- [ ] **Step 7: Write the request and its profile**

Create `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/ListSales/ListSalesRequest.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.ListSales;

/// <summary>
/// The query string of <c>GET /api/sales</c> (<c>.doc/general-api.md</c>). Every parameter is optional: an omitted one
/// is <c>null</c> and takes its default in the Application layer.
/// </summary>
public sealed class ListSalesRequest
{
    /// <summary>
    /// Gets or sets the page number, <c>_page</c>: at least 1, default 1.
    /// </summary>
    [FromQuery(Name = "_page")]
    public int? Page { get; set; }

    /// <summary>
    /// Gets or sets the page size, <c>_size</c>: 1 to 100, default 10.
    /// </summary>
    [FromQuery(Name = "_size")]
    public int? Size { get; set; }

    /// <summary>
    /// Gets or sets the order, <c>_order</c>, such as <c>"saleDate desc, totalAmount"</c>: default <c>saleDate desc</c>.
    /// </summary>
    [FromQuery(Name = "_order")]
    public string? Order { get; set; }
}
```

Create `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/ListSales/ListSalesProfile.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.ListSales;

/// <summary>
/// Maps the list request to its query. The sales use the map in <see cref="SaleContractProfile"/>.
/// </summary>
public sealed class ListSalesProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListSalesProfile"/> class with the request map.
    /// </summary>
    public ListSalesProfile()
    {
        CreateMap<ListSalesRequest, ListSalesQuery>();
    }
}
```

- [ ] **Step 8: Add the action**

In `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs`, replace `using Ambev.DeveloperEvaluation.Application.Sales.GetSale;` with

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
```

replace `using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;` with

```csharp
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.ListSales;
```

and replace

```csharp
        return Created(nameof(GetSale), new { id = result.Id }, _mapper.Map<SaleResponse>(result), "Sale created successfully");
    }
```

with

```csharp
        return Created(nameof(GetSale), new { id = result.Id }, _mapper.Map<SaleResponse>(result), "Sale created successfully");
    }

    /// <summary>
    /// Lists sales a page at a time, each with its items: <c>_page</c> (default 1), <c>_size</c> (default 10, at most 100)
    /// and <c>_order</c> (default <c>saleDate desc</c>), as <c>.doc/general-api.md</c> describes.
    /// </summary>
    /// <param name="request">The paging and ordering parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 with the page of sales and the paging totals.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<SaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListSales([FromQuery] ListSalesRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(_mapper.Map<ListSalesQuery>(request), cancellationToken);
        var page = new PaginatedList<SaleResponse>(
            _mapper.Map<List<SaleResponse>>(result.Sales), result.TotalCount, result.Page, result.Size);

        return OkPaginated(page, "Sales retrieved successfully");
    }
```

- [ ] **Step 9: Verify (GREEN)**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected: `0 Error(s)`, no `warning CS`; Unit U0 + 35; Functional F0 + 6.

- [ ] **Step 10: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/ListSales/ListSalesRequest.cs src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/ListSales/ListSalesProfile.cs src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesMappingTests.cs tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesControllerTests.cs tests/Ambev.DeveloperEvaluation.Functional/Sales/SaleJson.cs tests/Ambev.DeveloperEvaluation.Functional/TestData/SalePageResponseBody.cs tests/Ambev.DeveloperEvaluation.Functional/Sales/ListSalesTests.cs
git commit -m "feat(sales): list sales over HTTP with paging and ordering" -m "GET /api/sales takes optional _page, _size and _order and returns 200 with {success, message, data, currentPage, totalPages, totalItems} through OkPaginated, each sale with its items. Invalid paging or ordering is a 400 ValidationError. The functional tests reset the data first and cover the default order, a quoted multi-field order, a bad _order, _size=101, a partial last page and a page past the end."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `8 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 8: Add the list requests to the `.http` file and replay them against `docker compose up`

**Files:**
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http`

- [ ] **Step 1: Look at the file (RED)**

```bash
grep -c '_order' src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
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

### List sales: page 1 of 5, newest first, then the highest total (200 with currentPage, totalPages and totalItems)
# _order is "saleDate desc, totalAmount desc", URL-encoded.
GET {{baseUrl}}/api/sales?_page=1&_size=5&_order=%22saleDate%20desc%2C%20totalAmount%20desc%22
Authorization: Bearer {{token}}

### List sales by a field that can't be sorted (400 ValidationError)
GET {{baseUrl}}/api/sales?_order=price%20desc
Authorization: Bearer {{token}}
```

- [ ] **Step 3: Check the file (GREEN)**

```bash
git diff --stat | cat
grep -n '_order' src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
```

Expected: `1 file changed, 9 insertions(+)`; three lines mention `_order` (the comment and the two requests).

- [ ] **Step 4: Start the stack from nothing**

```bash
docker compose down -v --remove-orphans
docker compose up --build --wait --wait-timeout 300; echo "exit=$?"
```

Expected: `exit=0`. If a port is already allocated, stop and ask the user.

- [ ] **Step 5: Replay with curl on a known data set**

Keep this in one Bash call.

```bash
BASE=http://localhost:8080
curl -s -o /dev/null -w "sign-up %{http_code}\n" -X POST $BASE/api/users -H 'Content-Type: application/json' --data-raw '{"username":"admin","password":"Admin@123","phone":"+5511999999999","email":"admin@developerstore.com","status":"Active","role":"Admin"}'
curl -s -o /tmp/ticket10-checks/login.json -X POST $BASE/api/auth -H 'Content-Type: application/json' --data-raw '{"email":"admin@developerstore.com","password":"Admin@123"}'
TOKEN=$(python3 -c "import json, sys; print(json.load(open(sys.argv[1]))['data']['token'])" /tmp/ticket10-checks/login.json)
for DATE in 2026-09-24T14:30:00Z 2026-09-25T10:00:00Z; do
  curl -s -o /tmp/ticket10-checks/create.json -w "create %{http_code}\n" -X POST $BASE/api/sales -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' --data-raw "{\"saleNumber\":\"S-$(( RANDOM % 900000 + 100000 ))\",\"saleDate\":\"$DATE\",\"customerId\":\"3f2b8c1e-1111-4a5b-9c2d-000000000001\",\"customerName\":\"Maria Silva\",\"branchId\":\"3f2b8c1e-2222-4a5b-9c2d-000000000002\",\"branchName\":\"Filial Centro\",\"items\":[{\"productId\":\"3f2b8c1e-3333-4a5b-9c2d-000000000003\",\"productName\":\"Cerveja 350ml\",\"quantity\":5,\"unitPrice\":4.50}]}"
done
LATEST=$(python3 -c "import json, sys; print(json.load(open(sys.argv[1]))['data']['id'])" /tmp/ticket10-checks/create.json)
curl -s -o /dev/null -w "cancel %{http_code}\n" -X PATCH "$BASE/api/sales/$LATEST/cancel" -H "Authorization: Bearer $TOKEN"
curl -s -o /tmp/ticket10-checks/list.json -w "list %{http_code} " "$BASE/api/sales?_page=1&_size=5&_order=%22saleDate%20desc%2C%20totalAmount%20desc%22" -H "Authorization: Bearer $TOKEN"
python3 -c "import json, sys; b = json.load(open(sys.argv[1])); print(list(b), 'latest first', b['data'][0]['id'] == sys.argv[2], 'cancelled', b['data'][0]['isCancelled'], 'items', [len(s['items']) for s in b['data']])" /tmp/ticket10-checks/list.json "$LATEST"
curl -s -w " %{http_code}\n" "$BASE/api/sales?_page=9&_size=5" -H "Authorization: Bearer $TOKEN"
curl -s -w " %{http_code}\n" "$BASE/api/sales?_order=price%20desc" -H "Authorization: Bearer $TOKEN"
curl -s -w " %{http_code}\n" "$BASE/api/sales?_size=101" -H "Authorization: Bearer $TOKEN"
```

Expected:

```
sign-up 201
create 201
create 201
cancel 200
list 200 ['success', 'message', 'data', 'currentPage', 'totalPages', 'totalItems'] latest first True cancelled True items [1, 1]
{"success":true,"message":"Sales retrieved successfully","data":[],"currentPage":9,"totalPages":1,"totalItems":2} 200
{"type":"ValidationError","error":"Invalid input data","detail":"_order: 'price' is not a sortable field. Use saleNumber, saleDate, customerName, branchName, totalAmount or isCancelled."} 400
{"type":"ValidationError","error":"Invalid input data","detail":"_size: '_size' must be between 1 and 100."} 400
```

- [ ] **Step 6: Take the stack down**

```bash
docker compose down -v
docker compose ps -a
```

Expected: `ps -a` lists nothing.

- [ ] **Step 7: Commit**

```bash
git add src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
git commit -m "docs(http): list sales with paging and ordering" -m "The .http file lists sales one page of 5 at a time, newest first then by total, and shows the 400 for an order by a field that can't be sorted."
git show --stat --format= HEAD | tail -1
```

Expected: `1 file changed, 9 insertions(+)`. No slopwatch run: no C# changed.

---

### Task 9: Verify everything, review, then open a pull request into `develop`

**Skills:** superpowers:verification-before-completion, `dotnet-slopwatch`, `test-anti-patterns` (report only), `clean-code` and superpowers:requesting-code-review (optional), superpowers:finishing-a-development-branch.

**Files:** none, unless Step 2 finds a real problem.

- [ ] **Step 1: Re-run every check fresh**

```bash
git status --short
dotnet build Ambev.DeveloperEvaluation.sln --no-incremental 2>&1 | grep -E 'warning CS|Warning\(s\)|Error\(s\)|Build succeeded' | sort -u
dotnet test Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-110
cat /tmp/ticket10-checks/baseline.txt
slopwatch analyze --fail-on warning 2>&1 | tail -1
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at ' | tail -1
git grep -n -E 'GetProperty\(|EF\.Property|nameof\(Sale\.' -- src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs || echo "no lookup by name"
```

Expected: only untracked tickets and plans; `0 Error(s)`, `2 Warning(s)`, `Build succeeded.`, no `warning CS`; Unit U0 + 35, Integration I0 + 21, Functional F0 + 6 against the baseline; `Scan complete: 0 issue(s) found`; `No changes have been made to the model since the last migration.`; `no lookup by name`.

| Ticket criterion | Evidence |
|---|---|
| `_page` default 1, ≥ 1; `_size` default 10, 1–100; else 400 | `ListSalesQueryValidatorTests`, `ListSalesHandlerTests`, `ListSalesTests` (`_size=101`) |
| `_order` parser in Application: quotes, case, default asc, whitelist, the four rejections | `SaleOrderParserTests`, `ListSalesTests` (bad `_order`) |
| Default `saleDate desc`; `id` final tie-breaker | `ListSalesHandlerTests`, `ListSalesTests` (default order), `SaleRepositoryListTests` (tie and no-sort tests) |
| `ListAsync`: no tracking, split query, `COUNT` + `Skip`/`Take`, expressions without reflection | `SaleRepositoryListTests` (query-shape test and Task 4 Step 7), the grep above |
| Body `{success, message, data, currentPage, totalPages, totalItems}`; past the end empty with totals | `BaseControllerTests`, `SalesControllerTests`, `ListSalesTests`, Task 8 Step 5 |
| Nullable contract; `OkPaginated` builds the body once | `ListSalesRequest`, `SalesMappingTests`, `SaleJson.ShouldBeSalePageEnvelopeAsync` |
| Tests reset the data first | `InitializeAsync` of `SaleRepositoryListTests` and `ListSalesTests` |
| Integration: every field both directions, `isCancelled` open and cancelled, ties, paging and totals | `SaleRepositoryListTests` |
| Functional: default order, multi-field, bad `_order`, `_size=101`, paging metadata | `ListSalesTests` |
| `.http` list request with paging and ordering | Task 8 |
| `feature/list-sales`, pull request into `develop`, Conventional Commits, no attribution | Steps 4–6 |

- [ ] **Step 2: Audit the tests (skill `test-anti-patterns`, report only)**

Run it on `SaleOrderParserTests`, `ListSalesQueryValidatorTests`, `ListSalesHandlerTests`, `SaleRepositoryListTests`, `ListSalesTests`, the new tests in `SalesMappingTests` and `SalesControllerTests`, and `SaleJson.ShouldBeSalePageEnvelopeAsync`.

Deliberate, report and leave:
- The page loop in the tie test and the seeding loops in `InitializeAsync`: the loop is the behaviour (every page once).
- `null!` fields in `ListSalesTests`: set in `InitializeAsync`, which xUnit runs before each test.
- Excluded timestamps: ticket 05 owns their round-trip precision.
- `CommandRecorder` is an EF Core interceptor on a real database, not a mock.

Fix a real finding in a separate `test(sales): …` commit, then re-run Step 1.

- [ ] **Step 3: Optional code review**

superpowers:requesting-code-review on `develop..feature/list-sales`, with `clean-code`, `dotnet-best-practices` and `efcore-patterns` as lenses. Findings that belong to tickets 11–13 go into the report, not this branch.

- [ ] **Step 4: Check the commit messages**

```bash
git log --format=%s develop..feature/list-sales
git log --format=%B develop..feature/list-sales | grep -n -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
```

Expected (plus any `test(sales):` commit from Step 2):

```
docs(http): list sales with paging and ordering
feat(sales): list sales over HTTP with paging and ordering
fix(api): name the paged response total totalItems
feat(sales): list sales through the application layer
feat(sales): list a page of sales ordered in SQL
feat(sales): validate the paging and ordering of the sales list
feat(sales): parse the _order parameter of the sales list
no attribution lines
```

- [ ] **Step 5: Push the branch and open a pull request into `develop`**

Write the body with the Write tool to `/tmp/ticket10-checks/pr-body.md`: the Goal in two sentences, the evidence table with this run's results, and what Steps 2 and 3 reported. No attribution lines. Then:

```bash
git push -u origin feature/list-sales
gh pr create --base develop --head feature/list-sales --title "Ticket 10: list sales with paging and ordering" --body-file /tmp/ticket10-checks/pr-body.md
```

Expected: the branch is pushed and `gh pr create` prints the URL. If `gh` fails (check `gh auth status`), stop and tell the user the branch is pushed.

- [ ] **Step 6: Verify the pull request**

```bash
gh pr view feature/list-sales --json state,baseRefName,commits --jq '"\(.state) \(.baseRefName) \(.commits | length) commits"'
gh pr view feature/list-sales --json title,body --jq '.title + "\n" + .body' | grep -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
git status -sb | head -1
```

Expected: `OPEN develop 7 commits` (8 with a `test(sales):` commit); `no attribution lines`; `## feature/list-sales...origin/feature/list-sales` with no `ahead` or `behind`. Don't merge it; keep the branch.

- [ ] **Step 7: Clean up**

```bash
docker compose ps -a
tasklist | grep -i 'Ambev.DeveloperEvaluation.WebApi' || echo "no API process left"
rm -rf /tmp/ticket10-checks
```

Expected: nothing listed, `no API process left`.

- [ ] **Step 8: Report to the user**

1. Step 1 results, and any output that differed from this plan (with what systematic-debugging found), or say none did.
2. The pull request URL. Ask the user to send the two new list requests once from their editor's `.http` runner.
3. For ticket 11:
   - add the filter to `SaleListQuery`, and the filter parameters to `ListSalesRequest` and `ListSalesQuery`, with `[FromQuery(Name = …)]` and nullable types
   - add the range rules to `ListSalesQueryValidator`
   - apply the filters to `sales` in `ListAsync` before `CountAsync`, so `totalItems` counts the filtered sales
4. For ticket 12: the `!IsDeleted` query filter hides deleted sales from `ListAsync` with no change to it.
