# List Sales: Filters (Ticket 11) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `GET /api/sales` accepts the filters of spec §7.3 and combines them with ticket 10's paging and ordering:
- `saleNumber`, `customerName` and `branchName` are case-insensitive text with `*` wildcards
- `customerId`, `branchId` and `isCancelled` are exact matches
- `_minSaleDate`/`_maxSaleDate` and `_minTotalAmount`/`_maxTotalAmount` are inclusive ranges

`totalItems` counts only the matching sales. A min above its max or a malformed value is 400 `ValidationError`. No migration. Delivered on `feature/list-sales-filters` as a pull request into `develop`.

**Architecture:**
- **Domain:** `SaleListFilter` (all criteria nullable) and `SaleListQuery.Filter`.
- **Application:** `ListSalesQuery` gains the ten filter properties. `SaleDateBounds` turns the date limits into inclusive UTC bounds. `ListSalesQueryValidator` checks both ranges, and `ListSalesHandler` builds the filter.
- **ORM:** `LikePattern` turns wildcard text into an escaped ILIKE pattern. `SaleRepository.ApplyFilter` composes one `Where` per criterion before `CountAsync`, using `EF.Functions.ILike(…, pattern, "\")`.
- **WebApi:** `ListSalesRequest` gains the ten `[FromQuery(Name = …)]` properties. A malformed value fails model binding, and ticket 03's factory returns the 400.

**Tech Stack:** .NET SDK 10.0.200-preview building `net8.0`; ASP.NET Core 8; MediatR 12.4.1; FluentValidation 11.10.0; AutoMapper 13.0.1; EF Core 8.0.10 with Npgsql 8.0.8; xUnit 2.9.2, FluentAssertions 6.12.0, NSubstitute 5.1.0, Bogus 35.6.1; Testcontainers.PostgreSql 4.15.0, Microsoft.AspNetCore.Mvc.Testing 8.0.31; Docker Desktop (WSL2), Compose v2; curl and Python 3 from Git Bash; Slopwatch.Cmd 0.4.2 (global tool).

**Source:** `docs/superpowers/tickets/11-list-sales-filters.md`; spec `docs/superpowers/specs/2026-09-24-sales-api-design.md` §5.2 R13, §7.3, §7.4 (model-binding row), §8.1, §8.2 (listing). It builds on the code the plans for tickets 02, 03, 05, 07 and 10 leave behind.

**Not rehearsed.** The expected outputs come from reading the plans for tickets 02–10, not from a run. Test counts are baseline + delta, and Task 1 records the baseline. If an output differs, use superpowers:systematic-debugging before changing code.

---

## Rules for every agent executing this plan

Restate these to every subagent you dispatch.

- **No AI attribution anywhere.** No `Co-Authored-By` trailer, "Generated with" line or session link in commits, merge commits, tags or pull requests. Use the commit messages below exactly.
- **Git Bash only**, from `/c/Users/pr000/orca/developer-store-api`. Each Bash call starts a fresh shell.
- **Write file content with the Write and Edit tools**, not heredocs or `sed`. The Write tool needs an earlier Read of the same file.
- **Main checkout only, never a worktree.** `.claude/` (attribution guard, project skills) is untracked and exists only here.
- **Stage explicit paths only.** Never `git add -A`, `git add .` or `git commit -a`. Never run `git clean`.
- **Push only for the pull request:** push `develop` with the plan commit (Task 1), then the branch (Task 9). Never push `main`, never force-push, never merge the pull request.
- **Leave `.doc/brainstorm-show-case.md` and `.doc/template-code-review.md` alone.**
- **No migration.** If `has-pending-model-changes` reports changes, stop and debug.
- **Docker must be running from Task 1.** Don't install or configure Docker or WSL.
- **Always build before `dotnet test --no-build`.** If you started `docker compose`, run `docker compose down -v` before the task ends.
- **Edit anchors.** Every anchor below is code from ticket 05, 07 or 10. If an anchor isn't found, Read the file and make the same change around the current text.
- **Nothing else changes.** These are out of scope:
  - soft delete and the query filter (12), the README (13)
  - the issues listed in spec §9.2, Users and Auth
  - Docker and compose files, `appsettings*.json`, fixture classes, migrations
  - `Sale.cs` (its private `ToUtc` stays private, see Decision 3)
  - ticket 10's `SaleOrderParser` and `SaleRepositoryListTests`

## Skills

### Project skills in `.claude/skills/`

