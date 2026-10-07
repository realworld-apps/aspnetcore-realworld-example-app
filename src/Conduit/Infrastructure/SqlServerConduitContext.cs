using Microsoft.EntityFrameworkCore;

namespace Conduit.Infrastructure;

public class SqlServerConduitContext(DbContextOptions<SqlServerConduitContext> options)
    : ConduitContext(options);
