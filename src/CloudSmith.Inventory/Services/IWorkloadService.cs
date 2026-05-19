// Copyright 2026 CloudSmith Contributors
// SPDX-License-Identifier: Apache-2.0

using CloudSmith.Inventory.Models;

namespace CloudSmith.Inventory.Services;

public interface IWorkloadService
{
    Task<Guid> CreateWorkloadAsync(CreateWorkloadRequest request, CancellationToken ct = default);
    Task<WorkloadDetail?> GetWorkloadAsync(Guid workloadId, Guid orgId, CancellationToken ct = default);
    Task<IReadOnlyList<WorkloadSummary>> ListWorkloadsAsync(Guid orgId, CancellationToken ct = default);
    Task DeleteWorkloadAsync(Guid workloadId, Guid orgId, CancellationToken ct = default);
}
