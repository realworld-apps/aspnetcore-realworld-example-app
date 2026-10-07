using Microsoft.EntityFrameworkCore;

namespace Conduit.Infrastructure;

// Provider-specific context types keep EF migration snapshots independent.
public class SqliteConduitContext(DbContextOptions<SqliteConduitContext> options)
    : ConduitContext(options);
