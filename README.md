# NitroType Tracker

![license](https://img.shields.io/github/license/ewancoder/nitrotype-tracker?color=blue)
![activity](https://img.shields.io/github/commit-activity/m/ewancoder/nitrotype-tracker)

Statistics tracker for NitroType racing. Initially made for KECATS team.
Shows league / season racing statistics.

## Production status

![ci](https://github.com/ewancoder/nitrotype-tracker/actions/workflows/deploy.yml/badge.svg?branch=main)
![status](https://img.shields.io/github/last-commit/ewancoder/nitrotype-tracker/main)
![diff](https://img.shields.io/github/commits-difference/ewancoder/nitrotype-tracker?base=main&head=main&logo=git&label=diff&color=orange)
![api-coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/ewancoder/0184962696ef0364be7a3f491133f2f9/raw/nitrotype-tracker-api-coverage-main.json)
![web-ui-coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/ewancoder/0184962696ef0364be7a3f491133f2f9/raw/nitrotype-tracker-web-coverage-main.json)
![todos](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/ewancoder/0184962696ef0364be7a3f491133f2f9/raw/nitrotype-tracker-todos-main.json)

## Development status

![ci](https://github.com/ewancoder/nitrotype-tracker/actions/workflows/deploy.yml/badge.svg?branch=develop)
![status](https://img.shields.io/github/last-commit/ewancoder/nitrotype-tracker/develop)
![diff](https://img.shields.io/github/commits-difference/ewancoder/nitrotype-tracker?base=main&head=develop&logo=git&label=diff&color=orange)
![api-coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/ewancoder/0184962696ef0364be7a3f491133f2f9/raw/nitrotype-tracker-api-coverage-develop.json)
![web-ui-coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/ewancoder/0184962696ef0364be7a3f491133f2f9/raw/nitrotype-tracker-web-coverage-develop.json)
![todos](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/ewancoder/0184962696ef0364be7a3f491133f2f9/raw/nitrotype-tracker-todos-develop.json)

## Powered by

This is the list of technologies used in every TyR pet project. Unchecked means it's (probably) still present in the stack, but is not being used by this particular project as of now.

### Both backend and frontend

- [x] [Rider IDE](https://www.jetbrains.com/rider)
- [x] [SonarQube](https://www.sonarsource.com/products/sonarqube)
- [x] [Stryker](https://stryker-mutator.io)
- [ ] [Sign In with Google](https://developers.google.com/identity/gsi/web/guides/overview)

### Backend

- [x] [.NET Core / C#](https://dotnet.microsoft.com)
- [x] [PostgreSQL](https://www.postgresql.org)
- [x] [DbMate](https://github.com/amacneil/dbmate)
- [x] [Seq](https://datalust.co/seq)
- [x] [Scalar](https://scalar.com)
- [x] [XUnit, Moq, AutoFixture, AutoMoq](https://xunit.net)
- [x] [NCrunch](https://www.ncrunch.net)
- [x] [Coverlet coverage collector](https://github.com/coverlet-coverage/coverlet)
- [ ] [Duende IdentityServer](https://duendesoftware.com/products/identityserver)

### Frontend

- [x] [TypeScript](https://www.typescriptlang.org)
- [x] [pNPm](https://pnpm.io)
- [x] [Angular](https://angular.dev)
- [x] [RxJS](https://rxjs.dev/)
- [x] [ESLint](https://eslint.org)
- [x] [Prettier](https://prettier.io)
- [x] [Karma Sabarivka coverage reporter](https://github.com/kopach/karma-sabarivka-reporter)
- [ ] [WallabyJS (doesn't work with monorepos)](https://wallabyjs.com)

### DevOps

- [x] [Bash](https://www.gnu.org/software/bash)
- [x] [GIT](https://git-scm.com)
- [x] [Github Actions](https://github.com/features/actions)
- [x] [Docker / Compose](https://www.docker.com)
- [x] [GitHub Container Registry](https://docs.github.com/en/packages)
- [x] [Shields.io](https://shields.io)
- [x] [Digital Ocean](https://www.digitalocean.com)
- [x] [Domain.com](https://www.domain.com)
- [x] [Caddy](https://caddyserver.com) & [Let's Encrypt](https://letsencrypt.org)
- [x] [Nginx Unprivileged](https://github.com/nginx/docker-nginx-unprivileged)
- [x] [pgAdmin](https://www.pgadmin.org)
- [x] [Netdata](https://www.netdata.cloud)

### Authentication

Authentication flow supports both JWT and Cookie authentication and works with Google sign in for user tokens & Duende IdentityServer for service to service tokens.

### Deployments

Project has 2 environments: development and production. Deployments are incremental: if frontend didn't change we only deploy backend, and wise versa; database can be deployed separately too.

