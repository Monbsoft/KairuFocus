using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace KairuFocus.Api.Tests.Auth;

/// <summary>
/// Issues a bearer token accepted by the API's default JwtBearer scheme in the Testing environment,
/// so that integration tests can reach <c>[Authorize]</c> endpoints through the real authentication pipeline.
/// </summary>
/// <remarks>
/// The key is <see cref="KairuFocusApiFactory.JwtSecretKey"/>, which the factory passes to Program.cs as
/// <c>Jwt:SecretKey</c>. The <c>sub</c> claim carries the user id as a GUID, as <c>JwtTokenService</c> does and as
/// <c>ClaimsCurrentUserService</c> expects.
/// </remarks>
public static class TestJwt
{
    public static string Create(Guid userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(KairuFocusApiFactory.JwtSecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: [new Claim("sub", userId.ToString())],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
