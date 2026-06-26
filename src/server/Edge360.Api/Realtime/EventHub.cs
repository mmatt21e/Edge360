using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Edge360.Application.Common.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Edge360.Api.Realtime;

/// <summary>Real-time safety-event channel, mirrored on the same per-group subscription model.</summary>
[Authorize]
public sealed class EventHub : Hub
{
    private readonly IAppDbContext _db;

    public EventHub(IAppDbContext db) => _db = db;

    public static string GroupChannel(Guid groupId) => $"group:{groupId}";

    public override async Task OnConnectedAsync()
    {
        var sub = Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (Guid.TryParse(sub, out var userId))
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
}
