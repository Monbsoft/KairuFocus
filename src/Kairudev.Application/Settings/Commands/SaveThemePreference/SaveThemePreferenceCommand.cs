using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairudev.Application.Settings.Commands.SaveThemePreference;

public sealed record SaveThemePreferenceCommand(string ThemePreference) : ICommand<SaveThemePreferenceResult>;
