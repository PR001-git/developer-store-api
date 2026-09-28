# Run the API Against PostgreSQL (Ticket 02) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A developer can start the whole system with `docker compose up` (PostgreSQL 13 plus the API over HTTP on host port 8080), or run the API with `dotnet run` against the compose database. Pending migrations apply at startup in Development, `dotnet ef` works from a pinned local tool, the missing `Users` timestamp columns exist, startup errors are no longer swallowed, and both database-backed test projects get working Testcontainers fixtures. Delivered on `feature/postgres-runtime`, as a pull request into `develop` for the user to review.

**Architecture:** Configuration and infrastructure fixes plus test scaffolding; no new API features. Phase 1 (Tasks 2–6) needs no Docker, and every change in it is driven by a failing check: `dotnet ef` output, an xUnit test or a compose check script. Phase 2 (Tasks 7–13) needs Docker. xUnit collection fixtures start one `postgres:13` container per test project per run and reset the data after every test class, then the last two tasks run `docker compose up` and `dotnet run` for real.

**Tech Stack:** .NET SDK 10.0.200-preview building the `net8.0` projects, with tests running on the installed 8.0.23 runtime; EF Core 8.0.10 with Npgsql.EntityFrameworkCore.PostgreSQL 8.0.8; `dotnet-ef` 8.0.10 (local tool); xUnit 2.9.2; FluentAssertions 6.12.0; Bogus; Testcontainers.PostgreSql 4.15.0; Microsoft.AspNetCore.Mvc.Testing 8.0.31; Docker Desktop (WSL2) with Compose v2; PostgreSQL 13; Python 3.14 with PyYAML (compose check); curl 8.18 (Git Bash); Slopwatch.Cmd 0.4.2 (global tool).

**Source:** `docs/superpowers/tickets/02-run-api-against-postgresql.md`; Sales API design spec `docs/superpowers/specs/2026-09-24-sales-api-design.md`, §8.2 (migrations), §9.1 items 4, 5, 6 and 10, §10 (integration and functional), §11. The ticket also fixes two template bugs that the spec doesn't list: the missing `Users` timestamp columns and silent startup errors.

**Rehearsed:** on 2026-09-25, in a scratch copy of the post-ticket-01 layout, every step that doesn't need Docker ran in this plan's order: all of Tasks 2–6, and in Tasks 7–11 every build, compile-error RED, `dotnet ef` command and commit. `slopwatch` found 0 issues. The expected outputs come from those runs. Docker isn't installed on this machine, so nothing that starts a container has run: the test runs in Tasks 7–11, and all of Tasks 12 and 13. Their expected outputs are marked **(derived)**. They're worked out from the code and from rehearsed equivalents (the `/health` body, the background `dotnet run` and `taskkill`). If a derived output differs, use superpowers:systematic-debugging before changing any code.

---

## Rules for every agent executing this plan

Restate these to every subagent you dispatch.

