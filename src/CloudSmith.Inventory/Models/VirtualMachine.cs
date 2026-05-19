// Copyright 2026 CloudSmith Contributors
// SPDX-License-Identifier: Apache-2.0

namespace CloudSmith.Inventory.Models;

public enum VmState { Unknown, Running, Stopped, Paused, Saved, Critical }

public sealed record VmSummary(
    Guid    VmId,
    Guid    OrgId,
    Guid    ClusterId,
    Guid?   NodeId,
    string  Name,
    string? VmGuid,
    VmState State,
    int?    CpuCount,
    long?   MemoryMb,
    Guid?   WorkloadId,
    DateTimeOffset LastSeen);

public sealed record VmDetail(
    Guid    VmId,
    Guid    OrgId,
    Guid    ClusterId,
    Guid?   NodeId,
    string  Name,
    string? VmGuid,
    int?    Generation,
    int?    CpuCount,
    long?   MemoryMb,
    VmState State,
    Guid?   WorkloadId,
    DateTimeOffset RegisteredAt,
    DateTimeOffset? LastSeen);

public sealed record RegisterVmRequest(
    Guid    OrgId,
    Guid    ClusterId,
    Guid?   NodeId,
    string  Name,
    string? VmGuid,
    int?    Generation,
    int?    CpuCount,
    long?   MemoryMb);