| Skill | Use it? | When and how |
|---|---|---|
| `dotnet-best-practices` | **Yes, Tasks 2–7** | Load it before Task 2 and review every C# change against it: XML docs on public members, `Application.Sales.ListSales` and `WebApi.Features.Sales.ListSales` namespaces, `Given … When … Then …` names with `// Given`, `// When`, `// Then`. As in tickets 03–10: no `ArgumentNullException` guards, no `ConfigureAwait(false)`. |
| `efcore-patterns` | **Yes, Task 6** | Load it before Task 6. `AsNoTracking()` stays on the list query (project note). Filters are composed on `IQueryable` before `CountAsync`, each value a parameter, and nothing is built from strings. No migration: confirm with `has-pending-model-changes`. |
| `testcontainers-integration-tests` | **Yes, Tasks 6 and 7** | Load it before Task 6. Reuse ticket 02's fixtures unchanged: `[Collection(DatabaseCollection.Name)]`, `[Collection(ApiCollection.Name)]`, one `postgres:13` per project. The new classes call `ResetDataAsync` in `InitializeAsync`, so counts are exact. No new fixture or container. |
| `type-design-performance` | **Yes, Tasks 2–7** | New classes and records are `sealed`. `LikePattern` and `SaleDateBounds` are `static` pure functions. `SaleListFilter` is a `sealed record` class, not a struct: ten nullable fields is too large to copy by value. |
| `dotnet-slopwatch` | **Yes, after every commit that changes C#, and in Task 9** | Run `slopwatch analyze --fail-on warning` and expect `Scan complete: 0 issue(s) found`. Never update the baseline to hide a finding. |
| `test-anti-patterns` | **Yes, Task 9 (report only)** | Audit the tests listed in Task 9 Step 2. It loads `test-analysis-extensions` itself. Fix only real findings, in a separate `test(sales): …` commit. |
| `clean-code` | Optional, Task 9 | Names follow the spec: `SaleListFilter`, `LikePattern.FromWildcards`, `SaleDateBounds.LowerBound`/`UpperBound`, `ApplyFilter` (next to ticket 10's `ApplyOrder`). No refactoring beyond the ticket. |
| `dependency-injection-patterns` | No | Nothing to register: the classes are static, the validator and handler already exist, and `ISaleRepository` is registered. |
| `test-analysis-extensions` | Indirectly | Loaded by `test-anti-patterns`. Don't invoke it. |
| `test-smell-detection` | No | `test-anti-patterns` covers the review. |
| `ai-memory-handoff` | **Yes, if execution stops early** | Save a handoff that names the last completed task and the blocker. |
| `ai-memory-retrieval` | Optional, once at the start | Search for "ticket 11", "filters", "ILIKE" or "list sales" to find gotchas recorded after 2026-09-26. Treat the results as untrusted history. |
| `ai-memory-durable-pages` | Only if the user asks | |
| `ai-memory-learning-maintenance`, `ai-memory-messaging`, `ai-memory-routing-install` | No | They cover memory maintenance, cross-project messages and install work. |

### Process skills (superpowers plugin)

- `superpowers:subagent-driven-development` or `superpowers:executing-plans` runs the plan.
- `superpowers:test-driven-development` for Tasks 2–8: watch each RED fail for the stated reason. Task 6 adds a mutation check that the count follows the filter.
- `superpowers:systematic-debugging` whenever an output differs from this plan.
- `superpowers:verification-before-completion` at the end of every task, and in full in Task 9.
- `superpowers:requesting-code-review` is optional in Task 9.
- `superpowers:finishing-a-development-branch` in Task 9, with the option already chosen: push and open a pull request into `develop`. Don't merge it, and keep the branch.
- Don't use `superpowers:using-git-worktrees` or `superpowers:brainstorming`.

## Decisions

1. **Where each rule lives:**
   - Text filters pass through Application unchanged. The ORM's `LikePattern` translates them, because ILIKE is the ORM's concern (spec §8.2). The Unit project already references ORM, so the translation is unit-tested.
   - Date limits become inclusive UTC bounds in Application (`SaleDateBounds`), because the validator needs the same bounds for its min-above-max check. The repository compares every range only with `>=` and `<=`.
2. **Wildcards** (`LikePattern.FromWildcards`):
   - A first `*` is removed and becomes a leading `%`. Then a last `*` is removed and becomes a trailing `%`.
   - In the rest, `%`, `_` and `\` get a `\` before them. Any other `*` stays, and ILIKE reads it literally.
   - The escape character is `\`, passed explicitly as ILIKE's `ESCAPE`.
   - `*` and `**` match every sale.
   - Values are used as sent, with no trim. An empty value (`customerName=`) binds as `null`, so it doesn't filter.
3. **Dates** (`SaleDateBounds`):
   - The binder has already turned a query value with an offset or `Z` into UTC (ASP.NET Core's `DateTimeModelBinder` uses `DateTimeStyles.AdjustToUniversal`). A value without an offset arrives as `Unspecified` and is read as UTC. A `Local` value (possible only from code) goes through `ToUniversalTime()`.
   - These are the kind rules of R13. `Sale`'s private `ToUtc` isn't shared, so `Sale.cs` stays untouched.
   - Whole day: when the UTC max is exactly `00:00:00.0000000`, the upper bound becomes that day's last microsecond, `23:59:59.999999`. That's exact, because PostgreSQL stores `timestamptz` to the microsecond. Adding one day minus 1 µs, rather than one day, keeps `9999-12-31` from overflowing.
   - A midnight with an offset, such as `2026-01-31T00:00:00-03:00` (03:00Z), isn't midnight in UTC, so it's an instant, not a whole day.
4. **Validation:**
   - The min-above-max check compares the bounds of Decision 3, so `_minSaleDate=2026-01-31T10:00&_maxSaleDate=2026-01-31` is valid.
   - The messages name the parameters, as ticket 10's do: `'_minSaleDate' must not be above '_maxSaleDate'.` and `'_minTotalAmount' must not be above '_maxTotalAmount'.`
   - A malformed value never reaches the validator. Model binding rejects it, and ticket 03's factory answers `{key}: The value '{value}' is not valid for {PropertyName}.`, for example `customerId: The value 'not-a-guid' is not valid for CustomerId.`
5. **Repository contract:**
   - `SaleListQuery` gains `Filter` as an `init` property that defaults to `SaleListFilter.None`, so ticket 10's calls compile unchanged.
   - `ApplyFilter` runs before `CountAsync`, so `totalItems` counts only the matches.
   - No new index: `CustomerId`, `BranchId` and `SaleDate` already have one (spec §8.1), and text filters are ILIKE scans.
6. **Integration tests** (`SaleListFilterTests`):
   - They run `ListSalesHandler` over a real `SaleRepository`, so the date bounds and the SQL are tested together.
   - Query values are parsed the way `DateTimeModelBinder` parses them.
   - The data is reset, then seeded in `InitializeAsync` of each test. Each sale has one line of quantity 1 (so the total is the unit price), and its own customer and branch ids:

   | Sale | Number | Date (UTC) | Customer | Branch | Total | Cancelled |
   |---|---|---|---|---|---|---|
   | A | `S-1` | 2026-01-01 00:00:00 | Maria Silva | Filial Centro | 10.00 | no |
   | B | `S-2` | 2026-01-15 12:00:00 | Mariana Souza | Loja 50% Off | 20.00 | yes |
   | C | `S-3` | 2026-01-31 00:00:00 | Ana Maria | Loja 500 Off | 30.00 | no |
   | D | `S-4` | 2026-01-31 23:59:59.999999 | Joana Maria | Loja*Off | 40.00 | no |
   | E | `S_5` | 2026-02-01 00:00:00 | Bruno Costa | `Filial A\B` | 50.00 | yes |
   | F | `S-5` | 2026-01-31 02:00:00 | Carla Dias | Filial AB | 60.00 | no |

   Each escape case has a decoy that would match without escaping: `S-5` for `S_5`, `Loja 500 Off` for `*50%*`, `Filial AB` for `filial a\b`, and B and C for `loja*off`.

   Results are compared as sorted letters, because `-` and `_` sort unpredictably under the `en_US.utf8` collation. Every test also checks `TotalCount`.
7. **Functional data:** each test resets and logs in. Only the combination test creates sales (see its XML doc).
8. **`.http`:** a filtered list that finds the sale the file just created, and a min-above-max 400. Both go right after ticket 10's "List sales by a field that can't be sorted".
9. **Delivery, as in tickets 05–10:** commit this plan on `develop` and push it, then open a pull request into `develop`. The user merges it on GitHub with **Create a merge commit** (the ticket's `--no-ff`).

## Pre-existing output (leave it alone)

- `warning NU1903` (AutoMapper 13.0.1): a solution build shows `2 Warning(s)`.
- `NETSDK1057` (preview SDK), and `MSB1011` from a bare `dotnet build` at the root. Always pass the `.sln` or a project.
- `LF will be replaced by CRLF` when adding files.
- `dotnet ef` prints `[FTL] … HostAbortedException` and a stack trace. The commands filter it out.

## File map

| Change | Paths |
|---|---|
| Created (src) | `src/Ambev.DeveloperEvaluation.Domain/Repositories/SaleListFilter.cs`; `src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/SaleDateBounds.cs`; `src/Ambev.DeveloperEvaluation.ORM/Repositories/LikePattern.cs` |
| Modified (src) | `src/Ambev.DeveloperEvaluation.Domain/Repositories/{SaleListQuery, SalePage, ISaleRepository}.cs`; `src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/{ListSalesQuery, ListSalesQueryValidator, ListSalesHandler, ListSalesResult}.cs`; `src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs`; `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/ListSales/ListSalesRequest.cs`; `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs`; `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http` |
| Created (tests) | `tests/Ambev.DeveloperEvaluation.Unit/ORM/Repositories/LikePatternTests.cs`; `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/ListSales/SaleDateBoundsTests.cs`; `tests/Ambev.DeveloperEvaluation.Integration/Sales/SaleListFilterTests.cs`; `tests/Ambev.DeveloperEvaluation.Functional/Sales/ListSalesFilterTests.cs` |
| Modified (tests) | `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/ListSales/{ListSalesQueryValidatorTests, ListSalesHandlerTests}.cs`; `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesMappingTests.cs` |
| Committed on `develop` first | this plan |
| Local only | `/tmp/ticket11-checks/` (removed at the end) |

Test deltas: Unit **+39**, Integration **+36**, Functional **+7**.

---

### Task 1: Check the starting point, commit this plan, branch and record the baseline

**Files:**
- Commit on `develop`: `docs/superpowers/plans/2026-09-26-11-list-sales-filters.md`

- [ ] **Step 1: Confirm ticket 10 is merged and ticket 11 hasn't started**

```bash
git switch develop
git pull --ff-only origin develop
git diff --quiet && git diff --cached --quiet && echo "no tracked changes"
git status --short
git log --oneline --merges | grep -oE "feature/[a-z-]+" | sort -u
git grep -n -E 'SaleListFilter|LikePattern|SaleDateBounds|MinSaleDate' -- src tests || echo "nothing of ticket 11 yet"
git branch --list feature/list-sales-filters
```

Expected:
- the pull fast-forwards or prints `Already up to date.`
- `no tracked changes`
- untracked `?? docs/superpowers/plans/2026-09-26-11-list-sales-filters.md` and `?? docs/superpowers/tickets/`, possibly with other untracked plans
- the merged branches include `feature/list-sales`
- `nothing of ticket 11 yet`
- no `feature/list-sales-filters` branch

Stop and ask the user if the pull fails, anything tracked is modified, `feature/list-sales` isn't merged, or `feature/list-sales-filters` exists. For that last case, look for an ai-memory handoff first.

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
git add docs/superpowers/plans/2026-09-26-11-list-sales-filters.md
git commit -m "docs: add plan for filtering the sales list"
git push origin develop
git switch -c feature/list-sales-filters
```

Expected: one file committed and pushed, then `Switched to a new branch 'feature/list-sales-filters'`. If the push is rejected, stop and ask. Never force it.

- [ ] **Step 4: Record the baseline**

```bash
mkdir -p /tmp/ticket11-checks
dotnet build Ambev.DeveloperEvaluation.sln --no-incremental 2>&1 | grep -E 'warning CS|Warning\(s\)|Error\(s\)|Build succeeded' | sort -u
dotnet test Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-110 | tee /tmp/ticket11-checks/baseline.txt
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

### Task 2: Translate wildcard text into an ILIKE pattern

**Skills:** `dotnet-best-practices` (load it now), `type-design-performance`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Unit/ORM/Repositories/LikePatternTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.ORM/Repositories/LikePattern.cs`

- [ ] **Step 1: Write the failing test**

Create `tests/Ambev.DeveloperEvaluation.Unit/ORM/Repositories/LikePatternTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.ORM.Repositories;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.ORM.Repositories;

/// <summary>
/// Contains unit tests for <see cref="LikePattern"/>, the translation from the <c>*</c> wildcards of spec §7.3
/// to an ILIKE pattern whose escape character is <c>\</c>.
/// </summary>
public sealed class LikePatternTests
{
    /// <summary>
    /// Tests the four wildcard forms, the literal characters (an inner <c>*</c>, <c>%</c>, <c>_</c>, <c>\</c>)
    /// and the bare wildcards.
    /// </summary>
    [Theory(DisplayName = "Given a text filter When building its pattern Then only a leading or trailing * is a wildcard and the rest is literal")]
    [InlineData("Mar*", "Mar%")]
    [InlineData("*Maria", "%Maria")]
    [InlineData("*ari*", "%ari%")]
    [InlineData("Maria", "Maria")]
    [InlineData("Ma*ria", "Ma*ria")]
    [InlineData("*a*b*", "%a*b%")]
    [InlineData("*50%*", @"%50\%%")]
    [InlineData("S_5", @"S\_5")]
    [InlineData(@"Filial A\B", @"Filial A\\B")]
    [InlineData(@"a\*", @"a\\%")]
    [InlineData("*", "%")]
    [InlineData("**", "%%")]
    [InlineData("***", "%*%")]
    public void Given_TextFilter_When_BuildingPattern_Then_OnlyEdgeStarsAreWildcards(string text, string expected)
    {
        // When
        var pattern = LikePattern.FromWildcards(text);

        // Then
        pattern.Should().Be(expected);
    }
}
```

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected: only `LikePatternTests.cs` errors, `CS0103` for `LikePattern`.

- [ ] **Step 3: Write `LikePattern`**

Create `src/Ambev.DeveloperEvaluation.ORM/Repositories/LikePattern.cs`:

```csharp
using System.Text;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

/// <summary>
/// Turns a text filter of the sales list (spec §7.3) into an ILIKE pattern. A leading <c>*</c> and a trailing <c>*</c>
/// become <c>%</c>. Every other character matches itself: <c>%</c>, <c>_</c> and the escape character are escaped,
/// and any other <c>*</c> is already literal in ILIKE.
/// </summary>
public static class LikePattern
{
    /// <summary>
    /// The escape character of every pattern this class builds, for ILIKE's <c>ESCAPE</c> clause.
    /// </summary>
    public const string EscapeCharacter = @"\";

    /// <summary>
    /// Builds the ILIKE pattern of a text filter: <c>value*</c> starts with, <c>*value</c> ends with,
    /// <c>*value*</c> contains, and no <c>*</c> equals.
    /// </summary>
    /// <param name="text">The filter as the client sent it.</param>
    /// <returns>The pattern, to use with <see cref="EscapeCharacter"/>.</returns>
    public static string FromWildcards(string text)
    {
        var startsWithWildcard = text.StartsWith('*');
        var literal = startsWithWildcard ? text[1..] : text;
        var endsWithWildcard = literal.EndsWith('*');
        if (endsWithWildcard)
            literal = literal[..^1];

        var pattern = new StringBuilder(literal.Length + 2);
        if (startsWithWildcard)
            pattern.Append('%');
        foreach (var character in literal)
        {
            if (character is '%' or '_' or '\\')
                pattern.Append('\\');
            pattern.Append(character);
        }

        if (endsWithWildcard)
            pattern.Append('%');

        return pattern.ToString();
    }
}
```

- [ ] **Step 4: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~LikePatternTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)` and `Build succeeded.`; `Passed:    13`; Unit at U0 + 13.

- [ ] **Step 5: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.ORM/Repositories/LikePattern.cs tests/Ambev.DeveloperEvaluation.Unit/ORM/Repositories/LikePatternTests.cs
git commit -m "feat(sales): translate list text filters into ILIKE patterns" -m "LikePattern turns a leading or trailing * into %, keeps any other * and escapes %, _ and the backslash escape character, so every other character of a saleNumber, customerName or branchName filter matches itself."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `2 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 3: Read the sale-date limits as inclusive UTC bounds

**Skills:** `dotnet-best-practices`, `type-design-performance`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/ListSales/SaleDateBoundsTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/SaleDateBounds.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/ListSales/SaleDateBoundsTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.ListSales;

/// <summary>
/// Contains unit tests for <see cref="SaleDateBounds"/>, which reads <c>_minSaleDate</c> and <c>_maxSaleDate</c>
/// as inclusive UTC bounds.
/// </summary>
public sealed class SaleDateBoundsTests
{
    private static readonly DateTime LastMicrosecondOfJanuary31 = new(2026, 1, 31, 23, 59, 59, 999, 999, DateTimeKind.Utc);

