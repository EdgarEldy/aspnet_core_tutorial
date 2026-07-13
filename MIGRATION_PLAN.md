# Plan de migration & modernisation — aspnet_core_tutorial

> Ce document est destiné à être lu par Claude Code pour piloter la migration de ce projet.
> Chaque section est une tâche autonome avec un objectif clair et des critères de validation.
> Travailler section par section, dans l'ordre, en committant après chaque étape validée.

## Contexte actuel

- **Framework** : .NET 6 (`net6.0`)
- **Type de projet** : ASP.NET Core MVC + Razor Pages (Identity)
- **ORM** : Entity Framework Core 6.0.26
- **Base de données** : SQL Server (`Microsoft.EntityFrameworkCore.SqlServer`), avec un provider SQLite secondaire non utilisé activement
- **Fonctionnalités** : gestion de `Category`, `Product`, `Customer`, `Order`, authentification via ASP.NET Identity, seeders au démarrage
- **Pas de tests, pas de CI, pas de Docker**

## Objectifs de la migration

1. Migrer vers **.NET 10** (LTS)
2. Remplacer **SQL Server** par **PostgreSQL** (provider **Npgsql.EntityFrameworkCore.PostgreSQL**), supprimer le support SQLite
3. Ajouter **Docker** + **docker-compose** (app + PostgreSQL + Adminer)
4. Ajouter des **tests unitaires** et des **tests d'intégration**
5. Ajouter une **pipeline CI GitHub Actions**
6. Appliquer des améliorations de qualité/modernité du code (voir section dédiée)

---

## Étape 0 — Stratégie Git (branches existantes)

Le dépôt contient déjà plusieurs branches issues du développement initial du tutoriel. **Ne pas partir de `master` pour la migration.**

- [x] Avant toute chose, analyser le graphe réel des branches pour confirmer/actualiser les dépendances (les relations ci-dessous ont été observées à un instant T, à revérifier car l'historique peut évoluer) :
  ```bash
  git fetch --all
  git log --oneline --all --graph
  # Pour vérifier si A est un ancêtre de B :
  git merge-base --is-ancestor origin/<A> origin/<B> && echo "A est fusionné dans B"
  ```
- [x] État observé au moment de la rédaction de ce plan :
  - `feature/auth` est à la base de tout l'historique (ancêtre commun de toutes les branches sauf `customers`)
  - `feature/config` et `feature/data-modeling` sont deux branches indépendantes l'une de l'autre, toutes deux déjà fusionnées dans `feature/categories`, `feature/products`, `develop` et `master`
  - `feature/categories`, `feature/customers`, `feature/products` sont déjà fusionnées dans `develop` et `master`
  - `feature/orders`, `feature/templating` et `customers` (branche sans préfixe `feature/`) **ne sont pas encore fusionnées** dans `develop`/`master` — elles contiennent du travail en attente d'intégration
  - `develop` est fusionnée dans `master`
- [x] Créer la branche de migration **à partir de `feature/config`** :
  ```bash
  git checkout -b feature/migration-dotnet10-postgres origin/feature/config
  ```
- [x] Réaliser l'intégralité des étapes 1 à 2 (migration .NET 10 + PostgreSQL) sur cette branche `feature/migration-dotnet10-postgres`, valider que le build passe
- [x] Une fois la migration validée sur cette branche, la fusionner **dans l'ordre de dépendance réel** (celui confirmé par l'analyse du graphe, pas seulement celui listé ci-dessus) vers les branches qui descendent de `feature/config`, en allant de la plus "en amont" à la plus "en aval". Sur la base de l'état observé, l'ordre attendu est approximativement :
  1. `feature/config`
  2. `feature/templating`
  3. `feature/data-modeling`
  4. `feature/categories`
  5. `feature/customers`
  6. `feature/products`
  7. `feature/orders`
  8. `customers` prend source sur `products` et `orders` prend source sur `customers` (à traiter avec prudence : cette branche semble avoir un historique disjoint, vérifier avant de fusionner si elle a un ancêtre commun exploitable ou si elle nécessite un traitement séparé/rebasage)
  8. `develop`
  9. `master`
