using iMessageSDK.Tests.Fixtures;

namespace iMessageSDK.Tests;

public class DiagnosticsTests
{
    [Fact]
    public async Task CheckDatabaseAsync_ReportsMissingFile_WhenPathDoesNotExist()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"does-not-exist-{Guid.NewGuid():N}.db");
        var client = await MessageClient.CreateAsync(new MessageClientOptions { MessagesDatabasePath = missingPath });

        try
        {
            var database = await client.Diagnostics.CheckDatabaseAsync();
            Assert.False(database.Exists);
            Assert.False(database.IsReadable);
            Assert.False(database.IsSchemaSupported);

            var fullDiskAccess = await client.Diagnostics.CheckFullDiskAccessAsync();
            Assert.False(fullDiskAccess.IsGranted);
        }
        finally
        {
            await client.DisposeAsync();
        }
    }

    [Fact]
    public async Task CheckDatabaseAsync_ReportsHealthy_ForRecognizedFixtureSchema()
    {
        await using var database = await TestChatDatabase.CreateAsync();
        var client = await MessageClient.CreateAsync(new MessageClientOptions { MessagesDatabasePath = database.Path });

        try
        {
            var result = await client.Diagnostics.CheckDatabaseAsync();
            Assert.True(result.Exists);
            Assert.True(result.IsReadable);
            Assert.True(result.IsSchemaSupported);

            var fullDiskAccess = await client.Diagnostics.CheckFullDiskAccessAsync();
            Assert.True(fullDiskAccess.IsGranted);
        }
        finally
        {
            await client.DisposeAsync();
        }
    }

    [Fact]
    public async Task RunAsync_DoesNotThrow_RegardlessOfMachinePermissionState()
    {
        await using var database = await TestChatDatabase.CreateAsync();
        var client = await MessageClient.CreateAsync(new MessageClientOptions { MessagesDatabasePath = database.Path });

        try
        {
            var report = await client.Diagnostics.RunAsync();

            Assert.NotNull(report.FullDiskAccess);
            Assert.NotNull(report.AutomationPermission);
            Assert.NotNull(report.MessagesAppAvailability);
            Assert.NotNull(report.Database);
        }
        finally
        {
            await client.DisposeAsync();
        }
    }
}
