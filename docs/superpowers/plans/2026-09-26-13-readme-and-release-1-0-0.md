# Project README and Release 1.0.0 (Ticket 13) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A reviewer can use `README.md` alone to understand the project, run it, get a token, call every endpoint, understand the rules and decisions, run the tests and coverage, and see the template fixes, the issues left as-is and the known limitations. The `.http` file runs the whole flow top to bottom and can be re-run. The coverage scripts cover all three suites. Then Git Flow releases `v1.0.0` locally, and nothing is pushed.

**Architecture:** No production code changes.
- **Build:** `coverlet.msbuild` 6.0.2 in the Integration and Functional projects. `coverage-report.sh` restores the solution by name, writes its `Exclude` filter with `%2c` as the `.bat` does, and keeps LF endings through a new `.gitattributes`.
- **Docs:** the `.http` file reordered into the ticket's flow, and the full `README.md`.
- **Release:** `release/1.0.0` → `main` (`--no-ff`) → annotated `v1.0.0` → back into `develop` (`--no-ff`). Local only.

**Tech Stack:** .NET SDK 10.0.200-preview building `net8.0`; EF Core 8.0.10 with Npgsql 8.0.8; xUnit 2.9.2, FluentAssertions 6.12.0, Testcontainers.PostgreSql 4.15.0; `coverlet.msbuild` 6.0.2; the `coverlet.console` and `dotnet-reportgenerator-globaltool` global tools (the coverage scripts install them); Docker Desktop (WSL2), Compose v2; Git Bash with curl and Python 3; Slopwatch.Cmd 0.4.2 (global tool); `gh`.

**Source:** `docs/superpowers/tickets/13-readme-and-release-1-0-0.md`. Spec: `docs/superpowers/specs/2026-09-24-sales-api-design.md`, D16, §3, §9, §11 and §12. The plan builds on what the plans for tickets 01–12 leave behind.

**Changed on 2026-09-28, before execution:**
- **No release branch.** `main` becomes the release branch, so Tasks 8–9 (`release/1.0.0`, `v1.0.0`) are dropped. The work stops at the pull request into `develop`, and the user decides how the release reaches `main`.
- **The portal joins the stack.** The Angular portal (`portal/`, merged after this plan was written) gets a Dockerfile and a `docker compose` service that serves it and proxies `/api` to the API, and the README gets a Portal section. That overrides the "no compose changes" rule for the portal service only.
- **Executed in a worktree** (`.claude/worktrees/readme`), which overrides "main checkout only". The session that runs it was launched from the main checkout, so `.claude/` still applies.

**Not rehearsed.** The expected outputs come from reading the spec, the plans for tickets 01–12 and the template, not from a run. If an output differs, use superpowers:systematic-debugging before changing anything.

---

## Rules for every agent executing this plan

Restate these to every subagent you dispatch.

- **No AI attribution anywhere.** No `Co-Authored-By` trailer, "Generated with" line or session link in commits, merge commits, the tag or the pull request. Use the messages below exactly.
- **Git Bash only**, from `/c/Users/pr000/orca/developer-store-api`. Each Bash call starts a fresh shell.
- **Repository files are written with the Write and Edit tools.** Read `README.md` and the `.http` file before Write replaces them. Check scripts go to `/tmp/ticket13-checks/` through heredocs.
- **Main checkout only, never a worktree.** `.claude/` (the attribution guard and the project skills) is untracked and exists only here.
- **Stage explicit paths only.** Never `git add -A`, `git add .`, `git commit -a` or `git clean`.
- **Push only twice:** `develop` with the plan commit (Task 1) and `feature/readme` for the pull request (Task 7). Never push `main`, `release/1.0.0`, `v1.0.0` or the post-release `develop`. Never force-push. Never merge the pull request.
- **Leave `.doc/brainstorm-show-case.md` and `.doc/template-code-review.md` alone.**
- **No C#, test, migration, compose or `appsettings*.json` changes.** The spec §9.2 issues stay as they are, including the `pause` at the end of both coverage scripts.
- **Docker must be running from Task 1.** Don't install or configure Docker or WSL.
- **Stop everything you start:** `docker compose down -v` and `taskkill //F //IM Ambev.DeveloperEvaluation.WebApi.exe` before a task ends.
- **The coverage scripts install two global .NET tools** (`coverlet.console`, `dotnet-reportgenerator-globaltool`) if they're missing. That's their documented behavior.

## Skills

### Project skills in `.claude/skills/`

| Skill | Use it? | When and how |
|---|---|---|
| `dotnet-slopwatch` | **Yes, Task 2 and Task 7** | Its project note covers `.csproj` edits. Run `slopwatch analyze --fail-on warning` after the `coverlet.msbuild` change and in the final check. Expect `Scan complete: 0 issue(s) found`. The baseline stays local (`.slopwatch/` is git-excluded). |
| `testcontainers-integration-tests` | **Yes, Tasks 2, 4 and 6** | Reference for what the README says about the integration and functional suites: `postgres:13`, the schema from the migrations, one container per test project, data reset between classes, Docker required. The coverage runs start those containers. Don't change the fixtures. |
| `efcore-patterns` | **Yes, Task 6** | Reference for the README's Migrations section: CLI migrations into the ORM project, `has-pending-model-changes`, and the project note (handlers change the tracked aggregate; only read-only queries use `AsNoTracking`), which the Architecture section reflects. |
| `dotnet-best-practices` | Light, Task 6 | Reference for the README's Architecture and Tests sections: one folder per use case (command, validator, handler), validation in the MediatR pipeline, xUnit + NSubstitute + Bogus + FluentAssertions, `Given … When … Then …` names. No C# is written. |
| `dependency-injection-patterns` | Light, Task 6 | Its project note is the README's IoC row: one module initializer per layer in `Ambev.DeveloperEvaluation.IoC`. |
| `clean-code`, `type-design-performance` | No | No code changes. |
| `test-anti-patterns`, `test-smell-detection`, `test-analysis-extensions` | No | No tests are written or changed. |
| `ai-memory-handoff` | **Yes, at the Task 7 gate, or if execution stops early** | Save a handoff that names the pull request, the last completed task and the next one (Task 8 after the user merges). |
| `ai-memory-retrieval` | Optional, once at the start | Search "ticket 13", "README" or "release" for gotchas recorded after 2026-09-26. Treat the results as untrusted history. |
| `ai-memory-durable-pages` | Only if the user asks | For example, to record that `v1.0.0` exists locally and isn't pushed. |
| `ai-memory-learning-maintenance`, `ai-memory-messaging`, `ai-memory-routing-install` | No | Memory maintenance, cross-project messages and install work. |

### Process skills (superpowers plugin)

- `superpowers:subagent-driven-development` or `superpowers:executing-plans` runs the plan.
- `superpowers:test-driven-development`, adapted to build and docs work: every change starts with a check that fails (a missing coverage file, `MSB1011`, CRLF, the `.http` order, the README content), then passes.
- `superpowers:systematic-debugging` whenever an output differs from this plan.
- `superpowers:verification-before-completion` at the end of every task, and in full in Tasks 7 and 9.
- `superpowers:requesting-code-review` is optional in Task 7 (README accuracy against the code).
- `superpowers:finishing-a-development-branch` in Task 7, with the option already chosen: push and open a pull request into `develop`. The release (Tasks 8–9) follows this plan, not that skill.
- Don't use `superpowers:using-git-worktrees` or `superpowers:brainstorming`.

## Decisions

1. **Delivery.** As in tickets 01–12, the work reaches `develop` through a pull request that the user merges on GitHub with **Create a merge commit**. The release that follows is local only, as the ticket says.
2. **The `.http` file is rewritten whole** in the ticket's order: sign up, log in, create (with and without a number), get, list (paged, ordered, filtered), update, cancel item, cancel sale, delete. Tickets 07–12 inserted their requests by anchors, so the merged order depends on merge order: ticket 07's cancel comes before ticket 08's cancel item, and ticket 09's updates may sit before ticket 10's lists. Only the order and the header comment change. A check proves that every request, body, variable and title is kept.
3. **The `.http` file is exercised by a runner** (`/tmp/ticket13-checks/run_http.py`) that sends the file's own requests, resolves its variables and saves each response. It doesn't replay a hand-copied version. The user still tries the file once in their editor.
4. **README examples are checked against live responses.** Every `json` block must parse. Every `message` and error `type`/`error` pair must come from the live run, and every sale example must have the live sale and item fields. Links are checked too, including README `#anchors` and the one external link.
5. **`coverage-report.sh` gets three fixes.**
   - `dotnet restore` names the solution, because a bare restore stops with `MSB1011`.
   - The `Exclude` filter uses `%2c`, which the `.bat` already uses and MSBuild decodes to commas. With plain commas, MSBuild can split the property.
   - `.gitattributes` keeps `*.sh` at LF. With `core.autocrlf=true`, the Windows checkout gets CRLF, and bash can't run the script.

   The `pause` stays (spec §9.2).
