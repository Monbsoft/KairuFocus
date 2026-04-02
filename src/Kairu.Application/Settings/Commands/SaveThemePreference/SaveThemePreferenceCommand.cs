using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairu.Application.Settings.Commands.SaveThemePreference;

public sealed record SaveThemePreferenceCommand(string ThemePreference) : ICommand<SaveThemePreferenceResult>;
