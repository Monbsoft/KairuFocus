using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Settings.Commands.SaveThemePreference;

public sealed record SaveThemePreferenceCommand(string ThemePreference) : ICommand<SaveThemePreferenceResult>;
