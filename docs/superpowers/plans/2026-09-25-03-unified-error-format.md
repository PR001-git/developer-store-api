# One Error Format (Ticket 03) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Every error the API raises (application exceptions, model-binding failures and request validation) answers with `{type, error, detail}` and the status codes of spec §7.4. Every success answers with the template envelope `{success, message, data}`, built exactly once. The Application validators finally run through `ValidationBehavior`, and `DomainException` gets its namespace. Delivered on `feature/error-format`, as a pull request into `develop` for the user to review.

**Architecture:** A global `ExceptionHandlingMiddleware` replaces `ValidationExceptionMiddleware`. It maps exception types to `ApiErrorResponse` records with a switch expression, logs only unexpected exceptions, and writes camelCase JSON with MVC's relaxed escaping. The `[ApiController]` invalid-model-state factory builds the same body from `ModelState`, and `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes` leaves FluentValidation as the only validator. `ApplicationModuleInitializer` registers the Application validators. `BaseController`'s helpers take the data and the message and build each envelope once, and the Users and Auth controllers throw `ValidationException` and use the helpers. Everything is driven by xUnit tests, plus one throwaway HTTP check script that runs the API without a database.

**Tech Stack:** .NET SDK 10.0.200-preview building the `net8.0` projects, with tests on the installed 8.0.23 runtime; ASP.NET Core 8 MVC; FluentValidation 11.10.0 with `FluentValidation.DependencyInjectionExtensions` 11.10.0 (new); MediatR 12.4.1; Serilog.AspNetCore 8.0.3; System.Text.Json; xUnit 2.9.2, FluentAssertions 6.12.0, NSubstitute 5.1.0, Bogus 35.6.1; curl 8.18 and `taskkill` from Git Bash; Slopwatch.Cmd 0.4.2 (global tool).

**Source:** `docs/superpowers/tickets/03-unified-error-format.md`; Sales API design spec `docs/superpowers/specs/2026-09-24-sales-api-design.md`, D10, D11, §7.2, §7.3 (paged shape), §7.4, §9.1 items 7, 8 and 11 (the namespace part), §10 and §11. The ticket also fixes responses wrapped twice, which the ticket review found, not the spec. This plan fixes one more template bug found while rehearsing it: the Serilog filter drops every warning and error (Decision 4).

**Rehearsed:** on 2026-09-25, in a scratch copy of the post-ticket-01 layout (ticket 02 not applied), every task ran in this plan's order, including each RED step, every commit, the HTTP check, slopwatch and a local `--no-ff` merge. The expected outputs below come from that run; the pushes and the pull request weren't rehearsed. A throwaway `WebApplicationFactory` probe with a stubbed `IMediator` also confirmed the new `201` from `POST /api/users`: `Location: /api/Users/{id}` and `{"success":true,"message":"User created successfully","data":{…}}`. Running after ticket 02 wasn't rehearsed. The section "Order with ticket 02" lists what differs, and none of it changes a step.

---

## Rules for every agent executing this plan

Restate these to every subagent you dispatch.

