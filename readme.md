# ![RealWorld Example App](logo.png)

ASP.NET Core codebase containing real world examples (CRUD, auth, advanced patterns, etc.) that adheres to the [RealWorld](https://github.com/gothinkster/realworld-example-apps) spec and API.

## [RealWorld](https://github.com/gothinkster/realworld)

This codebase demonstrates a fully fledged application built with ASP.NET Core and feature-oriented vertical slices, including CRUD operations, authentication, routing, pagination, and more.

The implementation follows ASP.NET Core community style guides and best practices where they fit the RealWorld contract.

For information on how this works with other frontends and backends, see the [RealWorld](https://github.com/gothinkster/realworld) repository.

## How it works

This uses ASP.NET Core with:

- CQRS and source-generated [Mediator](https://github.com/martinothamar/Mediator)
- [Mapperly](https://mapperly.riok.app/) for compile-time object mapping
- [FluentValidation](https://github.com/FluentValidation/FluentValidation)
- Feature folders and vertical slices
- [Entity Framework Core](https://learn.microsoft.com/ef/core/) with SQLite for local/demo use. The application also includes SQL Server support.
- Built-in Swagger via [Swashbuckle.AspNetCore](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
- [Bullseye](https://github.com/adamralph/bullseye) for building
- JWT authentication using [ASP.NET Core JWT Bearer Authentication](https://learn.microsoft.com/aspnet/core/security/authentication/jwt)
- [CSharpier](https://csharpier.com/) for formatting
- `.editorconfig` to enforce usage patterns

The basic architecture is based on this reference architecture: [ContosoUniversityCore](https://github.com/jbogard/ContosoUniversityCore).

## Getting started

Install the .NET SDK pinned in [`global.json`](global.json), currently `10.0.302`.

The main validation target formats the repository, builds the solution in Release mode, and runs the integration tests:

```sh
dotnet run --project build/build.csproj -- test
```

To format without building:

```sh
dotnet run --project build/build.csproj -- format
```

The full CI-equivalent build, test, and publish pipeline is:

```sh
dotnet run --project build/build.csproj
```

See [`AGENTS.md`](AGENTS.md) for repository layout, development conventions, and complete validation guidance.

## Docker Build

There is a `Makefile` for macOS and Linux:

- `make build` executes `docker compose build`
- `make run` executes `docker compose up`

The above might work for Docker on Windows.

## Local building

The build is a C# project:

```sh
dotnet run --project build/build.csproj -- test
```

## Local API

Run the API with `make run-local`. Swagger is available at:

`http://localhost:5000/swagger`

## RealWorld API spec tests

The official [RealWorld API spec](https://github.com/realworld-apps/realworld) test collections ([Hurl](https://hurl.dev) and [Bruno](https://www.usebruno.com)) run against this implementation. The spec repository is vendored as the `realworld` git submodule:

- `make submodule` fetches the spec (`git submodule update --init realworld`)
- `make test-hurl-with-managed-server` starts the API on a fresh SQLite database, runs the Hurl suite, and shuts it down (requires [Hurl](https://hurl.dev))
- `make test-bruno-with-managed-server` does the same with the Bruno collection (requires [Bun](https://bun.sh))
- `make test-hurl` / `make test-bruno` run the suites against an already running server (`make run-local`)

Both suites run in CI via the "RealWorld API Tests" workflow.

All endpoints are rooted under `/api` as the spec requires; the prefix can be changed through the `ApiPrefix` configuration key (appsettings or an environment variable).

## GitHub Actions build

![Build and Test](https://github.com/gothinkster/aspnetcore-realworld-example-app/workflows/Build%20and%20Test/badge.svg)