- [x] Après chaque fusion, relancer le build (et les tests dès qu'ils existent) avant de passer à la branche suivante ; résoudre les conflits localement à chaque étape plutôt que d'accumuler les conflits
- [x] Une fois `feature/migration-dotnet10-postgres` intégrée jusqu'à `develop`/`master`, poursuivre avec les étapes 3 à 7 (Docker, tests, CI, améliorations) directement sur `develop`, avec des sous-branches `feature/xxx` dédiées par sujet (ex: `feature/docker-compose`, `feature/unit-tests`, `feature/ci-pipeline`, `feature/security-hardening`) fusionnées ensuite dans `develop` puis `master`, en conservant la même discipline (une branche = un sujet = une PR)

**Critère de validation** : aucune branche active du dépôt ne reste basée sur .NET 6 / SQL Server après cette étape ; l'historique Git reste lisible (pas de fusions non résolues, pas de conflits laissés en l'état).

---



## Étape 1 — Migration vers .NET 10

- [x] Mettre à jour `aspnet_core_tutorial.csproj` : `<TargetFramework>net10.0</TargetFramework>`
- [x] Activer `<Nullable>enable</Nullable>` et `<ImplicitUsings>enable</ImplicitUsings>` si absents
- [x] Mettre à jour tous les packages `Microsoft.AspNetCore.*` / `Microsoft.EntityFrameworkCore.*` / `Microsoft.VisualStudio.Web.CodeGeneration.Design` vers les versions compatibles .NET 10 (versions majeures 10.x, vérifier sur NuGet les dernières versions stables au moment du build)
- [x] Corriger les éventuels breaking changes de compilation (warnings nullable, API dépréciées entre .NET 6 → 10)
- [x] Vérifier que `dotnet build` passe sans erreur

**Critère de validation** : `dotnet build` réussit avec `net10.0` comme cible.

---

## Étape 2 — Remplacement SQL Server → PostgreSQL

- [x] Retirer `Microsoft.EntityFrameworkCore.SqlServer` et `Microsoft.EntityFrameworkCore.Sqlite` du `.csproj`
- [x] Ajouter `Npgsql.EntityFrameworkCore.PostgreSQL` (dernière version stable compatible EF Core 10 / PostgreSQL 16)
- [x] Dans `Program.cs`, remplacer :
  ```csharp
  options.UseSqlServer(connectionString)
  ```
  par :
  ```csharp
  options.UseNpgsql(connectionString)
  ```
- [x] Mettre à jour `appsettings.json` avec une chaîne de connexion PostgreSQL, par exemple :
  ```json
  "DefaultConnection": "Host=postgres;Port=5432;Database=aspnet_core_db;Username=app_user;Password=app_password"
  ```
- [x] Supprimer la clé `SqliteConnection` de `appsettings.json`
- [x] Supprimer le dossier `Migrations/` existant (les migrations SQL Server ne sont pas compatibles PostgreSQL)
- [x] Régénérer les migrations depuis zéro :
  ```bash
  dotnet tool install --global dotnet-ef  # si pas déjà installé
  dotnet ef migrations add InitialCreate
  dotnet ef database update
  ```