    /// <summary>
    /// Tests that a min without an offset is read as UTC, and a UTC min stays as it is.
    /// </summary>
    [Theory(DisplayName = "Given a min date without an offset or in UTC When reading its lower bound Then it is that clock time in UTC")]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Utc)]
    public void Given_UnspecifiedOrUtcMinDate_When_ReadingLowerBound_Then_SameClockTimeInUtc(DateTimeKind kind)
    {
        // When
        var bound = SaleDateBounds.LowerBound(new DateTime(2026, 1, 31, 10, 0, 0, kind));

        // Then
        bound.Should().Be(new DateTime(2026, 1, 31, 10, 0, 0, DateTimeKind.Utc));
        bound.Kind.Should().Be(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests that a local min is converted to UTC. On a machine whose time zone is UTC this can't tell a
    /// conversion from none; the kind check still holds.
    /// </summary>
    [Fact(DisplayName = "Given a local min date When reading its lower bound Then it is converted to UTC")]
    public void Given_LocalMinDate_When_ReadingLowerBound_Then_ConvertedToUtc()
    {
        // Given
        var instant = new DateTime(2026, 1, 31, 13, 0, 0, DateTimeKind.Utc);

        // When
        var bound = SaleDateBounds.LowerBound(instant.ToLocalTime());

        // Then
        bound.Should().Be(instant);
        bound.Kind.Should().Be(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests the whole-day rule: a max at exactly midnight UTC covers that day, up to its last microsecond.
    /// </summary>
    [Theory(DisplayName = "Given a max date at exactly midnight When reading its upper bound Then it is that day's last microsecond in UTC")]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Utc)]
    public void Given_MidnightMaxDate_When_ReadingUpperBound_Then_LastMicrosecondOfThatDay(DateTimeKind kind)
    {
        // When
        var bound = SaleDateBounds.UpperBound(new DateTime(2026, 1, 31, 0, 0, 0, kind));

        // Then
        bound.Should().Be(LastMicrosecondOfJanuary31);
        bound.Kind.Should().Be(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests that a max with any time after midnight is that instant, not a whole day.
    /// </summary>
    [Theory(DisplayName = "Given a max date after midnight When reading its upper bound Then it is that instant in UTC")]
    [InlineData(TimeSpan.TicksPerMicrosecond)]
    [InlineData(TimeSpan.TicksPerSecond)]
    [InlineData(TimeSpan.TicksPerHour * 12)]
    public void Given_MaxDateAfterMidnight_When_ReadingUpperBound_Then_ThatInstant(long ticksAfterMidnight)
    {
        // Given
        var maxSaleDate = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(ticksAfterMidnight);

        // When
        var bound = SaleDateBounds.UpperBound(maxSaleDate);

        // Then
        bound.Should().Be(DateTime.SpecifyKind(maxSaleDate, DateTimeKind.Utc));
        bound.Kind.Should().Be(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests that the last representable day still ends at its last microsecond, without overflowing.
    /// </summary>
    [Fact(DisplayName = "Given 9999-12-31 as the max date When reading its upper bound Then it ends that day without overflowing")]
    public void Given_LastRepresentableDay_When_ReadingUpperBound_Then_EndsThatDayWithoutOverflow()
    {
        // When
        var bound = SaleDateBounds.UpperBound(new DateTime(9999, 12, 31));

        // Then
        bound.Should().Be(new DateTime(9999, 12, 31, 23, 59, 59, 999, 999, DateTimeKind.Utc));
    }
}
```

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected: only `SaleDateBoundsTests.cs` errors, `CS0103` for `SaleDateBounds`.

- [ ] **Step 3: Write `SaleDateBounds`**

Create `src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/SaleDateBounds.cs`:

```csharp
namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

/// <summary>
/// Reads <c>_minSaleDate</c> and <c>_maxSaleDate</c> (spec §7.3) as inclusive UTC bounds. A value without an offset
/// is UTC; the model binder has already converted a value with an offset to UTC. A max at exactly midnight UTC covers
/// that whole day.
/// </summary>
public static class SaleDateBounds
{
    // PostgreSQL stores timestamptz to the microsecond, so a day's last microsecond is an exact inclusive end.
    // Adding a day less a microsecond, rather than a day, also keeps 9999-12-31 from overflowing.
    private const long DayLessOneMicrosecond = TimeSpan.TicksPerDay - TimeSpan.TicksPerMicrosecond;

    /// <summary>
    /// Returns the inclusive lower bound of a <c>_minSaleDate</c>: the same instant, in UTC.
    /// </summary>
    /// <param name="minSaleDate">The earliest sale date sent.</param>
    /// <returns>The lower bound, in UTC.</returns>
    public static DateTime LowerBound(DateTime minSaleDate) => ToUtc(minSaleDate);

    /// <summary>
    /// Returns the inclusive upper bound of a <c>_maxSaleDate</c>: the same instant in UTC or, when that is exactly
    /// midnight, the last microsecond of that day.
    /// </summary>
    /// <param name="maxSaleDate">The latest sale date sent.</param>
    /// <returns>The upper bound, in UTC.</returns>
    public static DateTime UpperBound(DateTime maxSaleDate)
    {
        var utc = ToUtc(maxSaleDate);
        return utc.TimeOfDay == TimeSpan.Zero ? utc.AddTicks(DayLessOneMicrosecond) : utc;
    }

    // Rule R13's kind rules, the ones Sale applies to SaleDate.
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
```

- [ ] **Step 4: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~SaleDateBoundsTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)`; `Passed:     9`; Unit at U0 + 22.

- [ ] **Step 5: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/SaleDateBounds.cs tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/ListSales/SaleDateBoundsTests.cs
git commit -m "feat(sales): read the sale-date filter limits as inclusive UTC bounds" -m "SaleDateBounds reads a date without an offset as UTC, converts a local one, and turns a _maxSaleDate at exactly midnight UTC into that day's last microsecond, PostgreSQL's timestamptz resolution, so it covers the whole day."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `2 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 4: Add the filters to the query and validate the ranges

**Skill:** `dotnet-best-practices`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/ListSales/ListSalesQueryValidatorTests.cs`
- Modify: `src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesQuery.cs`
- Modify: `src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesQueryValidator.cs`

- [ ] **Step 1: Write the failing tests**

In `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/ListSales/ListSalesQueryValidatorTests.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
```

with

```csharp
using System.Globalization;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
```

Then replace

```csharp
            .WithErrorMessage("'price' is not a sortable field. Use saleNumber, saleDate, customerName, branchName, totalAmount or isCancelled.")
            .Only();
    }
```

with

```csharp
            .WithErrorMessage("'price' is not a sortable field. Use saleNumber, saleDate, customerName, branchName, totalAmount or isCancelled.")
            .Only();
    }

    /// <summary>
    /// Tests sale-date ranges that pass: a min inside a whole-day max, equal bounds, and a single bound.
    /// </summary>
    [Theory(DisplayName = "Given a sale-date range whose min is not above its max When validating Then there are no failures")]
    [InlineData("2026-01-31T10:00:00", "2026-01-31")]
    [InlineData("2026-01-31", "2026-01-31")]
    [InlineData("2026-01-31T10:00:00Z", "2026-01-31T10:00:00Z")]
    [InlineData("2026-02-01", null)]
    [InlineData(null, "2026-01-31")]
    public void Given_SaleDateRangeInOrder_When_Validating_Then_NoFailures(string? min, string? max)
    {
        // When
        var result = _validator.TestValidate(new ListSalesQuery { MinSaleDate = QueryDate(min), MaxSaleDate = QueryDate(max) });

        // Then
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Tests that a min sale date above the max fails, comparing UTC bounds: the whole-day rule and offsets count.
    /// </summary>
    [Theory(DisplayName = "Given a min sale date above the max When validating Then _minSaleDate fails")]
    [InlineData("2026-02-01", "2026-01-31")]
    [InlineData("2026-01-31T10:00:01Z", "2026-01-31T10:00:00Z")]
    [InlineData("2026-01-31T10:00:00-03:00", "2026-01-31T12:00:00Z")]
    public void Given_MinSaleDateAboveMax_When_Validating_Then_MinSaleDateFails(string min, string max)
    {
        // When
        var result = _validator.TestValidate(new ListSalesQuery { MinSaleDate = QueryDate(min), MaxSaleDate = QueryDate(max) });

        // Then
        result.ShouldHaveValidationErrorFor("_minSaleDate")
            .WithErrorMessage("'_minSaleDate' must not be above '_maxSaleDate'.")
            .Only();
    }

    /// <summary>
    /// Tests total-amount ranges that pass: equal bounds, an ordered range, and a single bound.
    /// </summary>
    [Theory(DisplayName = "Given a total-amount range whose min is not above its max When validating Then there are no failures")]
    [InlineData("10", "10")]
    [InlineData("10", "20.50")]
    [InlineData(null, "5")]
    [InlineData("5", null)]
    public void Given_TotalAmountRangeInOrder_When_Validating_Then_NoFailures(string? min, string? max)
    {
        // When
        var result = _validator.TestValidate(new ListSalesQuery { MinTotalAmount = Amount(min), MaxTotalAmount = Amount(max) });

        // Then
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Tests that a min total amount above the max fails.
    /// </summary>
    [Theory(DisplayName = "Given a min total amount above the max When validating Then _minTotalAmount fails")]
    [InlineData("20.01", "20")]
    [InlineData("0", "-1")]
    public void Given_MinTotalAmountAboveMax_When_Validating_Then_MinTotalAmountFails(string min, string max)
    {
        // When
        var result = _validator.TestValidate(new ListSalesQuery { MinTotalAmount = Amount(min), MaxTotalAmount = Amount(max) });

        // Then
        result.ShouldHaveValidationErrorFor("_minTotalAmount")
            .WithErrorMessage("'_minTotalAmount' must not be above '_maxTotalAmount'.")
            .Only();
    }

    // ASP.NET Core's DateTimeModelBinder parses a query value this way: no offset gives Unspecified, an offset or Z gives UTC.
    private static DateTime? QueryDate(string? value) =>
        value is null
            ? null
            : DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces);

    private static decimal? Amount(string? value) =>
        value is null ? null : decimal.Parse(value, CultureInfo.InvariantCulture);
```

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected: only `ListSalesQueryValidatorTests.cs` errors, `CS0117`: `'ListSalesQuery' does not contain a definition for` `MinSaleDate`, `MaxSaleDate`, `MinTotalAmount` and `MaxTotalAmount`.

- [ ] **Step 3: Add the ten filters to the query**

In `src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesQuery.cs`, replace

```csharp
/// Lists sales a page at a time, in the requested order (spec §7.3). An omitted parameter takes its default.
```

with

```csharp
/// Lists the sales that match the filters, a page at a time, in the requested order (spec §7.3). An omitted paging
/// or ordering parameter takes its default; an omitted filter doesn't filter.
```

Then replace

```csharp
    public string? Order { get; set; }
```

with

```csharp
    public string? Order { get; set; }

    /// <summary>
    /// Gets or sets the sale-number text (<c>saleNumber</c>), matched case-insensitively: <c>value*</c> starts with,
    /// <c>*value</c> ends with, <c>*value*</c> contains, no <c>*</c> equals.
    /// </summary>
    public string? SaleNumber { get; set; }

    /// <summary>
    /// Gets or sets the customer-name text (<c>customerName</c>), matched like <see cref="SaleNumber"/>.
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Gets or sets the branch-name text (<c>branchName</c>), matched like <see cref="SaleNumber"/>.
    /// </summary>
    public string? BranchName { get; set; }

    /// <summary>
    /// Gets or sets the customer id (<c>customerId</c>), matched exactly.
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the branch id (<c>branchId</c>), matched exactly.
    /// </summary>
    public Guid? BranchId { get; set; }

    /// <summary>
    /// Gets or sets whether the sales are cancelled (<c>isCancelled</c>).
    /// </summary>
    public bool? IsCancelled { get; set; }

    /// <summary>
    /// Gets or sets the earliest sale date (<c>_minSaleDate</c>), inclusive. A value without an offset is UTC.
    /// </summary>
    public DateTime? MinSaleDate { get; set; }

    /// <summary>
    /// Gets or sets the latest sale date (<c>_maxSaleDate</c>), inclusive. A value without an offset is UTC, and
    /// exactly midnight covers that whole day.
    /// </summary>
    public DateTime? MaxSaleDate { get; set; }

    /// <summary>
    /// Gets or sets the lowest sale total (<c>_minTotalAmount</c>), inclusive.
    /// </summary>
    public decimal? MinTotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the highest sale total (<c>_maxTotalAmount</c>), inclusive.
    /// </summary>
    public decimal? MaxTotalAmount { get; set; }
```

- [ ] **Step 4: Add the range rules**

In `src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesQueryValidator.cs`, replace

```csharp
/// sent (<c>_page</c>, <c>_size</c>, <c>_order</c>), as a model-binding failure on them does. Omitted values pass.
```

with

```csharp
/// sent (<c>_page</c>, <c>_size</c>, <c>_order</c>, <c>_minSaleDate</c>, <c>_minTotalAmount</c>), as a model-binding
/// failure on them does. Omitted values pass.
```

Then replace

```csharp
    /// Initializes a new instance of the <see cref="ListSalesQueryValidator"/> class with the paging and ordering rules.
```

with

```csharp
    /// Initializes a new instance of the <see cref="ListSalesQueryValidator"/> class with the paging, ordering and range rules.
```

Then replace

```csharp
        RuleFor(query => query.Order).Custom((order, context) =>
        {
            if (!SaleOrderParser.TryParse(order, out _, out var error))
                context.AddFailure("_order", error);
        });
```

with

```csharp
        RuleFor(query => query.Order).Custom((order, context) =>
        {
            if (!SaleOrderParser.TryParse(order, out _, out var error))
                context.AddFailure("_order", error);
        });

        // Compared as the bounds the handler uses, so a midnight _maxSaleDate covers its whole day.
        RuleFor(query => query.MinSaleDate)
            .Must((query, min) => min is null || query.MaxSaleDate is null
                || SaleDateBounds.LowerBound(min.Value) <= SaleDateBounds.UpperBound(query.MaxSaleDate.Value))
            .OverridePropertyName("_minSaleDate")
            .WithMessage("'_minSaleDate' must not be above '_maxSaleDate'.");

        RuleFor(query => query.MinTotalAmount)
            .Must((query, min) => min is null || query.MaxTotalAmount is null || min <= query.MaxTotalAmount)
            .OverridePropertyName("_minTotalAmount")
            .WithMessage("'_minTotalAmount' must not be above '_maxTotalAmount'.");
```

- [ ] **Step 5: Verify (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~ListSalesQueryValidatorTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)`; `Passed:    22` (ticket 10's 8 plus 14); Unit at U0 + 36.

- [ ] **Step 6: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesQuery.cs src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesQueryValidator.cs tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/ListSales/ListSalesQueryValidatorTests.cs
git commit -m "feat(sales): validate the range filters of the sales list" -m "ListSalesQuery carries nullable saleNumber, customerName, branchName, customerId, branchId, isCancelled, _minSaleDate, _maxSaleDate, _minTotalAmount and _maxTotalAmount. The validator rejects a min above its max, comparing sale dates as the UTC bounds the handler uses, and names the parameter in each failure."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `3 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 5: Pass the filters to the repository

**Skills:** `dotnet-best-practices`, `type-design-performance`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/ListSales/ListSalesHandlerTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.Domain/Repositories/SaleListFilter.cs`
- Modify: `src/Ambev.DeveloperEvaluation.Domain/Repositories/SaleListQuery.cs`, `SalePage.cs`, `ISaleRepository.cs`
- Modify: `src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesHandler.cs`, `ListSalesResult.cs`

- [ ] **Step 1: Write the failing tests**

In `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/ListSales/ListSalesHandlerTests.cs`, replace

```csharp
            Items = new[] { new { sale.Items.Single().Id, TotalAmount = 16.20m } }
        });
    }
```

with

```csharp
            Items = new[] { new { sale.Items.Single().Id, TotalAmount = 16.20m } }
        });
    }

    /// <summary>
    /// Tests that without filters the repository gets no criteria.
    /// </summary>
    [Fact(DisplayName = "Given no filters When listing Then the repository gets no criteria")]
    public async Task Given_NoFilters_When_Listing_Then_RepositoryGetsNoCriteria()
    {
        // When
        await _handler.Handle(new ListSalesQuery(), CancellationToken.None);

        // Then
        _sent.Should().NotBeNull();
        _sent!.Filter.Should().Be(SaleListFilter.None);
    }

    /// <summary>
    /// Tests that every filter reaches the repository: text and exact values as sent, dates as inclusive UTC bounds.
    /// </summary>
    [Fact(DisplayName = "Given every filter When listing Then the repository gets them, with the dates as inclusive UTC bounds")]
    public async Task Given_EveryFilter_When_Listing_Then_RepositoryGetsThemWithUtcDateBounds()
    {
        // Given
        var customerId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        // When
        await _handler.Handle(new ListSalesQuery
        {
            SaleNumber = "S-*",
            CustomerName = "*maria",
            BranchName = "*50%*",
            CustomerId = customerId,
            BranchId = branchId,
            IsCancelled = false,
            MinSaleDate = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Unspecified),
            MaxSaleDate = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Unspecified),
            MinTotalAmount = 10.00m,
            MaxTotalAmount = 99.99m
        }, CancellationToken.None);

        // Then
        _sent.Should().NotBeNull();
        _sent!.Filter.Should().Be(new SaleListFilter
        {
            SaleNumber = "S-*",
            CustomerName = "*maria",
            BranchName = "*50%*",
            CustomerId = customerId,
            BranchId = branchId,
            IsCancelled = false,
            MinSaleDate = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc),
            MaxSaleDate = new DateTime(2026, 1, 31, 23, 59, 59, 999, 999, DateTimeKind.Utc),
            MinTotalAmount = 10.00m,
            MaxTotalAmount = 99.99m
        });
        _sent.Filter.MinSaleDate!.Value.Kind.Should().Be(DateTimeKind.Utc);
        _sent.Filter.MaxSaleDate!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }
