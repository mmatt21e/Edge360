using Edge360.Application.Geofencing;
using Edge360.Application.Geofencing.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edge360.Api.Controllers;

[Authorize]
[Route("api/geofences")]
public sealed class GeofencesController : ApiControllerBase
{
    private readonly GeofenceService _geofences;

    public GeofencesController(GeofenceService geofences) => _geofences = geofences;

    /// <summary>Lists places (geofences) for a group the caller belongs to.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlaceDto>>> List([FromQuery] Guid groupId, CancellationToken ct) =>
        Ok(await _geofences.ListAsync(CurrentUserId, groupId, ct));

    [HttpPost]
    public async Task<ActionResult<PlaceDto>> Create(CreatePlaceRequest request, CancellationToken ct) =>
        Ok(await _geofences.CreateAsync(CurrentUserId, request, ct));

    [HttpPut("{placeId:guid}")]
    public async Task<ActionResult<PlaceDto>> Update(Guid placeId, UpdatePlaceRequest request, CancellationToken ct) =>
        Ok(await _geofences.UpdateAsync(CurrentUserId, placeId, request, ct));

    [HttpDelete("{placeId:guid}")]
    public async Task<IActionResult> Delete(Guid placeId, CancellationToken ct)
    {
        await _geofences.DeleteAsync(CurrentUserId, placeId, ct);
        return NoContent();
    }
}
