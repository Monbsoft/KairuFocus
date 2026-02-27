using Kairudev.Application.Settings.Commands.SaveThemePreference;
using Kairudev.Application.Settings.Queries.GetUserSettings;
using Microsoft.AspNetCore.Mvc;

namespace Kairudev.Api.Settings;

[ApiController]
[Route("api/settings")]
public sealed class SettingsController : ControllerBase
{
    private readonly GetUserSettingsQueryHandler _getUserSettings;
    private readonly SaveThemePreferenceCommandHandler _saveThemePreference;

    public SettingsController(
        GetUserSettingsQueryHandler getUserSettings,
        SaveThemePreferenceCommandHandler saveThemePreference)
    {
        _getUserSettings = getUserSettings;
        _saveThemePreference = saveThemePreference;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var query = new GetUserSettingsQuery();
        var result = await _getUserSettings.Handle(query);
        return Ok(result.Settings);
    }

    [HttpPut("theme")]
    public async Task<IActionResult> SaveThemePreference([FromBody] SaveThemePreferenceRequest request)
    {
        var command = new SaveThemePreferenceCommand(request.ThemePreference);
        var result = await _saveThemePreference.Handle(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }
}

public sealed record SaveThemePreferenceRequest(string ThemePreference);
