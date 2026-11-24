using System.Globalization;
using Microsoft.Data.SqlClient;
using Stockly.StocklyDbTestSupport;
using FluentAssertions;

namespace Stockly.ConcurrencyTests;

[Collection(SqlServerCollection.Name)]
public sealed class RowVersionConcurrencyTests(SqlServerFixture fixture) : IAsyncLifetime
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _unitId = Guid.NewGuid();
    private readonly Guid _firstDuplicateId = Guid.NewGuid();
    private readonly Guid _secondDuplicateId = Guid.NewGuid();
    private readonly Guid _transactionUnitId = Guid.NewGuid();
    private readonly Guid _rolledBackUnitId = Guid.NewGuid();
    private readonly string _code = $"CONC-{Guid.NewGuid():N}"[..32];
    private readonly string _duplicateCode = $"DUP-{Guid.NewGuid():N}"[..32];
    private readonly string _rollbackCode = $"ROLL-{Guid.NewGuid():N}"[..32];

    public async Task InitializeAsync()
    {
        fixture.RequireServer();
        var platform = await fixture.EnsurePlatformPrincipalAsync();
        await using var connection = await ScopedSql.OpenPlatformAsync(platform);

        await ScopedSql.ExecuteAsync(connection, $"""
            INSERT INTO [identity].[Tenant] (Id, Code, Name, Status, TimeZoneId, DefaultCulture, DateFormat, IsDeleted)
            VALUES ('{_tenantId:D}', N'{_code}', N'Concurrency test', 1, N'UTC', N'en-GB', N'dd/MM/yyyy', 0);
            """);

        await SeedUnitAsync(connection, _unitId, _code);
    }

    public async Task DisposeAsync()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        var platform = await fixture.EnsurePlatformPrincipalAsync();
        await using var connection = await ScopedSql.OpenPlatformAsync(platform);

        await ScopedSql.ExecuteAsync(connection, $"""
            DELETE FROM [master].[Unit]
            WHERE Id IN ('{_unitId:D}', '{_firstDuplicateId:D}', '{_secondDuplicateId:D}', '{_transactionUnitId:D}', '{_rolledBackUnitId:D}')
               OR Code IN (N'{_code}', N'{_duplicateCode}', N'{_rollbackCode}');
            """);

        await ScopedSql.ExecuteAsync(connection, $"DELETE FROM [identity].[Tenant] WHERE Id = '{_tenantId:D}';");
    }

    [Fact]
    public async Task Stale_rowversion_update_is_rejected()
    {
        var platform = fixture.PlatformPrincipalConnectionString;
        var tenant = fixture.TenantPrincipalConnectionString;

        byte[] rowVersion;
        await using (var groundTruth = await ScopedSql.OpenPlatformAsync(platform))
        {
            var rows = await ScopedSql.QueryAsync(groundTruth, $"""
                SELECT RowVersion FROM [master].[Unit] WHERE Id = '{_unitId:D}';
                """);
            rowVersion = (byte[])rows.Single()["RowVersion"]!;
        }

        await using var firstConnection = await ScopedSql.OpenTenantAsync(tenant, _tenantId);
        await using var secondConnection = await ScopedSql.OpenTenantAsync(tenant, _tenantId);

        var token = RowVersionLiteral(rowVersion);
        var first = ScopedSql.ExecuteAsync(firstConnection, $"""
            UPDATE [master].[Unit]
            SET [Name] = N'first'
            WHERE Id = '{_unitId:D}' AND TenantId = '{_tenantId:D}' AND RowVersion = {token};
            """);
        var second = ScopedSql.ExecuteAsync(secondConnection, $"""
            UPDATE [master].[Unit]
            SET [Name] = N'second'
            WHERE Id = '{_unitId:D}' AND TenantId = '{_tenantId:D}' AND RowVersion = {token};
            """);

        var results = await Task.WhenAll(first, second);
        results.Count(affected => affected == 1).Should().Be(1);
        results.Count(affected => affected == 0).Should().Be(1);

        await using var verification = await ScopedSql.OpenPlatformAsync(platform);
        var name = await ScopedSql.ScalarAsync<string?>(verification, $"""
            SELECT [Name] FROM [master].[Unit] WHERE Id = '{_unitId:D}';
            """);
        name.Should().BeOneOf("first", "second");
    }

    [Fact]
    public async Task Duplicate_business_key_race_allows_exactly_one_writer()
    {
        var tenant = fixture.TenantPrincipalConnectionString;

        await using var firstConnection = await ScopedSql.OpenTenantAsync(tenant, _tenantId);
        await using var secondConnection = await ScopedSql.OpenTenantAsync(tenant, _tenantId);

        var first = ScopedSql.ErrorNumberFromAsync(() => InsertUnitAsync(firstConnection, _firstDuplicateId, _duplicateCode));
        var second = ScopedSql.ErrorNumberFromAsync(() => InsertUnitAsync(secondConnection, _secondDuplicateId, _duplicateCode));

        var results = await Task.WhenAll(first, second);
        results.Count(error => error is null).Should().Be(1);
        results.Count(error => error is 2601 or 2627).Should().Be(1);

        await using var platform = await ScopedSql.OpenPlatformAsync(fixture.PlatformPrincipalConnectionString);
        var count = await ScopedSql.ScalarAsync<int>(platform, $"""
            SELECT COUNT(*) FROM [master].[Unit]
            WHERE TenantId = '{_tenantId:D}' AND Code = N'{_duplicateCode}';
            """);
        count.Should().Be(1);
    }

    [Fact]
    public async Task Failed_transaction_rolls_back_the_business_write()
    {
        var tenant = fixture.TenantPrincipalConnectionString;
        await using var connection = await ScopedSql.OpenTenantAsync(tenant, _tenantId);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        await using (var valid = connection.CreateCommand())
        {
            valid.Transaction = transaction;
            valid.CommandText = $"""
                INSERT INTO [master].[Unit] (Id, TenantId, Code, Name, UnitType, DecimalPlaces, IsBaseUnit, IsActive, IsDeleted)
                VALUES ('{_transactionUnitId:D}', '{_tenantId:D}', N'{_rollbackCode}', N'valid', 1, 0, 1, 1, 0);
                """;
            (await valid.ExecuteNonQueryAsync()).Should().Be(1);
        }

        SqlException? failure = null;
        await using (var invalid = connection.CreateCommand())
        {
            invalid.Transaction = transaction;
            invalid.CommandText = $"""
                INSERT INTO [master].[Unit] (Id, TenantId, Code, Name, UnitType, DecimalPlaces, IsBaseUnit, IsActive, IsDeleted)
                VALUES ('{_rolledBackUnitId:D}', '{_tenantId:D}', N'{_rollbackCode}-BAD', N'invalid', 99, 0, 1, 1, 0);
                """;

            try
            {
                await invalid.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                failure = ex;
            }
        }

        failure.Should().NotBeNull();
        failure!.Number.Should().Be(ScopedSql.CheckConstraintViolationErrorNumber);
        await transaction.RollbackAsync();

        await using var platform = await ScopedSql.OpenPlatformAsync(fixture.PlatformPrincipalConnectionString);
        var count = await ScopedSql.ScalarAsync<int>(platform, $"""
            SELECT COUNT(*) FROM [master].[Unit] WHERE TenantId = '{_tenantId:D}' AND Code LIKE N'{_rollbackCode}%';
            """);
        count.Should().Be(0);
    }

    private async Task SeedUnitAsync(SqlConnection connection, Guid unitId, string code) =>
        await ScopedSql.ExecuteAsync(connection, $"""
            INSERT INTO [master].[Unit] (Id, TenantId, Code, Name, UnitType, DecimalPlaces, IsBaseUnit, IsActive, IsDeleted)
            VALUES ('{unitId:D}', '{_tenantId:D}', N'{code}', N'Unit', 1, 0, 1, 1, 0);
            """);

    private Task<int> InsertUnitAsync(SqlConnection connection, Guid unitId, string code) =>
        ScopedSql.ExecuteAsync(connection, $"""
            INSERT INTO [master].[Unit] (Id, TenantId, Code, Name, UnitType, DecimalPlaces, IsBaseUnit, IsActive, IsDeleted)
            VALUES ('{unitId:D}', '{_tenantId:D}', N'{code}', N'Concurrent', 1, 0, 1, 1, 0);
            """);

    private static string RowVersionLiteral(byte[] value) =>
        "0x" + Convert.ToHexString(value).ToUpperInvariant();
}
