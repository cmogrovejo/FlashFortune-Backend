using System.Security.Claims;

namespace FlashFortune.API.Middleware;

/// <summary>
/// Validates that the BusinessUnitId in the JWT claim matches the X-Unit-Id header.
/// Prevents a user from accessing data of a different Business Unit by spoofing the header.
/// </summary>
public sealed class TenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tokenUnitId = context.User.FindFirstValue("unit_id");
            var headerUnitId = context.Request.Headers["X-Unit-Id"].FirstOrDefault();

            if (!string.IsNullOrEmpty(headerUnitId) && tokenUnitId != headerUnitId)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Business Unit mismatch.");
                return;
            }
        }

        await next(context);
    }
}
