# Sign Up and Log In for a JWT (Ticket 04) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A client creates an active user with `POST /api/users` (status and role sent as strings), logs in with `POST /api/auth` and gets a JWT at `data.token` for the Sales endpoints. The sign-up response shows what was saved, a duplicate email is a 409, `GET /api/users/{id}` works, a missing `Jwt:SecretKey` stops startup with a clear error, Swagger's **Authorize** button accepts the token, and the `.http` file walks through the flow. Delivered on `feature/auth-login`, as a pull request into `develop` for the user to review.

**Architecture:** Small, test-first fixes to the template's Users and Auth features. The missing AutoMapper maps go into the existing profiles. `CreateUserResult` gains the saved user's fields, and `CreateUserHandler` throws `DomainException` for a taken email, so ticket 03's middleware answers 409. MVC's JSON options get `JsonStringEnumConverter`. A small internal `JwtSecretKey` reader gives startup and token generation the same configuration error. A static `SwaggerSecurity.AddJwtBearer` declares the Bearer scheme. Unit tests cover the maps, the handler and the JWT configuration. Functional tests go through ticket 02's `ApiFixture`, which starts one PostgreSQL 13 container per run, and cover sign-up, login, get-user and the Swagger document over HTTP.

**Tech Stack:** .NET SDK 10.0.200-preview building the `net8.0` projects, with tests on the installed 8.0.23 runtime; ASP.NET Core 8 MVC with System.Text.Json; AutoMapper 13.0.1; MediatR 12.4.1; FluentValidation 11.10.0; Microsoft.AspNetCore.Authentication.JwtBearer 8.0.10 (System.IdentityModel.Tokens.Jwt); Swashbuckle.AspNetCore 6.8.1 (Microsoft.OpenApi 1.6); xUnit 2.9.2, FluentAssertions 6.12.0, NSubstitute 5.1.0, Bogus 35.6.1; Microsoft.AspNetCore.Mvc.Testing 8.0.31 and Testcontainers.PostgreSql 4.15.0 (from ticket 02); Docker Desktop (WSL2) with Compose v2; curl 8.18 and Python 3.14 from Git Bash; Slopwatch.Cmd 0.4.2 (global tool).

**Source:** `docs/superpowers/tickets/04-sign-up-and-log-in-for-a-jwt.md`; Sales API design spec `docs/superpowers/specs/2026-09-24-sales-api-design.md`, D13, §7.2 (JSON), §7.4 (401 and 409 rows), §7.5, and §9.1 items 2, 3, 11 (the JWT key part), 12 (the users and auth part) and 13. The ticket also fixes two problems found in the ticket review, not in the spec: a duplicate sign-up returning 500, and the sign-up response that is empty apart from the id.

**Rehearsed:** on 2026-09-26, in a scratch copy of the template with ticket 03 applied in full and ticket 02 applied as far as this ticket touches it (the `Program.cs` rethrow and startup migrations, the functional and integration test projects with their packages and fixtures), both taken from their plans. The starting point matched those plans: `3 Warning(s)`, 73 unit tests, and a passing `StartupFailureTests`. Then Tasks 2–9 ran in this plan's order: every RED, build, commit and `slopwatch` scan. The outputs below come from that run. Docker still isn't installed on this machine, so three markers show how far each expected output was checked:
- **No marker:** rehearsed exactly as written.
- **(probe):** a functional test run against a stand-in `ApiFixture`: the same `WebApplicationFactory<Program>`, but in Production (no startup migrations) and with an in-memory `IUserRepository` instead of PostgreSQL. The HTTP answers go through the same controllers, maps, handlers and middleware, but the run against the real container hasn't happened. The Swagger document was checked the same way, through Swashbuckle's `ISwaggerProvider` instead of the `/swagger/v1/swagger.json` route, which only exists in Development.
- **(derived):** not run at all, for example `docker compose up` and the whole-solution test counts. It's worked out from the code and the rehearsed equivalents.

If a (probe) or (derived) output differs, use superpowers:systematic-debugging before changing any code.

---

## Rules for every agent executing this plan

Restate these to every subagent you dispatch.

