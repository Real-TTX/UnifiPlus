using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace UnifiPlus.Web.Services;

public sealed class DemoClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        return Task.FromResult(principal);
    }
}
