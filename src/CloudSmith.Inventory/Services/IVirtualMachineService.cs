// Copyright 2026 CloudSmith Contributors
// SPDX-License-Identifier: Apache-2.0

using CloudSmith.Inventory.Models;

namespace CloudSmith.Inventory.Services;

public interface IVirtualMachineService
{
    Task<Guid> RegisterVmAsync(RegisterVmRequest request, CancellationToken ct = default);
    Task<VmDetail?> GetVmAsync(Guid vmId, Guid orgId, CancellationToken ct = default);
    Task<IReadOnlyList<VmSummary>> ListByClusterAsync(Guid clusterId, Guid orgId, CancellationToken ct = default);
    Task<IReadOnlyList<VmSummary>> ListByNodeAsync(Guid nodeId, Guid orgId, CancellationToken ct = default);
    Task<IReadOnlyList<VmSummary>> ListByWorkloadAsync(Guid workloadId, Guid orgId, CancellationToken ct = default);
    Task UpdateStateAsync(Guid vmId, Guid orgId, VmState state, CancellationToken ct = default);
    Task AssignWorkloadAsync(Guid vmId, Guid orgId, Guid? workloadId, CancellationToken ct = default);
    Task DeregisterAsync(Guid vmId, Guid orgId, CancellationToken ct = default);
}
