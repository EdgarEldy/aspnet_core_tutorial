# Migration log — .NET 6 → .NET 10 + SQL Server → PostgreSQL

> Reference document for future migrations of this kind on similar ASP.NET Core projects.
> Describes precisely what was done, in what order, with what tools, and why — not just the
> end result. Complements `MIGRATION_PLAN.md` (the plan) with the actual account of its
> execution (Étapes 0 to 2).

## Starting context

- `aspnet_core_tutorial` repo, ASP.NET Core MVC + Razor Pages (Identity), EF Core 6.0.26, with
  `Microsoft.EntityFrameworkCore.SqlServer` as the active provider and `Sqlite` as an unused
  secondary provider.
- Git history with many `feature/*` branches, some already merged, some still pending
  integration (see `MIGRATION_PLAN.md`, Étape 0).
- Target: `.NET 10` (LTS) + PostgreSQL, without breaking anything in downstream branches.

## Étape 0 — Analyze, then fix, the branch topology before migrating any code

**Why start here instead of jumping straight into the code.** Migrating the code on the wrong
base branch, or merging afterward in the wrong order, means redoing the work. The initial plan
documented a branch topology "observed at a point in time" — it turned out to be partially wrong
once actually verified.

1. **Never trust a documented branch graph without re-verifying it.** Run `git fetch --all`,
   then for every relevant branch pair run
   `git merge-base --is-ancestor origin/<A> origin/<B>` to confirm who is actually an ancestor
   of whom. Follow up with `git log <A> ^<B>` to tell "A is a Git ancestor of B" apart from "B
   actually contains all of A's useful work" (a branch can be "merged" in the Git sense through
   an old shared merge commit without ever having received the other side's recent commits).
2. **Real discrepancies found versus the documented plan** (delegated to a branch-analysis agent,
   read-only) :
   - A branch supposed to carry a feature (`feature/customers`) actually had zero commits of its
     own: its tip pointed at an old merge commit shared with another branch.
   - A branch marked "not yet merged" (`feature/orders`) turned out to be far more stale than
     expected: it had never received updates from any of the branches it was supposed to depend
     on.
   - A branch without the `feature/` prefix had exactly one useful commit, on a very old base,
     well before the rest of the repo's restructuring — it did not descend from the branch the
     plan claimed.
3. **Decision made with the user**: rather than replaying the divergent history as-is, the
   branches in question were **rebuilt** to match the intended dependency topology (`A` must
   source from `B`):
   - Create the branch from the correct, up-to-date base
     (`git checkout -b <branch> <up-to-date-base>`).
   - Cherry-pick only the genuinely useful commit(s) from the old branch
     (`git cherry-pick <sha>`), inspecting their content first (`git show <sha>`) to anticipate
     conflicts.
   - Resolve conflicts by keeping the file's evolution on the new base (e.g. a model that had
     gained new fields in the meantime) while still integrating the cherry-picked addition.
   - Validate with `dotnet build` after each cherry-pick, not only at the very end.