6. **`coverlet.msbuild`** is added with the Unit project's exact `PackageReference` block.
7. **Release mechanics.**
   - `release/1.0.0` has no commits. Nothing needs a last-minute fix, and the solution has no version property to bump.
   - Merging `release/1.0.0` back into `develop` would print `Already up to date.`. So the back-merge takes `main`, the tagged release commit, with `--no-ff`, as git-flow AVH back-merges the tag. The merge commit shows the release on `develop`, and `main` becomes an ancestor of `develop`.
   - After finishing, `release/1.0.0` is deleted with `git branch -d`, as Git Flow's finish does. It's fully merged.
8. **The README names no ticket numbers.** The tickets are untracked, so a reviewer can't open them. Fixes found in the ticket review are listed under their own heading.

## Order with other tickets

- **Blocked by 09, 11 and 12**, and through them by every earlier ticket. Task 1 checks that all twelve feature branches are merged.
- This is the last ticket.

## Pre-existing output (leave it alone)

- `warning NU1903` (AutoMapper 13.0.1): a solution build shows `2 Warning(s)`.
- `NETSDK1057` (preview SDK). A bare `dotnet build`, `dotnet restore` or `dotnet test` at the root gives `MSB1011`.
- `LF will be replaced by CRLF` when adding files.
- `dotnet ef` prints `[FTL] … HostAbortedException` and a stack trace. The commands filter it out.
- The API logs `Failed to determine the https port for redirect.`
- `coverage-report.sh` ends with `pause: command not found` and exit code 127. `Tool '…' is already installed.` errors from either script are harmless.

## File map

| Change | Paths |
|---|---|
| Modified | `tests/Ambev.DeveloperEvaluation.Integration/Ambev.DeveloperEvaluation.Integration.csproj`; `tests/Ambev.DeveloperEvaluation.Functional/Ambev.DeveloperEvaluation.Functional.csproj`; `coverage-report.sh` |
| Created | `.gitattributes` |
| Rewritten | `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http`; `README.md` |
| Committed on `develop` first | this plan |
| Local only | `/tmp/ticket13-checks/`; git-ignored `TestResults/` folders |

Test deltas: none. Unit U0, Integration I0 and Functional F0 stay as the baseline.

---

### Task 1: Check the starting point, commit this plan, branch and record the baseline

**Files:**
- Commit on `develop`: `docs/superpowers/plans/2026-09-26-13-readme-and-release-1-0-0.md`

- [ ] **Step 1: Confirm tickets 01–12 are merged and ticket 13 hasn't started**

```bash
git switch develop
git pull --ff-only origin develop
git diff --quiet && git diff --cached --quiet && echo "no tracked changes"
git status --short
git log --oneline --merges | grep -oE "feature/[a-z-]+" | sort -u
git grep -l 'coverlet.msbuild' -- tests
git branch --list feature/readme 'release/*'
git tag --list 'v*'
git ls-remote --heads --tags origin | grep -E 'feature/readme|release/|refs/tags/v' || echo "nothing of ticket 13 on origin"
```

Expected:
- the pull fast-forwards or prints `Already up to date.`
- `no tracked changes`
- untracked `?? docs/superpowers/plans/2026-09-26-13-readme-and-release-1-0-0.md` and `?? docs/superpowers/tickets/`, possibly with other untracked plans
- twelve branches: `feature/auth-login`, `feature/cancel-sale`, `feature/cancel-sale-item`, `feature/create-sale`, `feature/delete-sale`, `feature/error-format`, `feature/list-sales`, `feature/list-sales-filters`, `feature/postgres-runtime`, `feature/repo-restructure`, `feature/sale-number`, `feature/update-sale`
- only `tests/Ambev.DeveloperEvaluation.Unit/Ambev.DeveloperEvaluation.Unit.csproj`
- no branch, no tag, and `nothing of ticket 13 on origin`

If the pull fails, anything tracked is modified, a branch is missing, or `feature/readme` exists, stop and ask the user. In that last case, look for an ai-memory handoff first.

- [ ] **Step 2: Check the tools and Docker**

```bash
dotnet tool restore 2>&1 | tail -1
command -v slopwatch gh python3
docker version --format 'Docker server {{.Server.Version}}' 2>&1 | tail -1
docker info --format '{{.OSType}}' 2>&1 | tail -1
```

Expected: `Restore was successful.`, three paths, `Docker server …` and `linux`. If Docker fails, stop, save a handoff (`ai-memory-handoff`) and ask the user to start Docker Desktop.

- [ ] **Step 3: Commit this plan on `develop`, push, branch**

```bash
git add docs/superpowers/plans/2026-09-26-13-readme-and-release-1-0-0.md
git commit -m "docs: add plan for the README and release 1.0.0"
git push origin develop
git switch -c feature/readme
```

Expected: one file committed and pushed, then `Switched to a new branch 'feature/readme'`. If the push is rejected, stop and ask. Never force it.

- [ ] **Step 4: Record the baseline**

```bash
mkdir -p /tmp/ticket13-checks
dotnet build Ambev.DeveloperEvaluation.sln --no-incremental 2>&1 | grep -E 'warning CS|Warning\(s\)|Error\(s\)|Build succeeded' | sort -u
dotnet test Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-110 | tee /tmp/ticket13-checks/baseline.txt
slopwatch analyze --fail-on warning 2>&1 | tail -1
dotnet ef migrations has-pending-model-changes --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi 2>&1 | grep -vE 'NU1903|\[FTL\]|HostAbortedException|^\s+at ' | tail -1
```

Expected:
- `Build succeeded.`, `2 Warning(s)`, `0 Error(s)` and no `warning CS`
- three `Passed!` lines (Unit, Integration, Functional). Record the counts as U0, I0 and F0.
- `Scan complete: 0 issue(s) found`
- `No changes have been made to the model since the last migration.`

If anything fails, stop.

---

### Task 2: Collect coverage from the integration and functional tests

**Skills:** `dotnet-slopwatch`, `testcontainers-integration-tests`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Integration/Ambev.DeveloperEvaluation.Integration.csproj`
- Modify: `tests/Ambev.DeveloperEvaluation.Functional/Ambev.DeveloperEvaluation.Functional.csproj`

- [ ] **Step 1: Watch the coverage properties do nothing (RED)**

One Bash call, with a 600000 ms timeout:

```bash
rm -rf tests/Ambev.DeveloperEvaluation.Integration/TestResults tests/Ambev.DeveloperEvaluation.Functional/TestResults
COV='/p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./TestResults/coverage.cobertura.xml'
dotnet test tests/Ambev.DeveloperEvaluation.Integration --filter "FullyQualifiedName~MigrationTests" $COV 2>&1 | grep -E 'Passed!|Failed!|Calculating coverage' | cut -c1-110
dotnet test tests/Ambev.DeveloperEvaluation.Functional --filter "FullyQualifiedName~HealthCheckTests" $COV 2>&1 | grep -E 'Passed!|Failed!|Calculating coverage' | cut -c1-110
ls tests/Ambev.DeveloperEvaluation.{Integration,Functional}/TestResults/coverage.cobertura.xml 2>&1
```

Expected: two `Passed!` lines, no `Calculating coverage result...`, and two `ls: cannot access …: No such file or directory` lines. Without `coverlet.msbuild`, MSBuild ignores the properties.

- [ ] **Step 2: Add `coverlet.msbuild` to both projects**

With the Edit tool, in each of the two `.csproj` files, replace

```xml
    <PackageReference Include="coverlet.collector" Version="6.0.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
```

with

```xml
    <PackageReference Include="coverlet.collector" Version="6.0.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.msbuild" Version="6.0.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
```

That's the Unit project's block. If the anchor isn't found, Read the file and insert the new block right after the `coverlet.collector` reference.

- [ ] **Step 3: Run the same commands (GREEN)**

Repeat Step 1 exactly.

Expected: each project prints `Calculating coverage result...` and `Passed!`, and `ls` lists both `coverage.cobertura.xml` files.

- [ ] **Step 4: Check the diff and slopwatch**

```bash
git diff --stat | cat
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `2 files changed, 8 insertions(+)` and `Scan complete: 0 issue(s) found`.

- [ ] **Step 5: Commit**

```bash
git add tests/Ambev.DeveloperEvaluation.Integration/Ambev.DeveloperEvaluation.Integration.csproj tests/Ambev.DeveloperEvaluation.Functional/Ambev.DeveloperEvaluation.Functional.csproj
git commit -m "build(tests): collect coverage from the integration and functional tests" -m "Only the Unit project referenced coverlet.msbuild, so the coverage scripts' /p:CollectCoverage=true did nothing for the other two suites and the report covered unit tests only."
```

---

### Task 3: Fix `coverage-report.sh`

**Files:**
- Create: `.gitattributes`
- Modify: `coverage-report.sh`

- [ ] **Step 1: See the three failures (RED)**

