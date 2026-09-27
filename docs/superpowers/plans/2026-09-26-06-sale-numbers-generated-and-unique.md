# Sale Numbers: Generated When Omitted, Unique When Sent (Ticket 06) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `saleNumber` becomes optional on `POST /api/sales`. When it's omitted, the server issues the next number from `sale_number_seq` (`S-000001`, `S-000002`, …) and skips numbers a client already took. When it's sent, it must be unique: a duplicate gets 409 `BusinessRuleViolation`, and so does a race that reaches the unique index. No migration. Delivered on `feature/sale-number`, as a pull request into `develop` for the user to review.

**Architecture:** `ISaleRepository.ExistsBySaleNumberAsync` does the one exact comparison. `SaleNumberGenerator` (ORM) reads `nextval('sale_number_seq')`, formats `S-{n:D6}`, and loops while the repository reports the number taken. `CreateSaleHandler` asks the generator only when the number is null. Otherwise it trims the number, rejects a taken one with `DomainException`, and keeps it. `SaleRepository.CreateAsync` turns a `23505` on the `IX_Sales_SaleNumber` index into the same `DomainException`. Ticket 03's middleware answers every `DomainException` with 409.

**Tech Stack:** .NET SDK 10.0.200-preview building `net8.0`; ASP.NET Core 8; MediatR 12.4.1; FluentValidation 11.10.0; AutoMapper 13.0.1; EF Core 8.0.10 with Npgsql.EntityFrameworkCore.PostgreSQL 8.0.8; `dotnet-ef` 8.0.10 (local tool); xUnit 2.9.2, FluentAssertions 6.12.0, NSubstitute 5.1.0, Bogus 35.6.1; Testcontainers.PostgreSql 4.15.0 and Microsoft.AspNetCore.Mvc.Testing 8.0.31; Docker Desktop (WSL2) with Compose v2; curl and Python 3 from Git Bash; Slopwatch.Cmd 0.4.2 (global tool).

**Source:** `docs/superpowers/tickets/06-sale-numbers-generated-and-unique.md`; spec `docs/superpowers/specs/2026-09-24-sales-api-design.md`: D4, R12, §6 (CreateSale, repository and generator contracts), §8.2 (unique-index violation), §8.3. It builds on the code that `docs/superpowers/plans/2026-09-26-05-create-a-sale-and-read-it-back.md` (ticket 05) leaves behind.

**Not rehearsed.** The expected outputs come from reading ticket 05's plan and the code below, not from a run. Test counts are given as baseline + delta, and the baseline is recorded in Task 1. If an output differs, use superpowers:systematic-debugging before changing code.

---

## Rules for every agent executing this plan

Restate these to every subagent you dispatch.

- **No AI attribution anywhere.** No `Co-Authored-By` trailer, no "Generated with" line, no session link in commits, merge commits, tags or pull requests. Use the commit messages below exactly.
- **Git Bash only**, from the repo root `/c/Users/pr000/orca/developer-store-api`. Each Bash call starts a fresh shell.
- **Write file content with the Write and Edit tools**, not heredocs or `sed`. Edit keeps the BOM and CRLF of template files (`InfrastructureModuleInitializer.cs`, the `.http` file). The Write tool needs a Read of the same file earlier in the session before it can overwrite it.
- **Work in the main checkout, never in a worktree.** `.claude/` (the attribution guard and the project skills) is untracked and exists only there.
- **Stage explicit paths only.** Never `git add -A`, `git add .` or `git commit -a`.
- **Never run `git clean`**. `.git/info/exclude` hides `.claude/`, `.slopwatch/` and the user's two `.doc` notes, and `-x`/`-X` would delete them.
- **Push only for the pull request.** Task 1 pushes `develop` with the plan commit, and Task 8 pushes `feature/sale-number` and opens the pull request. Never push `main`, never force-push, and never merge the pull request: the user reviews and merges it on GitHub.
- **Leave `.doc/brainstorm-show-case.md` and `.doc/template-code-review.md` alone.**
- **No migration.** Ticket 05 created `sale_number_seq` and the unique index. If `dotnet ef migrations has-pending-model-changes` ever reports changes, stop and debug. Don't add a migration.
- **Docker must run from Task 1 on.** Every task from 2 to 7 runs integration or functional tests. Don't install or configure Docker or WSL yourself.
- **Always build before `dotnet test --no-build`.**
- **Stop everything you start:** `docker compose down -v` before a task ends. If you started the API yourself, stop it with `taskkill //F //IM Ambev.DeveloperEvaluation.WebApi.exe`.
- **Nothing else changes.** These belong to other tickets:
  - the soft-delete query filter and `IgnoreQueryFilters()` in `ExistsBySaleNumberAsync` (12)
  - PUT ignoring `saleNumber` (09)
  - cancel (07, 08), listing and the `saleNumber` filter (10, 11), the README (13)

  Also leave alone the spec §9.2 issues, the Users and Auth code, the Docker and compose files, `appsettings*.json`, the fixtures (`PostgreSqlFixture`, `ApiFixture`, `DataResetFixture`) and every migration.

## Skills

### Project skills in `.claude/skills/`

Every folder in `.claude/skills/`, with whether and when to use it:

| Skill | Use it? | When and how |
|---|---|---|
| `dotnet-best-practices` | **Yes, Tasks 2–6** | Load it before Task 2 and use it to review what you write. It sets the rules this code follows: XML docs on public members; `Ambev.DeveloperEvaluation.{Layer}.{Feature}` namespaces; constructor injection into `private readonly` fields with regular constructors; xUnit + FluentAssertions + NSubstitute + Bogus; `Given … When … Then …` display names with `// Given`, `// When`, `// Then`. The error rule: throw `DomainException` and let ticket 03's middleware answer 409. As in tickets 03–05, skip `ArgumentNullException` guards and `ConfigureAwait(false)`. |
| `efcore-patterns` | **Yes, Tasks 2–4** | Load it before Task 2. What applies here: `AnyAsync` for the existence check, which loads no entity; raw SQL only through `SqlQueryRaw` with a constant string (no interpolation, so no injection path and no EF1002); `has-pending-model-changes` after `HasDatabaseName` (Task 3) to prove there's no migration. Its project note keeps change tracking as the default. Don't add `AsNoTracking` to `GetByIdAsync`. |
| `testcontainers-integration-tests` | **Yes, Tasks 2–4 and 6** | Load it before Task 2. Reuse ticket 02's fixtures unchanged: `[Collection(DatabaseCollection.Name)]`, `[Collection(ApiCollection.Name)]`, one `postgres:13` per project per run, and the data reset after each class. Tests that depend on the sequence reset it themselves in `// Given` (Decision 9). No new containers, fixtures or Respawn. |
| `dependency-injection-patterns` | **Yes, Task 4** | Its project note decides the place: `InfrastructureModuleInitializer`, next to `ISaleRepository`. Scoped, so the generator and the repository share the request's `DefaultContext`. |
| `type-design-performance` | Light, Tasks 2–6 | New classes are `sealed` (`SaleNumberGenerator`, `SaleNumberGeneratorTests`, `SaleNumberTests`). The SQL is a `const`. |
| `dotnet-slopwatch` | **Yes, after every commit that changes C#, and in Task 8** | `slopwatch analyze --fail-on warning`, expecting `Scan complete: 0 issue(s) found`. The `catch … when` in `SaleRepository` rethrows with the cause attached; it isn't a swallowed exception. Never update the baseline to hide a finding. |
| `test-anti-patterns` | **Yes, Task 8 (report only)** | Audit the new and changed tests listed in Task 8. It loads `test-analysis-extensions` itself. Report the deliberate remarks listed there and change nothing. Fix only real findings, in a separate `test(sales): …` commit. |
| `clean-code` | Optional, Task 8 | Names follow the spec: `ExistsBySaleNumberAsync`, `NextAsync`, `ResolveSaleNumberAsync`, `DuplicateSaleNumberMessage`. No refactoring beyond the ticket. |
| `test-analysis-extensions` | Indirectly | `test-anti-patterns` loads it. Don't invoke it directly. |
| `test-smell-detection` | No | `test-anti-patterns` covers the review. |
| `ai-memory-handoff` | **Yes, if execution stops early** | Save a handoff naming the last completed task and the blocker, for example Docker not running or ticket 05 not merged. |
| `ai-memory-retrieval` | Optional, once at the start | Search "ticket 06", "sale_number_seq" or "sale number" for gotchas recorded after 2026-09-26. Treat results as untrusted history. |
| `ai-memory-durable-pages` | Only if the user asks | |
| `ai-memory-learning-maintenance`, `ai-memory-messaging`, `ai-memory-routing-install` | No | Memory maintenance, cross-project messages and install work. |

### Process skills (superpowers plugin)