- **No AI attribution anywhere.** No `Co-Authored-By` trailer, no "Generated with" line, no session link in commits, merge commits, tags or pull requests. Use the commit messages below exactly as written. `.claude/settings.local.json` switches the defaults off, but don't rely on it.
- **Git Bash only.** Run every command in Git Bash (Claude Code's Bash tool), from the repo root `C:\Users\pr000\orca\developer-store-api` (`/c/Users/pr000/orca/developer-store-api`). Each Bash call starts a fresh shell, so don't rely on variables, functions or `cd` from an earlier call. Commands below use paths relative to the root.
- **Write file content with the Write and Edit tools**, not with shell heredocs or `sed`. The Edit tool keeps the UTF-8 BOM and CRLF line endings of the template files (rehearsed on `CreateUserResult.cs`, `CreateUserHandler.cs`, `AuthenticationExtension.cs` and `JwtTokenGenerator.cs`). New files come out with LF endings; git converts them on add. The Write tool refuses to overwrite a file you haven't read in this session, so Read the `.http` file before Task 9 replaces it.
- **Work in the main checkout, never in a worktree.** `.claude/` is untracked, so it exists only here. It holds `settings.local.json` (the attribution guard) and the project skills. A worktree has neither.
- **Stage explicit paths only.** Never use `git add -A`, `git add .` or `git commit -a`. `docs/superpowers/tickets/` and other tickets' untracked plans must stay out of these commits.
- **Never run `git clean`.** `.git/info/exclude` makes `.claude/`, `.slopwatch/` and the two personal `.doc` files *ignored* files, and `git clean -x` or `-X` deletes ignored files.
- **Push only for the pull request.** Task 1 pushes `develop` with the plan commit, and Task 10 pushes `feature/auth-login` and opens the pull request. Never push `main`, never force-push, and never merge the pull request: the user reviews and merges it on GitHub.
- **Leave `.doc/brainstorm-show-case.md` and `.doc/template-code-review.md` alone.** They are the user's untracked personal notes. Don't open, edit, move or stage them.
- **Docker from Task 3 on.** Task 2 needs no Docker. Every later task runs functional tests against a PostgreSQL container. Don't install or configure Docker, WSL or any other system software. If Docker isn't running at the gate after Task 2, stop there (see the gate).
- **Always build before `dotnet test --no-build`.** A failed build leaves the previous DLL in place, and `--no-build` then runs stale tests (the rehearsal hit this once).
- **Stop everything you start.** Take compose stacks down (`docker compose down -v`) before a task ends. If you ever start the API yourself, stop it with `taskkill //F //IM Ambev.DeveloperEvaluation.WebApi.exe`, because a running API locks the build output and holds its port.
- **Nothing else changes.** Don't fix the pre-existing issues listed in spec §9.2, even when they show up here: the duplicate `CreateUserRequest → CreateUserCommand` map, the second `IJwtTokenGenerator` registration in `AddJwtAuthentication`, the anonymous Users endpoints, the unused `User → AuthenticateUserResponse` and `User → AuthenticateUserResult` maps, and `GetCurrentUserId`. Also leave these alone: the controllers and `BaseController` (ticket 03 finished them), the Docker and compose files, `appsettings*.json`, both `launchSettings.json` files, the ORM (no migration, no unique index; see Decision 5), the integration tests, ticket 02's `ApiFixture` and `DataResetFixture`, and `README.md` (ticket 13).

## Skills

### Project skills in `.claude/skills/`

Every skill folder in `.claude/skills/` is listed, with whether and when to use it.

| Skill | Use it? | When and how |
|---|---|---|
| `dotnet-best-practices` | **Yes, every task that writes C# (2–8)** | Load it before Task 2. The code below follows it: XML docs on public types and members, the `Ambev.DeveloperEvaluation.{Layer}.{Feature}` namespaces, xUnit with FluentAssertions, NSubstitute and Bogus, `Given … When … Then …` display names with `// Given`, `// When`, `// Then` sections, `Given_X_When_Y_Then_Z` method names, regular constructors (`.editorconfig` turns off primary constructors). Its error rule is the heart of Task 5: signal an expected failure by throwing `DomainException` and let the middleware answer. Two bullets are deliberately not applied, as in ticket 03 (Decision 13 there): `ArgumentNullException` constructor guards and `ConfigureAwait(false)`. Its "strongly-typed configuration classes" bullet isn't applied either (Decision 1). Use it to review what you write. |
| `testcontainers-integration-tests` | **Yes, Tasks 3–8** | Load it before Task 3. Functional tests reuse ticket 02's setup unchanged: `[Collection(ApiCollection.Name)]`, one `postgres:13` container and one `WebApplicationFactory<Program>` per run, the schema from the migrations, and a data reset after each test class. Don't add fixtures, containers or Respawn. Tests in one class share the database, so every sign-up uses a unique email (Decision 7). |
| `dotnet-slopwatch` | **Yes, after every commit and in Task 10** | Run `slopwatch analyze --fail-on warning` and expect `Scan complete: 0 issue(s) found` (rehearsed after every task). Its project note: slopwatch is a global tool, `.slopwatch/` stays git-excluded, and the baseline comes from ticket 01. Never update the baseline to make a finding go away. |
| `test-anti-patterns` | **Yes, Task 10 (report only)** | Before opening the pull request, audit the new test classes and helpers listed in Task 10. The skill loads `test-analysis-extensions` by itself. Expected, deliberate remarks are listed there. Report them without changing anything, and fix only real findings, in a separate `test: …` commit. |
| `type-design-performance` | Light, Tasks 2, 3 and 8 | New classes are `sealed` or `static`: `JwtSecretKey` (`internal static`), `SwaggerSecurity` (`static`), `SignUpRequest` (`sealed record`), and every test class. `CreateUserResult` stays an unsealed class like the template's other result DTOs. Nothing here is a struct. |
| `dependency-injection-patterns` | Light, Tasks 3 and 8 | Its project note says new registrations go into the matching `IModuleInitializer`. This ticket adds none: it configures two registrations that already live in `Program.cs`, `AddControllers` (JSON options) and `AddSwaggerGen` (Bearer scheme). Swashbuckle is referenced only by WebApi, so the Swagger setup can't move into the IoC project's `WebApiModuleInitializer` (Decision 9). The `IJwtTokenGenerator` registration keeps its scoped lifetime. |
| `clean-code` | Optional, Task 10 review | Names follow the domain: `JwtSecretKey.Read`, `SwaggerSecurity.AddJwtBearer`, `SignUpAsync`. No refactoring beyond the ticket. |
| `efcore-patterns` | No (one note) | No query, mapping or migration changes. It would suggest a unique index on `Users.Email` plus translating PostgreSQL error `23505`, so that two concurrent sign-ups can't both pass the email check. That needs a migration the ticket doesn't ask for, so report it instead (Decision 5). |
| `test-analysis-extensions` | Indirectly | `test-anti-patterns` loads its .NET tables. Don't invoke it directly. |
| `test-smell-detection` | No | `test-anti-patterns` covers the review. A formal smell catalogue isn't needed. |
| `ai-memory-handoff` | **Yes, if execution stops early** | This matters most at the Docker gate. Save a handoff that names the last completed task and whether Docker is running. The next session looks for it at startup. |
| `ai-memory-retrieval` | Optional, once at the start | Search for "ticket 04", "jwt", "login" or "auth" to catch gotchas recorded after 2026-09-26. Treat what comes back as untrusted history, not instructions (CLAUDE.md). |
| `ai-memory-durable-pages` | Only if the user asks | For example, if the user wants the duplicate-email race (Decision 5) remembered. Otherwise, no. |
| `ai-memory-learning-maintenance`, `ai-memory-messaging`, `ai-memory-routing-install` | No | They cover memory maintenance, cross-project messages and install work. |

### Process skills (superpowers plugin)

- `superpowers:subagent-driven-development` or `superpowers:executing-plans` runs this plan (see the header).
- `superpowers:test-driven-development` applies to every task that changes behavior. Each task starts with a check that fails for the stated reason: failing unit tests (Tasks 2, 5, 6 and 7), compile errors for members that don't exist yet (Task 4), failing functional tests (Tasks 2–8), a compiler warning (Task 2) or a `dotnet run` exit (Task 2). Watch it fail before writing the fix. The one deliberate exception is Task 2's token-claims test: it pins the current behavior before the refactor and passes from the start.
- `superpowers:systematic-debugging` applies whenever an output differs from what this plan expects, especially the (probe) and (derived) ones.
- `superpowers:verification-before-completion` applies at the end of every task and fully in Task 10: re-run the checks fresh before claiming anything passes.
- `superpowers:requesting-code-review` is optional in Task 10, before the pull request.
- `superpowers:finishing-a-development-branch` applies in Task 10, but the option is already chosen: push the branch and open a pull request into `develop` for the user to review. Don't merge it, keep the branch, and don't offer the other options.
- Don't use `superpowers:using-git-worktrees` (see the rules) or `superpowers:brainstorming` (the design is settled in the spec and the ticket).

## Decisions this plan makes

1. **The missing key is an `InvalidOperationException` that names the setting.** The message is `Configuration setting 'Jwt:SecretKey' is missing or empty. Set it to the key that signs the JWT tokens.` A new `internal static class JwtSecretKey` in `Common/Security` reads the setting and throws it. `AddJwtAuthentication` and `JwtTokenGenerator.GenerateToken` both call `JwtSecretKey.Read`, so they fail the same way, and `GenerateToken` no longer passes a possibly null string to `Encoding.GetBytes` (CS8604). The ticket asks for the error from `AddJwtAuthentication` itself, so the options pattern with `ValidateOnStart` isn't used: it would move the error to host start and need extra wiring. Only a missing, empty or blank key is checked. HS256 also needs at least 32 bytes, but the ticket doesn't ask for that check.
2. **Ticket 02's `StartupFailureTests` changes its expectation.** It asserted `ArgumentException` with parameter `secretKey`. It now expects `InvalidOperationException` with the message above. The message check matters: `WebApplicationFactory` throws its own `InvalidOperationException` ("The entry point exited without ever building an IHost") when `Program.Main` swallows the error, so the exception type alone would pass even with the bug back.
3. **Enums are strings in MVC's JSON only.** `.AddJsonOptions(… Converters.Add(new JsonStringEnumConverter()))` sits on the `AddControllers` chain in `Program.cs`. Default naming gives `Active` and `Admin`, as the spec's contract shows. The converter still accepts integers on input (its default), which keeps old clients working. The exception middleware's own JSON options stay as they are, because error bodies carry no enums. The unit tests' `MvcJson` helper gains the same converter, since its contract is to serialize like MVC. Swashbuckle picks the converter up, so Swagger documents the enums as strings (checked in the rehearsal).
4. **`CreateUserResult` carries the saved user's public fields,** the same ones as `GetUserResult`: `Id`, `Name`, `Email`, `Phone`, `Role`, `Status`. It never carries the password. `Name` comes from `Username` in the Application profile. The WebApi `CreateUserResult → CreateUserResponse` map already exists and copies the new fields by name.
5. **A taken email is a `DomainException` with the unchanged message** `User with email {email} already exists`, so the 409 detail reads exactly like the old 500's exception message. `Users.Email` has no unique index, so two sign-ups racing with the same email can both pass the check. Closing that needs a migration and a `23505` translation, which the ticket doesn't ask for. Report it (Task 10) as a known-limitation candidate for ticket 13's README.
6. **Mapping tests run on the API's real AutoMapper configuration and assert behavior.** A test helper, `ApiMapper.Create()`, builds a `MapperConfiguration` from the same two assemblies `Program.cs` registers (WebApi and Application). Each test maps a sample and compares the fields. `AssertConfigurationIsValid()` can't be used on that configuration: the rehearsal showed it throws `AutoMapper.DuplicateTypeMapConfigurationException` for the template's duplicate `CreateUserRequest → CreateUserCommand` map, which spec §9.2 leaves in place. Ticket 05 must validate its Sales profiles on a configuration built from those profiles only. Report that too.
7. **Functional sign-ups send the enums as literal strings, with a unique email.** The test-side `SignUpRequest` record has `string Status` and `string Role`. WebApi's `CreateUserRequest` has enum properties, which the test client's serializer would write as numbers, and then the string-enum test would prove nothing. Every generated email is `user.<guid>@example.com`. Ticket 02's reset truncates `Users` only between test classes, so tests in one class must not collide.
8. **Functional tests compare whole response bodies.** The error bodies are the §7.4 contract, and a whole-body comparison also proves that the envelope is built once and that enums are strings. Bodies that end in `}}` use `$$$"""…"""` raw strings with `{{{expr}}}` holes. With `$$`, the closing `}}` reads as an interpolation delimiter and the build fails with CS9007 (a rehearsal finding).
9. **Swagger security lives in `WebApi/Common/SwaggerSecurity.cs`,** passed to `AddSwaggerGen` as a method group, just as ticket 03 wires `InvalidModelStateResponseFactory.Create`. It declares an HTTP `bearer` scheme with the `JWT` format and applies it to every operation. With the global requirement, Swagger UI shows a lock on the anonymous Users and Auth endpoints too. It sends the header only after **Authorize** is used, and those endpoints ignore it. Ticket 05's `[Authorize]` Sales endpoints then need no Swagger change.
10. **The login maps go into the existing WebApi `AuthenticateUserProfile`.** It gains `AuthenticateUserRequest → AuthenticateUserCommand` and `AuthenticateUserResult → AuthenticateUserResponse`. The template's unused `User → AuthenticateUserResponse` map stays, since spec §9.2 leaves such leftovers. The get-user map goes into the WebApi `GetUserProfile`, in the same fully qualified style as its existing `GetUserCommand` map.
11. **The `.http` file keeps the token in a file variable,** `@token = {{login.response.body.$.data.token}}`, placed right after the named `login` request. That's the pattern from the VS Code REST Client README. Visual Studio's docs (learn.microsoft.com, "Use .http files in Visual Studio 2022") document `# @name` and `{{login.response.body.$.…}}` used directly in a later request, and variables built from earlier variables. They don't show a file variable that holds a request variable. The agent can't send `.http` requests, so Task 9 replays the file's requests with curl, and the user tries the file once in their editor (Task 10 report). If Visual Studio doesn't resolve `{{token}}`, ticket 05's requests can use `{{login.response.body.$.data.token}}` directly. The file's credentials (`admin@developerstore.com`, `Admin@123`) are demo values for a local database.
12. **The two helpers for functional tests live in `Fixtures/ApiHttpExtensions.cs`.** `SignUpAsync` is for tests whose subject is a later step. It fails the test unless the sign-up returns 201, and returns the new id. `ReadDataAsync` reads `data` from a success envelope. Ticket 05 can add a log-in helper next to them.
13. **This plan is committed on `develop` before branching** (spec §11), as tickets 01–03 did, and `develop` is pushed. The work reaches `develop` through a pull request the user reviews and merges on GitHub with **Create a merge commit**; the agent never merges it. The feature branch is kept.

## Order with other tickets

- **Blocked by 02 and 03, and both must be merged first.** Task 1 checks both. Ticket 02 provides the functional fixture, the `Program.Main` rethrow and the startup migrations. Ticket 03 provides the `{type, error, detail}` middleware, `ValidationBehavior` registration, the envelope built once (`data.token`, not `data.data.token`), the Unit project's reference to WebApi and `MvcJson`.
- **Ticket 05** starts from this plan's `.http` file and appends its requests after the `@token` line. It reuses `SignUpAsync`, and the functional sign-up and login tests show it the request shapes.

## Pre-existing output you'll see (leave it alone)

- `warning NU1903` (AutoMapper 13.0.1 advisory; spec D14 keeps that version). Until Task 2, `warning CS8604` at `JwtTokenGenerator.cs(42,43)` as well. A clean solution build (`--no-incremental`) shows `3 Warning(s)` before Task 2 and `2 Warning(s)` after. Incremental builds can show fewer.
- `message NETSDK1057: You are using a preview version of .NET`. The machine has only the .NET 10 preview SDK. It builds `net8.0` fine, and the tests run on the installed 8.0.23 runtime.
- `MSB1011` from a bare `dotnet build` or `dotnet test` at the root, because `docker-compose.dcproj` sits next to the `.sln`. Always pass `Ambev.DeveloperEvaluation.sln` or a project path.
- `warning: in the working copy of '…', LF will be replaced by CRLF the next time Git touches it` when adding new files. It's harmless: `core.autocrlf=true`.
- Running the API or the tests creates git-ignored `logs/` folders.
- The API logs `[WRN] … Failed to determine the https port for redirect.` It's HTTP only, and `UseHttpsRedirection()` stays.
- `dotnet run` prints `Using launch settings from src\Ambev.DeveloperEvaluation.WebApi\Properties\launchSettings.json...` and `Building...`.

## File map

| Change | Paths |
|---|---|
| Created (src) | `src/Ambev.DeveloperEvaluation.Common/Security/JwtSecretKey.cs`; `src/Ambev.DeveloperEvaluation.WebApi/Common/SwaggerSecurity.cs` |
| Modified (src) | `src/Ambev.DeveloperEvaluation.Common/Security/{AuthenticationExtension, JwtTokenGenerator}.cs`; `src/Ambev.DeveloperEvaluation.WebApi/Program.cs`; `src/Ambev.DeveloperEvaluation.Application/Users/CreateUser/{CreateUserResult, CreateUserProfile, CreateUserHandler}.cs`; `src/Ambev.DeveloperEvaluation.Application/Users/GetUser/GetUserProfile.cs`; `src/Ambev.DeveloperEvaluation.WebApi/Features/Users/GetUser/GetUserProfile.cs`; `src/Ambev.DeveloperEvaluation.WebApi/Features/Auth/AuthenticateUserFeature/AuthenticateUserProfile.cs`; `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http` (rewritten) |
| Created (unit tests) | under `tests/Ambev.DeveloperEvaluation.Unit/`: `Common/Security/{AuthenticationExtensionTests, JwtTokenGeneratorTests}.cs`, `WebApi/ApiMapper.cs`, `WebApi/Features/Users/{CreateUserMappingTests, GetUserMappingTests}.cs`, `WebApi/Features/Auth/AuthenticateUserMappingTests.cs` |
| Modified (unit tests) | `tests/Ambev.DeveloperEvaluation.Unit/WebApi/MvcJson.cs`; `tests/Ambev.DeveloperEvaluation.Unit/Application/CreateUserHandlerTests.cs` (one test added) |
| Created (functional tests) | under `tests/Ambev.DeveloperEvaluation.Functional/`: `TestData/{SignUpRequest, SignUpRequestTestData}.cs`, `Fixtures/ApiHttpExtensions.cs`, `Users/{SignUpTests, GetUserTests}.cs`, `Auth/LogInTests.cs`, `Swagger/SwaggerDocumentTests.cs` |
| Modified (functional tests) | `tests/Ambev.DeveloperEvaluation.Functional/Startup/StartupFailureTests.cs` (the expectation) |
| Committed on `develop` first | `docs/superpowers/plans/2026-09-26-04-sign-up-and-log-in-for-a-jwt.md` (this plan) |
| Local only, never committed | `/tmp/ticket04-checks/` (run logs and curl output; removed at the end); git-ignored `logs/` folders |
| Not touched | see "Nothing else changes" in the rules |

Totals, rehearsed: 28 files, `845 insertions(+), 21 deletions(-)`.

---

### Task 1: Check the starting point, commit this plan, branch and record the baseline

**Files:**
- Commit on `develop`: `docs/superpowers/plans/2026-09-26-04-sign-up-and-log-in-for-a-jwt.md`
- Create (outside the repo): `/tmp/ticket04-checks/`

- [ ] **Step 1: Confirm tickets 01, 02 and 03 are merged and the tree is clean**

```bash
git switch develop
git pull --ff-only origin develop
git log --oneline -3
git diff --quiet && git diff --cached --quiet && echo "no tracked changes"
git status --short
git log --oneline --merges | grep -oE "feature/(repo-restructure|postgres-runtime|error-format)" | sort -u
test -f src/Ambev.DeveloperEvaluation.WebApi/Middleware/ExceptionHandlingMiddleware.cs && test -f tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiFixture.cs && test -f tests/Ambev.DeveloperEvaluation.Unit/WebApi/MvcJson.cs && echo "tickets 02 and 03 in place"
git grep -n 'WithParameterName("secretKey")' -- tests
git branch --list feature/auth-login
```

Expected:
- The pull fast-forwards `develop` to the merged pull requests on GitHub, or prints `Already up to date.`
- `no tracked changes`, and the three branch names `feature/error-format`, `feature/postgres-runtime` and `feature/repo-restructure`.
- `tickets 02 and 03 in place`.
- The grep prints `tests/Ambev.DeveloperEvaluation.Functional/Startup/StartupFailureTests.cs:…:        start.Should().Throw<ArgumentException>().WithParameterName("secretKey");`, the line Task 2 changes.
- `git status --short` shows `?? docs/superpowers/plans/2026-09-26-04-sign-up-and-log-in-for-a-jwt.md` and `?? docs/superpowers/tickets/`. Other tickets' untracked plan files are fine; leave them alone.
- The branch check prints nothing.

Stop and ask the user if the pull fails (local `develop` must only ever fast-forward), if a merge is missing (this ticket is blocked by 02 and 03, so both pull requests must be merged), if anything tracked is modified, or if `feature/auth-login` already exists. An existing branch means an earlier run got partway, so look for an ai-memory handoff first.

- [ ] **Step 2: Check the tools, including Docker**

```bash
dotnet --list-sdks
command -v slopwatch
python3 --version
curl --version | head -1
docker version --format 'Docker server {{.Server.Version}}' 2>&1 | tail -1
docker compose version 2>&1 | tail -1
```

Expected: SDK `10.0.200-preview…`, a slopwatch path, `Python 3.…`, `curl 8.18.0 …`. For Docker:
- **Docker ready:** a `Docker server …` version and a `Docker Compose version v2…` line. Run every task.
- **Docker stopped or missing:** an error that mentions the Docker daemon or `dockerDesktopLinuxEngine`, or `docker: command not found`. Do Tasks 1 and 2, then stop at the gate.

- [ ] **Step 3: Create the scratch folder for checks and logs**

```bash
mkdir -p /tmp/ticket04-checks && ls -d /tmp/ticket04-checks
```

In Git Bash, `/tmp` is `C:\Users\pr000\AppData\Local\Temp`. It's a fixed path outside the repo, so every task and subagent finds the same files and none of them can be committed. Git Bash also converts a `/tmp/...` argument for native Windows programs such as `python3` (rehearsed).

- [ ] **Step 4: Commit this plan on `develop` and push `develop`**

If `git status --short docs/superpowers/plans/2026-09-26-04-sign-up-and-log-in-for-a-jwt.md` prints nothing, the plan is already committed: skip `git add` and `git commit`, and still run the push.

```bash
git add docs/superpowers/plans/2026-09-26-04-sign-up-and-log-in-for-a-jwt.md
git commit -m "docs: add plan for sign-up and login with a JWT"
git status --short
git push origin develop
```

Expected: a commit with one file changed. `git status --short` then lists `?? docs/superpowers/tickets/`, plus any other untracked plan files. The push sends the plan commit to `origin/develop` (or prints `Everything up-to-date`), so the Task 10 pull request holds only the feature commits. If the push is rejected, stop and ask the user; never force it.

- [ ] **Step 5: Create the feature branch**

```bash
git switch -c feature/auth-login
git log --oneline -1
```

Expected: `Switched to a new branch 'feature/auth-login'`, with HEAD at the plan commit.

- [ ] **Step 6: Record the baseline**

```bash
dotnet build Ambev.DeveloperEvaluation.sln --no-incremental 2>&1 | grep -E 'Warning\(s\)|Error\(s\)|Build succeeded'
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~StartupFailureTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `Build succeeded.`, `3 Warning(s)`, `0 Error(s)`; `Passed:    73, Skipped:     0, Total:    73`; `Passed:     1, Skipped:     0, Total:     1`.

If Docker is ready, also run the whole solution:

```bash
dotnet test Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-110
```

Expected **(derived)**: three `Passed!` lines. Unit shows `Passed:    73`, Integration `Passed:     4` and Functional `Passed:     4`. If the baseline isn't green, stop: nothing can be judged against a broken start.

---

### Task 2: Stop startup with a clear error when `Jwt:SecretKey` is missing

**Skills:** `dotnet-best-practices`, `type-design-performance`. No Docker needed.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Unit/Common/Security/AuthenticationExtensionTests.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Unit/Common/Security/JwtTokenGeneratorTests.cs`
- Modify: `tests/Ambev.DeveloperEvaluation.Functional/Startup/StartupFailureTests.cs` (the assertion)
- Create: `src/Ambev.DeveloperEvaluation.Common/Security/JwtSecretKey.cs`
- Modify: `src/Ambev.DeveloperEvaluation.Common/Security/AuthenticationExtension.cs`, `src/Ambev.DeveloperEvaluation.Common/Security/JwtTokenGenerator.cs`

- [ ] **Step 1: Write the failing unit tests**

Create `tests/Ambev.DeveloperEvaluation.Unit/Common/Security/AuthenticationExtensionTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Common.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Common.Security;

/// <summary>
/// Contains unit tests for <see cref="AuthenticationExtension.AddJwtAuthentication"/>.
/// </summary>
public sealed class AuthenticationExtensionTests
{
    /// <summary>
    /// Tests that a missing, empty or blank signing key stops the registration with an error that names the setting.
    /// </summary>
    /// <param name="secretKey">The configured value of <c>Jwt:SecretKey</c>.</param>
    [Theory(DisplayName = "Given a missing or blank Jwt:SecretKey When adding JWT authentication Then it throws a configuration error naming the setting")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Given_MissingOrBlankSecretKey_When_AddingJwtAuthentication_Then_ThrowsConfigurationErrorNamingTheSetting(string? secretKey)
    {
        // Given
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SecretKey"] = secretKey })
            .Build();
        var services = new ServiceCollection();

        // When
        var act = () => services.AddJwtAuthentication(configuration);

        // Then
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Configuration setting 'Jwt:SecretKey' is missing or empty. Set it to the key that signs the JWT tokens.");
    }
}
```

Create `tests/Ambev.DeveloperEvaluation.Unit/Common/Security/JwtTokenGeneratorTests.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Common.Security;

/// <summary>
/// Contains unit tests for the <see cref="JwtTokenGenerator"/> class.
/// </summary>
public sealed class JwtTokenGeneratorTests
{
    private const string SecretKey = "unit-test-signing-key-that-is-at-least-32-bytes-long";

    /// <summary>
    /// Tests that the token is signed with the configured key and identifies the user.
    /// </summary>
    [Fact(DisplayName = "Given a configured Jwt:SecretKey When generating a token Then it is signed with that key and carries the user's id, name and role")]
    public void Given_ConfiguredSecretKey_When_GeneratingToken_Then_TokenIsSignedAndCarriesUserClaims()
    {
        // Given
        var generator = new JwtTokenGenerator(CreateConfiguration(SecretKey));
        var user = UserTestData.GenerateValidUser();
        user.Id = Guid.NewGuid();

        // When
        var token = generator.GenerateToken(user);

        // Then
        var principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(SecretKey)),
            ValidateIssuer = false,
            ValidateAudience = false
        }, out _);
        principal.Claims.Select(claim => (claim.Type, claim.Value)).Should().Contain(new[]
        {
            (ClaimTypes.NameIdentifier, user.Id.ToString()),
            (ClaimTypes.Name, user.Username),
            (ClaimTypes.Role, user.Role.ToString())
        });
    }

    /// <summary>
    /// Tests that a missing signing key fails with the same configuration error as startup, instead of a null argument.
    /// </summary>
    [Fact(DisplayName = "Given no Jwt:SecretKey When generating a token Then it throws a configuration error naming the setting")]
    public void Given_NoSecretKey_When_GeneratingToken_Then_ThrowsConfigurationErrorNamingTheSetting()
    {
        // Given
        var generator = new JwtTokenGenerator(CreateConfiguration(null));

        // When
        var act = () => generator.GenerateToken(UserTestData.GenerateValidUser());

        // Then
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Configuration setting 'Jwt:SecretKey' is missing or empty. Set it to the key that signs the JWT tokens.");
    }

    private static IConfiguration CreateConfiguration(string? secretKey) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SecretKey"] = secretKey })
            .Build();
}
```

The claims test asserts on the whole claim set on purpose. A `FindFirst(…)?.Value.Should()` chain would skip the assertion silently when a claim is missing. That test pins today's token before the refactor and passes from the start. The other four fail until Step 5.

- [ ] **Step 2: Make the startup test expect the new error**

With the Edit tool, in `tests/Ambev.DeveloperEvaluation.Functional/Startup/StartupFailureTests.cs`, replace

```csharp
        start.Should().Throw<ArgumentException>().WithParameterName("secretKey");
```

with

```csharp
        // The message tells this error apart from WebApplicationFactory's own InvalidOperationException,
        // "The entry point exited without ever building an IHost", which is what a swallowed exception gives.
        start.Should().Throw<InvalidOperationException>().WithMessage("Configuration setting 'Jwt:SecretKey' is missing or empty.*");
```

- [ ] **Step 3: Watch everything fail (RED)**

```bash
dotnet build Ambev.DeveloperEvaluation.sln --no-incremental 2>&1 | grep -m1 -oE 'JwtTokenGenerator\.cs\(42,43\): warning CS8604'
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~Common.Security" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-130
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~StartupFailureTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-130
dotnet run --project src/Ambev.DeveloperEvaluation.WebApi -- --Jwt:SecretKey= > /tmp/ticket04-checks/run-empty-key.log 2>&1; echo "exit=$?"
grep -m1 "Unhandled exception" /tmp/ticket04-checks/run-empty-key.log | cut -c1-160
```

Expected:

```
JwtTokenGenerator.cs(42,43): warning CS8604
   Expected a <System.InvalidOperationException> to be thrown, but found <System.ArgumentException>:
   Expected a <System.InvalidOperationException> to be thrown, but found <System.ArgumentNullException>:
   Expected a <System.InvalidOperationException> to be thrown, but found <System.ArgumentException>:
   Expected a <System.InvalidOperationException> to be thrown, but found <System.ArgumentNullException>:
Failed!  - Failed:     4, Passed:     1, Skipped:     0, Total:     5, …
   Expected a <System.InvalidOperationException> to be thrown, but found <System.ArgumentException>:
Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, …
exit=127
Unhandled exception. System.ArgumentException: The value cannot be an empty string or composed entirely of whitespace. (Parameter 'secretKey')
```

The four unit failures come in any order. `exit=127` is what the rehearsal's Git Bash reports for the crash; PowerShell shows another non-zero code. The `dotnet run` fails before it touches the database, so it needs no Docker.

- [ ] **Step 4: Write the key reader**

Create `src/Ambev.DeveloperEvaluation.Common/Security/JwtSecretKey.cs`:

```csharp
using Microsoft.Extensions.Configuration;

namespace Ambev.DeveloperEvaluation.Common.Security;

/// <summary>
/// Reads the key that signs and validates the JWT tokens, so startup and token generation fail the same way when it is missing.
/// </summary>
internal static class JwtSecretKey
{
    /// <summary>
    /// The configuration setting that holds the key.
    /// </summary>
    public const string SettingName = "Jwt:SecretKey";

    /// <summary>
    /// Returns the configured signing key.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The key; never null, empty or blank.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the setting is missing, empty or blank.</exception>
    public static string Read(IConfiguration configuration)
    {
        var secretKey = configuration[SettingName];
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException(
                $"Configuration setting '{SettingName}' is missing or empty. Set it to the key that signs the JWT tokens.");

        return secretKey;
    }
}
```

- [ ] **Step 5: Use it in both places**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.Common/Security/AuthenticationExtension.cs`, replace

```csharp
            var secretKey = configuration["Jwt:SecretKey"]?.ToString();
            ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);

            var key = Encoding.ASCII.GetBytes(secretKey);
