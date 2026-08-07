# ![RealWorld Example App](logo.png)

ASP.NET Core codebase containing real world examples (CRUD, auth, advanced patterns, etc) that adheres to the [RealWorld](https://github.com/gothinkster/realworld-example-apps) spec and API.

## [RealWorld](https://github.com/gothinkster/realworld)

This codebase was created to demonstrate a fully fledged fullstack application built with ASP.NET Core (with Feature orientation) including CRUD operations, authentication, routing, pagination, and more.

We've gone to great lengths to adhere to the ASP.NET Core community styleguides & best practices.

For more information on how to this works with other frontends/backends, head over to the [RealWorld](https://github.com/gothinkster/realworld) repo.

## How it works

This is using ASP.NET Core with:

- CQRS and [MediatR](https://github.com/jbogard/MediatR)
  - [Simplifying Development and Separating Concerns with MediatR](https://blogs.msdn.microsoft.com/cdndevs/2016/01/26/simplifying-development-and-separating-concerns-with-mediatr/)
  - [CQRS with MediatR and AutoMapper](https://lostechies.com/jimmybogard/2015/05/05/cqrs-with-mediatr-and-automapper/)
  - [Thin Controllers with CQRS and MediatR](https://codeopinion.com/thin-controllers-cqrs-mediatr/)
- [AutoMapper](http://automapper.org)
- [Fluent Validation](https://github.com/JeremySkinner/FluentValidation)
- Feature folders and vertical slices
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/) on SQLite for demo purposes. Can easily be anything else EF Core supports. Open to porting to other ORMs/DBs.
- Built-in Swagger via [Swashbuckle.AspNetCore](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
- [Bullseye](https://github.com/adamralph/bullseye) for building!
- JWT authentication using [ASP.NET Core JWT Bearer Authentication](https://github.com/aspnet/Security/tree/master/src/Microsoft.AspNetCore.Authentication.JwtBearer).
- Use [dotnet-format](https://github.com/dotnet/format) for style checking
- `.editorconfig` to enforce some usage patterns

This basic architecture is based on this reference architecture: [https://github.com/jbogard/ContosoUniversityCore](https://github.com/jbogard/ContosoUniversityCore)

## Getting started

Install the .NET Core SDK and lots of documentation: [https://www.microsoft.com/net/download/core](https://www.microsoft.com/net/download/core)

Documentation for ASP.NET Core: [https://docs.microsoft.com/en-us/aspnet/core/](https://docs.microsoft.com/en-us/aspnet/core/)

## Docker Build

There is a 'Makefile' for OS X and Linux:

- `make build` executes `docker-compose build`
- `make run` executes `docker-compose up`

The above might work for Docker on Windows

## Local building

- It's just another C# file!   `dotnet run -p build/build.csproj`

## Swagger URL

- `http://localhost:5000/swagger`

## RealWorld API spec tests

The official [RealWorld API spec](https://github.com/realworld-apps/realworld) test collections ([Hurl](https://hurl.dev) and [Bruno](https://www.usebruno.com)) run against this implementation. The spec repo is vendored as the `realworld` git submodule:

- `make submodule` fetches the spec (`git submodule update --init realworld`)
- `make test-hurl-with-managed-server` starts the API on a fresh SQLite database, runs the Hurl suite, and shuts it down (requires [hurl](https://hurl.dev))
- `make test-bruno-with-managed-server` same with the Bruno collection (requires [bun](https://bun.sh))
- `make test-hurl` / `make test-bruno` run the suites against an already running server (`make run-local`)

Both suites run in CI via the “RealWorld API Tests” workflow.

All endpoints are rooted under `/api` as the spec requires; the prefix can be changed through the `ApiPrefix` configuration key (appsettings or environment variable).

## GitHub Actions build

![Build and Test](https://github.com/gothinkster/aspnetcore-realworld-example-app/workflows/Build%20and%20Test/badge.svg)