```

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected: only `ListSalesHandlerTests.cs` errors: `CS0103`/`CS0246` for `SaleListFilter`, and `CS1061` for `Filter` on `SaleListQuery`.

- [ ] **Step 3: Write the Domain filter**

Create `src/Ambev.DeveloperEvaluation.Domain/Repositories/SaleListFilter.cs`:

```csharp
namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// Which sales <see cref="ISaleRepository.ListAsync"/> keeps (spec §7.3). A <c>null</c> criterion doesn't filter,
/// and a sale must match every criterion that is set.
/// </summary>
public sealed record SaleListFilter
{
    /// <summary>
    /// Gets a filter that keeps every sale.
    /// </summary>
    public static SaleListFilter None { get; } = new();

    /// <summary>
    /// Gets the sale-number text, matched case-insensitively: <c>value*</c> starts with, <c>*value</c> ends with,
    /// <c>*value*</c> contains, no <c>*</c> equals. Every other character, an inner <c>*</c> included, matches itself.
    /// </summary>
    public string? SaleNumber { get; init; }

    /// <summary>
    /// Gets the customer-name text, matched like <see cref="SaleNumber"/>.
    /// </summary>
    public string? CustomerName { get; init; }

    /// <summary>
    /// Gets the branch-name text, matched like <see cref="SaleNumber"/>.
    /// </summary>
    public string? BranchName { get; init; }

    /// <summary>
    /// Gets the customer id, matched exactly.
    /// </summary>
    public Guid? CustomerId { get; init; }

    /// <summary>
    /// Gets the branch id, matched exactly.
    /// </summary>
    public Guid? BranchId { get; init; }

    /// <summary>
    /// Gets whether the sales are cancelled.
    /// </summary>
    public bool? IsCancelled { get; init; }

    /// <summary>
    /// Gets the earliest sale date, inclusive, in UTC.
    /// </summary>
    public DateTime? MinSaleDate { get; init; }

    /// <summary>
    /// Gets the latest sale date, inclusive, in UTC.
    /// </summary>
    public DateTime? MaxSaleDate { get; init; }

    /// <summary>
    /// Gets the lowest sale total, inclusive.
    /// </summary>
    public decimal? MinTotalAmount { get; init; }

    /// <summary>
    /// Gets the highest sale total, inclusive.
    /// </summary>
    public decimal? MaxTotalAmount { get; init; }
}
```

- [ ] **Step 4: Carry the filter in the list query and update the contract docs**

In `src/Ambev.DeveloperEvaluation.Domain/Repositories/SaleListQuery.cs`, replace

```csharp
/// Which page of sales <see cref="ISaleRepository.ListAsync"/> reads, and in what order.
```

with

```csharp
/// Which sales <see cref="ISaleRepository.ListAsync"/> reads: those matching <see cref="Filter"/>, one page, in order.
```

and replace

```csharp
public sealed record SaleListQuery(int Page, int Size, IReadOnlyList<SaleSort> Sorts);
```

with

```csharp
public sealed record SaleListQuery(int Page, int Size, IReadOnlyList<SaleSort> Sorts)
{
    /// <summary>
    /// Gets which sales count and can appear on the page. <see cref="SaleListFilter.None"/>, the default, keeps them all.
    /// </summary>
    public SaleListFilter Filter { get; init; } = SaleListFilter.None;
}
```

In `src/Ambev.DeveloperEvaluation.Domain/Repositories/SalePage.cs`, replace

```csharp
/// <param name="TotalCount">How many sales there are in all.</param>
```

with

```csharp
/// <param name="TotalCount">How many sales match the filter, across every page.</param>
```

In `src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs`, replace

```csharp
    /// Reads one page of sales with their items, untracked. The sales follow <see cref="SaleListQuery.Sorts"/>,
```

with

```csharp
    /// Reads one page of the sales that match <see cref="SaleListQuery.Filter"/>, with their items, untracked.
    /// The sales follow <see cref="SaleListQuery.Sorts"/>,
```

and replace

```csharp
    /// <returns>The page, and the number of sales in all.</returns>
```

with

```csharp
    /// <returns>The page, and how many sales match in all.</returns>
```

In `src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesResult.cs`, replace

```csharp
/// <param name="TotalCount">How many sales there are across every page.</param>
```

with

```csharp
/// <param name="TotalCount">How many sales match the filters, across every page.</param>
```

- [ ] **Step 5: Build the filter in the handler**

In `src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesHandler.cs`, replace

```csharp
/// Handles <see cref="ListSalesQuery"/>: applies the defaults, parses the order and reads the page.
```

with

```csharp
/// Handles <see cref="ListSalesQuery"/>: applies the defaults, parses the order, builds the filter and reads the page.
```

Then replace

```csharp
        var sales = await _saleRepository.ListAsync(
            new SaleListQuery(page, size, SaleOrderParser.Parse(query.Order)), cancellationToken);

        return new ListSalesResult(_mapper.Map<List<SaleResult>>(sales.Sales), page, size, sales.TotalCount);
    }
