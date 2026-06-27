using Edge360.Application.Common.Models;
using Edge360.Application.Driving;
using Edge360.Application.Driving.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edge360.Api.Controllers;

[Authorize]
[Route("api/driving")]
public sealed class DrivingController : ApiControllerBase
{
    private readonly DrivingService _driving;

    public DrivingController(DrivingService driving) => _driving = driving;

    /// <summary>Reconstructs and scores trips from the caller's location history.</summary>
    [HttpPost("analyze")]
    public async Task<ActionResult<AnalyzeResultDto>> Analyze(AnalyzeDrivingRequest request, CancellationToken ct) =>
        Ok(await _driving.AnalyzeAsync(CurrentUserId, request, ct));

    /// <summary>Lists trips for the caller, or for a member sharing a group via subjectUserId.</summary>
    [HttpGet("trips")]
    public async Task<ActionResult<PagedResult<TripDto>>> ListTrips(
        [FromQuery] Guid? subjectUserId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default) =>
        Ok(await _driving.ListTripsAsync(CurrentUserId, subjectUserId, from, to, page, pageSize, ct));

    /// <summary>Trip detail including events and the reconstructed route.</summary>
    [HttpGet("trips/{tripId:guid}")]
    public async Task<ActionResult<TripDetailDto>> GetTrip(Guid tripId, CancellationToken ct) =>
        Ok(await _driving.GetTripAsync(CurrentUserId, tripId, ct));

    /// <summary>Aggregate driving score summary over a period.</summary>
    [HttpGet("score")]
    public async Task<ActionResult<DrivingScoreDto>> Score(
        [FromQuery] Guid? subjectUserId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken ct = default) =>
        Ok(await _driving.GetScoreSummaryAsync(CurrentUserId, subjectUserId, from, to, ct));
}
