using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Kairu.Application.Identity.Commands.GetOrCreateUser;
using Kairu.Application.OAuth;
using Kairu.Domain.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Monbsoft.BrilliantMediator.Abstractions;

namespace Kairu.Api.OAuth;

[ApiController]
[AllowAnonymous]
public sealed class OAuthController : ControllerBase
{
    private const string OAuthStateCookieName = ".Kairu.OAuthState";
    private const string DataProtectionPurpose = "Kairu.OAuth.State";
    private const int AuthorizationCodeTtlMinutes = 5;

    private readonly IAuthorizationCodeStore _codeStore;
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    private readonly IDataProtector _protector;
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(
        IAuthorizationCodeStore codeStore,
        IMediator mediator,
        IConfiguration configuration,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<OAuthController> logger)
    {
        _codeStore = codeStore;
        _mediator = mediator;
        _configuration = configuration;
        _protector = dataProtectionProvider.CreateProtector(DataProtectionPurpose);
        _logger = logger;
    }

    /// <summary>
    /// RFC 8414 — Authorization Server Metadata.
    /// Allows MCP clients to discover authorize + token endpoints.
    /// </summary>
    [HttpGet("/.well-known/oauth-authorization-server")]
    public IActionResult GetAuthServerMetadata()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        return Ok(new
        {
            issuer = baseUrl,
            authorization_endpoint = $"{baseUrl}/oauth/authorize",
            token_endpoint = $"{baseUrl}/oauth/token",
            response_types_supported = new[] { "code" },
            grant_types_supported = new[] { "authorization_code" },
            code_challenge_methods_supported = new[] { "S256" },
            token_endpoint_auth_methods_supported = new[] { "none" },
        });
    }

    /// <summary>
    /// OAuth 2.1 Authorization Endpoint.
    /// Validates PKCE params, stores state in encrypted cookie, challenges GitHub OAuth.
    /// </summary>
    [HttpGet("/oauth/authorize")]
    public IActionResult Authorize(
        [FromQuery] string? client_id,
        [FromQuery] string? redirect_uri,
        [FromQuery] string? response_type,
        [FromQuery] string? code_challenge,
        [FromQuery] string? code_challenge_method,
        [FromQuery] string? state,
        [FromQuery] string? scope)
    {
        // Validate required params
        if (response_type != "code")
            return BadRequest(new { error = "unsupported_response_type" });

        if (string.IsNullOrEmpty(code_challenge) || code_challenge_method != "S256")
            return BadRequest(new { error = "invalid_request", error_description = "PKCE S256 is required." });

        if (string.IsNullOrEmpty(redirect_uri))
            return BadRequest(new { error = "invalid_request", error_description = "redirect_uri is required." });

        // Store PKCE state in encrypted cookie (survives GitHub redirect)
        var statePayload = $"{code_challenge}|{redirect_uri}|{state ?? string.Empty}";
        var encrypted = _protector.Protect(statePayload);

        Response.Cookies.Append(OAuthStateCookieName, encrypted, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(10),
            Path = "/oauth"
        });

        // Challenge GitHub OAuth — redirect back to our MCP callback
        var callbackUrl = Url.Action(nameof(GitHubCallback), "OAuth", null, Request.Scheme);
        var properties = new AuthenticationProperties { RedirectUri = callbackUrl };
        return Challenge(properties, "GitHub");
    }

    /// <summary>
    /// Internal GitHub OAuth callback for MCP flow.
    /// Reads cookie state, creates user, generates authorization code, redirects to MCP client.
    /// </summary>
    [HttpGet("/oauth/github/callback")]
    public async Task<IActionResult> GitHubCallback(CancellationToken ct)
    {
        // Read and clear encrypted state cookie
        if (!Request.Cookies.TryGetValue(OAuthStateCookieName, out var encrypted) || string.IsNullOrEmpty(encrypted))
        {
            _logger.LogWarning("OAuth callback: missing state cookie");
            return BadRequest(new { error = "invalid_request", error_description = "Missing OAuth state." });
        }

        Response.Cookies.Delete(OAuthStateCookieName, new CookieOptions { Path = "/oauth" });

        string codeChallenge, redirectUri, state;
        try
        {
            var decrypted = _protector.Unprotect(encrypted);
            var parts = decrypted.Split('|', 3);
            if (parts.Length != 3) throw new FormatException();
            codeChallenge = parts[0];
            redirectUri = parts[1];
            state = parts[2];
        }
        catch
        {
            return BadRequest(new { error = "invalid_request", error_description = "Corrupted OAuth state." });
        }

        // Authenticate via GitHub cookie
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!result.Succeeded)
        {
            _logger.LogWarning("OAuth callback: GitHub auth failed: {Error}", result.Failure?.Message);
            return BadRequest(new { error = "access_denied" });
        }

        var githubId = result.Principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var login = result.Principal.FindFirst("urn:github:login")?.Value
            ?? result.Principal.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
        var displayName = result.Principal.FindFirst(ClaimTypes.Name)?.Value ?? login;
        var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(githubId))
            return BadRequest(new { error = "access_denied", error_description = "No GitHub ID." });

        // Get or create Kairu user
        var command = new GetOrCreateUserCommand(githubId, login, displayName, email);
        var userResult = await _mediator.DispatchAsync<GetOrCreateUserCommand, Result<GetOrCreateUserResult>>(command, ct);

        if (userResult.IsFailure)
        {
            _logger.LogError("OAuth callback: user creation failed: {Error}", userResult.Error);
            return BadRequest(new { error = "server_error" });
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Generate authorization code (cryptographically random, single-use)
        var codeBytes = RandomNumberGenerator.GetBytes(32);
        var code = Convert.ToBase64String(codeBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');

        var entry = new AuthorizationCodeEntry(
            userResult.Value.UserId,
            codeChallenge,
            redirectUri,
            DateTime.UtcNow.AddMinutes(AuthorizationCodeTtlMinutes));

        await _codeStore.StoreAsync(code, entry, ct);

        // Redirect to MCP client with authorization code
        var separator = redirectUri.Contains('?') ? "&" : "?";
        var redirectUrl = $"{redirectUri}{separator}code={Uri.EscapeDataString(code)}";
        if (!string.IsNullOrEmpty(state))
            redirectUrl += $"&state={Uri.EscapeDataString(state)}";

        return Redirect(redirectUrl);
    }

    /// <summary>
    /// OAuth 2.1 Token Endpoint.
    /// Exchanges authorization code + PKCE verifier for a JWT access token.
    /// </summary>
    [HttpPost("/oauth/token")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Token(
        [FromForm] string? grant_type,
        [FromForm] string? code,
        [FromForm] string? code_verifier,
        [FromForm] string? redirect_uri,
        [FromForm] string? client_id,
        CancellationToken ct)
    {
        if (grant_type != "authorization_code")
            return BadRequest(new { error = "unsupported_grant_type" });

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(code_verifier) || string.IsNullOrEmpty(redirect_uri))
            return BadRequest(new { error = "invalid_request" });

        // Consume code (single-use)
        var entry = await _codeStore.ConsumeAsync(code, ct);
        if (entry is null)
            return BadRequest(new { error = "invalid_grant" });

        // Verify redirect_uri matches
        if (!string.Equals(entry.RedirectUri, redirect_uri, StringComparison.Ordinal))
            return BadRequest(new { error = "invalid_grant" });

        // Verify PKCE: BASE64URL(SHA256(code_verifier)) == code_challenge
        var challengeBytes = SHA256.HashData(Encoding.ASCII.GetBytes(code_verifier));
        var computedChallenge = Convert.ToBase64String(challengeBytes)
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        if (!string.Equals(computedChallenge, entry.CodeChallenge, StringComparison.Ordinal))
            return BadRequest(new { error = "invalid_grant" });

        // Generate JWT (same format as Web flow)
        var token = GenerateJwt(entry.UserId);
        var expiryHours = _configuration.GetValue<int>("Jwt:ExpiryHours", 24);

        return Ok(new
        {
            access_token = token,
            token_type = "Bearer",
            expires_in = expiryHours * 3600
        });
    }

    private string GenerateJwt(Domain.Identity.UserId userId)
    {
        var secretKey = _configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("Jwt:SecretKey is not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiryHours = _configuration.GetValue<int>("Jwt:ExpiryHours", 24);

        var claims = new[]
        {
            new Claim("sub", userId.Value.ToString()),
        };

        var jwtToken = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiryHours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(jwtToken);
    }
}