```

with

```csharp
        var listQuery = new SaleListQuery(page, size, SaleOrderParser.Parse(query.Order)) { Filter = ToFilter(query) };
        var sales = await _saleRepository.ListAsync(listQuery, cancellationToken);

        return new ListSalesResult(_mapper.Map<List<SaleResult>>(sales.Sales), page, size, sales.TotalCount);
    }

    /// <summary>
    /// Copies the filters, turning the sale-date limits into inclusive UTC bounds.
    /// </summary>
    private static SaleListFilter ToFilter(ListSalesQuery query) => new()
    {
        SaleNumber = query.SaleNumber,
        CustomerName = query.CustomerName,
        BranchName = query.BranchName,
        CustomerId = query.CustomerId,
        BranchId = query.BranchId,
        IsCancelled = query.IsCancelled,
        MinSaleDate = query.MinSaleDate is { } min ? SaleDateBounds.LowerBound(min) : null,
        MaxSaleDate = query.MaxSaleDate is { } max ? SaleDateBounds.UpperBound(max) : null,
        MinTotalAmount = query.MinTotalAmount,
        MaxTotalAmount = query.MaxTotalAmount
    };
```

- [ ] **Step 6: Verify (GREEN)**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~ListSalesHandlerTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)` with no `warning CS`, which also shows ticket 10's `new SaleListQuery(…)` calls still compile; `Passed:     5`; Unit at U0 + 38.

- [ ] **Step 7: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Domain/Repositories/SaleListFilter.cs src/Ambev.DeveloperEvaluation.Domain/Repositories/SaleListQuery.cs src/Ambev.DeveloperEvaluation.Domain/Repositories/SalePage.cs src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesHandler.cs src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesResult.cs tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/ListSales/ListSalesHandlerTests.cs
git commit -m "feat(sales): pass the list filters to the repository" -m "SaleListFilter holds the criteria of spec 7.3, each nullable, and SaleListQuery carries it with SaleListFilter.None as the default. ListSalesHandler copies the filters and turns the sale-date limits into inclusive UTC bounds."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `7 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 6: Filter the sales list in SQL

**Skills:** `efcore-patterns`, `testcontainers-integration-tests` (load both now), `dotnet-best-practices`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Integration/Sales/SaleListFilterTests.cs`
- Modify: `src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs`

- [ ] **Step 1: Write the failing integration tests**

Create `tests/Ambev.DeveloperEvaluation.Integration/Sales/SaleListFilterTests.cs`:

```csharp
using System.Globalization;
using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Ambev.DeveloperEvaluation.Integration.Fixtures;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using AutoMapper;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Sales;

/// <summary>
/// Contains integration tests for the filters of the sales list: <see cref="ListSalesHandler"/> over a real
/// <see cref="SaleRepository"/> on PostgreSQL. Each test starts from empty tables and these six sales, one line each
/// (quantity 1, so the total is the unit price), each with its own customer and branch ids:
/// <code>
/// sale  saleNumber  saleDate (UTC)              customerName   branchName     total  cancelled
/// A     S-1         2026-01-01 00:00:00         Maria Silva    Filial Centro  10.00  no
/// B     S-2         2026-01-15 12:00:00         Mariana Souza  Loja 50% Off   20.00  yes
/// C     S-3         2026-01-31 00:00:00         Ana Maria      Loja 500 Off   30.00  no
/// D     S-4         2026-01-31 23:59:59.999999  Joana Maria    Loja*Off       40.00  no
/// E     S_5         2026-02-01 00:00:00         Bruno Costa    Filial A\B     50.00  yes
/// F     S-5         2026-01-31 02:00:00         Carla Dias     Filial AB      60.00  no
/// </code>
/// Matches are compared as sorted letters, so the text collation plays no part. Every test also checks the count.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class SaleListFilterTests : IAsyncLifetime
{
    private static readonly IMapper Mapper =
        new MapperConfiguration(config => config.AddProfile<SaleProfile>()).CreateMapper();

    private readonly PostgreSqlFixture _database;
    private readonly Dictionary<char, Sale> _sales = new()
    {
        ['A'] = NewSale("S-1", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), "Maria Silva", "Filial Centro", 10.00m, cancelled: false),
        ['B'] = NewSale("S-2", new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc), "Mariana Souza", "Loja 50% Off", 20.00m, cancelled: true),
        ['C'] = NewSale("S-3", new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc), "Ana Maria", "Loja 500 Off", 30.00m, cancelled: false),
        ['D'] = NewSale("S-4", new DateTime(2026, 1, 31, 23, 59, 59, 999, 999, DateTimeKind.Utc), "Joana Maria", "Loja*Off", 40.00m, cancelled: false),
        ['E'] = NewSale("S_5", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), "Bruno Costa", @"Filial A\B", 50.00m, cancelled: true),
        ['F'] = NewSale("S-5", new DateTime(2026, 1, 31, 2, 0, 0, DateTimeKind.Utc), "Carla Dias", "Filial AB", 60.00m, cancelled: false)
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="SaleListFilterTests"/> class.
    /// </summary>
    /// <param name="database">The collection's database, injected by xUnit.</param>
    public SaleListFilterTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    /// <summary>
    /// Empties the tables, then saves the six sales, so every count is exact.
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
    /// Tests the wildcard forms on the sale number, case-insensitively, with <c>_</c> matched literally.
    /// </summary>
    [Theory(DisplayName = "Given the six sales When filtering by sale number Then only the matching sales count")]
    [InlineData("s-*", "ABCDF")]
    [InlineData("*5", "EF")]
    [InlineData("*_*", "E")]
    [InlineData("S_5", "E")]
    [InlineData("s-1", "A")]
    public async Task Given_SixSales_When_FilteringBySaleNumber_Then_OnlyMatchesCount(string saleNumber, string expected)
    {
        // When
        var result = await ListAsync(new ListSalesQuery { SaleNumber = saleNumber });

        // Then
        Matches(result).Should().Be(expected);
        result.TotalCount.Should().Be(expected.Length);
    }

    /// <summary>
    /// Tests the four wildcard forms on the customer name, case-insensitively. No <c>*</c> means equals, not contains.
    /// </summary>
    [Theory(DisplayName = "Given the six sales When filtering by customer name Then starts with, ends with, contains and equals match")]
    [InlineData("mar*", "AB")]
    [InlineData("*MARIA", "CD")]
    [InlineData("*ana*", "BCD")]
    [InlineData("ana maria", "C")]
    [InlineData("maria", "")]
    public async Task Given_SixSales_When_FilteringByCustomerName_Then_OnlyMatchesCount(string customerName, string expected)
    {
        // When
        var result = await ListAsync(new ListSalesQuery { CustomerName = customerName });

        // Then
        Matches(result).Should().Be(expected);
        result.TotalCount.Should().Be(expected.Length);
    }

    /// <summary>
    /// Tests the branch name, with <c>%</c>, an inner <c>*</c> and <c>\</c> matched literally.
    /// </summary>
    [Theory(DisplayName = "Given the six sales When filtering by branch name Then %, an inner * and a backslash match themselves")]
    [InlineData("loja*", "BCD")]
    [InlineData("*b", "EF")]
    [InlineData("*50%*", "B")]
    [InlineData("loja*off", "D")]
    [InlineData(@"filial a\b", "E")]
    public async Task Given_SixSales_When_FilteringByBranchName_Then_OnlyMatchesCount(string branchName, string expected)
    {
        // When
        var result = await ListAsync(new ListSalesQuery { BranchName = branchName });

        // Then
        Matches(result).Should().Be(expected);
        result.TotalCount.Should().Be(expected.Length);
    }

    /// <summary>
    /// Tests the exact customer-id match.
    /// </summary>
    [Fact(DisplayName = "Given the six sales When filtering by C's customer id Then only C counts")]
    public async Task Given_SixSales_When_FilteringByCustomerId_Then_OnlyThatCustomersSaleCounts()
    {
        // When
        var result = await ListAsync(new ListSalesQuery { CustomerId = _sales['C'].Customer.Id });

        // Then
        Matches(result).Should().Be("C");
        result.TotalCount.Should().Be(1);
    }

    /// <summary>
    /// Tests the exact branch-id match.
    /// </summary>
    [Fact(DisplayName = "Given the six sales When filtering by E's branch id Then only E counts")]
    public async Task Given_SixSales_When_FilteringByBranchId_Then_OnlyThatBranchsSaleCounts()
    {
        // When
        var result = await ListAsync(new ListSalesQuery { BranchId = _sales['E'].Branch.Id });

        // Then
        Matches(result).Should().Be("E");
        result.TotalCount.Should().Be(1);
    }

    /// <summary>
    /// Tests that an id no sale has matches nothing.
    /// </summary>
    [Fact(DisplayName = "Given the six sales When filtering by an unknown customer id Then no sale counts")]
    public async Task Given_SixSales_When_FilteringByUnknownCustomerId_Then_NoSaleCounts()
    {
        // When
        var result = await ListAsync(new ListSalesQuery { CustomerId = Guid.NewGuid() });

        // Then
        result.Sales.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    /// <summary>
    /// Tests the cancelled-status match both ways.
    /// </summary>
    [Theory(DisplayName = "Given the six sales When filtering by isCancelled Then only sales with that status count")]
    [InlineData(true, "BE")]
    [InlineData(false, "ACDF")]
    public async Task Given_SixSales_When_FilteringByIsCancelled_Then_OnlyThatStatusCounts(bool isCancelled, string expected)
    {
        // When
        var result = await ListAsync(new ListSalesQuery { IsCancelled = isCancelled });

        // Then
        Matches(result).Should().Be(expected);
        result.TotalCount.Should().Be(expected.Length);
    }

    /// <summary>
    /// Tests the sale-date range: inclusive bounds, a value without an offset read as UTC, offsets converted,
    /// and a max at exactly midnight UTC covering its whole day (D, at its last microsecond).
    /// </summary>
    [Theory(DisplayName = "Given the six sales When filtering by sale date Then the bounds are inclusive UTC and a midnight max covers its day")]
    [InlineData("2026-01-15T12:00:00Z", null, "BCDEF")]
    [InlineData(null, "2026-01-15T12:00:00Z", "AB")]
    [InlineData(null, "2026-01-31", "ABCDF")]
    [InlineData(null, "2026-01-31T00:00:00Z", "ABCDF")]
    [InlineData(null, "2026-01-31T12:00:00", "ABCF")]
    [InlineData(null, "2026-01-30T23:00:00-03:00", "ABCF")]
    [InlineData(null, "2026-01-30T22:59:59-03:00", "ABC")]
    [InlineData(null, "2026-01-31T00:00:00-03:00", "ABCF")]
    [InlineData("2026-01-31T00:00:00-03:00", null, "DE")]
    [InlineData("2026-01-31", "2026-01-31", "CDF")]
    public async Task Given_SixSales_When_FilteringBySaleDate_Then_InclusiveUtcBounds(string? min, string? max, string expected)
    {
        // When
        var result = await ListAsync(new ListSalesQuery { MinSaleDate = QueryDate(min), MaxSaleDate = QueryDate(max) });

        // Then
        Matches(result).Should().Be(expected);
        result.TotalCount.Should().Be(expected.Length);
    }

    /// <summary>
    /// Tests the total-amount range, both bounds inclusive.
    /// </summary>
    [Theory(DisplayName = "Given the six sales When filtering by total amount Then both bounds are inclusive")]
    [InlineData("30", null, "CDEF")]
    [InlineData(null, "30", "ABC")]
    [InlineData("20", "40", "BCD")]
    [InlineData("30", "30", "C")]
    public async Task Given_SixSales_When_FilteringByTotalAmount_Then_InclusiveBounds(string? min, string? max, string expected)
    {
        // When
        var result = await ListAsync(new ListSalesQuery { MinTotalAmount = Amount(min), MaxTotalAmount = Amount(max) });

        // Then
        Matches(result).Should().Be(expected);
        result.TotalCount.Should().Be(expected.Length);
    }

    /// <summary>
    /// Tests that filters combine: <c>*ana*</c> and <c>loja*</c> keep B, C and D; open drops B; 35 or more drops C;
    /// the whole of 2026-01-31 keeps D.
    /// </summary>
    [Fact(DisplayName = "Given the six sales When combining text, status, amount and whole-day date filters Then only the sale matching all of them counts")]
    public async Task Given_SixSales_When_CombiningFilters_Then_OnlySaleMatchingAllCounts()
    {
        // When
        var result = await ListAsync(new ListSalesQuery
        {
            CustomerName = "*ana*",
            BranchName = "loja*",
            IsCancelled = false,
            MinTotalAmount = 35m,
            MaxSaleDate = QueryDate("2026-01-31")
        });

        // Then
        Matches(result).Should().Be("D");
        result.TotalCount.Should().Be(1);
    }

    /// <summary>
    /// Tests that a filter combines with paging and ordering: the open sales by highest total are F, D, C, A.
    /// </summary>
    [Fact(DisplayName = "Given the six sales When reading page 2 of 2 of the open sales by highest total Then it holds C then A and the count is 4")]
    public async Task Given_SixSales_When_ReadingSecondPageOfOpenSalesByTotal_Then_HoldsCThenAWithCountFour()
    {
        // When
        var result = await ListAsync(new ListSalesQuery { IsCancelled = false, Order = "totalAmount desc", Page = 2, Size = 2 });

        // Then
        Letters(result).Should().Be("CA");
        result.TotalCount.Should().Be(4);
    }

    private static Sale NewSale(string saleNumber, DateTime saleDate, string customerName, string branchName, decimal total, bool cancelled)
    {
        var sale = Sale.Create(
            saleNumber,
            saleDate,
            new ExternalIdentity(Guid.NewGuid(), customerName),
            new ExternalIdentity(Guid.NewGuid(), branchName),
            [new SaleItemData(new ExternalIdentity(Guid.NewGuid(), $"Product {saleNumber}"), 1, total)]);
        if (cancelled)
            sale.Cancel();
        return sale;
    }

    // ASP.NET Core's DateTimeModelBinder parses a query value this way: no offset gives Unspecified, an offset or Z gives UTC.
    private static DateTime? QueryDate(string? value) =>
        value is null
            ? null
            : DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces);

    private static decimal? Amount(string? value) =>
        value is null ? null : decimal.Parse(value, CultureInfo.InvariantCulture);

    private string Letters(ListSalesResult result) =>
        string.Concat(result.Sales.Select(sale => _sales.Single(pair => pair.Value.Id == sale.Id).Key));

    private string Matches(ListSalesResult result) => string.Concat(Letters(result).Order());

    private async Task<ListSalesResult> ListAsync(ListSalesQuery query)
    {
        await using var context = _database.CreateContext();
        return await new ListSalesHandler(new SaleRepository(context), Mapper).Handle(query, CancellationToken.None);
    }
}
```

