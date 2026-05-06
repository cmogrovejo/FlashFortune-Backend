using FlashFortune.Domain.Exceptions;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace FlashFortune.API.Middleware;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            await WriteResponse(context, HttpStatusCode.BadRequest, new
            {
                type = "validation_error",
                errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            });
        }
        catch (RaffleLockedException ex)
        {
            await WriteResponse(context, HttpStatusCode.Conflict, new { type = "raffle_locked", message = ex.Message });
        }
        catch (DomainException ex)
        {
            await WriteResponse(context, HttpStatusCode.BadRequest, new { type = "domain_error", message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteResponse(context, HttpStatusCode.InternalServerError, new { type = "server_error", message = "An unexpected error occurred." });
        }
    }

    private static Task WriteResponse(HttpContext context, HttpStatusCode status, object body)
    {
        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }
}