- **No AI attribution anywhere.** No `Co-Authored-By` trailer, no "Generated with" line, no session link in commits, merge commits, tags or pull requests. Use the commit messages below exactly as written. `.claude/settings.local.json` switches the defaults off, but don't rely on it.
- **Git Bash only.** Run every command in Git Bash (Claude Code's Bash tool), from the repo root `C:\Users\pr000\orca\developer-store-api` (`/c/Users/pr000/orca/developer-store-api`). Each Bash call starts a fresh shell, so don't rely on variables, functions or `cd` from an earlier call. Commands below use paths relative to the root.
- **Write file content with the Write and Edit tools**, not with shell heredocs or `sed`. The rehearsal confirmed the Edit tool keeps `DefaultContext.cs`'s UTF-8 BOM and the CRLF line endings of the existing files. New files come out with LF endings; git converts them on add.
- **Work in the main checkout, never in a worktree.** `.claude/` is untracked, so it exists only here. It holds `settings.local.json` (the attribution guard) and the project skills. A worktree has neither.
- **Stage explicit paths only.** Never use `git add -A`, `git add .` or `git commit -a`. `docs/superpowers/tickets/` is untracked on purpose and must stay out of these commits.
- **Never run `git clean`.** `.git/info/exclude` makes `.claude/`, `.slopwatch/` and the two personal `.doc` files *ignored* files, and `git clean -x` or `-X` deletes ignored files.
- **Push only for the pull request.** Task 1 pushes `develop` with the plan commit, and Task 14 pushes `feature/postgres-runtime` and opens the pull request. Never push `main`, never force-push, and never merge the pull request: the user reviews and merges it on GitHub.
- **Leave `.doc/brainstorm-show-case.md` and `.doc/template-code-review.md` alone.** They are the user's untracked personal notes. Don't open, edit, move or stage them.
- **Don't write or edit migration files by hand** (efcore-patterns). If a generated migration is wrong, undo it with `dotnet ef migrations remove` (same `--project`/`--startup-project` flags as Task 8) and generate it again.
- **Don't install or configure Docker, WSL or any other system software, and don't change machine settings.** If Docker is missing or stopped at the Docker gate, stop and tell the user (see the gate after Task 6).
- **Stop everything you start.** Kill background `dotnet run` processes (`taskkill //F //IM Ambev.DeveloperEvaluation.WebApi.exe`) and take compose stacks down (`docker compose down -v`) before a task ends. A leftover process locks build output and holds ports 5119, 8080 or 5432.
- **Nothing else changes.** Don't fix the pre-existing issues listed below, even when they show up in output. The Dockerfiles, `UseHttpsRedirection()`, the `Log.Information` call before Serilog is configured (spec §9.2) and the empty `docker-compose.override.yml` stay as they are.

## Skills

### Project skills in `.claude/skills/`

| Skill | Use it? | When and how |
|---|---|---|
| `testcontainers-integration-tests` | **Yes, Tasks 7–11** | Load it before Task 7. It sets the approach this plan follows: a real `postgres:13` in a container, the schema from the EF Core migrations, one container per run through an xUnit collection fixture, and `IAsyncLifetime` for start and stop. Its reference file suggests Respawn for data resets; this plan resets with two SQL statements instead (Decision 10). |
| `efcore-patterns` | **Yes, Tasks 3, 8 and 10** | Migrations go only through the CLI: generate `AddUserTimestamps` with `dotnet ef migrations add`, never hand-edit it, undo with `dotnet ef migrations remove`. Its project note overrides two parts: don't make `NoTracking` the context default, and skip the Aspire migration-service pattern (the spec migrates at startup in Development). |
| `dotnet-best-practices` | **Yes, every task that writes C# (3, 5, 7–11)** | XML docs on public members, xUnit with FluentAssertions, Bogus test data, `Given … When … Then …` display names with `// Given`, `// When`, `// Then` sections, `Given_X_When_Y_Then_Z` method names, regular constructors (`.editorconfig` turns off primary constructors). The code below follows it; use it to review what you write. |
| `dotnet-slopwatch` | **Yes, after every task that changes code, and in Task 14** | Run `slopwatch analyze --fail-on warning` and expect `Scan complete: 0 issue(s) found`. The baseline in `.slopwatch/baseline.json` came from ticket 01. Its project note says slopwatch stays a global tool: the `.config/dotnet-tools.json` this ticket adds pins `dotnet-ef` only, so don't add slopwatch to it. The generated migration's `#pragma warning disable 612, 618` lives in an `<auto-generated />` file and isn't flagged (rehearsed). |
| `test-anti-patterns` | **Yes, Task 14 (report only)** | Before opening the pull request, audit the seven new test classes. Fix real findings in a `test:` commit and report the rest to the user. It loads `test-analysis-extensions` (the .NET tables) by itself. |
| `dependency-injection-patterns` | Light, Task 10 | Only its lifetime rules apply: the startup migration resolves the scoped `DefaultContext` from a scope it creates, never from the root provider. This ticket adds no registrations, so its `IModuleInitializer` note doesn't come into play. |
| `type-design-performance` | Light | Every new class is `sealed`, as it recommends. Nothing here is a struct or a value object. |
| `clean-code` | Optional, in review | The changes are small, and `dotnet-best-practices` covers what matters here. |
| `test-smell-detection`, `test-analysis-extensions` | No | `test-anti-patterns` covers the review, and a formal smell catalogue isn't needed. The extensions file is only loaded through the analysis skills. |
| `ai-memory-handoff` | **Yes, if execution stops early** | This matters most at the Docker gate. Save a handoff that names the last completed task and whether Docker is installed. The next session looks for it at startup. |
| `ai-memory-retrieval` | Optional, once at the start | Search for "ticket 02", "postgres" or "docker" to catch gotchas recorded after 2026-09-25. |
| `ai-memory-durable-pages`, `ai-memory-learning-maintenance`, `ai-memory-messaging`, `ai-memory-routing-install` | No | They cover explicit memory writes, maintenance, cross-project messages and install work. |

### Process skills (superpowers plugin)

- `superpowers:subagent-driven-development` or `superpowers:executing-plans` runs this plan (see the header).
- `superpowers:test-driven-development` applies to every task that changes behavior. Each one starts with a check that fails for the stated reason: `dotnet ef` output (Tasks 2–4 and 8), an xUnit test (Tasks 5, 8 and 10), the compose check script (Task 6), or compile errors for a fixture API that doesn't exist yet (Tasks 7, 9, 10 and 11). Watch it fail before you write the fix.
- `superpowers:systematic-debugging` applies whenever a derived output doesn't match, since those steps never ran.
- `superpowers:verification-before-completion` applies at the end of every task and fully in Task 14: re-run the checks fresh before claiming anything passes.
- `superpowers:requesting-code-review` is optional in Task 14, before the pull request.
- `superpowers:finishing-a-development-branch` applies in Task 14, but the option is already chosen: push the branch and open a pull request into `develop` for the user to review. Don't merge it, keep the branch, and don't offer the other options.
- Don't use `superpowers:using-git-worktrees` (see the rules) or `superpowers:brainstorming` (the design is settled in the spec and the ticket).

## Decisions this plan makes

1. **Two phases with a Docker gate.** Tasks 2–6 need no Docker. Tasks 7–13 need Docker Desktop. If Docker isn't available after Task 6, stop there: the feature branch keeps Phase 1's commits, and nothing is pushed or opened as a pull request. The ticket isn't done until Phase 2 passes.
2. **The design-time factory gets its own file**, `src/Ambev.DeveloperEvaluation.ORM/DefaultContextFactory.cs`, sealed and documented. `DefaultContext.cs` keeps only the context. The body is the template's, apart from the migrations assembly.
3. **The tool manifest goes in `.config/` explicitly.** On this machine's .NET 10 SDK, `dotnet new tool-manifest` writes `dotnet-tools.json` at the root unless you pass `-o .config`. It pins `dotnet-ef` only.
4. **The timestamp migration is committed exactly as generated**, named `AddUserTimestamps`. Its file-name prefix is the generation time, so commands below use `*_AddUserTimestamps` globs. EF gives the new non-null `CreatedAt` column a `-infinity` default for existing rows. There are none, because every insert failed before this migration, so the generated code stays as it is.
5. **A plain `throw;` with no exception filter.** `dotnet ef` stops `Program.Main` at `builder.Build()` with a `HostAbortedException`. The rethrow hands it back to the EF tools, which expect it. The rehearsal confirmed that `dotnet ef` still works and that nothing extra gets logged.
6. **Startup migrations run synchronously right after `builder.Build()`**, inside a scope and only in Development (spec §8.2). There's no retry loop. Compose starts the API only after the database healthcheck passes, and the functional fixture starts the container before the API.
7. **Compose details the ticket leaves open.** The obsolete `version: '3.8'` key goes, since Compose v2 warns about it. The healthcheck runs `pg_isready -h localhost`, which checks over TCP: on first boot the postgres image runs a temporary server that listens only on the Unix socket, and a socket check would pass too early. `restart: unless-stopped` stays on the database. Both Dockerfiles keep `EXPOSE 8081`, for three reasons: EXPOSE only documents a port, nothing listens on 8081 without `ASPNETCORE_HTTPS_PORTS`, and spec §9.2 leaves the two Dockerfiles as they are.
8. **Package versions:** Testcontainers.PostgreSql 4.15.0 (the newest 4.x on 2026-09-25), FluentAssertions 6.12.0 (what the Unit project uses; 8.x is commercial), and Microsoft.AspNetCore.Mvc.Testing 8.0.31 (the newest 8.0.x). The fixtures call `new PostgreSqlBuilder("postgres:13")`, because 4.15 marks the parameterless constructor obsolete (warning CS0618).
9. **One collection per test project, with the reset as a class fixture declared on the collection definition.** xUnit 2.9 creates a class fixture declared on a `[CollectionDefinition]` for every test class in that collection. So `DataResetFixture.DisposeAsync` runs after each class without the classes opting in. A throwaway xUnit 2.9.2 probe confirmed this on 2026-09-25: the fixture initialized and disposed once around each class. The first class starts on the freshly migrated, empty database.
10. **The reset is two SQL statements, not Respawn.** The ticket asks for exactly this: truncate the tables it's given, restart the sequences it's given. The explicit lists (`DataResetFixture.Tables` and `DataResetFixture.Sequences`) show ticket 05 exactly what to add: the Sales tables and `sale_number_seq`. Identifiers are double-quoted, with embedded quotes doubled, because EF creates mixed-case names such as `"Users"`.
11. **Each test project keeps its own copy of the reset**, about 15 lines. Sharing it would need a new test-support project, which the spec's layout doesn't have.
12. **`StartupFailureTests` stays outside the collection.** It needs no database, so it runs without Docker (it's the only functional test in Phase 1) and never waits for the container.
13. **The integration tests get their own `UserTestData`**, with the same Bogus rules as the Unit project's, rather than a reference to the Unit test project.
14. **This plan is committed on `develop` before branching** (spec §11), as ticket 01's was, and `develop` is pushed. The work reaches `develop` through a pull request the user reviews and merges on GitHub with **Create a merge commit**; the agent never merges it. The feature branch is kept.

## Pre-existing output you'll see (leave it alone)

- `warning NU1903` (AutoMapper 13.0.1 advisory; spec D14 keeps that version) and `warning CS8604` at `JwtTokenGenerator.cs(42,43)`, fixed by a later ticket. A clean solution build shows `3 Warning(s)`. A single-project build shows `2 Warning(s)`, and an incremental build can show fewer.
- `message NETSDK1057: You are using a preview version of .NET`. The machine has only the .NET 10 preview SDK. It builds `net8.0` fine, and the tests run on the installed 8.0.23 runtime.
- `MSB1011` from a bare `dotnet build` or `dotnet test` at the root, because `docker-compose.dcproj` sits next to the `.sln`. Always pass `Ambev.DeveloperEvaluation.sln` or a project path.
- `No test is available in …Functional.dll` until Task 5, and `… in …Integration.dll` until Task 7.
- `warning: in the working copy of '…', LF will be replaced by CRLF the next time Git touches it` when adding new files. It's harmless: `core.autocrlf=true`.
- `dotnet ef` prints `Build started...` and `Build succeeded.`, and repeats the NU1903 warning from its own build. `dotnet ef migrations list --no-connect` ends with `Pending status not shown. Unable to determine which migrations have been applied. …`, because `--no-connect` skips the database.
- `dotnet run` prints `Using launch settings from src\Ambev.DeveloperEvaluation.WebApi\Properties\launchSettings.json...` and `Building...`.
- Running the app (`dotnet run`, `dotnet ef`, the tests) creates `logs/` folders, for example `src/Ambev.DeveloperEvaluation.WebApi/logs/`. `.gitignore` ignores them (`[Ll]ogs/`).
- **(derived)** In Phase 2 the API logs `Failed to determine the https port for redirect.` The API is HTTP only now, and `UseHttpsRedirection()` stays.
- **(derived)** The first Docker-backed test run pulls `postgres:13` and `testcontainers/ryuk`, and the first `docker compose up --build` pulls the .NET SDK and ASP.NET images. Expect a few minutes.

## File map

| Change | Paths |
|---|---|
| Created | `.config/dotnet-tools.json`; `src/Ambev.DeveloperEvaluation.ORM/DefaultContextFactory.cs`; `src/Ambev.DeveloperEvaluation.ORM/Migrations/<timestamp>_AddUserTimestamps.cs` and `.Designer.cs` (generated) |
| Created (integration tests) | `tests/Ambev.DeveloperEvaluation.Integration/Fixtures/{PostgreSqlFixture, DatabaseCollection, DataResetFixture, DataResetTests}.cs`, `ORM/{MigrationTests, UserRepositoryTests}.cs`, `TestData/UserTestData.cs` |
| Created (functional tests) | `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/{ApiFixture, ApiCollection, DataResetFixture, DataResetTests}.cs`, `Health/HealthCheckTests.cs`, `Startup/{StartupFailureTests, StartupMigrationTests}.cs` |
| Modified | `src/Ambev.DeveloperEvaluation.ORM/DefaultContext.cs` (factory removed); `src/Ambev.DeveloperEvaluation.ORM/Migrations/DefaultContextModelSnapshot.cs` (generated); `src/Ambev.DeveloperEvaluation.WebApi/appsettings.json`; `src/Ambev.DeveloperEvaluation.WebApi/Program.cs`; `docker-compose.yml`; both test `.csproj` files |
| Committed on `develop` first | `docs/superpowers/plans/2026-09-25-02-run-api-against-postgresql.md` (this plan) |
| Local only, never committed | `/tmp/ticket02-checks/` (compose check and run logs, removed at the end); git-ignored `logs/` folders |
| Not touched | both Dockerfiles, `docker-compose.override.yml`, `docker-compose.dcproj`, `.dockerignore`, both `launchSettings.json` files, `appsettings.Development.json`, `.gitignore`, the Unit test project, `docs/superpowers/tickets/`, `.claude/`, `.slopwatch/`, the personal `.doc` files |

---

### Task 1: Check the starting point, commit this plan and branch

**Files:**
- Commit on `develop`: `docs/superpowers/plans/2026-09-25-02-run-api-against-postgresql.md`
- Create (outside the repo): `/tmp/ticket02-checks/`

- [ ] **Step 1: Confirm ticket 01 is merged and the tree is clean**

```bash
git switch develop
git pull --ff-only origin develop
git log --oneline -3
git diff --quiet && git diff --cached --quiet && echo "no tracked changes"
git status --short
test ! -e template && test -f Ambev.DeveloperEvaluation.sln && test -d src && echo "ticket 01 layout in place"
test -f .slopwatch/baseline.json && echo "slopwatch baseline present"
git branch --list feature/postgres-runtime
ls -d .config 2>&1
```

Expected:
- The pull fast-forwards `develop` to the merged pull requests on GitHub, or prints `Already up to date.`
- HEAD is ticket 01's pull request merge, `Merge pull request #<n> from PR001-git/feature/repo-restructure`, or a later `develop` commit.
- `no tracked changes`, `ticket 01 layout in place` and `slopwatch baseline present`.
- `git status --short` normally shows `?? docs/superpowers/plans/2026-09-25-02-run-api-against-postgresql.md` and `?? docs/superpowers/tickets/`. Other untracked plan files for later tickets are fine; leave them alone.
- The branch check prints nothing, and `ls` prints `ls: cannot access '.config': No such file or directory`.

Stop and ask the user if:
- the pull fails. Local `develop` must only ever fast-forward to `origin/develop`.
- `template/` still exists. Ticket 01 isn't done (or its pull request isn't merged yet), and this ticket is blocked by it.
- anything tracked is modified.
- `feature/postgres-runtime` already exists, because an earlier run got partway. In that case, look for an ai-memory handoff first.

- [ ] **Step 2: Check the tools, including Docker**

```bash
dotnet --list-sdks
dotnet --list-runtimes | grep -E 'Microsoft\.(NETCore|AspNetCore)\.App 8\.'
command -v slopwatch
python3 -c "import yaml; print('PyYAML', yaml.__version__)"
curl --version | head -1
dotnet ef --version 2>&1 | head -1
docker version --format 'Docker server {{.Server.Version}}' 2>&1 | tail -1
docker compose version 2>&1 | tail -1
```

Expected: SDK `10.0.200-preview…`, the 8.0.23 `NETCore` and `AspNetCore` runtimes, a slopwatch path, a `PyYAML …` line, `curl 8.18.0 …` and `Could not execute because the specified command or file was not found.` (no `dotnet ef` yet; Task 2 adds it).

The last two lines decide the Docker gate:
- **Docker ready:** a `Docker server …` version and a `Docker Compose version v2…` line. Run every task.
- **Docker missing or stopped:** `docker: command not found`, or an error that mentions the Docker daemon or `dockerDesktopLinuxEngine`. Do Tasks 1–6, then stop at the gate.

If `import yaml` fails, stop and ask the user: Task 6's check needs PyYAML.

- [ ] **Step 3: Create the scratch folder for checks and logs**

```bash
mkdir -p /tmp/ticket02-checks && ls -d /tmp/ticket02-checks
```

In Git Bash, `/tmp` is `C:\Users\pr000\AppData\Local\Temp`. That's a fixed path outside the repo, so every task and subagent finds the same files and none of them can be committed.

- [ ] **Step 4: Commit this plan on `develop` and push `develop`**

If `git status --short docs/superpowers/plans/2026-09-25-02-run-api-against-postgresql.md` prints nothing, the plan is already committed: skip `git add` and `git commit`, and still run the push.

```bash
git add docs/superpowers/plans/2026-09-25-02-run-api-against-postgresql.md
git commit -m "docs: add plan for running the API against PostgreSQL"
git status --short
git push origin develop
```

Expected: a commit with one file changed. `git status --short` then lists `?? docs/superpowers/tickets/`, plus any other untracked plan files. The push sends the plan commit to `origin/develop` (or prints `Everything up-to-date`), so the Task 14 pull request holds only the feature commits. If the push is rejected, stop and ask the user; never force it.

- [ ] **Step 5: Create the feature branch**

```bash
git switch -c feature/postgres-runtime
git log --oneline -1
```

Expected: `Switched to a new branch 'feature/postgres-runtime'`, with HEAD at the plan commit.

- [ ] **Step 6: Record the baseline**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E 'Warning\(s\)|Error\(s\)|Build succeeded'
dotnet test Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!|No test is available' | cut -c1-110
```

Expected: `Build succeeded.`, `3 Warning(s)`, `0 Error(s)`; then `Passed!  - Failed:     0, Passed:    49, Skipped:     0, Total:    49, …`, plus two `No test is available` lines (Functional and Integration). If the baseline isn't green, stop: nothing can be judged against a broken start.

---

## Phase 1: no Docker needed

### Task 2: Pin `dotnet-ef` 8.0.10 in a local tool manifest

**Files:**
- Create: `.config/dotnet-tools.json`

- [ ] **Step 1: Watch `dotnet ef` fail**

```bash
dotnet ef --version 2>&1 | head -1; echo "exit=${PIPESTATUS[0]}"
```

Expected (RED): `Could not execute because the specified command or file was not found.` and `exit=1`.

- [ ] **Step 2: Create the manifest in `.config/` and install the tool**

```bash
dotnet new tool-manifest -o .config
dotnet tool install dotnet-ef --version 8.0.10
```

Expected: `The template "Dotnet local tool manifest file" was created successfully.`, then a `You can invoke the tool from this directory …` line and `Tool 'dotnet-ef' (version '8.0.10') was successfully installed. Entry is added to the manifest file C:\Users\pr000\orca\developer-store-api\.config\dotnet-tools.json.` If the path doesn't end in `\.config\dotnet-tools.json`, the `-o .config` was missed: delete the stray root `dotnet-tools.json` and rerun both commands.

- [ ] **Step 3: Verify the manifest and the restore (GREEN)**

```bash
cat .config/dotnet-tools.json
dotnet tool restore
dotnet ef --version
git status --short
```

Expected: exactly this manifest:

```json
{
  "version": 1,
  "isRoot": true,
  "tools": {
    "dotnet-ef": {
      "version": "8.0.10",
      "commands": [
        "dotnet-ef"
      ],
      "rollForward": false
    }
  }
}
```

After it: `Tool 'dotnet-ef' (version '8.0.10') was restored. Available commands: dotnet-ef` and `Restore was successful.`, then `Entity Framework Core .NET Command-line Tools` and `8.0.10`. `git status --short` shows `?? .config/` and `?? docs/superpowers/tickets/`.

- [ ] **Step 4: Commit**

```bash
git add .config/dotnet-tools.json
git commit -m "build: pin dotnet-ef 8.0.10 in a local tool manifest" -m "dotnet tool restore now installs the EF Core CLI that matches the EF Core 8.0.10 packages, so dotnet ef works on a fresh clone."
```

Expected: `1 file changed, 13 insertions(+)`.

---

### Task 3: Point the design-time factory at the ORM migrations

**Skill:** `efcore-patterns`, and `dotnet-best-practices` for the new class.

**Files:**
- Modify: `src/Ambev.DeveloperEvaluation.ORM/DefaultContext.cs` (remove two `using` lines and the `YourDbContextFactory` class)
- Create: `src/Ambev.DeveloperEvaluation.ORM/DefaultContextFactory.cs`

- [ ] **Step 1: Watch the EF tools miss the migrations (RED)**

```bash
dotnet ef migrations list --no-connect --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -v NU1903
dotnet ef dbcontext info --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -E 'Options'
```

Expected: `No migrations were found.` and `Options: MigrationsAssembly=Ambev.DeveloperEvaluation.WebApi`. The template's factory sends the tools to the WebApi assembly, while `InitialMigrations` lives in ORM and `Program.cs` already says ORM.

- [ ] **Step 2: Remove the factory from `DefaultContext.cs`**

Use the Edit tool (it keeps the file's BOM and CRLF endings). Delete these two lines:

```csharp
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
```

and delete the whole `public class YourDbContextFactory : IDesignTimeDbContextFactory<DefaultContext>` class, from that line to the end of the file. The file then reads:

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Ambev.DeveloperEvaluation.ORM;

public class DefaultContext : DbContext
{
    public DbSet<User> Users { get; set; }

    public DefaultContext(DbContextOptions<DefaultContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
```

- [ ] **Step 3: Create `src/Ambev.DeveloperEvaluation.ORM/DefaultContextFactory.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Ambev.DeveloperEvaluation.ORM;

/// <summary>
/// Creates the <see cref="DefaultContext"/> that the EF Core tools (<c>dotnet ef</c>) use at design time.
/// </summary>
/// <remarks>
/// Reads <c>ConnectionStrings:DefaultConnection</c> from the <c>appsettings.json</c> in the current directory.
/// The tools set that directory to the startup project's folder, so run them with
/// <c>--startup-project src/Ambev.DeveloperEvaluation.WebApi</c>.
/// </remarks>
public sealed class DefaultContextFactory : IDesignTimeDbContextFactory<DefaultContext>
{
    /// <summary>
    /// Creates a context that uses PostgreSQL and keeps its migrations in this assembly.
    /// </summary>
    /// <param name="args">Arguments passed by the design-time tools; not used.</param>
    /// <returns>A new <see cref="DefaultContext"/>.</returns>
    public DefaultContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var builder = new DbContextOptionsBuilder<DefaultContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        builder.UseNpgsql(
               connectionString,
               b => b.MigrationsAssembly("Ambev.DeveloperEvaluation.ORM")
        );

        return new DefaultContext(builder.Options);
    }
}
```

The remark about the working directory is why the ticket's command needs `--startup-project src/Ambev.DeveloperEvaluation.WebApi`: the rehearsal's `--verbose` run showed `Using working directory '…\src\Ambev.DeveloperEvaluation.WebApi'`.

- [ ] **Step 4: Verify (GREEN)**

```bash
git diff --stat
git grep -n "YourDbContextFactory" -- src tests || echo "no YourDbContextFactory left"
dotnet ef migrations list --no-connect --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -E '^20|No migrations'
dotnet ef dbcontext info --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -E 'Database name|Options'
```

Expected:
- `1 file changed, 22 deletions(-)` for `DefaultContext.cs` (git may shorten the path to `.../DefaultContext.cs`). If every line shows as changed, the BOM or line endings were lost: `git checkout -- src/Ambev.DeveloperEvaluation.ORM/DefaultContext.cs` and redo Step 2 with the Edit tool.
- `no YourDbContextFactory left`.
- `20241014011203_InitialMigrations`.
- `Database name: DeveloperEvaluation` (Task 4 fixes that) and `Options: MigrationsAssembly=Ambev.DeveloperEvaluation.ORM`.

- [ ] **Step 5: Commit**

```bash
git add src/Ambev.DeveloperEvaluation.ORM/DefaultContext.cs src/Ambev.DeveloperEvaluation.ORM/DefaultContextFactory.cs
git commit -m "fix(orm): point the design-time factory at the ORM migrations" -m "The factory told the EF tools that migrations live in the WebApi assembly, while Program.cs and the existing migrations use the ORM assembly, so dotnet ef found no migrations. YourDbContextFactory becomes DefaultContextFactory, in its own file, with Ambev.DeveloperEvaluation.ORM as the migrations assembly."
```

Expected: `2 files changed, 39 insertions(+), 22 deletions(-)`.

---

### Task 4: Use the Npgsql connection string for the compose database

**Files:**
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/appsettings.json` (line 3)

- [ ] **Step 1: Watch the design-time context pick the wrong database (RED)**

```bash
dotnet ef dbcontext info --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -E 'Database name|Data source'
```

Expected: `Database name: DeveloperEvaluation` and `Data source: tcp://localhost:5432`. Npgsql accepts the SQL Server keywords as aliases, so it targets the wrong database as user `sa`.

- [ ] **Step 2: Replace the connection string**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.WebApi/appsettings.json`, replace

```json
    "DefaultConnection": "Server=localhost;Database=DeveloperEvaluation;User Id=sa;Password=Pass@word;TrustServerCertificate=True"
```

with

```json
    "DefaultConnection": "Host=localhost;Port=5432;Database=developer_evaluation;Username=developer;Password=ev@luAt10n"
```

- [ ] **Step 3: Verify (GREEN)**

```bash
dotnet ef dbcontext info --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -E 'Database name|Data source|Options'
git diff --stat
```

Expected: `Database name: developer_evaluation`, `Data source: tcp://localhost:5432`, `Options: MigrationsAssembly=Ambev.DeveloperEvaluation.ORM`, then `src/Ambev.DeveloperEvaluation.WebApi/appsettings.json | 2 +-`.

- [ ] **Step 4: Commit**

```bash
git add src/Ambev.DeveloperEvaluation.WebApi/appsettings.json
git commit -m "fix(webapi): use the Npgsql connection string for the compose database" -m "The default connection string was in SQL Server format, with credentials that match nothing in the repo. It now points at the compose PostgreSQL database on localhost:5432."
```

---

### Task 5: Rethrow startup exceptions after logging them

**Skill:** `dotnet-best-practices`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Functional/Ambev.DeveloperEvaluation.Functional.csproj` (two packages, one project reference)
- Create: `tests/Ambev.DeveloperEvaluation.Functional/Startup/StartupFailureTests.cs`
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Program.cs` (the `catch` block)

- [ ] **Step 1: Add the packages and the WebApi reference to the functional project**

```bash
dotnet add tests/Ambev.DeveloperEvaluation.Functional/Ambev.DeveloperEvaluation.Functional.csproj package FluentAssertions --version 6.12.0
dotnet add tests/Ambev.DeveloperEvaluation.Functional/Ambev.DeveloperEvaluation.Functional.csproj package Microsoft.AspNetCore.Mvc.Testing --version 8.0.31
dotnet add tests/Ambev.DeveloperEvaluation.Functional/Ambev.DeveloperEvaluation.Functional.csproj reference src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.csproj
git diff tests/Ambev.DeveloperEvaluation.Functional/Ambev.DeveloperEvaluation.Functional.csproj | grep -E '^[+-] '
```

Expected: two `info : PackageReference for package '…' added to file '…'` lines, then ``Reference `..\..\src\Ambev.DeveloperEvaluation.WebApi\Ambev.DeveloperEvaluation.WebApi.csproj` added to the project.``, and exactly these added lines:

```
+    <PackageReference Include="FluentAssertions" Version="6.12.0" />
+    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.31" />
+    <ProjectReference Include="..\..\src\Ambev.DeveloperEvaluation.WebApi\Ambev.DeveloperEvaluation.WebApi.csproj" />
```

- [ ] **Step 2: Write the failing test**

Create `tests/Ambev.DeveloperEvaluation.Functional/Startup/StartupFailureTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.WebApi;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Startup;

/// <summary>
/// Contains tests for what the API does when it cannot start.
/// They need no Docker: startup fails before anything touches the database.
/// </summary>
public sealed class StartupFailureTests
{
    /// <summary>
    /// Tests that a startup exception reaches the host instead of being swallowed by <c>Program.Main</c>.
    /// </summary>
    [Fact(DisplayName = "Given an empty JWT secret key When the API starts Then the startup exception reaches the host")]
    public void Given_EmptyJwtSecretKey_When_ApiStarts_Then_StartupExceptionReachesHost()
    {
        // Given
        using var factory = new WebApplicationFactory<Program>();
        var misconfigured = factory.WithWebHostBuilder(builder => builder.UseSetting("Jwt:SecretKey", string.Empty));

        // When
        var start = () => misconfigured.CreateClient();

        // Then
        start.Should().Throw<ArgumentException>().WithParameterName("secretKey");
    }
}
```

`AddJwtAuthentication` calls `ArgumentException.ThrowIfNullOrWhiteSpace(secretKey)` before `builder.Build()`, so an empty key is a real startup failure that needs no database.

- [ ] **Step 3: Run it and the empty-key `dotnet run`, and watch both fail (RED)**

```bash
dotnet test tests/Ambev.DeveloperEvaluation.Functional --filter "FullyQualifiedName~StartupFailureTests" 2>&1 | grep -E 'Passed!|Failed!|Expected a|entry point' | cut -c1-120
dotnet run --project src/Ambev.DeveloperEvaluation.WebApi -- --Jwt:SecretKey= > /tmp/ticket02-checks/run-empty-key.log 2>&1; echo "exit=$?"
grep -v NU1903 /tmp/ticket02-checks/run-empty-key.log | head -4 | cut -c1-160
```

Expected:

```
   Expected a <System.ArgumentException> to be thrown, but found <System.InvalidOperationException>:
System.InvalidOperationException: The entry point exited without ever building an IHost.
Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, …
exit=0
Using launch settings from src\Ambev.DeveloperEvaluation.WebApi\Properties\launchSettings.json...
Building...
```

The swallowed exception makes the app exit 0 and print nothing, which is the bug.

- [ ] **Step 4: Rethrow**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.WebApi/Program.cs`, replace

```csharp
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
```

with

```csharp
            Log.Fatal(ex, "Application terminated unexpectedly");
            throw;
        }