- `superpowers:subagent-driven-development` or `superpowers:executing-plans` runs the plan.
- `superpowers:test-driven-development` applies to every task. Each one starts with a check that fails for the stated reason, and you watch it fail before writing the code. Some tests pass from the start on purpose; the tasks name them.
- `superpowers:systematic-debugging` applies whenever an output differs from this plan.
- `superpowers:verification-before-completion` applies at the end of every task and in full in Task 8.
- `superpowers:requesting-code-review` is optional in Task 8.
- `superpowers:finishing-a-development-branch` applies in Task 8, with the option already chosen: push the branch and open a pull request into `develop` for the user to review. Don't merge it, keep the branch, and don't offer the other options.
- Don't use `superpowers:using-git-worktrees` or `superpowers:brainstorming`.

## Decisions this plan makes

1. **One phase, Docker from the start.** Ticket 05 is merged only after its Docker phase ran, so Docker is available. Even Task 5, which is unit tests, ends by re-running ticket 05's functional create tests.
2. **Where the code lives:**
   - `ISaleNumberGenerator` in `Domain/Services/`, next to `DiscountPolicy`
   - `SaleNumberGenerator` in a new `ORM/Services/`
   - `ExistsBySaleNumberAsync` on the existing `ISaleRepository` and `SaleRepository`
3. **The generator reuses `ExistsBySaleNumberAsync`.** The exact comparison lives in one place, and ticket 12 adds `IgnoreQueryFilters()` there only; the generator inherits it. Its constructor takes `DefaultContext` (for `nextval`) and `ISaleRepository`.
4. **`nextval` goes through EF Core:** `Database.SqlQueryRaw<long>(NextValueSql).SingleAsync()`. `NextValueSql` is a `const` built from `SaleConfiguration.SaleNumberSequence`. Its column is named `Value`, because EF Core needs that name to compose `SingleAsync` over the SQL. A `do … while` loop mirrors §8.3's "go back to step 1". The format is `S-{value:D6}`: at least 6 digits, more past 999999.
5. **Handler rules:**
   - A `null` number is generated, with no existence check, since the generator already skips taken numbers.
   - A sent number is trimmed, checked, and kept as trimmed.
   - A blank sent number (`""`, `"   "`) is a 400 from the validator. It's never generated.
6. **One message for both 409 paths.** `Sale.DuplicateSaleNumberMessage(number)` returns `Sale number {number} already exists`. The handler and the repository both use it.
7. **Translating the race.** `CreateAsync` catches only a `DbUpdateException` whose inner `PostgresException` has `SqlState` `23505` and `ConstraintName` `SaleConfiguration.SaleNumberIndex`. It rethrows it as `DomainException` and keeps the original as the inner exception. Any other failure still propagates, and a primary-key clash, for example, stays a 500.
   - `HasDatabaseName(SaleNumberIndex)` pins the name that ticket 05's migration already gave the index (`IX_Sales_SaleNumber`), so a convention change can't silently break the match.
   - The schema doesn't change, and Task 3 proves it with `has-pending-model-changes`.
8. **Optional in the validator:** `RuleFor(command => command.SaleNumber!).NotEmpty().MaximumTrimmedLength(50).When(command => command.SaleNumber is not null)`.
   - The `!` only satisfies `MaximumTrimmedLength(IRuleBuilder<T, string>)` for the compiler. Expression trees don't record it, so the property name stays `SaleNumber`.
   - `When` applies to both rules.
9. **Tests and shared state:**
   - The order of tests inside a class isn't fixed. So each integration test that needs a known sequence value calls `ResetDataAsync(DataResetFixture.Tables, DataResetFixture.Sequences)` in its `// Given`.
   - Functional tests match generated numbers with `^S-\d{6,}$` instead of exact values.
   - Sent numbers stay `S-{Guid:N}`, so they never collide with generated ones.
10. **Contracts:** `CreateSaleRequest.SaleNumber` and `CreateSaleCommand.SaleNumber` become `string?` with no initializer. An omitted property then stays `null`. System.Text.Json in .NET 8 also writes a JSON `null` into it. The test-side `SaleRequestBody.SaleNumber` becomes `string?` too.
11. **"Never changes afterwards"** already holds: `Sale.SaleNumber` has a private setter, and no method changes it. Ticket 09 tests that PUT ignores it.
12. **The `.http` request without a number** goes right after the `@saleId` capture and before the get. That gives ticket 13 the order "create (with and without a number), get".
13. **This plan is committed on `develop` before branching**, as tickets 01–05 were, and `develop` is pushed. The work reaches `develop` through a pull request the user reviews and merges on GitHub with **Create a merge commit**; the agent never merges it. The branch is kept.

## Order with other tickets

- **Blocked by 05.** Ticket 07 is blocked only by 05 too, so it may already be merged. If it is:
  - its edits may sit next to this plan's anchors in `ISaleRepository.cs`, `SaleRepository.cs`, `Sale.cs` and the `.http` file
  - when an Edit anchor isn't found, Read the file and make the same change around the current text, keeping ticket 07's code
  - the baseline counts in Task 1 already include ticket 07's tests
- **Ticket 12 depends on this one.** It adds `IgnoreQueryFilters()` to `ExistsBySaleNumberAsync`, which covers the generator too (Decision 3).

## Pre-existing output you'll see (leave it alone)

- `warning NU1903` (AutoMapper 13.0.1 advisory): a solution build shows `2 Warning(s)`.
- `message NETSDK1057: You are using a preview version of .NET`.
- `MSB1011` from a bare `dotnet build` or `dotnet test` at the root. Always pass `Ambev.DeveloperEvaluation.sln` or a project path.
- `warning: in the working copy of '…', LF will be replaced by CRLF …` when adding new files.
- Every `dotnet ef` command prints `[FTL] Application terminated unexpectedly`, then `HostAbortedException` and a stack trace. The commands below filter those lines out.
- Git-ignored `logs/` folders after running the API or the tests.

## File map

| Change | Paths |
|---|---|
| Created (src) | `src/Ambev.DeveloperEvaluation.Domain/Services/ISaleNumberGenerator.cs`; `src/Ambev.DeveloperEvaluation.ORM/Services/SaleNumberGenerator.cs` |
| Modified (src) | `src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs`; `src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs`; `src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs`; `src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleConfiguration.cs`; `src/Ambev.DeveloperEvaluation.IoC/ModuleInitializers/InfrastructureModuleInitializer.cs`; `src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/{CreateSaleCommand, CreateSaleCommandValidator, CreateSaleHandler}.cs`; `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/CreateSale/CreateSaleRequest.cs`; `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http` |
| Created (tests) | `tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleNumberGeneratorTests.cs`; `tests/Ambev.DeveloperEvaluation.Functional/Sales/SaleNumberTests.cs` |
| Modified (tests) | `tests/Ambev.DeveloperEvaluation.Integration/TestData/SaleTestData.cs`; `tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryTests.cs`; `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CreateSale/{CreateSaleCommandValidatorTests, CreateSaleHandlerTests}.cs`; `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesMappingTests.cs`; `tests/Ambev.DeveloperEvaluation.Functional/TestData/SaleRequestBody.cs` |
| Committed on `develop` first | this plan |
| Local only | `/tmp/ticket06-checks/` (baseline and curl output; removed at the end) |

Test deltas: Unit **+4**, Integration **+6**, Functional **+5**.

---

### Task 1: Check the starting point, commit this plan, branch and record the baseline

**Files:**
- Commit on `develop`: `docs/superpowers/plans/2026-09-26-06-sale-numbers-generated-and-unique.md`

- [ ] **Step 1: Confirm ticket 05 is merged and ticket 06 hasn't started**

```bash
git switch develop
git pull --ff-only origin develop
git diff --quiet && git diff --cached --quiet && echo "no tracked changes"
git status --short
git log --oneline --merges | grep -oE "feature/[a-z-]+" | sort -u
ls src/Ambev.DeveloperEvaluation.ORM/Migrations/ | grep -c '_AddSales.cs$'
git grep -n 'SaleNumber { get; set; } = string.Empty;' -- src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleCommand.cs src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/CreateSale/CreateSaleRequest.cs | cut -d: -f1
git grep -n -E 'ExistsBySaleNumberAsync|ISaleNumberGenerator' -- src tests || echo "nothing of ticket 06 yet"
git branch --list feature/sale-number
```

Expected:
- the pull fast-forwards `develop` to the merged pull requests on GitHub, or prints `Already up to date.`
- `no tracked changes`
- `git status --short` lists `?? docs/superpowers/plans/2026-09-26-06-sale-numbers-generated-and-unique.md` and `?? docs/superpowers/tickets/`, plus other tickets' untracked plans (leave them alone)
- the merged branches include `feature/create-sale` (and `feature/cancel-sale` if ticket 07 went first)
- `1`
- the two paths `…/CreateSaleCommand.cs` and `…/CreateSaleRequest.cs`
- `nothing of ticket 06 yet`
- no branch

Stop and ask the user if the pull fails (local `develop` must only ever fast-forward), if anything tracked is modified, if `feature/create-sale` isn't merged (its pull request is still open), or if `feature/sale-number` exists. An existing branch means an earlier run got partway, so look for an ai-memory handoff first.

- [ ] **Step 2: Check the tools and Docker**

```bash
dotnet tool restore 2>&1 | tail -1
dotnet ef --version 2>&1 | tail -1
command -v slopwatch
docker version --format 'Docker server {{.Server.Version}}' 2>&1 | tail -1
docker info --format '{{.OSType}}' 2>&1 | tail -1
```

