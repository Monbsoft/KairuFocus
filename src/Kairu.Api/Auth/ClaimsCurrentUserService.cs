using System.Security.Claims;
using Kairu.Application.Common;
using Kairu.Domain.Identity;

namespace Kairu.Api.Auth;

public sealed class ClaimsCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClaimsCurrentUserService(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public UserId CurrentUserId
    {
        get
        {
            var sub = _httpContextAccessor.HttpContext?.User
                .FindFirst("sub")?.Value
                ?? _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(sub))
                throw new UnauthorizedAccessException("User is not authenticated.");

            if (!Guid.TryParse(sub, out var guid))
                throw new UnauthorizedAccessException("User identifier is not a valid GUID.");

            return UserId.From(guid);
        }
    }
}
