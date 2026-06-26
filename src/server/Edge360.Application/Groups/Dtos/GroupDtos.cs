namespace Edge360.Application.Groups.Dtos;

public sealed record CreateGroupRequest(string Name);

public sealed record JoinGroupRequest(string InviteCode);

public sealed record GroupDto(
    Guid Id,
    string Name,
    string InviteCode,
    string Role,
    int MemberCount,
    DateTimeOffset CreatedAt);

public sealed record GroupMemberDto(
    Guid UserId,
    string DisplayName,
    string Email,
    string Role,
    bool LocationSharingEnabled,
    DateTimeOffset JoinedAt);