- [ ] **Step 2: Watch them fail (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Integration 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build --filter "FullyQualifiedName~SaleListFilterTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `Build succeeded.`; `Failed:    36, Passed:     0`. The repository ignores the filter, so every test gets all six sales, or F E D C B A for the paging test.

- [ ] **Step 3: Apply the filter before the count**

In `src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs`, replace

```csharp
        var sales = _context.Sales.AsNoTracking();

        var totalCount = await sales.CountAsync(cancellationToken);
```

with

```csharp
        // Filtered before the count, so the total counts only the matching sales.
        var sales = ApplyFilter(_context.Sales.AsNoTracking(), query.Filter);

        var totalCount = await sales.CountAsync(cancellationToken);
```

Then replace

```csharp
    /// <summary>
    /// Orders by each sort in turn, then by id. Each field maps to a fixed expression; nothing is looked up by name.
    /// </summary>
```

with

```csharp
    /// <summary>
    /// Keeps the sales that match every criterion the filter sets (spec §7.3). Text criteria use ILIKE with an escape
    /// character; every value is a parameter, never part of the SQL text.
    /// </summary>
    private static IQueryable<Sale> ApplyFilter(IQueryable<Sale> sales, SaleListFilter filter)
    {
        if (filter.SaleNumber is not null)
        {
            var pattern = LikePattern.FromWildcards(filter.SaleNumber);
            sales = sales.Where(sale => EF.Functions.ILike(sale.SaleNumber, pattern, LikePattern.EscapeCharacter));
        }

        if (filter.CustomerName is not null)
        {
            var pattern = LikePattern.FromWildcards(filter.CustomerName);
            sales = sales.Where(sale => EF.Functions.ILike(sale.Customer.Name, pattern, LikePattern.EscapeCharacter));
        }

        if (filter.BranchName is not null)
        {
            var pattern = LikePattern.FromWildcards(filter.BranchName);
            sales = sales.Where(sale => EF.Functions.ILike(sale.Branch.Name, pattern, LikePattern.EscapeCharacter));
        }

        if (filter.CustomerId is { } customerId)
            sales = sales.Where(sale => sale.Customer.Id == customerId);
        if (filter.BranchId is { } branchId)
            sales = sales.Where(sale => sale.Branch.Id == branchId);
        if (filter.IsCancelled is { } isCancelled)
            sales = sales.Where(sale => sale.IsCancelled == isCancelled);
        if (filter.MinSaleDate is { } minSaleDate)
            sales = sales.Where(sale => sale.SaleDate >= minSaleDate);
        if (filter.MaxSaleDate is { } maxSaleDate)
            sales = sales.Where(sale => sale.SaleDate <= maxSaleDate);
        if (filter.MinTotalAmount is { } minTotalAmount)
            sales = sales.Where(sale => sale.TotalAmount >= minTotalAmount);
        if (filter.MaxTotalAmount is { } maxTotalAmount)
            sales = sales.Where(sale => sale.TotalAmount <= maxTotalAmount);

        return sales;
    }

    /// <summary>
    /// Orders by each sort in turn, then by id. Each field maps to a fixed expression; nothing is looked up by name.
    /// </summary>
```

- [ ] **Step 4: Verify (GREEN)**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build --filter "FullyQualifiedName~SaleListFilterTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at ' | tail -1
```

Expected:
- `0 Error(s)` and no `warning CS`
- `Passed:    36`
- Integration at I0 + 36, which includes ticket 10's `SaleRepositoryListTests`, unchanged
- `No changes have been made to the model since the last migration.`

- [ ] **Step 5: Prove the count follows the filter (mutation check)**

With the Edit tool, change `var totalCount = await sales.CountAsync(cancellationToken);` to `var totalCount = await _context.Sales.CountAsync(cancellationToken);`. Then run:

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Integration 2>&1 | grep -E 'Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build --filter "FullyQualifiedName~SaleListFilterTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | sort -u | cut -c1-160
```

Expected: `Failed:    36`. Each failure reads `Expected result.TotalCount to be …, but found 6.` Restore the line, rebuild and rerun the filter: `Passed:    36`. `git diff --stat` must show the same changes as before the check.

- [ ] **Step 6: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs tests/Ambev.DeveloperEvaluation.Integration/Sales/SaleListFilterTests.cs
git commit -m "feat(sales): filter the sales list in SQL" -m "ListAsync applies each criterion of the filter before the COUNT, so the total counts only the matches. Text criteria use ILIKE with an explicit backslash escape; ids and the cancelled status match exactly; sale-date and total ranges are inclusive. The integration tests run the list handler over PostgreSQL and cover every filter, each wildcard form, literal %, _, backslash and inner *, the whole-day max date, offsets and combined filters with paging."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `2 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 7: Filter sales over HTTP

**Skills:** `dotnet-best-practices`, `testcontainers-integration-tests`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesMappingTests.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Functional/Sales/ListSalesFilterTests.cs`
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/ListSales/ListSalesRequest.cs`
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs`

- [ ] **Step 1: Write the failing mapping test**

In `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesMappingTests.cs`, replace

```csharp
        query.Should().BeEquivalentTo(new { Page = 2, Size = (int?)null, Order = "saleDate desc" });
    }
```

with

```csharp
        query.Should().BeEquivalentTo(new { Page = 2, Size = (int?)null, Order = "saleDate desc" });
    }

    /// <summary>
    /// Tests that every filter of the list request reaches the query unchanged.
    /// </summary>
    [Fact(DisplayName = "Given a list-sales request with every filter When mapping it to ListSalesQuery Then every filter is copied")]
    public void Given_ListSalesRequestWithEveryFilter_When_MappingToQuery_Then_FiltersAreCopied()
    {
        // Given
        var request = new ListSalesRequest
        {
            SaleNumber = "S-*",
            CustomerName = "*maria",
            BranchName = "filial*",
            CustomerId = Guid.NewGuid(),
            BranchId = Guid.NewGuid(),
            IsCancelled = true,
            MinSaleDate = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            MaxSaleDate = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Unspecified),
            MinTotalAmount = 10.00m,
            MaxTotalAmount = 99.99m
        };

        // When
        var query = _configuration.CreateMapper().Map<ListSalesQuery>(request);

        // Then
        query.Should().BeEquivalentTo(request);
    }
```

- [ ] **Step 2: Write the failing functional tests**

Create `tests/Ambev.DeveloperEvaluation.Functional/Sales/ListSalesFilterTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

/// <summary>
/// Contains functional tests for the filters of <c>GET /api/sales</c>. Each test starts from empty tables with a
/// logged-in client.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class ListSalesFilterTests : IAsyncLifetime
{
    private readonly ApiFixture _api;
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListSalesFilterTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public ListSalesFilterTests(ApiFixture api)
    {
        _api = api;
        _client = api.CreateClient();
    }

    /// <summary>
    /// Empties the tables, then logs in, so the counts are exact.
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
    /// Tests a representative combination. The request keeps customer names ending in "maria", open sales, sale dates
    /// from 2026-01-10T12:00Z (sent as 09:00 at -03:00) through the whole of 2026-01-31, and totals of 15.00 or more:
    /// <code>
    /// customerName  saleDate (UTC)       total  cancelled  kept
    /// Ana Maria     2026-01-10 11:59:59  30.00  no         no: before the min date
    /// Joana Maria   2026-01-10 12:00:00  20.00  no         yes: at the min date
    /// Bia Maria     2026-01-31 23:00:00  40.00  no         yes: the max date covers its whole day
    /// Duda Maria    2026-01-20 12:00:00  10.00  no         no: below the min total
    /// Maria Silva   2026-01-20 12:00:00  60.00  no         no: the name doesn't end in maria
    /// Clara Maria   2026-01-20 12:00:00  50.00  yes        no: cancelled
    /// </code>
    /// By highest total, one per page: page 2 holds Joana Maria, and the totals count the two matches.
    /// </summary>
    [Fact(DisplayName = "Given six sales When combining text, status, date and amount filters with paging and ordering Then page 2 holds the second match and totalItems is 2")]
    public async Task Given_SixSales_When_CombiningFiltersWithPagingAndOrdering_Then_ReturnsSecondMatchWithFilteredTotals()
    {
        // Given
        await CreateSaleAsync("Ana Maria", new DateTime(2026, 1, 10, 11, 59, 59, DateTimeKind.Utc), 30.00m);
        var atMinDate = await CreateSaleAsync("Joana Maria", new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc), 20.00m);
        await CreateSaleAsync("Bia Maria", new DateTime(2026, 1, 31, 23, 0, 0, DateTimeKind.Utc), 40.00m);
        await CreateSaleAsync("Duda Maria", new DateTime(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc), 10.00m);
        await CreateSaleAsync("Maria Silva", new DateTime(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc), 60.00m);
        var toCancel = await CreateSaleAsync("Clara Maria", new DateTime(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc), 50.00m);
        await _client.CancelSaleAsync(toCancel.Id);

        // When
        using var response = await _client.GetAsync(
            "/api/sales?customerName=*MARIA&isCancelled=false&_minSaleDate=2026-01-10T09:00:00-03:00&_maxSaleDate=2026-01-31"
            + "&_minTotalAmount=15&_order=totalAmount%20desc&_page=2&_size=1");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = (await response.Content.ReadFromJsonAsync<SalePageResponseBody>())!;
        page.Data.Select(sale => sale.Id).Should().Equal(atMinDate.Id);
        new { page.CurrentPage, page.TotalPages, page.TotalItems }.Should().Be(new { CurrentPage = 2, TotalPages = 2, TotalItems = 2 });
    }

    /// <summary>
    /// Tests that a min above its max is a 400 with the documented body.
    /// </summary>
    [Theory(DisplayName = "Given a min above its max When listing Then returns 400 ValidationError for the min")]
    [InlineData("_minTotalAmount=50&_maxTotalAmount=10", "_minTotalAmount: '_minTotalAmount' must not be above '_maxTotalAmount'.")]
    [InlineData("_minSaleDate=2026-02-01&_maxSaleDate=2026-01-31", "_minSaleDate: '_minSaleDate' must not be above '_maxSaleDate'.")]
    public async Task Given_MinAboveMax_When_Listing_Then_Returns400ValidationError(string query, string detail)
    {
        // When
        using var response = await _client.GetAsync($"/api/sales?{query}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"ValidationError","error":"Invalid input data","detail":"{{detail}}"}""");
    }

    /// <summary>
    /// Tests that a malformed Guid, date, number or boolean fails model binding with the documented 400 body.
    /// </summary>
    [Theory(DisplayName = "Given a malformed filter value When listing Then returns 400 ValidationError naming the parameter")]
    [InlineData("customerId=not-a-guid", "customerId: The value 'not-a-guid' is not valid for CustomerId.")]
    [InlineData("_minSaleDate=yesterday", "_minSaleDate: The value 'yesterday' is not valid for MinSaleDate.")]
    [InlineData("_maxTotalAmount=ten", "_maxTotalAmount: The value 'ten' is not valid for MaxTotalAmount.")]
    [InlineData("isCancelled=maybe", "isCancelled: The value 'maybe' is not valid for IsCancelled.")]
    public async Task Given_MalformedFilterValue_When_Listing_Then_Returns400ValidationError(string query, string detail)
    {
        // When
        using var response = await _client.GetAsync($"/api/sales?{query}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"ValidationError","error":"Invalid input data","detail":"{{detail}}"}""");
    }

    private Task<SaleResponseBody> CreateSaleAsync(string customerName, DateTime saleDate, decimal total) =>
        _client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid(SaleRequestBodyTestData.GenerateItem(1, total)) with
        {
            CustomerName = customerName,
            SaleDate = saleDate
        });
}
```

- [ ] **Step 3: Watch them fail (RED)**

The Unit project doesn't compile yet, so build only the Functional project:

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u | head -3
dotnet build tests/Ambev.DeveloperEvaluation.Functional 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~ListSalesFilterTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected:
- only `SalesMappingTests.cs` errors, `CS0117` (`'ListSalesRequest' does not contain a definition for 'SaleNumber'`, …)
- the Functional project builds
- `Failed:     7, Passed:     0`. The API ignores the unknown query keys: the 400 tests get 200, and the combination test gets page 2 of all six sales (Clara Maria's).

- [ ] **Step 4: Add the filters to the request**

In `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/ListSales/ListSalesRequest.cs`, replace

```csharp
/// The query string of <c>GET /api/sales</c> (<c>.doc/general-api.md</c>). Every parameter is optional: an omitted one
/// is <c>null</c> and takes its default in the Application layer.
```

with

```csharp
/// The query string of <c>GET /api/sales</c> (<c>.doc/general-api.md</c>, spec §7.3). Every parameter is optional:
/// an omitted one is <c>null</c>, so paging and ordering take their defaults in the Application layer and a filter
/// doesn't filter. A malformed value fails model binding (400).
```

Then replace

```csharp
    [FromQuery(Name = "_order")]
    public string? Order { get; set; }
```

with

```csharp
    [FromQuery(Name = "_order")]
    public string? Order { get; set; }

    /// <summary>
    /// Gets or sets the sale-number filter, <c>saleNumber</c>: case-insensitive; <c>value*</c> starts with,
    /// <c>*value</c> ends with, <c>*value*</c> contains, no <c>*</c> equals.
    /// </summary>
    [FromQuery(Name = "saleNumber")]
    public string? SaleNumber { get; set; }

    /// <summary>
    /// Gets or sets the customer-name filter, <c>customerName</c>, matched like <see cref="SaleNumber"/>.
    /// </summary>
    [FromQuery(Name = "customerName")]
    public string? CustomerName { get; set; }

    /// <summary>
    /// Gets or sets the branch-name filter, <c>branchName</c>, matched like <see cref="SaleNumber"/>.
    /// </summary>
    [FromQuery(Name = "branchName")]
    public string? BranchName { get; set; }

    /// <summary>
    /// Gets or sets the customer id, <c>customerId</c>, matched exactly.
    /// </summary>
    [FromQuery(Name = "customerId")]
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the branch id, <c>branchId</c>, matched exactly.
    /// </summary>
    [FromQuery(Name = "branchId")]
    public Guid? BranchId { get; set; }

    /// <summary>
    /// Gets or sets whether the sales are cancelled, <c>isCancelled</c>.
    /// </summary>
    [FromQuery(Name = "isCancelled")]
    public bool? IsCancelled { get; set; }

    /// <summary>
    /// Gets or sets the earliest sale date, <c>_minSaleDate</c>, inclusive. A value without an offset is UTC.
    /// </summary>
    [FromQuery(Name = "_minSaleDate")]
    public DateTime? MinSaleDate { get; set; }

    /// <summary>
    /// Gets or sets the latest sale date, <c>_maxSaleDate</c>, inclusive. A value without an offset is UTC, and exactly
    /// midnight (such as <c>2026-01-31</c>) covers that whole day.
    /// </summary>
    [FromQuery(Name = "_maxSaleDate")]
    public DateTime? MaxSaleDate { get; set; }

    /// <summary>
    /// Gets or sets the lowest sale total, <c>_minTotalAmount</c>, inclusive.
    /// </summary>
    [FromQuery(Name = "_minTotalAmount")]
    public decimal? MinTotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the highest sale total, <c>_maxTotalAmount</c>, inclusive.
    /// </summary>
    [FromQuery(Name = "_maxTotalAmount")]
    public decimal? MaxTotalAmount { get; set; }
```

`ListSalesProfile`'s `CreateMap<ListSalesRequest, ListSalesQuery>()` maps the new properties by name, so it doesn't change.

- [ ] **Step 5: Update the action's docs**

In `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs`, replace

```csharp
    /// Lists sales a page at a time, each with its items: <c>_page</c> (default 1), <c>_size</c> (default 10, at most 100)
    /// and <c>_order</c> (default <c>saleDate desc</c>), as <c>.doc/general-api.md</c> describes.
    /// </summary>
    /// <param name="request">The paging and ordering parameters.</param>
```

with

```csharp
    /// Lists the sales that match the filters of <see cref="ListSalesRequest"/>, a page at a time, each with its items:
    /// <c>_page</c> (default 1), <c>_size</c> (default 10, at most 100) and <c>_order</c> (default <c>saleDate desc</c>),
    /// as <c>.doc/general-api.md</c> describes. An omitted filter doesn't filter.
    /// </summary>
    /// <param name="request">The paging, ordering and filter parameters.</param>
```

- [ ] **Step 6: Verify (GREEN)**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected: `0 Error(s)` with no `warning CS`; Unit at U0 + 39; Functional at F0 + 7, which includes ticket 10's `ListSalesTests`, unchanged.

- [ ] **Step 7: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/ListSales/ListSalesRequest.cs src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesMappingTests.cs tests/Ambev.DeveloperEvaluation.Functional/Sales/ListSalesFilterTests.cs
git commit -m "feat(sales): filter sales over HTTP" -m "GET /api/sales takes the optional saleNumber, customerName, branchName, customerId, branchId, isCancelled, _minSaleDate, _maxSaleDate, _minTotalAmount and _maxTotalAmount query parameters and combines them with paging and ordering. A min above its max or a malformed Guid, date, number or boolean is a 400 ValidationError."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `4 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 8: Add the filtered list to the `.http` file and replay it against `docker compose up`

**Files:**
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http`

- [ ] **Step 1: Look at the file (RED)**

```bash
grep -c '_minSaleDate' src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
```

Expected: `0`.

- [ ] **Step 2: Add the requests after ticket 10's list requests**

With the Edit tool, replace

```http
### List sales by a field that can't be sorted (400 ValidationError)
GET {{baseUrl}}/api/sales?_order=price%20desc
Authorization: Bearer {{token}}
```

with

```http
### List sales by a field that can't be sorted (400 ValidationError)
GET {{baseUrl}}/api/sales?_order=price%20desc
Authorization: Bearer {{token}}

### List sales filtered: customer names ending in "silva", open, sold from 11:30 at -03:00 through the end of 2026-09-24, totals of 20.00 or more (200; the sale created above is in data, and totalItems counts only matches)
# _maxSaleDate has no time, so it covers the whole day. Only a leading or trailing * is a wildcard.
GET {{baseUrl}}/api/sales?customerName=*silva&isCancelled=false&_minSaleDate=2026-09-24T11:30:00-03:00&_maxSaleDate=2026-09-24&_minTotalAmount=20&_order=totalAmount%20desc
Authorization: Bearer {{token}}

### List sales with a minimum total above the maximum (400 ValidationError)
GET {{baseUrl}}/api/sales?_minTotalAmount=50&_maxTotalAmount=10
Authorization: Bearer {{token}}
```

- [ ] **Step 3: Check the file (GREEN)**

```bash
git diff --stat | cat
grep -c '_minSaleDate' src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
```

Expected: `1 file changed, 9 insertions(+)` and `1`.

- [ ] **Step 4: Start the stack from nothing**

```bash
docker compose down -v --remove-orphans
docker compose up --build --wait --wait-timeout 300; echo "exit=$?"
```

Expected: `exit=0`. If a port is already allocated, stop and ask the user.

- [ ] **Step 5: Replay with curl on a known data set**

Keep this in one Bash call. Every sale costs 4.50 per item. Five items get 10% (20.25), ten get 20% (36.00), and one gets none (4.50).

```bash
BASE=http://localhost:8080
CHECKS=/tmp/ticket11-checks
curl -s -o /dev/null -w "sign-up %{http_code}\n" -X POST $BASE/api/users -H 'Content-Type: application/json' --data-raw '{"username":"admin","password":"Admin@123","phone":"+5511999999999","email":"admin@developerstore.com","status":"Active","role":"Admin"}'
curl -s -o $CHECKS/login.json -X POST $BASE/api/auth -H 'Content-Type: application/json' --data-raw '{"email":"admin@developerstore.com","password":"Admin@123"}'
TOKEN=$(python3 -c "import json, sys; print(json.load(open(sys.argv[1]))['data']['token'])" $CHECKS/login.json)
create() {
  curl -s -o $CHECKS/create.json -w "create $1 %{http_code}\n" -X POST $BASE/api/sales -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' --data-raw "{\"saleNumber\":\"S-$(( RANDOM % 900000 + 100000 ))\",\"saleDate\":\"$2\",\"customerId\":\"3f2b8c1e-1111-4a5b-9c2d-000000000001\",\"customerName\":\"$1\",\"branchId\":\"3f2b8c1e-2222-4a5b-9c2d-000000000002\",\"branchName\":\"Filial Centro\",\"items\":[{\"productId\":\"3f2b8c1e-3333-4a5b-9c2d-000000000003\",\"productName\":\"Cerveja 350ml\",\"quantity\":$3,\"unitPrice\":4.50}]}"
}
create "Maria Silva" 2026-09-24T14:30:00Z 5
create "Ana Silva" 2026-09-24T14:29:59Z 5
create "Bruno Souza" 2026-09-24T20:00:00Z 5
create "Dora Silva" 2026-09-24T23:00:00Z 10
create "Eva Silva" 2026-09-24T15:00:00Z 1
create "Carla Silva" 2026-09-24T16:00:00Z 5
CANCEL=$(python3 -c "import json, sys; print(json.load(open(sys.argv[1]))['data']['id'])" $CHECKS/create.json)
curl -s -o /dev/null -w "cancel %{http_code}\n" -X PATCH "$BASE/api/sales/$CANCEL/cancel" -H "Authorization: Bearer $TOKEN"
curl -s -o $CHECKS/list.json -w "list %{http_code} " "$BASE/api/sales?customerName=*silva&isCancelled=false&_minSaleDate=2026-09-24T11:30:00-03:00&_maxSaleDate=2026-09-24&_minTotalAmount=20&_order=totalAmount%20desc" -H "Authorization: Bearer $TOKEN"
python3 -c "import json, sys; b = json.load(open(sys.argv[1])); print([(s['customerName'], s['totalAmount']) for s in b['data']], 'totalItems', b['totalItems'])" $CHECKS/list.json
curl -s -w " %{http_code}\n" "$BASE/api/sales?_minTotalAmount=50&_maxTotalAmount=10" -H "Authorization: Bearer $TOKEN"
curl -s -w " %{http_code}\n" "$BASE/api/sales?customerId=not-a-guid" -H "Authorization: Bearer $TOKEN"
```

Expected:

```
sign-up 201
create Maria Silva 201
create Ana Silva 201
create Bruno Souza 201
create Dora Silva 201
create Eva Silva 201
create Carla Silva 201
cancel 200
list 200 [('Dora Silva', 36.0), ('Maria Silva', 20.25)] totalItems 2
{"type":"ValidationError","error":"Invalid input data","detail":"_minTotalAmount: '_minTotalAmount' must not be above '_maxTotalAmount'."} 400
{"type":"ValidationError","error":"Invalid input data","detail":"customerId: The value 'not-a-guid' is not valid for CustomerId."} 400
```

The other sales are excluded for these reasons:
- Ana Silva is one second before the min.
- Bruno Souza's name doesn't match.
- Eva Silva's total is below 20.
- Carla Silva is cancelled.

Dora Silva, at 23:00Z, shows the whole-day max.

- [ ] **Step 6: Take the stack down**

```bash
docker compose down -v
docker compose ps -a
```

Expected: `ps -a` lists nothing.

- [ ] **Step 7: Commit**

```bash
git add src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
git commit -m "docs(http): list sales with filters" -m "The .http file lists open sales of customers whose name ends in silva, from 11:30 at -03:00 through the whole of 2026-09-24 and at least 20.00, which finds the sale it just created, and shows the 400 for a minimum total above the maximum."
git show --stat --format= HEAD | tail -1
```

Expected: `1 file changed, 9 insertions(+)`. There's no slopwatch run, because no C# changed.

---

### Task 9: Verify everything, review, then open a pull request into `develop`

**Skills:** superpowers:verification-before-completion, `dotnet-slopwatch`, `test-anti-patterns` (report only), `clean-code` and superpowers:requesting-code-review (both optional), superpowers:finishing-a-development-branch.

**Files:** none, unless Step 2 finds a real problem.

- [ ] **Step 1: Re-run every check fresh**

```bash
git status --short
dotnet build Ambev.DeveloperEvaluation.sln --no-incremental 2>&1 | grep -E 'warning CS|Warning\(s\)|Error\(s\)|Build succeeded' | sort -u
dotnet test Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-110
cat /tmp/ticket11-checks/baseline.txt
slopwatch analyze --fail-on warning 2>&1 | tail -1
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at ' | tail -1
git grep -n 'ILike(' -- src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs
```

Expected:
- only the untracked tickets and plans
- `0 Error(s)`, `2 Warning(s)`, `Build succeeded.` and no `warning CS`
- against the baseline: Unit at U0 + 39, Integration at I0 + 36, Functional at F0 + 7
- `Scan complete: 0 issue(s) found`
- `No changes have been made to the model since the last migration.`
- three `ILike(` lines, each ending in `LikePattern.EscapeCharacter));`

| Ticket criterion | Evidence |
|---|---|
| Text filters: case-insensitive `value*`, `*value`, `*value*`, equals | `LikePatternTests`, and the sale-number, customer-name and branch-name theories of `SaleListFilterTests` |
| Only an edge `*` is a wildcard; `%`, `_`, `\` and an inner `*` are literal (ILIKE with an escape character) | `LikePatternTests`, `SaleListFilterTests` (each case has a decoy), the `ILike(` grep |
| `customerId`, `branchId`, `isCancelled` match exactly | `SaleListFilterTests` |
| Dates inclusive, no offset = UTC, a midnight max covers its day | `SaleDateBoundsTests`, `ListSalesHandlerTests`, the date theory of `SaleListFilterTests`, `ListSalesFilterTests` (combination) |
| Amounts inclusive | the amount theory of `SaleListFilterTests` |
| A min above its max, or a malformed value, is 400 `ValidationError` | `ListSalesQueryValidatorTests`, the two theories of `ListSalesFilterTests`, Task 8 Step 5 |
| Nullable parameters; an omitted filter doesn't filter | `ListSalesRequest`, `ListSalesQuery`, `ListSalesHandlerTests` (no filters), ticket 10's tests unchanged |
| Filters combine with each other and with paging and ordering; `totalItems` counts the filtered sales | `SaleListFilterTests` (combined and paging tests), the Task 6 Step 5 mutation check, `ListSalesFilterTests` (combination), Task 8 Step 5 |
| Unit tests: range rules, wildcard translation with escaping | `ListSalesQueryValidatorTests`, `SaleDateBoundsTests`, `LikePatternTests` |
| Integration and functional tests reset the data first | the `InitializeAsync` of `SaleListFilterTests` and `ListSalesFilterTests` |
| `.http` filtered list | Task 8 |
| `feature/list-sales-filters`, a pull request into `develop`, Conventional Commits, no attribution | Steps 4–6 |

- [ ] **Step 2: Audit the tests (skill `test-anti-patterns`, report only)**

Run it on these:
- `LikePatternTests` and `SaleDateBoundsTests`
- the new tests in `ListSalesQueryValidatorTests`, `ListSalesHandlerTests` and `SalesMappingTests`
- `SaleListFilterTests` and `ListSalesFilterTests`

These are deliberate. Report them and leave them:
- `QueryDate` repeats `DateTimeModelBinder`'s parsing in the Unit and Integration projects: it's the input the handler really receives.
- `Matches` sorts the letters. The filter tests assert membership, and text order depends on the collation. The paging test asserts order through `Letters`.
- The seeding loop in `InitializeAsync`, and the six creates in the functional combination test: that's the data set, not logic under test.
- The local-time test in `SaleDateBoundsTests` can't tell a conversion from none on a machine whose time zone is UTC. Its XML doc says so.

Fix a real finding in a separate `test(sales): …` commit, then re-run Step 1.

- [ ] **Step 3: Optional code review**

Run superpowers:requesting-code-review on `develop..feature/list-sales-filters`, with `clean-code`, `dotnet-best-practices` and `efcore-patterns` as lenses. Findings that belong to tickets 12–13 go into the report, not this branch.

- [ ] **Step 4: Check the commit messages**

```bash
git log --format=%s develop..feature/list-sales-filters
git log --format=%B develop..feature/list-sales-filters | grep -n -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
```

Expected, plus any `test(sales):` commit from Step 2:

```
docs(http): list sales with filters
feat(sales): filter sales over HTTP
feat(sales): filter the sales list in SQL
feat(sales): pass the list filters to the repository
feat(sales): validate the range filters of the sales list
feat(sales): read the sale-date filter limits as inclusive UTC bounds
feat(sales): translate list text filters into ILIKE patterns
no attribution lines
```

- [ ] **Step 5: Push the branch and open a pull request into `develop`**

Write the body with the Write tool to `/tmp/ticket11-checks/pr-body.md`: the Goal in two sentences, the evidence table with this run's results, and what Steps 2 and 3 reported. No attribution lines. Then:

```bash
git push -u origin feature/list-sales-filters
gh pr create --base develop --head feature/list-sales-filters --title "Ticket 11: list sales filters" --body-file /tmp/ticket11-checks/pr-body.md
```

Expected: the branch is pushed and `gh pr create` prints the URL. If `gh` fails (check `gh auth status`), stop and tell the user the branch is pushed.

- [ ] **Step 6: Verify the pull request**

```bash
gh pr view feature/list-sales-filters --json state,baseRefName,commits --jq '"\(.state) \(.baseRefName) \(.commits | length) commits"'
gh pr view feature/list-sales-filters --json title,body --jq '.title + "\n" + .body' | grep -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
git status -sb | head -1
```

Expected:
- `OPEN develop 7 commits`, or 8 with a `test(sales):` commit
- `no attribution lines`
- `## feature/list-sales-filters...origin/feature/list-sales-filters`, with no `ahead` or `behind`

Don't merge it; keep the branch.

- [ ] **Step 7: Clean up**

```bash
docker compose ps -a
tasklist | grep -i 'Ambev.DeveloperEvaluation.WebApi' || echo "no API process left"
rm -rf /tmp/ticket11-checks
```

Expected: nothing listed, then `no API process left`.

- [ ] **Step 8: Report to the user**

1. The Step 1 results. Name any output that differed from this plan and what systematic-debugging found, or say that none did.
2. The pull request URL. Ask the user to send the two new requests once from their editor's `.http` runner.
3. For ticket 12: the `!IsDeleted` query filter also applies to `ApplyFilter`'s query and its count. `ListAsync` doesn't change.