```

- [ ] **Step 5: Verify (GREEN), and that `dotnet ef` and the unit tests still work**

```bash
git diff --stat src/Ambev.DeveloperEvaluation.WebApi/Program.cs
dotnet test tests/Ambev.DeveloperEvaluation.Functional --filter "FullyQualifiedName~StartupFailureTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet run --project src/Ambev.DeveloperEvaluation.WebApi -- --Jwt:SecretKey= > /tmp/ticket02-checks/run-empty-key.log 2>&1; echo "exit=$?"
grep -m1 "Unhandled exception" /tmp/ticket02-checks/run-empty-key.log | cut -c1-160
dotnet ef migrations list --no-connect --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -E '^20|rror'
dotnet test tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected:
- `src/Ambev.DeveloperEvaluation.WebApi/Program.cs | 1 +`.
- `Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1`.
- A non-zero exit: `exit=127` in the rehearsal's Git Bash (PowerShell reports `-532462766` for the same crash). Then `Unhandled exception. System.ArgumentException: The value cannot be an empty string or composed entirely of whitespace. (Parameter 'secretKey')`.
- `20241014011203_InitialMigrations`, with no error: the EF tools' `HostAbortedException` passes through the rethrow.
- `Passed!  - Failed:     0, Passed:    49, Skipped:     0, Total:    49`.

