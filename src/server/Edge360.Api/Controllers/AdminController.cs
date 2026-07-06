using Edge360.Application.Admin;
using Edge360.Application.Admin.Dtos;
using Edge360.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edge360.Api.Controllers;

[Authorize(Roles = "Administrator")]
[Route("api/admin")]
public sealed class AdminController : ApiControllerBase
{
    private readonly AdminService _admin;

    public AdminController(AdminService admin) => _admin = admin;

    [HttpGet("stats")]
    public async Task<ActionResult<SystemStatsDto>> Stats(CancellationToken ct) =>
        Ok(await _admin.GetStatsAsync(ct));

    [HttpGet("users")]
    public async Task<ActionResult<PagedResult<AdminUserDto>>> Users(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
        Ok(await _admin.ListUsersAsync(page, pageSize, search, ct));

    [HttpPost("users/{userId:guid}/role")]
    public async Task<ActionResult<AdminUserDto>> SetRole(Guid userId, SetRoleRequest request, CancellationToken ct) =>
        Ok(await _admin.SetUserRoleAsync(CurrentUserId, userId, request.SystemRole, ct));

    [HttpPost("users/{userId:guid}/active")]
    public async Task<ActionResult<AdminUserDto>> SetActive(Guid userId, SetActiveRequest request, CancellationToken ct) =>
        Ok(await _admin.SetUserActiveAsync(CurrentUserId, userId, request.IsActive, ct));

    [HttpGet("groups")]
    public async Task<ActionResult<PagedResult<AdminGroupDto>>> Groups(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
        Ok(await _admin.ListGroupsAsync(page, pageSize, ct));

    [HttpGet("audit")]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> Audit(
        [FromQuery] string? action, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
        Ok(await _admin.ListAuditAsync(page, pageSize, action, ct));

    [HttpGet("retention")]
    public ActionResult<RetentionPolicyDto> Retention() => Ok(_admin.GetRetentionPolicy());

    [HttpPost("retention/run")]
    public async Task<ActionResult<RetentionResultDto>> RunRetention(CancellationToken ct) =>
        Ok(await _admin.RunRetentionAsync(CurrentUserId, ct));
}
