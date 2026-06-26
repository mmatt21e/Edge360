using Edge360.Application.Common.Models;
using Edge360.Application.Events;
using Edge360.Application.Events.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edge360.Api.Controllers;

[Authorize]
[Route("api/events")]
public sealed class EventsController : ApiControllerBase
{
    private readonly EventService _events;

    public EventsController(EventService events) => _events = events;

    /// <summary>Paged activity/event feed for a group.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<EventDto>>> List(
        [FromQuery] Guid groupId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default) =>
        Ok(await _events.ListAsync(CurrentUserId, new EventQuery(groupId, page, pageSize), ct));

    /// <summary>Raises a critical SOS event and fans out alerts to other group members.</summary>
    [HttpPost("sos")]
    public async Task<ActionResult<EventDto>> Sos(SosRequest request, CancellationToken ct) =>
        Ok(await _events.RaiseSosAsync(CurrentUserId, request, ct));

    [HttpPost("{eventId:guid}/acknowledge")]
    public async Task<ActionResult<EventDto>> Acknowledge(Guid eventId, CancellationToken ct) =>
        Ok(await _events.AcknowledgeAsync(CurrentUserId, eventId, ct));
}