- [ ] **Step 6: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.WebApi/Program.cs tests/Ambev.DeveloperEvaluation.Functional/Ambev.DeveloperEvaluation.Functional.csproj tests/Ambev.DeveloperEvaluation.Functional/Startup/StartupFailureTests.cs
git commit -m "fix(webapi): rethrow startup exceptions after logging them" -m "Program.Main caught every exception and returned normally. Serilog has no sinks before builder.Build(), so a startup failure exited with code 0 and printed nothing, and WebApplicationFactory reported only that the entry point never built a host. The new functional test starts the API with an empty JWT key and expects the real ArgumentException."
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `3 files changed, 34 insertions(+)` and `Scan complete: 0 issue(s) found`.

---

### Task 6: Run only PostgreSQL and the API, over HTTP, in compose

**Files:**
- Create (outside the repo): `/tmp/ticket02-checks/check-compose.py`
- Modify (full rewrite): `docker-compose.yml`

- [ ] **Step 1: Write the compose check**

Create `/tmp/ticket02-checks/check-compose.py` with the Write tool (Windows path `C:\Users\pr000\AppData\Local\Temp\ticket02-checks\check-compose.py`):

```python
"""Ticket 02 compose check. Run from the repo root: docker-compose.yml runs only
PostgreSQL 13 and the API; the API serves HTTP on host port 8080 with the compose
connection string and waits for a healthy database; the database is on host port 5432."""
import sys

import yaml

API = "ambev.developerevaluation.webapi"
DB = "ambev.developerevaluation.database"
CONNECTION = ("Host=ambev.developerevaluation.database;Port=5432;Database=developer_evaluation;"
              "Username=developer;Password=ev@luAt10n")

with open("docker-compose.yml", encoding="utf-8") as f:
    compose = yaml.safe_load(f)
services = compose.get("services") or {}
api = services.get(API) or {}
db = services.get(DB) or {}
env = api.get("environment") or []
env = dict(item.split("=", 1) for item in env) if isinstance(env, list) else env
depends_on = api.get("depends_on") or {}
depends_on = depends_on if isinstance(depends_on, dict) else {name: {} for name in depends_on}
healthcheck = (db.get("healthcheck") or {}).get("test") or []

checks = [
    ("no obsolete top-level 'version' key", "version" not in compose),
    (f"services are the API and the database only (found {sorted(services)})", sorted(services) == sorted([API, DB])),
    (f"API publishes only 8080:8080 (found {api.get('ports')})", api.get("ports") == ["8080:8080"]),
    ("API sets no HTTPS port", "ASPNETCORE_HTTPS_PORTS" not in env),
    ("API mounts no certificate or user-secrets volumes", "volumes" not in api),
    ("API connection string points at the database service", env.get("ConnectionStrings__DefaultConnection") == CONNECTION),
    ("API waits for a healthy database", (depends_on.get(DB) or {}).get("condition") == "service_healthy"),
    (f"database image is postgres:13 (found {db.get('image')})", db.get("image") == "postgres:13"),
    (f"database publishes 5432:5432 (found {db.get('ports')})", db.get("ports") == ["5432:5432"]),
    ("database healthcheck runs pg_isready over TCP", "pg_isready" in healthcheck and "localhost" in healthcheck),
]
problems = 0
for label, ok in checks:
    print(("ok       " if ok else "PROBLEM  ") + label)
    problems += 0 if ok else 1
print(f"compose problems: {problems}")
sys.exit(1 if problems else 0)
```

- [ ] **Step 2: Run it against the template's compose file (RED)**

```bash
python3 /tmp/ticket02-checks/check-compose.py; echo "exit=$?"
```

Expected:

```
PROBLEM  no obsolete top-level 'version' key
PROBLEM  services are the API and the database only (found ['ambev.developerevaluation.cache', 'ambev.developerevaluation.database', 'ambev.developerevaluation.nosql', 'ambev.developerevaluation.webapi'])
PROBLEM  API publishes only 8080:8080 (found ['8080', '8081'])
PROBLEM  API sets no HTTPS port
PROBLEM  API mounts no certificate or user-secrets volumes
PROBLEM  API connection string points at the database service
PROBLEM  API waits for a healthy database
ok       database image is postgres:13 (found postgres:13)
PROBLEM  database publishes 5432:5432 (found ['5432'])
PROBLEM  database healthcheck runs pg_isready over TCP
compose problems: 9
exit=1
```

- [ ] **Step 3: Rewrite `docker-compose.yml`**

Replace the whole file with the Write tool:

```yaml
services:
  ambev.developerevaluation.webapi:
    container_name: ambev_developer_evaluation_webapi
    image: ${DOCKER_REGISTRY-}ambevdeveloperevaluationwebapi
    build:
      context: .
      dockerfile: src/Ambev.DeveloperEvaluation.WebApi/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_HTTP_PORTS=8080
      - ConnectionStrings__DefaultConnection=Host=ambev.developerevaluation.database;Port=5432;Database=developer_evaluation;Username=developer;Password=ev@luAt10n
    ports:
      - "8080:8080"
    depends_on:
      ambev.developerevaluation.database:
        condition: service_healthy

  ambev.developerevaluation.database:
    container_name: ambev_developer_evaluation_database
    image: postgres:13
    environment:
      POSTGRES_DB: developer_evaluation
      POSTGRES_USER: developer
      POSTGRES_PASSWORD: ev@luAt10n
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD", "pg_isready", "-h", "localhost", "-U", "developer", "-d", "developer_evaluation"]
      interval: 5s
      timeout: 5s
      retries: 10
    restart: unless-stopped
```

The port mappings stay quoted: YAML 1.1 can read an unquoted `8080:8080` as a base-60 number.

- [ ] **Step 4: Verify (GREEN)**

```bash
python3 /tmp/ticket02-checks/check-compose.py | tail -1; echo "exit=${PIPESTATUS[0]}"
git diff --stat
```

Expected: `compose problems: 0`, `exit=0`, and `docker-compose.yml | 40 +++++++++++-----------------------------` (`11 insertions(+), 29 deletions(-)`).

If Docker was ready in Task 1, also run `docker compose config --quiet; echo "exit=$?"` and expect `exit=0` with nothing else printed **(derived)**. If Compose complains about the empty `docker-compose.override.yml`, stop and ask the user. Don't delete that file: `docker-compose.dcproj` lists it.

- [ ] **Step 5: Commit**

```bash
git add docker-compose.yml
git commit -m "build(docker): run only PostgreSQL and the API, over HTTP, in compose" -m "The API is published on host port 8080 over HTTP only, without the HTTPS port and the certificate and user-secrets volumes, and PostgreSQL on 5432. The API gets the compose connection string and starts after a pg_isready healthcheck passes. The unused MongoDB and Redis services and the obsolete version key are gone."
```

---

## Docker gate

Run this before Task 7, even if Task 1 found Docker:

```bash
docker version --format 'Docker server {{.Server.Version}}' 2>&1 | tail -1
docker info --format '{{.OSType}}' 2>&1 | tail -1
```

Expected: a `Docker server …` version and `linux`.

If either command fails, **stop here**:
1. Don't install Docker yourself. Don't push the branch or open a pull request, and leave `feature/postgres-runtime` as it is: Tasks 2–6 are committed.
2. Save an ai-memory handoff (skill `ai-memory-handoff`) that says Phase 1 is done and the next step is Task 7 once Docker Desktop runs.
3. Tell the user that Docker Desktop with the WSL2 backend must be installed and running (on Windows 11 Home it needs WSL2; see spec §11), and that execution resumes at Task 7.

---

## Phase 2: needs Docker

### Task 7: Run the integration tests against a migrated PostgreSQL 13 container

**Skills:** `testcontainers-integration-tests` (load it now), `dotnet-best-practices`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Integration/Ambev.DeveloperEvaluation.Integration.csproj` (two packages)
- Create: `tests/Ambev.DeveloperEvaluation.Integration/ORM/MigrationTests.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Integration/Fixtures/PostgreSqlFixture.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Integration/Fixtures/DatabaseCollection.cs`

- [ ] **Step 1: Add the packages**

```bash
dotnet add tests/Ambev.DeveloperEvaluation.Integration/Ambev.DeveloperEvaluation.Integration.csproj package FluentAssertions --version 6.12.0
dotnet add tests/Ambev.DeveloperEvaluation.Integration/Ambev.DeveloperEvaluation.Integration.csproj package Testcontainers.PostgreSql --version 4.15.0
git diff tests/Ambev.DeveloperEvaluation.Integration/Ambev.DeveloperEvaluation.Integration.csproj | grep -E '^[+-] '
```

Expected: exactly these added lines:

```
+    <PackageReference Include="FluentAssertions" Version="6.12.0" />
+    <PackageReference Include="Testcontainers.PostgreSql" Version="4.15.0" />
```

- [ ] **Step 2: Write the first test**

Create `tests/Ambev.DeveloperEvaluation.Integration/ORM/MigrationTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.ORM;