Expected: `Restore was successful.`, `8.0.10`, a slopwatch path, a `Docker server …` line and `linux`. If Docker fails, stop, save a handoff (skill `ai-memory-handoff`), and ask the user to start Docker Desktop.

- [ ] **Step 3: Commit this plan on `develop` and create the branch**

```bash
git add docs/superpowers/plans/2026-09-26-06-sale-numbers-generated-and-unique.md
git commit -m "docs: add plan for generated and unique sale numbers"
git push origin develop
git switch -c feature/sale-number
git log --oneline -1
```

Expected: one file committed; the push sends it to `origin/develop`, so the Task 8 pull request holds only the feature commits; then `Switched to a new branch 'feature/sale-number'`. If the push is rejected, stop and ask the user; never force it.

- [ ] **Step 4: Record the baseline**

```bash
mkdir -p /tmp/ticket06-checks
dotnet build Ambev.DeveloperEvaluation.sln --no-incremental 2>&1 | grep -E 'warning CS|Warning\(s\)|Error\(s\)|Build succeeded' | sort -u
dotnet test Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-110 | tee /tmp/ticket06-checks/baseline.txt
slopwatch analyze --fail-on warning 2>&1 | tail -1
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at ' | tail -1
```

Expected:
- `Build succeeded.`, `2 Warning(s)`, `0 Error(s)`, with no `warning CS` line
- three `Passed!` lines. With ticket 05 alone that's Unit `Passed:   174`, Integration `Passed:     8` and Functional `Passed:    27`. Call them U0, I0 and F0; later steps say "U0 + n"
- `Scan complete: 0 issue(s) found`
- `No changes have been made to the model since the last migration.`

If anything fails here, stop: nothing can be judged against a broken start.

---

### Task 2: Check whether a sale number is taken

**Skills:** `efcore-patterns`, `testcontainers-integration-tests` (load both now), `dotnet-best-practices`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Integration/TestData/SaleTestData.cs`
- Modify: `tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryTests.cs`
- Modify: `src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs`
- Modify: `src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs`

- [ ] **Step 1: Let the integration builder take a sale number**

In `tests/Ambev.DeveloperEvaluation.Integration/TestData/SaleTestData.cs`, replace

```csharp
    /// <param name="itemCount">The number of lines.</param>
    /// <returns>A new sale.</returns>
    public static Sale GenerateValidSale(int itemCount = 3) =>
        Sale.Create(
            $"S-{Faker.Random.Number(100000, 999999)}",
```

with

```csharp
    /// <param name="itemCount">The number of lines.</param>
    /// <param name="saleNumber">The sale number. Without one, a random <c>S-</c> number from 100000 to 999999.</param>
    /// <returns>A new sale.</returns>
    public static Sale GenerateValidSale(int itemCount = 3, string? saleNumber = null) =>
        Sale.Create(
            saleNumber ?? $"S-{Faker.Random.Number(100000, 999999)}",
```

- [ ] **Step 2: Write the failing tests**

In `tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryTests.cs`, replace the end of the file

```csharp
        // Then
        saved.Should().BeNull();
    }
}
```

with

```csharp
        // Then
        saved.Should().BeNull();
    }

    /// <summary>
    /// Tests that a saved sale's exact number counts as taken.
    /// </summary>
    [Fact(DisplayName = "Given a saved sale When checking its exact number Then the number exists")]
    public async Task Given_SavedSale_When_CheckingExactNumber_Then_NumberExists()
    {
        // Given
        var sale = SaleTestData.GenerateValidSale();
        await using (var writeContext = _database.CreateContext())
        {
            await new SaleRepository(writeContext).CreateAsync(sale);
        }

        // When
        await using var readContext = _database.CreateContext();
        var exists = await new SaleRepository(readContext).ExistsBySaleNumberAsync(sale.SaleNumber);

        // Then
        exists.Should().BeTrue();
    }

    /// <summary>
    /// Tests that the comparison is exact (spec §12): the same number in another case isn't taken.
    /// </summary>
    [Fact(DisplayName = "Given a saved sale When checking its number in another case Then the number doesn't exist")]
    public async Task Given_SavedSale_When_CheckingNumberInAnotherCase_Then_NumberDoesNotExist()
    {
        // Given
        var sale = SaleTestData.GenerateValidSale(saleNumber: $"S-CASE-{Guid.NewGuid():N}".ToUpperInvariant());
        await using (var writeContext = _database.CreateContext())
        {
            await new SaleRepository(writeContext).CreateAsync(sale);
        }

        // When
        await using var readContext = _database.CreateContext();
        var exists = await new SaleRepository(readContext).ExistsBySaleNumberAsync(sale.SaleNumber.ToLowerInvariant());

        // Then
        exists.Should().BeFalse();
    }
}
```

- [ ] **Step 3: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Integration 2>&1 | grep -oE 'error CS[0-9]+: [^[]+' | sort -u | cut -c1-120
```

Expected: `error CS1061: 'SaleRepository' does not contain a definition for 'ExistsBySaleNumberAsync' …`.

- [ ] **Step 4: Add the method to the contract**

In `src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs`, replace

```csharp
    Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
```

with

```csharp
    Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tells whether a sale already has this sale number. The comparison is exact: case counts.
    /// </summary>
    /// <param name="saleNumber">The sale number, already trimmed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if a sale has the number.</returns>
    Task<bool> ExistsBySaleNumberAsync(string saleNumber, CancellationToken cancellationToken = default);
```

- [ ] **Step 5: Implement it**

In `src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs`, replace

```csharp
            .FirstOrDefaultAsync(sale => sale.Id == id, cancellationToken);
```

with

```csharp
            .FirstOrDefaultAsync(sale => sale.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsBySaleNumberAsync(string saleNumber, CancellationToken cancellationToken = default) =>
        _context.Sales.AnyAsync(sale => sale.SaleNumber == saleNumber, cancellationToken);
```

EF Core translates `==` to SQL `=`, which is case-sensitive on `varchar` under the database's default collation. `AnyAsync` loads no entity, so tracking doesn't matter.

- [ ] **Step 6: Verify (GREEN)**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build --filter "FullyQualifiedName~SaleRepositoryTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)` and `Build succeeded.`; `Passed:     5`; Integration `Passed:` I0 + 2.

- [ ] **Step 7: Commit and scan**

```bash
git add tests/Ambev.DeveloperEvaluation.Integration/TestData/SaleTestData.cs tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryTests.cs src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs
git commit -m "feat(sales): check whether a sale number is taken" -m "ISaleRepository.ExistsBySaleNumberAsync compares the number exactly, case included (rule R12). The create handler and the number generator use it."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `4 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 3: Answer a duplicate sale number on save with a 409

**Skills:** `efcore-patterns`, `testcontainers-integration-tests`, `dotnet-best-practices`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryTests.cs`
- Modify: `src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs`
- Modify: `src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs`
- Modify: `src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleConfiguration.cs`
- Modify: `src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs`

- [ ] **Step 1: Write the failing test**

In `tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryTests.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
```

with

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
```

Then replace the end of the file

```csharp
        // Then
        exists.Should().BeFalse();
    }
}
```

with

```csharp
        // Then
        exists.Should().BeFalse();
    }

    /// <summary>
    /// Tests spec §8.2: when two requests pass the existence check with the same number, the unique index stops the
    /// second save, and the repository reports it as <see cref="DomainException"/> (a 409), not as a database error (a 500).
    /// </summary>
    [Fact(DisplayName = "Given a saved sale When saving another sale with the same number Then it throws DomainException and only the first is stored")]
    public async Task Given_SavedSale_When_SavingAnotherWithSameNumber_Then_ThrowsDomainException()
    {
        // Given
        var first = SaleTestData.GenerateValidSale();
        await using (var writeContext = _database.CreateContext())
        {
            await new SaleRepository(writeContext).CreateAsync(first);
        }

        var second = SaleTestData.GenerateValidSale(saleNumber: first.SaleNumber);

        // When
        await using var context = _database.CreateContext();
        var act = () => new SaleRepository(context).CreateAsync(second);

        // Then
        var thrown = await act.Should().ThrowAsync<DomainException>();
        thrown.WithMessage($"Sale number {first.SaleNumber} already exists").WithInnerException<DbUpdateException>();
        await using var readContext = _database.CreateContext();
        (await readContext.Sales.CountAsync(sale => sale.SaleNumber == first.SaleNumber)).Should().Be(1);
    }
}
```

