using Microsoft.EntityFrameworkCore;
using qc_authorization.Infrastructure.Data;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Infrastructure.IntegrationTests.Data;

using qc_authorization.Tests.TestSupport;

[TestFixture]
public class MigrationIntegrationTests
{
    [Test]
    public async Task Migration_Applies_On_Empty_Database()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"qc-migrate-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.MigrateAsync();

                (await context.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
                (await context.Permissions.CountAsync()).ShouldBe(0);
                await context.Database.CloseConnectionAsync();
            }
        }
        finally
        {
            try
            {
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }
            }
            catch (IOException)
            {
                // SQLite may still hold the file handle briefly on Windows.
            }
        }
    }
}