- **No AI attribution anywhere.** No `Co-Authored-By` trailer, no "Generated with" line, no session link in commits, merge commits, tags or pull requests. Use the commit messages below exactly as written. `.claude/settings.local.json` switches the defaults off, but don't rely on it.
- **Git Bash only.** Run every command in Git Bash (Claude Code's Bash tool), from the repo root `C:\Users\pr000\orca\developer-store-api` (`/c/Users/pr000/orca/developer-store-api`). Each Bash call starts a fresh shell, so don't rely on variables, functions or `cd` from an earlier call. Commands below use paths relative to the root.
- **Write file content with the Write and Edit tools**, not with shell heredocs or `sed`. The Edit tool keeps the UTF-8 BOM and CRLF line endings of the template files (rehearsed on `BaseController.cs`, `ApiResponse.cs`, `ApplicationModuleInitializer.cs`, `Program.cs` and both controllers). New files come out with LF endings; git converts them on add.
- **Work in the main checkout, never in a worktree.** `.claude/` is untracked, so it exists only here. It holds `settings.local.json` (the attribution guard) and the project skills. A worktree has neither.
- **Stage explicit paths only.** Never use `git add -A`, `git add .` or `git commit -a`. `docs/superpowers/tickets/` and other tickets' untracked plans must stay out of these commits.
- **Never run `git clean`.** `.git/info/exclude` makes `.claude/`, `.slopwatch/` and the two personal `.doc` files *ignored* files, and `git clean -x` or `-X` deletes ignored files.
- **Push only for the pull request.** Task 1 pushes `develop` with the plan commit, and Task 9 pushes `feature/error-format` and opens the pull request. Never push `main`, never force-push, and never merge the pull request: the user reviews and merges it on GitHub.
- **Leave `.doc/brainstorm-show-case.md` and `.doc/template-code-review.md` alone.** They are the user's untracked personal notes. Don't open, edit, move or stage them.
- **Don't install or configure Docker or any other system software.** This ticket needs no Docker.
- **Stop everything you start.** The HTTP check stops the API it starts. If you ever start the API yourself, stop it with `taskkill //F //IM Ambev.DeveloperEvaluation.WebApi.exe` before the task ends, because a running API locks the build output.
- **Nothing else changes.** Don't fix the pre-existing issues listed below, even when they show up in output. In particular, leave alone the AutoMapper maps that ticket 04 adds, `GetCurrentUserId`, the handlers that validate their own commands, the second `AddControllers()` in `WebApiModuleInitializer`, `PaginatedResponse.TotalCount` (ticket 10 renames it), the `.http` file (ticket 04) and the README (ticket 13).

## Skills

### Project skills in `.claude/skills/`

| Skill | Use it? | When and how |
|---|---|---|
| `dotnet-best-practices` | **Yes, every task that writes C# (2, 3, 5–8)** | Load it before Task 2. The code below follows it: XML docs on every new public type and member, the `Ambev.DeveloperEvaluation.{Layer}.{Feature}` namespaces, xUnit with FluentAssertions, NSubstitute and Bogus, `Given … When … Then …` display names with `// Given`, `// When`, `// Then` sections, and regular constructors (`.editorconfig` turns off primary constructors). Its error-handling rule is exactly what this ticket builds: throw `ValidationException`, `KeyNotFoundException` or `DomainException` and let the global middleware answer. Two bullets don't apply (Decision 13): `ArgumentNullException` guards in constructors, and `ConfigureAwait(false)`. Use the skill to review what you write. |
| `dependency-injection-patterns` | **Yes, Task 6** | Its project note decides where the validators go: register them in `ApplicationModuleInitializer`, not in a new `Add*` extension method. Its lifetime rules hold: `AddValidatorsFromAssembly` registers the validators as scoped, `ValidationBehavior` is transient and resolves them inside the request scope, and the pipeline test creates a scope before resolving `IMediator`. Don't register validators as singletons. |
| `dotnet-slopwatch` | **Yes, after every commit and in Task 9** | Run `slopwatch analyze --fail-on warning` and expect `Scan complete: 0 issue(s) found` (rehearsed after every task). The baseline in `.slopwatch/baseline.json` came from ticket 01. Follow its project note: slopwatch is a global tool, so don't add a tool manifest for it, and `.slopwatch/` stays git-excluded. |
| `test-anti-patterns` | **Yes, Task 9 (report only)** | Before opening the pull request, audit the six new test classes and the two helpers (`MvcJson`, `UserRequestTestData`). The rehearsal pre-check found nothing Critical or High. The one likely remark is unseeded Bogus data in `UserRequestTestData`: it's the template's convention (spec §10), and every generated value is valid by construction, so report it and don't change it. Fix any real finding in a separate `test: …` commit. The skill loads `test-analysis-extensions` by itself. |
| `type-design-performance` | Light, Tasks 3, 5 and 7 | Every new class is `sealed` (the middleware and all test classes), `ApiErrorResponse` is a `sealed record`, and the factory is a `static class`. The middleware keeps one `static readonly JsonSerializerOptions`: creating options per request defeats System.Text.Json's metadata cache. `BaseController`, `ApiResponse` and `ApiResponseWithData<T>` stay unsealed, because other types inherit from them. |
| `clean-code` | Optional, Task 9 review | The middleware's switch expression *is* the §7.4 table, one arm per row. Names follow the spec's words (`ValidationError`, `ConcurrencyConflict`). No other refactoring. |
| `test-analysis-extensions` | Indirectly | `test-anti-patterns` loads its .NET tables. Don't invoke it directly. |
| `efcore-patterns` | No | The only EF Core touchpoint is catching `DbUpdateConcurrencyException` by type. No queries, mappings or migrations change. |
| `testcontainers-integration-tests` | No | Nothing here needs a database. The functional tests that check these bodies over HTTP with a real database come in tickets 04 and 05. |
| `test-smell-detection` | No | `test-anti-patterns` covers the review. A formal smell catalogue isn't needed. |
| `ai-memory-handoff` | **Yes, if execution stops early** | Save a handoff naming the last completed task and step. The next session looks for it at startup. |
| `ai-memory-retrieval` | Optional, once at the start | Search for "ticket 03", "error format" or "envelope" to catch gotchas recorded after 2026-09-25. |
| `ai-memory-durable-pages` | Only if the user asks | For example, if the user wants the Serilog-filter finding (Decision 4) remembered permanently. Otherwise, no. |
| `ai-memory-learning-maintenance`, `ai-memory-messaging`, `ai-memory-routing-install` | No | They cover memory maintenance, cross-project messages and install work. |

### Process skills (superpowers plugin)

- `superpowers:subagent-driven-development` or `superpowers:executing-plans` runs this plan (see the header).
- `superpowers:test-driven-development` applies to every task that changes behavior. Each task starts with a check that fails for the stated reason: compile errors for types that don't exist yet (Tasks 3, 5 and 7), failing assertions (Tasks 6, 7 and 8), a `git grep` (Task 2), or lines of the HTTP check (Tasks 1 and 4). Watch it fail before writing the fix.
- `superpowers:systematic-debugging` applies whenever an output differs from what this plan expects.
- `superpowers:verification-before-completion` applies at the end of every task and fully in Task 9: re-run the checks fresh before claiming anything passes.
- `superpowers:requesting-code-review` is optional in Task 9, before the pull request.
- `superpowers:finishing-a-development-branch` applies in Task 9, but the option is already chosen: push the branch and open a pull request into `develop` for the user to review. Don't merge it, keep the branch, and don't offer the other options.
- Don't use `superpowers:using-git-worktrees` (see the rules) or `superpowers:brainstorming` (the design is settled in the spec and the ticket).

## Decisions this plan makes

1. **The error body is `ApiErrorResponse`**, a `sealed record (Type, Error, Detail)` in `WebApi/Common`. Its static `ValidationError(failures)` formats `Property: message` pairs joined with `; ` (the message alone when there's no property). The middleware and the model-binding factory both call it, so the two 400s can't drift apart.
2. **The middleware maps with one switch expression** in the order of the §7.4 table. Only the 500 arm logs: `LogError(exception, "Unhandled exception while processing {Method} {Path}", …)`, so Serilog writes the stack trace. 400, 401, 404 and 409 are expected outcomes and aren't logged.
3. **Error JSON matches MVC's JSON.** The middleware writes with `JsonSerializerDefaults.Web` (camelCase) plus `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`, the encoder MVC's output formatter uses. Without it, an apostrophe comes out as a Unicode escape sequence, so R1's message would read differently in a 409 than in a 400. `WriteAsJsonAsync` sets `Content-Type: application/json; charset=utf-8`. The media type is `application/json`, as the ticket asks, and it's the same header MVC writes for the model-binding 400.
4. **The Serilog filter is fixed here (template bug, not in the spec).** `LoggingExtension._filterPredicate` is meant to hide successful `/health` request logs. It returns `true` for every event that isn't `Information`, and `Filter.ByExcluding` drops whatever the predicate matches. So every warning, error and fatal event has always been discarded. The rehearsal showed it: with the new middleware, a 500 logged nothing at all. The ticket requires "logged with its stack trace", so Task 4 changes `return true` to `return false` for non-Information events, in its own commit. The `/health` exclusion is unchanged. There's no unit test: the predicate is a private lambda inside `AddDefaultLogging`, so testing it would need reflection or a new seam. The HTTP check reads the real log instead. Ticket 13's README list of review findings should mention it; tell the user (Task 9).
5. **The model-binding factory is `InvalidModelStateResponseFactory.Create`**, a static method wired in `Program.cs` through `ConfigureApiBehaviorOptions`. Keys are sorted by ordinal comparison, because `ModelStateDictionary` doesn't enumerate in insertion order (rehearsed: `_page` came before `$.quantity`). An error that carries only an exception gets ASP.NET Core's own generic text, `The input was not valid.`, so exception messages never leak. Keys stay exactly as ASP.NET Core reports them (`$.email`, `id`). FluentValidation property names stay PascalCase (`Email`), as FluentValidation reports them. The spec asks only for `Property: message`.
6. **`SuppressImplicitRequiredAttributeForNonNullableReferenceTypes` is set where `Program.cs` calls `AddControllers`.** The rehearsal proved it matters: without the flag, malformed JSON adds `request: The request field is required.` to the detail. No request or query contract has an optional field today (every `AuthenticateUserRequest` and `CreateUserRequest` property is required by its validator, and `GetUserRequest` and `DeleteUserRequest` hold a `Guid`). So no contract changes. The Sales contracts in tickets 05 and 06 declare `saleNumber` as `string?`.
7. **The validators are registered in `ApplicationModuleInitializer`** with `AddValidatorsFromAssembly(typeof(ApplicationLayer).Assembly)`, following the `dependency-injection-patterns` project note, with the package in the IoC project. Only the Application assembly is scanned. The controllers keep creating their WebApi request validators with `new`, as today.
8. **The pipeline test uses `AuthenticateUserCommand`.** `CreateUserHandler`, `GetUserHandler` and `DeleteUserHandler` validate their own commands and throw `ValidationException` themselves, so a test built on them would pass even with no validators registered. `AuthenticateUserHandler` doesn't validate, so only `ValidationBehavior` can throw. The test builds the MediatR pipeline the way `Program.cs` does and takes the validators from the real IoC module.
9. **`BaseController`'s helpers:** `Ok(data, message)`, `Ok(message)`, `Created(actionName, routeValues, data, message)` and `OkPaginated(pagedList, message)`, which calls `base.Ok`. `Created` now uses `CreatedAtAction`. The template's `CreatedAtRoute(routeName, …)` was never called, and no route has a name. The `BadRequest(string)` and `NotFound(string)` helpers are removed: they built the old `{success: false, message, errors}` shape and nothing called them. `ApiResponse.Errors` is removed.
10. **Envelope property order follows the spec's examples.** System.Text.Json writes a derived class's properties first, which gave `{"data":…,"success":…,"message":…}` in the rehearsal. `[JsonPropertyOrder]` on `Success` (-3), `Message` (-2) and `Data` (-1) gives `{success, message, data}`, and the paged shape becomes `{success, message, data, currentPage, totalPages, totalCount}` (spec §7.2 and §7.3).
11. **`POST /api/users` now sends a `Location` header**, `/api/Users/{id}`, because it uses the `Created` helper. Before, `Created(string.Empty, …)` sent none. The probe confirmed the URL. Ticket 05 uses the same helper for `Location: /api/sales/{id}`.
12. **Tests serialize like MVC.** A small test helper, `MvcJson`, serializes with the same options MVC's output formatter uses. So the tests compare the exact bodies clients get, including the relaxed escaping.
13. **Constructors follow the template: no `ArgumentNullException` guards.** ASP.NET Core's DI builds the middleware and the controllers and never passes null, and the template's middleware, handlers and controllers have no guards. `ConfigureAwait(false)` isn't used either: ASP.NET Core has no synchronization context, and the template doesn't use it. If the user wants guards anyway, they're a follow-up that touches every constructor.
14. **A throwaway HTTP check stands in for functional tests.** It runs the real API in Production with no launch profile, so there are no startup migrations and no database. It sends requests that fail before any database access, compares each body byte for byte, and checks that the 500 is logged with its stack trace. It also confirms that the framework's own 404, 405 and 415 keep ASP.NET Core's defaults. It lives in `/tmp/ticket03-checks/`, outside the repo, and is deleted at the end.
15. **The README line about 404, 405 and 415 is ticket 13's job.** Ticket 13's checklist already lists it ("the framework's own 404, 405 and 415 responses … (ticket 03)"), and `README.md` is a placeholder until then. This ticket keeps the defaults and proves it in the HTTP check.
16. **Swagger documents the real error body.** `[ProducesResponseType]` on the Users and Auth error statuses now names `ApiErrorResponse` instead of `ApiResponse`.
17. **This plan is committed on `develop` before branching** (spec §11), as tickets 01 and 02 did, and `develop` is pushed. The work reaches `develop` through a pull request the user reviews and merges on GitHub with **Create a merge commit**; the agent never merges it. The feature branch is kept.

## Order with ticket 02

Ticket 03 is blocked only by ticket 01, so it may run before or after ticket 02. Every edit here anchors on lines that ticket 02 doesn't change: `builder.Services.AddControllers();`, the `app.UseMiddleware<ValidationExceptionMiddleware>();` line itself, and the `using` block. So each step works in either order. What differs:

- **If ticket 02 is already merged:** its Functional and Integration tests need Docker. That's why this plan always runs `dotnet test tests/Ambev.DeveloperEvaluation.Unit` rather than the whole solution. The HTTP check runs the API in Production, so ticket 02's Development-only startup migrations never run, and nothing needs the database. Task 9 Step 2 runs the whole solution only when Docker is up.
- **If ticket 02 hasn't run yet:** its plan (`docs/superpowers/plans/2026-09-25-02-run-api-against-postgresql.md`) was written against today's code, and three things in it go stale once this ticket merges. Don't edit that plan; report these to the user in Task 9:
  - Task 10 Step 6 anchors on `app.UseMiddleware<ValidationExceptionMiddleware>();`, which becomes `app.UseMiddleware<ExceptionHandlingMiddleware>();`.
  - Its unit-test baselines expect `Passed:    49`, which becomes `73`.
  - Its derived log line `Failed to determine the https port for redirect.` only appears once Task 4 here has fixed the Serilog filter. Before that, the filter drops it.

## Pre-existing output you'll see (leave it alone)

- `warning NU1903` (AutoMapper 13.0.1 advisory; spec D14 keeps that version) and `warning CS8604` at `JwtTokenGenerator.cs(42,43)`, fixed by ticket 04. A clean solution build (`--no-incremental`) shows `3 Warning(s)`; incremental builds show `2 Warning(s)` or fewer.
- `message NETSDK1057: You are using a preview version of .NET`. The machine has only the .NET 10 preview SDK. It builds `net8.0` fine, and the tests run on the installed 8.0.23 runtime.
- `MSB1011` from a bare `dotnet build` or `dotnet test` at the root, because `docker-compose.dcproj` sits next to the `.sln`. Always pass `Ambev.DeveloperEvaluation.sln` or a project path.
- `warning: in the working copy of '…', LF will be replaced by CRLF the next time Git touches it` when adding new files, and after `dotnet add` writes an LF line into a CRLF `.csproj`. It's harmless: `core.autocrlf=true`.
- The HTTP check's API writes `src/Ambev.DeveloperEvaluation.WebApi/logs/`, which `.gitignore` ignores (`[Ll]ogs/`), and prints many `[INF]` lines into `/tmp/ticket03-checks/api.log`.
- From Task 4 on, that log also shows `[WRN] Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware Failed to determine the https port for redirect.` The API is HTTP only, and `UseHttpsRedirection()` stays.
- The HTTP check's 500 comes from AutoMapper's `Missing type map configuration or unsupported mapping. … AuthenticateUserRequest -> AuthenticateUserCommand`. That missing map is ticket 04's fix. Here it's just a convenient real unexpected exception that needs no database.

## File map

| Change | Paths |
|---|---|
| Created (WebApi) | `src/Ambev.DeveloperEvaluation.WebApi/Common/ApiErrorResponse.cs`, `src/Ambev.DeveloperEvaluation.WebApi/Common/InvalidModelStateResponseFactory.cs`, `src/Ambev.DeveloperEvaluation.WebApi/Middleware/ExceptionHandlingMiddleware.cs` |
| Deleted | `src/Ambev.DeveloperEvaluation.WebApi/Middleware/ValidationExceptionMiddleware.cs` |
| Modified (WebApi) | `Program.cs`; `Common/ApiResponse.cs`, `Common/ApiResponseWithData.cs`, `Common/BaseController.cs`; `Features/Auth/AuthController.cs`; `Features/Users/UsersController.cs` |
| Modified (other layers) | `src/Ambev.DeveloperEvaluation.Domain/Exceptions/DomainException.cs` (namespace); `src/Ambev.DeveloperEvaluation.Common/Logging/LoggingExtension.cs` (one line); `src/Ambev.DeveloperEvaluation.IoC/Ambev.DeveloperEvaluation.IoC.csproj` (package) and `ModuleInitializers/ApplicationModuleInitializer.cs` |
| Created (unit tests) | under `tests/Ambev.DeveloperEvaluation.Unit/`: `WebApi/Middleware/ExceptionHandlingMiddlewareTests.cs`, `WebApi/Common/{InvalidModelStateResponseFactoryTests, BaseControllerTests}.cs`, `WebApi/Features/Auth/AuthControllerTests.cs`, `WebApi/Features/Users/UsersControllerTests.cs`, `IoC/ValidationPipelineTests.cs`, `WebApi/MvcJson.cs`, `WebApi/TestData/UserRequestTestData.cs` |
| Modified (unit tests) | `tests/Ambev.DeveloperEvaluation.Unit/Ambev.DeveloperEvaluation.Unit.csproj` (reference to WebApi) |
| Committed on `develop` first | `docs/superpowers/plans/2026-09-25-03-unified-error-format.md` (this plan) |
| Local only, never committed | `/tmp/ticket03-checks/` (the HTTP check, its log and response body; removed at the end); git-ignored `logs/` folders |
| Not touched | the Dockerfiles and compose files, `appsettings*.json`, both `launchSettings.json` files, the `.http` file, `README.md`, the Application handlers and validators, `PaginatedList.cs`, `PaginatedResponse.cs`, `WebApiModuleInitializer.cs`, the Integration and Functional test projects, `docs/superpowers/tickets/`, `.claude/`, `.slopwatch/`, the personal `.doc` files |

---

### Task 1: Check the starting point, commit this plan, branch, and write the HTTP check

**Files:**
- Commit on `develop`: `docs/superpowers/plans/2026-09-25-03-unified-error-format.md`
- Create (outside the repo): `/tmp/ticket03-checks/check-errors.sh`

- [ ] **Step 1: Confirm ticket 01 is merged and the tree is clean**

```bash
git switch develop
git pull --ff-only origin develop
git log --oneline -3
git diff --quiet && git diff --cached --quiet && echo "no tracked changes"
git status --short
test ! -e template && test -f Ambev.DeveloperEvaluation.sln && test -d src && echo "ticket 01 layout in place"
test -f .slopwatch/baseline.json && echo "slopwatch baseline present"
git log --oneline --merges | grep -oE "feature/(repo-restructure|postgres-runtime)"
git branch --list feature/error-format
test -f src/Ambev.DeveloperEvaluation.WebApi/Middleware/ValidationExceptionMiddleware.cs && echo "template middleware still there"
```

Expected:
- The pull fast-forwards `develop` to the merged pull requests on GitHub, or prints `Already up to date.`
- `no tracked changes`, `ticket 01 layout in place`, `slopwatch baseline present` and `template middleware still there`.
- `git status --short` shows `?? docs/superpowers/plans/2026-09-25-03-unified-error-format.md` and `?? docs/superpowers/tickets/`. Other tickets' untracked plan files are fine; leave them alone.
- The merges line prints `feature/repo-restructure`, plus `feature/postgres-runtime` if ticket 02 is done. Note which: it only matters for Task 9 (see "Order with ticket 02").
- The branch check prints nothing.

Stop and ask the user if:
- the pull fails. Local `develop` must only ever fast-forward to `origin/develop`.
- `template/` still exists. Ticket 01 isn't done (or its pull request isn't merged yet), and this ticket is blocked by it.
- anything tracked is modified.
- `feature/error-format` already exists, because an earlier run got partway. In that case, look for an ai-memory handoff first.

- [ ] **Step 2: Check the tools**

```bash
dotnet --list-sdks
command -v slopwatch
curl --version | head -1
command -v taskkill
```

Expected: SDK `10.0.200-preview…`, a slopwatch path, `curl 8.18.0 …` and `/c/WINDOWS/system32/taskkill`. The HTTP check needs curl and taskkill.

- [ ] **Step 3: Commit this plan on `develop` and push `develop`**

If `git status --short docs/superpowers/plans/2026-09-25-03-unified-error-format.md` prints nothing, the plan is already committed: skip `git add` and `git commit`, and still run the push.

```bash
git add docs/superpowers/plans/2026-09-25-03-unified-error-format.md
git commit -m "docs: add plan for the unified error format"
git status --short
git push origin develop
```

Expected: a commit with one file changed. `git status --short` then lists `?? docs/superpowers/tickets/`, plus any other untracked plan files. The push sends the plan commit to `origin/develop` (or prints `Everything up-to-date`), so the Task 9 pull request holds only the feature commits. If the push is rejected, stop and ask the user; never force it.

- [ ] **Step 4: Create the feature branch**

```bash
git switch -c feature/error-format
git log --oneline -1
```

Expected: `Switched to a new branch 'feature/error-format'`, with HEAD at the plan commit.

- [ ] **Step 5: Record the baseline**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E 'Warning\(s\)|Error\(s\)|Build succeeded'
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `Build succeeded.`, `3 Warning(s)`, `0 Error(s)`, then `Passed!  - Failed:     0, Passed:    49, Skipped:     0, Total:    49`. If the baseline isn't green, stop: nothing can be judged against a broken start.

- [ ] **Step 6: Write the HTTP check**

In Git Bash, `/tmp` is `C:\Users\pr000\AppData\Local\Temp`. That's a fixed path outside the repo, so every task and subagent finds the same file, and it can't be committed. With the Write tool, create `C:\Users\pr000\AppData\Local\Temp\ticket03-checks\check-errors.sh` with exactly this content:

```bash
#!/usr/bin/env bash
# Ticket 03 HTTP check. Run from the repo root. It starts the API in Production on
# http://localhost:5119 with no launch profile, so nothing touches a database, sends
# requests that fail before any database access, and compares each response with
# the {type, error, detail} contract. The framework's own 404, 405 and 415 must keep
# ASP.NET Core's defaults. The API is stopped on exit.
checks_dir=$(cd "$(dirname "$0")" && pwd)
log="$checks_dir/api.log"
body_file="$checks_dir/body.txt"
url=http://localhost:5119

stop_api() { taskkill //F //IM Ambev.DeveloperEvaluation.WebApi.exe >/dev/null 2>&1; }
trap stop_api EXIT
stop_api

ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=$url \
  dotnet run --no-launch-profile --project src/Ambev.DeveloperEvaluation.WebApi > "$log" 2>&1 &
if ! curl -s -o /dev/null --retry 120 --retry-delay 1 --retry-connrefused "$url/health"; then
  echo "PROBLEM  the API did not start; see $log"; exit 1
fi

problems=0
# check NAME METHOD PATH CONTENT_TYPE BODY EXPECTED_STATUS EXPECTED_MEDIA_TYPE EXPECTED_BODY
# EXPECTED_MEDIA_TYPE and EXPECTED_BODY: "-" means none, "*" means anything.
check() {
  local name=$1 method=$2 path=$3 content_type=$4 body=$5 want_status=$6 want_media=$7 want_body=$8
  local args=(-s -o "$body_file" -w '%{http_code} %{content_type}' -X "$method")
  [ "$content_type" != "-" ] && args+=(-H "Content-Type: $content_type")
  [ "$body" != "-" ] && args+=(--data-raw "$body")
  local got status media actual
  got=$(curl "${args[@]}" "$url$path")
  status=${got%% *}
  media=${got#* }; media=${media%%;*}; [ -z "$media" ] && media="-"
  actual=$(cat "$body_file"); [ -z "$actual" ] && actual="-"
  if [ "$status" = "$want_status" ] && { [ "$want_media" = "*" ] || [ "$media" = "$want_media" ]; } \
     && { [ "$want_body" = "*" ] || [ "$actual" = "$want_body" ]; }; then
    echo "ok       $name"
  else
    echo "PROBLEM  $name"
    echo "         got      $status $media $actual"
    echo "         expected $want_status $want_media $want_body"
    problems=$((problems + 1))
  fi
}

check "malformed JSON -> 400 ValidationError" POST /api/auth application/json '{"email":' \
  400 application/json '{"type":"ValidationError","error":"Invalid input data","detail":"$.email: Expected depth to be zero at the end of the JSON payload. There is an open JSON object or array that should be closed. Path: $.email | LineNumber: 0 | BytePositionInLine: 9."}'
check "empty login body -> 400 from FluentValidation" POST /api/auth application/json '{}' \
  400 application/json '{"type":"ValidationError","error":"Invalid input data","detail":"Email: Email is required; Email: Invalid email format; Password: Password is required"}'
check "bad route value -> 400 ValidationError" GET /api/users/not-a-guid - - \
  400 application/json '{"type":"ValidationError","error":"Invalid input data","detail":"id: The value '"'"'not-a-guid'"'"' is not valid."}'
check "empty user id -> 400 from the request validator" GET /api/users/00000000-0000-0000-0000-000000000000 - - \
  400 application/json '{"type":"ValidationError","error":"Invalid input data","detail":"Id: User ID is required"}'
check "unexpected exception -> 500 without internals" POST /api/auth application/json '{"email":"user@example.com","password":"Secret@123"}' \
  500 application/json '{"type":"InternalServerError","error":"Internal server error","detail":"An unexpected error occurred."}'
check "unknown route keeps the framework 404" GET /api/nope - - 404 - -
check "wrong method keeps the framework 405" PUT /api/auth application/json '{}' 405 - -
check "wrong content type keeps the framework 415" POST /api/auth text/plain 'hello' 415 application/problem+json '*'

if grep -q 'AutoMapper.AutoMapperMappingException' "$log" && grep -q '^   at ' "$log"; then
  echo "ok       the 500 is logged with its stack trace"
else
  echo "PROBLEM  the 500 is not logged with its stack trace"; problems=$((problems + 1))
fi

echo "error-format problems: $problems"
[ "$problems" -eq 0 ]
```

What each line proves:
- **Malformed JSON:** the model-binding factory. The exact detail, with no trailing `; request: The request field is required.`, also proves the suppress flag (Decision 6).
- **Empty login body and empty user id:** the controllers' `ValidationException` going through the middleware.
- **Bad route value:** the factory for a route value.
- **The valid-looking login:** a real unexpected exception, the missing AutoMapper map, becoming a clean 500, plus the log line with its stack trace.
- **404, 405 and 415:** the framework's defaults, untouched.

- [ ] **Step 7: Run it and watch it fail (RED)**

```bash
bash /tmp/ticket03-checks/check-errors.sh | grep -E '^(ok|PROBLEM)|problems:'; echo "exit=${PIPESTATUS[0]}"
```

Expected (about 20 seconds, most of it the build):

```
PROBLEM  malformed JSON -> 400 ValidationError
PROBLEM  empty login body -> 400 from FluentValidation
PROBLEM  bad route value -> 400 ValidationError
PROBLEM  empty user id -> 400 from the request validator
PROBLEM  unexpected exception -> 500 without internals
ok       unknown route keeps the framework 404
ok       wrong method keeps the framework 405
ok       wrong content type keeps the framework 415
PROBLEM  the 500 is not logged with its stack trace
error-format problems: 6
exit=1
```

The full output (without the `grep`) shows the template's bodies:
- `application/problem+json` `ValidationProblemDetails` for malformed JSON and the bad route value. The malformed-JSON body also has `"request":["The request field is required."]`.
- FluentValidation's raw failure list (`[{"propertyName":"Email",…}]`) for the two validator failures.
- `500 - -`, an empty 500 with no body, for the unexpected exception.

---

### Task 2: Give `DomainException` its namespace

**Skill:** `dotnet-best-practices`.

**Files:**
- Modify: `src/Ambev.DeveloperEvaluation.Domain/Exceptions/DomainException.cs`

- [ ] **Step 1: Confirm it has no namespace (RED)**

```bash
git grep -n '^namespace' -- src/Ambev.DeveloperEvaluation.Domain/Exceptions/DomainException.cs || echo "no namespace"
git grep -n 'DomainException' -- src tests | grep -v '/Exceptions/DomainException.cs'
```

Expected: `no namespace`, and the second command prints nothing: no code references `DomainException` yet, so moving it breaks nothing.

- [ ] **Step 2: Add the namespace**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.Domain/Exceptions/DomainException.cs`, replace

```csharp
public class DomainException:Exception
{
```

with

```csharp
namespace Ambev.DeveloperEvaluation.Domain.Exceptions;

public class DomainException:Exception
{
```

Change nothing else in the file.

- [ ] **Step 3: Verify (GREEN)**

```bash
git grep -n '^namespace' -- src/Ambev.DeveloperEvaluation.Domain/Exceptions/DomainException.cs
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E 'Warning\(s\)|Error\(s\)|Build succeeded'
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `src/Ambev.DeveloperEvaluation.Domain/Exceptions/DomainException.cs:1:namespace Ambev.DeveloperEvaluation.Domain.Exceptions;`, `Build succeeded.` with `0 Error(s)`, and `Passed:    49`.

- [ ] **Step 4: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Domain/Exceptions/DomainException.cs
git commit -m "refactor(domain): move DomainException into the Domain.Exceptions namespace" -m "DomainException was declared in the global namespace. It now lives in Ambev.DeveloperEvaluation.Domain.Exceptions, matching its folder, so the exception middleware and the Sales aggregate can import it. Nothing referenced it yet."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `1 file changed, 2 insertions(+)` and `Scan complete: 0 issue(s) found`.

---

### Task 3: Map exceptions to `{type, error, detail}` in a global middleware

**Skills:** `dotnet-best-practices`, `type-design-performance`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/Ambev.DeveloperEvaluation.Unit.csproj` (reference to WebApi)
- Create: `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Middleware/ExceptionHandlingMiddlewareTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.WebApi/Common/ApiErrorResponse.cs`
- Create: `src/Ambev.DeveloperEvaluation.WebApi/Middleware/ExceptionHandlingMiddleware.cs`
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Program.cs` (one line)
- Delete: `src/Ambev.DeveloperEvaluation.WebApi/Middleware/ValidationExceptionMiddleware.cs`

- [ ] **Step 1: Reference WebApi from the Unit test project**

```bash
dotnet add tests/Ambev.DeveloperEvaluation.Unit/Ambev.DeveloperEvaluation.Unit.csproj reference src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.csproj
git diff tests/Ambev.DeveloperEvaluation.Unit/Ambev.DeveloperEvaluation.Unit.csproj | grep -E '^[+-][^+-]'
```

Expected: ``Reference `..\..\src\Ambev.DeveloperEvaluation.WebApi\Ambev.DeveloperEvaluation.WebApi.csproj` added to the project.``, then exactly one added line (tab-indented):

```
+		<ProjectReference Include="..\..\src\Ambev.DeveloperEvaluation.WebApi\Ambev.DeveloperEvaluation.WebApi.csproj" />
```

The reference brings in ASP.NET Core, EF Core (through IoC and ORM) and FluentValidation, so the tests below need no new packages.

- [ ] **Step 2: Write the failing tests, one per row of the §7.4 table**

Create `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Middleware/ExceptionHandlingMiddlewareTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.WebApi.Middleware;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Middleware;

/// <summary>
/// Contains unit tests for the <see cref="ExceptionHandlingMiddleware"/> class:
/// one test per row of the error table in the Sales API design spec (§7.4).
/// </summary>
public sealed class ExceptionHandlingMiddlewareTests
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = Substitute.For<ILogger<ExceptionHandlingMiddleware>>();

    /// <summary>
    /// Tests that a request that doesn't throw passes through untouched.
    /// </summary>
    [Fact(DisplayName = "Given a request that succeeds When the middleware runs Then the response is left untouched")]
    public async Task Given_RequestThatSucceeds_When_MiddlewareRuns_Then_ResponseIsUntouched()
    {
        // Given
        var middleware = new ExceptionHandlingMiddleware(
            context =>
            {
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            },
            _logger);
        var context = CreateContext();

        // When
        await middleware.InvokeAsync(context);

        // Then
        context.Response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        (await ReadBodyAsync(context)).Should().BeEmpty();
    }

    /// <summary>
    /// Tests that a <see cref="ValidationException"/> becomes a 400 listing every failure.
    /// </summary>
    [Fact(DisplayName = "Given a ValidationException When the middleware handles it Then it returns 400 ValidationError with each failure as Property: message")]
    public async Task Given_ValidationException_When_Handled_Then_Returns400ValidationError()
    {
        // Given
        var exception = new ValidationException(new[]
        {
            new ValidationFailure("Email", "Invalid email format"),
            new ValidationFailure("Password", "Password is required")
        });

        // When
        var context = await HandleAsync(exception);

        // Then
        await ShouldBeErrorAsync(context, StatusCodes.Status400BadRequest,
            """{"type":"ValidationError","error":"Invalid input data","detail":"Email: Invalid email format; Password: Password is required"}""");
    }

    /// <summary>
    /// Tests that an <see cref="UnauthorizedAccessException"/> becomes a 401 with the exception message.
    /// </summary>
    [Fact(DisplayName = "Given an UnauthorizedAccessException When the middleware handles it Then it returns 401 AuthenticationError with the message")]
    public async Task Given_UnauthorizedAccessException_When_Handled_Then_Returns401AuthenticationError()
    {
        // Given
        var exception = new UnauthorizedAccessException("Invalid credentials");

        // When
        var context = await HandleAsync(exception);

        // Then
        await ShouldBeErrorAsync(context, StatusCodes.Status401Unauthorized,
            """{"type":"AuthenticationError","error":"Authentication failed","detail":"Invalid credentials"}""");
    }

    /// <summary>
    /// Tests that a <see cref="KeyNotFoundException"/> becomes a 404 with the exception message.
    /// </summary>
    [Fact(DisplayName = "Given a KeyNotFoundException When the middleware handles it Then it returns 404 ResourceNotFound with the message")]
    public async Task Given_KeyNotFoundException_When_Handled_Then_Returns404ResourceNotFound()
    {
        // Given
        var exception = new KeyNotFoundException("The sale with ID 7f9c2a44-5555-4d1e-8a3b-000000000010 does not exist");

        // When
        var context = await HandleAsync(exception);

        // Then
        await ShouldBeErrorAsync(context, StatusCodes.Status404NotFound,
            """{"type":"ResourceNotFound","error":"Resource not found","detail":"The sale with ID 7f9c2a44-5555-4d1e-8a3b-000000000010 does not exist"}""");
    }

    /// <summary>
    /// Tests that a <see cref="DomainException"/> becomes a 409 with the exception message,
    /// escaped the way MVC escapes success bodies (the apostrophe stays as it is).
    /// </summary>
    [Fact(DisplayName = "Given a DomainException When the middleware handles it Then it returns 409 BusinessRuleViolation with the message")]
    public async Task Given_DomainException_When_Handled_Then_Returns409BusinessRuleViolation()
    {
        // Given
        var exception = new DomainException("It's not possible to sell above 20 identical items");

        // When
        var context = await HandleAsync(exception);

        // Then
        await ShouldBeErrorAsync(context, StatusCodes.Status409Conflict,
            """{"type":"BusinessRuleViolation","error":"Business rule violation","detail":"It's not possible to sell above 20 identical items"}""");
    }

    /// <summary>
    /// Tests that a <see cref="DbUpdateConcurrencyException"/> becomes a 409 with a fixed detail.
    /// </summary>
    [Fact(DisplayName = "Given a DbUpdateConcurrencyException When the middleware handles it Then it returns 409 ConcurrencyConflict with the fixed detail")]
    public async Task Given_DbUpdateConcurrencyException_When_Handled_Then_Returns409ConcurrencyConflict()
    {
        // Given
        var exception = new DbUpdateConcurrencyException("The database operation was expected to affect 1 row(s), but actually affected 0 row(s).");

        // When
        var context = await HandleAsync(exception);

        // Then
        await ShouldBeErrorAsync(context, StatusCodes.Status409Conflict,
            """{"type":"ConcurrencyConflict","error":"Concurrent modification","detail":"The sale was changed by another request. Reload it and try again."}""");
    }

    /// <summary>
    /// Tests that any other exception becomes a 500 whose body reveals nothing about it.
    /// </summary>
    [Fact(DisplayName = "Given an unexpected exception When the middleware handles it Then it returns 500 InternalServerError without the exception details")]
    public async Task Given_UnexpectedException_When_Handled_Then_Returns500WithoutDetails()
    {
        // Given
        var exception = new InvalidOperationException("Host=db;Password=secret");

        // When
        var context = await HandleAsync(exception);

        // Then
        await ShouldBeErrorAsync(context, StatusCodes.Status500InternalServerError,
            """{"type":"InternalServerError","error":"Internal server error","detail":"An unexpected error occurred."}""");
    }

    /// <summary>
    /// Tests that an unexpected exception is logged as an error, together with the exception and its stack trace.
    /// </summary>
    [Fact(DisplayName = "Given an unexpected exception When the middleware handles it Then it logs the exception as an error")]
    public async Task Given_UnexpectedException_When_Handled_Then_LogsItAsError()
    {
        // Given
        var exception = new InvalidOperationException("Host=db;Password=secret");

        // When
        await HandleAsync(exception);

        // Then
        var logCall = _logger.ReceivedCalls().Should().ContainSingle(call => call.GetMethodInfo().Name == nameof(ILogger.Log)).Subject;
        logCall.GetArguments()[0].Should().Be(LogLevel.Error);
        logCall.GetArguments()[3].Should().BeSameAs(exception);
    }

    private async Task<HttpContext> HandleAsync(Exception exception)
    {
        var middleware = new ExceptionHandlingMiddleware(_ => throw exception, _logger);
        var context = CreateContext();
        await middleware.InvokeAsync(context);
        return context;
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    private static async Task ShouldBeErrorAsync(HttpContext context, int statusCode, string body)
    {
        context.Response.StatusCode.Should().Be(statusCode);
        context.Response.ContentType.Should().Be("application/json; charset=utf-8");
        (await ReadBodyAsync(context)).Should().Be(body);
    }
}
```

Notes for reviewers:
- `ILogger.Log` is generic over an internal state type, so the log test reads the one received call's arguments rather than matching them with `Arg.Any`. Argument 0 is the level, and argument 3 is the exception, which Serilog renders with its stack trace.
- The 500 tests use an exception message that looks like a secret, and the exact-body assertion proves it never leaks.

- [ ] **Step 3: Build and watch it fail (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected:

```
ExceptionHandlingMiddlewareTests.cs(20,30): error CS0246: The type or namespace name 'ExceptionHandlingMiddleware' could not be found (are you missing a using directive or an assembly reference?)
```

- [ ] **Step 4: Write the error body**

Create `src/Ambev.DeveloperEvaluation.WebApi/Common/ApiErrorResponse.cs`:

```csharp
namespace Ambev.DeveloperEvaluation.WebApi.Common;

/// <summary>
/// The body of every error response, <c>{type, error, detail}</c>, as described in <c>.doc/general-api.md</c>.
/// </summary>
/// <param name="Type">A machine-readable error type identifier, such as <c>ValidationError</c>.</param>
/// <param name="Error">A short, human-readable summary of the problem.</param>
/// <param name="Detail">A human-readable explanation specific to this occurrence of the problem.</param>
public sealed record ApiErrorResponse(string Type, string Error, string Detail)
{
    /// <summary>
    /// Creates the 400 <c>ValidationError</c> body. Each failure appears as <c>Property: message</c>,
    /// or as the message alone when it has no property, and the failures are joined with <c>; </c>.
    /// </summary>
    /// <param name="failures">The property name and message of each validation failure.</param>
    /// <returns>The validation error body.</returns>
    public static ApiErrorResponse ValidationError(IEnumerable<(string Property, string Message)> failures) =>
        new("ValidationError", "Invalid input data", string.Join("; ", failures.Select(FormatFailure)));

    private static string FormatFailure((string Property, string Message) failure) =>
        string.IsNullOrEmpty(failure.Property) ? failure.Message : $"{failure.Property}: {failure.Message}";
}
```

- [ ] **Step 5: Write the middleware**

Create `src/Ambev.DeveloperEvaluation.WebApi/Middleware/ExceptionHandlingMiddleware.cs`:

```csharp
using System.Text.Encodings.Web;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.WebApi.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.WebApi.Middleware;

/// <summary>
/// Turns every exception that escapes the rest of the pipeline into a <c>{type, error, detail}</c> response,
/// following the error table in the Sales API design spec (§7.4).
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private const string ConcurrencyConflictDetail = "The sale was changed by another request. Reload it and try again.";
    private const string InternalServerErrorDetail = "An unexpected error occurred.";

    /// <summary>
    /// The web defaults (camelCase) with the relaxed escaping that MVC's JSON output uses, so error and success
    /// bodies escape text the same way: an apostrophe is written as it is, not as a Unicode escape sequence.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger that records unexpected exceptions.</param>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Runs the rest of the pipeline and, if it throws, writes the matching error response.
    /// Unexpected exceptions are logged with their stack trace; the response never includes it.
    /// </summary>
    /// <param name="context">The HTTP context of the current request.</param>
    /// <returns>A task that completes when the response has been written.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            var (statusCode, body) = ToErrorResponse(exception);

            if (statusCode == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
                    context.Request.Method, context.Request.Path);
            }

            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(body, JsonOptions);
        }
    }

    private static (int StatusCode, ApiErrorResponse Body) ToErrorResponse(Exception exception) => exception switch
    {
        ValidationException validation => (StatusCodes.Status400BadRequest,
            ApiErrorResponse.ValidationError(validation.Errors.Select(failure => (failure.PropertyName, failure.ErrorMessage)))),
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized,
            new ApiErrorResponse("AuthenticationError", "Authentication failed", exception.Message)),
        KeyNotFoundException => (StatusCodes.Status404NotFound,
            new ApiErrorResponse("ResourceNotFound", "Resource not found", exception.Message)),
        DomainException => (StatusCodes.Status409Conflict,
            new ApiErrorResponse("BusinessRuleViolation", "Business rule violation", exception.Message)),
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict,
            new ApiErrorResponse("ConcurrencyConflict", "Concurrent modification", ConcurrencyConflictDetail)),
        _ => (StatusCodes.Status500InternalServerError,
            new ApiErrorResponse("InternalServerError", "Internal server error", InternalServerErrorDetail))
    };
}
```

`HttpContext`, `RequestDelegate`, `StatusCodes` and `ILogger<T>` come from the Web SDK's implicit usings.

- [ ] **Step 6: Run the tests (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |Error\(s\)|Build succeeded'
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~ExceptionHandlingMiddlewareTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `Build succeeded.` and `0 Error(s)`; `Passed:     8, Skipped:     0, Total:     8`; `Passed:    57, Skipped:     0, Total:    57`.

- [ ] **Step 7: Put the new middleware in the pipeline and delete the old one**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.WebApi/Program.cs`, replace

```csharp
            app.UseMiddleware<ValidationExceptionMiddleware>();
```

with

```csharp
            app.UseMiddleware<ExceptionHandlingMiddleware>();
```

`Program.cs` already has `using Ambev.DeveloperEvaluation.WebApi.Middleware;`. The middleware stays first in the pipeline, right after `builder.Build()` (and after ticket 02's migration block, if that's merged). Then:

```bash
git rm -q src/Ambev.DeveloperEvaluation.WebApi/Middleware/ValidationExceptionMiddleware.cs
git grep -n "ValidationExceptionMiddleware" -- src tests || echo "no references left"
git diff --stat src/Ambev.DeveloperEvaluation.WebApi/Program.cs | tail -1
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |Error\(s\)|Build succeeded'
```

Expected: `no references left`, `1 file changed, 1 insertion(+), 1 deletion(-)`, and `Build succeeded.` with `0 Error(s)`.

- [ ] **Step 8: Run the HTTP check: the 500 is right, but nothing is logged**

```bash
bash /tmp/ticket03-checks/check-errors.sh | grep -E '^(ok|PROBLEM)|problems:'
grep -cE '\[(ERR|WRN|FTL)\]' /tmp/ticket03-checks/api.log
```

Expected:

```
PROBLEM  malformed JSON -> 400 ValidationError
PROBLEM  empty login body -> 400 from FluentValidation
PROBLEM  bad route value -> 400 ValidationError
PROBLEM  empty user id -> 400 from the request validator
ok       unexpected exception -> 500 without internals
ok       unknown route keeps the framework 404
ok       wrong method keeps the framework 405
ok       wrong content type keeps the framework 415
PROBLEM  the 500 is not logged with its stack trace
error-format problems: 5
0
```

The 500 now has the right body, but the log has no warning or error line at all, although the middleware called `LogError`. Task 4 fixes that.

- [ ] **Step 9: Commit and scan**

```bash
git add tests/Ambev.DeveloperEvaluation.Unit/Ambev.DeveloperEvaluation.Unit.csproj tests/Ambev.DeveloperEvaluation.Unit/WebApi/Middleware/ExceptionHandlingMiddlewareTests.cs src/Ambev.DeveloperEvaluation.WebApi/Common/ApiErrorResponse.cs src/Ambev.DeveloperEvaluation.WebApi/Middleware/ExceptionHandlingMiddleware.cs src/Ambev.DeveloperEvaluation.WebApi/Program.cs
git status --short | grep -v '^??'
git commit -m "feat(api): map exceptions to {type, error, detail} in a global middleware" -m "ExceptionHandlingMiddleware replaces ValidationExceptionMiddleware, which handled only ValidationException and answered with the {success, message, errors} shape. Every exception now becomes the {type, error, detail} body from .doc/general-api.md with the status codes of spec 7.4: ValidationException 400, UnauthorizedAccessException 401, KeyNotFoundException 404, DomainException and DbUpdateConcurrencyException 409, anything else 500. A 500 is logged with its exception, and its body never includes it. The Unit test project now references WebApi."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: the status lists `A` for the three new files, `D` for `ValidationExceptionMiddleware.cs` and `M` for `Program.cs` and the `.csproj`; then `6 files changed, 307 insertions(+), 51 deletions(-)` and `Scan complete: 0 issue(s) found`.

---

### Task 4: Let Serilog write warnings and errors

**Files:**
- Modify: `src/Ambev.DeveloperEvaluation.Common/Logging/LoggingExtension.cs` (line 34)

- [ ] **Step 1: Look at the filter (the RED is Task 3 Step 8)**

```bash
grep -n -B2 -A8 'exclusionPredicate.Level' src/Ambev.DeveloperEvaluation.Common/Logging/LoggingExtension.cs
grep -n 'Filter.ByExcluding' src/Ambev.DeveloperEvaluation.Common/Logging/LoggingExtension.cs
```

Expected: line 34 reads `if (exclusionPredicate.Level != LogEventLevel.Information) return true;`, and line 65 applies the predicate with `.Filter.ByExcluding(_filterPredicate);`. `ByExcluding` drops every event the predicate returns `true` for. So every non-Information event is dropped, which is why Task 3 Step 8 counted `0` warning and error lines. The rest of the predicate, which hides Information-level `/health` requests that returned 200, is what the filter is for.

- [ ] **Step 2: Keep non-Information events**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.Common/Logging/LoggingExtension.cs`, replace

```csharp
        if (exclusionPredicate.Level != LogEventLevel.Information) return true;
```

with

```csharp
        if (exclusionPredicate.Level != LogEventLevel.Information) return false;
```

- [ ] **Step 3: Verify (GREEN for the log line)**

```bash
git diff src/Ambev.DeveloperEvaluation.Common/Logging/LoggingExtension.cs | grep -E '^[+-][^+-]'
bash /tmp/ticket03-checks/check-errors.sh | grep -E 'logged|problems:'
grep -E '\[(ERR|WRN|FTL)\]' /tmp/ticket03-checks/api.log | cut -c31-160
grep -A1 '\[ERR\]' /tmp/ticket03-checks/api.log | tail -1
dotnet test tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected:

```
-        if (exclusionPredicate.Level != LogEventLevel.Information) return true;
+        if (exclusionPredicate.Level != LogEventLevel.Information) return false;
ok       the 500 is logged with its stack trace
error-format problems: 4
 [WRN] Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware Failed to determine the https port for redirect.
 [ERR] Ambev.DeveloperEvaluation.WebApi.Middleware.ExceptionHandlingMiddleware Unhandled exception while processing POST /api/auth
AutoMapper.AutoMapperMappingException: Missing type map configuration or unsupported mapping.
```

Then `Passed:    57`. The four remaining problems are the 400 lines, which Tasks 5 and 8 fix. The `/health` polling still logs nothing, so the exclusion still works.

- [ ] **Step 4: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Common/Logging/LoggingExtension.cs
git commit -m "fix(logging): stop the Serilog filter from dropping warnings and errors" -m "The filter meant to hide successful /health request logs returned true for every event that wasn't Information, and Filter.ByExcluding drops whatever the predicate matches. So every warning, error and fatal event was discarded, including the unhandled exceptions the new middleware logs. Non-Information events now pass; the /health exclusion is unchanged."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `1 file changed, 1 insertion(+), 1 deletion(-)` and `Scan complete: 0 issue(s) found`.

---

### Task 5: Return model-binding failures as `ValidationError`

**Skills:** `dotnet-best-practices`, `type-design-performance`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Unit/WebApi/MvcJson.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Common/InvalidModelStateResponseFactoryTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.WebApi/Common/InvalidModelStateResponseFactory.cs`
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Program.cs` (`AddControllers` and one `using`)

- [ ] **Step 1: Add the test helper that serializes like MVC**

Create `tests/Ambev.DeveloperEvaluation.Unit/WebApi/MvcJson.cs`. `SerializeToElement` is used from Task 7 on.

```csharp
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Ambev.DeveloperEvaluation.Unit.WebApi;

/// <summary>
/// Serializes values the way the API's MVC JSON output does: web defaults (camelCase) and relaxed escaping.
/// Tests use it to compare the exact body an action result would produce.
/// </summary>
internal static class MvcJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Serializes a value as its runtime type, as MVC does for an <c>ObjectResult</c>.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The JSON text.</returns>
    public static string Serialize(object? value) => JsonSerializer.Serialize(value, Options);

    /// <summary>
    /// Serializes a value as its runtime type into a <see cref="JsonElement"/>, for tests that check parts of a body.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The JSON element.</returns>
    public static JsonElement SerializeToElement(object? value) => JsonSerializer.SerializeToElement(value, Options);
}
```

- [ ] **Step 2: Write the failing tests**

Create `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Common/InvalidModelStateResponseFactoryTests.cs`. The first test adds `_page` before `$.quantity` and expects them sorted, so it fails without the sort.

```csharp
using Ambev.DeveloperEvaluation.WebApi.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Common;

/// <summary>
/// Contains unit tests for <see cref="InvalidModelStateResponseFactory"/>, which shapes model-binding failures
/// (malformed JSON, bad route or query values) as <c>{type, error, detail}</c>.
/// </summary>
public sealed class InvalidModelStateResponseFactoryTests
{
    /// <summary>
    /// Tests that every binding error is listed as <c>Key: message</c>, sorted by key and joined with <c>; </c>, in a JSON 400.
    /// </summary>
    [Fact(DisplayName = "Given binding errors When the factory builds the response Then it returns 400 ValidationError listing each as Key: message, sorted by key")]
    public void Given_BindingErrors_When_ResponseIsBuilt_Then_Returns400ValidationError()
    {
        // Given
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("_page", "The value 'abc' is not valid for _page.");
        modelState.AddModelError("$.quantity", "The JSON value could not be converted to System.Int32. Path: $.quantity | LineNumber: 0 | BytePositionInLine: 16.");

        // When
        var result = InvalidModelStateResponseFactory.Create(CreateActionContext(modelState));

        // Then
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        badRequest.ContentTypes.Should().Equal("application/json");
        MvcJson.Serialize(badRequest.Value).Should().Be(
            """{"type":"ValidationError","error":"Invalid input data","detail":"$.quantity: The JSON value could not be converted to System.Int32. Path: $.quantity | LineNumber: 0 | BytePositionInLine: 16.; _page: The value 'abc' is not valid for _page."}""");
    }

    /// <summary>
    /// Tests that an error that carries only an exception uses ASP.NET Core's generic message, never the exception's.
    /// </summary>
    [Fact(DisplayName = "Given a binding error without a message When the factory builds the response Then it uses the generic message")]
    public void Given_BindingErrorWithoutMessage_When_ResponseIsBuilt_Then_UsesGenericMessage()
    {
        // Given
        var modelState = new ModelStateDictionary();
        modelState.TryAddModelException("id", new FormatException("Internal parser detail"));

        // When
        var result = InvalidModelStateResponseFactory.Create(CreateActionContext(modelState));

        // Then
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        MvcJson.Serialize(badRequest.Value).Should().Be(
            """{"type":"ValidationError","error":"Invalid input data","detail":"id: The input was not valid."}""");
    }

    /// <summary>
    /// Tests that an error recorded against the whole request (empty key) is listed as its message alone.
    /// </summary>
    [Fact(DisplayName = "Given a binding error without a key When the factory builds the response Then the detail is the message alone")]
    public void Given_BindingErrorWithoutKey_When_ResponseIsBuilt_Then_DetailIsMessageAlone()
    {
        // Given
        var modelState = new ModelStateDictionary();
        modelState.AddModelError(string.Empty, "A non-empty request body is required.");

        // When
        var result = InvalidModelStateResponseFactory.Create(CreateActionContext(modelState));

        // Then
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        MvcJson.Serialize(badRequest.Value).Should().Be(
            """{"type":"ValidationError","error":"Invalid input data","detail":"A non-empty request body is required."}""");
    }

    private static ActionContext CreateActionContext(ModelStateDictionary modelState) =>
        new(new DefaultHttpContext(), new RouteData(), new ActionDescriptor(), modelState);
}
```

- [ ] **Step 3: Build and watch it fail (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected:

```
InvalidModelStateResponseFactoryTests.cs(30,22): error CS0103: The name 'InvalidModelStateResponseFactory' does not exist in the current context
InvalidModelStateResponseFactoryTests.cs(51,22): error CS0103: The name 'InvalidModelStateResponseFactory' does not exist in the current context
InvalidModelStateResponseFactoryTests.cs(70,22): error CS0103: The name 'InvalidModelStateResponseFactory' does not exist in the current context
```

- [ ] **Step 4: Write the factory**

Create `src/Ambev.DeveloperEvaluation.WebApi/Common/InvalidModelStateResponseFactory.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Common;

/// <summary>
/// Builds the response that <c>[ApiController]</c> returns when model binding fails (malformed JSON,
/// a bad route or query value), so those failures share the <c>{type, error, detail}</c> body of every other error.
/// </summary>
public static class InvalidModelStateResponseFactory
{
    /// <summary>ASP.NET Core's own text for a model error that carries only an exception.</summary>
    private const string GenericErrorMessage = "The input was not valid.";

    /// <summary>
    /// Creates a 400 <c>ValidationError</c> response that lists each model-state error as <c>Key: message</c>.
    /// Keys are sorted (ordinal), because <see cref="Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary"/>
    /// doesn't enumerate them in the order the errors were added.
    /// </summary>
    /// <param name="context">The action context whose model state is invalid.</param>
    /// <returns>A 400 result with an <see cref="ApiErrorResponse"/> body and the content type <c>application/json</c>.</returns>
    public static IActionResult Create(ActionContext context)
    {
        var failures = new List<(string Property, string Message)>();
        foreach (var (key, entry) in context.ModelState.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            if (entry is null)
                continue;

            foreach (var error in entry.Errors)
                failures.Add((key, string.IsNullOrEmpty(error.ErrorMessage) ? GenericErrorMessage : error.ErrorMessage));
        }

        return new BadRequestObjectResult(ApiErrorResponse.ValidationError(failures))
        {
            ContentTypes = { "application/json" }
        };
    }
}
```

`ContentTypes` pins `application/json`. Without it, MVC would negotiate `application/problem+json`, the default for `[ApiController]` 400s.

- [ ] **Step 5: Run the tests (GREEN)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -E ' error |Error\(s\)|Build succeeded'
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `Build succeeded.`, `0 Error(s)`, then `Passed:    60, Skipped:     0, Total:    60`.

- [ ] **Step 6: Wire the factory and the suppress flag into `Program.cs`**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.WebApi/Program.cs`, replace

```csharp
            builder.Services.AddControllers();
```

with

```csharp
            builder.Services
                .AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
                .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = InvalidModelStateResponseFactory.Create);
```

Then replace

```csharp
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.WebApi.Middleware;
```

with

```csharp
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Middleware;
```

`WebApiModuleInitializer` calls `AddControllers()` a second time with no options. That doesn't reset what is configured here, and spec §9.2 leaves the duplicate registration alone.

- [ ] **Step 7: Verify with the HTTP check**

```bash
git diff --stat src/Ambev.DeveloperEvaluation.WebApi/Program.cs | tail -1
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |Error\(s\)|Build succeeded'
bash /tmp/ticket03-checks/check-errors.sh | grep -E '^(ok|PROBLEM)|problems:'
```

Expected: `1 file changed, 4 insertions(+), 1 deletion(-)`, `Build succeeded.` with `0 Error(s)`, then:

```
ok       malformed JSON -> 400 ValidationError
PROBLEM  empty login body -> 400 from FluentValidation
ok       bad route value -> 400 ValidationError
PROBLEM  empty user id -> 400 from the request validator
ok       unexpected exception -> 500 without internals
ok       unknown route keeps the framework 404
ok       wrong method keeps the framework 405
ok       wrong content type keeps the framework 415
ok       the 500 is logged with its stack trace
error-format problems: 2
```

The two remaining problems are the controllers' raw FluentValidation lists (Task 8). With the suppress flag, `{}` now reaches the controller instead of failing MVC's implicit `[Required]` checks.

- [ ] **Step 8: Commit and scan**

```bash
git add tests/Ambev.DeveloperEvaluation.Unit/WebApi/MvcJson.cs tests/Ambev.DeveloperEvaluation.Unit/WebApi/Common/InvalidModelStateResponseFactoryTests.cs src/Ambev.DeveloperEvaluation.WebApi/Common/InvalidModelStateResponseFactory.cs src/Ambev.DeveloperEvaluation.WebApi/Program.cs
git commit -m "feat(api): return model-binding failures as ValidationError" -m "Malformed JSON and bad route or query values produced ASP.NET Core's ValidationProblemDetails. The [ApiController] invalid-model-state factory now returns 400 {type: ValidationError, error: Invalid input data, detail} with each binding error as Key: message. SuppressImplicitRequiredAttributeForNonNullableReferenceTypes is on, so MVC no longer treats every non-nullable string as required and FluentValidation is the only validator (spec D10)."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `4 files changed, 152 insertions(+), 1 deletion(-)` and `Scan complete: 0 issue(s) found`.

---

### Task 6: Register the Application validators so `ValidationBehavior` runs

**Skills:** `dependency-injection-patterns`, `dotnet-best-practices`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Unit/IoC/ValidationPipelineTests.cs`
- Modify: `src/Ambev.DeveloperEvaluation.IoC/Ambev.DeveloperEvaluation.IoC.csproj` (package)
- Modify: `src/Ambev.DeveloperEvaluation.IoC/ModuleInitializers/ApplicationModuleInitializer.cs`

- [ ] **Step 1: Write the failing test**

Create `tests/Ambev.DeveloperEvaluation.Unit/IoC/ValidationPipelineTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application;
using Ambev.DeveloperEvaluation.Application.Auth.AuthenticateUser;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.IoC.ModuleInitializers;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.IoC;

/// <summary>
/// Contains unit tests showing that the Application validators, registered by <see cref="ApplicationModuleInitializer"/>,
/// run in the MediatR pipeline through <see cref="ValidationBehavior{TRequest, TResponse}"/>.
/// </summary>
public sealed class ValidationPipelineTests
{
    /// <summary>
    /// Tests that an invalid command is rejected by <see cref="ValidationBehavior{TRequest, TResponse}"/> before its handler runs.
    /// <see cref="AuthenticateUserHandler"/> doesn't validate by itself, so only the behavior can throw the exception.
    /// </summary>
    [Fact(DisplayName = "Given an invalid login command When it is sent through MediatR Then ValidationBehavior throws ValidationException before the handler runs")]
    public async Task Given_InvalidLoginCommand_When_SentThroughMediatR_Then_ValidationBehaviorThrows()
    {
        // Given
        var userRepository = Substitute.For<IUserRepository>();
        await using var provider = BuildPipeline(userRepository);
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new AuthenticateUserCommand { Email = "not-an-email", Password = "123" };

        // When
        var act = () => mediator.Send(command);

        // Then
        var thrown = await act.Should().ThrowAsync<ValidationException>();
        thrown.Which.Errors.Select(failure => failure.PropertyName).Should().Equal("Email", "Password");
        await userRepository.DidNotReceive().GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Builds the MediatR pipeline the way <c>Program.cs</c> does, with the validators that the IoC module registers.
    /// </summary>
    private static ServiceProvider BuildPipeline(IUserRepository userRepository)
    {
        var builder = WebApplication.CreateBuilder();
        new ApplicationModuleInitializer().Initialize(builder);
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationLayer).Assembly));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        builder.Services.AddSingleton(userRepository);
        builder.Services.AddSingleton(Substitute.For<IJwtTokenGenerator>());
        return builder.Services.BuildServiceProvider();
    }
}
```

`AuthenticateUserValidator` rejects `not-an-email` (`EmailAddress`) and `123` (`MinimumLength(6)`), one failure each.

- [ ] **Step 2: Run it and watch it fail (RED)**

```bash
dotnet test tests/Ambev.DeveloperEvaluation.Unit --filter "FullyQualifiedName~ValidationPipelineTests" 2>&1 | grep -E 'Passed!|Failed!|Expected a' | cut -c1-140
```

Expected:

```
   Expected a <FluentValidation.ValidationException> to be thrown, but found <System.UnauthorizedAccessException>:
Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, …
```

With no validators registered, the behavior lets the command through, and the handler rejects the unknown user with "Invalid credentials". That's the template bug: the behavior is registered but never validates anything.

- [ ] **Step 3: Add the DI extensions package to the IoC project**

```bash
dotnet add src/Ambev.DeveloperEvaluation.IoC/Ambev.DeveloperEvaluation.IoC.csproj package FluentValidation.DependencyInjectionExtensions --version 11.10.0 2>&1 | grep -E 'info : PackageReference|error'
git diff src/Ambev.DeveloperEvaluation.IoC/Ambev.DeveloperEvaluation.IoC.csproj | grep -E '^[+-][^+-]'
```

Expected: `info : PackageReference for package 'FluentValidation.DependencyInjectionExtensions' version '11.10.0' added to file '…\Ambev.DeveloperEvaluation.IoC.csproj'.`, and one added line next to the Npgsql package:

```
+	  <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.10.0" />
```

11.10.0 matches the FluentValidation 11.10.0 that Common and Domain use. The local NuGet cache only has 11.11.0, so the restore downloads 11.10.0 from nuget.org.

- [ ] **Step 4: Register the validators**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.IoC/ModuleInitializers/ApplicationModuleInitializer.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Common.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Ambev.DeveloperEvaluation.IoC.ModuleInitializers;

public class ApplicationModuleInitializer : IModuleInitializer
{
    public void Initialize(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
    }
```

with

```csharp
using Ambev.DeveloperEvaluation.Application;
using Ambev.DeveloperEvaluation.Common.Security;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Ambev.DeveloperEvaluation.IoC.ModuleInitializers;

public class ApplicationModuleInitializer : IModuleInitializer
{
    public void Initialize(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        // ValidationBehavior resolves IValidator<TRequest> for every MediatR request.
        builder.Services.AddValidatorsFromAssembly(typeof(ApplicationLayer).Assembly);
    }
```

The IoC project already references Application, and `AddValidatorsFromAssembly` registers the validators as scoped.

- [ ] **Step 5: Verify (GREEN)**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |Error\(s\)|Build succeeded'
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~ValidationPipelineTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `Build succeeded.` with `0 Error(s)`; `Passed:     1`; `Passed:    61, Skipped:     0, Total:    61`.

- [ ] **Step 6: Commit and scan**

```bash
git add tests/Ambev.DeveloperEvaluation.Unit/IoC/ValidationPipelineTests.cs src/Ambev.DeveloperEvaluation.IoC/Ambev.DeveloperEvaluation.IoC.csproj src/Ambev.DeveloperEvaluation.IoC/ModuleInitializers/ApplicationModuleInitializer.cs
git commit -m "fix(ioc): register the Application validators so ValidationBehavior runs" -m "ValidationBehavior was registered in the MediatR pipeline, but no validators were registered in DI, so it never validated anything. ApplicationModuleInitializer now registers every validator in the Application assembly with FluentValidation.DependencyInjectionExtensions 11.10.0, so each MediatR request is validated before its handler runs."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `3 files changed, 66 insertions(+), 1 deletion(-)` and `Scan complete: 0 issue(s) found`.

---

### Task 7: Build the success envelope exactly once

**Skills:** `dotnet-best-practices`, `type-design-performance`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Unit/WebApi/TestData/UserRequestTestData.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Auth/AuthControllerTests.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Users/UsersControllerTests.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Common/BaseControllerTests.cs`
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Common/ApiResponse.cs`, `ApiResponseWithData.cs`, `BaseController.cs`
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Features/Auth/AuthController.cs`, `src/Ambev.DeveloperEvaluation.WebApi/Features/Users/UsersController.cs` (success returns only)

- [ ] **Step 1: Add valid request builders for the controller tests**

Create `tests/Ambev.DeveloperEvaluation.Unit/WebApi/TestData/UserRequestTestData.cs`:

```csharp
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using Ambev.DeveloperEvaluation.WebApi.Features.Auth.AuthenticateUserFeature;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.CreateUser;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.TestData;

/// <summary>
/// Generates valid request contracts for the Users and Auth controllers, reusing the Bogus rules of <see cref="UserTestData"/>.
/// </summary>
public static class UserRequestTestData
{
    /// <summary>
    /// Generates a <see cref="CreateUserRequest"/> that passes <see cref="CreateUserRequestValidator"/>.
    /// </summary>
    /// <returns>A valid sign-up request for an active customer.</returns>
    public static CreateUserRequest GenerateValidCreateUserRequest() => new()
    {
        Username = UserTestData.GenerateValidUsername(),
        Password = UserTestData.GenerateValidPassword(),
        Phone = UserTestData.GenerateValidPhone(),
        Email = UserTestData.GenerateValidEmail(),
        Status = UserStatus.Active,
        Role = UserRole.Customer
    };

    /// <summary>
    /// Generates an <see cref="AuthenticateUserRequest"/> that passes <see cref="AuthenticateUserRequestValidator"/>.
    /// </summary>
    /// <returns>A valid login request.</returns>
    public static AuthenticateUserRequest GenerateValidAuthenticateUserRequest() => new()
    {
        Email = UserTestData.GenerateValidEmail(),
        Password = UserTestData.GenerateValidPassword()
    };
}
```

- [ ] **Step 2: Write the controllers' success tests**

These show the bug the ticket describes, at the controller level. Task 8 adds the validation tests to the same two files.

Create `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Auth/AuthControllerTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Auth.AuthenticateUser;
using Ambev.DeveloperEvaluation.Unit.WebApi.TestData;
using Ambev.DeveloperEvaluation.WebApi.Features.Auth;
using Ambev.DeveloperEvaluation.WebApi.Features.Auth.AuthenticateUserFeature;
using AutoMapper;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Auth;

/// <summary>
/// Contains unit tests for the <see cref="AuthController"/> class.
/// </summary>
public sealed class AuthControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly AuthController _controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthControllerTests"/> class.
    /// </summary>
    public AuthControllerTests()
    {
        _controller = new AuthController(_mediator, _mapper);
    }

    /// <summary>
    /// Tests that a successful login returns the envelope once, with the token at <c>data.token</c>.
    /// </summary>
    [Fact(DisplayName = "Given valid credentials When authenticating Then it returns 200 with the token at data.token")]
    public async Task Given_ValidCredentials_When_Authenticating_Then_ReturnsTokenAtDataToken()
    {
        // Given
        var request = UserRequestTestData.GenerateValidAuthenticateUserRequest();
        var command = new AuthenticateUserCommand { Email = request.Email, Password = request.Password };
        var result = new AuthenticateUserResult { Token = "jwt-token", Email = "admin@example.com", Name = "admin", Role = "Admin" };
        _mapper.Map<AuthenticateUserCommand>(request).Returns(command);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(result);
        _mapper.Map<AuthenticateUserResponse>(result).Returns(new AuthenticateUserResponse
        {
            Token = result.Token,
            Email = result.Email,
            Name = result.Name,
            Role = result.Role
        });

        // When
        var response = await _controller.AuthenticateUser(request, CancellationToken.None);

        // Then
        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        MvcJson.Serialize(ok.Value).Should().Be(
            """{"success":true,"message":"User authenticated successfully","data":{"token":"jwt-token","email":"admin@example.com","name":"admin","role":"Admin"}}""");
    }
}
```

Create `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Users/UsersControllerTests.cs`. `CreateUserResponse` and `GetUserResponse` carry enums, whose JSON form ticket 04 changes to strings, so those two tests check the envelope's shape and the id rather than the exact text.

```csharp
using Ambev.DeveloperEvaluation.Application.Users.CreateUser;
using Ambev.DeveloperEvaluation.Application.Users.DeleteUser;
using Ambev.DeveloperEvaluation.Application.Users.GetUser;
using Ambev.DeveloperEvaluation.Unit.WebApi.TestData;
using Ambev.DeveloperEvaluation.WebApi.Features.Users;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.CreateUser;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.GetUser;
using AutoMapper;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Users;

/// <summary>
/// Contains unit tests for the <see cref="UsersController"/> class.
/// </summary>
public sealed class UsersControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly UsersController _controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="UsersControllerTests"/> class.
    /// </summary>
    public UsersControllerTests()
    {
        _controller = new UsersController(_mediator, _mapper);
    }

    /// <summary>
    /// Tests that a sign-up returns 201 pointing at <c>GetUser</c>, with the envelope built once.
    /// </summary>
    [Fact(DisplayName = "Given a valid sign-up request When creating a user Then it returns 201 at GetUser with {success, message, data}")]
    public async Task Given_ValidSignUpRequest_When_CreatingUser_Then_Returns201WithEnvelope()
    {
        // Given
        var request = UserRequestTestData.GenerateValidCreateUserRequest();
        var command = new CreateUserCommand();
        var result = new CreateUserResult { Id = Guid.NewGuid() };
        _mapper.Map<CreateUserCommand>(request).Returns(command);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(result);
        _mapper.Map<CreateUserResponse>(result).Returns(new CreateUserResponse { Id = result.Id });

        // When
        var response = await _controller.CreateUser(request, CancellationToken.None);

        // Then
        var created = response.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(UsersController.GetUser));
        created.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(result.Id);
        var body = MvcJson.SerializeToElement(created.Value);
        body.EnumerateObject().Select(property => property.Name).Should().Equal("success", "message", "data");
        body.GetProperty("message").GetString().Should().Be("User created successfully");
        body.GetProperty("data").GetProperty("id").GetGuid().Should().Be(result.Id);
    }

    /// <summary>
    /// Tests that getting a user returns 200 with the envelope built once.
    /// </summary>
    [Fact(DisplayName = "Given an existing user id When getting a user Then it returns 200 with {success, message, data}")]
    public async Task Given_ExistingUserId_When_GettingUser_Then_Returns200WithEnvelope()
    {
        // Given
        var id = Guid.NewGuid();
        var command = new GetUserCommand(id);
        var result = new GetUserResult { Id = id };
        _mapper.Map<GetUserCommand>(id).Returns(command);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(result);
        _mapper.Map<GetUserResponse>(result).Returns(new GetUserResponse { Id = id });

        // When
        var response = await _controller.GetUser(id, CancellationToken.None);

        // Then
        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        var body = MvcJson.SerializeToElement(ok.Value);
        body.EnumerateObject().Select(property => property.Name).Should().Equal("success", "message", "data");
        body.GetProperty("message").GetString().Should().Be("User retrieved successfully");
        body.GetProperty("data").GetProperty("id").GetGuid().Should().Be(id);
    }

    /// <summary>
    /// Tests that deleting a user returns 200 with <c>{success, message}</c> and nothing else.
    /// </summary>
    [Fact(DisplayName = "Given an existing user id When deleting a user Then it returns 200 with exactly {success, message}")]
    public async Task Given_ExistingUserId_When_DeletingUser_Then_Returns200WithSuccessMessage()
    {
        // Given
        var id = Guid.NewGuid();
        var command = new DeleteUserCommand(id);
        _mapper.Map<DeleteUserCommand>(id).Returns(command);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(new DeleteUserResponse { Success = true });

        // When
        var response = await _controller.DeleteUser(id, CancellationToken.None);

        // Then
        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        MvcJson.Serialize(ok.Value).Should().Be("""{"success":true,"message":"User deleted successfully"}""");
    }
}
```

- [ ] **Step 3: Run them and see the double wrap (RED)**

```bash
dotnet test tests/Ambev.DeveloperEvaluation.Unit --filter "FullyQualifiedName~ControllerTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-330
```

Expected: `Failed!  - Failed:     4, Passed:     0, Skipped:     0, Total:     4`, and these four failures (in any order):
- The login body is `{"data":{"data":{"token":"jwt-token",…},"success":true,"message":"User authenticated successfully","errors":[]},"success":true,"message":"","errors":[]}`. The token is at `data.data.token`, the bug the ticket describes.
- The delete body is `{"data":{"success":true,"message":"User deleted successfully","errors":[]},"success":true,"message":"","errors":[]}`.
- The get-user body's top-level properties are `{"data", "success", "message", "errors"}`, not `{"success", "message", "data"}`.
- The sign-up returns `CreatedResult`, not `CreatedAtActionResult`: no `Location`.

- [ ] **Step 4: Write the helper tests**

Create `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Common/BaseControllerTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.WebApi.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Common;

/// <summary>
/// Contains unit tests for the response helpers of <see cref="BaseController"/>. Each test serializes the
/// helper's result the way MVC does and checks the exact JSON, so an envelope can't be wrapped twice.
/// </summary>
public sealed class BaseControllerTests
{
    private readonly EnvelopeController _controller = new();

    /// <summary>
    /// Tests that <c>Ok(data, message)</c> builds <c>{success, message, data}</c> once.
    /// </summary>
    [Fact(DisplayName = "Given data and a message When Ok is called Then the body is exactly {success, message, data}")]
    public void Given_DataAndMessage_When_OkIsCalled_Then_BodyIsSuccessMessageData()
    {
        // Given
        var data = new { Token = "jwt-token" };

        // When
        var result = _controller.CallOk(data, "User authenticated successfully");

        // Then
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        MvcJson.Serialize(ok.Value).Should().Be(
            """{"success":true,"message":"User authenticated successfully","data":{"token":"jwt-token"}}""");
    }

    /// <summary>
    /// Tests that <c>Ok(message)</c> builds <c>{success, message}</c>, with no <c>data</c> or <c>errors</c>.
    /// </summary>
    [Fact(DisplayName = "Given only a message When Ok is called Then the body is exactly {success, message}")]
    public void Given_OnlyMessage_When_OkIsCalled_Then_BodyIsSuccessMessage()
    {
        // When
        var result = _controller.CallOk("User deleted successfully");

        // Then
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        MvcJson.Serialize(ok.Value).Should().Be("""{"success":true,"message":"User deleted successfully"}""");
    }

    /// <summary>
    /// Tests that <c>Created</c> returns 201 pointing at the given action, with <c>{success, message, data}</c>.
    /// </summary>
    [Fact(DisplayName = "Given data and a message When Created is called Then it returns 201 at the action with {success, message, data}")]
    public void Given_DataAndMessage_When_CreatedIsCalled_Then_Returns201WithEnvelope()
    {
        // Given
        var id = Guid.Parse("7f9c2a44-5555-4d1e-8a3b-000000000010");
        var data = new { Id = id };

        // When
        var result = _controller.CallCreated("GetSale", new { id }, data, "Sale created successfully");

        // Then
        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be("GetSale");
        created.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(id);
        MvcJson.Serialize(created.Value).Should().Be(
            """{"success":true,"message":"Sale created successfully","data":{"id":"7f9c2a44-5555-4d1e-8a3b-000000000010"}}""");
    }

    /// <summary>
    /// Tests that <c>OkPaginated</c> builds the paged envelope once: the items in <c>data</c> plus the paging fields.
    /// </summary>
    [Fact(DisplayName = "Given a page of items When OkPaginated is called Then the body is the paged envelope, not wrapped again")]
    public void Given_PageOfItems_When_OkPaginatedIsCalled_Then_BodyIsPagedEnvelope()
    {
        // Given
        var page = new PaginatedList<string>(["S-000003", "S-000004"], count: 5, pageNumber: 2, pageSize: 2);

        // When
        var result = _controller.CallOkPaginated(page, "Sales retrieved successfully");

        // Then
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        MvcJson.Serialize(ok.Value).Should().Be(
            """{"success":true,"message":"Sales retrieved successfully","data":["S-000003","S-000004"],"currentPage":2,"totalPages":3,"totalCount":5}""");
    }

    /// <summary>
    /// Exposes the protected helpers of <see cref="BaseController"/> to the tests.
    /// </summary>
    private sealed class EnvelopeController : BaseController
    {
        public IActionResult CallOk<T>(T data, string message) => Ok(data, message);

        public IActionResult CallOk(string message) => Ok(message);

        public IActionResult CallCreated<T>(string actionName, object routeValues, T data, string message) =>
            Created(actionName, routeValues, data, message);

        public IActionResult CallOkPaginated<T>(PaginatedList<T> pagedList, string message) =>
            OkPaginated(pagedList, message);
    }
}
```

- [ ] **Step 5: Build and watch it fail (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
```

Expected:

```
BaseControllerTests.cs(100,13): error CS1501: No overload for method 'OkPaginated' takes 2 arguments
BaseControllerTests.cs(92,67): error CS1501: No overload for method 'Ok' takes 2 arguments
BaseControllerTests.cs(97,13): error CS1501: No overload for method 'Created' takes 4 arguments
```

`CallOk(string message) => Ok(message)` compiles against the template, because the old `Ok<T>(T data)` accepts a string. It would wrap it, which the test catches once the file compiles.

- [ ] **Step 6: Drop the errors list and fix the property order**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.WebApi/Common/ApiResponse.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Common.Validation;

namespace Ambev.DeveloperEvaluation.WebApi.Common;

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public IEnumerable<ValidationErrorDetail> Errors { get; set; } = [];
}
```

with

```csharp
using System.Text.Json.Serialization;

namespace Ambev.DeveloperEvaluation.WebApi.Common;

public class ApiResponse
{
    // System.Text.Json writes a derived class's properties first; the orders keep success and message on top.
    [JsonPropertyOrder(-3)]
    public bool Success { get; set; }

    [JsonPropertyOrder(-2)]
    public string Message { get; set; } = string.Empty;
}
```

In `src/Ambev.DeveloperEvaluation.WebApi/Common/ApiResponseWithData.cs`, replace

```csharp
namespace Ambev.DeveloperEvaluation.WebApi.Common;

public class ApiResponseWithData<T> : ApiResponse
{
    public T? Data { get; set; }
}
```

with

```csharp
using System.Text.Json.Serialization;

namespace Ambev.DeveloperEvaluation.WebApi.Common;

public class ApiResponseWithData<T> : ApiResponse
{
    // After success and message, before the paging fields of PaginatedResponse<T>.
    [JsonPropertyOrder(-1)]
    public T? Data { get; set; }
}
```

- [ ] **Step 7: Rewrite the helpers**

In `src/Ambev.DeveloperEvaluation.WebApi/Common/BaseController.cs`, replace everything from `    protected IActionResult Ok<T>(T data) =>` to the end of the file (the `Ok`, `Created`, `BadRequest`, `NotFound` and `OkPaginated` helpers and the closing brace), which is

```csharp
    protected IActionResult Ok<T>(T data) =>
            base.Ok(new ApiResponseWithData<T> { Data = data, Success = true });

    protected IActionResult Created<T>(string routeName, object routeValues, T data) =>
        base.CreatedAtRoute(routeName, routeValues, new ApiResponseWithData<T> { Data = data, Success = true });

    protected IActionResult BadRequest(string message) =>
        base.BadRequest(new ApiResponse { Message = message, Success = false });

    protected IActionResult NotFound(string message = "Resource not found") =>
        base.NotFound(new ApiResponse { Message = message, Success = false });

    protected IActionResult OkPaginated<T>(PaginatedList<T> pagedList) =>
            Ok(new PaginatedResponse<T>
            {
                Data = pagedList,
                CurrentPage = pagedList.CurrentPage,
                TotalPages = pagedList.TotalPages,
                TotalCount = pagedList.TotalCount,
                Success = true
            });
}
```

with

```csharp
    /// <summary>
    /// Returns 200 with the body <c>{success, message, data}</c>.
    /// </summary>
    /// <typeparam name="T">The type of the response data.</typeparam>
    /// <param name="data">The response data. Pass the data itself, never an envelope.</param>
    /// <param name="message">The success message.</param>
    /// <returns>A 200 result whose body is built exactly once.</returns>
    protected IActionResult Ok<T>(T data, string message) =>
        base.Ok(new ApiResponseWithData<T> { Success = true, Message = message, Data = data });

    /// <summary>
    /// Returns 200 with the body <c>{success, message}</c>, for responses without data.
    /// </summary>
    /// <param name="message">The success message.</param>
    /// <returns>A 200 result whose body is built exactly once.</returns>
    protected IActionResult Ok(string message) =>
        base.Ok(new ApiResponse { Success = true, Message = message });

    /// <summary>
    /// Returns 201 with the body <c>{success, message, data}</c> and a <c>Location</c> header that points at
    /// <paramref name="actionName"/> in the same controller.
    /// </summary>
    /// <typeparam name="T">The type of the response data.</typeparam>
    /// <param name="actionName">The action that reads the created resource, such as <c>nameof(GetUser)</c>.</param>
    /// <param name="routeValues">The route values of that action, such as <c>new { id }</c>.</param>
    /// <param name="data">The response data. Pass the data itself, never an envelope.</param>
    /// <param name="message">The success message.</param>
    /// <returns>A 201 result whose body is built exactly once.</returns>
    protected IActionResult Created<T>(string actionName, object routeValues, T data, string message) =>
        base.CreatedAtAction(actionName, routeValues, new ApiResponseWithData<T> { Success = true, Message = message, Data = data });

    /// <summary>
    /// Returns 200 with the paged body: <c>{success, message, data}</c> plus <c>currentPage</c>, <c>totalPages</c> and <c>totalCount</c>.
    /// </summary>
    /// <typeparam name="T">The type of the items.</typeparam>
    /// <param name="pagedList">The page of items and its paging information.</param>
    /// <param name="message">The success message.</param>
    /// <returns>A 200 result whose body is built exactly once.</returns>
    protected IActionResult OkPaginated<T>(PaginatedList<T> pagedList, string message) =>
        base.Ok(new PaginatedResponse<T>
        {
            Success = true,
            Message = message,
            Data = pagedList,
            CurrentPage = pagedList.CurrentPage,
            TotalPages = pagedList.TotalPages,
            TotalCount = pagedList.TotalCount
        });
}
```

Leave `GetCurrentUserId` and `GetCurrentUserEmail` above them as they are (spec §9.2).

Watch for one trap in later controllers. A one-argument `Ok(someDto)` matches none of these helpers, so it compiles to `ControllerBase.Ok(object)` and returns the DTO with no envelope. Always pass the message. The controller tests catch it for every endpoint.

- [ ] **Step 8: Make the controllers pass the data, not an envelope**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.WebApi/Features/Auth/AuthController.cs`, replace

```csharp
        return Ok(new ApiResponseWithData<AuthenticateUserResponse>
        {
            Success = true,
            Message = "User authenticated successfully",
            Data = _mapper.Map<AuthenticateUserResponse>(response)
        });
```

with

```csharp
        return Ok(_mapper.Map<AuthenticateUserResponse>(response), "User authenticated successfully");
```

In `src/Ambev.DeveloperEvaluation.WebApi/Features/Users/UsersController.cs`, make three replacements. Replace

```csharp
        return Created(string.Empty, new ApiResponseWithData<CreateUserResponse>
        {
            Success = true,
            Message = "User created successfully",
            Data = _mapper.Map<CreateUserResponse>(response)
        });
```

with

```csharp
        return Created(nameof(GetUser), new { id = response.Id }, _mapper.Map<CreateUserResponse>(response), "User created successfully");
```

Replace

```csharp
        return Ok(new ApiResponseWithData<GetUserResponse>
        {
            Success = true,
            Message = "User retrieved successfully",
            Data = _mapper.Map<GetUserResponse>(response)
        });
```

with

```csharp
        return Ok(_mapper.Map<GetUserResponse>(response), "User retrieved successfully");
```

Replace

```csharp
        return Ok(new ApiResponse
        {
            Success = true,
            Message = "User deleted successfully"
        });
```

with

```csharp
        return Ok("User deleted successfully");
```

- [ ] **Step 9: Verify (GREEN)**

```bash
git diff --stat -- src | cat
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |Error\(s\)|Build succeeded'
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!|\[FAIL\]' | cut -c1-90
git grep -n -E 'IEnumerable<ValidationErrorDetail>|BadRequest\(string|NotFound\(string' -- src/Ambev.DeveloperEvaluation.WebApi || echo "no errors list or old error helpers left"
```

Expected:

```
 .../Common/ApiResponse.cs                          |  7 ++-
 .../Common/ApiResponseWithData.cs                  |  6 ++-
 .../Common/BaseController.cs                       | 63 +++++++++++++++-------
 .../Features/Auth/AuthController.cs                |  7 +--
 .../Features/Users/UsersController.cs              | 20 ++-----
 5 files changed, 59 insertions(+), 44 deletions(-)
```

Then `Build succeeded.` with `0 Error(s)`, `Passed:    69, Skipped:     0, Total:    69`, and `no errors list or old error helpers left`. `ValidationErrorDetail` itself stays in Common: `User`, `CreateUserCommand` and `BaseEntity` still use it.

- [ ] **Step 10: Commit and scan**

```bash
git add tests/Ambev.DeveloperEvaluation.Unit/WebApi/TestData/UserRequestTestData.cs tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Auth/AuthControllerTests.cs tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Users/UsersControllerTests.cs tests/Ambev.DeveloperEvaluation.Unit/WebApi/Common/BaseControllerTests.cs src/Ambev.DeveloperEvaluation.WebApi/Common/ApiResponse.cs src/Ambev.DeveloperEvaluation.WebApi/Common/ApiResponseWithData.cs src/Ambev.DeveloperEvaluation.WebApi/Common/BaseController.cs src/Ambev.DeveloperEvaluation.WebApi/Features/Auth/AuthController.cs src/Ambev.DeveloperEvaluation.WebApi/Features/Users/UsersController.cs
git commit -m "fix(api): build the success envelope exactly once" -m "BaseController.Ok<T> wrapped any value in ApiResponseWithData<T>, and the controllers passed it envelopes they had already built, so the login token landed at data.data.token. OkPaginated had the same problem. The helpers now take the data and the message and build {success, message, data}, {success, message} or the paged shape once. OkPaginated calls base.Ok, Created points at an action, so POST /api/users now sends a Location header, and the unused BadRequest and NotFound helpers and the empty errors list are gone. The Users and Auth controllers use the helpers."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `9 files changed, 361 insertions(+), 44 deletions(-)` and `Scan complete: 0 issue(s) found`.

---

### Task 8: Throw `ValidationException` from the Users and Auth controllers

**Skill:** `dotnet-best-practices`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Auth/AuthControllerTests.cs`, `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Users/UsersControllerTests.cs`
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Features/Auth/AuthController.cs`, `src/Ambev.DeveloperEvaluation.WebApi/Features/Users/UsersController.cs`

- [ ] **Step 1: Add the failing validation tests**

With the Edit tool, in `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Auth/AuthControllerTests.cs`, replace

```csharp
using FluentAssertions;
using MediatR;
```

with

```csharp
using FluentAssertions;
using FluentValidation;
using MediatR;
```

Then insert this test before the success test, by replacing

```csharp
    /// <summary>
    /// Tests that a successful login returns the envelope once, with the token at <c>data.token</c>.
```

with

```csharp
    /// <summary>
    /// Tests that an invalid login request throws <see cref="ValidationException"/> instead of returning a raw error list.
    /// </summary>
    [Fact(DisplayName = "Given an invalid login request When authenticating Then it throws ValidationException and sends no command")]
    public async Task Given_InvalidLoginRequest_When_Authenticating_Then_ThrowsValidationException()
    {
        // Given
        var request = new AuthenticateUserRequest { Email = "not-an-email", Password = string.Empty };

        // When
        var act = () => _controller.AuthenticateUser(request, CancellationToken.None);

        // Then
        var thrown = await act.Should().ThrowAsync<ValidationException>();
        thrown.Which.Errors.Select(failure => failure.ErrorMessage).Should().Equal("Invalid email format", "Password is required");
        _mediator.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// Tests that a successful login returns the envelope once, with the token at <c>data.token</c>.
```

In `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Users/UsersControllerTests.cs`, replace

```csharp
using FluentAssertions;
using MediatR;
```

with

```csharp
using FluentAssertions;
using FluentValidation;
using MediatR;
```

Then insert three tests, each before the success test of the same action. Replace

```csharp
    /// <summary>
    /// Tests that a sign-up returns 201 pointing at <c>GetUser</c>, with the envelope built once.
```

with

```csharp
    /// <summary>
    /// Tests that an invalid sign-up request throws <see cref="ValidationException"/> instead of returning a raw error list.
    /// </summary>
    [Fact(DisplayName = "Given an invalid sign-up request When creating a user Then it throws ValidationException and sends no command")]
    public async Task Given_InvalidSignUpRequest_When_CreatingUser_Then_ThrowsValidationException()
    {
        // Given
        var request = UserRequestTestData.GenerateValidCreateUserRequest();
        request.Email = "not-an-email";

        // When
        var act = () => _controller.CreateUser(request, CancellationToken.None);

        // Then
        var thrown = await act.Should().ThrowAsync<ValidationException>();
        thrown.Which.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be("The provided email address is not valid.");
        _mediator.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// Tests that a sign-up returns 201 pointing at <c>GetUser</c>, with the envelope built once.
```

Replace

```csharp
    /// <summary>
    /// Tests that getting a user returns 200 with the envelope built once.
```

with

```csharp
    /// <summary>
    /// Tests that an empty user id throws <see cref="ValidationException"/> instead of returning a raw error list.
    /// </summary>
    [Fact(DisplayName = "Given an empty user id When getting a user Then it throws ValidationException and sends no command")]
    public async Task Given_EmptyUserId_When_GettingUser_Then_ThrowsValidationException()
    {
        // When
        var act = () => _controller.GetUser(Guid.Empty, CancellationToken.None);

        // Then
        var thrown = await act.Should().ThrowAsync<ValidationException>();
        thrown.Which.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be("User ID is required");
        _mediator.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// Tests that getting a user returns 200 with the envelope built once.
```

Replace

```csharp
    /// <summary>
    /// Tests that deleting a user returns 200 with <c>{success, message}</c> and nothing else.
```

with

```csharp
    /// <summary>
    /// Tests that an empty user id throws <see cref="ValidationException"/> instead of returning a raw error list.
    /// </summary>
    [Fact(DisplayName = "Given an empty user id When deleting a user Then it throws ValidationException and sends no command")]
    public async Task Given_EmptyUserId_When_DeletingUser_Then_ThrowsValidationException()
    {
        // When
        var act = () => _controller.DeleteUser(Guid.Empty, CancellationToken.None);

        // Then
        var thrown = await act.Should().ThrowAsync<ValidationException>();
        thrown.Which.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be("User ID is required");
        _mediator.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// Tests that deleting a user returns 200 with <c>{success, message}</c> and nothing else.
```

- [ ] **Step 2: Run them and watch them fail (RED)**

```bash
dotnet test tests/Ambev.DeveloperEvaluation.Unit --filter "FullyQualifiedName~ControllerTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-120
```

Expected: four copies of `   Expected a <FluentValidation.ValidationException> to be thrown, but no exception was thrown.`, then `Failed!  - Failed:     4, Passed:     8, Skipped:     0, Total:    12`. The controllers still `return BadRequest(validationResult.Errors)`.

- [ ] **Step 3: Throw instead of returning the raw list**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.WebApi/Features/Auth/AuthController.cs`:
- Replace `using AutoMapper;` + newline + `using Ambev.DeveloperEvaluation.WebApi.Common;` with the same two lines and `using FluentValidation;` between them:

  ```csharp
  using AutoMapper;
  using FluentValidation;
  using Ambev.DeveloperEvaluation.WebApi.Common;
  ```

- Replace `            return BadRequest(validationResult.Errors);` with `            throw new ValidationException(validationResult.Errors);`.
- Replace

  ```csharp
      [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
      [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
  ```

  with

  ```csharp
      [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
      [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
  ```

In `src/Ambev.DeveloperEvaluation.WebApi/Features/Users/UsersController.cs`:
- Add `using FluentValidation;` the same way, between `using AutoMapper;` and `using Ambev.DeveloperEvaluation.WebApi.Common;`.
- With `replace_all`, replace `            return BadRequest(validationResult.Errors);` with `            throw new ValidationException(validationResult.Errors);` (three places).
- With `replace_all`, replace `    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]` with `    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]` (three places).
- With `replace_all`, replace `    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]` with `    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]` (two places).

`DeleteUser`'s `[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]` stays: its success body is `{success, message}`.

- [ ] **Step 4: Verify (GREEN)**

```bash
git diff -- src | grep -E '^[+-][^+-]' | sort | uniq -c
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |Error\(s\)|Build succeeded'
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!|\[FAIL\]' | cut -c1-90
git grep -n -E 'return BadRequest\(|typeof\(ApiResponse\), StatusCodes.Status(4|5)' -- src || echo "no raw error lists left"
bash /tmp/ticket03-checks/check-errors.sh | grep -E '^(ok|PROBLEM)|problems:'
```

Expected:
- The diff lines are exactly these (counted):

  ```
        4 -            return BadRequest(validationResult.Errors);
        4 -    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        1 -    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        2 -    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        4 +            throw new ValidationException(validationResult.Errors);
        4 +    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        1 +    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        2 +    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        2 +using FluentValidation;
  ```

  The 400 attribute and the `throw` appear four times each: once in `AuthController` and three times in `UsersController`.

- `Build succeeded.` with `0 Error(s)`, and `Passed:    73, Skipped:     0, Total:    73`.
- `no raw error lists left`.
- Every HTTP check line is `ok`, ending in `error-format problems: 0`.

- [ ] **Step 5: Commit and scan**

```bash
git add tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Auth/AuthControllerTests.cs tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Users/UsersControllerTests.cs src/Ambev.DeveloperEvaluation.WebApi/Features/Auth/AuthController.cs src/Ambev.DeveloperEvaluation.WebApi/Features/Users/UsersController.cs
git commit -m "fix(api): throw ValidationException from the Users and Auth controllers" -m "The controllers returned FluentValidation's raw failure list with a 400. They now throw ValidationException, so the middleware answers with the same {type, error, detail} body as every other error, and Swagger documents that body for their error statuses."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `4 files changed, 82 insertions(+), 11 deletions(-)` and `Scan complete: 0 issue(s) found`.

---

### Task 9: Verify everything, review, then open a pull request into `develop`

- [ ] **Step 1: Re-run every check fresh (superpowers:verification-before-completion)**

```bash
git status --short
dotnet build Ambev.DeveloperEvaluation.sln --no-incremental 2>&1 | grep -E 'Warning\(s\)|Error\(s\)|Build succeeded'
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
bash /tmp/ticket03-checks/check-errors.sh | tail -1
slopwatch analyze --fail-on warning --stats 2>&1 | tail -3
git grep -n -E 'ValidationExceptionMiddleware|return BadRequest\(|IEnumerable<ValidationErrorDetail>' -- src/Ambev.DeveloperEvaluation.WebApi || echo "old error shape gone"
git grep -n 'AddValidatorsFromAssembly\|SuppressImplicitRequiredAttributeForNonNullableReferenceTypes\|InvalidModelStateResponseFactory.Create\|UseMiddleware' -- src | cut -c1-150
```

Expected:
- `git status --short` lists only `?? docs/superpowers/tickets/` (plus any other tickets' untracked plan files).
- `Build succeeded.`, `3 Warning(s)`, `0 Error(s)`.
- `Passed:    73`, and `error-format problems: 0`.
- `Scan complete: 0 issue(s) found`, with about 110 files analyzed.
- `old error shape gone`.
- Four wiring lines: `ApplicationModuleInitializer.cs` (`AddValidatorsFromAssembly`), then three in `Program.cs` (`SuppressImplicitRequired…`, `InvalidModelStateResponseFactory.Create`, `UseMiddleware<ExceptionHandlingMiddleware>`).

Each ticket criterion and its evidence:

| Ticket criterion | Evidence |
|---|---|
| Middleware maps the §7.4 table (400, 401, 404, 409, 409, 500; the 500 logged with its stack trace and never in the body) | `ExceptionHandlingMiddlewareTests` (8 tests), plus the HTTP check's 500 and log lines (Tasks 3 and 4) |
| Error bodies are camelCase JSON with content type `application/json` | exact bodies and the `application/json; charset=utf-8` assertion in the middleware tests; `ContentTypes` in the factory tests; the HTTP check compares the media type |
| Model-binding failure → 400 `ValidationError` through the `[ApiController]` factory | `InvalidModelStateResponseFactoryTests`, plus the HTTP check's malformed-JSON and bad-route lines |
| Validators registered with `FluentValidation.DependencyInjectionExtensions` 11.10.0; a test shows `ValidationBehavior` throwing through MediatR | Task 6: the package line and `ValidationPipelineTests` |
| `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes` is `true`; optional contract fields nullable | the wiring grep above; the malformed-JSON detail has no `request: The request field is required.`; no contract has optional fields (Decision 6) |
| Users and Auth throw `ValidationException`; no endpoint returns `{success: false, message, errors}` | the four controller validation tests; `old error shape gone`; the HTTP check's two validator lines |
| Success bodies exactly `{success, message, data}` / `{success, message}` / paged, with no `errors` | `BaseControllerTests` and the controller success tests (exact JSON or exact property names) |
| Helpers build the envelope once and take the message; `OkPaginated` calls `base.Ok`; Users and Auth use them; the token at `data.token`; tests serialize each helper's result | `BaseControllerTests` (4) and `AuthControllerTests`' `data.token` test; Task 7 Step 3 showed the old `data.data.token` |
| `DomainException` in `Ambev.DeveloperEvaluation.Domain.Exceptions` | Task 2 Step 3; the middleware tests import it from there |
| Unit tests cover every row, the factory and the controllers throwing; the Unit project references WebApi | the classes above; Task 3 Step 1 |
| The framework's 404, 405 and 415 keep ASP.NET Core's defaults | the HTTP check's three framework lines. The README line is ticket 13's (Decision 15) |
| `feature/error-format`, pull request into `develop`, Conventional Commits, no attribution | Steps 5–7 |

- [ ] **Step 2: Run the whole solution if ticket 02 is merged and Docker is up (optional)**

Skip this step if Task 1 Step 1 didn't list `feature/postgres-runtime`, or if `docker version` fails. Otherwise:

```bash
dotnet test Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-100
```

Expected: three `Passed!` lines. Unit shows `Passed:    73`; Integration and Functional show ticket 02's counts, with nothing failed. This ticket changes nothing they depend on, but they host the real `Program`.

- [ ] **Step 3: Audit the new tests (skill `test-anti-patterns`, report only)**

Run the skill on the six new test classes (`ExceptionHandlingMiddlewareTests`, `InvalidModelStateResponseFactoryTests`, `BaseControllerTests`, `AuthControllerTests`, `UsersControllerTests`, `ValidationPipelineTests`) and the two helpers (`MvcJson`, `UserRequestTestData`). The rehearsal pre-check found nothing Critical or High. Expect at most the unseeded-Bogus remark (see the Skills table), and report it without changing anything. If the skill finds a real problem, fix it in a separate `test: …` commit, then re-run Step 1.

- [ ] **Step 4: Optional code review**

`superpowers:requesting-code-review` against `develop..feature/error-format`, with `clean-code` and `dotnet-best-practices` as the lenses, if you want a second look before the pull request.

- [ ] **Step 5: Check the commit messages**

```bash
git log --format=%s develop..feature/error-format
git log --format=%B develop..feature/error-format | grep -n -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
```

Expected (plus any `test:` commit from Step 3):

```
fix(api): throw ValidationException from the Users and Auth controllers
fix(api): build the success envelope exactly once
fix(ioc): register the Application validators so ValidationBehavior runs
feat(api): return model-binding failures as ValidationError
fix(logging): stop the Serilog filter from dropping warnings and errors
feat(api): map exceptions to {type, error, detail} in a global middleware
refactor(domain): move DomainException into the Domain.Exceptions namespace
no attribution lines
```

- [ ] **Step 6: Push the branch and open a pull request into `develop`**

This is the superpowers:finishing-a-development-branch step, with the option already fixed: a pull request into `develop` for the user to review. Don't merge it yourself.

Write the pull request body with the Write tool to `/tmp/ticket03-checks/pr-body.md`: two or three sentences on what the ticket delivers (the Goal above), the evidence table from Step 1 with this run's results, and what Steps 2 and 3 reported. No `Co-Authored-By` trailer, no "Generated with" line and no session link. Then:

```bash
git push -u origin feature/error-format
gh pr create --base develop --head feature/error-format --title "Ticket 03: one error format" --body-file /tmp/ticket03-checks/pr-body.md
```

Expected: the push prints `* [new branch]      feature/error-format -> feature/error-format`, and `gh pr create` prints the pull request URL, `https://github.com/PR001-git/developer-store-api/pull/<n>`. If `gh` fails (for example, it isn't logged in; check with `gh auth status`), stop and tell the user: the branch is pushed, so they can open the pull request on GitHub.

- [ ] **Step 7: Verify the pull request**

```bash
gh pr view feature/error-format --json state,baseRefName,commits --jq '"\(.state) \(.baseRefName) \(.commits | length) commits"'
gh pr view feature/error-format --json title,body --jq '.title + "\n" + .body' | grep -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
git diff --shortstat develop...feature/error-format
git status -sb | head -1
git status --short
```

Expected: `OPEN develop 7 commits` (8 if Step 3 added a `test:` commit), `no attribution lines`, ` 23 files changed, 971 insertions(+), 109 deletions(-)` (more with a `test:` commit), `## feature/error-format...origin/feature/error-format` with no `ahead` or `behind`, and only `?? docs/superpowers/tickets/` (plus any other tickets' untracked plan files).

Give the user the pull request URL in the Step 9 report. Don't merge it: the user reviews it and merges it on GitHub with **Create a merge commit** (the `--no-ff` equivalent that the later plans' starting-point checks look for). Keep `feature/error-format`, since the ticket doesn't ask to delete it.

- [ ] **Step 8: Clean up**

```bash
tasklist | grep -i 'Ambev.DeveloperEvaluation.WebApi' || echo "no API process left"
rm -rf /tmp/ticket03-checks
```

Expected: `no API process left`, and `rm` prints nothing.

- [ ] **Step 9: Report to the user**

Besides the results, tell the user:
1. **The Serilog filter fix (Decision 4)** is a template bug found while planning this ticket. Before it, no warning or error was ever logged. Ticket 13's README list of review findings should include it, next to "responses wrapped twice (ticket 03)".
2. **`POST /api/users` now returns `Location: /api/Users/{id}`** (Decision 11), and the success bodies keep the order `success, message, data` (Decision 10).
3. **If ticket 02 hasn't run yet,** its plan needs the three adjustments listed in "Order with ticket 02".
4. **For ticket 05's author:** `Created(nameof(GetSale), new { id }, data, message)` gives `Location: /api/sales/{id}`. A one-argument `Ok(dto)` compiles but skips the envelope (Task 7 Step 7).
