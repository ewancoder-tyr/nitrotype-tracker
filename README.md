# NitroType Tracker

![license](https://img.shields.io/github/license/ewancoder/nitrotype-tracker?color=blue)
![activity](https://img.shields.io/github/commit-activity/m/ewancoder/nitrotype-tracker)

| API | UI |
| --- | -- |
| [![Quality gate](https://sonarcloud.io/api/project_badges/quality_gate?project=ewancoder-nitrotype-tracker-api)](https://sonarcloud.io/summary/new_code?id=ewancoder-nitrotype-tracker-api) | [![Quality gate](https://sonarcloud.io/api/project_badges/quality_gate?project=ewancoder-nitrotype-tracker-web)](https://sonarcloud.io/summary/new_code?id=ewancoder-nitrotype-tracker-web) |

Statistics tracker for NitroType racing. Initially made for KECATS team.
Shows league / season racing statistics.

## Production status

![ci](https://github.com/ewancoder/nitrotype-tracker/actions/workflows/deploy.yml/badge.svg?branch=main)
![status](https://img.shields.io/github/last-commit/ewancoder/nitrotype-tracker/main)
![diff](https://img.shields.io/github/commits-difference/ewancoder/nitrotype-tracker?base=main&head=main&logo=git&label=diff&color=orange)
![api-coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/ewancoder/0184962696ef0364be7a3f491133f2f9/raw/nitrotype-tracker-api-coverage-main.json)
![be-fetch-coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/ewancoder/0184962696ef0364be7a3f491133f2f9/raw/nitrotype-tracker-be-fetch-coverage-main.json)
![web-ui-coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/ewancoder/0184962696ef0364be7a3f491133f2f9/raw/nitrotype-tracker-web-coverage-main.json)
![todos](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/ewancoder/0184962696ef0364be7a3f491133f2f9/raw/nitrotype-tracker-todos-main.json)

## Development status

![ci](https://github.com/ewancoder/nitrotype-tracker/actions/workflows/deploy.yml/badge.svg?branch=develop)
![status](https://img.shields.io/github/last-commit/ewancoder/nitrotype-tracker/develop)
![diff](https://img.shields.io/github/commits-difference/ewancoder/nitrotype-tracker?base=main&head=develop&logo=git&label=diff&color=orange)
![api-coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/ewancoder/0184962696ef0364be7a3f491133f2f9/raw/nitrotype-tracker-api-coverage-develop.json)
![be-fetch-coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/ewancoder/0184962696ef0364be7a3f491133f2f9/raw/nitrotype-tracker-be-fetch-coverage-develop.json)
![web-ui-coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/ewancoder/0184962696ef0364be7a3f491133f2f9/raw/nitrotype-tracker-web-coverage-develop.json)
![todos](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/ewancoder/0184962696ef0364be7a3f491133f2f9/raw/nitrotype-tracker-todos-develop.json)

## Powered by

This is the list of technologies used in every TyR pet project. Unchecked means it's (probably) still present in the stack, but is not being used by this particular project as of now.

### Both backend and frontend

- [x] [Rider IDE](https://www.jetbrains.com/rider)
  - Visual Studio level features & cross platform: allows me using Linux during development.
  - Also has great support for web development - no need for a separate editor/IDE when developing frontend.
  - [ ] [Visual Studio](https://visualstudio.microsoft.com/)
- [x] [GoLand](https://www.jetbrains.com/go)
  - JetBrains IDE for Go (golang).
- [x] [SonarQube](https://www.sonarsource.com/products/sonarqube)
  - Code static analysis tool, checks the quality of the code and makes sure it doesn't get worse.
  - IDE extensions show errors in real time (linting).
- [x] [Stryker](https://stryker-mutator.io)
  - Unit test mutation tool: makes sure your unit tests are good quality.
- [ ] [Sign In with Google](https://developers.google.com/identity/gsi/web/guides/overview)
  - Google OneTap OIDC frontend functionality: used to sign in users when we need authentication.

### Backend

- [x] [.NET Core / C#](https://dotnet.microsoft.com)
  - Main Web API backend stack.
- [x] [GoLang](https://go.dev)
  - Additional services stack for simple services that need to conserve RAM.
  - Typical Go service uses 5 Mb of RAM, where typical .NET service uses 60+.
- [x] [PostgreSQL](https://www.postgresql.org)
  - Database type I'm using in all my projects where I need a database.
  - One of the most powerful DBs, supporting both relational and document-based (JSON) entities.
- [x] [DbMate](https://github.com/amacneil/dbmate)
  - Technology-agnostic database migration tool.
  - Allows writing pure raw SQL queries for up/down actions and tracks the migration of the DB.
- [x] [ValKey](https://valkey.io)
  - Open-Source fork of Redis, most of open source world is migrating into it after Redis license change.
  - [ ] [KeyDB](https://docs.keydb.dev)
    - Redis fork with support for multithreading - great alternative, but my servers have a single CPU core so it's useless for me.
  - [ ] [Redis](https://redis.io)
    - Old school redis, key-value store in memory for quick access. Due to some license shenanigans using ValKey now.
- [x] [Seq](https://datalust.co/seq)
  - Logging self-hosted solution. Serilog support (but I'm also sending logs from Go to it)
  - I have a centralized Seq server for aggregating logs from all my projects.
- [x] [Scalar](https://scalar.com)
  - Alternative to SwaggerUI, to provide OpenAPI documentation & interface to call your endpoints.
  - Now that .NET 9 decouples from Swagger completely we can use Scalar with OpenAPI document generated by .NET.
- [x] [XUnit, Moq, AutoFixture, AutoMoq](https://xunit.net)
  - Scalable unit-testing framework for .NET.
- [x] [NCrunch](https://www.ncrunch.net)
  - Must have extension for continuous unit-testing and TDD.
  - It shows you real time feedback on your tested branches, as soon as you type/change any code.
- [x] [Coverlet coverage collector](https://github.com/coverlet-coverage/coverlet)
  - Tool for unit testing coverage statistics, used in build pipelines.
- [ ] [Duende IdentityServer](https://duendesoftware.com/products/identityserver)
  - An extensive solution for authentication. I'm only using it for machine-to-machine (client credentials) authentication.

### Frontend

- [x] [TypeScript](https://www.typescriptlang.org)
  - Main language for my frontends, for type safety.
- [x] [pNPm](https://pnpm.io)
  - Faster and better than npm, although it is less widely supported.
- [x] [Angular](https://angular.dev)
  - Main framework for my frondends. I'm a big Angular fan.
- [x] [RxJS](https://rxjs.dev/)
  - Extensive library for management of reactive state.
- [x] [ESLint](https://eslint.org)
  - Code linting.
- [x] [Prettier](https://prettier.io)
  - Makes sure code anheres to a single standard & formatted accordingly.
  - It is set up to format the files on save.
- [x] [Karma Sabarivka coverage reporter](https://github.com/kopach/karma-sabarivka-reporter)
  - Need to use this in order for Karma to acknowledge files that are not covered by unit tests at all, and calculate them into the resulting overall coverage.
- [ ] [WallabyJS (doesn't work with monorepos)](https://wallabyjs.com)
  - Real-time unit test coverage reporter: similar to NCrunch.

### DevOps

- [x] [Bash](https://www.gnu.org/software/bash)
  - Automation scripts in linux & build scripts & deployment scripts.
- [x] [GIT](https://git-scm.com)
  - Decentralized SCM for code management.
- [x] [Github Actions](https://github.com/features/actions)
  - Build and deployment automation.
- [x] [Docker](https://www.docker.com) / [Compose](https://docs.docker.com/compose) / [Swarm](https://docs.docker.com/engine/swarm)
  - Deployment automation. Infrastructure management.
- [x] [GitHub Container Registry](https://docs.github.com/en/packages)
  - Here I'm storing most of my containers (some shared ones are pushed to dockerhub instead).
- [x] [Shields.io](https://shields.io)
  - A tool for generation of badges that can be placed on github README page.
- [x] [Digital Ocean](https://www.digitalocean.com)
  - My VPS provider, where I'm hosting all my projects.
- [x] [Cloudflare.com](https://www.cloudflare.com)
  - [x] [Domain.com](https://www.domain.com)
  - My domain providers.
- [x] [Caddy](https://caddyserver.com) & [Let's Encrypt](https://letsencrypt.org)
  - Reverse proxy with automatic HTTPS certificate acquisition (by using Let's Encrypt).
  - Forwards the traffic to different projects based on the URL.
- [x] [Nginx Unprivileged](https://github.com/nginx/docker-nginx-unprivileged)
  - [x] Slim version: ~12.2 Mb image (default ~47.5 Mb)
    - Used for frontend apps - to serve the Angular app.
- [x] [Chiseled image](https://github.com/dotnet/dotnet-docker/blob/main/documentation/ubuntu-chiseled.md) for Asp.NET: 126 Mb image (default 233 Mb)
  - Used for hosting .NET applications in secure rootless environment.
- [x] [Distroless Debian](https://gcr.io/distroless/base-debian12)
  - Used for hosting GoLang apps in distroless secure environment.
- [x] [pgAdmin](https://www.pgadmin.org)
  - Easy access to view data in any of the databases, using Web UI.
  - Production databases are not connected to the network by default.
- [x] [Redis Insight](https://redis.io/insight)
  - Easy access to view data in any of the redis stores, using Web UI.
  - Production redis stores are not connected to the network by default.
- [x] [Netdata](https://www.netdata.cloud)
  - An extensive solution for monitoring overall system and all the projects, physical machines.

### Authentication

Authentication flow supports both JWT and Cookie authentication and works with Google sign in for user tokens & Duende IdentityServer for service to service tokens.

### Deployments

Project has 2 environments: development and production. Deployments are incremental: if frontend didn't change we only deploy backend, and wise versa; database can be deployed separately too.

### Infrastructure diagram

![Infrastructure](https://github.com/ewancoder/nitrotype-tracker/blob/main/infra.png?raw=true)
