# ASP.NET Core Tutorial

A hands-on ASP.NET Core MVC + Razor Pages tutorial, server-rendered (no REST API, no SPA
frontend), built around **ASP.NET Core Identity** for authentication and **EF Core** over
**PostgreSQL** for persistence. Originally built on .NET 6 / SQL Server, migrated to
**.NET 10 (LTS)** and **PostgreSQL**, then containerized with Docker and wired to a GitHub
Actions CI pipeline.

The data model follows this schema: `categories` -> `products` -> `orders` <- `customers`.

Repository: https://github.com/EdgarEldy/aspnet_core_tutorial

## Table of contents

- [Tech stack](#tech-stack)
- [Data model](#data-model)
- [Branching strategy](#branching-strategy)
- [Project structure](#project-structure)
- [feature/core-architecture](#featurecore-architecture)
- [Roadmap](#roadmap)
- [Getting started](#getting-started)
- [Migration reference](#migration-reference)

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
customers (Id, FirstName, LastName, Tel, Email, Address, CreatedAt, UpdatedAt)
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
| FirstName | varchar(100) | |
| LastName | varchar(100) | |
| Tel | varchar(100) | |
| Email | varchar(100) | |
| Address | varchar(100) | |
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
| `feature/migration-dotnet10-postgres` | One-time migration branch (Etapes 0-2 of `MIGRATION_PLAN.md`): .NET 6 -> .NET 10, SQL Server -> PostgreSQL. Branched from `feature/config`. |
| `feature/core-architecture` | Continues directly from `feature/migration-dotnet10-postgres`: technical foundation for the modernized stack (Docker, docker-compose, CI). Named after the equivalent branch in the sibling `spring-boot-tutorial` project, since it plays the same role: architecture/infrastructure skeleton merged first, before feature branches. |
| `feature/config`, `feature/templating`, `feature/data-modeling` | Pre-migration tutorial branches (configuration, Razor layout, initial data modeling), already merged into `develop`/`master` before the .NET 10 / PostgreSQL migration started. |
| `feature/categories`, `feature/customers`, `feature/products`, `feature/orders` | Pre-migration tutorial branches implementing CRUD for each entity on .NET 6 / SQL Server. Re-validated against .NET 10 / PostgreSQL as part of the Etape 0 merge order in `MIGRATION_PLAN.md`. |
| `feature/auth` | Pre-migration tutorial branch, base of the original branch history (ASP.NET Core Identity wiring). |

See `MIGRATION_PLAN.md` (Etape 0) for the full branch dependency graph and the merge order used
to reconcile pre-migration branches with the .NET 10 / PostgreSQL base, and `MIGRATION_LOG.md`
for how that analysis was actually carried out.

## Project structure

```
aspnet_core_tutorial/
├── Areas/
│   └── Identity/
│       ├── IdentityHostingStartup.cs
│       └── Pages/
│           └── Account/ (Login, Register, scaffolded ASP.NET Core Identity UI)
├── Controllers/
│   └── HomeController.cs
├── Data/
│   └── ApplicationDbContext.cs      (IdentityDbContext + Category/Product/Customer/Order DbSets)
├── Migrations/                      (EF Core migrations, PostgreSQL/Npgsql-native)
├── Models/
│   ├── Category.cs
│   ├── Product.cs
│   ├── Customer.cs
│   ├── Order.cs
│   └── ErrorViewModel.cs
├── Views/
│   ├── Home/
│   ├── Layouts/
│   ├── Partials/
│   └── Shared/
├── wwwroot/                         (static assets: css, js, fonts)
├── .github/
│   └── workflows/
│       └── ci.yml
├── Dockerfile                       (multi-stage: SDK 10 build, ASP.NET 10 runtime, non-root user)
├── docker-compose.yml               (app + postgres, no Adminer)
├── .dockerignore
├── .env.example                     (placeholder values, copy to .env for local use)
├── appsettings.json
├── appsettings.Development.json
├── aspnet_core_tutorial.csproj
├── MIGRATION_PLAN.md                (step-by-step migration/modernization plan)
├── MIGRATION_LOG.md                 (account of how Etapes 0-2 were actually executed)
└── README.md
```

`Controllers/`, `Views/`, and `Models/` currently only cover the Home/Identity scaffolding plus
the four business entities; the CRUD controllers and views for `Category`/`Product`/`Customer`/
`Order` land with their respective `feature/*` branches per the roadmap below. There is no
`Seeders/` folder yet on this branch for the same reason.

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

- **No Adminer in `docker-compose.yml`.** Unlike the original Etape 3 plan text in
  `MIGRATION_PLAN.md` (which mentions Adminer), the actual `docker-compose.yml` on this branch
  only declares `app` and `postgres`. pgAdmin is used externally (outside of docker-compose) for
  database administration, so an in-compose admin UI would be redundant. If this changes later,
  add the service back with its own healthcheck and no hardcoded credentials.
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

## Roadmap

Remaining `MIGRATION_PLAN.md` steps, not yet implemented on this branch:

- **Etape 4** - Unit tests (xUnit, `Microsoft.EntityFrameworkCore.InMemory`) for controllers,
  seeders, and model validation.
- **Etape 5** - Integration tests (`Microsoft.AspNetCore.Mvc.Testing` + `Testcontainers.PostgreSql`)
  covering the main HTTP flows and authentication redirects.
- **Etape 6 (completion)** - Wire the `test`/`integration-test` CI jobs once the projects above
  exist; publish test results as run summaries/artifacts.
- **Etape 7** - Hardening pass: service/repository layer extraction out of controllers, stronger
  Data Annotations, centralized exception handling, Serilog structured logging, `/health` endpoint
  backed by PostgreSQL, pagination, rate limiting, security headers, anti-forgery checks on POST
  forms, `dotnet list package --vulnerable` audit, `AsNoTracking()`/response compression/
  `IMemoryCache`, `CreatedAt`/`UpdatedAt` auto-population, per-environment `appsettings.*.json`
  plus `docker-compose.override.yml` (dev) and `docker-compose.prod.yml` (prod, no Adminer,
  minimal exposed ports).

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
# (adjust ConnectionStrings:DefaultConnection in appsettings.Development.json first)
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

The app is served on `http://localhost:8080`. There is no Adminer service in
`docker-compose.yml`; use an externally-run pgAdmin (or any PostgreSQL client) pointed at
`localhost:5432` with the credentials from `.env` to inspect the database.

### Validating the setup

```bash
# Catch YAML/interpolation errors in docker-compose.yml without starting containers
docker compose config

# Validate the Dockerfile builds end to end
docker build .
```

## Migration reference

- `MIGRATION_PLAN.md` - the authoritative, step-by-step plan (Etape 0 through Etape 7), meant to
  be followed in order.
- `MIGRATION_LOG.md` - a detailed account of how Etapes 0-2 (branch topology reconciliation,
  .NET 10 upgrade, PostgreSQL swap) were actually executed, including the pitfalls found along
  the way (e.g. a `.gitignore` rule silently excluding `Migrations/` from version control).