- [ ] Vérifier que les `Seeders/*.cs` fonctionnent toujours correctement avec PostgreSQL (types de colonnes, `nvarchar` → `text`/`varchar`, sensibilité à la casse des identifiants, séquences pour les clés auto-incrémentées, etc.) — non applicable sur `feature/core-architecture` elle-même (ce dossier n'existe pas encore sur cette branche, il arrive avec les branches `feature/categories`/`feature/products`/`feature/customers`/`feature/orders`) ; à revalider concrètement contre une instance PostgreSQL réelle une fois ces branches réconciliées
- [x] Attention aux conventions de nommage : PostgreSQL est sensible à la casse pour les identifiants non quotés ; EF Core Npgsql gère normalement ça correctement, mais bien vérifier les noms de tables/colonnes générés

**Critère de validation** : l'application démarre, se connecte à PostgreSQL, les migrations s'appliquent, les seeders insèrent les données sans erreur.

---

## Étape 3 — Docker & docker-compose

- [x] Créer un `Dockerfile` multi-stage :
  - Stage `build` : image `mcr.microsoft.com/dotnet/sdk:10.0`, restore + publish
  - Stage `runtime` : image `mcr.microsoft.com/dotnet/aspnet:10.0`, copie du `publish`, `ENTRYPOINT`
  - Exécuter le processus applicatif avec un utilisateur **non-root** dédié dans l'image runtime (créer un utilisateur système, `USER` non privilégié) plutôt que l'utilisateur root par défaut
- [x] Créer un `.dockerignore` (bin/, obj/, .git/, etc.)
- [x] Créer un `docker-compose.yml` avec les services :
  - `app` : build depuis le Dockerfile, dépend de `postgres` (avec `depends_on` + `condition: service_healthy`), variables d'environnement pour la connection string, port exposé (ex: `8080:8080`)
  - `postgres` : image `postgres:16`, variables d'environnement (`POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`), volume nommé pour la persistance, `healthcheck` (`pg_isready`)
  - ~~`adminer`~~ : décision prise de ne **pas** inclure Adminer dans `docker-compose.yml` — pgAdmin est utilisé en dehors du compose pour l'administration de la base, ce qui rend un service admin supplémentaire dans le compose redondant. `docker-compose.yml` ne contient donc que `app` + `postgres`.
- [x] Utiliser un fichier `.env` à la racine (non commité, ajouté au `.gitignore`) contenant toutes les valeurs sensibles utilisées par `docker-compose.yml` (mot de passe PostgreSQL, chaîne de connexion, etc.), référencées dans le compose via `${VARIABLE}`
- [x] Fournir un `.env.example` commité avec les mêmes clés mais des valeurs factices/vides, pour que n'importe qui puisse copier `.env.example` → `.env` et le remplir
- [x] Documenter dans le README comment lancer le tout : `docker compose up --build`

**Critère de validation** : `docker compose up --build` démarre l'app et la base PostgreSQL ; l'app est accessible et fonctionnelle. Adminer n'étant plus dans le périmètre (voir ci-dessus), l'administration de la base se fait via pgAdmin en dehors du compose. Validé localement : `docker build .` (image construite avec succès), `docker compose config` (interpolation `.env` correcte, healthcheck et `condition: service_healthy` bien présents), et `docker compose up --build` réel — `postgres` passe `healthy` via `pg_isready`, `app` démarre seulement après (`condition: service_healthy` effectif) et répond `HTTP 200` sur `http://localhost:8080/`. Pile arrêtée et volumes nettoyés (`docker compose down -v`) après validation.

---

## Étape 4 — Tests unitaires

- [ ] Créer un projet `aspnet_core_tutorial.UnitTests` (xUnit)
- [ ] Ajouter les packages : `xunit`, `xunit.runner.visualstudio`, `Moq` (ou `NSubstitute`), `Microsoft.EntityFrameworkCore.InMemory` (ou un provider en mémoire adapté)
- [ ] Tester au minimum :
  - Les contrôleurs (`CategoriesController`, `ProductsController`, `HomeController`) : actions GET/POST, cas nominal + cas d'erreur (ex: entité introuvable → 404)
  - Les `Seeders` : logique de seed (ne duplique pas les données si déjà présentes, par exemple)
  - Les `Models` : validations éventuelles (data annotations)
- [ ] Isoler l'accès aux données via `ApplicationDbContext` en mémoire (`UseInMemoryDatabase`) pour ne pas dépendre de PostgreSQL dans les tests unitaires

**Critère de validation** : `dotnet test` sur le projet UnitTests passe à 100%, couverture raisonnable sur les contrôleurs.

---

## Étape 5 — Tests d'intégration

- [ ] Créer un projet `aspnet_core_tutorial.IntegrationTests`
- [ ] Utiliser `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`)
- [ ] Utiliser **Testcontainers** (`Testcontainers.PostgreSql`) pour lancer un vrai conteneur PostgreSQL éphémère pendant les tests (plus fiable qu'une base in-memory pour valider les migrations EF Core réelles)
- [ ] Tester les parcours principaux :
  - Affichage de la page d'accueil (200 OK)
  - CRUD complet sur `Products` et `Categories` via les endpoints HTTP
  - Comportement de l'authentification (accès refusé aux pages protégées si non connecté, redirection vers `/Identity/Account/Login`)
- [ ] S'assurer que les migrations EF Core s'appliquent automatiquement au démarrage du conteneur de test

**Critère de validation** : `dotnet test` sur le projet IntegrationTests passe, avec un vrai conteneur PostgreSQL démarré/arrêté automatiquement par les tests.

---

## Étape 6 — Pipeline CI GitHub Actions

- [ ] Créer `.github/workflows/ci.yml` déclenché sur `push` et `pull_request` (branches `main`/`master`)
- [ ] Jobs à inclure :
  1. **build** : `actions/checkout`, `actions/setup-dotnet` (version 10.x), `dotnet restore`, `dotnet build --configuration Release`
  2. **test** : lancer les tests unitaires (rapides, sans dépendance externe)
  3. **integration-test** : lancer les tests d'intégration (nécessite Docker disponible sur le runner GitHub-hosted — c'est le cas par défaut sur `ubuntu-latest`, compatible avec Testcontainers)
  4. **docker-build** (optionnel) : builder l'image Docker de l'app pour valider que le `Dockerfile` reste fonctionnel (`docker build .`)
  5. **docker-scan** : scanner l'image Docker construite avec un outil comme **Trivy** pour détecter les vulnérabilités connues dans l'image et ses dépendances OS
