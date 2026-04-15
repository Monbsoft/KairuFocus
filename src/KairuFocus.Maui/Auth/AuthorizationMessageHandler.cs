using System.Net.Http.Headers;

namespace KairuFocus.Maui.Auth;

public sealed class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly MauiAuthService _authService;

    public AuthorizationMessageHandler(MauiAuthService authService)
        => _authService = authService;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _authService.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}