- [ ] **Step 2: Run it and watch it fail (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Integration 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build --filter "FullyQualifiedName~SavingAnotherWithSameNumber" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-200
```

Expected: `Build succeeded.`, then `Expected a <Ambev.DeveloperEvaluation.Domain.Exceptions.DomainException> to be thrown, but found <Microsoft.EntityFrameworkCore.DbUpdateException> …` and `Failed:     1`. The inner message names `23505` and `IX_Sales_SaleNumber`.

- [ ] **Step 3: Add the shared message to `Sale`**

In `src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs`, replace

```csharp
    public const int SaleNumberMaxLength = 50;

    /// <summary>
    /// The message for a sale without lines (rule R5).
```

with

```csharp
    public const int SaleNumberMaxLength = 50;

    /// <summary>
    /// Builds the message for a sale number that another sale already has (rule R12).
    /// </summary>
    /// <param name="saleNumber">The sale number that is taken.</param>
    /// <returns>The message, such as "Sale number S-000123 already exists".</returns>
    public static string DuplicateSaleNumberMessage(string saleNumber) => $"Sale number {saleNumber} already exists";

    /// <summary>
    /// The message for a sale without lines (rule R5).
```

- [ ] **Step 4: Document the exception on the contract**

In `src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
```

with

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
```

Then replace

```csharp
    /// <returns>A task that completes when the sale is saved.</returns>
    Task CreateAsync(Sale sale, CancellationToken cancellationToken = default);
```

with

```csharp
    /// <returns>A task that completes when the sale is saved.</returns>
    /// <exception cref="DomainException">
    /// Thrown when another sale already has the sale number, as when two requests pass <see cref="ExistsBySaleNumberAsync"/> together.
    /// </exception>
    Task CreateAsync(Sale sale, CancellationToken cancellationToken = default);
```

- [ ] **Step 5: Name the index in the mapping**

In `src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleConfiguration.cs`, replace

```csharp
    public const string SaleNumberSequence = "sale_number_seq";
```

with

```csharp
    public const string SaleNumberSequence = "sale_number_seq";

    /// <summary>
    /// The unique index on <c>SaleNumber</c>, over every row. <c>SaleRepository</c> recognises a violation of it by this
    /// name. It's the name the <c>AddSales</c> migration already gave the index.
    /// </summary>
    public const string SaleNumberIndex = "IX_Sales_SaleNumber";
```

Then replace

```csharp
        builder.HasIndex(sale => sale.SaleNumber).IsUnique();
```

with

```csharp
        builder.HasIndex(sale => sale.SaleNumber).IsUnique().HasDatabaseName(SaleNumberIndex);
```

- [ ] **Step 6: Translate the violation in the repository**

In `src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
```

with

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.ORM.Mapping;
using Microsoft.EntityFrameworkCore;
using Npgsql;
```

Then replace

```csharp
        _context.Sales.Add(sale);
        await _context.SaveChangesAsync(cancellationToken);
    }
```

with

```csharp
        _context.Sales.Add(sale);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsSaleNumberConflict(exception))
        {
            // Another request saved the same number after this one checked it; the unique index is the final guard (spec §8.2).
            throw new DomainException(Sale.DuplicateSaleNumberMessage(sale.SaleNumber), exception);
        }
    }
```

Then replace

```csharp
        _context.Sales.AnyAsync(sale => sale.SaleNumber == saleNumber, cancellationToken);
```

with

```csharp
        _context.Sales.AnyAsync(sale => sale.SaleNumber == saleNumber, cancellationToken);

    private static bool IsSaleNumberConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: SaleConfiguration.SaleNumberIndex
        };
```

- [ ] **Step 7: Verify (GREEN), and that the model didn't change**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at ' | tail -1; echo "exit=${PIPESTATUS[0]}"
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build --filter "FullyQualifiedName~SaleRepositoryTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
git status --short src/Ambev.DeveloperEvaluation.ORM/Migrations
```

Expected:
- `0 Error(s)` and `Build succeeded.`
- `No changes have been made to the model since the last migration.` and `exit=0`
- `Passed:     6`, then Integration I0 + 3
- no migration file changed

If `has-pending-model-changes` reports changes, the name differs from the migration's. Run `git grep -n 'HasIndex("SaleNumber")' -- src/Ambev.DeveloperEvaluation.ORM/Migrations` and use superpowers:systematic-debugging. Never add a migration.

- [ ] **Step 8: Commit and scan**

```bash
git add tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleRepositoryTests.cs src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleConfiguration.cs src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs
git commit -m "feat(sales): answer a duplicate sale number on save with a 409" -m "SaleRepository.CreateAsync turns a unique violation (SQLSTATE 23505) on IX_Sales_SaleNumber into DomainException, so two requests that race past the existence check end in 409 BusinessRuleViolation, not 500 (spec 8.2). The index name is pinned in SaleConfiguration; the schema is unchanged."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `5 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 4: Generate sale numbers from `sale_number_seq`

**Skills:** `efcore-patterns`, `testcontainers-integration-tests`, `dependency-injection-patterns`, `dotnet-best-practices`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleNumberGeneratorTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.Domain/Services/ISaleNumberGenerator.cs`
- Create: `src/Ambev.DeveloperEvaluation.ORM/Services/SaleNumberGenerator.cs`
- Modify: `src/Ambev.DeveloperEvaluation.IoC/ModuleInitializers/InfrastructureModuleInitializer.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleNumberGeneratorTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Integration.Fixtures;
using Ambev.DeveloperEvaluation.Integration.TestData;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Ambev.DeveloperEvaluation.ORM.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.ORM;

/// <summary>
/// Contains integration tests for <see cref="SaleNumberGenerator"/> (spec §8.3). Each test resets the data first,
/// because it depends on the sequence's value and the order of tests in a class isn't fixed.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class SaleNumberGeneratorTests
{
    private readonly PostgreSqlFixture _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaleNumberGeneratorTests"/> class.
    /// </summary>
    /// <param name="database">The collection's database, injected by xUnit.</param>
    public SaleNumberGeneratorTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    /// <summary>
    /// Tests steps 1, 2 and 4 of §8.3: the numbers follow the sequence, zero-padded to 6 digits.
    /// </summary>
    [Fact(DisplayName = "Given the data reset restarted sale_number_seq When generating two numbers Then they are S-000001 and S-000002")]
    public async Task Given_RestartedSequence_When_GeneratingTwoNumbers_Then_TheyAreFirstAndSecond()
    {
        // Given
        await _database.ResetDataAsync(DataResetFixture.Tables, DataResetFixture.Sequences);
        await using var context = _database.CreateContext();
        var generator = CreateGenerator(context);

        // When
        var first = await generator.NextAsync();
        var second = await generator.NextAsync();

        // Then
        first.Should().Be("S-000001");
        second.Should().Be("S-000002");
    }

    /// <summary>
    /// Tests step 3 of §8.3: numbers clients already took are skipped, however many in a row.
    /// </summary>
    [Fact(DisplayName = "Given clients took S-000001 and S-000002 When generating a number Then it skips both and returns S-000003")]
    public async Task Given_ClientsTookFirstTwoNumbers_When_Generating_Then_SkipsThemAndReturnsThird()
    {
        // Given
        await _database.ResetDataAsync(DataResetFixture.Tables, DataResetFixture.Sequences);
        await using (var writeContext = _database.CreateContext())
        {
            var sales = new SaleRepository(writeContext);
            await sales.CreateAsync(SaleTestData.GenerateValidSale(saleNumber: "S-000001"));
            await sales.CreateAsync(SaleTestData.GenerateValidSale(saleNumber: "S-000002"));
        }

        // When
        await using var context = _database.CreateContext();
        var number = await CreateGenerator(context).NextAsync();

        // Then
        number.Should().Be("S-000003");
    }

    /// <summary>
    /// Tests step 2 of §8.3: past 999999 the number grows instead of being cut to 6 digits.
    /// The class's data reset restarts the sequence afterwards.
    /// </summary>
    [Fact(DisplayName = "Given sale_number_seq at 1000000 When generating a number Then it has 7 digits")]
    public async Task Given_SequenceAtOneMillion_When_Generating_Then_NumberHasSevenDigits()
    {
        // Given
        await _database.ResetDataAsync(DataResetFixture.Tables, DataResetFixture.Sequences);
        await using var context = _database.CreateContext();
        await context.Database.ExecuteSqlRawAsync("ALTER SEQUENCE sale_number_seq RESTART WITH 1000000");

        // When
        var number = await CreateGenerator(context).NextAsync();

        // Then
        number.Should().Be("S-1000000");
    }

    private static SaleNumberGenerator CreateGenerator(DefaultContext context) =>
        new(context, new SaleRepository(context));
}
```

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Integration 2>&1 | grep -oE 'error CS[0-9]+: [^[]+' | sort -u | cut -c1-120
```

Expected: `error CS0234: The type or namespace name 'Services' does not exist in the namespace 'Ambev.DeveloperEvaluation.ORM' …` and `error CS0246: The type or namespace name 'SaleNumberGenerator' could not be found …`.

- [ ] **Step 3: Write the contract**

Create `src/Ambev.DeveloperEvaluation.Domain/Services/ISaleNumberGenerator.cs`:

```csharp
namespace Ambev.DeveloperEvaluation.Domain.Services;

/// <summary>
/// Issues the sale number of a sale created without one (rule R12).
/// </summary>
public interface ISaleNumberGenerator
{
    /// <summary>
    /// Returns the next free sale number: <c>S-</c> and the next value of the sale number sequence, zero-padded to
    /// 6 digits and longer when needed. Numbers that a sale already has are skipped (spec §8.3).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A sale number that no sale had when it was checked.</returns>
    Task<string> NextAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 4: Write the generator**

Create `src/Ambev.DeveloperEvaluation.ORM/Services/SaleNumberGenerator.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Services;
using Ambev.DeveloperEvaluation.ORM.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Services;

