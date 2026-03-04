using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.JSInterop;

namespace Kairudev.Web.Auth;

public sealed class AuthService
{
    private const string TokenKey = "kairudev_jwt";
    private readonly IJSRuntime _js;

    public AuthService(IJSRuntime js) => _js = js;

    public async Task<string?> GetTokenAsync()
        => await _js.InvokeAsync<string?>("localStorage.getItem", TokenKey);

    public async Task SetTokenAsync(string token)
        => await _js.InvokeVoidAsync("localStorage.setItem", TokenKey, token);

    public async Task RemoveTokenAsync()
        => await _js.InvokeVoidAsync("localStorage.removeItem", TokenKey);

    public async Task<ClaimsPrincipal> GetCurrentUserAsync()
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrEmpty(token))
            return new ClaimsPrincipal(new ClaimsIdentity());

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            if (jwt.ValidTo < DateTime.UtcNow)
            {
                await RemoveTokenAsync();
                return new ClaimsPrincipal(new ClaimsIdentity());
            }

            var identity = new ClaimsIdentity(jwt.Claims, "jwt");
            return new ClaimsPrincipal(identity);
        }
        catch
        {
            await RemoveTokenAsync();
            return new ClaimsPrincipal(new ClaimsIdentity());
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var principal = await GetCurrentUserAsync();
        return principal.Identity?.IsAuthenticated == true;
    }
}