```bash
file coverage-report.sh
dotnet restore 2>&1 | grep -o 'MSB1011.*'
rm -rf tests/Ambev.DeveloperEvaluation.Unit/TestResults
dotnet test tests/Ambev.DeveloperEvaluation.Unit /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./TestResults/coverage.cobertura.xml /p:Exclude="[*]*.Program,[*]*.Startup,[*]*.Migrations.*" 2>&1 | grep -E 'MSB1006|Passed!|Failed!' | cut -c1-110
grep -cE 'class name="Ambev\.DeveloperEvaluation\.(WebApi\.Program|ORM\.Migrations\.)' tests/Ambev.DeveloperEvaluation.Unit/TestResults/coverage.cobertura.xml
```

Expected:
- `coverage-report.sh: Bourne-Again shell script, ASCII text executable, with CRLF line terminators`
- `MSB1011: Specify which project or solution file to use because this folder contains more than one project or solution file.`
- `MSBUILD : error MSB1006: Property is not valid.` (derived), then `grep: …coverage.cobertura.xml: No such file or directory`. If the run passes instead, the count shows whether the filter worked: above 0 means `Program` or the migrations were covered. Record what you saw. Step 3 switches to `%2c` either way (Decision 5).

- [ ] **Step 2: Keep shell scripts at LF**

With the Write tool, create `.gitattributes`:

```gitattributes
# Shell scripts keep LF endings on every checkout: with core.autocrlf=true, Windows
# would write CRLF, and bash can't run a script with carriage returns.
*.sh text eol=lf
```

Then write the working copy again from the index, which already holds LF:

```bash
rm coverage-report.sh && git checkout -- coverage-report.sh
file coverage-report.sh
git status --short
```

Expected: `coverage-report.sh: Bourne-Again shell script, ASCII text executable` with no CRLF. `git status --short` shows no modified file, only `?? .gitattributes` and the untracked tickets.

- [ ] **Step 3: Restore the solution and escape the filter**

With the Edit tool, in `coverage-report.sh`, replace

```bash
dotnet restore
dotnet build  Ambev.DeveloperEvaluation.sln --configuration Release --no-restore
```

with

```bash
dotnet restore Ambev.DeveloperEvaluation.sln
dotnet build  Ambev.DeveloperEvaluation.sln --configuration Release --no-restore
```

and replace

```bash
/p:Exclude="[*]*.Program,[*]*.Startup,[*]*.Migrations.*"
```

with

```bash
/p:Exclude="[*]*.Program%2c[*]*.Startup%2c[*]*.Migrations.*"
```

- [ ] **Step 4: Check (GREEN)**

```bash
bash -n coverage-report.sh && echo "syntax ok"
dotnet restore Ambev.DeveloperEvaluation.sln 2>&1 | grep -cE 'MSB1011|error'
rm -rf tests/Ambev.DeveloperEvaluation.Unit/TestResults
dotnet test tests/Ambev.DeveloperEvaluation.Unit /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./TestResults/coverage.cobertura.xml /p:Exclude="[*]*.Program%2c[*]*.Startup%2c[*]*.Migrations.*" 2>&1 | grep -E 'MSB1006|Passed!|Failed!' | cut -c1-110
grep -cE 'class name="Ambev\.DeveloperEvaluation\.(WebApi\.Program|ORM\.Migrations\.)' tests/Ambev.DeveloperEvaluation.Unit/TestResults/coverage.cobertura.xml
git diff --stat | cat
```

Expected: `syntax ok`, `0`, `Passed!` with U0 tests, `0`, and `coverage-report.sh | 4 ++--` (`1 file changed, 2 insertions(+), 2 deletions(-)`).

- [ ] **Step 5: Commit**

```bash
git add .gitattributes coverage-report.sh
git commit -m "build: fix coverage-report.sh restore, filter and line endings" -m "A bare dotnet restore stops with MSB1011 because docker-compose.dcproj sits next to the solution, so the script names the solution. The Exclude filter separates its patterns with %2c, as coverage-report.bat does, so MSBuild can't split the property on the commas. .gitattributes keeps *.sh at LF: with core.autocrlf=true, a Windows checkout wrote CRLF and bash couldn't run the script."
```

---

### Task 4: Produce the coverage report with both scripts

**Skill:** `testcontainers-integration-tests` (both runs start the test containers).

**Files:** none (evidence only).

- [ ] **Step 1: Write the report check**

```bash
cat > /tmp/ticket13-checks/check_coverage.sh <<'EOF'
#!/usr/bin/env bash
# Ticket 13: one coverage run produced a report fed by all three suites. Usage: check_coverage.sh <log>
grep -E 'Passed!|Failed!' "$1" | cut -c1-110
grep -F 'Coverage report generated at TestResults/CoverageReport/index.html' "$1"
find tests -path '*/TestResults/coverage.cobertura.xml' -newer /tmp/ticket13-checks/coverage-start | sort
test -s TestResults/CoverageReport/index.html && echo "report: TestResults/CoverageReport/index.html"
python3 - <<'PY'
import xml.etree.ElementTree as ET
checks = [("Unit", "Ambev.DeveloperEvaluation.Domain.Entities.Sale"),
          ("Integration", "Ambev.DeveloperEvaluation.ORM.Repositories.SaleRepository"),
          ("Functional", "Ambev.DeveloperEvaluation.WebApi.Features.Sales.SalesController")]
for suite, name in checks:
    root = ET.parse(f"tests/Ambev.DeveloperEvaluation.{suite}/TestResults/coverage.cobertura.xml").getroot()
    rates = [float(c.get("line-rate")) for c in root.iter("class") if c.get("name", "").startswith(name)]
    print(suite, name.rsplit(".", 1)[1], "covered" if rates and max(rates) > 0 else "NOT covered")
PY
EOF
```

`SaleRepository` is only reached by the integration suite and `SalesController` by the functional suite, so a `covered` for each proves that suite feeds the report.

- [ ] **Step 2: Run `coverage-report.sh`**

Run it with the Bash tool's `run_in_background`, then wait for the completion notice:

```bash
rm -rf TestResults tests/*/TestResults
touch /tmp/ticket13-checks/coverage-start
bash coverage-report.sh > /tmp/ticket13-checks/coverage-sh.log 2>&1; echo "exit=$?" >> /tmp/ticket13-checks/coverage-sh.log
```

Then:

```bash
bash /tmp/ticket13-checks/check_coverage.sh /tmp/ticket13-checks/coverage-sh.log
tail -n 2 /tmp/ticket13-checks/coverage-sh.log
```

Expected:

```
Passed!  - Failed:     0, Passed:    U0, … Ambev.DeveloperEvaluation.Unit.dll (net8.0)
Passed!  - Failed:     0, Passed:    I0, … Ambev.DeveloperEvaluation.Integration.dll (net8.0)
Passed!  - Failed:     0, Passed:    F0, … Ambev.DeveloperEvaluation.Functional.dll (net8.0)
Coverage report generated at TestResults/CoverageReport/index.html
tests/Ambev.DeveloperEvaluation.Functional/TestResults/coverage.cobertura.xml
tests/Ambev.DeveloperEvaluation.Integration/TestResults/coverage.cobertura.xml
tests/Ambev.DeveloperEvaluation.Unit/TestResults/coverage.cobertura.xml
report: TestResults/CoverageReport/index.html
Unit Sale covered
Integration SaleRepository covered
Functional SalesController covered
coverage-report.sh: line 29: pause: command not found
exit=127
```

The `Passed!` lines may come in any order. The `pause` line and `exit=127` are the template's (spec §9.2).

- [ ] **Step 3: Run `coverage-report.bat`**

Again in the background:

```bash
rm -rf TestResults tests/*/TestResults
touch /tmp/ticket13-checks/coverage-start
cmd //c "coverage-report.bat < NUL" > /tmp/ticket13-checks/coverage-bat.log 2>&1; echo "exit=$?" >> /tmp/ticket13-checks/coverage-bat.log
```

`< NUL` answers the final `pause`. Then:

```bash
bash /tmp/ticket13-checks/check_coverage.sh /tmp/ticket13-checks/coverage-bat.log
```

Expected: the same eleven lines as Step 2, without the `pause` line.

- [ ] **Step 4: Clean up**

```bash
docker ps --filter ancestor=postgres:13 --format '{{.ID}}' | wc -l
git status --short
```

Expected: `0` (Testcontainers removed its containers) and no tracked changes: `TestResults/` is git-ignored.

---

### Task 5: Put the `.http` file in the ticket's order

**Files:**
- Rewrite: `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http`

- [ ] **Step 1: Write the expected titles and check the current order (RED)**

