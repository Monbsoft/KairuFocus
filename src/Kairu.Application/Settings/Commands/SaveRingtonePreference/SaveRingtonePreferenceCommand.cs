using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairu.Application.Settings.Commands.SaveRingtonePreference;

public sealed record SaveRingtonePreferenceCommand(string RingtonePreference) : ICommand<SaveRingtonePreferenceResult>;
