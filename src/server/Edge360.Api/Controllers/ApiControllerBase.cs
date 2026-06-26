using Edge360.Application.Common.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Edge360.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    private ICurrentUser? _currentUser;

    protected ICurrentUser CurrentUser =>
        _currentUser ??= HttpContext.RequestServices.GetRequiredService<ICurrentUser>();

    protected Guid CurrentUserId => CurrentUser.RequireUserId();
}
