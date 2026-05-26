// Copyright 2026 CloudSmith Contributors
// SPDX-License-Identifier: Apache-2.0

using FluentMigrator;

namespace CloudSmith.Inventory.Migrations;

/// <summary>
/// Creates hardware profile catalog and drift detection tables (AB#1496).
/// hardware_catalog_profiles — expected firmware baseline per hardware model.
/// drift_reports / drift_items — detected deviations from expected baseline.
/// </summary>
[Migration(20260526003)]
public sealed class M20260526003_CreateHardwareCatalogAndDrift : Migration
{
    public override void Up()
    {
        Execute.Sql("""
            -- Hardware profile catalog: defines the expected firmware baseline for a hardware model.
            CREATE TABLE IF NOT EXISTS inventory.hardware_catalog_profiles (
                profile_id          uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
                org_id              uuid        NOT NULL,
                name                text        NOT NULL,
                manufacturer        text,
                model               text,
                os_version_target   text,
                description         text,
                components_json     jsonb       NOT NULL DEFAULT '[]',
                created_by_user_id  uuid,
                created_at          timestamptz NOT NULL DEFAULT now(),
                updated_at          timestamptz NOT NULL DEFAULT now(),
                UNIQUE (org_id, name)
            );

            CREATE INDEX IF NOT EXISTS ix_hw_profiles_org_id ON inventory.hardware_catalog_profiles (org_id);
            CREATE INDEX IF NOT EXISTS ix_hw_profiles_manufacturer ON inventory.hardware_catalog_profiles (org_id, manufacturer) WHERE manufacturer IS NOT NULL;

            -- Drift reports: one report per collection run per node where drift was detected.
            CREATE TABLE IF NOT EXISTS inventory.drift_reports (
                drift_report_id     uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
                org_id              uuid        NOT NULL,
                run_id              uuid,
                cluster_id          uuid        NOT NULL,
                node_id             uuid        NOT NULL,
                node_name           text,
                cluster_name        text,
                change_count        int         NOT NULL DEFAULT 0,
                highest_severity    text        NOT NULL DEFAULT 'Info'
                                                CHECK (highest_severity IN ('Info','Warning','Critical')),
                acknowledged        bool        NOT NULL DEFAULT false,
                acknowledged_at     timestamptz,
                acknowledged_by     uuid,
                acknowledge_note    text,
                detected_at         timestamptz NOT NULL DEFAULT now()
            );

            CREATE INDEX IF NOT EXISTS ix_drift_reports_org_id ON inventory.drift_reports (org_id, detected_at DESC);
            CREATE INDEX IF NOT EXISTS ix_drift_reports_cluster ON inventory.drift_reports (cluster_id);
            CREATE INDEX IF NOT EXISTS ix_drift_reports_node ON inventory.drift_reports (node_id);

            -- Drift items: individual field-level changes within a drift report.
            CREATE TABLE IF NOT EXISTS inventory.drift_items (
                drift_item_id   uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
                drift_report_id uuid        NOT NULL REFERENCES inventory.drift_reports (drift_report_id) ON DELETE CASCADE,
                org_id          uuid        NOT NULL,
                entity_type     text        NOT NULL,
                entity_name     text        NOT NULL,
                change_type     text        NOT NULL CHECK (change_type IN ('Added','Removed','Modified')),
                field_name      text        NOT NULL,
                previous_value  text,
                current_value   text,
                severity        text        NOT NULL DEFAULT 'Info'
                                            CHECK (severity IN ('Info','Warning','Critical')),
                acknowledged    bool        NOT NULL DEFAULT false
            );

            CREATE INDEX IF NOT EXISTS ix_drift_items_report_id ON inventory.drift_items (drift_report_id);
            """);
    }

    public override void Down() { }
}