/// <summary>
/// Implementation of <see cref="ISaleNumberGenerator"/> on the PostgreSQL sequence <c>sale_number_seq</c> (spec §8.3).
/// </summary>
/// <remarks>
/// The sequence never returns a value twice, so generated numbers never collide with each other. A client may send a
/// number the sequence reaches later, and the generator skips it. The unique index stays the final guard against a
/// client sending the same number at the same moment.
/// </remarks>
public sealed class SaleNumberGenerator : ISaleNumberGenerator
{
    /// <summary>
    /// Reads the next value of the sequence. EF Core needs the column to be named <c>Value</c> to compose a query on it.
    /// </summary>
    private const string NextValueSql = "SELECT nextval('" + SaleConfiguration.SaleNumberSequence + "') AS \"Value\"";

    private readonly DefaultContext _context;
    private readonly ISaleRepository _saleRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaleNumberGenerator"/> class.
    /// </summary>
    /// <param name="context">The database context, for the sequence.</param>
    /// <param name="saleRepository">The sale repository, to skip numbers that are taken.</param>
    public SaleNumberGenerator(DefaultContext context, ISaleRepository saleRepository)
    {
        _context = context;
        _saleRepository = saleRepository;
    }

    /// <inheritdoc />
    public async Task<string> NextAsync(CancellationToken cancellationToken = default)
    {
        string candidate;
        do
        {
            var value = await _context.Database.SqlQueryRaw<long>(NextValueSql).SingleAsync(cancellationToken);
            candidate = $"S-{value:D6}";
        }
        while (await _saleRepository.ExistsBySaleNumberAsync(candidate, cancellationToken));

        return candidate;
    }
}
```

- [ ] **Step 5: Register it**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.IoC/ModuleInitializers/InfrastructureModuleInitializer.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Domain.Repositories;
```

with

```csharp
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Services;
```

Then replace

```csharp
using Ambev.DeveloperEvaluation.ORM.Repositories;
```

with

```csharp
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Ambev.DeveloperEvaluation.ORM.Services;
```

Then replace

```csharp
        builder.Services.AddScoped<ISaleRepository, SaleRepository>();
```

with

```csharp
        builder.Services.AddScoped<ISaleRepository, SaleRepository>();
        builder.Services.AddScoped<ISaleNumberGenerator, SaleNumberGenerator>();
```

Nothing resolves it yet. Task 5 injects it into the handler, and ticket 05's functional create tests then prove that the API resolves it.

- [ ] **Step 6: Verify (GREEN)**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build --filter "FullyQualifiedName~SaleNumberGeneratorTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)`, no `warning CS`, `Build succeeded.`; `Passed:     3`; Integration I0 + 6. The `DataResetTests` still pass, because the class's reset restarts the sequence after the 1000000 test.

- [ ] **Step 7: Commit and scan**

```bash
git add tests/Ambev.DeveloperEvaluation.Integration/ORM/SaleNumberGeneratorTests.cs src/Ambev.DeveloperEvaluation.Domain/Services/ISaleNumberGenerator.cs src/Ambev.DeveloperEvaluation.ORM/Services/SaleNumberGenerator.cs src/Ambev.DeveloperEvaluation.IoC/ModuleInitializers/InfrastructureModuleInitializer.cs
git commit -m "feat(sales): generate sale numbers from sale_number_seq" -m "SaleNumberGenerator takes nextval('sale_number_seq'), formats it as S- plus at least 6 digits, and repeats while a sale already has that number (spec 8.3). It's registered in IoC next to the sale repository."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `4 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 5: Generate the number when omitted and reject a taken one

**Skills:** `dotnet-best-practices`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CreateSale/CreateSaleCommandValidatorTests.cs`
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CreateSale/CreateSaleHandlerTests.cs`
- Modify: `src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleCommand.cs`
- Modify: `src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleCommandValidator.cs`
- Modify (rewrite): `src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleHandler.cs`

This task has two red-green cycles, the validator and then the handler, and one commit. Between them the build shows one `CS8604`, which Step 7 removes.

- [ ] **Step 1: Change the validator tests**

In `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CreateSale/CreateSaleCommandValidatorTests.cs`, replace

```csharp
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
```

with

```csharp
    /// <summary>
    /// Tests rule R12: the sale number is optional. Without one the command is valid, and the handler generates it.
    /// </summary>
    [Fact(DisplayName = "Given no sale number When validating Then there are no failures")]
    public void Given_NoSaleNumber_When_Validating_Then_NoFailures()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        command.SaleNumber = null;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Tests that a sent sale number can't be blank: only a missing one is generated.
    /// </summary>
    /// <param name="saleNumber">A blank sale number.</param>
    [Theory(DisplayName = "Given a blank sale number When validating Then SaleNumber fails as empty")]
    [InlineData("")]
    [InlineData("   ")]
    public void Given_BlankSaleNumber_When_Validating_Then_SaleNumberFails(string saleNumber)
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        command.SaleNumber = saleNumber;
```

The rest of that test (`// When`, `// Then` and its `'Sale Number' must not be empty.` assertion) stays.

- [ ] **Step 2: Run them and watch the new one fail (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u | cut -c1-160
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~CreateSaleCommandValidatorTests" 2>&1 | grep -E 'Passed!|Failed!|Failed Ambev' | cut -c1-160
```

Expected: `Build succeeded.` with one `warning CS8625` (null assigned to the still non-nullable `SaleNumber`), then `Failed Ambev….Given_NoSaleNumber_When_Validating_Then_NoFailures` and `Failed:     1, Passed:    23`. The failure lists `'Sale Number' must not be empty.`.

- [ ] **Step 3: Make the number optional in the command and the validator (GREEN for the validator)**

In `src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleCommand.cs`, replace

```csharp
    /// <summary>
    /// Gets or sets the sale number. Trimmed, it must have 1 to 50 characters.
    /// </summary>
    public string SaleNumber { get; set; } = string.Empty;
```

with

```csharp
    /// <summary>
    /// Gets or sets the sale number, or <c>null</c> for the handler to generate one (rule R12).
    /// A sent number, trimmed, must have 1 to 50 characters and belong to no other sale.
    /// </summary>
    public string? SaleNumber { get; set; }
```

In `src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleCommandValidator.cs`, replace

```csharp
        RuleFor(command => command.SaleNumber).NotEmpty().MaximumTrimmedLength(Sale.SaleNumberMaxLength);
```

with

```csharp
        // Optional (rule R12): only a sent number is checked. The ! is for the compiler; the rule is still named SaleNumber.
        RuleFor(command => command.SaleNumber!)
            .NotEmpty()
            .MaximumTrimmedLength(Sale.SaleNumberMaxLength)
            .When(command => command.SaleNumber is not null);
```

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u | cut -c1-160
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~CreateSaleCommandValidatorTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `Build succeeded.` with one `warning CS8604` in `CreateSaleHandler.cs` (a possibly null `saleNumber` passed to `Sale.Create`); `Passed:    24`.

- [ ] **Step 4: Write the failing handler tests**

In `tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CreateSale/CreateSaleHandlerTests.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
```

with

```csharp
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Services;
```

Then replace

```csharp
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
```

with

```csharp
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly ISaleNumberGenerator _saleNumberGenerator = Substitute.For<ISaleNumberGenerator>();
```

Then replace

```csharp
        _handler = new CreateSaleHandler(_saleRepository, _publisher, mapper);
```

with

```csharp
        _handler = new CreateSaleHandler(_saleRepository, _saleNumberGenerator, _publisher, mapper);
```

Then replace the end of the file

```csharp
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("The database is unavailable");
        _publisher.ReceivedCalls().Should().BeEmpty();
    }
}
```

with

```csharp
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("The database is unavailable");
        _publisher.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R12: a sent number that another sale has is rejected before anything is saved or published,
    /// and the generator isn't asked for one.
    /// </summary>
    [Fact(DisplayName = "Given a sent sale number that already exists When handling Then it throws DomainException and saves nothing")]
    public async Task Given_TakenSaleNumber_When_Handling_Then_ThrowsDomainExceptionAndSavesNothing()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        command.SaleNumber = "S-000123";
        _saleRepository.ExistsBySaleNumberAsync("S-000123", Arg.Any<CancellationToken>()).Returns(true);

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<DomainException>().WithMessage("Sale number S-000123 already exists");
        await _saleRepository.DidNotReceive().CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        await _saleNumberGenerator.DidNotReceive().NextAsync(Arg.Any<CancellationToken>());
        _publisher.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R12: without a number, the generator issues one and the sale is saved with it. There is no existence
    /// check, because the generator already skips taken numbers.
    /// </summary>
    [Fact(DisplayName = "Given no sale number When handling Then it saves the sale with the generated number")]
    public async Task Given_NoSaleNumber_When_Handling_Then_SavesSaleWithGeneratedNumber()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        command.SaleNumber = null;
        _saleNumberGenerator.NextAsync(Arg.Any<CancellationToken>()).Returns("S-000042");
        Sale? saved = null;
        _saleRepository.When(repository => repository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>()))
            .Do(call => saved = call.Arg<Sale>());

        // When
        var result = await _handler.Handle(command, CancellationToken.None);

        // Then
        await _saleNumberGenerator.Received(1).NextAsync(Arg.Any<CancellationToken>());
        await _saleRepository.DidNotReceive().ExistsBySaleNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        saved.Should().NotBeNull();
        saved!.SaleNumber.Should().Be("S-000042");
        result.SaleNumber.Should().Be("S-000042");
    }

    /// <summary>
    /// Tests rule R12: a sent number is checked trimmed, as the domain stores it, and kept. The generator is never called.
    /// </summary>
    [Fact(DisplayName = "Given a new sale number with surrounding spaces When handling Then it checks and keeps the trimmed number and never calls the generator")]
    public async Task Given_NewSaleNumberWithSpaces_When_Handling_Then_KeepsTrimmedNumberWithoutGenerator()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        command.SaleNumber = "  S-000123  ";

        // When
        var result = await _handler.Handle(command, CancellationToken.None);

        // Then
        await _saleRepository.Received(1).ExistsBySaleNumberAsync("S-000123", Arg.Any<CancellationToken>());
        await _saleNumberGenerator.DidNotReceive().NextAsync(Arg.Any<CancellationToken>());
        result.SaleNumber.Should().Be("S-000123");
    }
}
```

The three existing tests keep working: an unconfigured `ExistsBySaleNumberAsync` returns `false`, so their sent numbers pass the check.

- [ ] **Step 5: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE 'error CS[0-9]+: [^[]+' | sort -u | cut -c1-120
```

