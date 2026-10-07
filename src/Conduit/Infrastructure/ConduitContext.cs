using System;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Infrastructure.Errors;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Conduit.Infrastructure;

public class ConduitContext(DbContextOptions options) : DbContext(options)
{
    private static readonly string[] UniqueFields = ["Username", "Email", "Slug"];
    private IDbContextTransaction? _currentTransaction;

    public DbSet<Article> Articles { get; init; } = null!;
    public DbSet<Comment> Comments { get; init; } = null!;
    public DbSet<Person> Persons { get; init; } = null!;
    public DbSet<Tag> Tags { get; init; } = null!;
    public DbSet<ArticleTag> ArticleTags { get; init; } = null!;
    public DbSet<ArticleFavorite> ArticleFavorites { get; init; } = null!;
    public DbSet<FollowedPeople> FollowedPeople { get; init; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Person>(b =>
        {
            b.Property(x => x.Username).HasMaxLength(256);
            b.Property(x => x.Email).HasMaxLength(320);
            b.HasIndex(x => x.Username).IsUnique();
            b.HasIndex(x => x.Email).IsUnique();
        });
        // timestamps are stored as UTC; restore the DateTimeKind lost by providers like SQLite so
        // they serialize with the trailing 'Z' the RealWorld spec relies on
        modelBuilder.Entity<Article>(b =>
        {
            b.Property(x => x.Slug).HasMaxLength(450);
            b.HasIndex(x => x.Slug).IsUnique();
            b.Property(x => x.CreatedAt)
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            b.Property(x => x.UpdatedAt)
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });

        modelBuilder.Entity<Comment>(b =>
        {
            b.Property(x => x.CreatedAt)
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            b.Property(x => x.UpdatedAt)
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });

        modelBuilder.Entity<ArticleTag>(b =>
        {
            b.HasKey(t => new { t.ArticleId, t.TagId });

            b.HasOne(pt => pt.Article)
                .WithMany(p => p.ArticleTags)
                .HasForeignKey(pt => pt.ArticleId);

            b.HasOne(pt => pt.Tag).WithMany(t => t.ArticleTags).HasForeignKey(pt => pt.TagId);
        });

        modelBuilder.Entity<ArticleFavorite>(b =>
        {
            b.HasKey(t => new { t.ArticleId, t.PersonId });

            b.HasOne(pt => pt.Article)
                .WithMany(p => p.ArticleFavorites)
                .HasForeignKey(pt => pt.ArticleId);

            b.HasOne(pt => pt.Person)
                .WithMany(t => t.ArticleFavorites)
                .HasForeignKey(pt => pt.PersonId);
        });

        modelBuilder.Entity<FollowedPeople>(b =>
        {
            b.HasKey(t => new { t.ObserverId, t.TargetId });

            // we need to add OnDelete RESTRICT otherwise for the SqlServer database provider,
            // app.ApplicationServices.GetRequiredService<ConduitContext>().Database.EnsureCreated(); throws the following error:
            // System.Data.SqlClient.SqlException
            // HResult = 0x80131904
            // Message = Introducing FOREIGN KEY constraint 'FK_FollowedPeople_Persons_TargetId' on table 'FollowedPeople' may cause cycles or multiple cascade paths.Specify ON DELETE NO ACTION or ON UPDATE NO ACTION, or modify other FOREIGN KEY constraints.
            // Could not create constraint or index. See previous errors.
            b.HasOne(pt => pt.Observer)
                .WithMany(p => p.Followers)
                .HasForeignKey(pt => pt.ObserverId)
                .OnDelete(DeleteBehavior.Restrict);

            // we need to add OnDelete RESTRICT otherwise for the SqlServer database provider,
            // app.ApplicationServices.GetRequiredService<ConduitContext>().Database.EnsureCreated(); throws the following error:
            // System.Data.SqlClient.SqlException
            // HResult = 0x80131904
            // Message = Introducing FOREIGN KEY constraint 'FK_FollowingPeople_Persons_TargetId' on table 'FollowedPeople' may cause cycles or multiple cascade paths.Specify ON DELETE NO ACTION or ON UPDATE NO ACTION, or modify other FOREIGN KEY constraints.
            // Could not create constraint or index. See previous errors.
            b.HasOne(pt => pt.Target)
                .WithMany(t => t.Following)
                .HasForeignKey(pt => pt.TargetId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    #region Transaction Handling
    public async Task BeginTransactionAsync(CancellationToken cancellationToken)
    {
        if (_currentTransaction != null)
        {
            return;
        }

        if (Database.IsRelational())
        {
            _currentTransaction = await Database.BeginTransactionAsync(
                Database.IsSqlite() ? IsolationLevel.Serializable : IsolationLevel.ReadCommitted,
                cancellationToken
            );
        }
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
            }
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        try
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync();
            }
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
            }
            _currentTransaction = null;
        }
    }
    #endregion

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException
                    is SqliteException { SqliteExtendedErrorCode: 2067 or 1555 }
                        or SqlException { Number: 2601 or 2627 }
            )
        {
            // Match only known identity indexes; unrelated constraint failures remain server errors.
            var detail = exception.InnerException.Message;
            var field = UniqueFields.FirstOrDefault(name =>
                detail.Contains($"Persons.{name}", StringComparison.Ordinal)
                || detail.Contains($"Articles.{name}", StringComparison.Ordinal)
                || detail.Contains($"IX_Persons_{name}", StringComparison.Ordinal)
                || detail.Contains($"IX_Articles_{name}", StringComparison.Ordinal)
            );
            if (field == null)
            {
                throw;
            }
            throw new RestException(
                HttpStatusCode.Conflict,
                field.ToLowerInvariant(),
                Constants.IN_USE
            );
        }
    }
}