/// <summary>
/// Contains integration tests for the EF Core migrations on PostgreSQL 13.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class MigrationTests
{
    private readonly PostgreSqlFixture _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationTests"/> class.
    /// </summary>
    /// <param name="database">The collection's database, injected by xUnit.</param>
    public MigrationTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    /// <summary>
    /// Tests that the fixture applied every migration in the ORM assembly, in order.
    /// </summary>
    [Fact(DisplayName = "Given the PostgreSQL fixture When reading the migration history Then every migration is applied")]
    public async Task Given_PostgreSqlFixture_When_ReadingMigrationHistory_Then_EveryMigrationIsApplied()
    {
        // Given
        await using var context = _database.CreateContext();

        // When
        var applied = await context.Database.GetAppliedMigrationsAsync();

        // Then
        applied.Should().Equal(context.Database.GetMigrations());
    }
}
```

- [ ] **Step 3: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Integration 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9]+,[0-9]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected:

```
MigrationTests.cs(1,45): error CS0234: The type or namespace name 'Fixtures' does not exist in the namespace 'Ambev.DeveloperEvaluation.Integration' (are you missing an assembly reference?)
MigrationTests.cs(11,13): error CS0103: The name 'DatabaseCollection' does not exist in the current context
MigrationTests.cs(14,22): error CS0246: The type or namespace name 'PostgreSqlFixture' could not be found (are you missing a using directive or an assembly reference?)
MigrationTests.cs(20,27): error CS0246: The type or namespace name 'PostgreSqlFixture' could not be found (are you missing a using directive or an assembly reference?)
```

- [ ] **Step 4: Write the fixture and the collection**

Create `tests/Ambev.DeveloperEvaluation.Integration/Fixtures/PostgreSqlFixture.cs`:

```csharp
using Ambev.DeveloperEvaluation.ORM;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Fixtures;

/// <summary>
/// Runs one PostgreSQL 13 container for the whole integration test run, with every migration applied.
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:13").Build();

    /// <summary>
    /// Gets the connection string of the running container.
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Creates a context connected to the container. The caller disposes it.
    /// </summary>
    /// <returns>A new <see cref="DefaultContext"/>.</returns>
    public DefaultContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new DefaultContext(options);
    }

    /// <summary>
    /// Starts the container and applies all migrations.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Stops and removes the container.
    /// </summary>
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
```

Create `tests/Ambev.DeveloperEvaluation.Integration/Fixtures/DatabaseCollection.cs`:

```csharp
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Fixtures;

