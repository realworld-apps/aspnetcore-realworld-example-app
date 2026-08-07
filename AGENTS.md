# Repository Guidance

## Overview

- The application is an ASP.NET Core 10 API implementing the RealWorld specification.
- `src/Conduit` contains the web application and its feature-oriented vertical slices.
- `tests/Conduit.IntegrationTests` contains xUnit integration tests using an EF Core InMemory database.
- `build` contains the Bullseye build targets used by CI.
- `realworld` is a vendored git submodule containing the upstream API specification tests. Do not edit it unless the task explicitly targets the submodule.

## Toolchain

- Use the .NET SDK version pinned in `global.json` (`10.0.302`) and target `net10.0`.
- Package versions are managed centrally in `Directory.Packages.props`.
- Restore packages with lock files enabled. Restore updates to `packages.lock.json` files should be included when dependencies change.
- Nullable reference types, recommended analyzers, code-style checks, and warnings-as-errors are enabled in `Directory.Build.props`.
- CSharpier `1.3.0` is the repository formatter. The repository uses four-space indentation, file-scoped namespaces, `var`, explicit accessibility modifiers, and sorted `System` usings.

## Validation

Run the standard validation target after code changes:

```sh
dotnet run --project build/build.csproj -- test
```

The target formats the repository, builds the solution in Release mode, and runs all test projects. To format without building, run:

```sh
dotnet run --project build/build.csproj -- format
```

The CI-equivalent build, test, and publish pipeline is:

```sh
dotnet run --project build/build.csproj
```

Run the application locally with `make run-local`. The API is available at `http://localhost:5000`, and Swagger is available at `/swagger`.

The optional RealWorld API suites require the `realworld` submodule plus Hurl or Bun/Bruno:

```sh
make submodule
make test-hurl-with-managed-server
make test-bruno-with-managed-server
```

## Development Practices

- Keep commands, queries, validators, handlers, and related models together under the relevant feature folder.
- Keep controllers thin and pass the request cancellation token through to handlers and database operations.
- Use FluentValidation for request validation and preserve the existing error response behavior.
- Use EF Core through `ConduitContext`; do not introduce a second persistence abstraction without a concrete requirement.
- Add or update integration tests for behavior changes, especially changes to the RealWorld API contract.
- Do not commit generated build output, local SQLite databases, secrets, or changes inside the `realworld` submodule unless required.
