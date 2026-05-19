// Copyright 2026 CloudSmith Contributors
// SPDX-License-Identifier: Apache-2.0

namespace CloudSmith.Inventory.Models;

public enum WorkloadType { VmGroup, Service, Application }

public sealed record WorkloadSummary(
    Guid         WorkloadId,
    Guid         OrgId,
    string       Name,
    WorkloadType WorkloadType,
    int          VmCount,
    DateTimeOffset CreatedAt);

public sealed record WorkloadDetail(
    Guid         WorkloadId,
    Guid         OrgId,
    string       Name,
    string?      Description,
    WorkloadType WorkloadType,
    DateTimeOffset CreatedAt);

public sealed record CreateWorkloadRequest(
    Guid         OrgId,
    string       Name,
    string?      Description,
    WorkloadType WorkloadType);
