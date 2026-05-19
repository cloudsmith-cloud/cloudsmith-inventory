// Copyright 2026 CloudSmith Contributors
// SPDX-License-Identifier: Apache-2.0

using CloudSmith.Inventory.Models;
using Npgsql;

namespace CloudSmith.Inventory.Services;

public sealed class PostgresVirtualMachineService : IVirtualMachineService
{
    private readonly NpgsqlDataSource _db;

    public PostgresVirtualMachineService(NpgsqlDataSource db) => _db = db;

    public async Task<Guid> RegisterVmAsync(RegisterVmRequest req, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        await using var cmd = _db.CreateCommand("""
            INSERT INTO inventory.virtual_machines
                (vm_id, org_id, cluster_id, node_id, name, vm_guid,
                 generation, cpu_count, memory_mb, state)
            VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, 'unknown')
            ON CONFLICT (cluster_id, vm_guid) WHERE vm_guid IS NOT NULL DO UPDATE
                SET node_id = EXCLUDED.node_id,
                    name    = EXCLUDED.name,
                    cpu_count = EXCLUDED.cpu_count,
                    memory_mb = EXCLUDED.memory_mb,
                    last_seen = now()
            RETURNING vm_id
            """);
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(req.OrgId);
        cmd.Parameters.AddWithValue(req.ClusterId);
        cmd.Parameters.AddWithValue((object?)req.NodeId ?? DBNull.Value);
        cmd.Parameters.AddWithValue(req.Name);
        cmd.Parameters.AddWithValue((object?)req.VmGuid ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)req.Generation ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)req.CpuCount ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)req.MemoryMb ?? DBNull.Value);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is Guid g ? g : id;
    }

    public async Task<VmDetail?> GetVmAsync(Guid vmId, Guid orgId, CancellationToken ct = default)
    {
        await using var cmd = _db.CreateCommand("""
            SELECT vm_id, org_id, cluster_id, node_id, name, vm_guid,
                   generation, cpu_count, memory_mb, state, workload_id,
                   registered_at, last_seen
            FROM inventory.virtual_machines
            WHERE vm_id = $1 AND org_id = $2
            """);
        cmd.Parameters.AddWithValue(vmId);
        cmd.Parameters.AddWithValue(orgId);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        return MapDetail(r);
    }

    public async Task<IReadOnlyList<VmSummary>> ListByClusterAsync(Guid clusterId, Guid orgId, CancellationToken ct = default)
    {
        await using var cmd = _db.CreateCommand("""
            SELECT vm_id, org_id, cluster_id, node_id, name, vm_guid,
                   state, cpu_count, memory_mb, workload_id, last_seen
            FROM inventory.virtual_machines
            WHERE cluster_id = $1 AND org_id = $2
            ORDER BY name
            """);
        cmd.Parameters.AddWithValue(clusterId);
        cmd.Parameters.AddWithValue(orgId);
        return await ReadSummaries(cmd, ct);
    }

    public async Task<IReadOnlyList<VmSummary>> ListByNodeAsync(Guid nodeId, Guid orgId, CancellationToken ct = default)
    {
        await using var cmd = _db.CreateCommand("""
            SELECT vm_id, org_id, cluster_id, node_id, name, vm_guid,
                   state, cpu_count, memory_mb, workload_id, last_seen
            FROM inventory.virtual_machines
            WHERE node_id = $1 AND org_id = $2
            ORDER BY name
            """);
        cmd.Parameters.AddWithValue(nodeId);
        cmd.Parameters.AddWithValue(orgId);
        return await ReadSummaries(cmd, ct);
    }

    public async Task<IReadOnlyList<VmSummary>> ListByWorkloadAsync(Guid workloadId, Guid orgId, CancellationToken ct = default)
    {
        await using var cmd = _db.CreateCommand("""
            SELECT vm_id, org_id, cluster_id, node_id, name, vm_guid,
                   state, cpu_count, memory_mb, workload_id, last_seen
            FROM inventory.virtual_machines
            WHERE workload_id = $1 AND org_id = $2
            ORDER BY name
            """);
        cmd.Parameters.AddWithValue(workloadId);
        cmd.Parameters.AddWithValue(orgId);
        return await ReadSummaries(cmd, ct);
    }

    public async Task UpdateStateAsync(Guid vmId, Guid orgId, VmState state, CancellationToken ct = default)
    {
        await using var cmd = _db.CreateCommand("""
            UPDATE inventory.virtual_machines
            SET state = $3, last_seen = now()
            WHERE vm_id = $1 AND org_id = $2
            """);
        cmd.Parameters.AddWithValue(vmId);
        cmd.Parameters.AddWithValue(orgId);
        cmd.Parameters.AddWithValue(state.ToString().ToLowerInvariant());
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task AssignWorkloadAsync(Guid vmId, Guid orgId, Guid? workloadId, CancellationToken ct = default)
    {
        await using var cmd = _db.CreateCommand("""
            UPDATE inventory.virtual_machines
            SET workload_id = $3
            WHERE vm_id = $1 AND org_id = $2
            """);
        cmd.Parameters.AddWithValue(vmId);
        cmd.Parameters.AddWithValue(orgId);
        cmd.Parameters.AddWithValue((object?)workloadId ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task DeregisterAsync(Guid vmId, Guid orgId, CancellationToken ct = default)
    {
        await using var cmd = _db.CreateCommand("""
            DELETE FROM inventory.virtual_machines WHERE vm_id = $1 AND org_id = $2
            """);
        cmd.Parameters.AddWithValue(vmId);
        cmd.Parameters.AddWithValue(orgId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task<IReadOnlyList<VmSummary>> ReadSummaries(NpgsqlCommand cmd, CancellationToken ct)
    {
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var list = new List<VmSummary>();
        while (await r.ReadAsync(ct))
        {
            list.Add(new VmSummary(
                r.GetGuid(0),
                r.GetGuid(1),
                r.GetGuid(2),
                r.IsDBNull(3) ? null : r.GetGuid(3),
                r.GetString(4),
                r.IsDBNull(5) ? null : r.GetString(5),
                Enum.Parse<VmState>(r.GetString(6), ignoreCase: true),
                r.IsDBNull(7) ? null : r.GetInt32(7),
                r.IsDBNull(8) ? null : r.GetInt64(8),
                r.IsDBNull(9) ? null : r.GetGuid(9),
                r.IsDBNull(10) ? DateTimeOffset.MinValue : r.GetFieldValue<DateTimeOffset>(10)));
        }
        return list;
    }

    private static VmDetail MapDetail(NpgsqlDataReader r) => new(
        r.GetGuid(0),
        r.GetGuid(1),
        r.GetGuid(2),
        r.IsDBNull(3) ? null : r.GetGuid(3),
        r.GetString(4),
        r.IsDBNull(5) ? null : r.GetString(5),
        r.IsDBNull(6) ? null : r.GetInt32(6),
        r.IsDBNull(7) ? null : r.GetInt32(7),
        r.IsDBNull(8) ? null : r.GetInt64(8),
        Enum.Parse<VmState>(r.GetString(9), ignoreCase: true),
        r.IsDBNull(10) ? null : r.GetGuid(10),
        r.GetFieldValue<DateTimeOffset>(11),
        r.IsDBNull(12) ? null : r.GetFieldValue<DateTimeOffset>(12));
}
