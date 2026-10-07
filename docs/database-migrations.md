# Database migrations

The API now uses EF Core migrations instead of `EnsureCreated()`. SQLite and SQL
Server share the `ConduitContext` model, but have independent migration snapshots:

| Provider | Context | Initial schema baseline |
| --- | --- | --- |
| SQLite | `SqliteConduitContext` | `20261006170650_InitialSchema` |
| SQL Server | `SqlServerConduitContext` | `20261006170711_InitialSchema` |

The initial migration describes the previous schema. `UniqueIdentities` adds unique
username, email, and slug indexes. On SQL Server it also bounds their column sizes.
No password columns change: the existing `Hash` column stores Identity's versioned
payload, while `Salt` remains available for legacy password verification.

## Fresh databases

Startup applies pending migrations by default. Configure `Database:Provider` and
`ConnectionStrings:Conduit` as described in the README. The database directory must
be writable for SQLite, and the SQL Server account needs schema permissions when
startup migrations are enabled.

For production, generate and review provider-specific scripts, apply them as a
deployment step, then start instances with `Database__ApplyMigrations=false`:

```sh
dotnet tool restore
dotnet ef migrations script --context SqliteConduitContext --project src/Conduit --output /path/to/sqlite.sql
dotnet ef migrations script --idempotent --context SqlServerConduitContext --project src/Conduit --output /path/to/sqlserver.sql
```

SQLite does not support EF's idempotent script generation; generate a script from
the actual last applied migration to the desired migration instead. EF tooling
uses design-time factories with placeholder connections for script generation.
To apply with `dotnet ef database update`, **always pass `--connection` explicitly**
so it targets the intended database, rather than the design-time default.

## Existing databases created by `EnsureCreated()`

**Do not apply the initial migration to a populated database, delete its tables,
or mark all migrations as applied.** Startup refuses to adopt these databases
automatically. The following is an explicit, operator-reviewed baseline procedure:

1. Stop all API instances and other writers. Back up the database and verify the backup.
2. Generate the provider's initial script (`migrations script 0 InitialSchema`) and
   compare the existing tables, columns, keys, indexes, and foreign keys against it.
   Only use this procedure for a matching schema. Older or customized schemas need
   their own reviewed upgrade first.
3. Check and resolve duplicate non-null usernames, emails, and slugs. On SQL Server,
   also check usernames over 256 UTF-16 code units, emails over 320, and slugs over
   450. Check collisions using the database's own collation. Do not silently truncate
   values or discard accounts/articles.
4. Create the migration history table and record **only** the initial schema using
   the SQL below. Do not run the initial script's table-creation statements.
5. Generate a script from `InitialSchema` to the latest migration, review it, and
   apply it on a backup copy first. Check counts, relationships, and authentication
   before applying it to the real database and restarting the API.

SQLite baseline SQL (run against the intended database with `sqlite3`):

```sql
BEGIN IMMEDIATE;
CREATE TABLE "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);
INSERT INTO "__EFMigrationsHistory" VALUES
    ('20261006170650_InitialSchema', '10.0.12');
COMMIT;
```

SQL Server baseline SQL:

```sql
BEGIN TRANSACTION;
CREATE TABLE [dbo].[__EFMigrationsHistory] (
    [MigrationId] nvarchar(150) NOT NULL,
    [ProductVersion] nvarchar(32) NOT NULL,
    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
);
INSERT INTO [dbo].[__EFMigrationsHistory] VALUES
    (N'20261006170711_InitialSchema', N'10.0.12');
COMMIT;
```

The statements deliberately fail if a history table already exists. Inspect that
database's migration history rather than overwriting it. A failed uniqueness upgrade
requires resolving the conflicting data before retrying; never skip the migration.

Example duplicate checks, valid for both providers:

```sql
SELECT Username, COUNT(*) FROM Persons WHERE Username IS NOT NULL GROUP BY Username HAVING COUNT(*) > 1;
SELECT Email, COUNT(*) FROM Persons WHERE Email IS NOT NULL GROUP BY Email HAVING COUNT(*) > 1;
SELECT Slug, COUNT(*) FROM Articles WHERE Slug IS NOT NULL GROUP BY Slug HAVING COUNT(*) > 1;
```

## Future model changes

Generate migrations for both provider contexts:

```sh
dotnet ef migrations add ChangeName --context SqliteConduitContext --project src/Conduit --output-dir Infrastructure/Migrations/Sqlite
dotnet ef migrations add ChangeName --context SqlServerConduitContext --project src/Conduit --output-dir Infrastructure/Migrations/SqlServer
```

Convert each generated migration's namespace to file-scoped form before building
the next migration (the repository enforces this style), then format and validate.
Review generated SQL for destructive operations and test upgrades on existing data,
not just empty databases. Integration tests cover fresh SQLite migrations, an
explicit legacy baseline, and SQL Server model/script generation; they do not replace
an upgrade rehearsal on a live SQL Server instance.
