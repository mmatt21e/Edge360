using System.Security.Cryptography;
using Edge360.Domain.Common;

namespace Edge360.Domain.Entities;

/// <summary>
/// A family/group aggregate. Owns members, places and the events scoped to it.
/// All authorization in the system is ultimately anchored to group membership.
/// </summary>
public class Group : Entity
{
    // Unambiguous alphabet (no 0/O/1/I) for human-friendly invite codes.
    private const string InviteAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public string Name { get; set; } = null!;

    /// <summary>Short shareable code used to join the group.</summary>
    public string InviteCode { get; set; } = null!;

    public Guid CreatedByUserId { get; set; }

    public ICollection<GroupMembership> Memberships { get; set; } = new List<GroupMembership>();
    public ICollection<Place> Places { get; set; } = new List<Place>();
    public ICollection<SafetyEvent> Events { get; set; } = new List<SafetyEvent>();

    /// <summary>Generates a cryptographically-random invite code of the given length.</summary>
    public static string GenerateInviteCode(int length = 8)
    {
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));

        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            var index = RandomNumberGenerator.GetInt32(InviteAlphabet.Length);
            chars[i] = InviteAlphabet[index];
        }
        return new string(chars);
    }
}
