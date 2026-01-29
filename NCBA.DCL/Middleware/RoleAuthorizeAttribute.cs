using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NCBA.DCL.Models;

namespace NCBA.DCL.Middleware;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RoleAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly UserRole[] _allowedRoles;

    public RoleAuthorizeAttribute(params UserRole[] allowedRoles)
    {
        _allowedRoles = allowedRoles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Not authorized" });
            return;
        }

        var roleClaim = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(roleClaim))
        {
            context.Result = new ForbidResult();
            return;
        }

        if (!Enum.TryParse<UserRole>(roleClaim, out var userRole))
        {
            context.Result = new ForbidResult();
            return;
        }

        if (!_allowedRoles.Contains(userRole))
        {
            context.Result = new ObjectResult(new { message = "Access denied: insufficient permissions" })
            {
                StatusCode = 403
            };
        }
    }
}
