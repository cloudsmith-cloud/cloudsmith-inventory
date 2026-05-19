// Copyright 2026 CloudSmith Contributors
// SPDX-License-Identifier: Apache-2.0

using CloudSmith.Inventory.Services;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;

namespace CloudSmith.Inventory;

public static class InventoryExtensions
{
    public static IServiceCollection AddCloudSmithInventory(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddScoped<IVirtualMachineService, PostgresVirtualMachineService>();
        services.AddScoped<IWorkloadService, PostgresWorkloadService>();

        services.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(InventoryExtensions).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        return services;
    }

    public static IServiceProvider MigrateInventoryDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateUp();
        return services;
    }
}
