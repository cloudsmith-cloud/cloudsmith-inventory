// Copyright 2026 CloudSmith Contributors
// SPDX-License-Identifier: Apache-2.0

using FluentMigrator;

namespace CloudSmith.Inventory.Migrations;

[Migration(20260519005)]
public sealed class M20260519005_CreateInventorySchema : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE SCHEMA IF NOT EXISTS inventory");

        Execute.Sql("""
            CREATE TABLE IF NOT EXISTS inventory.workloads (
                workload_id   UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                org_id        UUID NOT NULL,
                name          TEXT NOT NULL,
                description   TEXT,
                workload_type TEXT NOT NULL DEFAULT 'vm-group',
                created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
                UNIQUE (org_id, name)
            )
            """);

        Execute.Sql("CREATE INDEX IF NOT EXISTS idx_workloads_org_id ON inventory.workloads (org_id)");

        Execute.Sql("""
            CREATE TABLE IF NOT EXISTS inventory.virtual_machines (
                vm_id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                org_id        UUID NOT NULL,
                cluster_id    UUID NOT NULL,
                node_id       UUID,
                name          TEXT NOT NULL,
                vm_guid       TEXT,
                generation    INT,
                cpu_count     INT,
                memory_mb     BIGINT,
                state         TEXT NOT NULL DEFAULT 'unknown',
                workload_id   UUID REFERENCES inventory.workloads(workload_id) ON DELETE SET NULL,
                registered_at TIMESTAMPTZ NOT NULL DEFAULT now(),
                last_seen     TIMESTAMPTZ,
                UNIQUE (cluster_id, vm_guid) WHERE vm_guid IS NOT NULL
            )
            """);

        Execute.Sql("CREATE INDEX IF NOT EXISTS idx_vms_org_id      ON inventory.virtual_machines (org_id)");
        Execute.Sql("CREATE INDEX IF NOT EXISTS idx_vms_cluster_id  ON inventory.virtual_machines (cluster_id)");
        Execute.Sql("CREATE INDEX IF NOT EXISTS idx_vms_node_id     ON inventory.virtual_machines (node_id) WHERE node_id IS NOT NULL");
        Execute.Sql("CREATE INDEX IF NOT EXISTS idx_vms_workload_id ON inventory.virtual_machines (workload_id) WHERE workload_id IS NOT NULL");
        Execute.Sql("CREATE INDEX IF NOT EXISTS idx_vms_state       ON inventory.virtual_machines (state)");

        Execute.Sql("""
            CREATE TABLE IF NOT EXISTS inventory.snapshots (
                snapshot_id    UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                org_id         UUID NOT NULL,
                cluster_id     UUID NOT NULL,
                captured_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
                vm_count       INT NOT NULL DEFAULT 0,
                running_count  INT NOT NULL DEFAULT 0,
                stopped_count  INT NOT NULL DEFAULT 0,
                other_count    INT NOT NULL DEFAULT 0,
                details        JSONB
            )
            """);

        Execute.Sql("CREATE INDEX IF NOT EXISTS idx_snapshots_cluster_id ON inventory.snapshots (cluster_id, captured_at DESC)");
    }

    public override void Down() { }
}
