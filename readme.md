# ![RealWorld Example App](logo.png)

ASP.NET Core codebase containing real world examples (CRUD, auth, advanced patterns, etc.) that adheres to the [RealWorld](https://github.com/realworld-apps/realworld) spec and API.

## [RealWorld](https://github.com/realworld-apps/realworld)

This codebase demonstrates a fully fledged application built with ASP.NET Core and feature-oriented vertical slices, including CRUD operations, authentication, routing, pagination, and more.

The implementation follows ASP.NET Core community style guides and best practices where they fit the RealWorld contract.

For information on how this works with other frontends and backends, see the [RealWorld](https://github.com/realworld-apps/realworld) repository.

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

Install the .NET SDK specified in [`global.json`](global.json), currently `10.0.401`.
`rollForward: latestFeature` allows newer .NET 10 feature bands and patches; CI reads this file too.

To start locally on a fresh SQLite database:

```sh
export Jwt__SigningKey="$(openssl rand -base64 32)"
make run-local
```

The API is at `http://localhost:5000/api` and Swagger at `http://localhost:5000/swagger`.
Keep the signing key stable between runs. **Existing databases created by older versions
must be backed up and baselined first:** see [database migrations](docs/database-migrations.md).

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

CI uses locked restores and checks formatting without modifying files:

```sh
dotnet restore Conduit.slnx --locked-mode
dotnet run --project build/build.csproj --no-restore -- --check-format
```

After changing dependencies, run `dotnet restore Conduit.slnx` and include the updated
`packages.lock.json` files. The build then enforces those lock files.

See [`AGENTS.md`](AGENTS.md) for repository layout, development conventions, and complete validation guidance.

## Docker Build

Before starting the API locally or with Docker, generate a signing key:

```sh
export Jwt__SigningKey="$(openssl rand -base64 32)"
```

Each deployment must use its own cryptographically random key (at least 32 bytes,
base64-encoded), provided through protected configuration such as a secret manager
or the `Jwt__SigningKey` environment variable. Never commit it or bake it into an
image. Startup fails for missing, malformed, short, or obvious placeholder keys.
Issuer and audience are identifiers, not secrets.

Only version-2 tokens with `conduit_token_version: "2"` and a stable `user:<id>`
subject are accepted. Legacy username-subject and unversioned tokens are rejected,
even when their subject resembles a new user ID. There is no compatibility fallback.
All existing sessions must sign in again after this upgrade. New sessions survive
username changes; authentication rejects tokens belonging to deleted users.

Keep the key stable across restarts and shared only among instances of the same
deployment. To rotate a compromised key, replace it on every instance and restart;
all previously issued tokens become invalid and users must sign in again. Deployments
using the former public default key should rotate immediately. Managed API test
targets generate a fresh ephemeral key when one is not supplied.

There is a `Makefile` for macOS and Linux:

- `make build` executes `docker compose build`
- `make run` executes `docker compose up`

Docker exposes `http://localhost:8080/api` and `/swagger`, not port 5000.
The container runs as the non-root `app` user. Compose stores SQLite data in the
`conduit-data` named volume mounted at `/data`; data survives container replacement.
For custom bind mounts, make the database directory writable by the container user.
Local databases and the RealWorld submodule are excluded from the image build context.

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
- `make test-bruno-with-managed-server` does the same with the Bruno collection (requires [Bun](https://bun.sh)); the wrapper pins Bruno CLI `4.2.1`
- `make test-hurl` / `make test-bruno` run the suites against an already running server (`make run-local`)

Both suites run in CI via the "RealWorld API Tests" workflow.

Managed targets use isolated temporary databases and generated signing keys, require a
successful readiness response, and clean up the server and database on exit. They do
not delete your local database. Override the port with, for example,
`make test-hurl-with-managed-server API_URL=http://localhost:5098`.
They require Bash, curl, OpenSSL, and .NET, but not GNU `timeout`.
Hurl `8.0.1` is used in CI. The pinned submodule is tested on pushes and pull requests;
a weekly Hurl canary also tests upstream HEAD without changing the committed pin.

All endpoints are rooted under `/api` as the spec requires; the prefix can be changed through the `ApiPrefix` configuration key (appsettings or an environment variable).

## Database configuration

Configuration can be supplied through appsettings or environment variables:

| Setting | Environment variable | Default |
| --- | --- | --- |
| Database provider | `Database__Provider` | `sqlite` (`sqlserver` also supported) |
| Connection string | `ConnectionStrings__Conduit` | `Data Source=realworld.db` locally; `/data/realworld.db` in Docker |
| Apply migrations on startup | `Database__ApplyMigrations` | `true` |

SQLite example:

```sh
export Database__Provider=sqlite
export ConnectionStrings__Conduit='Data Source=/absolute/writable/path/conduit.db'
make run-local
```

SQL Server example (use a secret manager for real credentials):

```sh
export Database__Provider=sqlserver
export ConnectionStrings__Conduit='Server=localhost,1433;Database=Conduit;User ID=conduit;Password=<secret>;Encrypt=True;TrustServerCertificate=True'
make run-local
```

`TrustServerCertificate=True` is for local development only. SQL Server requires an
explicit connection string and a reachable server; it does not require a Windows API
container. The old `ASPNETCORE_Conduit_DatabaseProvider` and
`ASPNETCORE_Conduit_ConnectionString` variables remain fallback aliases.
For managed deployments, apply reviewed migration scripts before starting instances
and set `Database__ApplyMigrations=false`. See [migration guidance](docs/database-migrations.md).

## Password storage

New and changed passwords use ASP.NET Core Identity's versioned PBKDF2-HMAC-SHA512
format with 210,000 iterations and a cryptographically random embedded salt.
Existing HMAC passwords remain usable and are upgraded after successful login;
failed logins never rewrite hashes. Rollback to an older application version will
not support upgraded passwords. Database uniqueness protects usernames, emails,
and article slugs, including concurrent writes. Usernames are limited to 256
characters and emails to 320 characters for SQL Server index compatibility.

## GitHub Actions build

![Build and Test](https://github.com/realworld-apps/aspnetcore-realworld-example-app/actions/workflows/dotnetcore.yml/badge.svg)
