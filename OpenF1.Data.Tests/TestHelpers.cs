using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace OpenF1.Data.Tests;

public sealed class TestHelpers
{
    public static (SqliteConnection connection, LiveTimingDbContext dbContext, IDbContextFactory<LiveTimingDbContext> factory) CreateDbContext()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        var dbContextOptions = new DbContextOptionsBuilder()
            .UseSqlite(connection)
            .Options;
        var dbContext = new LiveTimingDbContext(dbContextOptions);
        dbContext.Database.EnsureCreated();

        var factory = Substitute.For<IDbContextFactory<LiveTimingDbContext>>();
        factory.CreateDbContextAsync().ReturnsForAnyArgs(Task.FromResult(dbContext));
        factory.CreateDbContext().ReturnsForAnyArgs(dbContext);

        return (connection, dbContext, factory);
    }
}

