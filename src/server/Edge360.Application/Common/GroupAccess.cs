using Edge360.Application.Common.Abstractions;
using Edge360.Application.Common.Exceptions;
using Edge360.Domain.Entities;
using Edge360.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Edge360.Application.Common;

/// <summary>
/// Shared authorization helpers anchoring access checks to group membership.
/// Every group-scoped use case routes through here so the rules live in one place.
/// </summary>
public sealed class GroupAccess
{
    private readonly IAppDbContext _db;

    public GroupAccess(IAppDbContext db) => _db = db;

    /// <summary>Returns the caller's membership in the group, or throws if they are not a member.</summary>
    public async Task<GroupMembership> RequireMembershipAsync(Guid groupId, Guid userId, CancellationToken ct = default)
    {
        var membership = await _db.Memberships
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId, ct);

        if (membership is null)
            throw new ForbiddenException("You are not a member of this group.");

        return membership;
    }

    /// <summary>Requires that the caller holds at least the given role (Admin &lt; Guardian &lt; Member ordering inverted).</summary>
    public async Task<GroupMembership> RequireRoleAsync(
        Guid groupId, Guid userId, MemberRole minimumRole, CancellationToken ct = default)
    {
        var membership = await RequireMembershipAsync(groupId, userId, ct);

        // Lower enum value == higher privilege (Admin=0, Guardian=1, Member=2).
        if (membership.Role > minimumRole)
            throw new ForbiddenException($"This action requires the {minimumRole} role or higher.");

        return membership;
    }
}