```bash
cat > /tmp/ticket13-checks/expected-titles.txt <<'EOF'
Sign up an active admin (201 the first time, 409 afterwards)
Log in: the JWT comes back at data.token
Create a sale: 5 identical items get 10%, which the API computes (201 with the sale)
Create a sale without a sale number: the server issues the next S- number (201 with the sale; 12 items get 20%)
Get the sale back (200 with the same sale)
List sales: page 1 of 5, newest first, then the highest total (200 with currentPage, totalPages and totalItems)
List sales by a field that can't be sorted (400 ValidationError)
List sales filtered: customer names ending in "silva", open, sold from 11:30 at -03:00 through the end of 2026-09-24, totals of 20.00 or more (200; the sale created above is in data, and totalItems counts only matches)
List sales with a minimum total above the maximum (400 ValidationError)
Update the sale: a new branch, the first product at 10 items (20%) and a second product (200; totalAmount 52.00)
Update it without the first product: its line is cancelled, not deleted (200; totalAmount 16.00)
Create a sale with two items, to cancel them one by one (201; 5 items get 10%, 2 items get none)
Cancel the first item: it leaves the total and the sale stays open (200; totalAmount 16.00)
Cancel it again: the item is already cancelled (409 BusinessRuleViolation)
Cancel the last active item: the sale is cancelled too (200; isCancelled true, totalAmount 0)
Cancel the sale: its items and total stay as they were (200 with isCancelled true)
Cancel it again: a cancelled sale is read-only (409 BusinessRuleViolation)
Update the cancelled sale: a cancelled sale is read-only (409 BusinessRuleViolation)
Delete the sale created above. The requests above cancelled it, and a cancelled sale can be deleted too (200 with only success and message)
Get the deleted sale (404 ResourceNotFound)
Delete it again (404 ResourceNotFound)
EOF
F=src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
grep -E '^### .' $F | tr -d '\r' | cut -c5- > /tmp/ticket13-checks/actual-titles.txt
diff /tmp/ticket13-checks/expected-titles.txt /tmp/ticket13-checks/actual-titles.txt && echo "titles in ticket order"
sort /tmp/ticket13-checks/expected-titles.txt | diff - <(sort /tmp/ticket13-checks/actual-titles.txt) && echo "same 21 titles"
```

Expected: the first `diff` prints moved lines (at least "Cancel the sale…" through "Update the cancelled sale…" before the cancel-item block), and the second prints `same 21 titles`. If the second `diff` prints anything, the file on `develop` differs from the plans for tickets 04–12: Read it and carry those differences into Step 3's content, keeping the order.

- [ ] **Step 2: Read the file**

Read `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http` with the Read tool. Write needs that before it can replace the file.

- [ ] **Step 3: Replace the whole file**

With the Write tool, replace the file with:

```http
# Ambev DeveloperStore API: the whole Sales flow, top to bottom.
# Send the requests in order: later requests use the token and the ids that earlier responses return.
#
# baseUrl is the API started by `docker compose up`. `dotnet run` (the http launch profile)
# serves it on http://localhost:5119 instead: change baseUrl to that.
#
# Safe to re-run: once the user exists, the sign-up returns 409 BusinessRuleViolation
# and the login still works. Sent sale numbers are random, and every id comes from a response.

@baseUrl = http://localhost:8080
@email = admin@developerstore.com
@password = Admin@123

### Sign up an active admin (201 the first time, 409 afterwards)
POST {{baseUrl}}/api/users
Content-Type: application/json

{
  "username": "admin",
  "password": "{{password}}",
  "phone": "+5511999999999",
  "email": "{{email}}",
  "status": "Active",
  "role": "Admin"
}

### Log in: the JWT comes back at data.token
# @name login
POST {{baseUrl}}/api/auth
Content-Type: application/json

{
  "email": "{{email}}",
  "password": "{{password}}"
}

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

### List sales filtered: customer names ending in "silva", open, sold from 11:30 at -03:00 through the end of 2026-09-24, totals of 20.00 or more (200; the sale created above is in data, and totalItems counts only matches)
# _maxSaleDate has no time, so it covers the whole day. Only a leading or trailing * is a wildcard.
GET {{baseUrl}}/api/sales?customerName=*silva&isCancelled=false&_minSaleDate=2026-09-24T11:30:00-03:00&_maxSaleDate=2026-09-24&_minTotalAmount=20&_order=totalAmount%20desc
Authorization: Bearer {{token}}

### List sales with a minimum total above the maximum (400 ValidationError)
GET {{baseUrl}}/api/sales?_minTotalAmount=50&_maxTotalAmount=10
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

### Cancel the sale: its items and total stay as they were (200 with isCancelled true)
PATCH {{baseUrl}}/api/sales/{{saleId}}/cancel
Authorization: Bearer {{token}}

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

- [ ] **Step 4: Check the order and that nothing was lost (GREEN)**

```bash
F=src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
grep -E '^### .' $F | tr -d '\r' | cut -c5- | diff /tmp/ticket13-checks/expected-titles.txt - && echo "titles in ticket order"
norm() { sed '1s/^\xEF\xBB\xBF//' | tr -d '\r' | grep -vE '^\s*$|^#$|^# [^@]' | sort; }
diff <(git show HEAD:$F | norm) <(norm < $F) && echo "same requests, bodies, variables and titles"
```

Expected: `titles in ticket order` and `same requests, bodies, variables and titles`. The second check ignores blank lines and plain comments, and keeps `###` titles and `# @name` lines. If it prints lines, Step 3 dropped or changed something: fix Step 3's content, not the check.

- [ ] **Step 5: Write the runner**

```bash
cat > /tmp/ticket13-checks/run_http.py <<'EOF'
#!/usr/bin/env python3
"""Ticket 13: send every request of a .http file from top to bottom, as an editor's runner does.

Supports what the file uses: file variables (@name = value, resolved when used),
request names (# @name x), {{$randomInt min max}} and {{x.response.body.$.a.b[0].c}}.
Prints one line per request and saves each response body to <out>/NN.json.
Usage: run_http.py <file.http> <out-dir>
"""
import json
import random
import re
import sys
import urllib.error
import urllib.request
from decimal import Decimal
from pathlib import Path
from urllib.parse import urlsplit

UUID = re.compile(r"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}")
REQUEST_LINE = re.compile(r"(GET|POST|PUT|PATCH|DELETE)\s+(\S+)")

http_file, out_dir = Path(sys.argv[1]), Path(sys.argv[2])
out_dir.mkdir(parents=True, exist_ok=True)

blocks, block = [], []
for line in http_file.read_text(encoding="utf-8-sig").splitlines():
    if line.startswith("###"):
        blocks.append(block)
        block = []
    else:
        block.append(line)
blocks.append(block)

variables, requests = {}, []
for block in blocks:
    name, request, state = None, None, "before"
    for line in block:
        if state == "before":
            if (m := re.match(r"#\s*@name\s+(\S+)", line)):
                name = m.group(1)
            elif (m := re.match(r"@([\w.-]+)\s*=\s*(.*)", line)):
                variables[m.group(1)] = m.group(2).strip()
            elif (m := REQUEST_LINE.match(line)):
                request = {"name": name, "method": m.group(1), "url": m.group(2), "headers": {}, "body": []}
                state = "headers"
        elif state == "headers":
            if line.strip():
                key, _, value = line.partition(":")
                request["headers"][key.strip()] = value.strip()
            else:
                state = "body"
        else:
            request["body"].append(line)
    if request:
        requests.append(request)

responses = {}


def resolve(text):
    def value(match):
        expr = match.group(1).strip()
        if expr.startswith("$randomInt"):
            low, high = map(int, expr.split()[1:3])
            return str(random.randrange(low, high))
        if (m := re.fullmatch(r"([\w-]+)\.response\.body\.\$(.*)", expr)):
            node = responses[m.group(1)]
            for key, index in re.findall(r"\.(\w+)|\[(\d+)\]", m.group(2)):
                node = node[key] if key else node[int(index)]
            return str(node)
        return resolve(variables[expr])
    return re.sub(r"\{\{(.+?)\}\}", value, text)


def summary(body):
    if not isinstance(body, dict):
        return ""
    if "type" in body:
        return body["type"]
    data = body.get("data")
    if isinstance(data, dict) and "totalAmount" in data:
        total = Decimal(str(data["totalAmount"]))
        return f'{data["saleNumber"]} total={total:.2f} cancelled={str(data["isCancelled"]).lower()}'
    if "totalItems" in body:
        return f'totalItems={body["totalItems"]} page={body["currentPage"]}/{body["totalPages"]}'
    return body.get("message", "") if data is None else ""


for number, request in enumerate(requests, 1):
    url = resolve(request["url"])
    body = "\n".join(request["body"]).strip()
    outgoing = urllib.request.Request(
        url,
        data=resolve(body).encode("utf-8") if body else None,
        method=request["method"],
        headers={key: resolve(value) for key, value in request["headers"].items()},
    )
    try:
        with urllib.request.urlopen(outgoing, timeout=30) as response:
            status, raw = response.status, response.read()
    except urllib.error.HTTPError as error:
        status, raw = error.code, error.read()
    try:
        parsed = json.loads(raw, parse_float=Decimal) if raw.strip() else None
    except ValueError:
        parsed = None
    if raw:
        (out_dir / f"{number:02d}.json").write_bytes(raw)
    if request["name"]:
        responses[request["name"]] = parsed
    parts = urlsplit(url)
    shown = UUID.sub("{id}", parts.path) + ("?" if parts.query else "")
    print(f"{number:02d} {status} {request['method']} {shown} {summary(parsed)}".rstrip())
EOF
```

