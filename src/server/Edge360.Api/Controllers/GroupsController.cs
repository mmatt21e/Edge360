using Edge360.Application.Groups;
using Edge360.Application.Groups.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edge360.Api.Controllers;

[Authorize]
public sealed class GroupsController : ApiControllerBase
{
    private readonly GroupService _groups;

    public GroupsController(GroupService groups) => _groups = groups;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GroupDto>>> List(CancellationToken ct) =>
        Ok(await _groups.ListForUserAsync(CurrentUserId, ct));

    [HttpPost]
    public async Task<ActionResult<GroupDto>> Create(CreateGroupRequest request, CancellationToken ct) =>
        Ok(await _groups.CreateAsync(CurrentUserId, request, ct));

    [HttpPost("join")]
    public async Task<ActionResult<GroupDto>> Join(JoinGroupRequest request, CancellationToken ct) =>
        Ok(await _groups.JoinAsync(CurrentUserId, request, ct));

    [HttpGet("{groupId:guid}/members")]
    public async Task<ActionResult<IReadOnlyList<GroupMemberDto>>> Members(Guid groupId, CancellationToken ct) =>
        Ok(await _groups.GetMembersAsync(CurrentUserId, groupId, ct));
}
