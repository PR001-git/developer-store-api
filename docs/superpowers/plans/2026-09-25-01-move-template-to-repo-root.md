# Move the Template to the Repo Root (Ticket 01) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move everything under `template/backend/` to the repository root with `git mv` so history stays intact. Move the challenge text from `README.md` to `.doc/challenge.md` with working links, and leave a placeholder project `README.md`. Deliver it on `feature/repo-restructure`, as a pull request into `develop` for the user to review.

**Architecture:** This is a move, not a refactor. The feature branch gets three commits: (1) the template move, 116 exact renames (R100); (2) the challenge move plus link retargeting; (3) the placeholder README. Splitting (2) from (3) lets git record `README.md → .doc/challenge.md` as a rename. No production code changes, so three throwaway check scripts stand in for tests: they fail before the change and pass after it. They live outside the repo.

**Tech Stack:** Git 2.53 (Git for Windows) driven from Git Bash; .NET SDK 10.0.200-preview building the `net8.0` projects, with tests running on the installed 8.0.23 runtime; xUnit; Markdown; Docker Compose files (checked statically, because Docker isn't installed yet); Slopwatch.Cmd 0.4.2 (global tool).

**Source:** `docs/superpowers/tickets/01-move-template-to-repo-root.md`; Sales API design spec `docs/superpowers/specs/2026-09-24-sales-api-design.md`, D15, D16, §9.1 item 1 and §11.

**Rehearsed:** the move, the doc edits, the three checks, a local merge and the slopwatch baseline were run end to end in a scratch clone on 2026-09-25. The expected outputs below come from that run. The pushes and the pull request weren't rehearsed.

---

## Rules for every agent executing this plan

Restate these to every subagent you dispatch.

- **No AI attribution anywhere.** No `Co-Authored-By` trailer, no "Generated with" line, no session link in commits, merge commits, tags or pull requests. Use the commit messages below exactly as written. `.claude/settings.local.json` switches the defaults off, but don't rely on it.
- **Git Bash only.** Run every command in Git Bash (Claude Code's Bash tool), from the repo root `C:\Users\pr000\orca\developer-store-api` (`/c/Users/pr000/orca/developer-store-api`). PowerShell doesn't expand the `*` glob for native commands, and it has no `sed` or `sha256sum`.
- **Work in the main checkout, never in a worktree.** `.claude/` is untracked, so it exists only here. It holds `settings.local.json` (the attribution guard) and the project skills. A worktree has neither.
- **Never run `git clean` without the `template/` path.** `.git/info/exclude` makes `.claude/`, `.slopwatch/` and the two personal `.doc` files *ignored* files, and `git clean -X` deletes ignored files. Scoped to `template/`, it can only remove build output there.
- **Stage explicit paths only.** Never use `git add -A`, `git add .` or `git commit -a`. `docs/superpowers/tickets/` is untracked on purpose and must stay out of these commits.
- **Push only for the pull request.** Task 1 pushes `develop`, and Task 6 pushes `feature/repo-restructure` and opens the pull request. Never push `main`, never force-push, and never merge the pull request: the user reviews and merges it on GitHub.
- **Leave `.doc/brainstorm-show-case.md` and `.doc/template-code-review.md` alone.** They are the user's untracked personal notes. Don't open, edit, move or stage them.
- **Nothing else changes.** Don't fix the pre-existing issues listed below, even when they show up in output.

## Skills

### Project skills in `.claude/skills/`

| Skill | Use it? | When and how |
|---|---|---|
| `dotnet-slopwatch` | **Yes, Task 7** | After the pull request is opened, create the baseline at the new root with `slopwatch init`, then confirm `slopwatch analyze` finds 0 issues. The baseline was deferred to this ticket because the move changes every path. Follow the skill's project note: the tool is installed globally and `.slopwatch/` is git-excluded. So skip the skill's `git add`/`git commit` step, don't add `.config/dotnet-tools.json`, and don't add its hook to `.claude/settings.json`. |
| `ai-memory-handoff` | Only if execution stops early | If a session ends before Task 7 is done, save a handoff that names the last completed step. When the next session starts, look for it. |
| `ai-memory-retrieval` | Optional, once at the start | Search for "ticket 01" or "repo restructure" to catch gotchas recorded after 2026-09-25. On that date the store held only raw session notes, and this plan already covers them. |
| `dotnet-best-practices`, `clean-code`, `type-design-performance`, `dependency-injection-patterns`, `efcore-patterns` | No | No C# is written or reviewed. Every `.cs` and `.csproj` file moves byte for byte. |
| `test-anti-patterns`, `test-smell-detection`, `test-analysis-extensions` | No | No tests are written or audited. The 49 existing unit tests only have to stay green. |
| `testcontainers-integration-tests` | No | No integration tests are written, and this ticket doesn't need Docker. |
| `ai-memory-durable-pages`, `ai-memory-learning-maintenance`, `ai-memory-messaging`, `ai-memory-routing-install` | No | They cover explicit memory writes, maintenance, cross-project messages and install work. This ticket needs none of them. |

### Process skills (superpowers plugin)

- `superpowers:subagent-driven-development` or `superpowers:executing-plans` runs this plan (see the header).
- `superpowers:test-driven-development` applies in spirit. Task 2 writes the acceptance checks and watches them fail before anything moves. There's no production code to drive.
- `superpowers:verification-before-completion` applies in Task 6: re-run every check fresh before calling anything done or opening the pull request.
- `superpowers:finishing-a-development-branch` applies in Task 6, but the option is already chosen: push the branch and open a pull request into `develop` for the user to review. Don't merge it, keep the branch, and don't offer the other options.
- `superpowers:requesting-code-review` is optional before the pull request. The diff is renames plus about 25 changed doc lines.
- Don't use `superpowers:using-git-worktrees` (see the rules) or `superpowers:brainstorming` (the design is settled in spec D15).

## Decisions this plan makes

1. **Link labels stay the same; only targets change.** The ticket asks where the links point. "Back to README" and "Previous: Read Me" keep their wording, because nothing else changes in this ticket.
2. **The challenge links become page-relative** (`./overview.md`), including the five inside the commented-out "API Structure" block. Root-relative `/.doc/...` links would still work on GitHub, but page-relative links work in every viewer and match the other `.doc` pages. `./docs/general-api.md` never resolved.
3. **Three commits.** Committing the challenge move before the new README lets git record `README.md → .doc/challenge.md` as a rename (R086), so `git log --follow` works for it. In one commit, git would see `README.md` as modified and `.doc/challenge.md` as new.
4. **The two personal files get added to `.git/info/exclude`.** The ticket says they're listed there, but as of 2026-09-25 only `.claude/` and `.slopwatch/` are.
5. **This plan is committed on `develop` before branching**, because spec §11 says "the spec and plan are committed there". `docs/superpowers/tickets/` stays untracked; committing it is the user's call.
6. **Docker is checked statically.** Docker isn't installed, so a script confirms every path the compose file and both Dockerfiles use. If Docker is installed by the time this runs, `docker compose config --quiet` runs as well.
7. **The work reaches `develop` through a pull request the user reviews.** `origin` has only `main` on 2026-09-25, so Task 1 pushes `develop` (the spec and plan commits) to create it, and the pull request then holds only the three feature commits. The agent never merges it. The user merges it on GitHub with **Create a merge commit**, the `--no-ff` equivalent that later plans' merge checks rely on. The feature branch is kept.
8. **The slopwatch baseline comes last**, after the pull request is opened, on the feature branch (the tree `develop` gets once the pull request is merged), and stays local.

## Pre-existing output you'll see (leave it alone)

- `warning NU1903` (AutoMapper 13.0.1 advisory; spec D14 keeps that version) and `warning CS8604` at `JwtTokenGenerator.cs(42,43)` (fixed by a later ticket, spec §9.1 item 11).
- `message NETSDK1057: You are using a preview version of .NET`. The machine has only the .NET 10 preview SDK. It builds `net8.0` fine, and the tests run on the installed 8.0.23 runtime.
- `No test is available in …Integration.dll` and `…Functional.dll`. Those projects have no tests yet.
- `MSB1011` from a bare `dotnet build` or `dotnet restore` at the root, because `docker-compose.dcproj` sits next to the `.sln`. Always pass `Ambev.DeveloperEvaluation.sln`. The same error breaks `coverage-report.sh`, whose first build command is a bare `dotnet restore`, both before and after the move. It's out of scope here, but it belongs on the README's template-fixes list.
- `warning: in the working copy of '…', LF will be replaced by CRLF` when git adds a file written with LF endings. It's harmless: `core.autocrlf=true` normalizes line endings on add.

## File map

| Change | Paths |
|---|---|
| Moved with `git mv`, content unchanged (116 files, all R100) | `template/backend/{Ambev.DeveloperEvaluation.sln, docker-compose.yml, docker-compose.override.yml, docker-compose.dcproj, Dockerfile, launchSettings.json, coverage-report.sh, coverage-report.bat, .dockerignore, .editorconfig}` → repo root; `template/backend/src/**` (93 files) → `src/**`; `template/backend/tests/**` (13 files) → `tests/**` |
| Moved, links edited | `README.md` → `.doc/challenge.md` (9 link lines) |
| Link target edited | line 1 of `.doc/{auth-api, carts-api, frameworks, general-api, overview, products-api, project-structure, tech-stack, users-api}.md`; line 34 of `.doc/overview.md` |
| Created | `README.md` (placeholder) |
| Committed on `develop` first | `docs/superpowers/plans/2026-09-25-01-move-template-to-repo-root.md` (this plan) |
| Deleted | `template/`, after its ignored `bin/` and `obj/` folders are cleaned |
| Local only, never committed | `.git/info/exclude` (2 new entries), `.slopwatch/`, `/tmp/ticket01-checks/` (removed at the end) |
| Not touched | `.gitignore`, `docs/superpowers/specs/`, `docs/superpowers/tickets/`, `.claude/`, `.doc/brainstorm-show-case.md`, `.doc/template-code-review.md`, and the content of every file under `src/` and `tests/` |

---

### Task 1: Prepare `develop` and the feature branch

**Files:**
- Modify (local only, never committed): `.git/info/exclude`
- Commit on `develop`: `docs/superpowers/plans/2026-09-25-01-move-template-to-repo-root.md`
- Create (outside the repo): `/tmp/ticket01-checks/backup/`, `/tmp/ticket01-checks/personal.sha256`

- [ ] **Step 1: Confirm the starting state**

```bash
git switch develop
git log --oneline -3
git diff --quiet && git diff --cached --quiet && echo "no tracked changes"
git status --short
ls -A
git branch --list feature/repo-restructure
```

Expected:
- HEAD is `ca69712 docs: add Sales API design spec`, or a later `develop` commit.
- `no tracked changes`.
- `git status --short` shows untracked entries only, normally these four:

  ```
  ?? .doc/brainstorm-show-case.md
  ?? .doc/template-code-review.md
  ?? docs/superpowers/plans/
  ?? docs/superpowers/tickets/
  ```

- `ls -A` shows no `src/` or `tests/` at the root; if one existed, `git mv` would fail with `destination already exists`. On 2026-09-25 the root held `.claude .doc .git .gitignore docs README.md template`.
- The last command prints nothing.

If anything tracked is modified, or `feature/repo-restructure` already exists (an earlier run got partway), stop and ask the user.

- [ ] **Step 2: Back up the local-only files and record the personal files' hashes**

```bash
mkdir -p /tmp/ticket01-checks/backup
cp -r .claude .doc/brainstorm-show-case.md .doc/template-code-review.md /tmp/ticket01-checks/backup/
sha256sum .doc/brainstorm-show-case.md .doc/template-code-review.md > /tmp/ticket01-checks/personal.sha256
cat /tmp/ticket01-checks/personal.sha256
```

Expected: two hash lines. In Git Bash, `/tmp` is `C:\Users\pr000\AppData\Local\Temp`. That's a fixed path outside the repo, so every task and every subagent finds the same files, and none of them can be committed.

- [ ] **Step 3: Exclude the personal files from git**

```bash
if ! grep -qF 'brainstorm-show-case.md' .git/info/exclude; then
  printf '\n# Personal notes in .doc (never committed)\n.doc/brainstorm-show-case.md\n.doc/template-code-review.md\n' >> .git/info/exclude
fi
git check-ignore -v .doc/brainstorm-show-case.md .doc/template-code-review.md
git status --short
```

Expected: `git check-ignore -v` prints one `.git/info/exclude:<line>:…` line per file, and `git status --short` now lists only `?? docs/superpowers/plans/` and `?? docs/superpowers/tickets/`.

- [ ] **Step 4: Commit this plan on `develop` and push `develop`**

If `git status --short docs/superpowers/plans/` prints nothing, the plan is already committed: skip `git add` and `git commit`, and still run the push.

```bash
git add docs/superpowers/plans/2026-09-25-01-move-template-to-repo-root.md
git commit -m "docs: add plan for moving the template to the repo root"
git status --short
git push -u origin develop
```

Expected: a commit with one file changed, after which `git status --short` lists only `?? docs/superpowers/tickets/`. The push prints `* [new branch]      develop -> develop` (or `Everything up-to-date` on a re-run): `origin/develop` now holds `ca69712` and the plan commit, and is the base of the Task 6 pull request. If the push is rejected, stop and ask the user; never force it.

- [ ] **Step 5: Create the feature branch**

```bash
git switch -c feature/repo-restructure
git log --oneline -1
```

Expected: `Switched to a new branch 'feature/repo-restructure'`, with HEAD at the plan commit.

---

### Task 2: Write the acceptance checks and watch them fail

These scripts stand in for tests. They live in `/tmp/ticket01-checks/`, outside the repo, and are never committed.

**Files:**
- Create: `/tmp/ticket01-checks/check-layout.sh`
- Create: `/tmp/ticket01-checks/check-docker-paths.sh`
- Create: `/tmp/ticket01-checks/check-links.sh`

- [ ] **Step 1: Write the layout check**

```bash
cat > /tmp/ticket01-checks/check-layout.sh <<'EOF'
#!/usr/bin/env bash
# Ticket 01 layout check. Run from the repo root: template/ is gone and every
# former template/backend entry is tracked (in the index) at the repo root.
problems=0
if [ -e template ]; then echo "PROBLEM  template/ still exists"; problems=$((problems + 1)); else echo "ok       template/ is gone"; fi
for p in src tests Ambev.DeveloperEvaluation.sln docker-compose.yml docker-compose.override.yml \
         docker-compose.dcproj Dockerfile launchSettings.json coverage-report.sh coverage-report.bat \
         .dockerignore .editorconfig; do
  if git ls-files --error-unmatch -- "$p" >/dev/null 2>&1; then echo "ok       $p"; else echo "PROBLEM  $p is not tracked at the root"; problems=$((problems + 1)); fi
done
echo "layout problems: $problems"
[ "$problems" -eq 0 ]
EOF
```

- [ ] **Step 2: Write the Docker path check**

Docker isn't installed on this machine. This check reads the compose file and both Dockerfiles, and confirms every path they use exists relative to the compose file.

```bash
cat > /tmp/ticket01-checks/check-docker-paths.sh <<'EOF'
#!/usr/bin/env bash
# Ticket 01 Docker path check, no Docker needed. Run from the repo root: the compose
# build context, the Dockerfile it names, .dockerignore, and every COPY source in both
# Dockerfiles must exist relative to docker-compose.yml.
if [ ! -f docker-compose.yml ]; then echo "MISSING  docker-compose.yml at the repo root"; echo "missing paths: 1"; exit 1; fi
missing=0
context=$(sed -n 's/^[[:space:]]*context:[[:space:]]*//p' docker-compose.yml | tr -d '\r')
dockerfile=$(sed -n 's/^[[:space:]]*dockerfile:[[:space:]]*//p' docker-compose.yml | tr -d '\r')
echo "compose context: $context, dockerfile: $dockerfile"
for p in "$context/$dockerfile" "$context/.dockerignore" Dockerfile docker-compose.override.yml; do
  if [ -f "$p" ]; then echo "ok       $p"; else echo "MISSING  $p"; missing=$((missing + 1)); fi
done
for df in Dockerfile "$context/$dockerfile"; do
  [ -f "$df" ] || continue
  while IFS= read -r src; do
    if [ -e "$context/$src" ]; then echo "ok       $df COPY $src"; else echo "MISSING  $df COPY $src"; missing=$((missing + 1)); fi
  done < <(grep -oE '^COPY \["[^"]+"' "$df" | sed -E 's/^COPY \["//; s/"$//')
done
echo "missing paths: $missing"
[ "$missing" -eq 0 ]
EOF
```

- [ ] **Step 3: Write the link check**

It reads only tracked pages (`git ls-files`), so the personal files are never checked. A target starting with `/` resolves from the repo root, as GitHub resolves it.

```bash
cat > /tmp/ticket01-checks/check-links.sh <<'EOF'
#!/usr/bin/env bash
# Ticket 01 link check. Run from the repo root: every relative link in the tracked
# README.md and .doc/*.md pages must point at an existing file.
broken=0
for f in $(git ls-files README.md '.doc/*.md'); do
  dir=$(dirname "$f")
  while IFS= read -r target; do
    case "$target" in http://*|https://*|mailto:*|'#'*) continue ;; esac
    path=${target%%#*}
    case "$path" in /*) resolved=".$path" ;; *) resolved="$dir/$path" ;; esac
    if [ -e "$resolved" ]; then echo "ok      $f -> $target"; else echo "BROKEN  $f -> $target"; broken=$((broken + 1)); fi
  done < <(grep -oE '\]\([^)[:space:]]+\)|href="[^"]+"' "$f" | sed -E 's/^\]\(//; s/\)$//; s/^href="//; s/"$//')
done
echo "broken links: $broken"
[ "$broken" -eq 0 ]
EOF
```

- [ ] **Step 4: Run the three checks and confirm they fail**

```bash
bash /tmp/ticket01-checks/check-layout.sh | grep -E 'template/|layout problems'; echo "exit=${PIPESTATUS[0]}"
bash /tmp/ticket01-checks/check-docker-paths.sh; echo "exit=$?"
bash /tmp/ticket01-checks/check-links.sh | grep -E 'BROKEN|broken links'; echo "exit=${PIPESTATUS[0]}"
```

Expected (RED):

```
PROBLEM  template/ still exists
layout problems: 13
exit=1
MISSING  docker-compose.yml at the repo root
missing paths: 1
exit=1
BROKEN  README.md -> ./docs/general-api.md
broken links: 1
exit=1
```

The layout count is 13: `template/` itself plus the 12 root entries. The broken link was already broken in the original challenge README, which points at a `docs/` folder that never existed. Task 4 fixes it.

- [ ] **Step 5: Record the baseline build and tests at the old location**

```bash
dotnet build template/backend/Ambev.DeveloperEvaluation.sln 2>&1 | grep -E 'warning (NU|CS)|Warning\(s\)|Error\(s\)|Build succeeded' | sort -u
dotnet test template/backend/Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!|No test is available'
```

Expected:
- The build prints `Build succeeded.` and `0 Error(s)`. Its only warnings are NU1903 (AutoMapper 13.0.1) and CS8604 (`JwtTokenGenerator.cs(42,43)`). An incremental build can show fewer.
- The tests print `Passed!  - Failed:     0, Passed:    49, Skipped:     0, Total:    49, … - Ambev.DeveloperEvaluation.Unit.dll (net8.0)`, plus two `No test is available` lines for Integration and Functional.

If the baseline isn't green, stop: the move can't be judged against a broken starting point.

---

### Task 3: Move `template/backend` to the repo root

**Files:**
- Move (content unchanged, 116 files): the 10 top-level entries of `template/backend/` → repo root; `template/backend/src/**` → `src/**`; `template/backend/tests/**` → `tests/**`
- Delete: the ignored `bin/` and `obj/` folders under `template/`, then the empty `template/backend/` and `template/` directories

- [ ] **Step 1: Release file locks**

Close Visual Studio or Rider if the solution is open, and stop any running WebApi. Then:

```bash
dotnet build-server shutdown
```

On Windows, any process holding a file or folder under `template/backend` makes `git mv` fail with `Permission denied`.

- [ ] **Step 2: Preview the clean**

```bash
git clean -ndX template/
```

Expected: only `bin/` and `obj/` folders under `template/backend/`. On 2026-09-25 that was 20 lines: `template/backend/bin/`, `template/backend/obj/`, plus one `bin/` and one `obj/` per project. `.vs/` or `TestResults/` folders are fine to remove too. If anything else is listed, stop and ask the user.

- [ ] **Step 3: Clean the ignored build output under `template/`**

Copy this command exactly. The `template/` argument keeps `.claude/`, `.slopwatch/` and the personal `.doc` files safe.

```bash
git clean -fdX template/
```

Why this step is needed: while the ignored folders exist, `git mv template/backend/* .` refuses them with `fatal: source directory is empty, source=template/backend/bin, destination=bin`, and moves nothing.

- [ ] **Step 4: Dry-run the move**

```bash
git mv -n template/backend/* .
```

Expected: 10 `Checking rename of 'template/backend/<name>' to '<name>'` lines, one each for the `.sln`, `coverage-report.bat`, `coverage-report.sh`, `docker-compose.dcproj`, `docker-compose.override.yml`, `docker-compose.yml`, `Dockerfile`, `launchSettings.json`, `src` and `tests`. There's no `fatal:` line. The `*` glob doesn't match dotfiles, so `.dockerignore` and `.editorconfig` get their own command in the next step.

- [ ] **Step 5: Move**

```bash
git mv template/backend/* .
git mv template/backend/.dockerignore template/backend/.editorconfig .
```

Expected: no output.

If a `git mv` fails with `renaming '…' failed: Permission denied`, git didn't record that command, but the entries it renamed before failing are already at the root on disk. `git status --short` shows each of them as ` D template/backend/…` lines plus `?? <name>`. For each one, run `mv <name> template/backend/<name>` to put it back, and check that `git status --short` has no ` D` lines left. Then release the lock (Step 1) and rerun the failed command.

- [ ] **Step 6: Remove the empty folders**

```bash
rmdir template/backend template
test ! -e template && echo "template/ is gone"
```

Expected: `template/ is gone`. If `rmdir` reports `Directory not empty`, run `ls -A template/backend`, then stop and ask the user: something is there that the preview didn't show.

- [ ] **Step 7: Check what's staged**

```bash
git status --short | cut -c1-2 | sort | uniq -c
git status --short | grep -v '^R  ' | grep -vx '?? docs/superpowers/tickets/'
```

Expected: the first command prints `116 R ` and `1 ??`, and the second prints nothing.

- [ ] **Step 8: Run the layout and Docker checks (GREEN)**

```bash
bash /tmp/ticket01-checks/check-layout.sh | tail -1; echo "exit=${PIPESTATUS[0]}"
bash /tmp/ticket01-checks/check-docker-paths.sh | sed -n '1p;$p'; echo "exit=${PIPESTATUS[0]}"
```

Expected:

```
layout problems: 0
exit=0
compose context: ., dockerfile: src/Ambev.DeveloperEvaluation.WebApi/Dockerfile
missing paths: 0
exit=0
```

The full Docker output has 16 `ok` lines. If `docker --version` works, also run `docker compose config --quiet` from the root; it should exit 0 and print nothing.

- [ ] **Step 9: Build and test at the root**

```bash
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E 'warning (NU|CS)|Warning\(s\)|Error\(s\)|Build succeeded' | sort -u
dotnet test Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!|No test is available'
dotnet build 2>&1 | grep MSB1011
```

Expected:
- `Build succeeded.` and `0 Error(s)`, with the same NU1903 and CS8604 warnings. This build starts clean, so it shows `3 Warning(s)`.
- `Passed!  - Failed:     0, Passed:    49, Skipped:     0, Total:    49`.
- `MSBUILD : error MSB1011: Specify which project or solution file to use because this folder contains more than one project or solution file.` The ticket expects this error, because the `.dcproj` sits next to the `.sln`.

`.gitignore` covers the new `bin/` and `obj/` folders, so `git status --short` is unchanged.

- [ ] **Step 10: Commit**

```bash
git commit -m "refactor: move template to repo root" -m "Move everything under template/backend/, including .dockerignore and .editorconfig, to the repository root with git mv, so the layout matches .doc/project-structure.md. File contents are unchanged, and paths relative to the solution and the compose file stay the same."
```

- [ ] **Step 11: Verify that git recorded renames and history follows them**

```bash
git show --format= --name-status -M HEAD | grep -c '^R100'
git show --format= --name-status -M HEAD | awk -F'\t' '$1 != "R100" || $2 != "template/backend/" $3'
git log --follow --oneline -- src/Ambev.DeveloperEvaluation.WebApi/Program.cs
```

Expected:
- `116`.
- Nothing from the `awk` line: every entry is an exact rename to its old path minus `template/backend/`.
- Two log lines: this commit, then `c3590f8 Initial commit`.

---

### Task 4: Move the challenge text to `.doc/challenge.md`

**Files:**
- Move and edit links: `README.md` → `.doc/challenge.md` (lines 52, 57, 62, 67–71, 77)
- Modify line 1: `.doc/auth-api.md`, `.doc/carts-api.md`, `.doc/frameworks.md`, `.doc/general-api.md`, `.doc/overview.md`, `.doc/products-api.md`, `.doc/project-structure.md`, `.doc/tech-stack.md`, `.doc/users-api.md`
- Modify line 34: `.doc/overview.md`

- [ ] **Step 1: Move the file**

```bash
git mv README.md .doc/challenge.md
```

- [ ] **Step 2: Make the challenge links page-relative**

The file now sits inside `.doc/`, so its links point straight at the neighbouring pages. These are the only 9 lines that change:

| Line | Before | After |
|---|---|---|
| 52 | `See [Overview](/.doc/overview.md)` | `See [Overview](./overview.md)` |
| 57 | `See [Tech Stack](/.doc/tech-stack.md)` | `See [Tech Stack](./tech-stack.md)` |
| 62 | `See [Frameworks](/.doc/frameworks.md)` | `See [Frameworks](./frameworks.md)` |
| 67 | `- [API General](./docs/general-api.md)` | `- [API General](./general-api.md)` |
| 68 | `- [Products API](/.doc/products-api.md)` | `- [Products API](./products-api.md)` |
| 69 | `- [Carts API](/.doc/carts-api.md)` | `- [Carts API](./carts-api.md)` |
| 70 | `- [Users API](/.doc/users-api.md)` | `- [Users API](./users-api.md)` |
| 71 | `- [Auth API](/.doc/auth-api.md)` | `- [Auth API](./auth-api.md)` |
| 77 | `See [Project Structure](/.doc/project-structure.md)` | `See [Project Structure](./project-structure.md)` |

Lines 67–71 sit inside the commented-out "API Structure" block. They're still links, so they get fixed too.

```bash
sed -b -i -e 's#](/\.doc/#](./#g' -e 's#](\./docs/general-api\.md)#](./general-api.md)#' .doc/challenge.md
```

`-b` keeps the working copy's CRLF line endings, so git prints no line-ending warnings.

- [ ] **Step 3: Point the challenge pages' navigation at `challenge.md`**

Only the link targets change: the labels "Back to README" and "Previous: Read Me" stay. Name the nine pages explicitly; a `.doc/*.md` glob would also rewrite the personal files.

```bash
sed -b -i 's#\[Back to README\](\.\./README\.md)#[Back to README](./challenge.md)#' \
  .doc/auth-api.md .doc/carts-api.md .doc/frameworks.md .doc/general-api.md .doc/overview.md \
  .doc/products-api.md .doc/project-structure.md .doc/tech-stack.md .doc/users-api.md
sed -b -i 's#<a href="\.\./README\.md">Previous: Read Me</a>#<a href="./challenge.md">Previous: Read Me</a>#' .doc/overview.md
```

- [ ] **Step 4: Verify and stage the edits**

```bash
git grep -n -E 'Back to README|Previous: Read Me' -- .doc/
git grep -n -E '\]\(\.\./README\.md\)|href="\.\./README\.md"' -- .doc/ || echo "no links to ../README.md left"
git add .doc/auth-api.md .doc/carts-api.md .doc/challenge.md .doc/frameworks.md .doc/general-api.md \
  .doc/overview.md .doc/products-api.md .doc/project-structure.md .doc/tech-stack.md .doc/users-api.md
git diff --cached -M --stat
```

Expected:

```
.doc/auth-api.md:1:[Back to README](./challenge.md)
.doc/carts-api.md:1:[Back to README](./challenge.md)
.doc/frameworks.md:1:[Back to README](./challenge.md)
.doc/general-api.md:1:[Back to README](./challenge.md)
.doc/overview.md:1:[Back to README](./challenge.md)
.doc/overview.md:34:  <a href="./challenge.md">Previous: Read Me</a>
.doc/products-api.md:1:[Back to README](./challenge.md)
.doc/project-structure.md:1:[Back to README](./challenge.md)
.doc/tech-stack.md:1:[Back to README](./challenge.md)
.doc/users-api.md:1:[Back to README](./challenge.md)
no links to ../README.md left
```

The stat shows `README.md => .doc/challenge.md | 18 +++++++++---------`, eight pages at `2 +-`, `.doc/overview.md | 4 ++--` and `10 files changed, 19 insertions(+), 19 deletions(-)`.

`.doc/project-structure.md` line 11 (`└── README.md`) is tree-diagram text, not a link. It's still correct, because the project README stays at the root.

- [ ] **Step 5: Run the link check**

```bash
bash /tmp/ticket01-checks/check-links.sh > /tmp/ticket01-checks/links.out; echo "exit=$?"
grep -c '^ok' /tmp/ticket01-checks/links.out
grep -E 'BROKEN|broken links' /tmp/ticket01-checks/links.out
```

Expected: `exit=0`, `34` and `broken links: 0`. There's no `README.md` until Task 5, so this run covers the ten `.doc` pages only.

- [ ] **Step 6: Commit**

```bash
git commit -m "docs: move challenge text to .doc/challenge.md" -m 'The challenge statement leaves README.md, which becomes the project README, and joins the pages it links to. Its links are now page-relative, and the "Back to README" and "Previous: Read Me" links in the .doc pages point at challenge.md.'
```

- [ ] **Step 7: Verify the rename and the content**

```bash
git show --format= --name-status -M HEAD | grep challenge
git diff -U0 develop:README.md HEAD:.doc/challenge.md | grep -E '^[-+]' | grep -vE '^(---|\+\+\+) '
git log --follow --oneline -- .doc/challenge.md
```

Expected: `R086	README.md	.doc/challenge.md` (any `R` score means git recorded the rename), then exactly these 18 lines:

```
-See [Overview](/.doc/overview.md)
+See [Overview](./overview.md)
-See [Tech Stack](/.doc/tech-stack.md)
+See [Tech Stack](./tech-stack.md)
-See [Frameworks](/.doc/frameworks.md)
+See [Frameworks](./frameworks.md)
-- [API General](./docs/general-api.md)
-- [Products API](/.doc/products-api.md)
-- [Carts API](/.doc/carts-api.md)
-- [Users API](/.doc/users-api.md)
-- [Auth API](/.doc/auth-api.md)
+- [API General](./general-api.md)
+- [Products API](./products-api.md)
+- [Carts API](./carts-api.md)
+- [Users API](./users-api.md)
+- [Auth API](./auth-api.md)
-See [Project Structure](/.doc/project-structure.md)
+See [Project Structure](./project-structure.md)
```

The log shows two lines: this commit, then `c3590f8 Initial commit`. (`develop:README.md` is still the original challenge text at this point, because `develop` hasn't been merged yet.)

---

### Task 5: Add the placeholder project README

**Files:**
- Create: `README.md`

- [ ] **Step 1: Write `README.md` with exactly this content**

```markdown
# DeveloperStore Sales API

A prototype Sales API for the Ambev DeveloperStore developer evaluation, built on the .NET 8 template the challenge provides. It records sales with complete CRUD, applies the quantity-based discount rules inside a DDD aggregate, requires a JWT on every sales endpoint, and writes the `SaleCreated`, `SaleModified`, `SaleCancelled` and `ItemCancelled` events to the application log.

The challenge statement is in [.doc/challenge.md](.doc/challenge.md).
```

Ticket 13 replaces it with the full README.

- [ ] **Step 2: Run the link check (GREEN)**

```bash
bash /tmp/ticket01-checks/check-links.sh > /tmp/ticket01-checks/links.out; echo "exit=$?"
grep -c '^ok' /tmp/ticket01-checks/links.out
grep -E 'README.md ->|broken links' /tmp/ticket01-checks/links.out
```

Expected: `exit=0`, `35`, `ok      README.md -> .doc/challenge.md` and `broken links: 0`.

- [ ] **Step 3: Commit**

```bash
git add README.md
git commit -m "docs: add placeholder project README"
```

---

### Task 6: Verify everything, then open a pull request into `develop`

- [ ] **Step 1: Re-run every acceptance check fresh (superpowers:verification-before-completion)**

```bash
git status --short
bash /tmp/ticket01-checks/check-layout.sh | tail -1
bash /tmp/ticket01-checks/check-docker-paths.sh | tail -1
bash /tmp/ticket01-checks/check-links.sh | tail -1
git log --follow --oneline -- src/Ambev.DeveloperEvaluation.WebApi/Program.cs | tail -1
git log --follow --oneline -- .doc/challenge.md | tail -1
dotnet build Ambev.DeveloperEvaluation.sln 2>&1 | grep -E 'Error\(s\)|Build succeeded'
dotnet test Ambev.DeveloperEvaluation.sln --no-build 2>&1 | grep -E 'Passed!|Failed!'
dotnet build 2>&1 | grep -c MSB1011
```

Expected: `git status --short` lists only `?? docs/superpowers/tickets/`, and each ticket criterion has its evidence:

| Ticket criterion | Evidence |
|---|---|
| `template/` is gone and the root entries are in place | `layout problems: 0` |
| Moves recorded as renames; `--follow` reaches the initial commit | both `--follow` lines are `c3590f8 Initial commit`, plus the `116` R100 renames from Task 3 Step 11 |
| Challenge at `.doc/challenge.md`, links resolve, navigation points at `challenge.md` | `broken links: 0`, plus the 18-line diff from Task 4 Step 7 |
| Placeholder `README.md` | `git show HEAD:README.md` prints the Task 5 text |
| The `.sln` builds and the 49 tests pass; a bare build gives MSB1011 | `Build succeeded.` with `0 Error(s)`; `Passed:    49`; the MSB1011 count is `1` |
| Compose paths still resolve | `missing paths: 0`, plus `docker compose config --quiet` if Docker is installed |
| Personal files left alone and excluded | Step 2 |
| Feature branch, pull request into `develop`, Conventional Commits, no attribution | Steps 3–5 |

- [ ] **Step 2: Confirm the personal files are untouched and still excluded**

```bash
sha256sum -c /tmp/ticket01-checks/personal.sha256
git check-ignore -q .doc/brainstorm-show-case.md && git check-ignore -q .doc/template-code-review.md && echo "both excluded"
git log --all --format= --name-only | grep -c -E 'brainstorm-show-case|template-code-review'
```

Expected: `.doc/brainstorm-show-case.md: OK`, `.doc/template-code-review.md: OK`, `both excluded` and `0`. If a hash differs, the user may have edited that file in the meantime: report it, and don't restore anything.

- [ ] **Step 3: Check the commit messages**

```bash
git log --format=%s develop..feature/repo-restructure
git log --format=%B ca69712..feature/repo-restructure | grep -n -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
```

Expected:

```
docs: add placeholder project README
docs: move challenge text to .doc/challenge.md
refactor: move template to repo root
no attribution lines
```

- [ ] **Step 4: Push the branch and open a pull request into `develop`**

This is the superpowers:finishing-a-development-branch step, with the option already fixed: a pull request into `develop` for the user to review. Don't merge it yourself.

Write the pull request body with the Write tool to `/tmp/ticket01-checks/pr-body.md`: two or three sentences on what the ticket delivers (the Goal above), the evidence table from Step 1 with this run's results, and the results of Steps 2 and 3. No `Co-Authored-By` trailer, no "Generated with" line and no session link. Then:

```bash
git push -u origin feature/repo-restructure
gh pr create --base develop --head feature/repo-restructure --title "Ticket 01: move the template to the repo root" --body-file /tmp/ticket01-checks/pr-body.md
```

Expected: the push prints `* [new branch]      feature/repo-restructure -> feature/repo-restructure`, and `gh pr create` prints the pull request URL, `https://github.com/PR001-git/developer-store-api/pull/<n>`. If `gh` fails (for example, it isn't logged in; check with `gh auth status`), stop and tell the user: the branch is pushed, so they can open the pull request on GitHub.

- [ ] **Step 5: Verify the pull request**

```bash
gh pr view feature/repo-restructure --json state,baseRefName,commits --jq '"\(.state) \(.baseRefName) \(.commits | length) commits"'
gh pr view feature/repo-restructure --json title,body --jq '.title + "\n" + .body' | grep -i -E 'co-authored-by|generated with|claude' || echo "no attribution lines"
git status -sb | head -1
git status --short
```

Expected: `OPEN develop 3 commits`, `no attribution lines`, `## feature/repo-restructure...origin/feature/repo-restructure` with no `ahead` or `behind`, and only `?? docs/superpowers/tickets/`.

Give the user the pull request URL when the run ends. Don't merge it: the user reviews it and merges it on GitHub with **Create a merge commit** (the `--no-ff` equivalent that ticket 02's starting-point check looks for). Keep `feature/repo-restructure`, since the ticket doesn't ask to delete it. Task 7 runs on this branch while the pull request waits.

---

### Task 7: Create the slopwatch baseline at the new root (local only)

**Skill:** `dotnet-slopwatch` (in `.claude/skills/`). Load it before this task.

**Files:**
- Create (git-excluded, never committed): `.slopwatch/baseline.json`, `.slopwatch/config.json.example`

- [ ] **Step 1: Create the baseline**

```bash
slopwatch init
```

Expected: `Slopwatch initialized successfully!` and `Entries:  0`, because the template has no disabled tests, suppressed warnings or empty catch blocks. Ignore the tool's "Commit .slopwatch/baseline.json" suggestion: `.slopwatch/` stays local.

- [ ] **Step 2: Confirm the baseline stays out of git, and analyze**

```bash
git check-ignore -v .slopwatch/baseline.json
git status --short
slopwatch analyze --stats
```

Expected: a `.git/info/exclude:<line>:.slopwatch/` match for `.slopwatch/baseline.json`; `git status --short` still lists only `?? docs/superpowers/tickets/`; and `Scan complete: 0 issue(s) found` with `Stats: 100 files analyzed` (about 100).

- [ ] **Step 3: Remove the scratch files**

```bash
test -f .claude/settings.local.json && ls .claude/skills | wc -l
rm -rf /tmp/ticket01-checks
```

Expected: a non-zero skill count (16 on 2026-09-25), which confirms `.claude/` survived and the backup isn't needed. The `rm` prints nothing.
