using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TdpGis.Infrastructure.Persistence;
using Xunit;

namespace TdpGis.Infrastructure.Tests.Persistence;

internal static class SqliteDbContextFactory
{
    public static async Task<DbFixture> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        var options = new DbContextOptionsBuilder<GisAppDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new GisAppDbContext(options);
        await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        return new DbFixture(dbContext, connection);
    }

    internal sealed class DbFixture(GisAppDbContext dbContext, SqliteConnection connection) : IAsyncDisposable
    {
        public GisAppDbContext DbContext { get; } = dbContext;

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}