using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Edge360.Application.Common.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Edge360.Api.Realtime;

/// <summary>
/// Real-time location channel. On connect, the caller is subscribed to a SignalR group per
/// family group they belong to; server-side broadcasts target those groups.
/// </summary>
[Authorize]
public sealed class LocationHub : Hub
{
    private readonly IAppDbContext _db;

    public LocationHub(IAppDbContext db) => _db = db;

    public static string GroupChannel(Guid groupId) => $"group:{groupId}";

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId is not null)
        {
            var groupIds = await _db.Memberships
                .Where(m => m.UserId == userId)
                .Select(m => m.GroupId)
                .ToListAsync(Context.ConnectionAborted);

            foreach (var groupId in groupIds)
                await Groups.AddToGroupAsync(Context.ConnectionId, GroupChannel(groupId));
        }

        await base.OnConnectedAsync();
    }

    private Guid? GetUserId()
    {
        var sub = Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