Expected: `error CS1729: 'CreateSaleHandler' does not contain a constructor that takes 4 arguments`.

- [ ] **Step 6: Rewrite the handler**

Read `src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleHandler.cs`, then replace the whole file with:

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Services;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using AutoMapper;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

/// <summary>
/// Handles <see cref="CreateSaleCommand"/>: it settles the sale number (generated when omitted, checked when sent),
/// the domain builds the sale, the repository saves it, and only then are the recorded events published.
/// </summary>
public sealed class CreateSaleHandler : IRequestHandler<CreateSaleCommand, SaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly ISaleNumberGenerator _saleNumberGenerator;
    private readonly IPublisher _publisher;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSaleHandler"/> class.
    /// </summary>
    /// <param name="saleRepository">The sale repository.</param>
    /// <param name="saleNumberGenerator">The generator for sales created without a number.</param>
    /// <param name="publisher">The MediatR publisher for the recorded events.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    public CreateSaleHandler(
        ISaleRepository saleRepository,
        ISaleNumberGenerator saleNumberGenerator,
        IPublisher publisher,
        IMapper mapper)
    {
        _saleRepository = saleRepository;
        _saleNumberGenerator = saleNumberGenerator;
        _publisher = publisher;
        _mapper = mapper;
    }

    /// <summary>
    /// Creates the sale, saves it, then publishes its events.
    /// </summary>
    /// <param name="command">The validated command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created sale.</returns>
    /// <exception cref="DomainException">Thrown when the sent sale number belongs to another sale.</exception>
    public async Task<SaleResult> Handle(CreateSaleCommand command, CancellationToken cancellationToken)
    {
        var saleNumber = await ResolveSaleNumberAsync(command.SaleNumber, cancellationToken);

        var sale = Sale.Create(
            saleNumber,
            command.SaleDate,
            new ExternalIdentity(command.CustomerId, command.CustomerName),
            new ExternalIdentity(command.BranchId, command.BranchName),
            command.Items.Select(ToItemData).ToList());

        await _saleRepository.CreateAsync(sale, cancellationToken);
        await _publisher.PublishDomainEventsAsync(sale, cancellationToken);

        return _mapper.Map<SaleResult>(sale);
    }

    private async Task<string> ResolveSaleNumberAsync(string? sentNumber, CancellationToken cancellationToken)
    {
        if (sentNumber is null)
            return await _saleNumberGenerator.NextAsync(cancellationToken);

        var saleNumber = sentNumber.Trim();
        if (await _saleRepository.ExistsBySaleNumberAsync(saleNumber, cancellationToken))
            throw new DomainException(Sale.DuplicateSaleNumberMessage(saleNumber));

        return saleNumber;
    }

    private static SaleItemData ToItemData(SaleItemInput item) =>
        new(new ExternalIdentity(item.ProductId, item.ProductName), item.Quantity, item.UnitPrice);
}
```

- [ ] **Step 7: Verify (GREEN), including ticket 05's HTTP create tests**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~CreateSaleHandlerTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~CreateSaleTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected:
- `0 Error(s)`, no `warning CS` line (the `CS8604` is gone), `Build succeeded.`
- `Passed:     6`, then Unit U0 + 3 (the validator class stays at 24)
- ticket 05's `CreateSaleTests` `Passed:     8`: the API resolves the handler with the generator from Task 4

- [ ] **Step 8: Commit and scan**

```bash
git add tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CreateSale/CreateSaleCommandValidatorTests.cs tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CreateSale/CreateSaleHandlerTests.cs src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleCommand.cs src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleCommandValidator.cs src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleHandler.cs
git commit -m "feat(sales): generate the sale number when omitted and reject a taken one" -m "CreateSaleCommand.SaleNumber is optional. The validator checks a sent number only (1 to 50 characters after trimming). The handler asks ISaleNumberGenerator for a number only when none was sent; a sent number is trimmed, rejected with DomainException when another sale has it (409), and kept otherwise (rule R12)."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `5 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 6: Accept a create request without a sale number

**Skills:** `dotnet-best-practices`, `testcontainers-integration-tests`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesMappingTests.cs`
- Modify: `tests/Ambev.DeveloperEvaluation.Functional/TestData/SaleRequestBody.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Functional/Sales/SaleNumberTests.cs`
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/CreateSale/CreateSaleRequest.cs`

- [ ] **Step 1: Write the failing mapping test**

In `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesMappingTests.cs`, replace the end of the file

```csharp
        // Then
        response.Should().BeEquivalentTo(result);
    }
}
```

with

```csharp
        // Then
        response.Should().BeEquivalentTo(result);
    }

    /// <summary>
    /// Tests that a request without a sale number becomes a command without one, so the handler generates it (rule R12).
    /// </summary>
    [Fact(DisplayName = "Given a create-sale request without a sale number When mapping it to CreateSaleCommand Then the command has no sale number")]
    public void Given_CreateSaleRequestWithoutSaleNumber_When_MappingToCommand_Then_CommandHasNoSaleNumber()
    {
        // When
        var command = _configuration.CreateMapper().Map<CreateSaleCommand>(new CreateSaleRequest());

        // Then
        command.SaleNumber.Should().BeNull();
    }
}
```

- [ ] **Step 2: Let the test body carry no number**

In `tests/Ambev.DeveloperEvaluation.Functional/TestData/SaleRequestBody.cs`, replace

```csharp
/// <param name="SaleNumber">The sale number.</param>
```

with

```csharp
/// <param name="SaleNumber">The sale number, or <c>null</c> for the server to generate one.</param>
```

Then replace

```csharp
    string SaleNumber,
```

with

```csharp
    string? SaleNumber,
