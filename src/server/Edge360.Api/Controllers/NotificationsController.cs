using Edge360.Application.Common.Models;
using Edge360.Application.Events;
using Edge360.Application.Events.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edge360.Api.Controllers;

[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController : ApiControllerBase
{
    private readonly EventService _events;

    public NotificationsController(EventService events) => _events = events;

    /// <summary>Lists the caller's notification (alert) delivery records, newest first.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<AlertDto>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
        Ok(await _events.ListMyAlertsAsync(CurrentUserId, page, pageSize, ct));
}
