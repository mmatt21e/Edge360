using Edge360.Application.Common;
using Edge360.Application.Common.Abstractions;
using Edge360.Application.Common.Exceptions;
using Edge360.Application.Groups.Dtos;
using Edge360.Domain.Entities;
using Edge360.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Edge360.Application.Groups;

/// <summary>Create/join groups, list a user's groups, and enumerate members.</summary>
public sealed class GroupService
{
    private readonly IAppDbContext _db;
    private readonly GroupAccess _access;
    private readonly IAuditWriter _audit;

    public GroupService(IAppDbContext db, GroupAccess access, IAuditWriter audit)
    {
        _db = db;
        _access = access;
        _audit = audit;
    }

    public async Task<GroupDto> CreateAsync(Guid userId, CreateGroupRequest request, CancellationToken ct = default)
    {
        var group = new Group
        {
            Name = request.Name.Trim(),
            InviteCode = await GenerateUniqueInviteCodeAsync(ct),
            CreatedByUserId = userId
        };

        var membership = new GroupMembership
        {
            Group = group,
            UserId = userId,
            Role = MemberRole.Admin
        };

        _db.Groups.Add(group);
        _db.Memberships.Add(membership);
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync("group.create", userId, nameof(Group), group.Id.ToString(), ct: ct);

        return new GroupDto(group.Id, group.Name, group.InviteCode, membership.Role.ToString(), 1, group.CreatedAt);
    }

    public async Task<GroupDto> JoinAsync(Guid userId, JoinGroupRequest request, CancellationToken ct = default)
    {
        var code = request.InviteCode.Trim().ToUpperInvariant();
        var group = await _db.Groups.FirstOrDefaultAsync(g => g.InviteCode == code, ct)
                    ?? throw new NotFoundException("No group matches that invite code.");

        var alreadyMember = await _db.Memberships.AnyAsync(m => m.GroupId == group.Id && m.UserId == userId, ct);
        if (alreadyMember)
            throw new ConflictException("You are already a member of this group.");

        _db.Memberships.Add(new GroupMembership
        {
            GroupId = group.Id,
            UserId = userId,
            Role = MemberRole.Member
        });
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync("group.join", userId, nameof(Group), group.Id.ToString(), ct: ct);

        var memberCount = await _db.Memberships.CountAsync(m => m.GroupId == group.Id, ct);
        return new GroupDto(group.Id, group.Name, group.InviteCode, MemberRole.Member.ToString(), memberCount, group.CreatedAt);
    }

    public async Task<IReadOnlyList<GroupDto>> ListForUserAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.Memberships
            .Where(m => m.UserId == userId)
            .Select(m => new GroupDto(
                m.Group.Id,
                m.Group.Name,
                m.Group.InviteCode,
                m.Role.ToString(),
                m.Group.Memberships.Count,
                m.Group.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<GroupMemberDto>> GetMembersAsync(Guid userId, Guid groupId, CancellationToken ct = default)
    {
        await _access.RequireMembershipAsync(groupId, userId, ct);

        return await _db.Memberships
            .Where(m => m.GroupId == groupId)
            .OrderBy(m => m.Role)
            .Select(m => new GroupMemberDto(
                m.UserId,
                m.User.DisplayName,
                m.User.Email,
                m.Role.ToString(),
                m.LocationSharingEnabled,
                m.CreatedAt))
            .ToListAsync(ct);
    }

    private async Task<string> GenerateUniqueInviteCodeAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = Group.GenerateInviteCode();
            if (!await _db.Groups.AnyAsync(g => g.InviteCode == code, ct))
                return code;
        }
        throw new ConflictException("Could not allocate a unique invite code; please retry.");
    }
}
