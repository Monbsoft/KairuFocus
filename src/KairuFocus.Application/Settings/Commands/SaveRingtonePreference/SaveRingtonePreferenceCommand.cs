using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Settings.Commands.SaveRingtonePreference;

public sealed record SaveRingtonePreferenceCommand(string RingtonePreference) : ICommand<SaveRingtonePreferenceResult>;