- [ ] **Step 6: Start the stack and run the file twice**

```bash
docker compose down -v --remove-orphans
docker compose up --build --wait --wait-timeout 300; echo "exit=$?"
```

Expected: `exit=0`. If a port is already allocated, stop and ask the user.

Then, in one Bash call:

```bash
C=/tmp/ticket13-checks
F=src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
rm -rf $C/run1 $C/run2
echo "== run 1"; python3 $C/run_http.py $F $C/run1
echo "== run 2"; python3 $C/run_http.py $F $C/run2
curl -s -o $C/run1/90-no-token.json -w "no token %{http_code}\n" http://localhost:8080/api/sales
curl -s -o $C/run1/91-wrong-password.json -w "wrong password %{http_code}\n" -X POST http://localhost:8080/api/auth -H 'Content-Type: application/json' --data-raw '{"email":"admin@developerstore.com","password":"Wrong@123"}'
```

Expected, with `S-<A>` and `S-<B>` random six-digit numbers that repeat where shown:

```
== run 1
01 201 POST /api/users
02 200 POST /api/auth
03 201 POST /api/sales S-<A> total=20.25 cancelled=false
04 201 POST /api/sales S-000001 total=43.20 cancelled=false
05 200 GET /api/sales/{id} S-<A> total=20.25 cancelled=false
06 200 GET /api/sales? totalItems=2 page=1/1
07 400 GET /api/sales? ValidationError
08 200 GET /api/sales? totalItems=2 page=1/1
09 400 GET /api/sales? ValidationError
10 200 PUT /api/sales/{id} S-<A> total=52.00 cancelled=false
11 200 PUT /api/sales/{id} S-<A> total=16.00 cancelled=false
12 201 POST /api/sales S-<B> total=36.25 cancelled=false
13 200 PATCH /api/sales/{id}/items/{id}/cancel S-<B> total=16.00 cancelled=false
14 409 PATCH /api/sales/{id}/items/{id}/cancel BusinessRuleViolation
15 200 PATCH /api/sales/{id}/items/{id}/cancel S-<B> total=0.00 cancelled=true
16 200 PATCH /api/sales/{id}/cancel S-<A> total=16.00 cancelled=true
17 409 PATCH /api/sales/{id}/cancel BusinessRuleViolation
18 409 PUT /api/sales/{id} BusinessRuleViolation
19 200 DELETE /api/sales/{id} Sale deleted successfully
20 404 GET /api/sales/{id} ResourceNotFound
21 404 DELETE /api/sales/{id} ResourceNotFound
== run 2
```

