// Copyright 2026 CloudSmith Contributors
// SPDX-License-Identifier: Apache-2.0

using CloudSmith.Inventory.Models;
using Npgsql;

namespace CloudSmith.Inventory.Services;

public sealed class PostgresWorkloadService : IWorkloadService
{
    private readonly NpgsqlDataSource _db;

    public PostgresWorkloadService(NpgsqlDataSource db) => _db = db;

    public async Task<Guid> CreateWorkloadAsync(CreateWorkloadRequest req, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        await using var cmd = _db.CreateCommand("""
            INSERT INTO inventory.workloads (workload_id, org_id, name, description, workload_type)
            VALUES ($1, $2, $3, $4, $5)
            """);
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(req.OrgId);
        cmd.Parameters.AddWithValue(req.Name);
        cmd.Parameters.AddWithValue((object?)req.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue(req.WorkloadType.ToString().ToLowerInvariant());
        await cmd.ExecuteNonQueryAsync(ct);
        return id;
    }

    public async Task<WorkloadDetail?> GetWorkloadAsync(Guid workloadId, Guid orgId, CancellationToken ct = default)
    {
        await using var cmd = _db.CreateCommand("""
            SELECT workload_id, org_id, name, description, workload_type, created_at
            FROM inventory.workloads
            WHERE workload_id = $1 AND org_id = $2
            """);
        cmd.Parameters.AddWithValue(workloadId);
        cmd.Parameters.AddWithValue(orgId);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        return new WorkloadDetail(
            r.GetGuid(0),
            r.GetGuid(1),
            r.GetString(2),
            r.IsDBNull(3) ? null : r.GetString(3),
            Enum.Parse<WorkloadType>(r.GetString(4), ignoreCase: true),
            r.GetFieldValue<DateTimeOffset>(5));
    }

    public async Task<IReadOnlyList<WorkloadSummary>> ListWorkloadsAsync(Guid orgId, CancellationToken ct = default)
    {
        await using var cmd = _db.CreateCommand("""
            SELECT w.workload_id, w.org_id, w.name, w.workload_type,
                   COUNT(v.vm_id) AS vm_count, w.created_at
            FROM inventory.workloads w
            LEFT JOIN inventory.virtual_machines v
                ON v.workload_id = w.workload_id AND v.org_id = w.org_id
            WHERE w.org_id = $1
            GROUP BY w.workload_id, w.org_id, w.name, w.workload_type, w.created_at
            ORDER BY w.name
            """);
        cmd.Parameters.AddWithValue(orgId);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var list = new List<WorkloadSummary>();
        while (await r.ReadAsync(ct))
        {
            list.Add(new WorkloadSummary(
                r.GetGuid(0),
                r.GetGuid(1),
                r.GetString(2),
                Enum.Parse<WorkloadType>(r.GetString(3), ignoreCase: true),
                (int)r.GetInt64(4),
                r.GetFieldValue<DateTimeOffset>(5)));
        }
        return list;
    }

    public async Task DeleteWorkloadAsync(Guid workloadId, Guid orgId, CancellationToken ct = default)
    {
        // Nulls out workload_id on VMs before deleting the workload
        await using var nullCmd = _db.CreateCommand("""
            UPDATE inventory.virtual_machines SET workload_id = NULL
            WHERE workload_id = $1 AND org_id = $2
            """);
        nullCmd.Parameters.AddWithValue(workloadId);
        nullCmd.Parameters.AddWithValue(orgId);
        await nullCmd.ExecuteNonQueryAsync(ct);

        await using var delCmd = _db.CreateCommand("""
            DELETE FROM inventory.workloads WHERE workload_id = $1 AND org_id = $2
            """);
        delCmd.Parameters.AddWithValue(workloadId);
        delCmd.Parameters.AddWithValue(orgId);
        await delCmd.ExecuteNonQueryAsync(ct);
    }
}