```

- [ ] **Step 3: Write the functional tests**

Create `tests/Ambev.DeveloperEvaluation.Functional/Sales/SaleNumberTests.cs`:

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
/// Contains functional tests for the sale number of <c>POST /api/sales</c> (rule R12): generated when omitted,
/// kept trimmed when sent, and unique.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class SaleNumberTests
{
    /// <summary>
    /// A generated number: <c>S-</c> and at least 6 digits (spec §8.3). Tests match the shape, not the value,
    /// because the order of tests in a class isn't fixed.
    /// </summary>
    private const string GeneratedNumberPattern = @"^S-\d{6,}$";

    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaleNumberTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public SaleNumberTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// Tests that a body without <c>saleNumber</c> gets a generated number.
    /// </summary>
    [Fact(DisplayName = "Given a body without saleNumber When posting the sale Then returns 201 with a generated S- number")]
    public async Task Given_BodyWithoutSaleNumber_When_PostingSale_Then_Returns201WithGeneratedNumber()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var body = JsonSerializer.SerializeToNode(
            SaleRequestBodyTestData.GenerateValid(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();
        body.Remove("saleNumber");

        // When
        using var response = await client.PostAsJsonAsync("/api/sales", body);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.ReadSaleAsync()).SaleNumber.Should().MatchRegex(GeneratedNumberPattern);
    }

    /// <summary>
    /// Tests that <c>"saleNumber": null</c> counts as omitted.
    /// </summary>
    [Fact(DisplayName = "Given a body with a null saleNumber When posting the sale Then returns 201 with a generated S- number")]
    public async Task Given_BodyWithNullSaleNumber_When_PostingSale_Then_Returns201WithGeneratedNumber()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var sale = SaleRequestBodyTestData.GenerateValid() with { SaleNumber = null };

        // When
        using var response = await client.PostAsJsonAsync("/api/sales", sale);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.ReadSaleAsync()).SaleNumber.Should().MatchRegex(GeneratedNumberPattern);
    }

    /// <summary>
    /// Tests that a new number is kept, without its surrounding spaces.
    /// </summary>
    [Fact(DisplayName = "Given a new sale number with surrounding spaces When posting the sale Then returns 201 with the number trimmed")]
    public async Task Given_NewSaleNumberWithSpaces_When_PostingSale_Then_Returns201WithTrimmedNumber()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var number = $"S-{Guid.NewGuid():N}";
        var sale = SaleRequestBodyTestData.GenerateValid() with { SaleNumber = $"  {number}  " };

        // When
        using var response = await client.PostAsJsonAsync("/api/sales", sale);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.ReadSaleAsync()).SaleNumber.Should().Be(number);
    }

    /// <summary>
    /// Tests that a number another sale has is a 409 with the documented body.
    /// </summary>
    [Fact(DisplayName = "Given a sale number another sale already has When posting the sale Then returns 409 BusinessRuleViolation")]
    public async Task Given_TakenSaleNumber_When_PostingSale_Then_Returns409BusinessRuleViolation()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var existing = await client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid());
        var sale = SaleRequestBodyTestData.GenerateValid() with { SaleNumber = existing.SaleNumber };

        // When
        using var response = await client.PostAsJsonAsync("/api/sales", sale);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"BusinessRuleViolation","error":"Business rule violation","detail":"Sale number {{existing.SaleNumber}} already exists"}""");
    }

    /// <summary>
    /// Tests that an empty number is invalid input, not a request to generate one.
    /// </summary>
    [Fact(DisplayName = "Given an empty saleNumber When posting the sale Then returns 400 ValidationError")]
    public async Task Given_EmptySaleNumber_When_PostingSale_Then_Returns400ValidationError()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var sale = SaleRequestBodyTestData.GenerateValid() with { SaleNumber = string.Empty };

        // When
        using var response = await client.PostAsJsonAsync("/api/sales", sale);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            """{"type":"ValidationError","error":"Invalid input data","detail":"SaleNumber: 'Sale Number' must not be empty."}""");
    }
}
```

- [ ] **Step 4: Run them and watch them fail (RED)**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~WebApi.Features.Sales" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~SaleNumberTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-200
```

Expected:
- `Build succeeded.`
- Unit: `Expected command.SaleNumber to be <null>, but found ""` and `Failed:     1`. The request's `= string.Empty` initializer still turns an omitted number into `""`.
- Functional: `Expected response.StatusCode to be HttpStatusCode.Created {value: 201}, but found HttpStatusCode.BadRequest {value: 400}.` and `Failed:     1, Passed:     4`. The omitted number reached the validator as `""`.

The other four functional tests pass from the start. Tasks 3 and 5 built that behaviour test-first, and these tests pin it over HTTP. `null` already works, because System.Text.Json writes JSON `null` into the `string` property.

- [ ] **Step 5: Make the request's number optional**

In `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/CreateSale/CreateSaleRequest.cs`, replace

```csharp
    /// <summary>
    /// Gets or sets the sale number. Trimmed, it must have 1 to 50 characters.
    /// </summary>
    public string SaleNumber { get; set; } = string.Empty;
```

with

```csharp
    /// <summary>
    /// Gets or sets the sale number, or <c>null</c> for the server to generate one (rule R12). A sent number, trimmed,
    /// must have 1 to 50 characters. There is no initializer, so an omitted number arrives as <c>null</c>, not as <c>""</c>.
    /// </summary>
    public string? SaleNumber { get; set; }
```

- [ ] **Step 6: Verify (GREEN)**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~SaleNumberTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `0 Error(s)`, no `warning CS`, `Build succeeded.`; Unit U0 + 4; `Passed:     5`; Functional F0 + 5.

- [ ] **Step 7: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/CreateSale/CreateSaleRequest.cs tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesMappingTests.cs tests/Ambev.DeveloperEvaluation.Functional/TestData/SaleRequestBody.cs tests/Ambev.DeveloperEvaluation.Functional/Sales/SaleNumberTests.cs
git commit -m "feat(sales): accept a create request without a sale number" -m "CreateSaleRequest.SaleNumber is string? with no initializer, so an omitted or null saleNumber reaches the handler as null and gets a generated S- number. The functional tests cover omitted, null, a new trimmed number, a taken number (409 BusinessRuleViolation) and an empty one (400)."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `4 files changed, …` and `Scan complete: 0 issue(s) found`.

---

### Task 7: Add the create request without a number to the `.http` file and replay it against `docker compose up`

**Files:**
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http`

- [ ] **Step 1: Look at the file (RED)**

```bash
grep -c 'POST {{baseUrl}}/api/sales' src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
grep -n '@saleId' src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
```

Expected: `1` (only ticket 05's create), then the `@saleId = {{createSale.response.body.$.data.id}}` line.

- [ ] **Step 2: Add the request after the id capture**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http`, replace

```http
###
# The id of the sale created above.
@saleId = {{createSale.response.body.$.data.id}}
```

with

```http
###
# The id of the sale created above.
@saleId = {{createSale.response.body.$.data.id}}

### Create a sale without a sale number: the server issues the next S- number (201 with the sale; 12 items get 20%)
POST {{baseUrl}}/api/sales
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "saleDate": "2026-09-24T15:00:00Z",
  "customerId": "3f2b8c1e-1111-4a5b-9c2d-000000000001",
  "customerName": "Maria Silva",
  "branchId": "3f2b8c1e-2222-4a5b-9c2d-000000000002",
  "branchName": "Filial Centro",
  "items": [
    { "productId": "3f2b8c1e-3333-4a5b-9c2d-000000000003", "productName": "Cerveja 350ml", "quantity": 12, "unitPrice": 4.50 }
  ]
}
```

- [ ] **Step 3: Check the file (GREEN)**

```bash
git diff --stat | cat
grep -E '^###|^(POST|GET) ' src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
```

Expected: `1 file changed, 16 insertions(+)`. In the listing, the new `### Create a sale without a sale number …` heading and its `POST {{baseUrl}}/api/sales` come after ticket 05's create and before `### Get the sale back …`.

- [ ] **Step 4: Start the stack from nothing**

```bash
docker compose down -v --remove-orphans
docker compose up --build --wait --wait-timeout 300; echo "exit=$?"
```

Expected: the database is healthy before the API starts, then `exit=0`. If a port is already allocated, stop and ask the user.

- [ ] **Step 5: Replay the requests with curl**

Keep this in one Bash call. It sends the file's requests with the variables filled in. Between them it sends a client-chosen `S-000003`, so the skip shows up on the real stack.

```bash
BASE=http://localhost:8080
curl -s -o /dev/null -w "sign-up %{http_code}\n" -X POST $BASE/api/users -H 'Content-Type: application/json' --data-raw '{"username":"admin","password":"Admin@123","phone":"+5511999999999","email":"admin@developerstore.com","status":"Active","role":"Admin"}'
curl -s -o /tmp/ticket06-checks/login.json -X POST $BASE/api/auth -H 'Content-Type: application/json' --data-raw '{"email":"admin@developerstore.com","password":"Admin@123"}'
TOKEN=$(python3 -c "import json, sys; print(json.load(open(sys.argv[1]))['data']['token'])" /tmp/ticket06-checks/login.json)
create() {
  curl -s -o /tmp/ticket06-checks/create.json -w "$1 %{http_code} " -X POST $BASE/api/sales -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' --data-raw "{$2\"saleDate\":\"2026-09-24T15:00:00Z\",\"customerId\":\"3f2b8c1e-1111-4a5b-9c2d-000000000001\",\"customerName\":\"Maria Silva\",\"branchId\":\"3f2b8c1e-2222-4a5b-9c2d-000000000002\",\"branchName\":\"Filial Centro\",\"items\":[{\"productId\":\"3f2b8c1e-3333-4a5b-9c2d-000000000003\",\"productName\":\"Cerveja 350ml\",\"quantity\":12,\"unitPrice\":4.50}]}"
  python3 -c "import json, sys; b = json.load(open(sys.argv[1])); print(b['data']['saleNumber'] if 'data' in b else json.dumps(b, separators=(',', ':')))" /tmp/ticket06-checks/create.json
}
create "random number" "\"saleNumber\":\"S-$(( RANDOM % 900000 + 100000 ))\","
create "omitted" ""
create "client sends S-000003" '"saleNumber":"S-000003",'
create "omitted" ""
create "omitted" ""
create "S-000001 again" '"saleNumber":"S-000001",'
```

Expected (the first number is random):

```
sign-up 201
random number 201 S-1…
omitted 201 S-000001
client sends S-000003 201 S-000003
omitted 201 S-000002
omitted 201 S-000004
S-000001 again 409 {"type":"BusinessRuleViolation","error":"Business rule violation","detail":"Sale number S-000001 already exists"}
```

- [ ] **Step 6: Check the database**

```bash
docker compose exec -T ambev.developerevaluation.database psql -U developer -d developer_evaluation -At -c 'SELECT "SaleNumber" FROM "Sales" ORDER BY 1;'
docker compose exec -T ambev.developerevaluation.database psql -U developer -d developer_evaluation -At -c 'SELECT last_value FROM sale_number_seq;'
```