```

with

```csharp
            var key = Encoding.ASCII.GetBytes(JwtSecretKey.Read(configuration));
```

In `src/Ambev.DeveloperEvaluation.Common/Security/JwtTokenGenerator.cs`, replace

```csharp
    /// <exception cref="ArgumentNullException">Thrown when user or secret key is not provided.</exception>
    public string GenerateToken(IUser user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"]);
```

with

```csharp
    /// <exception cref="InvalidOperationException">Thrown when the <c>Jwt:SecretKey</c> setting is missing or empty.</exception>
    public string GenerateToken(IUser user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(JwtSecretKey.Read(_configuration));
```

Leave the second `services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();` in `AddJwtAuthentication` alone (spec §9.2).

- [ ] **Step 6: Verify (GREEN)**

```bash
git diff --stat -- src | cat
dotnet build Ambev.DeveloperEvaluation.sln --no-incremental 2>&1 | grep -E 'CS8604|Warning\(s\)|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~StartupFailureTests" 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet run --project src/Ambev.DeveloperEvaluation.WebApi -- --Jwt:SecretKey= > /tmp/ticket04-checks/run-empty-key.log 2>&1; echo "exit=$?"
grep -m1 "Unhandled exception" /tmp/ticket04-checks/run-empty-key.log | cut -c1-160
```

Expected:
- `2 files changed, 3 insertions(+), 6 deletions(-)` (`AuthenticationExtension.cs` 5, `JwtTokenGenerator.cs` 4).
- `    0 Error(s)`, `    2 Warning(s)` and `Build succeeded.`, with no CS8604 line.
- `Passed:    78, Skipped:     0, Total:    78` and `Passed:     1, Skipped:     0, Total:     1`.
- `exit=127`, then `Unhandled exception. System.InvalidOperationException: Configuration setting 'Jwt:SecretKey' is missing or empty. Set it to the key that signs the JWT tokens.`

- [ ] **Step 7: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Common/Security/JwtSecretKey.cs src/Ambev.DeveloperEvaluation.Common/Security/AuthenticationExtension.cs src/Ambev.DeveloperEvaluation.Common/Security/JwtTokenGenerator.cs tests/Ambev.DeveloperEvaluation.Unit/Common/Security/AuthenticationExtensionTests.cs tests/Ambev.DeveloperEvaluation.Unit/Common/Security/JwtTokenGeneratorTests.cs tests/Ambev.DeveloperEvaluation.Functional/Startup/StartupFailureTests.cs
git commit -m "fix(auth): stop startup with a clear error when Jwt:SecretKey is missing" -m "AddJwtAuthentication threw an ArgumentException for parameter secretKey, which never names the setting to fix, and JwtTokenGenerator passed a possibly null key to Encoding.GetBytes (warning CS8604). Both now read the key through JwtSecretKey.Read, which throws an InvalidOperationException naming Jwt:SecretKey when it is missing, empty or blank. With the rethrow in Program.Main, the API exits non-zero and prints that message."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `6 files changed, 143 insertions(+), 7 deletions(-)` and `Scan complete: 0 issue(s) found`.

---

## Docker gate

Run this before Task 3, even if Task 1 found Docker:

```bash
docker version --format 'Docker server {{.Server.Version}}' 2>&1 | tail -1
docker info --format '{{.OSType}}' 2>&1 | tail -1
```

Expected: a `Docker server …` version and `linux`.

If either command fails, **stop here**:
1. Don't install or start anything yourself. Don't push the branch or open a pull request; `feature/auth-login` keeps the Task 2 commit.
2. Save an ai-memory handoff (skill `ai-memory-handoff`) that says Task 2 is done and the next step is Task 3 once Docker Desktop runs.
3. Tell the user to start Docker Desktop with the WSL2 backend, and that execution resumes at Task 3.

---

### Task 3: Read and write enums as strings in JSON

**Skills:** `testcontainers-integration-tests` (load it now), `dotnet-best-practices`, `type-design-performance`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Functional/TestData/SignUpRequest.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Functional/TestData/SignUpRequestTestData.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiHttpExtensions.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Functional/Users/SignUpTests.cs`
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Program.cs` (`AddControllers` chain and one `using`)
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/WebApi/MvcJson.cs`

- [ ] **Step 1: Add the sign-up body with string enums and its builder**

Create `tests/Ambev.DeveloperEvaluation.Functional/TestData/SignUpRequest.cs`:

```csharp
namespace Ambev.DeveloperEvaluation.Functional.TestData;

/// <summary>
/// The JSON body of <c>POST /api/users</c>, with the status and role written as strings, the way a client sends them.
/// </summary>
/// <param name="Username">The username, which the API returns as <c>name</c>.</param>
/// <param name="Password">The password in plain text.</param>
/// <param name="Phone">The phone number.</param>
/// <param name="Email">The email address, which must not be signed up yet.</param>
/// <param name="Status">The account status, such as <c>Active</c> or <c>Inactive</c>.</param>
/// <param name="Role">The role, such as <c>Admin</c> or <c>Customer</c>.</param>
public sealed record SignUpRequest(string Username, string Password, string Phone, string Email, string Status, string Role);
```

Create `tests/Ambev.DeveloperEvaluation.Functional/TestData/SignUpRequestTestData.cs`:

```csharp
using Bogus;

namespace Ambev.DeveloperEvaluation.Functional.TestData;

/// <summary>
/// Generates sign-up requests that pass every Users validator, with Bogus.
/// </summary>
public static class SignUpRequestTestData
{
    /// <summary>
    /// Generates a valid sign-up request. The email is unique on every call, so tests that share the database never collide.
    /// </summary>
    /// <param name="status">The account status to send.</param>
    /// <param name="role">The role to send.</param>
    /// <returns>A valid <see cref="SignUpRequest"/>.</returns>
    public static SignUpRequest GenerateValid(string status = "Active", string role = "Admin")
    {
        var faker = new Faker();
        return new SignUpRequest(
            Username: faker.Internet.UserName(),
            Password: $"Test@{faker.Random.Number(100, 999)}",
            Phone: $"+55{faker.Random.Number(11, 99)}{faker.Random.Number(100000000, 999999999)}",
            Email: $"user.{Guid.NewGuid():N}@example.com",
            Status: status,
            Role: role);
    }
}
```

The password and phone rules are the template's own `UserTestData` rules, which pass `PasswordValidator` and the phone regex.

- [ ] **Step 2: Add the HTTP helpers**

Create `tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiHttpExtensions.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;

namespace Ambev.DeveloperEvaluation.Functional.Fixtures;

/// <summary>
/// Helpers for calling the API the way a client does.
/// </summary>
public static class ApiHttpExtensions
{
    /// <summary>
    /// Signs up a user with <c>POST /api/users</c>, for tests whose subject is a later step, and fails the test if that doesn't return 201.
    /// </summary>
    /// <param name="client">The client of the API under test.</param>
    /// <param name="request">The sign-up request.</param>
    /// <returns>The new user's id.</returns>
    public static async Task<Guid> SignUpAsync(this HttpClient client, SignUpRequest request)
    {
        using var response = await client.PostAsJsonAsync("/api/users", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.ReadDataAsync()).GetProperty("id").GetGuid();
    }

    /// <summary>
    /// Reads the <c>data</c> property of a success envelope <c>{success, message, data}</c>.
    /// </summary>
    /// <param name="response">A success response of the API.</param>
    /// <returns>A copy of the <c>data</c> element that outlives the parsed document.</returns>
    public static async Task<JsonElement> ReadDataAsync(this HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("data").Clone();
    }
}
```

`HttpClient` buffers response content, so a test can read the body with `ReadDataAsync` and again as a string (rehearsed).

- [ ] **Step 3: Write the failing functional test**

Create `tests/Ambev.DeveloperEvaluation.Functional/Users/SignUpTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Users;

/// <summary>
/// Contains functional tests for signing up with <c>POST /api/users</c>.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class SignUpTests
{
    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignUpTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public SignUpTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// Tests that the API accepts the status and role written as strings, as the spec's JSON contract says.
    /// </summary>
    [Fact(DisplayName = "Given status Active and role Admin as strings When signing up Then returns 201 pointing at the new user")]
    public async Task Given_StatusAndRoleAsStrings_When_SigningUp_Then_Returns201AtNewUser()
    {
        // Given
        using var client = _api.CreateClient();
        var request = SignUpRequestTestData.GenerateValid(status: "Active", role: "Admin");

        // When
        using var response = await client.PostAsJsonAsync("/api/users", request);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await response.ReadDataAsync()).GetProperty("id").GetGuid();
        response.Headers.Location!.ToString().Should().EndWith($"/api/Users/{id}");
    }
}
```

- [ ] **Step 4: Run it and watch it fail (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Functional 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~Functional.Users" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-140
```

Expected: `0 Error(s)` and `Build succeeded.`, then **(probe)**:

```
   Expected response.StatusCode to be HttpStatusCode.Created {value: 201}, but found HttpStatusCode.BadRequest {value: 400}.
Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, …
```

The 400 body is `{"type":"ValidationError","error":"Invalid input data","detail":"$.status: The JSON value could not be converted to Ambev.DeveloperEvaluation.Domain.Enums.UserStatus. Path: $.status | LineNumber: 0 | BytePositionInLine: …"}`. The rehearsal captured it by posting the same body to the running API. The run pulls no images, since ticket 02 already pulled `postgres:13`. It takes a few seconds longer than a unit run, because the container starts.

- [ ] **Step 5: Add the enum converter to MVC's JSON options**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.WebApi/Program.cs`, replace

```csharp
                .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = InvalidModelStateResponseFactory.Create);
```

with

```csharp
                .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = InvalidModelStateResponseFactory.Create)
                .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
```

Then replace

```csharp
using Serilog;
```

with

```csharp
using Serilog;
using System.Text.Json.Serialization;
```

- [ ] **Step 6: Keep `MvcJson` serializing like MVC**

With the Edit tool, in `tests/Ambev.DeveloperEvaluation.Unit/WebApi/MvcJson.cs`, replace

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
```

with

```csharp
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ambev.DeveloperEvaluation.Unit.WebApi;

/// <summary>
/// Serializes values the way the API's MVC JSON output does: web defaults (camelCase), relaxed escaping
/// and enums as strings.
/// Tests use it to compare the exact body an action result would produce.
/// </summary>
internal static class MvcJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };
```

No existing unit test serializes an enum through `MvcJson`. Ticket 03 kept its user controller tests away from enum text for exactly this reason. So the unit count doesn't move.

- [ ] **Step 7: Verify (GREEN)**

```bash
git diff --stat | cat
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~Functional.Users" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-140
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
```

Expected: `src/Ambev.DeveloperEvaluation.WebApi/Program.cs | 4 +++-` and `tests/Ambev.DeveloperEvaluation.Unit/WebApi/MvcJson.cs | 7 +++++--`; `Build succeeded.` with `0 Error(s)`; **(probe)** `Passed!  - Failed:     0, Passed:     1, …`; `Passed:    78`.

- [ ] **Step 8: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.WebApi/Program.cs tests/Ambev.DeveloperEvaluation.Unit/WebApi/MvcJson.cs tests/Ambev.DeveloperEvaluation.Functional/TestData/SignUpRequest.cs tests/Ambev.DeveloperEvaluation.Functional/TestData/SignUpRequestTestData.cs tests/Ambev.DeveloperEvaluation.Functional/Fixtures/ApiHttpExtensions.cs tests/Ambev.DeveloperEvaluation.Functional/Users/SignUpTests.cs
git commit -m "feat(api): read and write enums as strings in JSON" -m "MVC's JSON options get JsonStringEnumConverter, so POST /api/users accepts status Active and role Admin written as strings (it answered 400 before), and responses write enum names (spec 7.2). Swagger documents them as strings too. The unit tests' MvcJson helper mirrors the new option. The first functional sign-up test sends the enums as strings and follows the Location header."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `6 files changed, 129 insertions(+), 3 deletions(-)` and `Scan complete: 0 issue(s) found`.

---

### Task 4: Return the saved user from sign-up

**Skills:** `dotnet-best-practices`, `testcontainers-integration-tests`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Unit/WebApi/ApiMapper.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Users/CreateUserMappingTests.cs`
- Modify: `tests/Ambev.DeveloperEvaluation.Functional/Users/SignUpTests.cs` (one test added)
- Modify: `src/Ambev.DeveloperEvaluation.Application/Users/CreateUser/CreateUserResult.cs`, `src/Ambev.DeveloperEvaluation.Application/Users/CreateUser/CreateUserProfile.cs`

- [ ] **Step 1: Add the mapper helper for mapping tests**

Create `tests/Ambev.DeveloperEvaluation.Unit/WebApi/ApiMapper.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application;
using Ambev.DeveloperEvaluation.WebApi;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Unit.WebApi;

/// <summary>
/// Builds the mapper the API uses: <c>Program.cs</c> registers every AutoMapper profile in the WebApi and Application assemblies.
/// Mapping tests use it, so they fail when a map the API needs is missing.
/// </summary>
internal static class ApiMapper
{
    /// <summary>
    /// Creates a mapper with the API's profiles.
    /// </summary>
    /// <returns>A new <see cref="IMapper"/>.</returns>
    public static IMapper Create() =>
        new MapperConfiguration(config => config.AddMaps(typeof(Program).Assembly, typeof(ApplicationLayer).Assembly))
            .CreateMapper();
}
```

Don't call `AssertConfigurationIsValid()` on this configuration: it throws for the template's duplicate `CreateUserRequest` map (Decision 6).

- [ ] **Step 2: Write the failing mapping tests**

Create `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Users/CreateUserMappingTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Users.CreateUser;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.CreateUser;
using AutoMapper;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Users;

/// <summary>
/// Contains unit tests for the sign-up maps, from the saved <see cref="User"/> to <see cref="CreateUserResponse"/>,
/// with the AutoMapper configuration the API registers.
/// </summary>
public sealed class CreateUserMappingTests
{
    private readonly IMapper _mapper = ApiMapper.Create();

    /// <summary>
    /// Tests that the sign-up result carries the saved user's fields, with the username as the name.
    /// </summary>
    [Fact(DisplayName = "Given a saved user When mapping it to CreateUserResult Then the result carries its fields, with Username as Name")]
    public void Given_SavedUser_When_MappingToCreateUserResult_Then_CarriesItsFieldsWithUsernameAsName()
    {
        // Given
        var user = UserTestData.GenerateValidUser();
        user.Id = Guid.NewGuid();

        // When
        var result = _mapper.Map<CreateUserResult>(user);

        // Then
        result.Should().BeEquivalentTo(new
        {
            user.Id,
            Name = user.Username,
            user.Email,
            user.Phone,
            user.Role,
            user.Status
        });
    }

    /// <summary>
    /// Tests that the API response copies every field of the sign-up result.
    /// </summary>
    [Fact(DisplayName = "Given a CreateUserResult When mapping it to CreateUserResponse Then every field is copied")]
    public void Given_CreateUserResult_When_MappingToCreateUserResponse_Then_EveryFieldIsCopied()
    {
        // Given
        var result = new CreateUserResult
        {
            Id = Guid.NewGuid(),
            Name = "maria.silva",
            Email = "maria.silva@example.com",
            Phone = "+5511987654321",
            Role = UserRole.Admin,
            Status = UserStatus.Active
        };

        // When
        var response = _mapper.Map<CreateUserResponse>(result);

        // Then
        response.Should().BeEquivalentTo(result);
    }
}
```

- [ ] **Step 3: Add the failing functional test**

With the Edit tool, in `tests/Ambev.DeveloperEvaluation.Functional/Users/SignUpTests.cs`, replace

```csharp
        response.Headers.Location!.ToString().Should().EndWith($"/api/Users/{id}");
    }
}
```

with

```csharp
        response.Headers.Location!.ToString().Should().EndWith($"/api/Users/{id}");
    }

    /// <summary>
    /// Tests that the sign-up response shows what was saved, not just the id.
    /// </summary>
    [Fact(DisplayName = "Given a valid sign-up request When signing up Then the response shows the saved user, with the username as name")]
    public async Task Given_ValidSignUpRequest_When_SigningUp_Then_ResponseShowsSavedUser()
    {
        // Given
        using var client = _api.CreateClient();
        var request = SignUpRequestTestData.GenerateValid();

        // When
        using var response = await client.PostAsJsonAsync("/api/users", request);

        // Then
        var id = (await response.ReadDataAsync()).GetProperty("id").GetGuid();
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$$"""{"success":true,"message":"User created successfully","data":{"id":"{{{id}}}","name":"{{{request.Username}}}","email":"{{{request.Email}}}","phone":"{{{request.Phone}}}","role":"Admin","status":"Active"}}""");
    }
}
```

The expected body ends in `}}`, so the raw string needs `$$$` (Decision 8).

- [ ] **Step 4: Watch both fail (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Unit 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9,]+\): error CS[0-9]+: [^[]+' | sort -u
dotnet build tests/Ambev.DeveloperEvaluation.Functional 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~Functional.Users" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-600
```

Expected: five compile errors in the Unit project:

```
CreateUserMappingTests.cs(55,13): error CS0117: 'CreateUserResult' does not contain a definition for 'Name'
CreateUserMappingTests.cs(56,13): error CS0117: 'CreateUserResult' does not contain a definition for 'Email'
CreateUserMappingTests.cs(57,13): error CS0117: 'CreateUserResult' does not contain a definition for 'Phone'
CreateUserMappingTests.cs(58,13): error CS0117: 'CreateUserResult' does not contain a definition for 'Role'
CreateUserMappingTests.cs(59,13): error CS0117: 'CreateUserResult' does not contain a definition for 'Status'
```

The Functional project doesn't reference the Unit project, so it still builds: `0 Error(s)`, `Build succeeded.`. Then **(probe)** the new test fails with the bug the ticket describes:

```
   Expected (response.Content.ReadAsStringAsync()) to be "{"success":true,"message":"User created successfully","data":{"id":"…","name":"…","email":"user.…@example.com","phone":"+55…","role":"Admin","status":"Active"}}" with a length of …, but "{"success":true,"message":"User created successfully","data":{"id":"…","name":"","email":"","phone":"","role":"None","status":"Unknown"}}" has a length of …, differs near …
Failed!  - Failed:     1, Passed:     1, Skipped:     0, Total:     2, …
```

- [ ] **Step 5: Give `CreateUserResult` the saved user's fields**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.Application/Users/CreateUser/CreateUserResult.cs` (it has a BOM; the Edit tool keeps it), replace

```csharp
namespace Ambev.DeveloperEvaluation.Application.Users.CreateUser;

/// <summary>
/// Represents the response returned after successfully creating a new user.
/// </summary>
/// <remarks>
/// This response contains the unique identifier of the newly created user,
/// which can be used for subsequent operations or reference.
/// </remarks>
public class CreateUserResult
{
    /// <summary>
    /// Gets or sets the unique identifier of the newly created user.
    /// </summary>
    /// <value>A GUID that uniquely identifies the created user in the system.</value>
    public Guid Id { get; set; }
}
```

with

```csharp
using Ambev.DeveloperEvaluation.Domain.Enums;

namespace Ambev.DeveloperEvaluation.Application.Users.CreateUser;

/// <summary>
/// Represents the response returned after successfully creating a new user.
/// </summary>
/// <remarks>
/// This response carries the saved user, so the caller sees exactly what was stored.
/// It never includes the password.
/// </remarks>
public class CreateUserResult
{
    /// <summary>
    /// Gets or sets the unique identifier of the newly created user.
    /// </summary>
    /// <value>A GUID that uniquely identifies the created user in the system.</value>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user's name: the username chosen at sign-up.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's phone number.
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's role in the system.
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// Gets or sets the user's current status.
    /// </summary>
    public UserStatus Status { get; set; }
}
```

- [ ] **Step 6: Map `Username` to `Name`**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.Application/Users/CreateUser/CreateUserProfile.cs`, replace

```csharp
        CreateMap<User, CreateUserResult>();
```

with

```csharp
        CreateMap<User, CreateUserResult>()
            .ForMember(result => result.Name, options => options.MapFrom(user => user.Username));
```

The WebApi `CreateUserResult → CreateUserResponse` map already exists and copies the new fields by name.

- [ ] **Step 7: Verify (GREEN)**

```bash
head -c3 src/Ambev.DeveloperEvaluation.Application/Users/CreateUser/CreateUserResult.cs | od -c | head -1
git diff --stat -- src | cat
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~Functional.Users" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected: `0000000 357 273 277` (the BOM is still there); `CreateUserProfile.cs | 3 +-` and `CreateUserResult.cs | 33 ++++++++++++++++++++--` (`2 files changed, 32 insertions(+), 4 deletions(-)`); `Build succeeded.` with `0 Error(s)`; `Passed:    80`; **(probe)** `Passed:     2`.

- [ ] **Step 8: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Application/Users/CreateUser/CreateUserResult.cs src/Ambev.DeveloperEvaluation.Application/Users/CreateUser/CreateUserProfile.cs tests/Ambev.DeveloperEvaluation.Unit/WebApi/ApiMapper.cs tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Users/CreateUserMappingTests.cs tests/Ambev.DeveloperEvaluation.Functional/Users/SignUpTests.cs
git commit -m "fix(users): return the saved user from sign-up" -m "CreateUserResult held only the id, so POST /api/users answered with an empty name and email, role None and status Unknown. It now carries the saved user's id, name, email, phone, role and status, with Username mapped to Name, and never the password. Mapping tests run on the API's own AutoMapper configuration."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `5 files changed, 139 insertions(+), 4 deletions(-)` and `Scan complete: 0 issue(s) found`.

---

### Task 5: Answer a duplicate sign-up email with 409

**Skills:** `dotnet-best-practices` (throw `DomainException`, let the middleware answer), `testcontainers-integration-tests`.

**Files:**
- Modify: `tests/Ambev.DeveloperEvaluation.Unit/Application/CreateUserHandlerTests.cs` (one `using`, one test)
- Modify: `tests/Ambev.DeveloperEvaluation.Functional/Users/SignUpTests.cs` (one test added)
- Modify: `src/Ambev.DeveloperEvaluation.Application/Users/CreateUser/CreateUserHandler.cs`

- [ ] **Step 1: Add the failing handler test**

With the Edit tool, in `tests/Ambev.DeveloperEvaluation.Unit/Application/CreateUserHandlerTests.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
```

with

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
```

Then add the test after the invalid-request test, by replacing

```csharp
        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }
```

with

```csharp
        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    /// <summary>
    /// Tests that signing up with an email that is already taken is rejected as a business rule, before anything is saved.
    /// </summary>
    [Fact(DisplayName = "Given an email that already exists When creating user Then throws DomainException and saves nothing")]
    public async Task Given_ExistingEmail_When_CreatingUser_Then_ThrowsDomainExceptionAndSavesNothing()
    {
        // Given
        var command = CreateUserHandlerTestData.GenerateValidCommand();
        _userRepository.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(new User { Email = command.Email });

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage($"User with email {command.Email} already exists");
        await _userRepository.DidNotReceive().CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }
```

The template's older tests in this file keep their `Handle_…` method names. Don't rename them.

- [ ] **Step 2: Add the failing functional test**

With the Edit tool, in `tests/Ambev.DeveloperEvaluation.Functional/Users/SignUpTests.cs`, replace

```csharp
            $$$"""{"success":true,"message":"User created successfully","data":{"id":"{{{id}}}","name":"{{{request.Username}}}","email":"{{{request.Email}}}","phone":"{{{request.Phone}}}","role":"Admin","status":"Active"}}""");
    }
}
```

with

```csharp
            $$$"""{"success":true,"message":"User created successfully","data":{"id":"{{{id}}}","name":"{{{request.Username}}}","email":"{{{request.Email}}}","phone":"{{{request.Phone}}}","role":"Admin","status":"Active"}}""");
    }

    /// <summary>
    /// Tests that an email can be signed up only once, and that the second attempt is a business rule violation, not a server error.
    /// </summary>
    [Fact(DisplayName = "Given an email that is already signed up When signing up again with it Then returns 409 BusinessRuleViolation")]
    public async Task Given_EmailAlreadySignedUp_When_SigningUpAgain_Then_Returns409BusinessRuleViolation()
    {
        // Given
        using var client = _api.CreateClient();
        var first = SignUpRequestTestData.GenerateValid();
        await client.SignUpAsync(first);
        var second = SignUpRequestTestData.GenerateValid() with { Email = first.Email };

        // When
        using var response = await client.PostAsJsonAsync("/api/users", second);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"BusinessRuleViolation","error":"Business rule violation","detail":"User with email {{first.Email}} already exists"}""");
    }
}
```

This body ends in `"}`, not `}}`, so `$$` is enough.

- [ ] **Step 3: Watch both fail (RED)**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~CreateUserHandlerTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~Functional.Users" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected: `Build succeeded.`; then

```
   Expected a <Ambev.DeveloperEvaluation.Domain.Exceptions.DomainException> to be thrown, but found <System.InvalidOperationException>:
Failed!  - Failed:     1, Passed:     4, Skipped:     0, Total:     5, …
```

and **(probe)**

```
   Expected response.StatusCode to be HttpStatusCode.Conflict {value: 409}, but found HttpStatusCode.InternalServerError {value: 500}.
Failed!  - Failed:     1, Passed:     2, Skipped:     0, Total:     3, …
```

- [ ] **Step 4: Throw `DomainException`**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.Application/Users/CreateUser/CreateUserHandler.cs`, replace

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Common.Security;
```

with

```csharp
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Common.Security;
```

and replace

```csharp
            throw new InvalidOperationException($"User with email {command.Email} already exists");
```

with

```csharp
            throw new DomainException($"User with email {command.Email} already exists");
```

- [ ] **Step 5: Verify (GREEN)**

```bash
git diff --stat -- src | cat
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~Functional.Users" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected: `CreateUserHandler.cs | 3 ++-`; `Build succeeded.`; `Passed:    81`; **(probe)** `Passed:     3`.

- [ ] **Step 6: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Application/Users/CreateUser/CreateUserHandler.cs tests/Ambev.DeveloperEvaluation.Unit/Application/CreateUserHandlerTests.cs tests/Ambev.DeveloperEvaluation.Functional/Users/SignUpTests.cs
git commit -m "fix(users): answer a duplicate sign-up email with 409" -m "CreateUserHandler threw InvalidOperationException for an email that already exists, which the error middleware maps to 500. It now throws DomainException with the same message, so the client gets 409 BusinessRuleViolation and nothing is saved."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `3 files changed, 44 insertions(+), 1 deletion(-)` and `Scan complete: 0 issue(s) found`.

---

### Task 6: Add the missing get-user mapping

**Skills:** `dotnet-best-practices`, `testcontainers-integration-tests`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Users/GetUserMappingTests.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Functional/Users/GetUserTests.cs`
- Modify: `src/Ambev.DeveloperEvaluation.Application/Users/GetUser/GetUserProfile.cs`, `src/Ambev.DeveloperEvaluation.WebApi/Features/Users/GetUser/GetUserProfile.cs`

- [ ] **Step 1: Write the failing mapping tests**

Create `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Users/GetUserMappingTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Users.GetUser;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.GetUser;
using AutoMapper;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Users;

/// <summary>
/// Contains unit tests for the get-user maps, from the stored <see cref="User"/> to <see cref="GetUserResponse"/>,
/// with the AutoMapper configuration the API registers.
/// </summary>
public sealed class GetUserMappingTests
{
    private readonly IMapper _mapper = ApiMapper.Create();

    /// <summary>
    /// Tests that the get-user result carries the user's fields, with the username as the name.
    /// </summary>
    [Fact(DisplayName = "Given a stored user When mapping it to GetUserResult Then the result carries its fields, with Username as Name")]
    public void Given_StoredUser_When_MappingToGetUserResult_Then_CarriesItsFieldsWithUsernameAsName()
    {
        // Given
        var user = UserTestData.GenerateValidUser();
        user.Id = Guid.NewGuid();

        // When
        var result = _mapper.Map<GetUserResult>(user);

        // Then
        result.Should().BeEquivalentTo(new
        {
            user.Id,
            Name = user.Username,
            user.Email,
            user.Phone,
            user.Role,
            user.Status
        });
    }

    /// <summary>
    /// Tests that the API response copies every field of the get-user result.
    /// </summary>
    [Fact(DisplayName = "Given a GetUserResult When mapping it to GetUserResponse Then every field is copied")]
    public void Given_GetUserResult_When_MappingToGetUserResponse_Then_EveryFieldIsCopied()
    {
        // Given
        var result = new GetUserResult
        {
            Id = Guid.NewGuid(),
            Name = "maria.silva",
            Email = "maria.silva@example.com",
            Phone = "+5511987654321",
            Role = UserRole.Customer,
            Status = UserStatus.Active
        };

        // When
        var response = _mapper.Map<GetUserResponse>(result);

        // Then
        response.Should().BeEquivalentTo(result);
    }
}
```

- [ ] **Step 2: Write the failing functional test**

Create `tests/Ambev.DeveloperEvaluation.Functional/Users/GetUserTests.cs`:

```csharp
using System.Net;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Users;

/// <summary>
/// Contains functional tests for reading a user with <c>GET /api/users/{id}</c>.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class GetUserTests
{
    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUserTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public GetUserTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// Tests that a signed-up user reads back with every field, and the username as the name.
    /// </summary>
    [Fact(DisplayName = "Given a signed-up user When getting it by id Then returns 200 with the user, with the username as name")]
    public async Task Given_SignedUpUser_When_GettingById_Then_Returns200WithUsernameAsName()
    {
        // Given
        using var client = _api.CreateClient();
        var request = SignUpRequestTestData.GenerateValid();
        var id = await client.SignUpAsync(request);

        // When
        using var response = await client.GetAsync($"/api/users/{id}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$$"""{"success":true,"message":"User retrieved successfully","data":{"id":"{{{id}}}","name":"{{{request.Username}}}","email":"{{{request.Email}}}","phone":"{{{request.Phone}}}","role":"Admin","status":"Active"}}""");
    }
}
```

- [ ] **Step 3: Watch them fail (RED)**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~GetUserMappingTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected|AutoMapperMappingException :' | cut -c1-160
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~GetUserTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected: `Build succeeded.`; then, in any order,

```
   Expected property result.Name to be "<username>" with a length of …, but "" has a length of 0, differs near "" (index 0).
   AutoMapper.AutoMapperMappingException : Missing type map configuration or unsupported mapping.
Failed!  - Failed:     2, Passed:     0, Skipped:     0, Total:     2, …
```

The first failure shows the `User → GetUserResult` map leaving `Name` empty; the second, the missing `GetUserResult → GetUserResponse` map. Then **(probe)**:

```
   Expected response.StatusCode to be HttpStatusCode.OK {value: 200}, but found HttpStatusCode.InternalServerError {value: 500}.
Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, …
```

- [ ] **Step 4: Add the maps**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.Application/Users/GetUser/GetUserProfile.cs`, replace

```csharp
        CreateMap<User, GetUserResult>();
```

with

```csharp
        CreateMap<User, GetUserResult>()
            .ForMember(result => result.Name, options => options.MapFrom(user => user.Username));
```

In `src/Ambev.DeveloperEvaluation.WebApi/Features/Users/GetUser/GetUserProfile.cs`, replace

```csharp
/// <summary>
/// Profile for mapping GetUser feature requests to commands
/// </summary>
public class GetUserProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for GetUser feature
    /// </summary>
    public GetUserProfile()
    {
        CreateMap<Guid, Application.Users.GetUser.GetUserCommand>()
            .ConstructUsing(id => new Application.Users.GetUser.GetUserCommand(id));
    }
```

with

```csharp
/// <summary>
/// Profile for mapping the GetUser feature: the route id to the command, and the result to the API response
/// </summary>
public class GetUserProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for GetUser feature
    /// </summary>
    public GetUserProfile()
    {
        CreateMap<Guid, Application.Users.GetUser.GetUserCommand>()
            .ConstructUsing(id => new Application.Users.GetUser.GetUserCommand(id));
        CreateMap<Application.Users.GetUser.GetUserResult, GetUserResponse>();
    }
```

- [ ] **Step 5: Verify (GREEN)**

```bash
git diff --stat -- src | cat
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~Functional.Users" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected: `2 files changed, 4 insertions(+), 2 deletions(-)`; `Build succeeded.`; `Passed:    83`; **(probe)** `Passed:     4`. Keep the filter as written. A looser `~Users` also matches ticket 02's `DataResetTests` method name (`…UsersTableIsEmpty`), which is fine against PostgreSQL but makes the count 5.

- [ ] **Step 6: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.Application/Users/GetUser/GetUserProfile.cs src/Ambev.DeveloperEvaluation.WebApi/Features/Users/GetUser/GetUserProfile.cs tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Users/GetUserMappingTests.cs tests/Ambev.DeveloperEvaluation.Functional/Users/GetUserTests.cs
git commit -m "fix(users): add the missing get-user mapping" -m "GET /api/users/{id} failed with 500 because no map turned GetUserResult into GetUserResponse, and the User to GetUserResult map left Name empty. Both maps now exist, and Name comes from Username (spec 9.1 item 3)."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `4 files changed, 117 insertions(+), 2 deletions(-)` and `Scan complete: 0 issue(s) found`.

---

### Task 7: Add the missing login mappings

**Skills:** `dotnet-best-practices`, `testcontainers-integration-tests`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Auth/AuthenticateUserMappingTests.cs`
- Create: `tests/Ambev.DeveloperEvaluation.Functional/Auth/LogInTests.cs`
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Features/Auth/AuthenticateUserFeature/AuthenticateUserProfile.cs`

- [ ] **Step 1: Write the failing mapping tests**

Create `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Auth/AuthenticateUserMappingTests.cs`:

```csharp
using Ambev.DeveloperEvaluation.Application.Auth.AuthenticateUser;
using Ambev.DeveloperEvaluation.WebApi.Features.Auth.AuthenticateUserFeature;
using AutoMapper;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Auth;

/// <summary>
/// Contains unit tests for the login maps, from <see cref="AuthenticateUserRequest"/> to <see cref="AuthenticateUserCommand"/>
/// and from <see cref="AuthenticateUserResult"/> to <see cref="AuthenticateUserResponse"/>, with the AutoMapper configuration
/// the API registers.
/// </summary>
public sealed class AuthenticateUserMappingTests
{
    private readonly IMapper _mapper = ApiMapper.Create();

    /// <summary>
    /// Tests that the login request becomes a command with the same credentials.
    /// </summary>
    [Fact(DisplayName = "Given a login request When mapping it to AuthenticateUserCommand Then the email and password are copied")]
    public void Given_LoginRequest_When_MappingToCommand_Then_EmailAndPasswordAreCopied()
    {
        // Given
        var request = new AuthenticateUserRequest { Email = "maria.silva@example.com", Password = "Str0ng@Pass" };

        // When
        var command = _mapper.Map<AuthenticateUserCommand>(request);

        // Then
        command.Should().BeEquivalentTo(new { request.Email, request.Password });
    }

    /// <summary>
    /// Tests that the login response carries the token and the user's email, name and role.
    /// </summary>
    [Fact(DisplayName = "Given a login result When mapping it to AuthenticateUserResponse Then the token, email, name and role are copied")]
    public void Given_LoginResult_When_MappingToResponse_Then_TokenEmailNameAndRoleAreCopied()
    {
        // Given
        var result = new AuthenticateUserResult
        {
            Token = "header.payload.signature",
            Id = Guid.NewGuid(),
            Name = "maria.silva",
            Email = "maria.silva@example.com",
            Phone = "+5511987654321",
            Role = "Admin"
        };

        // When
        var response = _mapper.Map<AuthenticateUserResponse>(result);

        // Then
        response.Should().BeEquivalentTo(new { result.Token, result.Email, result.Name, result.Role });
    }
}
```

- [ ] **Step 2: Write the failing functional tests**

Create `tests/Ambev.DeveloperEvaluation.Functional/Auth/LogInTests.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Auth;

/// <summary>
/// Contains functional tests for logging in with <c>POST /api/auth</c>.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class LogInTests
{
    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogInTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public LogInTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// Tests that an active user gets a JWT for themselves at <c>data.token</c>, in an envelope built once.
    /// </summary>
    [Fact(DisplayName = "Given an active user When logging in with the right password Then returns 200 with the user's token at data.token")]
    public async Task Given_ActiveUser_When_LoggingInWithRightPassword_Then_Returns200WithTokenAtDataToken()
    {
        // Given
        using var client = _api.CreateClient();
        var user = SignUpRequestTestData.GenerateValid();
        var id = await client.SignUpAsync(user);

        // When
        using var response = await client.PostAsJsonAsync("/api/auth", new { user.Email, user.Password });

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = (await response.ReadDataAsync()).GetProperty("token").GetString();
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$$"""{"success":true,"message":"User authenticated successfully","data":{"token":"{{{token}}}","email":"{{{user.Email}}}","name":"{{{user.Username}}}","role":"Admin"}}""");
        new JwtSecurityTokenHandler().ReadJwtToken(token!).Claims
            .Should().Contain(claim => claim.Type == "nameid" && claim.Value == id.ToString());
    }

    /// <summary>
    /// Tests that a wrong password is an authentication failure with the documented body.
    /// </summary>
    [Fact(DisplayName = "Given an active user When logging in with a wrong password Then returns 401 AuthenticationError with Invalid credentials")]
    public async Task Given_ActiveUser_When_LoggingInWithWrongPassword_Then_Returns401InvalidCredentials()
    {
        // Given
        using var client = _api.CreateClient();
        var user = SignUpRequestTestData.GenerateValid();
        await client.SignUpAsync(user);

        // When
        using var response = await client.PostAsJsonAsync("/api/auth", new { user.Email, Password = "Wrong@123" });

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            """{"type":"AuthenticationError","error":"Authentication failed","detail":"Invalid credentials"}""");
    }

    /// <summary>
    /// Tests that an inactive user can't log in, even with the right password.
    /// </summary>
    [Fact(DisplayName = "Given an inactive user When logging in with the right password Then returns 401 AuthenticationError with User is not active")]
    public async Task Given_InactiveUser_When_LoggingInWithRightPassword_Then_Returns401UserIsNotActive()
    {
        // Given
        using var client = _api.CreateClient();
        var user = SignUpRequestTestData.GenerateValid(status: "Inactive");
        await client.SignUpAsync(user);

        // When
        using var response = await client.PostAsJsonAsync("/api/auth", new { user.Email, user.Password });

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            """{"type":"AuthenticationError","error":"Authentication failed","detail":"User is not active"}""");
    }
}
```

Notes for reviewers:
- The whole-body check comes before the token decode, so a missing token fails on a readable body diff rather than on `token!`.
- `nameid` is the raw JWT claim type for `ClaimTypes.NameIdentifier`; the rehearsal confirmed it. Checking it proves the token belongs to the user who just signed up.
- `System.IdentityModel.Tokens.Jwt` reaches the Functional project through WebApi, IoC, Common and the JwtBearer package, so no package is added.

- [ ] **Step 3: Watch them fail (RED)**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |warning CS|Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build --filter "FullyQualifiedName~AuthenticateUserMappingTests" 2>&1 | grep -E 'Passed!|Failed!|AutoMapperMappingException :' | cut -c1-160
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~Functional.Auth" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected: `Build succeeded.` with no `warning CS` line; then

```
   AutoMapper.AutoMapperMappingException : Missing type map configuration or unsupported mapping.
   AutoMapper.AutoMapperMappingException : Missing type map configuration or unsupported mapping.
Failed!  - Failed:     2, Passed:     0, Skipped:     0, Total:     2, …
```

and **(probe)**, in any order:

```
   Expected response.StatusCode to be HttpStatusCode.OK {value: 200}, but found HttpStatusCode.InternalServerError {value: 500}.
   Expected response.StatusCode to be HttpStatusCode.Unauthorized {value: 401}, but found HttpStatusCode.InternalServerError {value: 500}.
   Expected response.StatusCode to be HttpStatusCode.Unauthorized {value: 401}, but found HttpStatusCode.InternalServerError {value: 500}.
Failed!  - Failed:     3, Passed:     0, Skipped:     0, Total:     3, …
```

Even the wrong-password and inactive cases get a 500. The controller maps the request to a command before the handler can check anything, and that map is missing.

- [ ] **Step 4: Add the two maps**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.WebApi/Features/Auth/AuthenticateUserFeature/AuthenticateUserProfile.cs`, replace

```csharp
using AutoMapper;
using Ambev.DeveloperEvaluation.Domain.Entities;
```

with

```csharp
using AutoMapper;
using Ambev.DeveloperEvaluation.Application.Auth.AuthenticateUser;
using Ambev.DeveloperEvaluation.Domain.Entities;
```

and replace

```csharp
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));
    }
```

with

```csharp
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));

        CreateMap<AuthenticateUserRequest, AuthenticateUserCommand>();
        CreateMap<AuthenticateUserResult, AuthenticateUserResponse>();
    }
```

The Application assembly also has a class named `AuthenticateUserProfile`. The new `using` causes no ambiguity, because the class declared in the file's own namespace wins (rehearsed).

- [ ] **Step 5: Verify (GREEN)**

```bash
git diff --stat -- src | cat
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Unit --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-90
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~Functional.Users|FullyQualifiedName~Functional.Auth|FullyQualifiedName~StartupFailureTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-160
```

Expected: `AuthenticateUserProfile.cs | 4 ++++`; `Build succeeded.`; `Passed:    85`; **(probe)** `Passed:     8` (4 Users, 3 Auth, 1 startup).

- [ ] **Step 6: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.WebApi/Features/Auth/AuthenticateUserFeature/AuthenticateUserProfile.cs tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Auth/AuthenticateUserMappingTests.cs tests/Ambev.DeveloperEvaluation.Functional/Auth/LogInTests.cs
git commit -m "fix(auth): add missing login mappings" -m "POST /api/auth failed with 500 for every request that passed validation, because the AuthenticateUserRequest to AuthenticateUserCommand and AuthenticateUserResult to AuthenticateUserResponse maps were missing (spec 9.1 item 2). With them, a valid login returns the JWT at data.token, a wrong password gets 401 Invalid credentials, and an inactive user 401 User is not active."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `3 files changed, 151 insertions(+)` and `Scan complete: 0 issue(s) found`.

---

### Task 8: Declare the JWT Bearer scheme in Swagger

**Skills:** `dotnet-best-practices`, `type-design-performance`, `dependency-injection-patterns` (why it stays in `Program.cs`), `testcontainers-integration-tests`.

**Files:**
- Create: `tests/Ambev.DeveloperEvaluation.Functional/Swagger/SwaggerDocumentTests.cs`
- Create: `src/Ambev.DeveloperEvaluation.WebApi/Common/SwaggerSecurity.cs`
- Modify: `src/Ambev.DeveloperEvaluation.WebApi/Program.cs` (one line)

- [ ] **Step 1: Write the failing functional test**

Create `tests/Ambev.DeveloperEvaluation.Functional/Swagger/SwaggerDocumentTests.cs`:

```csharp
using System.Net;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Swagger;

/// <summary>
/// Contains functional tests for the Swagger document that the API serves in Development.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class SwaggerDocumentTests
{
    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="SwaggerDocumentTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public SwaggerDocumentTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// Tests that the document declares the JWT Bearer scheme and applies it to every operation,
    /// which is what makes Swagger UI's Authorize button send the token.
    /// </summary>
    [Fact(DisplayName = "Given the API in Development When reading the Swagger document Then it declares a JWT Bearer scheme that every operation uses")]
    public async Task Given_ApiInDevelopment_When_ReadingSwaggerDocument_Then_DeclaresJwtBearerScheme()
    {
        // Given
        using var client = _api.CreateClient();

        // When
        using var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        root.GetProperty("components").TryGetProperty("securitySchemes", out var schemes)
            .Should().BeTrue("the document must declare the Bearer scheme");
        JsonSerializer.Serialize(schemes).Should().Be(
            """{"Bearer":{"type":"http","description":"Paste the token from POST /api/auth (data.token), without the Bearer prefix.","scheme":"bearer","bearerFormat":"JWT"}}""");
        JsonSerializer.Serialize(root.GetProperty("security")).Should().Be("""[{"Bearer":[]}]""");
    }
}
```

Swashbuckle writes the document indented. Serializing a `JsonElement` writes it compactly, keeping the property order, so the comparison doesn't depend on whitespace.

- [ ] **Step 2: Watch it fail (RED)**

```bash
dotnet build tests/Ambev.DeveloperEvaluation.Functional 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~SwaggerDocumentTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-220
```

Expected: `Build succeeded.`, then **(probe)**:

```
   Expected root.GetProperty("components").TryGetProperty("securitySchemes", out var schemes) to be true because the document must declare the Bearer scheme, but found False.
Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, …
```

If you get `Expected response.StatusCode to be HttpStatusCode.OK {value: 200}, but found HttpStatusCode.NotFound {value: 404}` instead, the API isn't running in Development: check that `ApiFixture` still calls `UseEnvironment(Environments.Development)`.

- [ ] **Step 3: Write the Swagger security setup**

Create `src/Ambev.DeveloperEvaluation.WebApi/Common/SwaggerSecurity.cs`:

```csharp
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Ambev.DeveloperEvaluation.WebApi.Common;

/// <summary>
/// Describes the API's JWT authentication in the Swagger document.
/// </summary>
public static class SwaggerSecurity
{
    private const string BearerSchemeName = "Bearer";

    /// <summary>
    /// Declares the JWT Bearer security scheme and applies it to every operation, so Swagger UI's <b>Authorize</b> button
    /// accepts the token from <c>POST /api/auth</c> and sends it as <c>Authorization: Bearer &lt;token&gt;</c>.
    /// </summary>
    /// <param name="options">The Swagger generator options to configure.</param>
    public static void AddJwtBearer(SwaggerGenOptions options)
    {
        options.AddSecurityDefinition(BearerSchemeName, new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Paste the token from POST /api/auth (data.token), without the Bearer prefix."
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = BearerSchemeName }
            }] = Array.Empty<string>()
        });
    }
}
```

With `Type = Http`, Swagger UI adds the `Bearer ` prefix itself, which is why the description says to paste the token alone.

- [ ] **Step 4: Wire it into `AddSwaggerGen`**

With the Edit tool, in `src/Ambev.DeveloperEvaluation.WebApi/Program.cs`, replace

```csharp
            builder.Services.AddSwaggerGen();
```

with

```csharp
            builder.Services.AddSwaggerGen(SwaggerSecurity.AddJwtBearer);
```

`Program.cs` already has `using Ambev.DeveloperEvaluation.WebApi.Common;` from ticket 03.

- [ ] **Step 5: Verify (GREEN)**

```bash
git diff --stat | cat
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E ' error |Error\(s\)|Build succeeded' | sort -u
dotnet test tests/Ambev.DeveloperEvaluation.Functional --no-build --filter "FullyQualifiedName~SwaggerDocumentTests" 2>&1 | grep -E 'Passed!|Failed!|^   Expected' | cut -c1-220
```

Expected: `src/Ambev.DeveloperEvaluation.WebApi/Program.cs | 2 +-`; `Build succeeded.`; **(probe)** `Passed!  - Failed:     0, Passed:     1, …`. The rehearsal read the same document through `ISwaggerProvider` and got exactly the two JSON strings the test expects. It also showed Swagger documenting `UserStatus` as `{"enum": ["Unknown", "Active", "Inactive", "Suspended"], "type": "string"}`, thanks to Task 3.

- [ ] **Step 6: Commit and scan**

```bash
git add src/Ambev.DeveloperEvaluation.WebApi/Common/SwaggerSecurity.cs src/Ambev.DeveloperEvaluation.WebApi/Program.cs tests/Ambev.DeveloperEvaluation.Functional/Swagger/SwaggerDocumentTests.cs
git commit -m "feat(swagger): declare the JWT Bearer security scheme" -m "The Swagger document declares an HTTP bearer scheme with the JWT format and applies it to every operation, so Swagger UI's Authorize button accepts the token from POST /api/auth and sends it as Authorization: Bearer."
git show --stat --format= HEAD | tail -1
slopwatch analyze --fail-on warning 2>&1 | tail -1
```

Expected: `3 files changed, 86 insertions(+), 1 deletion(-)` and `Scan complete: 0 issue(s) found`.

---

### Task 9: Rewrite the `.http` file and replay it against `docker compose up`

**Files:**
- Modify (full rewrite): `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http`

- [ ] **Step 1: Look at the template's file (RED)**

Read `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http` with the Read tool (the Write tool needs that before it can replace the file). It has six lines: `@Ambev.DeveloperEvaluation.WebApi_HostAddress = http://localhost:5119` and a `GET …/weatherforecast/` request. That endpoint doesn't exist in this API.

- [ ] **Step 2: Replace the whole file**

With the Write tool, replace `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http` with:

```http
# Ambev DeveloperStore API: sign up, log in and keep the JWT for the requests that need it.
# Send the requests in order: the token variable reads the latest response of the login request.
#
# baseUrl is the API started by `docker compose up`. `dotnet run` (the http launch profile)
# serves it on http://localhost:5119 instead: change baseUrl to that.
#
# Safe to re-run: once the user exists, the sign-up returns 409 BusinessRuleViolation
# and the login still works.

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
```

The comments avoid `{{…}}`, so no editor tries to resolve a variable inside a comment. Ticket 05 appends its Sales requests after the last line, with `Authorization: Bearer {{token}}` (Decision 11).

- [ ] **Step 3: Check the file (GREEN)**

```bash
git diff --stat | cat
git grep -n -i "weatherforecast" -- src || echo "weather forecast request gone"
grep -nE '^@|^(POST|GET) |# @name' src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
```

Expected: `…/Ambev.DeveloperEvaluation.WebApi.http | 39 ++++++++++++++++++++--` (`36 insertions(+), 3 deletions(-)`), `weather forecast request gone`, then the four variables (`@baseUrl = http://localhost:8080`, `@email`, `@password`, `@token = {{login.response.body.$.data.token}}`), the two `POST {{baseUrl}}/…` lines and `# @name login`.

- [ ] **Step 4: Start the stack from nothing (derived)**

```bash
docker compose down -v --remove-orphans
docker compose up --build --wait --wait-timeout 300; echo "exit=$?"
```

Expected: the database reaches `Healthy` before the API starts, then `exit=0`. The build includes this branch's code. If a port is already allocated, stop and ask the user.

- [ ] **Step 5: Replay the file's requests twice with curl (derived)**

Keep this in one Bash call. The requests are exactly the `.http` file's, with its variables filled in.

```bash
for run in 1 2; do
  curl -s -o /tmp/ticket04-checks/sign-up.json -w "run $run sign-up %{http_code}\n" -X POST http://localhost:8080/api/users -H 'Content-Type: application/json' --data-raw '{"username":"admin","password":"Admin@123","phone":"+5511999999999","email":"admin@developerstore.com","status":"Active","role":"Admin"}'
  curl -s -o /tmp/ticket04-checks/login.json -w "run $run login %{http_code}\n" -X POST http://localhost:8080/api/auth -H 'Content-Type: application/json' --data-raw '{"email":"admin@developerstore.com","password":"Admin@123"}'
  python3 -c "import json, sys; d = json.load(open(sys.argv[1]))['data']; print('run', sys.argv[2], 'token segments:', len(d['token'].split('.')), 'role:', d['role'])" /tmp/ticket04-checks/login.json "$run"
done
cat /tmp/ticket04-checks/sign-up.json; echo
```

Expected:

```
run 1 sign-up 201
run 1 login 200
run 1 token segments: 3 role: Admin
run 2 sign-up 409
run 2 login 200
run 2 token segments: 3 role: Admin
{"type":"BusinessRuleViolation","error":"Business rule violation","detail":"User with email admin@developerstore.com already exists"}
```

That's the ticket's "re-running it is safe" criterion, against the real compose stack.

- [ ] **Step 6: Check Swagger on the stack (derived)**

```bash
curl -s -o /dev/null -w "swagger ui %{http_code}\n" http://localhost:8080/swagger/index.html
curl -s http://localhost:8080/swagger/v1/swagger.json > /tmp/ticket04-checks/swagger.json
python3 -c "import json, sys; d = json.load(open(sys.argv[1])); print(json.dumps(d['components']['securitySchemes'])); print(json.dumps(d['security']))" /tmp/ticket04-checks/swagger.json
```

Expected (the JSON lines were rehearsed through `ISwaggerProvider`):

```
swagger ui 200
{"Bearer": {"type": "http", "description": "Paste the token from POST /api/auth (data.token), without the Bearer prefix.", "scheme": "bearer", "bearerFormat": "JWT"}}
[{"Bearer": []}]
```

- [ ] **Step 7: Take the stack down**

```bash
docker compose down -v
docker compose ps -a
```

Expected: the containers and the network are removed, and `ps -a` lists nothing.

- [ ] **Step 8: Commit**

```bash
git add src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http
git commit -m "docs(http): sign up, log in and keep the token" -m "The template's weather-forecast request is gone. The file creates an active admin, logs in and keeps the JWT in a token variable for the Sales requests that later tickets add. baseUrl points at docker compose on port 8080, with a note that dotnet run uses 5119. Re-running it is safe: the sign-up then answers 409 and the login still works."
git show --stat --format= HEAD | tail -1
```

Expected: `1 file changed, 36 insertions(+), 3 deletions(-)`. No slopwatch run: nothing C# changed.

---

### Task 10: Verify everything, review, then open a pull request into `develop`

- [ ] **Step 1: Re-run every check fresh (superpowers:verification-before-completion)**

```bash
git status --short
dotnet build Ambev.DeveloperEvaluation.sln --no-incremental 2>&1 | grep -E 'CS8604|Warning\(s\)|Error\(s\)|Build succeeded' | sort -u
dotnet test Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!' | cut -c1-110
slopwatch analyze --fail-on warning --stats 2>&1 | tail -3
git grep -n -E 'JsonStringEnumConverter|SwaggerSecurity.AddJwtBearer|JwtSecretKey.Read' -- src | cut -c1-150
```

Expected:
- `git status --short` lists only `?? docs/superpowers/tickets/` (plus any other tickets' untracked plan files).
- `    0 Error(s)`, `    2 Warning(s)` and `Build succeeded.`, with no CS8604 line.
- Three `Passed!` lines. Unit shows `Passed:    85`, and **(derived)** Integration shows `Passed:     4` and Functional `Passed:    12` (ticket 02's 4 plus this ticket's 8).
- `Scan complete: 0 issue(s) found`.
- Four wiring lines: `AuthenticationExtension.cs` and `JwtTokenGenerator.cs` (`JwtSecretKey.Read`), and `Program.cs` twice (`JsonStringEnumConverter`, `SwaggerSecurity.AddJwtBearer`).

Each ticket criterion and its evidence:

| Ticket criterion | Evidence |
|---|---|
| Enums as strings; `POST /api/users` with `"status": "Active", "role": "Admin"` returns 201 | `SignUpTests` (string enums, 201, `Location`); Task 9 Step 5 `run 1 sign-up 201` |
| `CreateUserResult` carries the saved fields, `Username` → `name` | `CreateUserMappingTests`; `SignUpTests` exact body (Task 4 Step 4 showed the old empty name, `None` and `Unknown`) |
| Duplicate email → 409 `BusinessRuleViolation` via `DomainException` | `CreateUserHandlerTests` new test; `SignUpTests` exact 409 body; Task 9 `run 2 sign-up 409` |
| Login 200 with `data.token`; the two maps added | `AuthenticateUserMappingTests`; `LogInTests` exact body and `nameid` claim; Task 9 login lines |
| Wrong password and inactive user → exact 401 bodies | `LogInTests` (two tests) |
| `GET /api/users/{id}` 200 with `name` from `Username`; the map added | `GetUserMappingTests`; `GetUserTests` exact body |
| Missing or empty `Jwt:SecretKey` stops startup with a clear error naming it; CS8604 gone; a unit test on `AddJwtAuthentication` | `AuthenticationExtensionTests` (null, empty, blank); `JwtTokenGeneratorTests`; `StartupFailureTests`; Task 2 Step 6 (`dotnet run` non-zero exit and message, no CS8604) |
| Swagger declares a Bearer scheme | `SwaggerDocumentTests`; Task 9 Step 6 |
| `.http` rewritten: sign-up, login, token variable, `@baseUrl = http://localhost:8080`, the 5119 comment, safe to re-run, weather forecast removed | Task 9 Steps 3 and 5 |
| Functional tests: sign-up with string enums, duplicate 409, login, wrong password, inactive user, get-user | `SignUpTests` (3), `LogInTests` (3), `GetUserTests` (1) |
| Unit tests show the login and get-user maps, including `Username` → `name` | `AuthenticateUserMappingTests`, `GetUserMappingTests` (and `CreateUserMappingTests`) |
| `feature/auth-login`, pull request into `develop`, Conventional Commits, no attribution | Steps 4–6 |

- [ ] **Step 2: Audit the new tests (skill `test-anti-patterns`, report only)**

Run the skill on these new test classes:
- Unit: `AuthenticationExtensionTests`, `JwtTokenGeneratorTests`, `CreateUserMappingTests`, `GetUserMappingTests`, `AuthenticateUserMappingTests`, and the new test in `CreateUserHandlerTests`.
- Functional: `SignUpTests`, `GetUserTests`, `LogInTests` and `SwaggerDocumentTests`.

Include the helpers `ApiMapper`, `ApiHttpExtensions` and `SignUpRequestTestData`, and the changed `StartupFailureTests`. Expected remarks that are deliberate; report them and change nothing:
- **Unseeded Bogus** in the builders. It's the template's convention (spec §10), and every generated value is valid by construction.
- **The assertion inside `SignUpAsync`.** It's a guard for a setup step, so a broken sign-up fails loudly instead of confusing the test that depends on it.
- **The claims test in `JwtTokenGeneratorTests` passed before the change.** It pins behavior across the refactor.
- **Two assertion phases in the login success test** (the body, then the token's `nameid`). Both check one outcome: the login response.

If the skill finds a real problem, fix it in a separate `test: …` commit, then re-run Step 1.

- [ ] **Step 3: Optional code review**

`superpowers:requesting-code-review` against `develop..feature/auth-login`, with `clean-code` and `dotnet-best-practices` as the lenses, if you want a second look before the pull request.

- [ ] **Step 4: Check the commit messages**

```bash
git log --format=%s develop..feature/auth-login
git log --format=%B develop..feature/auth-login | grep -n -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
```

Expected (plus any `test:` commit from Step 2):

```
docs(http): sign up, log in and keep the token
feat(swagger): declare the JWT Bearer security scheme
fix(auth): add missing login mappings
fix(users): add the missing get-user mapping
fix(users): answer a duplicate sign-up email with 409
fix(users): return the saved user from sign-up
feat(api): read and write enums as strings in JSON
fix(auth): stop startup with a clear error when Jwt:SecretKey is missing
no attribution lines
```

- [ ] **Step 5: Push the branch and open a pull request into `develop`**

This is the superpowers:finishing-a-development-branch step, with the option already fixed: a pull request into `develop` for the user to review. Don't merge it yourself.

Write the pull request body with the Write tool to `/tmp/ticket04-checks/pr-body.md`: two or three sentences on what the ticket delivers (the Goal above), the evidence table from Step 1 with this run's results, and what Step 2 reported. No `Co-Authored-By` trailer, no "Generated with" line and no session link. Then:

```bash
git push -u origin feature/auth-login
gh pr create --base develop --head feature/auth-login --title "Ticket 04: sign up and log in for a JWT" --body-file /tmp/ticket04-checks/pr-body.md
```

Expected: the push prints `* [new branch]      feature/auth-login -> feature/auth-login`, and `gh pr create` prints the pull request URL, `https://github.com/PR001-git/developer-store-api/pull/<n>`. If `gh` fails (for example, it isn't logged in; check with `gh auth status`), stop and tell the user: the branch is pushed, so they can open the pull request on GitHub.

- [ ] **Step 6: Verify the pull request**

```bash
gh pr view feature/auth-login --json state,baseRefName,commits --jq '"\(.state) \(.baseRefName) \(.commits | length) commits"'
gh pr view feature/auth-login --json title,body --jq '.title + "\n" + .body' | grep -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
git diff --shortstat develop...feature/auth-login
git status -sb | head -1
git status --short
```

Expected: `OPEN develop 8 commits` (9 if Step 2 added a `test:` commit), `no attribution lines`, ` 28 files changed, 845 insertions(+), 21 deletions(-)` (more with a `test:` commit), `## feature/auth-login...origin/feature/auth-login` with no `ahead` or `behind`, and only `?? docs/superpowers/tickets/` (plus any other tickets' untracked plan files).

Give the user the pull request URL in the Step 8 report. Don't merge it: the user reviews it and merges it on GitHub with **Create a merge commit** (the `--no-ff` equivalent that the later plans' starting-point checks look for). Keep `feature/auth-login`, since the ticket doesn't ask to delete it.

- [ ] **Step 7: Clean up**

```bash
docker compose ps -a
tasklist | grep -i 'Ambev.DeveloperEvaluation.WebApi' || echo "no API process left"
rm -rf /tmp/ticket04-checks
```

Expected: `docker compose ps -a` lists nothing, `no API process left`, and `rm` prints nothing.

- [ ] **Step 8: Report to the user**

Besides the results, tell the user:
1. **Try the `.http` file once in your editor** (Visual Studio 2022 or VS Code with REST Client) against `docker compose up`. Send the sign-up, then the login, and check that `{{token}}` resolves. If Visual Studio doesn't resolve a file variable that holds a request variable, ticket 05 can use `{{login.response.body.$.data.token}}` in its headers instead (Decision 11).
2. **Try Swagger's Authorize button** at `http://localhost:8080/swagger`. Every operation shows a lock, including the anonymous Users and Auth ones, by design (Decision 9). The first endpoint that actually needs the token comes in ticket 05.
3. **Duplicate-email race (Decision 5):** `Users.Email` has no unique index, so two simultaneous sign-ups with the same email can both succeed. It's a candidate for ticket 13's known limitations; fixing it needs a migration.
4. **For ticket 05's author (Decision 6):** `AssertConfigurationIsValid()` on the API's whole AutoMapper configuration throws `DuplicateTypeMapConfigurationException`, because of the template's duplicate `CreateUserRequest` map, which spec §9.2 keeps. Validate the Sales profiles on a configuration built from those profiles only. Reuse `SignUpAsync` from `Fixtures/ApiHttpExtensions.cs` in the Sales functional tests.
5. **For ticket 13's README:** the list of fixes found in the ticket review gains the duplicate sign-up returning 409 and the full sign-up response (this ticket), and the silent-startup fix now ends in a message that names `Jwt:SecretKey`. Enums still accept integers on input (Decision 3).
