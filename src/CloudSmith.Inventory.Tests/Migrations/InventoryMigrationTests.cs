// Copyright 2026 CloudSmith Contributors
// SPDX-License-Identifier: Apache-2.0

using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace CloudSmith.Inventory.Tests.Migrations;

public sealed class InventoryMigrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public Task InitializeAsync() => _pg.StartAsync();
    public Task DisposeAsync()    => _pg.DisposeAsync().AsTask();

    private IServiceProvider BuildServices()
    {
        return new ServiceCollection()
            .AddSingleton(NpgsqlDataSource.Create(_pg.GetConnectionString()))
            .AddCloudSmithInventory(_pg.GetConnectionString())
            .BuildServiceProvider();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Migration_CreatesAllInventoryTables()
    {
        var services = BuildServices();
        services.MigrateInventoryDatabase();

        await using var conn = new NpgsqlConnection(_pg.GetConnectionString());
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT table_name FROM information_schema.tables
            WHERE table_schema = 'inventory'
            ORDER BY table_name
            """;
        await using var reader = await cmd.ExecuteReaderAsync();
        var tables = new List<string>();
        while (await reader.ReadAsync()) tables.Add(reader.GetString(0));

        Assert.Contains("virtual_machines", tables);
        Assert.Contains("workloads",        tables);
        Assert.Contains("snapshots",        tables);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Migration_IsIdempotent()
    {
        var services = BuildServices();
        services.MigrateInventoryDatabase();
        services.MigrateInventoryDatabase();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task VirtualMachines_HasPartialUniqueIndex_OnVmGuid()
    {
        var services = BuildServices();
        services.MigrateInventoryDatabase();

        await using var conn = new NpgsqlConnection(_pg.GetConnectionString());
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT indexname FROM pg_indexes
            WHERE tablename = 'virtual_machines'
              AND schemaname = 'inventory'
              AND indexdef LIKE '%vm_guid%'
            """;
        var indexName = (string?)await cmd.ExecuteScalarAsync();
        Assert.NotNull(indexName);
    }
}