Expected: `S-000001`, `S-000002`, `S-000003`, `S-000004` and the random `S-1…` (five sales; the 409 stored nothing), then `4`. The sequence handed out 3 and the generator skipped it.

- [ ] **Step 7: Take the stack down**

```bash
docker compose down -v
docker compose ps -a
```

Expected: `ps -a` lists nothing.

- [ ] **Step 8: Commit**

```bash
git add src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
git commit -m "docs(http): create a sale without a sale number" -m "The .http file gains a create request without saleNumber, so the server issues the next S- number."
git show --stat --format= HEAD | tail -1
```

Expected: `1 file changed, 16 insertions(+)`. No slopwatch run: no C# changed.

---

### Task 8: Verify everything, review, then open a pull request into `develop`

**Skills:** superpowers:verification-before-completion, `dotnet-slopwatch`, `test-anti-patterns` (report only), `clean-code` and superpowers:requesting-code-review (optional), superpowers:finishing-a-development-branch.

**Files:** none, unless Step 2 finds a real problem.

- [ ] **Step 1: Re-run every check fresh**

```bash
git status --short
dotnet build Ambev.DeveloperEvaluation.sln --no-incremental 2>&1 | grep -E 'warning CS|Warning\(s\)|Error\(s\)|Build succeeded' | sort -u
cat /tmp/ticket06-checks/baseline.txt
dotnet test Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-110
slopwatch analyze --fail-on warning 2>&1 | tail -1
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at ' | tail -1
git diff --stat develop -- src/Ambev.DeveloperEvaluation.ORM/Migrations | tail -1
git grep -n -E 'AddScoped<ISaleNumberGenerator|HasDatabaseName\(SaleNumberIndex|PostgresErrorCodes.UniqueViolation' -- src | cut -c1-150
```

Expected:
- only `?? docs/superpowers/tickets/` and other tickets' untracked plans
- `0 Error(s)`, `2 Warning(s)`, `Build succeeded.`, no `warning CS`
- against the baseline: Unit U0 + 4, Integration I0 + 6, Functional F0 + 5 (with ticket 05 alone, 178, 14 and 32)
- `Scan complete: 0 issue(s) found`
- `No changes have been made to the model since the last migration.`
- no migration diff (the `diff --stat` prints nothing)
- three wiring lines: `InfrastructureModuleInitializer.cs`, `SaleConfiguration.cs`, `SaleRepository.cs`

Each ticket criterion and its evidence:

| Ticket criterion | Evidence |
|---|---|
| The validator accepts a missing `saleNumber`; a sent one is 1–50 characters after trimming | `CreateSaleCommandValidatorTests`: no number, blank, 51 characters, 50 with spaces (Task 5); the empty-number 400 in `SaleNumberTests` |
| `string?` with no initializer; omitted and `null` both give 201 with a generated number | `CreateSaleRequest`, `CreateSaleCommand`; the mapping test (Task 6); the omitted and null tests in `SaleNumberTests` |
| `ISaleNumberGenerator.NextAsync()` in ORM, registered in IoC, following §8.3 | `SaleNumberGenerator`; `SaleNumberGeneratorTests` (sequence, skip, 7 digits); the IoC line; ticket 05's `CreateSaleTests` resolving the handler (Task 5) |
| `ExistsBySaleNumberAsync` is exact and case-sensitive | the two `SaleRepositoryTests` from Task 2 |
| The handler throws `DomainException` ("Sale number … already exists") → 409 `BusinessRuleViolation`; the generator is called only when the number is omitted | `CreateSaleHandlerTests` (Task 5); the 409 body in `SaleNumberTests`; Task 7 Step 5 |
| `23505` on `SaleNumber` becomes `DomainException` (race → 409) | the duplicate-save test in `SaleRepositoryTests` (Task 3) |
| Handler unit tests | `CreateSaleHandlerTests`: taken number, omitted, sent and trimmed |
| Integration tests: `S-000001` then `S-000002` after the reset; skip; duplicate through the repository | `SaleNumberGeneratorTests`; `SaleRepositoryTests` |
| Functional tests: omitted → 201 `S-…`; new number kept trimmed; existing → 409 | `SaleNumberTests` |
| `.http` create without `saleNumber` | Task 7 Steps 3 and 5 |
| `feature/sale-number`, pull request into `develop`, Conventional Commits, no attribution | Steps 4–6 |

- [ ] **Step 2: Audit the tests (skill `test-anti-patterns`, report only)**

Audit `SaleNumberGeneratorTests`, `SaleNumberTests`, and the changed `SaleRepositoryTests`, `CreateSaleCommandValidatorTests`, `CreateSaleHandlerTests` and `SalesMappingTests`, with the helpers `SaleTestData` (Integration) and `SaleRequestBody`.

Deliberate choices to report without changing anything:
- **`ResetDataAsync` in `// Given`** in `SaleNumberGeneratorTests`: the tests depend on the sequence's value, and the order of tests in a class isn't fixed (Decision 9).
- **A regex instead of exact generated numbers** in `SaleNumberTests`, for the same reason.
- **Four functional tests passed at RED.** Tasks 3 and 5 built that behaviour test-first; these tests pin it over HTTP.
- **Raw SQL in a test** (`ALTER SEQUENCE … RESTART WITH 1000000`) moves the sequence past 6 digits. The class's reset undoes it.
- **`WithInnerException<DbUpdateException>`** checks that the translation keeps the cause.

If the skill finds a real problem, fix it in a separate `test(sales): …` commit, then re-run Step 1.

- [ ] **Step 3: Optional code review**

Run superpowers:requesting-code-review against `develop..feature/sale-number`, using `clean-code`, `dotnet-best-practices` and `efcore-patterns` as the lenses. Anything that belongs to tickets 07–13 goes into the report, not into this branch.

- [ ] **Step 4: Check the commit messages**

```bash
git log --format=%s develop..feature/sale-number
git log --format=%B develop..feature/sale-number | grep -n -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
```

Expected (plus any `test(sales):` commit from Step 2):

```
docs(http): create a sale without a sale number
feat(sales): accept a create request without a sale number
feat(sales): generate the sale number when omitted and reject a taken one
feat(sales): generate sale numbers from sale_number_seq
feat(sales): answer a duplicate sale number on save with a 409
feat(sales): check whether a sale number is taken
no attribution lines
```

- [ ] **Step 5: Push the branch and open a pull request into `develop`**

This is the superpowers:finishing-a-development-branch step: a pull request into `develop` for the user to review. Don't merge it yourself.

Write the pull request body with the Write tool to `/tmp/ticket06-checks/pr-body.md`: two or three sentences on what the ticket delivers (the Goal above), the evidence table from Step 1 with this run's results, and what Steps 2 and 3 reported. No `Co-Authored-By` trailer, no "Generated with" line and no session link. Then:

```bash
git push -u origin feature/sale-number
gh pr create --base develop --head feature/sale-number --title "Ticket 06: sale numbers generated when omitted, unique when sent" --body-file /tmp/ticket06-checks/pr-body.md
```

Expected: the push creates `origin/feature/sale-number`, and `gh pr create` prints the pull request URL. If `gh` fails (check `gh auth status`), stop and tell the user: the branch is pushed, so they can open the pull request on GitHub.

- [ ] **Step 6: Verify the pull request**

```bash
gh pr view feature/sale-number --json state,baseRefName,commits --jq '"\(.state) \(.baseRefName) \(.commits | length) commits"'
gh pr view feature/sale-number --json title,body --jq '.title + "\n" + .body' | grep -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
git status -sb | head -1
git status --short
```

Expected: `OPEN develop 6 commits` (7 with a `test(sales):` commit from Step 2); `no attribution lines`; `## feature/sale-number...origin/feature/sale-number` with no `ahead` or `behind`; only the untracked tickets and plans. Give the user the pull request URL in the Step 8 report. Don't merge it: the user merges it on GitHub with **Create a merge commit**, so later plans' merge checks find it. Keep `feature/sale-number`.

- [ ] **Step 7: Clean up**

```bash
docker compose ps -a
tasklist | grep -i 'Ambev.DeveloperEvaluation.WebApi' || echo "no API process left"
rm -rf /tmp/ticket06-checks
```

Expected: nothing listed, `no API process left`.

- [ ] **Step 8: Report to the user**

Besides the results, tell the user:
1. **Try the new `.http` request once in your editor** against `docker compose up`, and check that it returns the next `S-` number.
2. **A rare race gives an omitted-number request a 409.** If a client sends `S-000007` at the moment the generator hands out `S-000007`, one request hits the unique index. The spec accepts this (§8.3, "the final guard"). Ticket 13's README can mention it; a retry gets the next number.
3. **For ticket 12:** add `IgnoreQueryFilters()` in `SaleRepository.ExistsBySaleNumberAsync`. The generator calls it, so deleted sales' numbers stay taken in both paths (Decision 3).
4. **Any expected output that differed** from this plan, since none was rehearsed: say what differed and what superpowers:systematic-debugging found. Say so explicitly if none did.