- [ ] Utiliser les **secrets GitHub Actions** pour toute valeur sensible nécessaire aux jobs (ex: mot de passe PostgreSQL utilisé par les tests d'intégration), jamais en clair dans le YAML
- [ ] Prévoir dans le workflow une distinction claire entre les jobs qui tournent sur toute PR (build, tests) et ceux qui ne concernent qu'une branche précise (ex: un futur job de déploiement Staging/Production déclenché uniquement sur `main`)
- [ ] Mettre en cache les dépendances NuGet (`actions/cache` ou cache intégré de `setup-dotnet`) pour accélérer les runs
- [ ] Publier les résultats de tests en résumé de run (`dotnet test --logger trx` + upload d'artifact, ou utiliser une action de reporting comme `dorny/test-reporter`)

**Critère de validation** : la pipeline passe au vert sur une pull request de test, build + tests unitaires + tests d'intégration + build Docker.

---

## Étape 7 — Améliorations de qualité et de robustesse

Cette étape fait partie intégrante de la migration, à réaliser après les étapes 1 à 6.

- [ ] **Swagger / OpenAPI** : exposer une documentation OpenAPI (`Swashbuckle.AspNetCore` ou le générateur intégré à .NET 10) pour les endpoints exposés, accessible en dev sur `/swagger`
- [ ] **Couche service/repository** : extraire la logique métier des contrôleurs (`CategoriesController`, `ProductsController`, `HomeController`) vers des services dédiés injectés par DI, pour découpler l'accès aux données et faciliter les tests unitaires
- [ ] **Validation des modèles** : renforcer les Data Annotations sur `Category`, `Product`, `Customer`, `Order` (champs requis, longueurs, plages de valeurs) et afficher des messages d'erreur clairs côté vues Razor
- [ ] **Gestion centralisée des erreurs** : middleware d'exception handling personnalisé (`UseExceptionHandler`) avec page d'erreur générique cohérente, logs des exceptions non gérées
- [ ] **Logging structuré (Serilog)** : sortie console + fichier (rolling file), niveaux de log configurés par environnement (`appsettings.Development.json` vs `appsettings.json`)
- [ ] **Health checks** : endpoint `/health` (`Microsoft.Extensions.Diagnostics.HealthChecks` + `AspNetCore.HealthChecks.NpgSql`) vérifiant la connexion PostgreSQL, branché sur le `healthcheck` du service `app` dans docker-compose
- [ ] **Pagination** : ajouter la pagination sur les listes `Products`, `Categories`, `Orders` (actuellement chargées en intégralité)
- [ ] **Secrets via variables d'environnement (`.env`)** : ne jamais mettre de mot de passe/chaîne de connexion en dur dans `appsettings*.json`. En local (avec ou sans Docker), utiliser un fichier `.env` chargé au démarrage (ex: via `DotNetEnv` côté app, ou `docker compose --env-file .env`) pour peupler les variables d'environnement (`ConnectionStrings__DefaultConnection`, etc.), lues nativement par la configuration ASP.NET Core. `dotnet user-secrets` reste une alternative acceptée pour le dev pur sans Docker, mais `.env` est le mécanisme de référence du projet, cohérent entre local et Docker
- [ ] **Analyzers / style de code** : ajouter un `.editorconfig`, activer `EnforceCodeStyleInBuild` dans le `.csproj`, corriger les warnings d'analyzers restants
- [ ] **Migrations documentées** : noms de migrations explicites, script/documentation de rollback (`dotnet ef migrations remove`, `dotnet ef database update <migration précédente>`)
- [ ] **README principal du projet** : mise à jour complète (stack technique, prérequis, instructions de lancement local et via Docker, comment lancer les tests, comment consulter Swagger et Adminer)
- [ ] **Sécurité applicative** : rate limiting (middleware natif `Microsoft.AspNetCore.RateLimiting`), en-têtes de sécurité (`X-Content-Type-Options`, `Content-Security-Policy`), vérification des jetons anti-forgery sur tous les formulaires POST
- [ ] **Audit des dépendances** : `dotnet list package --vulnerable` intégré en local et/ou en CI pour détecter les packages NuGet avec vulnérabilités connues
- [ ] **Performance des requêtes EF Core** : `AsNoTracking()` sur les requêtes en lecture seule (listes), compression des réponses HTTP (`ResponseCompression`), mise en cache mémoire (`IMemoryCache`) pour les données peu volatiles comme les catégories
- [ ] **Traçabilité des entités** : ajouter `CreatedAt`/`UpdatedAt` sur `Category`, `Product`, `Customer`, `Order`, renseignés automatiquement (interceptor EF Core ou surcharge de `SaveChanges`)
- [ ] **Gestion des environnements multiples** (Dev / Staging / Production) :
  - Créer `appsettings.Staging.json` et `appsettings.Production.json` (en plus de `Development.json` existant), avec uniquement les valeurs qui diffèrent (niveaux de logs, connection strings vides — jamais de secret en clair dans ces fichiers)
  - Piloter l'environnement actif via la variable `ASPNETCORE_ENVIRONMENT`
  - Tous les secrets (mots de passe DB, clés diverses) injectés via **variables d'environnement**, portées par des fichiers `.env` dédiés par contexte (`.env` pour le local/Docker, avec un `.env.example` commité comme référence), jamais commités eux-mêmes : en CI/CD, les mêmes noms de variables sont injectés via les **secrets GitHub Actions** (`Settings > Secrets and variables > Actions`) plutôt que via un fichier
  - Un `.env.staging.example` et `.env.production.example` peuvent aussi être fournis pour documenter les variables attendues par environnement, sans jamais contenir de vraie valeur
  - Prévoir des fichiers `docker-compose.override.yml` (dev, avec hot-reload/volumes montés) et `docker-compose.prod.yml` (production, sans exposition inutile de ports, sans Adminer) en plus du `docker-compose.yml` de base
  - Documenter dans le README la matrice des environnements : quel fichier de config, quelles variables attendues, comment lancer chaque environnement

**Critère de validation** : `dotnet build` sans warning bloquant, `/health` retourne `Healthy` quand PostgreSQL est up, `/swagger` accessible en dev, pagination visible sur au moins une liste, README à jour et suivi par un développeur externe sans connaissance préalable du projet, `dotnet list package --vulnerable` ne remonte aucune vulnérabilité critique non traitée, en-têtes de sécurité présents sur les réponses HTTP, l'application démarre correctement avec `ASPNETCORE_ENVIRONMENT=Staging` et `=Production` sans qu'aucun secret ne soit présent en dur dans le code ou les fichiers commités.

---

## Notes pour Claude Code

- Ne pas essayer de faire toutes les étapes en un seul commit : une étape = une unité de travail testable.
- Après chaque étape, lancer `dotnet build` (et `dotnet test` dès que les projets de test existent) avant de passer à la suite.
- Toujours vérifier les versions de packages NuGet disponibles au moment de l'exécution plutôt que de se fier à des versions codées en dur ici, certaines pouvant être sorties après la rédaction de ce plan.
- Poser une question à l'utilisateur si un choix d'architecture n'est pas explicitement tranché ici (ex: Serilog vs autre, nom des variables d'environnement, etc.).
