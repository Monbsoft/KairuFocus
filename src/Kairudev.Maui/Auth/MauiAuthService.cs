using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Kairudev.Maui.Auth;

public sealed class MauiAuthService
{
    private const string TokenKey = "kairudev_jwt";

    public async Task<string?> GetTokenAsync()
    {
        try { return await SecureStorage.Default.GetAsync(TokenKey); }
        catch { return null; }
    }

    public async Task SetTokenAsync(string token)
        => await SecureStorage.Default.SetAsync(TokenKey, token);

    public Task RemoveTokenAsync()
    {
        try { SecureStorage.Default.Remove(TokenKey); }
        catch { }
        return Task.CompletedTask;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrEmpty(token)) return false;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            return jwt.ValidTo > DateTime.UtcNow;
        }
        catch { return false; }
    }

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
            return new ClaimsPrincipal(new ClaimsIdentity(jwt.Claims, "jwt"));
        }
        catch { return new ClaimsPrincipal(new ClaimsIdentity()); }
    }
}
