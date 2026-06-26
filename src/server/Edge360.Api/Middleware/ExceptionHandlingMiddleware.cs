using System.Net;
using Edge360.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Edge360.Api.Middleware;

/// <summary>Translates application exceptions into RFC 7807 ProblemDetails responses.</summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await WriteProblemAsync(context, ex);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception ex)
    {
        var (status, title) = ex switch
        {
            AppValidationException => (HttpStatusCode.BadRequest, "Validation failed"),
            NotFoundException => (HttpStatusCode.NotFound, "Resource not found"),
            ConflictException => (HttpStatusCode.Conflict, "Conflict"),
            ForbiddenException => (HttpStatusCode.Forbidden, "Forbidden"),
            UnauthorizedException => (HttpStatusCode.Unauthorized, "Unauthorized"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
        };

        if (status == HttpStatusCode.InternalServerError)
            _logger.LogError(ex, "Unhandled exception processing {Path}", context.Request.Path);
        else
            _logger.LogInformation("Handled {Type}: {Message}", ex.GetType().Name, ex.Message);

        var problem = new ProblemDetails
        {
            Status = (int)status,
            Title = title,
            Detail = status == HttpStatusCode.InternalServerError ? "An unexpected error occurred." : ex.Message,
            Instance = context.Request.Path
        };

        if (ex is AppValidationException validation)
            problem.Extensions["errors"] = validation.Errors;

        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    }
}
