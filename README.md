# ASP.NET Core Tutorial

A hands-on ASP.NET Core MVC + Razor Pages tutorial, server-rendered (no REST API, no SPA
frontend), built around **ASP.NET Core Identity** for authentication and **EF Core** over
**PostgreSQL** for persistence. Originally built on .NET 6 / SQL Server, migrated to
**.NET 10 (LTS)** and **PostgreSQL**, then containerized with Docker and wired to a GitHub
Actions CI pipeline.

The data model follows this schema: `categories` -> `products` -> `orders` <- `customers`.

Repository: https://github.com/EdgarEldy/aspnet_core_tutorial

> This README is the living reference for the project's state and remaining work (see
> [Roadmap](#roadmap)).

## Table of contents

- [Tech stack](#tech-stack)
- [Data model](#data-model)
- [Branching strategy](#branching-strategy)
- [Project structure](#project-structure)
- [feature/core-architecture](#featurecore-architecture)
- [feature/products](#featureproducts)
- [feature/customers](#featurecustomers)
- [Roadmap](#roadmap)
- [Getting started](#getting-started)
- [Further reading](#further-reading)

## Tech stack

| Component | Choice |
|---|---|
| Framework | ASP.NET Core MVC + Razor Pages |
| Language | C# / .NET 10 (LTS) |
| Rendering | Server-rendered views (Razor), no REST API layer |
| Database | PostgreSQL 16 |
| ORM | Entity Framework Core 10 (Npgsql provider) |
| Authentication | ASP.NET Core Identity (cookie-based) |
| Containerization | Docker (multi-stage, non-root runtime user), docker-compose |
| Database administration | pgAdmin, run externally (not part of docker-compose) |
| CI/CD | GitHub Actions |
| Image security | Trivy (container vulnerability scanning) |

## Data model

```
categories (Id, CategoryName, CreatedAt, UpdatedAt)
    |  1
    |
    |  N
products (Id, CategoryId, ProductName, UnitPrice, CreatedAt, UpdatedAt)
    |  1
    |
    |  N
orders (Id, CustomerId, ProductId, Quantity, Total, CreatedAt, UpdatedAt)
    |  N
    |
    |  1
customers (Id, FirstName, LastName, Telephone, Email, Address, CreatedAt, UpdatedAt)
```

`Order.CustomerId` and `Order.ProductId` are nullable foreign keys in the current model
(`Models/Order.cs`), so an order can technically exist without a resolved customer or product
at the database level; the business rule enforcing both at the application layer is expected to
land with the `Products`/`Customers`/`Orders` feature work (see [Roadmap](#roadmap)).

ASP.NET Core Identity adds its own schema alongside these tables (`AspNetUsers`, `AspNetRoles`,
`AspNetUserRoles`, etc.), managed by `ApplicationDbContext : IdentityDbContext` in
`Data/ApplicationDbContext.cs` and applied through the same EF Core migrations.

### Column details

**Category** (`Models/Category.cs`)
| Column | Type | Constraints |
|---|---|---|
| Id | int | PK, identity |
| CategoryName | varchar(100) | |
| CreatedAt | timestamp | |
| UpdatedAt | timestamp | |

**Product** (`Models/Product.cs`)
| Column | Type | Constraints |
|---|---|---|
| Id | int | PK, identity |
| CategoryId | int? | FK -> Categories.Id, nullable |
| ProductName | varchar(100) | |
| UnitPrice | double | |
| CreatedAt | timestamp | |
| UpdatedAt | timestamp | |

**Customer** (`Models/Customer.cs`)
| Column | Type | Constraints |
|---|---|---|
| Id | int | PK, identity |
| FirstName | varchar(255) | |
| LastName | varchar(255) | |
| Telephone | varchar(50) | |
| Email | varchar(255) | |
| Address | varchar(255) | |
| CreatedAt | timestamp | |
| UpdatedAt | timestamp | |

**Order** (`Models/Order.cs`)
| Column | Type | Constraints |
|---|---|---|
| Id | int | PK, identity |
| CustomerId | int? | FK -> Customers.Id, nullable |
| ProductId | int? | FK -> Products.Id, nullable |
| Quantity | int | |
| Total | double | |
| CreatedAt | timestamp | |
| UpdatedAt | timestamp | |

## Branching strategy

| Branch | Role |
|---|---|
| `master` | Stable branch, integration target once `develop` is validated. |
| `develop` | Integration branch for feature work merged after `master` was still on .NET 6 / SQL Server. |
| `feature/migration-dotnet10-postgres` | One-time migration branch (the .NET 6 -> .NET 10, SQL Server -> PostgreSQL swap). Branched from `feature/config`. |
| `feature/core-architecture` | Continues directly from `feature/migration-dotnet10-postgres`: technical foundation for the modernized stack (Docker, docker-compose, CI). Named after the equivalent branch in the sibling `spring-boot-tutorial` project, since it plays the same role: architecture/infrastructure skeleton merged first, before feature branches. |
| `feature/config`, `feature/templating`, `feature/data-modeling` | Pre-migration tutorial branches (configuration, Razor layout, initial data modeling), already merged into `develop`/`master` before the .NET 10 / PostgreSQL migration started. |
| `feature/products` | `Category` and `Product` CRUD, built on `feature/core-architecture`. The former `feature/categories` branch was merged into it and retired: its pre-migration history never diverged from `feature/products`, so keeping both was redundant. |
| `feature/customers` | `Customer` CRUD, built on `feature/products` per the plan's chained-branch topology. Its schema was aligned to the project's canonical `customers` table (dropped an unplanned `Pays`/country column, renamed `Tel` to `Telephone`). |
| `feature/orders` | Pending: `Order` CRUD, to be built on `feature/customers` once it merges to `develop`. |
| `feature/auth` | Pre-migration tutorial branch, base of the original branch history (ASP.NET Core Identity wiring). |

See `MIGRATION_LOG.md` for the full branch dependency graph, the merge order used to reconcile
pre-migration branches with the .NET 10 / PostgreSQL base, and how that analysis was actually
carried out.

## Project structure

```
aspnet_core_tutorial/
├── Areas/
│   └── Identity/
│       ├── IdentityHostingStartup.cs
│       └── Pages/
│           └── Account/ (Login, Register, scaffolded ASP.NET Core Identity UI)
├── Controllers/
│   ├── HomeController.cs
│   ├── CategoriesController.cs
│   ├── ProductsController.cs
│   └── CustomersController.cs
├── Data/
│   └── ApplicationDbContext.cs      (IdentityDbContext + Category/Product/Customer/Order DbSets)
├── Infrastructure/
│   └── GlobalExceptionHandler.cs    (IExceptionHandler, logs every unhandled exception)
├── Migrations/                      (EF Core migrations, PostgreSQL/Npgsql-native)
├── Models/
│   ├── Category.cs
│   ├── Product.cs
│   ├── Customer.cs
│   ├── Order.cs
│   ├── PaginatedList.cs             (generic pagination helper used by list views)
│   └── ErrorViewModel.cs
├── Seeders/                         (static Seed(app) methods called from Program.cs at startup)
├── Views/
│   ├── Home/
│   ├── Categories/
│   ├── Products/
│   ├── Customers/
│   ├── Layouts/
│   ├── Partials/
│   └── Shared/
├── wwwroot/                         (static assets: css, js, fonts)
├── .github/
│   └── workflows/
│       └── ci.yml
├── Dockerfile                       (multi-stage: SDK 10 build, ASP.NET 10 runtime, non-root user)
├── docker-compose.yml               (app + postgres)
├── .dockerignore
├── .env.example                     (placeholder values, copy to .env for local use)
├── appsettings.json
├── appsettings.Development.json
├── aspnet_core_tutorial.csproj
├── MIGRATION_LOG.md                 (account of how Etapes 0-2 were actually executed)
└── README.md
```

`Category`, `Product`, and `Customer` have full CRUD (controllers, views, seeders); `Order` still
only exists as an EF Core model pending its own `feature/orders` branch per the roadmap below.

## feature/core-architecture

Technical foundation for the modernized stack: .NET 10 / PostgreSQL migration, containerization,
and CI, merged first so every subsequent feature branch builds on a working, tested base.

### Tasks

- [x] Etape 0: reconcile the pre-migration branch topology, create
  `feature/migration-dotnet10-postgres` from `feature/config` (see `MIGRATION_LOG.md`)
- [x] Etape 1: upgrade to `.NET 10`, enable `Nullable`/`ImplicitUsings`, update every
  `Microsoft.AspNetCore.*`/`Microsoft.EntityFrameworkCore.*` package to its current 10.x version
- [x] Etape 2: replace `Microsoft.EntityFrameworkCore.SqlServer`/`.Sqlite` with
  `Npgsql.EntityFrameworkCore.PostgreSQL`, update `Program.cs` (`UseNpgsql`), regenerate
  `Migrations/` from scratch against PostgreSQL
- [x] Etape 3: multi-stage `Dockerfile` (SDK 10 build stage, ASP.NET 10 runtime stage, non-root
  `app` user), `.dockerignore`, `docker-compose.yml` (`app` + `postgres`, `.env`/`.env.example`
  for secrets, `pg_isready` healthcheck, `depends_on: condition: service_healthy`)
- [x] Etape 6 (anticipated): `.github/workflows/ci.yml` — build job (restore + build in Release,
  NuGet cache via `setup-dotnet`, a `postgres:16` service matching `docker-compose.yml`'s image
  and variable names for the future integration-test job), `docker-build` job (`docker build .`),
  `docker-scan` job (Trivy, fails the build on CRITICAL/HIGH vulnerabilities)
- [x] Branch README section explaining the configuration choices (this section)

### Configuration notes

- **Database administration stays outside docker-compose.** `docker-compose.yml` only declares
  `app` and `postgres`; pgAdmin (or any PostgreSQL client) is run externally, pointed at the
  published `postgres` port, so there's no extra admin UI service to maintain credentials for.
- **Secrets never hardcoded.** `docker-compose.yml` interpolates every credential from `.env`
  (`${POSTGRES_DB}`, `${POSTGRES_USER}`, `${POSTGRES_PASSWORD}`), which is git-ignored;
  `.env.example` documents the same keys with placeholder/empty values. In CI, the equivalent
  values are sourced from GitHub Actions secrets (`${{ secrets.CI_POSTGRES_PASSWORD }}`), never
  inlined in `ci.yml`.
- **Non-root runtime user reuses the base image's built-in account.** The
  `mcr.microsoft.com/dotnet/aspnet:10.0` image ships a dedicated non-root `app` user since
  .NET 8; the `Dockerfile` reuses it (`USER app`) instead of creating a new one, avoiding
  uid/gid collisions with accounts already present in the base image.
- **`postgres` healthcheck gates `app` startup.** `docker-compose.yml`'s `app` service depends on
  `postgres` with `condition: service_healthy` (backed by `pg_isready`), not just container
  start order, so the app never races PostgreSQL's initialization.
- **CI's `postgres:16` service is declared but not yet consumed by a real test job.** Etapes 4-5
  (unit/integration tests) don't exist on this branch yet. The service is still wired up now,
  with the exact same image and `POSTGRES_DB`/`POSTGRES_USER`/`POSTGRES_PASSWORD` variable names
  as `docker-compose.yml`, so local Docker and CI never drift into two different Postgres
  configurations, and adding the `integration-test` job later is a drop-in change rather than a
  redesign. `POSTGRES_HOST_AUTH_METHOD: trust` lets the service container start even before the
  `CI_POSTGRES_PASSWORD` secret is configured in the repository; it only applies to this
  ephemeral, network-isolated CI container, never to `docker-compose.yml`.
- **CI jobs stay granular.** `build`, `docker-build`, and `docker-scan` are separate jobs
  (`docker-build`/`docker-scan` chained via `needs:`) so a failure is easy to attribute to a
  single stage; `test`/`integration-test` jobs are intentionally left out until Etapes 4-5 add
  real test projects, rather than referencing projects that don't exist yet.

## feature/products

`Category` and `Product` CRUD, built directly on `feature/core-architecture`. Originally split
into a separate `feature/categories` branch (the pre-migration tutorial history for the two
entities never actually diverged from each other), merged into `feature/products` and retired
once confirmed fully redundant.

### Tasks

- [x] `CategoriesController`: full CRUD (`Index`, `Create`, `Edit`, `Delete`) - already complete
  from the pre-migration branch, re-validated against PostgreSQL
- [x] `ProductsController`: completed the missing `Create` (POST), `Edit`, and `Delete` actions -
  only `Index` and a `Create` GET existed before, so the create form had nothing to submit to and
  there was no way to update or remove a product
- [x] Fixed a startup crash: the seeders wrote `DateTime.Now` (`Kind=Local`) into `timestamp with
  time zone` columns, which Npgsql rejects outright - switched to `DateTime.UtcNow`
- [x] Search and pagination on both list views (`PaginatedList<T>` helper, `AsNoTracking()` since
  the lists are read-only), anticipated from Etape 7 for the same reason as
  `feature/core-architecture`'s cross-cutting items
- [x] Etape 4/5 test coverage for everything above: 50 unit tests (InMemory) plus 30 integration
  tests (real Postgres via Testcontainers, real antiforgery tokens, no bypasses) - 80 tests, full
  suite runs in well under a minute

### Configuration notes

- **`PaginatedList<T>`** (`Models/PaginatedList.cs`) is a thin `List<T>` subclass carrying
  `PageIndex`/`TotalPages`, following the standard ASP.NET Core MVC pagination pattern - no
  external paging library needed for a page count this small.
- **Search is a simple `Contains()` filter** on `CategoryName`/`ProductName`, applied before
  `OrderBy` and pagination so the count and page split are computed against the filtered set, not
  the full table.
- **All list/detail reads use `AsNoTracking()`**; only the single entity fetched in `Edit`/`Delete`
  before a write stays tracked.

## feature/customers

`Customer` CRUD, built directly on `feature/products` per the plan's chained-branch topology
(`feature/customers` -> `feature/products` -> `feature/core-architecture`).

### Tasks

- [x] Aligned `Models/Customer.cs` to the project's canonical `customers` schema: dropped an
  unplanned `Pays` (country) column that had been added speculatively, renamed `Tel` to
  `Telephone`, resized `FirstName`/`LastName`/`Email`/`Address` to `varchar(255)` and `Telephone`
  to `varchar(50)` - via a data-preserving `RenameColumn` migration, not a drop-and-recreate
- [x] `CustomersController`: full CRUD (`Index`, `Create`, `Edit`, `Delete`), mirroring the
  `CategoriesController`/`ProductsController` pattern exactly (fetch-then-patch on `Edit` to
  preserve `CreatedAt`, `AsNoTracking()` on reads)
- [x] Search and pagination on the `Index` view (`PaginatedList<T>`, filtering on
  `FirstName`/`LastName`)
- [x] Etape 4/5 test coverage: 15 unit tests (InMemory) plus 12 integration tests (real Postgres
  via Testcontainers, real antiforgery tokens) - all green alongside the existing suite

### Configuration notes

- **Schema followed the canonical `customers` table, not the earlier speculative `Pays`
  addition.** A `Pays` column had been cherry-picked from pre-migration history before the
  canonical schema was confirmed; once confirmed, it showed no country field, so it was removed
  and `Tel` was renamed to `Telephone` to match exactly.
- **The `Tel` -> `Telephone` migration renames rather than drops and recreates the column**, so
  any pre-existing customer data survives the schema change instead of being lost.

## Roadmap

The .NET 10 / PostgreSQL migration and the initial technical foundation are done. This section
is the living task list for what's left. Check items off as they land, in order, one branch/PR
per group unless noted otherwise.

### Cross-cutting foundation (anticipated on `feature/core-architecture`, ahead of the original
plan's Etape 7, to match the sibling `spring-boot-tutorial` project's structure)

- [x] Centralized exception handling middleware, with unhandled-exception logging
- [x] Structured logging (Serilog: console + rolling file, per-environment levels)
- [x] `/health` endpoint backed by a real PostgreSQL connection check
- [x] Swagger / OpenAPI, exposed in dev only
- [x] `docker-compose.yml`'s `app` service healthcheck wired to `/health`

### Etape 4 - Unit tests

- [x] `aspnet_core_tutorial.UnitTests` project (xUnit, `Microsoft.EntityFrameworkCore.InMemory`)
- [x] Controller tests (`CategoriesController`, `ProductsController`, `CustomersController`,
  `HomeController`): nominal + error cases (entity not found -> 404)
- [x] Seeder tests: no duplicate seeding on repeated runs
- [x] Model validation tests (Data Annotations)

### Etape 5 - Integration tests

- [x] `aspnet_core_tutorial.IntegrationTests` project (`Microsoft.AspNetCore.Mvc.Testing` +
  `Testcontainers.PostgreSql`, one real ephemeral PostgreSQL container shared for the whole run
  rather than one per test, so the suite stays fast)
- [x] Home page renders (200 OK)
- [x] Full CRUD over HTTP for `Products`, `Categories`, and `Customers`, with the real antiforgery
  token
- [x] Authentication redirects (`/Identity/Account/Manage` -> `/Identity/Account/Login` when
  signed out; `Categories`/`Products` carry no `[Authorize]` yet, so there's no business-CRUD
  redirect case to test until that lands)
- [x] EF Core migrations apply automatically at container startup

### Etape 6 - CI completion

- [ ] Wire the `test`/`integration-test` jobs into `.github/workflows/ci.yml` once the projects
  above exist (the `build` job's `postgres:16` service is already in place for this)
- [ ] Publish test results as run summaries/artifacts (`dotnet test --logger trx` + upload, or
  `dorny/test-reporter`)

### Etape 7 - Remaining hardening

- [ ] Service/repository layer extraction out of controllers
- [ ] Stronger Data Annotations on `Category`/`Product`/`Customer`/`Order`, surfaced in Razor views
- [x] Pagination on `Products`/`Categories` lists (see `feature/products`); still needed on `Orders`
  once that entity's CRUD lands
- [ ] Rate limiting, security headers (`X-Content-Type-Options`, `Content-Security-Policy`),
  anti-forgery checks on every POST form
- [ ] `dotnet list package --vulnerable` audit (local and/or CI)
- [ ] `AsNoTracking()` on read-only queries, response compression, `IMemoryCache` for low-churn
  data (categories)
- [ ] Per-environment `appsettings.Staging.json`/`appsettings.Production.json`, plus
  `docker-compose.override.yml` (dev) and `docker-compose.prod.yml` (prod, minimal exposed ports)

## Getting started

### Prerequisites

- .NET 10 SDK
- Docker + Docker Compose (for the containerized workflow)
- A PostgreSQL 16 instance (local install, or via `docker compose up postgres`)

### Local development, without Docker

```bash
# Restore and build
dotnet restore
dotnet build

# Apply EF Core migrations against a local PostgreSQL instance
# (adjust ConnectionStrings:DefaultConnection in appsettings.json first)
dotnet ef database update

# Run the app
dotnet run
```

### With Docker Compose

```bash
# One-time setup: copy the example env file and fill in real values
cp .env.example .env

# Build and start the app + PostgreSQL
docker compose up --build
```

The app is served on `http://localhost:8084`. To inspect the database, point an externally-run
pgAdmin (or any PostgreSQL client) at `localhost:5433` with the credentials from `.env`.

### Validating the setup

```bash
# Catch YAML/interpolation errors in docker-compose.yml without starting containers
docker compose config

# Validate the Dockerfile builds end to end
docker build .
```

## Further reading

- `MIGRATION_LOG.md` - a detailed account of how the .NET 10 upgrade, the PostgreSQL swap, and
  the branch topology reconciliation were actually executed, including the pitfalls found along
  the way (e.g. a `.gitignore` rule silently excluding `Migrations/` from version control).