4. **Do everything locally before publishing anything.** None of these operations (branch
   recreation, cherry-picking, effectively rewriting a branch's history) were pushed to `origin`
   before explicit validation — a shared remote branch is a point of no return for other
   contributors, a local rebuild is not.
5. Create the actual migration branch at the end of this step, from the validated base branch:
   `git checkout -b feature/migration-dotnet10-postgres origin/feature/config`.

**Main takeaway**: treat branch analysis as investigative work in its own right, with
systematic empirical verification, before any merge decision — never just a reading of the plan.

## Étape 1 — .NET 6 → .NET 10 migration

A single commit, strictly scoped to the version bump (no functional change).

1. **`.csproj`**:
   ```xml
   <TargetFramework>net10.0</TargetFramework>
   <Nullable>enable</Nullable>
   <ImplicitUsings>enable</ImplicitUsings>
   ```
2. **Package versions: always checked at execution time, never guessed.** The NuGet API was
   queried for each package's actual latest stable version at build time, rather than trusting a
   number hardcoded in a plan written in advance (which can go stale). Result for this project
   (July 2026):
   - `Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore`: `6.0.26` → `10.0.9`
   - `Microsoft.AspNetCore.Identity.EntityFrameworkCore`: `6.0.26` → `10.0.9`
   - `Microsoft.AspNetCore.Identity.UI`: `6.0.26` → `10.0.9`
   - `Microsoft.EntityFrameworkCore`: `6.0.26` → `10.0.9`
   - `Microsoft.EntityFrameworkCore.Design`: `6.0.26` → `10.0.9`
   - `Microsoft.EntityFrameworkCore.Tools`: `6.0.26` → `10.0.9`
   - `Microsoft.VisualStudio.Web.CodeGeneration.Design`: `6.0.16` → `10.0.2`
3. **Breaking changes encountered, all tied to enabling `Nullable`, none tied to the .NET 6 → 10
   jump itself**:
   - ~24 nullable-reference warnings surfaced in the scaffolded Identity pages
     (`Areas/Identity/Pages/Account/Login.cshtml(.cs)`, `Register.cshtml.cs`) and in
     `Models/ErrorViewModel.cs` — fixed with standard nullable annotations/default values, no
     behavior change.
   - A `CS8602` false positive in Razor-generated code (`Login.cshtml`, accessing
     `Model.ExternalLogins` inside a nested `@{ }` block) resolved by hoisting the access into a
     local variable with a null-coalescing fallback, rather than suppressing the warning.
4. **Validation**: clean `dotnet build` (0 errors, 0 compiler warnings) before moving to the next
   step — never chain into Étape 2 on top of code that doesn't compile.

## Étape 2 — SQL Server/SQLite → PostgreSQL (Npgsql)

One commit, strictly scoped to the EF Core provider swap.

1. **`.csproj`**: removed `Microsoft.EntityFrameworkCore.SqlServer` and `.Sqlite`, added
   `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (verified compatible with EF Core's
   `10.0.4-10.x` dependency range at the time it was added, not assumed compatible by default).
2. **`Program.cs`**: a single-line change, `options.UseSqlServer(connectionString)` →
   `options.UseNpgsql(connectionString)`. Nothing else in the request pipeline was touched — any
   temptation to "take advantage of the change" to touch something else was avoided.
3. **`appsettings.json`**:
   - Connection string replaced with a Postgres one:
     `Host=localhost;Port=5432;Database=aspnet_core_db;Username=app_user;Password=`.
   - **No real password committed in clear text**: the `Password=` field is intentionally empty,
     meant to be overridden by an environment variable/`.env` (mechanism formalized in
     Étape 3/7 of the plan, see the `env-secrets` skill). Committing a connection string shape
     with no real secret inside is acceptable; committing a real password never is.
   - `SqliteConnection` key removed (no more secondary provider).
4. **Regenerating EF Core migrations from scratch**:
   - Full removal of the old `Migrations/` folder (SQL Server migrations aren't directly
     replayable against PostgreSQL — different column types and auto-increment strategy).
   - Updated the global `dotnet-ef` tool (`7.0.14` → `10.0.9`) to match the project's EF Core
     version **before** generating anything — a mismatched tool version causes confusing
     generation errors.
   - `dotnet ef migrations add InitialCreate`.
   - Manually inspected the generated content, not just whether it ran: confirmed Postgres-native
     types (`text`, `character varying(n)`, `boolean`, `timestamp with time zone`) and
     auto-incrementing keys via `Npgsql:ValueGenerationStrategy = IdentityByDefaultColumn`
     (a Postgres sequence) instead of SQL Server's `IDENTITY` strategy.
5. **Pitfall found: `.gitignore` was ignoring the entire `Migrations/` folder.** A `Migrations`
   rule inherited from the project's initial bootstrap was silently excluding migration files
   from version control. Worked around immediately with a forced add
   (`git add -f Migrations/`) so the work wasn't lost, then **fixed at the root** by removing the
   rule from `.gitignore` in a dedicated commit — EF Core migrations must be tracked by default
   so any clone (dev machine, CI) can apply them without a manual step. Habit worth keeping:
   check `git ls-files <folder>` after a `git add`, not just `git status`, to confirm a file
   meant to be tracked actually is, despite any pre-existing ignore rules.
6. **No seeders to adjust at this point.** No domain model (`Category`/`Product`/`Customer`/
   `Order`) nor `Seeders/` folder existed yet on the migration branch at this stage of Étape 0
   (they arrive via branches merged later): nothing to adjust for identifier casing or column
   types on the seed side at this precise step. Worth explicitly re-checking once those branches
   are merged into the migration branch.
7. **Validation**: clean `dotnet build` after each commit (`dotnet clean` + full rebuild to avoid
   a false positive from incremental caching). End-to-end validation
   (`dotnet ef database update` against a real PostgreSQL instance) was **not** done at this
   stage due to no PostgreSQL instance being available in the build environment — deferred to
   Étape 3 (docker-compose), not to be assumed already proven before that real test happens.

## Code review applied before considering the step done

A dedicated, read-only review agent was run over both commits before closing them out, with
explicit instructions to fix nothing itself — only report. One real blocking gap was found (the
`.gitignore` issue from point 5 above) and fixed separately; everything else (connection string
with no real password, no unjustified warning suppression, Postgres-idiomatic migration, no
out-of-scope change) was confirmed compliant.

**Takeaway to generalize**: have each step reviewed by an independent pass before closing it,
even when the author is convinced it's correct — this is exactly what surfaced the `.gitignore`
pitfall, which was invisible from just reading the migration commit's own diff.

## Key commands recap

```bash
# Checking a package's latest version (example, repeat for every future migration)
curl -s https://api.nuget.org/v3-flatcontainer/<lowercase-package-id>/index.json

# Updating the global dotnet-ef tool before regenerating migrations
dotnet tool update --global dotnet-ef

# Fully removing and regenerating migrations
rm -rf Migrations/
dotnet ef migrations add InitialCreate

# Clean validation (avoid incremental cache false positives)
dotnet clean && dotnet build
```

## Pitfalls to avoid next time

- Don't assume a migration plan written in advance still describes the real state of the
  branches at execution time — always re-verify empirically.
- Don't merge into, or push to, a shared remote branch before full local validation, even when
  the operation looks trivial.
- Never leave a package version as a guess — check the actual latest stable version at the time,
  including for a global tool like `dotnet-ef`.
- Verify that a generated folder (migrations, artifacts) is actually tracked by Git after
  generation, not just that it exists on disk — an inherited `.gitignore` rule can silently hide
  it.
- Have each step reviewed before closing it out, rather than only at the very end of the full
  migration.
