using Microsoft.AspNetCore.Components.Authorization;

namespace Kairudev.Web.Auth;

public sealed class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly AuthService _authService;

    public JwtAuthenticationStateProvider(AuthService authService)
        => _authService = authService;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var principal = await _authService.GetCurrentUserAsync();
        return new AuthenticationState(principal);
    }

    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