/// <summary>
/// Groups every database-backed integration test, so one container serves the whole run.
/// </summary>
[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<PostgreSqlFixture>
{
    /// <summary>
    /// The collection name to put in <c>[Collection(DatabaseCollection.Name)]</c>.
    /// </summary>
    public const string Name = "Database";
}
```

- [ ] **Step 5: Build, then run against Docker (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Integration 2>&1 | grep -E 'Warning\(s\)|Error\(s\)|Build succeeded|CS0618'
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build 2>&1 | grep -E 'Passed!|Failed!|Docker' | cut -c1-160
```

Expected:
- `Build succeeded.` and `0 Error(s)`, with no `CS0618` line. The only warnings are the pre-existing NU1903 and CS8604, and an incremental build can show fewer.
- **(derived)** `Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, … - Ambev.DeveloperEvaluation.Integration.dll (net8.0)`. The first run pulls images.

If the output says `Docker is either not running or misconfigured … Failed to connect to Docker endpoint at 'npipe://./pipe/docker_engine'`, Docker stopped: go back to the Docker gate.

- [ ] **Step 6: Commit and scan**

```bash
git add tests/Ambev.DeveloperEvaluation.Integration/Ambev.DeveloperEvaluation.Integration.csproj tests/Ambev.DeveloperEvaluation.Integration/Fixtures/PostgreSqlFixture.cs tests/Ambev.DeveloperEvaluation.Integration/Fixtures/DatabaseCollection.cs tests/Ambev.DeveloperEvaluation.Integration/ORM/MigrationTests.cs
git commit -m "test(integration): run the tests against a migrated PostgreSQL 13 container" -m "An xUnit collection fixture starts one postgres:13 container per run with Testcontainers and applies every migration. The first test checks the migration history."
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `4 files changed, 105 insertions(+)` and `Scan complete: 0 issue(s) found`.

---

### Task 8: Add the missing `Users` timestamp columns

**Skills:** `efcore-patterns` (CLI-only migrations), `testcontainers-integration-tests`, `dotnet-best-practices`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Integration/TestData/UserTestData.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Integration/ORM/UserRepositoryTests.cs`
- Generate: `src/Ambev.DeveloperEvaluation.ORM/Migrations/<timestamp>_AddUserTimestamps.cs` and `<timestamp>_AddUserTimestamps.Designer.cs`
- Modify (generated): `src/Ambev.DeveloperEvaluation.ORM/Migrations/DefaultContextModelSnapshot.cs`

- [ ] **Step 1: Write the test data builder**

Create `tests/Ambev.DeveloperEvaluation.Integration/TestData/UserTestData.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Bogus;

namespace Ambev.DeveloperEvaluation.Integration.TestData;

/// <summary>
/// Generates valid <see cref="User"/> entities with Bogus, within the column limits of the Users table.
/// </summary>
public static class UserTestData
{
    private static readonly Faker<User> UserFaker = new Faker<User>()
        .RuleFor(u => u.Username, f => f.Internet.UserName())
        .RuleFor(u => u.Password, f => $"Test@{f.Random.Number(100, 999)}")
        .RuleFor(u => u.Email, f => f.Internet.Email())
        .RuleFor(u => u.Phone, f => $"+55{f.Random.Number(11, 99)}{f.Random.Number(100000000, 999999999)}")
        .RuleFor(u => u.Status, f => f.PickRandom(UserStatus.Active, UserStatus.Suspended))
        .RuleFor(u => u.Role, f => f.PickRandom(UserRole.Customer, UserRole.Admin));

    /// <summary>
    /// Generates a valid user that has not been saved yet.
    /// </summary>
    /// <returns>A new <see cref="User"/> with random valid data.</returns>
    public static User GenerateValidUser() => UserFaker.Generate();
}
```

- [ ] **Step 2: Write the failing test**

Create `tests/Ambev.DeveloperEvaluation.Integration/ORM/UserRepositoryTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Integration.Fixtures;
using Ambev.DeveloperEvaluation.Integration.TestData;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.ORM;

/// <summary>
/// Contains integration tests for <see cref="UserRepository"/> against the migrated PostgreSQL schema.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class UserRepositoryTests
{
    private readonly PostgreSqlFixture _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserRepositoryTests"/> class.
    /// </summary>
    /// <param name="database">The collection's database, injected by xUnit.</param>
    public UserRepositoryTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    /// <summary>
    /// Tests that a saved user, including its timestamps, reads back unchanged from a fresh context.
    /// </summary>
    [Fact(DisplayName = "Given a valid user When it is saved Then a new context reads it back unchanged")]
    public async Task Given_ValidUser_When_Saved_Then_NewContextReadsItBackUnchanged()
    {
        // Given
        var user = UserTestData.GenerateValidUser();

        // When
        await using (var writeContext = _database.CreateContext())
        {
            await new UserRepository(writeContext).CreateAsync(user);
        }

        await using var readContext = _database.CreateContext();
        var saved = await new UserRepository(readContext).GetByIdAsync(user.Id);

        // Then (timestamptz keeps microseconds; DateTime keeps 100 ns ticks)
        saved.Should().BeEquivalentTo(user, options => options
            .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromMicroseconds(1)))
            .WhenTypeIs<DateTime>());
    }
}
```

The database generates the key (`gen_random_uuid()`), and EF writes it back into `user.Id` on save. So `user` is the complete expectation.

- [ ] **Step 3: Run it and watch the insert fail (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Integration 2>&1 | grep -E 'Error\(s\)|Build succeeded'
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build 2>&1 | grep -E 'Passed!|Failed!|42703|DbUpdateException' | cut -c1-160
```

Expected: `Build succeeded.`, then **(derived)** a `Microsoft.EntityFrameworkCore.DbUpdateException : An error occurred while saving the entity changes. …` whose inner `Npgsql.PostgresException` reads `42703: column "CreatedAt" of relation "Users" does not exist`, and `Failed!  - Failed:     1, Passed:     1, Skipped:     0, Total:     2`. This is the template bug: `User` has `CreatedAt` and `UpdatedAt`, but no migration creates them.

- [ ] **Step 4: Confirm the model is ahead of the migrations**

```bash
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -v NU1903; echo "exit=${PIPESTATUS[0]}"
```

Expected: `Changes have been made to the model since the last migration. Add a new migration.` and `exit=1`.

- [ ] **Step 5: Generate the migration with the documented command**

This is the command the README will document (ticket 13), run from the repo root:

```bash
dotnet ef migrations add AddUserTimestamps --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -v NU1903
git status --short
```

Expected: `Done. To undo this action, use 'ef migrations remove'`, then:

```
 M src/Ambev.DeveloperEvaluation.ORM/Migrations/DefaultContextModelSnapshot.cs
?? docs/superpowers/tickets/
?? src/Ambev.DeveloperEvaluation.ORM/Migrations/<timestamp>_AddUserTimestamps.Designer.cs
?? src/Ambev.DeveloperEvaluation.ORM/Migrations/<timestamp>_AddUserTimestamps.cs
?? tests/Ambev.DeveloperEvaluation.Integration/ORM/UserRepositoryTests.cs
?? tests/Ambev.DeveloperEvaluation.Integration/TestData/
```

- [ ] **Step 6: Check what was generated (don't edit it)**

```bash
cat src/Ambev.DeveloperEvaluation.ORM/Migrations/*_AddUserTimestamps.cs
git diff src/Ambev.DeveloperEvaluation.ORM/Migrations/DefaultContextModelSnapshot.cs | grep -E '^[+-] '
dotnet ef migrations script 20241014011203_InitialMigrations AddUserTimestamps --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -E 'ALTER|INSERT|VALUES'
```

Expected:
- `Up` has two `AddColumn` calls on `"Users"`: `CreatedAt` (`type: "timestamp with time zone"`, `nullable: false`, `defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)`) and `UpdatedAt` (`type: "timestamp with time zone"`, `nullable: true`). `Down` has two matching `DropColumn` calls.
- The snapshot gains `b.Property<DateTime>("CreatedAt")` and `b.Property<DateTime?>("UpdatedAt")`, each with `.HasColumnType("timestamp with time zone");`.
- The SQL:

  ```
  ALTER TABLE "Users" ADD "CreatedAt" timestamp with time zone NOT NULL DEFAULT TIMESTAMPTZ '-infinity';
  ALTER TABLE "Users" ADD "UpdatedAt" timestamp with time zone;
  INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
  VALUES ('<timestamp>_AddUserTimestamps', '8.0.10');
  ```

`timestamp with time zone` is `timestamptz`, as the ticket requires. The `-infinity` default only affects rows that already exist, and there are none (Decision 4).

- [ ] **Step 7: Verify (GREEN)**

```bash
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -v NU1903; echo "exit=${PIPESTATUS[0]}"
dotnet test tests/Ambev.DeveloperEvaluation.Integration 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `No changes have been made to the model since the last migration.` and `exit=0`; then **(derived)** `Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2`.

- [ ] **Step 8: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.ORM/Migrations/*_AddUserTimestamps.cs src/Ambev.DeveloperEvaluation.ORM/Migrations/*_AddUserTimestamps.Designer.cs src/Ambev.DeveloperEvaluation.ORM/Migrations/DefaultContextModelSnapshot.cs tests/Ambev.DeveloperEvaluation.Integration/ORM/UserRepositoryTests.cs tests/Ambev.DeveloperEvaluation.Integration/TestData/UserTestData.cs
git diff --cached --stat
git commit -m "fix(orm): add the missing Users timestamp columns" -m "User has CreatedAt and UpdatedAt, but InitialMigrations and the model snapshot never had the columns, so every user insert failed once migrations ran. The AddUserTimestamps migration adds them as timestamptz, UpdatedAt nullable, and an integration test saves and reads a user."
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: 5 files, `198 insertions(+)` (Designer 78, migration 40, snapshot 6, `UserRepositoryTests.cs` 49, `UserTestData.cs` 25), and `Scan complete: 0 issue(s) found`.

---

### Task 9: Reset the integration data after each test class

**Skills:** `testcontainers-integration-tests`, `dotnet-best-practices`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Integration/Fixtures/DataResetTests.cs`
- Modify (full rewrite): `tests/Ambev.DeveloperEvaluation.Integration/Fixtures/PostgreSqlFixture.cs` (adds `ResetDataAsync`)
- Create: `tests/Ambev.DeveloperEvaluation.Integration/Fixtures/DataResetFixture.cs`
- Modify (full rewrite): `tests/Ambev.DeveloperEvaluation.Integration/Fixtures/DatabaseCollection.cs` (adds the class fixture)

- [ ] **Step 1: Write the failing tests**

Create `tests/Ambev.DeveloperEvaluation.Integration/Fixtures/DataResetTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Integration.TestData;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Fixtures;

/// <summary>
/// Contains tests for <see cref="PostgreSqlFixture.ResetDataAsync"/>, which runs after every test class.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class DataResetTests
{
    private readonly PostgreSqlFixture _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataResetTests"/> class.
    /// </summary>
    /// <param name="database">The collection's database, injected by xUnit.</param>
    public DataResetTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    /// <summary>
    /// Tests that the reset empties the listed tables.
    /// </summary>
    [Fact(DisplayName = "Given a saved user When the data is reset Then the Users table is empty")]
    public async Task Given_SavedUser_When_DataIsReset_Then_UsersTableIsEmpty()
    {
        // Given
        await using (var context = _database.CreateContext())
        {
            await new UserRepository(context).CreateAsync(UserTestData.GenerateValidUser());
        }

        // When
        await _database.ResetDataAsync(DataResetFixture.Tables, DataResetFixture.Sequences);

        // Then
        await using var readContext = _database.CreateContext();
        (await readContext.Users.CountAsync()).Should().Be(0);
    }

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

    private async Task ExecuteAsync(string sql)
    {
        await using var dataSource = NpgsqlDataSource.Create(_database.ConnectionString);
        await using var command = dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<long> NextValueAsync(string sequence)
    {
        await using var dataSource = NpgsqlDataSource.Create(_database.ConnectionString);
        await using var command = dataSource.CreateCommand($"SELECT nextval('{sequence}');");
        return (long)(await command.ExecuteScalarAsync())!;
    }
}
```

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Integration 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9]+,[0-9]+\): error CS[0-9]+: [^[]+' | sort -u | cut -c1-110
```

Expected:

```
DataResetTests.cs(40,25): error CS1061: 'PostgreSqlFixture' does not contain a definition for 'ResetDataAsync'
DataResetTests.cs(40,40): error CS0103: The name 'DataResetFixture' does not exist in the current context
DataResetTests.cs(40,65): error CS0103: The name 'DataResetFixture' does not exist in the current context
DataResetTests.cs(62,29): error CS1061: 'PostgreSqlFixture' does not contain a definition for 'ResetDataAsync'
```

- [ ] **Step 3: Add `ResetDataAsync` to the fixture**

Replace `tests/Ambev.DeveloperEvaluation.Integration/Fixtures/PostgreSqlFixture.cs` with:

```csharp
using Ambev.DeveloperEvaluation.ORM;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Fixtures;

/// <summary>
/// Runs one PostgreSQL 13 container for the whole integration test run, with every migration applied.
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:13").Build();

    /// <summary>
    /// Gets the connection string of the running container.
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Creates a context connected to the container. The caller disposes it.
    /// </summary>
    /// <returns>A new <see cref="DefaultContext"/>.</returns>
    public DefaultContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new DefaultContext(options);
    }

    /// <summary>
    /// Starts the container and applies all migrations.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Empties the given tables and restarts the given sequences.
    /// </summary>
    /// <param name="tables">Tables to truncate.</param>
    /// <param name="sequences">Sequences to restart from their start value.</param>
    public async Task ResetDataAsync(IReadOnlyCollection<string> tables, IReadOnlyCollection<string> sequences)
    {
        var statements = new List<string>();
        if (tables.Count > 0)
            statements.Add($"TRUNCATE TABLE {string.Join(", ", tables.Select(Quote))};");
        statements.AddRange(sequences.Select(sequence => $"ALTER SEQUENCE {Quote(sequence)} RESTART;"));

        if (statements.Count == 0)
            return;

        await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
        await using var command = dataSource.CreateCommand(string.Join(Environment.NewLine, statements));
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Stops and removes the container.
    /// </summary>
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    private static string Quote(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";
}
```

- [ ] **Step 4: Add the reset fixture and declare it on the collection**

Create `tests/Ambev.DeveloperEvaluation.Integration/Fixtures/DataResetFixture.cs`:

```csharp
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Fixtures;

/// <summary>
/// Resets the data after each test class in <see cref="DatabaseCollection"/>, so the next class starts with empty tables.
/// </summary>
public sealed class DataResetFixture : IAsyncLifetime
{
    /// <summary>
    /// The tables the tests write to. Add a table here when a migration creates one.
    /// </summary>
    public static readonly IReadOnlyCollection<string> Tables = ["Users"];

    /// <summary>
    /// The sequences the tests advance. Add a sequence here when a migration creates one.
    /// </summary>
    public static readonly IReadOnlyCollection<string> Sequences = [];

    private readonly PostgreSqlFixture _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataResetFixture"/> class.
    /// </summary>
    /// <param name="database">The collection's database, injected by xUnit.</param>
    public DataResetFixture(PostgreSqlFixture database)
    {
        _database = database;
    }

    /// <inheritdoc />
    public Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Runs after the last test of the class.
    /// </summary>
    public Task DisposeAsync() => _database.ResetDataAsync(Tables, Sequences);
}
```

Replace `tests/Ambev.DeveloperEvaluation.Integration/Fixtures/DatabaseCollection.cs` with:

```csharp
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Fixtures;

/// <summary>
/// Groups every database-backed integration test: one container for the run, and a data reset after each test class.
/// </summary>
[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<PostgreSqlFixture>, IClassFixture<DataResetFixture>
{
    /// <summary>
    /// The collection name to put in <c>[Collection(DatabaseCollection.Name)]</c>.
    /// </summary>
    public const string Name = "Database";
}
```

Every class marked `[Collection(DatabaseCollection.Name)]` now gets a `DataResetFixture`, and its `DisposeAsync` runs after the class's last test (Decision 9).

- [ ] **Step 5: Build, list and run (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Integration 2>&1 | grep -E 'Error\(s\)|Build succeeded'
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build --list-tests 2>&1 | grep -E '^\s+Given'
dotnet test tests/Ambev.DeveloperEvaluation.Integration --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `Build succeeded.` and `0 Error(s)`, then these four tests:

```
    Given the PostgreSQL fixture When reading the migration history Then every migration is applied
    Given a valid user When it is saved Then a new context reads it back unchanged
    Given a saved user When the data is reset Then the Users table is empty
    Given an advanced sequence When the data is reset Then the sequence starts over at 1
```

and **(derived)** `Passed!  - Failed:     0, Passed:     4, Skipped:     0, Total:     4`.

- [ ] **Step 6: Commit and scan**

```bash
git add tests/Ambev.DeveloperEvaluation.Integration/Fixtures/DataResetTests.cs tests/Ambev.DeveloperEvaluation.Integration/Fixtures/DataResetFixture.cs tests/Ambev.DeveloperEvaluation.Integration/Fixtures/PostgreSqlFixture.cs tests/Ambev.DeveloperEvaluation.Integration/Fixtures/DatabaseCollection.cs
git commit -m "test(integration): reset tables and sequences after each test class" -m "A class fixture declared on the collection truncates the listed tables and restarts the listed sequences after every test class, so each class starts from empty tables. Ticket 05 adds the Sales tables and sale_number_seq to the lists."
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `4 files changed, 149 insertions(+), 2 deletions(-)` and `Scan complete: 0 issue(s) found`.

---

### Task 10: Apply pending migrations at startup in Development

**Skills:** `testcontainers-integration-tests`, `efcore-patterns`, `dependency-injection-patterns` (scope lifetime), `dotnet-best-practices`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Functional/Ambev.DeveloperEvaluation.Functional.csproj` (one package)
- Create: `tests/Ambev.DeveloperEvaluation.Functional/Health/HealthCheckTests.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Functional/Startup/StartupMigrationTests.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiFixture.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiCollection.cs`
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Program.cs` (after `builder.Build()`)

- [ ] **Step 1: Add Testcontainers to the functional project**

```bash
dotnet add tests/Ambev.DeveloperEvaluation.Functional/Ambev.DeveloperEvaluation.Functional.csproj package Testcontainers.PostgreSql --version 4.15.0
git diff tests/Ambev.DeveloperEvaluation.Functional/Ambev.DeveloperEvaluation.Functional.csproj | grep -E '^[+-] '
```

Expected: `+    <PackageReference Include="Testcontainers.PostgreSql" Version="4.15.0" />`.

- [ ] **Step 2: Write the tests**

Create `tests/Ambev.DeveloperEvaluation.Functional/Health/HealthCheckTests.cs`:

```csharp
using System.Net;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Health;

/// <summary>
/// Contains functional tests for the health check endpoint.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class HealthCheckTests
{
    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthCheckTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public HealthCheckTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// Tests that the running API reports itself healthy.
    /// </summary>
    [Fact(DisplayName = "Given the API is running When GET /health Then returns 200 OK")]
    public async Task Given_RunningApi_When_GetHealth_Then_ReturnsOk()
    {
        // Given
        using var client = _api.CreateClient();

        // When
        using var response = await client.GetAsync("/health");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

Create `tests/Ambev.DeveloperEvaluation.Functional/Startup/StartupMigrationTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Ambev.DeveloperEvaluation.ORM;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Startup;

/// <summary>
/// Contains functional tests for the migrations the API applies when it starts in Development.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class StartupMigrationTests
{
    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupMigrationTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public StartupMigrationTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// Tests that starting the API in Development applied every migration to the empty database.
    /// </summary>
    [Fact(DisplayName = "Given the API started in Development When reading the migration history Then every migration is applied")]
    public async Task Given_ApiStartedInDevelopment_When_ReadingMigrationHistory_Then_EveryMigrationIsApplied()
    {
        // Given the API started in Development against an empty database (ApiFixture)
        await using var scope = _api.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DefaultContext>();

        // When
        var applied = await context.Database.GetAppliedMigrationsAsync();

        // Then
        applied.Should().Equal(context.Database.GetMigrations());
    }
}
```

- [ ] **Step 3: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Functional 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9]+,[0-9]+\): error CS[0-9]+: [^[]+' | sort -u | cut -c1-100
```

Expected: eight errors, each saying that `ApiCollection`, `ApiFixture` or the `Ambev.DeveloperEvaluation.Functional.Fixtures` namespace doesn't exist: four in `HealthCheckTests.cs` and four in `StartupMigrationTests.cs`.

- [ ] **Step 4: Write the fixture and the collection**

Create `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiFixture.cs`:

```csharp
using Ambev.DeveloperEvaluation.WebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Fixtures;

/// <summary>
/// Hosts the API in memory for the whole functional test run, connected to one PostgreSQL 13 container.
/// The API runs in Development, so it applies the migrations itself at startup.
/// </summary>
public sealed class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:13").Build();

    /// <summary>
    /// Starts the container, then the API.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        // Reading Server builds and starts the API now, so its startup migrations finish before the first test.
        _ = Server;
    }

    /// <summary>
    /// Stops the API, then removes the container.
    /// </summary>
    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _database.DisposeAsync();
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.UseSetting("ConnectionStrings:DefaultConnection", _database.GetConnectionString());
    }
}
```

`UseSetting` reaches the configuration that `Program.Main` reads before `builder.Build()`. Task 5's test proves this: it overrides `Jwt:SecretKey` the same way. So the API uses the container's connection string from the start.

Create `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiCollection.cs`:

```csharp
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Fixtures;

/// <summary>
/// Groups every database-backed functional test, so one container and one in-memory API serve the whole run.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
    /// <summary>
    /// The collection name to put in <c>[Collection(ApiCollection.Name)]</c>.
    /// </summary>
    public const string Name = "Api";
}
```

- [ ] **Step 5: Build, then run and watch the migration test fail (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Functional 2>&1 | grep -E 'Error\(s\)|Build succeeded'
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --list-tests 2>&1 | grep -E '^\s+Given'
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build 2>&1 | grep -E 'Passed!|Failed!|Expected applied' | cut -c1-160
```

Expected: `Build succeeded.`; three tests listed (the startup failure, startup migration and health tests). **(derived)** The migration test fails with `Expected applied to be equal to {"20241014011203_InitialMigrations", "<timestamp>_AddUserTimestamps"}, but {empty} contains 2 item(s) less.`, the health and startup-failure tests pass, and the summary is `Failed!  - Failed:     1, Passed:     2, Skipped:     0, Total:     3`. Nothing migrates the database yet, which is the bug.

- [ ] **Step 6: Apply pending migrations after `builder.Build()` in Development**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.WebApi/Program.cs`, replace

```csharp
            var app = builder.Build();
            app.UseMiddleware<ValidationExceptionMiddleware>();
```

with

```csharp
            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                using var scope = app.Services.CreateScope();
                scope.ServiceProvider.GetRequiredService<DefaultContext>().Database.Migrate();
            }

            app.UseMiddleware<ValidationExceptionMiddleware>();
```

`DefaultContext` is scoped, so it's resolved from a scope that is disposed once the migration finishes. `Program.cs` already has `using Ambev.DeveloperEvaluation.ORM;` and `using Microsoft.EntityFrameworkCore;`.

- [ ] **Step 7: Verify (GREEN), and that the Docker-free checks still pass**

```bash
git diff --stat src/Ambev.DeveloperEvaluation.WebApi/Program.cs
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E 'Warning\(s\)|Error\(s\)|Build succeeded'
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet ef migrations list --no-connect --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -E '^20|rror'
```

Expected:
- `src/Ambev.DeveloperEvaluation.WebApi/Program.cs | 7 +++++++`.
- `Build succeeded.`, `0 Error(s)`.
- **(derived)** `Passed!  - Failed:     0, Passed:     3, Skipped:     0, Total:     3`.
- `Passed:    49` for the unit tests.
- `20241014011203_InitialMigrations` and `<timestamp>_AddUserTimestamps`. `dotnet ef` still works without a database, because the EF tools stop `Program.Main` at `builder.Build()`, before the migration code.

- [ ] **Step 8: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.WebApi/Program.cs tests/Ambev.DeveloperEvaluation.Functional/Ambev.DeveloperEvaluation.Functional.csproj tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiFixture.cs tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiCollection.cs tests/Ambev.DeveloperEvaluation.Functional/Health/HealthCheckTests.cs tests/Ambev.DeveloperEvaluation.Functional/Startup/StartupMigrationTests.cs
git commit -m "fix(webapi): apply pending migrations at startup in Development" -m "Nothing applied migrations, so a fresh database had no schema. In Development the API now migrates before it starts serving, which covers docker compose, dotnet run and the functional tests. The functional tests host the API with WebApplicationFactory against one postgres:13 container per run."
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `6 files changed, 150 insertions(+)` and `Scan complete: 0 issue(s) found`.

---

### Task 11: Reset the functional data after each test class

**Skills:** `testcontainers-integration-tests`, `dotnet-best-practices`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/DataResetTests.cs`
- Modify (full rewrite): `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiFixture.cs` (adds `ResetDataAsync`)
- Create: `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/DataResetFixture.cs`
- Modify (full rewrite): `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiCollection.cs` (adds the class fixture)

- [ ] **Step 1: Write the failing test**

Create `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/DataResetTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.ORM;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Fixtures;

/// <summary>
/// Contains tests for <see cref="ApiFixture.ResetDataAsync"/>, which runs after every test class.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class DataResetTests
{
    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataResetTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public DataResetTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// Tests that the reset empties the tables listed in <see cref="DataResetFixture.Tables"/>.
    /// </summary>
    [Fact(DisplayName = "Given a user saved through the API's services When the data is reset Then the Users table is empty")]
    public async Task Given_SavedUser_When_DataIsReset_Then_UsersTableIsEmpty()
    {
        // Given
        await using (var scope = _api.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            await users.CreateAsync(new User
            {
                Username = "reset.probe",
                Email = "reset.probe@example.com",
                Phone = "+5511999999999",
                Password = "Test@123",
                Role = UserRole.Customer,
                Status = UserStatus.Active,
            });
        }

        // When
        await _api.ResetDataAsync(DataResetFixture.Tables, DataResetFixture.Sequences);

        // Then
        await using var readScope = _api.Services.CreateAsyncScope();
        var context = readScope.ServiceProvider.GetRequiredService<DefaultContext>();
        (await context.Users.CountAsync()).Should().Be(0);
    }
}
```

- [ ] **Step 2: Build and watch it fail to compile (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Functional 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9]+,[0-9]+\): error CS[0-9]+: [^[]+' | sort -u | cut -c1-110
```

Expected:

```
DataResetTests.cs(51,20): error CS1061: 'ApiFixture' does not contain a definition for 'ResetDataAsync' and no
DataResetTests.cs(51,35): error CS0103: The name 'DataResetFixture' does not exist in the current context
DataResetTests.cs(51,60): error CS0103: The name 'DataResetFixture' does not exist in the current context
```

- [ ] **Step 3: Add `ResetDataAsync` to the API fixture**

Replace `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiFixture.cs` with:

```csharp
using Ambev.DeveloperEvaluation.WebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Fixtures;

/// <summary>
/// Hosts the API in memory for the whole functional test run, connected to one PostgreSQL 13 container.
/// The API runs in Development, so it applies the migrations itself at startup.
/// </summary>
public sealed class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:13").Build();

    /// <summary>
    /// Starts the container, then the API.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        // Reading Server builds and starts the API now, so its startup migrations finish before the first test.
        _ = Server;
    }

    /// <summary>
    /// Empties the given tables and restarts the given sequences.
    /// </summary>
    /// <param name="tables">Tables to truncate.</param>
    /// <param name="sequences">Sequences to restart from their start value.</param>
    public async Task ResetDataAsync(IReadOnlyCollection<string> tables, IReadOnlyCollection<string> sequences)
    {
        var statements = new List<string>();
        if (tables.Count > 0)
            statements.Add($"TRUNCATE TABLE {string.Join(", ", tables.Select(Quote))};");
        statements.AddRange(sequences.Select(sequence => $"ALTER SEQUENCE {Quote(sequence)} RESTART;"));

        if (statements.Count == 0)
            return;

        await using var dataSource = NpgsqlDataSource.Create(_database.GetConnectionString());
        await using var command = dataSource.CreateCommand(string.Join(Environment.NewLine, statements));
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Stops the API, then removes the container.
    /// </summary>
    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _database.DisposeAsync();
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.UseSetting("ConnectionStrings:DefaultConnection", _database.GetConnectionString());
    }

    private static string Quote(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";
}
```

- [ ] **Step 4: Add the reset fixture and declare it on the collection**

Create `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/DataResetFixture.cs`:

```csharp
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Fixtures;

/// <summary>
/// Resets the data after each test class in <see cref="ApiCollection"/>, so the next class starts with empty tables.
/// </summary>
public sealed class DataResetFixture : IAsyncLifetime
{
    /// <summary>
    /// The tables the tests write to. Add a table here when a migration creates one.
    /// </summary>
    public static readonly IReadOnlyCollection<string> Tables = ["Users"];

    /// <summary>
    /// The sequences the tests advance. Add a sequence here when a migration creates one.
    /// </summary>
    public static readonly IReadOnlyCollection<string> Sequences = [];

    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataResetFixture"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public DataResetFixture(ApiFixture api)
    {
        _api = api;
    }

    /// <inheritdoc />
    public Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Runs after the last test of the class.
    /// </summary>
    public Task DisposeAsync() => _api.ResetDataAsync(Tables, Sequences);
}
```

Replace `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiCollection.cs` with:

```csharp
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Fixtures;

/// <summary>
/// Groups every database-backed functional test: one container and one in-memory API for the run,
/// and a data reset after each test class.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>, IClassFixture<DataResetFixture>
{
    /// <summary>
    /// The collection name to put in <c>[Collection(ApiCollection.Name)]</c>.
    /// </summary>
    public const string Name = "Api";
}
```

- [ ] **Step 5: Build, list and run (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Functional 2>&1 | grep -E 'Error\(s\)|Build succeeded'
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --list-tests 2>&1 | grep -E '^\s+Given'
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `Build succeeded.`, then these four tests:

```
    Given an empty JWT secret key When the API starts Then the startup exception reaches the host
    Given the API started in Development When reading the migration history Then every migration is applied
    Given the API is running When GET /health Then returns 200 OK
    Given a user saved through the API's services When the data is reset Then the Users table is empty
```

and **(derived)** `Passed!  - Failed:     0, Passed:     4, Skipped:     0, Total:     4`.

- [ ] **Step 6: Commit and scan**

```bash
git add tests/Ambev.DeveloperEvaluation.Functional/Fixtures/DataResetTests.cs tests/Ambev.DeveloperEvaluation.Functional/Fixtures/DataResetFixture.cs tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiFixture.cs tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiCollection.cs
git commit -m "test(functional): reset tables and sequences after each test class" -m "The same reset as the integration tests, run against the API's container after every functional test class."
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `4 files changed, 122 insertions(+), 2 deletions(-)` and `Scan complete: 0 issue(s) found`.

---

### Task 12: Run the whole system with `docker compose up`

No files change in this task; it checks the ticket's compose criteria against a real stack. Every output here is **(derived)**.

- [ ] **Step 1: Start from nothing, then build and start the stack**

```bash
docker compose config --services | sort
docker compose down -v --remove-orphans
docker compose up --build --wait --wait-timeout 300; echo "exit=$?"
```

Expected: the two services `ambev.developerevaluation.database` and `ambev.developerevaluation.webapi`. The `up` output shows the database container reach `Healthy` before the API container starts, and then `exit=0`.

If `up` reports `port is already allocated` or `address already in use` for 8080 or 5432, another program owns the port: stop and ask the user. If it reports `The container name "/ambev_developer_evaluation_…" is already in use`, a container from another project has that name: ask before removing it.

- [ ] **Step 2: Check what runs, where, and in what order**

```bash
docker compose ps --services --status running | sort
docker inspect --format '{{.State.Health.Status}}' ambev_developer_evaluation_database
docker compose port ambev.developerevaluation.webapi 8080
docker compose port ambev.developerevaluation.database 5432
```

Expected: exactly the two services; `healthy`; `0.0.0.0:8080`; `0.0.0.0:5432`.

- [ ] **Step 3: Check `/health` over HTTP on 8080**

```bash
curl --silent --show-error --fail --retry 30 --retry-delay 2 --retry-all-errors http://localhost:8080/health; echo
```

Expected: `{"status":"Healthy","healthChecks":[]}`. The rehearsal's `dotnet run` on 5119 returned exactly this body.

- [ ] **Step 4: Check that startup migrations created the schema**

```bash
docker compose exec -T ambev.developerevaluation.database psql -U developer -d developer_evaluation -At -c 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY 1;'
docker compose exec -T ambev.developerevaluation.database psql -U developer -d developer_evaluation -At -c "SELECT column_name, data_type, is_nullable FROM information_schema.columns WHERE table_name = 'Users' AND column_name IN ('CreatedAt', 'UpdatedAt') ORDER BY column_name;"
docker compose logs ambev.developerevaluation.webapi 2>&1 | grep -E 'Applying migration|Now listening|terminated' | cut -c1-160
```

Expected:

```
20241014011203_InitialMigrations
<timestamp>_AddUserTimestamps
CreatedAt|timestamp with time zone|NO
UpdatedAt|timestamp with time zone|YES
```

The log lines are `Applying migration '20241014011203_InitialMigrations'.`, `Applying migration '<timestamp>_AddUserTimestamps'.` and `Now listening on: http://[::]:8080`, with no `Application terminated unexpectedly`.

- [ ] **Step 5: Take the stack down**

```bash
docker compose down -v
docker compose ps -a
```

Expected: the containers and the network are removed, and `ps -a` lists nothing.

---

### Task 13: Run the API with `dotnet run` against the compose database

No files change. It proves `dotnet run` migrates an empty compose database and serves `/health`. Every output here is **(derived)**, except that the background start, `curl` probe and `taskkill` pattern was rehearsed.

- [ ] **Step 1: Start only the database, empty**

```bash
docker compose up -d --wait ambev.developerevaluation.database; echo "exit=$?"
docker compose exec -T ambev.developerevaluation.database psql -U developer -d developer_evaluation -At -c "SELECT count(*) FROM information_schema.tables WHERE table_name = '__EFMigrationsHistory';"
```

Expected: `exit=0` and `0`: no migrations have run yet.

- [ ] **Step 2: Run the API in the background, probe it, then stop it (one Bash call)**

Keep these commands in one Bash call, so the background process is stopped in the same shell that started it:

```bash
dotnet run --project src/Ambev.DeveloperEvaluation.WebApi > /tmp/ticket02-checks/run.log 2>&1 &
curl --silent --show-error --fail --retry 60 --retry-delay 2 --retry-all-errors http://localhost:5119/health; echo
docker compose exec -T ambev.developerevaluation.database psql -U developer -d developer_evaluation -At -c 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY 1;'
taskkill //F //IM Ambev.DeveloperEvaluation.WebApi.exe
wait
grep -E 'Applying migration|Now listening' /tmp/ticket02-checks/run.log | cut -c1-160
```

Expected: `{"status":"Healthy","healthChecks":[]}`, then the two migration ids, then `SUCCESS: The process "Ambev.DeveloperEvaluation.WebApi.exe" with PID … has been terminated.` (rehearsed), then the two `Applying migration …` lines and `Now listening on: http://localhost:5119`. The `http` launch profile runs in Development on port 5119 and uses `appsettings.json`'s connection string (`localhost:5432`).

- [ ] **Step 3: Take the database down and check nothing is left running**

```bash
docker compose down -v
tasklist //FI "IMAGENAME eq Ambev.DeveloperEvaluation.WebApi.exe" | grep -c Ambev
```

Expected: the container is removed, and the count is `0`.

---

### Task 14: Verify everything, review, then open a pull request into `develop`

- [ ] **Step 1: Re-run every check fresh (superpowers:verification-before-completion)**

```bash
git status --short
dotnet build Ambev.DeveloperEvaluation.sln --no-incremental 2>&1 | grep -E 'Warning\(s\)|Error\(s\)|Build succeeded'
dotnet test Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-100
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -v NU1903 | tail -1
python3 /tmp/ticket02-checks/check-compose.py | tail -1
slopwatch analyze --fail-on warning --stats 2>&1 | tail -3
```

Expected: `git status --short` lists only `?? docs/superpowers/tickets/` (and any other untracked plan files); `Build succeeded.`, `3 Warning(s)`, `0 Error(s)`; three `Passed!` lines: Unit `Passed:    49`, Integration `Passed:     4`, Functional `Passed:     4` **(derived for the last two)**; `No changes have been made to the model since the last migration.`; `compose problems: 0`; `Scan complete: 0 issue(s) found`.

Each ticket criterion and its evidence:

| Ticket criterion | Evidence |
|---|---|
| Npgsql connection string with the compose credentials | Task 4: `Database name: developer_evaluation`, `Data source: tcp://localhost:5432` |
| Compose runs only PostgreSQL 13 and the API; HTTP only on 8080; no 8081, certificates or user secrets; database on 5432 | `compose problems: 0`, plus Task 12 Steps 1–2 |
| API gets `ConnectionStrings__DefaultConnection`; waits for the `pg_isready` healthcheck | the compose check, plus Task 12 Step 1 (database `Healthy` before the API starts) |
| Startup migrations in Development; `Users` exists and `/health` returns 200 after `docker compose up` | Task 12 Steps 3–4, plus the functional `StartupMigrationTests` |
| `dotnet run` works against the compose database | Task 13 Step 2 |
| `Program.Main` rethrows; non-zero exit with the error printed; `WebApplicationFactory` sees the real exception | Task 5 Steps 3 and 5, plus `StartupFailureTests` |
| `DefaultContextFactory` targets the ORM migrations assembly; the documented `dotnet ef migrations add` command works; `.config/dotnet-tools.json` pins `dotnet-ef` 8.0.10 | Task 3 Step 4, Task 8 Step 5, Task 2 Step 3 |
| Migration adds `Users.CreatedAt` (timestamptz, not null) and `UpdatedAt` (timestamptz, null); no pending model changes; the save-and-read test passes | Task 8 Steps 6–7, Task 12 Step 4, and the has-pending line above |
| Integration fixture runs `postgres:13` with all migrations; a basic test saves and reads a `User` | `MigrationTests`, `UserRepositoryTests` |
| Functional project references WebApi; one container and one `WebApplicationFactory<Program>` per run; Development; `/health` returns 200 | `ApiFixture`, `ApiCollection`, `HealthCheckTests` |
| Data reset between test classes (given tables and sequences); one collection per project | the `DataResetFixture` classes on both collections, and both `DataResetTests` |
| Package versions | Testcontainers.PostgreSql 4.15.0, FluentAssertions 6.12.0, Mvc.Testing 8.0.31 (Tasks 5, 7, 10) |
| Unit tests still pass; integration and functional pass with Docker | the three `Passed!` lines |
| Branch, pull request into `develop`, Conventional Commits, no attribution | Steps 4–6 |

- [ ] **Step 2: Audit the new tests (skill `test-anti-patterns`, report only)**

Run the skill on the seven new test classes: `tests/Ambev.DeveloperEvaluation.Integration/ORM/{MigrationTests,UserRepositoryTests}.cs`, `tests/Ambev.DeveloperEvaluation.Integration/Fixtures/DataResetTests.cs`, `tests/Ambev.DeveloperEvaluation.Functional/Startup/{StartupFailureTests,StartupMigrationTests}.cs`, `tests/Ambev.DeveloperEvaluation.Functional/Health/HealthCheckTests.cs` and `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/DataResetTests.cs`. If it finds a real problem, fix it in a separate `test: …` commit and re-run Step 1. Report everything else to the user. The sequence test's own `CREATE SEQUENCE` and `DROP SEQUENCE` are deliberate: no migration creates a sequence until ticket 05.

- [ ] **Step 3: Optional code review**

`superpowers:requesting-code-review` against `develop..feature/postgres-runtime`, if you want a second look before the pull request.

- [ ] **Step 4: Check the commit messages**

```bash
git log --format=%s develop..feature/postgres-runtime
git log --format=%B develop..feature/postgres-runtime | grep -n -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
```

Expected (plus any `test:` commit from Step 2):

```
test(functional): reset tables and sequences after each test class
fix(webapi): apply pending migrations at startup in Development
test(integration): reset tables and sequences after each test class
fix(orm): add the missing Users timestamp columns
test(integration): run the tests against a migrated PostgreSQL 13 container
build(docker): run only PostgreSQL and the API, over HTTP, in compose
fix(webapi): rethrow startup exceptions after logging them
fix(webapi): use the Npgsql connection string for the compose database
fix(orm): point the design-time factory at the ORM migrations
build: pin dotnet-ef 8.0.10 in a local tool manifest
no attribution lines
```

- [ ] **Step 5: Push the branch and open a pull request into `develop`**

This is the superpowers:finishing-a-development-branch step, with the option already fixed: a pull request into `develop` for the user to review. Don't merge it yourself.

Write the pull request body with the Write tool to `/tmp/ticket02-checks/pr-body.md`: two or three sentences on what the ticket delivers (the Goal above), the evidence table from Step 1 with this run's results, and what Step 2 reported. No `Co-Authored-By` trailer, no "Generated with" line and no session link. Then:

```bash
git push -u origin feature/postgres-runtime
gh pr create --base develop --head feature/postgres-runtime --title "Ticket 02: run the API against PostgreSQL" --body-file /tmp/ticket02-checks/pr-body.md
```

Expected: the push prints `* [new branch]      feature/postgres-runtime -> feature/postgres-runtime`, and `gh pr create` prints the pull request URL, `https://github.com/PR001-git/developer-store-api/pull/<n>`. If `gh` fails (for example, it isn't logged in; check with `gh auth status`), stop and tell the user: the branch is pushed, so they can open the pull request on GitHub.

- [ ] **Step 6: Verify the pull request**

```bash
gh pr view feature/postgres-runtime --json state,baseRefName,commits --jq '"\(.state) \(.baseRefName) \(.commits | length) commits"'
gh pr view feature/postgres-runtime --json title,body --jq '.title + "\n" + .body' | grep -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
git status -sb | head -1
git status --short
```

Expected: `OPEN develop 10 commits` (11 if Step 2 added a `test:` commit), `no attribution lines`, `## feature/postgres-runtime...origin/feature/postgres-runtime` with no `ahead` or `behind`, and only `?? docs/superpowers/tickets/` (plus any other untracked plan files).

Give the user the pull request URL when the run ends. Don't merge it: the user reviews it and merges it on GitHub with **Create a merge commit** (the `--no-ff` equivalent that the later plans' starting-point checks look for). Keep `feature/postgres-runtime`, since the ticket doesn't ask to delete it.

- [ ] **Step 7: Clean up**

```bash
docker compose ps -a
rm -rf /tmp/ticket02-checks
```

Expected: `ps -a` lists nothing (Tasks 12 and 13 took the stacks down), and `rm` prints nothing.
