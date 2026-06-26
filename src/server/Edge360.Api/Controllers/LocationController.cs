using Edge360.Application.Common.Models;
using Edge360.Application.Locations;
using Edge360.Application.Locations.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edge360.Api.Controllers;

[Authorize]
[Route("api/location")]
public sealed class LocationController : ApiControllerBase
{
    private readonly LocationService _locations;

    public LocationController(LocationService locations) => _locations = locations;

    /// <summary>Ingests a location sample for the authenticated user.</summary>
    [HttpPost]
    public async Task<ActionResult<LocationDto>> Record(RecordLocationRequest request, CancellationToken ct) =>
        Ok(await _locations.RecordAsync(CurrentUserId, request, ct));

    /// <summary>Latest known location for each sharing member of a group.</summary>
    [HttpGet("latest")]
    public async Task<ActionResult<IReadOnlyList<LocationDto>>> Latest([FromQuery] Guid groupId, CancellationToken ct) =>
        Ok(await _locations.GetLatestAsync(CurrentUserId, groupId, ct));

    /// <summary>Paged location history for a member within a group.</summary>
    [HttpGet("history")]
    public async Task<ActionResult<PagedResult<LocationDto>>> History(
        [FromQuery] Guid groupId,
        [FromQuery] Guid subjectUserId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken ct = default) =>
        Ok(await _locations.GetHistoryAsync(CurrentUserId,
            new LocationHistoryQuery(groupId, subjectUserId, from, to, page, pageSize), ct));
}