Run 2 prints the same lines with new random numbers, except:
- `01 409 POST /api/users BusinessRuleViolation`
- `04 201 POST /api/sales S-000002 total=43.20 cancelled=false`
- `06 200 GET /api/sales? totalItems=4 page=1/1` (run 1's `S-000001` and its cancelled two-item sale, plus run 2's two sales; run 1's deleted sale is hidden)
- `08 200 GET /api/sales? totalItems=3 page=1/1` (the cancelled two-item sale is filtered out)

Then `no token 401` and `wrong password 401`. The response bodies stay in `/tmp/ticket13-checks/run1` and `run2` for Task 6.

- [ ] **Step 7: Check the event log**

```bash
docker compose logs ambev.developerevaluation.webapi 2>&1 | grep -oE 'Sale event [\\"]*[A-Za-z]+[\\"]* published' | tr -d '"\\' | sort | uniq -c
```

Expected, for two runs:

```
      6 Sale event ItemCancelledEvent published
      4 Sale event SaleCancelledEvent published
      6 Sale event SaleCreatedEvent published
      4 Sale event SaleModifiedEvent published
```

Per run: 3 creates; 2 updates; 3 item cancellations (the second update's dropped line and two explicit ones); 2 sale cancellations (the last item's and the explicit one). The 409s and the delete publish nothing.

- [ ] **Step 8: Take the stack down**

```bash
docker compose down -v
docker compose ps -a
```

Expected: `ps -a` lists nothing.

- [ ] **Step 9: Commit**

```bash
git add src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
git commit -m "docs(http): run the whole sales flow top to bottom" -m "The requests now follow the flow the README describes: sign up, log in, create with and without a number, get, list (paged, ordered, filtered), update, cancel an item, cancel the sale, delete. Only the order and the header comment change. Re-running is safe: the sign-up then answers 409, sent sale numbers are random, and ids come from responses."
```

---

### Task 6: Write the project README

**Skills:** `efcore-patterns`, `testcontainers-integration-tests`, `dotnet-best-practices`, `dependency-injection-patterns` (all as references for the facts below).

**Files:**
- Rewrite: `README.md`

- [ ] **Step 1: Write the three README checks**

The content check lists what the ticket requires the README to cover:

```bash
cat > /tmp/ticket13-checks/check_readme_content.sh <<'EOF'
#!/usr/bin/env bash
# Ticket 13: README.md must mention everything the ticket lists. Run from the repo root.
missing=0
while IFS= read -r needle; do
  grep -qF -- "$needle" README.md || { echo "missing: $needle"; missing=$((missing + 1)); }
done <<'NEEDLES'
docker compose up
dotnet run --project src/Ambev.DeveloperEvaluation.WebApi
POST /api/users
POST /api/auth
**Authorize**
/api/sales/{id}/items/{itemId}/cancel
Why 4 items get 10%
dotnet test Ambev.DeveloperEvaluation.sln
MSB1011
docker-compose.dcproj
coverage-report.sh
coverage-report.bat
dotnet ef migrations add <Name> --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi
## Decisions
## Template fixes
## Template issues left as-is
## Known limitations
Missing `Users` timestamp columns
Silent startup errors
Responses wrapped twice
Duplicate sign-up
Empty sign-up response
GHSA-rvv3-g6hj-g44x
405 (wrong method)
415 (missing or wrong `Content-Type`)
(.doc/challenge.md)
NEEDLES
echo "missing: $missing"
[ "$missing" -eq 0 ]
EOF
```

The link check covers `README.md` and the tracked `.doc` pages:

```bash
cat > /tmp/ticket13-checks/check_links.py <<'EOF'
#!/usr/bin/env python3
"""Ticket 13 link check, run from the repo root.

Every relative link in README.md and the tracked .doc pages must point at an existing
file. In README.md, #anchors must match a heading (GitHub's slugs), and external links
must answer below 400.
"""
import re
import subprocess
import sys
import urllib.request
from pathlib import Path

LINK = re.compile(r'\]\(([^)\s]+)\)|href="([^"]+)"')


def lines_outside_code(path):
    fenced = False
    for line in Path(path).read_text(encoding="utf-8-sig").splitlines():
        if line.lstrip().startswith("```"):
            fenced = not fenced
        elif not fenced:
            yield line


def anchors(path):
    found, seen = set(), {}
    for line in lines_outside_code(path):
        if (m := re.match(r"#{1,6}\s+(.+)", line)):
            slug = re.sub(r"[^\w\- ]", "", m.group(1).strip().lower()).replace(" ", "-")
            count = seen.get(slug, 0)
            seen[slug] = count + 1
            found.add(slug if count == 0 else f"{slug}-{count}")
    return found


def external_ok(url):
    request = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            return response.status < 400
    except Exception:
        return False


pages = subprocess.run(["git", "ls-files", "README.md", ".doc/*.md"],
                       capture_output=True, text=True, check=True).stdout.split()
broken = 0
for page in pages:
    is_readme = page == "README.md"
    for line in lines_outside_code(page):
        for markdown_target, html_target in LINK.findall(line):
            target = markdown_target or html_target
            if target.startswith("mailto:"):
                continue
            if target.startswith(("http://", "https://")):
                if not is_readme:
                    continue
                ok = external_ok(target)
            else:
                path, _, fragment = target.partition("#")
                if not path and not is_readme:
                    continue
                resolved = Path(path.lstrip("/")) if path.startswith("/") else Path(page).parent / path if path else Path(page)
                ok = resolved.exists()
                if ok and fragment and is_readme and resolved.suffix == ".md":
                    ok = fragment in anchors(resolved)
            print(("ok      " if ok else "BROKEN  ") + f"{page} -> {target}")
            broken += 0 if ok else 1
print(f"broken links: {broken}")
sys.exit(1 if broken else 0)
EOF
```

The examples check compares the README's `json` blocks with Task 5's live responses (Decision 4):

```bash
cat > /tmp/ticket13-checks/check_readme_examples.py <<'EOF'
#!/usr/bin/env python3
"""Ticket 13: check README.md's JSON examples against live API responses.

Every ```json block must parse. Every "message" and every (type, error) pair in the
examples must appear in a saved live response. A success example must have the same
top-level fields as the live body with that message, and every sale example must have
the same sale and item fields as a live sale.
Usage: check_readme_examples.py <dir-with-live-json> [<dir> ...]
"""
import json
import re
import sys
from pathlib import Path


def sale_shape(body):
    data = body.get("data")
    if isinstance(data, list):
        data = data[0] if data else None
    if isinstance(data, dict) and data.get("items"):
        return tuple(sorted(data)), tuple(sorted(data["items"][0]))
    return None


live = []
for folder in sys.argv[1:]:
    for path in sorted(Path(folder).glob("*.json")):
        body = json.loads(path.read_text(encoding="utf-8"))
        if isinstance(body, dict):
            live.append(body)

readme = Path("README.md").read_text(encoding="utf-8").replace("\r\n", "\n")
blocks = re.findall(r"```json\n(.*?)```", readme, re.S)
examples, problems = [], 0
for number, block in enumerate(blocks, 1):
    try:
        examples.append(json.loads(block))
    except ValueError as error:
        print(f"json block {number} doesn't parse: {error}")
        problems += 1

fields_by_message = {}
for body in live:
    if "message" in body:
        fields_by_message.setdefault(body["message"], set(body))
errors = {(body["type"], body["error"]) for body in live if "type" in body}
shapes = {shape for shape in map(sale_shape, live) if shape}

for example in examples:
    if not isinstance(example, dict):
        continue
    if "message" in example:
        if example["message"] not in fields_by_message:
            print(f'message never seen live: {example["message"]}')
            problems += 1
        elif set(example) != fields_by_message[example["message"]]:
            print(f'fields differ from the live body: {example["message"]}')
            problems += 1
    if "type" in example and (example["type"], example.get("error")) not in errors:
        print(f'error never seen live: {example["type"]} / {example.get("error")}')
        problems += 1
    shape = sale_shape(example)
    if shape and shape not in shapes:
        print(f'sale fields differ from every live sale: {example.get("message")}')
        problems += 1
print(f"json blocks: {len(blocks)}, problems: {problems}")
sys.exit(1 if problems else 0)
EOF
```

- [ ] **Step 2: Run the content check on the placeholder (RED)**

```bash
bash /tmp/ticket13-checks/check_readme_content.sh | tail -1; echo "exit=${PIPESTATUS[0]}"
```

Expected: `missing: 25` and `exit=1`. Only `(.doc/challenge.md)` is in ticket 01's placeholder.

- [ ] **Step 3: Read `README.md`, then replace it**

Read `README.md` with the Read tool. Then, with the Write tool, replace it with exactly:

````markdown
# DeveloperStore Sales API

A prototype Sales API for the Ambev DeveloperStore developer evaluation, built on the .NET 8 template the challenge provides. It records sales with complete CRUD, applies the quantity-based discount rules inside a DDD aggregate, requires a JWT on every sales endpoint, and writes the `SaleCreated`, `SaleModified`, `SaleCancelled` and `ItemCancelled` events to the application log.

- The challenge statement: [.doc/challenge.md](.doc/challenge.md)
- The API conventions it follows (paging, ordering, filtering, errors): [.doc/general-api.md](.doc/general-api.md)
- The full design spec: [docs/superpowers/specs/2026-09-24-sales-api-design.md](docs/superpowers/specs/2026-09-24-sales-api-design.md)

## Contents

- [Run it](#run-it)
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
| PostgreSQL 13 | `localhost:5432`, database `developer_evaluation`, user `developer`, password `ev@luAt10n` |

The API runs in the Development environment, so it applies pending EF Core migrations at startup. `docker compose down -v` stops both containers and deletes the data.

### The API with `dotnet run`

Start only the database, then run the API:

```bash
docker compose up -d --wait ambev.developerevaluation.database
dotnet run --project src/Ambev.DeveloperEvaluation.WebApi
```

The API listens on `http://localhost:5119` (Swagger at `/swagger`) in Development. It uses the connection string in `src/Ambev.DeveloperEvaluation.WebApi/appsettings.json`, which points at `localhost:5432`.

### The whole flow in one file

[Ambev.DeveloperEvaluation.WebApi.http](src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http) runs in Visual Studio 2022, or in VS Code with the REST Client extension. Send its requests from top to bottom against `docker compose up`: sign up, log in, create a sale with and without a sale number, get it, list sales (paged, ordered, filtered), update, cancel an item, cancel a sale, delete. Each request's title gives the expected status. The file can be re-run: the sign-up then returns 409, sent sale numbers are random, and every id comes from an earlier response. For `dotnet run`, set `@baseUrl` to `http://localhost:5119`.

## Get a token

Every `/api/sales` route needs `Authorization: Bearer <token>`. The Users and Auth routes are anonymous. A token is valid for 8 hours.

1. Create an **Active** user with `POST /api/users`.
2. Log in with `POST /api/auth`. The token is at `data.token`.
3. Send it as `Authorization: Bearer <token>`. In Swagger, click **Authorize** and paste the token without the `Bearer` prefix.

```bash
curl -X POST http://localhost:8080/api/users -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin@123","phone":"+5511999999999","email":"admin@developerstore.com","status":"Active","role":"Admin"}'

curl -X POST http://localhost:8080/api/auth -H "Content-Type: application/json" \
  -d '{"email":"admin@developerstore.com","password":"Admin@123"}'
```

The sign-up answers 201. The same email again answers 409.

```json
{
  "success": true,
  "message": "User created successfully",
  "data": {
    "id": "0b7f7a8e-2f4e-4c1a-9d53-8a3c2f1e6b10",
    "name": "admin",
    "email": "admin@developerstore.com",
    "phone": "+5511999999999",
    "role": "Admin",
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
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6ImFkbWluIn0.c2lnbmF0dXJl",
    "email": "admin@developerstore.com",
    "name": "admin",
    "role": "Admin"
  }
}
```

A password needs at least 8 characters, with an upper-case letter, a lower-case letter, a digit and a special character. `status` and `role` travel as strings, such as `"Active"` and `"Admin"`. A user who isn't active can't log in (401).

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

Stack: .NET 8, ASP.NET Core, EF Core 8 with Npgsql, PostgreSQL 13, MediatR 12, FluentValidation 11, AutoMapper 13 and Serilog. Tests: xUnit, NSubstitute, Bogus, FluentAssertions 6, Testcontainers and `Microsoft.AspNetCore.Mvc.Testing`.

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
| D16 | Git Flow and Conventional Commits | `feature/*` branches merged into `develop` with merge commits; `release/1.0.0` merged into `main` and tagged `v1.0.0` |

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

The tests follow the template's conventions: xUnit, NSubstitute, Bogus test-data builders, FluentAssertions, and `Given … When … Then …` names.

Coverage for all three suites, as an HTML report at `TestResults/CoverageReport/index.html`:

```bash
./coverage-report.sh      # Linux, macOS, Git Bash
coverage-report.bat       # Windows cmd
```

The scripts install the `coverlet.console` and `dotnet-reportgenerator-globaltool` global tools if they're missing, and they need Docker for the integration and functional suites. Both end with the template's `pause`. In bash that prints `pause: command not found`, which is harmless.

## Migrations

`dotnet-ef` 8.0.10 is pinned in `.config/dotnet-tools.json`. Add a migration from the repository root:

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi
```

The migrations live in the ORM project. The startup project must be WebApi, because the design-time factory (`DefaultContextFactory`) reads `appsettings.json` from the startup project's folder. The API applies pending migrations at startup in Development. `dotnet ef` also prints a `[FTL] … HostAbortedException` stack trace: that's how the EF tools stop `Program.Main`, and the command still succeeds.

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
- **Docker Compose:** HTTP only, without the certificate and user-secrets volumes; fixed host ports 8080 and 5432; the API gets the database connection string and waits for a `pg_isready` healthcheck; the unused MongoDB and Redis services are gone.
- **Migrations:** the design-time factory, renamed `DefaultContextFactory`, targets the ORM migrations assembly as `Program.cs` does, and `dotnet-ef` is pinned in a local tool manifest.
- **Startup migrations:** pending migrations apply in Development. Before, nothing created the schema.
- **Errors:** one global exception middleware, the model-binding 400 and the JWT 401 all return `{type, error, detail}`. The Users and Auth controllers no longer return raw FluentValidation lists.
- **Validation:** validators are registered in dependency injection, so the template's `ValidationBehavior`, which was registered but never ran, now validates every MediatR request.
- **Paging:** `PaginatedResponse.TotalCount` is renamed `TotalItems`, as `general-api.md` names it.
- **Small fixes:** `DomainException` gets the `Ambev.DeveloperEvaluation.Domain.Exceptions` namespace. A missing `Jwt:SecretKey` stops startup with an error that names the setting; it used to be a possible-null warning (CS8604).
- **`.http` file:** the weather-forecast request is replaced by the whole Sales flow.
- **Swagger and JSON:** Swagger has a Bearer scheme for its **Authorize** button, and enums travel as strings.

Found while reviewing the work, beyond the original list:

- **Missing `Users` timestamp columns:** `User` has `CreatedAt` and `UpdatedAt`, but the initial migration didn't create them, so every sign-up failed once migrations ran. A migration adds them.
- **Silent startup errors:** `Program.Main` logged startup exceptions before Serilog had a sink, and exited with code 0. It now rethrows, so a startup failure prints the error and exits non-zero.
- **Warnings and errors never logged:** the Serilog filter meant to hide successful `/health` requests dropped every warning, error and fatal event.
- **Responses wrapped twice:** `BaseController.Ok` wrapped an envelope inside another one, so the login token sat at `data.data.token` and paged bodies were nested. Each body is now built once.
- **Duplicate sign-up:** an email that already exists returned 500. It's now a 409 `BusinessRuleViolation`.
- **Empty sign-up response:** `POST /api/users` returned only the id, with an empty name and email. It now returns the saved user.
- **Coverage:** the integration and functional projects get `coverlet.msbuild`, so the coverage scripts cover all three suites. `coverage-report.sh` restores the solution by name (a bare `dotnet restore` stops with MSB1011), escapes the commas in its `Exclude` filter as the `.bat` does, and keeps LF line endings through `.gitattributes`: with Windows line endings, bash can't run it.

## Template issues left as-is

These don't affect Sales, the JWT flow or running the app:

- Phone and password rules disagree between the `User` entity, the create command and the login.
- `BaseController.GetCurrentUserId()` parses an `int`, although ids are `Guid`s.
- The template's routes differ from `.doc/users-api.md` and `.doc/auth-api.md`: `/api/users`, and `POST /api/auth` by email.
- Some services are registered twice, and the `CreateUserRequest` AutoMapper map is declared twice.
- One startup log line is written before Serilog is configured.
- The `ListUsers` and `UpdateUser` folders are empty.
- The Users endpoints, including `DELETE /api/users/{id}`, are anonymous. `IUserService` is a placeholder, the solution folder is spelled "Aplication", both coverage scripts end with `pause`, and there are two equivalent Dockerfiles.

## Known limitations

- **No outbox.** Events are published after the commit, so a crash between the commit and the publish loses them. Production would use a transactional outbox.
- **Events are only logged.** There is no message broker.
- **External identity names are snapshots.** They're copied when a sale is written and never refreshed from their source domains.
- **Sale numbers are compared exactly** (case-sensitive). If a client sends the number that the generator hands out at the same moment, the unique index rejects one of the two requests with 409. A retry gets the next number.
- **AutoMapper 13.0.1 has advisory [GHSA-rvv3-g6hj-g44x](https://github.com/advisories/GHSA-rvv3-g6hj-g44x):** a stack overflow (denial of service) when it maps deeply nested object graphs. The build shows it as warning NU1903. It can't be triggered here, because no mapped type is recursive and System.Text.Json limits request bodies to a depth of 64. The fixes exist only in the commercially licensed 15.1.1+ and 16.1.1+.
- **The template's Users endpoints stay anonymous,** including `DELETE /api/users/{id}`.
- **Sign-ups can race:** `Users.Email` has no unique index, so two simultaneous sign-ups with the same email can both succeed.
- **Framework responses:** ASP.NET Core's own 404 (unknown route), 405 (wrong method) and 415 (missing or wrong `Content-Type`) keep the framework's defaults instead of `{type, error, detail}`.
- **Enums also accept numbers** on input (`"status": 1`), besides their names.
- **A create response and a read format numbers differently.** A create response comes from memory (`"discountPercentage": 10`), while reads come from PostgreSQL's `numeric` columns (`10.00`). Likewise, `createdAt` has 100 ns precision after a create and microseconds after a read. The values are equal.
- **HTTP only.** The API logs `Failed to determine the https port for redirect` at startup: the template's `UseHttpsRedirection()` stays, with nothing to redirect to.
````

- [ ] **Step 4: Run the three checks (GREEN)**

```bash
bash /tmp/ticket13-checks/check_readme_content.sh | tail -1
python3 /tmp/ticket13-checks/check_links.py | grep -E '^BROKEN|broken links'
python3 /tmp/ticket13-checks/check_readme_examples.py /tmp/ticket13-checks/run1 /tmp/ticket13-checks/run2
```

Expected: `missing: 0`, `broken links: 0`, and `json blocks: 13, problems: 0`.
- If only the GHSA link is `BROKEN`, run `curl -sI https://github.com/advisories/GHSA-rvv3-g6hj-g44x | head -1`. If GitHub is unreachable from this machine, report it; don't remove the link.
- If an example check fails, compare the README block with the matching body in `/tmp/ticket13-checks/run1` and fix the README, not the check.

- [ ] **Step 5: Follow the README's "Everything in Docker" and "Get a token" steps**

```bash
docker compose down -v --remove-orphans
docker compose up --build --wait --wait-timeout 300; echo "exit=$?"
```

Then, in one Bash call:

```bash
C=/tmp/ticket13-checks
curl -s -o /dev/null -w "health %{http_code}\n" http://localhost:8080/health
curl -s -o /dev/null -w "swagger %{http_code}\n" http://localhost:8080/swagger/index.html
curl -s -o $C/readme-sign-up.json -w "sign-up %{http_code}\n" -X POST http://localhost:8080/api/users -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin@123","phone":"+5511999999999","email":"admin@developerstore.com","status":"Active","role":"Admin"}'
curl -s -o $C/readme-login.json -w "login %{http_code}\n" -X POST http://localhost:8080/api/auth -H "Content-Type: application/json" \
  -d '{"email":"admin@developerstore.com","password":"Admin@123"}'
TOKEN=$(python3 -c "import json, sys; print(json.load(open(sys.argv[1]))['data']['token'])" $C/readme-login.json)
curl -s -o /dev/null -w "list with the token %{http_code}\n" http://localhost:8080/api/sales -H "Authorization: Bearer $TOKEN"
docker compose down -v
```

Expected: `exit=0`, then `health 200`, `swagger 200`, `sign-up 201`, `login 200` and `list with the token 200`.

- [ ] **Step 6: Follow the README's `dotnet run` steps**

One Bash call:

```bash
docker compose up -d --wait ambev.developerevaluation.database; echo "exit=$?"
dotnet run --project src/Ambev.DeveloperEvaluation.WebApi > /tmp/ticket13-checks/run.log 2>&1 &
curl --silent --show-error --fail --retry 60 --retry-delay 2 --retry-all-errors http://localhost:5119/health; echo
curl -s -o /dev/null -w "swagger %{http_code}\n" http://localhost:5119/swagger/index.html
taskkill //F //IM Ambev.DeveloperEvaluation.WebApi.exe
docker compose down -v
```

Expected: `exit=0`, `{"status":"Healthy","healthChecks":[]}`, `swagger 200`, then `SUCCESS: The process "Ambev.DeveloperEvaluation.WebApi.exe" with PID … has been terminated.`

- [ ] **Step 7: Commit**

```bash
git add README.md
git commit -m "docs: write project README" -m "The README explains the project, how to run it with docker compose or dotnet run, how to get a token, every endpoint with examples, the business rules (including why 4 items get 10%), the architecture and decisions, the test and coverage commands, how to add a migration, the template fixes, the issues left as-is and the known limitations."
```

---

### Task 7: Verify everything, then open a pull request into `develop`

**Skills:** superpowers:verification-before-completion, `dotnet-slopwatch`, superpowers:requesting-code-review (optional), superpowers:finishing-a-development-branch, `ai-memory-handoff`.

**Files:** none.

- [ ] **Step 1: Re-run every check fresh**

```bash
git status --short
dotnet build Ambev.DeveloperEvaluation.sln --no-incremental 2>&1 | grep -E 'warning CS|Warning\(s\)|Error\(s\)|Build succeeded' | sort -u
dotnet test Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-110
cat /tmp/ticket13-checks/baseline.txt
slopwatch analyze --fail-on warning 2>&1 | tail -1
bash /tmp/ticket13-checks/check_readme_content.sh | tail -1
python3 /tmp/ticket13-checks/check_links.py | tail -1
python3 /tmp/ticket13-checks/check_readme_examples.py /tmp/ticket13-checks/run1 /tmp/ticket13-checks/run2 | tail -1
git diff --stat develop..HEAD | tail -1
git diff --name-only develop..HEAD
```

Expected:
- only the untracked tickets and plans
- `0 Error(s)`, `2 Warning(s)`, `Build succeeded.` and no `warning CS`
- the same three counts as the baseline (U0, I0, F0)
- `Scan complete: 0 issue(s) found`, `missing: 0`, `broken links: 0`, `json blocks: 13, problems: 0`
- six files: `.gitattributes`, `README.md`, `coverage-report.sh`, the `.http` file and the two test `.csproj` files

| Ticket criterion | Evidence |
|---|---|
| README covers the project, running it, the token, the endpoint table with examples, the rules with D3, the architecture and decisions, tests with the named solution and MSB1011, coverage, the migration command, fixes, issues left as-is, and limitations | Task 6 Step 3 content; content check; examples check against live responses |
| README links resolve, including `.doc/challenge.md` | link check |
| README's run and token steps work | Task 6 Steps 5 and 6 |
| `.http` runs top to bottom and can be re-run | Task 5 Steps 4 and 6 (two runs: 409 on the sign-up, random numbers, captured ids) |
| All tests pass; the coverage report covers all three suites; `coverlet.msbuild` 6.0.2 in Integration and Functional | Step 1; Task 2; Task 4 (both scripts) |
| Release `1.0.0` through Git Flow, not pushed | Tasks 8–9 |
| Conventional Commits, no AI attribution | Step 3 below; Task 9 Step 5 |

- [ ] **Step 2: Optional review**

Run superpowers:requesting-code-review on `develop..feature/readme`, with this lens: every statement in the README matches the code (routes, messages, rules, commands). Fix real findings in a separate `docs: …` commit, then re-run Step 1.

- [ ] **Step 3: Check the commit messages**

```bash
git log --format=%s develop..feature/readme
git log --format=%B develop..feature/readme | grep -n -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
```

Expected (plus any `docs:` commit from Step 2):

```
docs: write project README
docs(http): run the whole sales flow top to bottom
build: fix coverage-report.sh restore, filter and line endings
build(tests): collect coverage from the integration and functional tests
no attribution lines
```

- [ ] **Step 4: Push the branch and open a pull request into `develop`**

This is the superpowers:finishing-a-development-branch step: open a pull request into `develop` for the user to review. Don't merge it.

Write the pull request body with the Write tool to `/tmp/ticket13-checks/pr-body.md`. It holds:
- two or three sentences on what the ticket delivers (the Goal above, without the release)
- the evidence table from Step 1, with this run's results
- a note that the release (`release/1.0.0`, `v1.0.0`) happens locally after the merge and isn't pushed

No `Co-Authored-By` trailer, no "Generated with" line and no session link. Then:

```bash
git push -u origin feature/readme
gh pr create --base develop --head feature/readme --title "Ticket 13: project README, full .http flow and coverage for all suites" --body-file /tmp/ticket13-checks/pr-body.md
```

Expected: the push creates `origin/feature/readme`, and `gh pr create` prints the pull request URL. If `gh` fails (check `gh auth status`), stop and tell the user: the branch is pushed, so they can open the pull request on GitHub.

- [ ] **Step 5: Verify the pull request**

```bash
gh pr view feature/readme --json state,baseRefName,commits --jq '"\(.state) \(.baseRefName) \(.commits | length) commits"'
gh pr view feature/readme --json title,body --jq '.title + "\n" + .body' | grep -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
git status -sb | head -1
```

Expected: `OPEN develop 4 commits` (5 with a Step 2 commit), `no attribution lines`, and `## feature/readme...origin/feature/readme` with no `ahead` or `behind`.

- [ ] **Step 6: Clean up, report, and stop at the gate**

```bash
docker compose ps -a
tasklist | grep -i 'Ambev.DeveloperEvaluation.WebApi' || echo "no API process left"
```

Expected: nothing listed, then `no API process left`. Keep `/tmp/ticket13-checks/`: Task 8 needs `baseline.txt`.

Report to the user:
1. The Step 1 results, the pull request URL, and any output that differed from this plan, with what systematic-debugging found. If nothing differed, say so.
2. Ask them to run the `.http` file top to bottom twice from their editor against `docker compose up`, and to skim the README on GitHub (the Mermaid diagrams render there).
3. Ask them to merge with **Create a merge commit**, titled in Conventional Commits form, for example `docs: project README, full .http flow and coverage for all suites (#N)`, and to say when it's merged.

Stop here until the user confirms the merge. If the session ends first, save a handoff with `ai-memory-handoff`: the pull request URL, "Tasks 1–7 done", and "next: Task 8 after the merge".

---

### Task 8: Sync `develop` after the merge and run the release gate

**Skill:** superpowers:verification-before-completion.

**Files:** none.

- [ ] **Step 1: Fast-forward `develop` and confirm the merge**

```bash
git switch develop
git pull --ff-only origin develop
git fetch origin
git merge-base --is-ancestor origin/feature/readme develop && echo "feature/readme is in develop"
git log -1 --format='%s%n%b' develop | grep -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
git status --short
git branch --list 'release/*'; git tag --list 'v*'
```

Expected: the pull fast-forwards, `feature/readme is in develop`, `no attribution lines`, only untracked tickets and plans, and no release branch or tag. If the merge commit carries attribution or `feature/readme` isn't in `develop`, stop and ask the user.

- [ ] **Step 2: Release gate on the `develop` tip**

```bash
dotnet build Ambev.DeveloperEvaluation.sln --no-incremental 2>&1 | grep -E 'Warning\(s\)|Error\(s\)|Build succeeded' | sort -u
dotnet test Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-110
cat /tmp/ticket13-checks/baseline.txt
```

Expected: `Build succeeded.`, `2 Warning(s)`, `0 Error(s)`, and three `Passed!` lines with the baseline counts. If anything fails, stop: don't release a red `develop`.

---

### Task 9: Release 1.0.0 with Git Flow (local only)

**Files:** none. Git refs only.

- [ ] **Step 1: Create the release branch from `develop`**

```bash
git switch -c release/1.0.0 develop
```

Expected: `Switched to a new branch 'release/1.0.0'`. No commits go on it (Decision 7).

- [ ] **Step 2: Merge it into `main` with a merge commit**

```bash
git switch main
git pull --ff-only origin main
git merge --no-ff release/1.0.0 -m "chore(release): merge release/1.0.0 into main"
```

Expected: `main` is `Already up to date.` with `origin/main`, then `Merge made by the 'ort' strategy.` If `main` has commits that `develop` lacks, git may report conflicts: stop and ask the user.

- [ ] **Step 3: Tag the release (annotated)**

```bash
git tag -a v1.0.0 -m "chore(release): v1.0.0" -m "Sales API with complete CRUD on the .NET 8 template: create (sale number optional), get, list with paging, ordering and filters, update, cancel a sale, cancel an item, and soft delete. Quantity discounts live in a DDD aggregate, every Sales endpoint requires a JWT, and the four sale events are logged after each save. Unit, integration and functional tests, with coverage for all three suites."
```

- [ ] **Step 4: Merge the release back into `develop`, then finish**

```bash
git switch develop
git merge --no-ff main -m "chore(release): merge v1.0.0 back into develop"
git branch -d release/1.0.0
```

Expected: `Merge made by the 'ort' strategy.` and `Deleted branch release/1.0.0 (was …).` Merging `main` rather than `release/1.0.0` is Decision 7: the release branch has no commits of its own.

- [ ] **Step 5: Verify the release, and that nothing was pushed**

```bash
git log --oneline --graph --decorate -5 develop main
git cat-file -t v1.0.0
test "$(git rev-parse 'v1.0.0^{commit}')" = "$(git rev-parse main)" && echo "v1.0.0 tags main"
git log -1 --format=%P main | wc -w
git log -1 --format=%P develop | wc -w
git merge-base --is-ancestor main develop && echo "develop contains the release"
git diff --quiet main develop && echo "main and develop have the same tree"
{ git log --format=%B -2 develop; git cat-file -p v1.0.0; } | grep -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
git branch --list 'release/*'
git status -sb | head -1
git switch main && git status -sb | head -1 && git switch develop
git ls-remote --heads --tags origin | grep -E 'release/|refs/tags/v1\.0\.0' || echo "nothing of the release on origin"
```

Expected:
- the graph shows `chore(release): merge v1.0.0 back into develop` on `develop`, whose second parent is `chore(release): merge release/1.0.0 into main` with `tag: v1.0.0` and `main`
- `tag`, then `v1.0.0 tags main`
- `2` and `2` (both are merge commits)
- `develop contains the release` and `main and develop have the same tree`
- `no attribution lines`
- no release branch
- `## develop...origin/develop [ahead 1]` and `## main...origin/main [ahead N]`, where N is every commit since the initial one plus the release merge
- `nothing of the release on origin`

- [ ] **Step 6: Clean up and report**

```bash
rm -rf /tmp/ticket13-checks
docker compose ps -a
```

Expected: nothing listed.

Report to the user:
1. `v1.0.0` exists locally on `main`, and `develop` has the back-merge. Nothing was pushed.
2. When they decide to publish: `git push origin main develop` and `git push origin v1.0.0`. If `main` is protected on GitHub, the push needs that rule relaxed, or the release goes through a pull request instead.
3. Any output that differed from this plan.
