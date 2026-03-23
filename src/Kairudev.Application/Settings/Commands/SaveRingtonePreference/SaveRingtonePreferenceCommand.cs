using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairudev.Application.Settings.Commands.SaveRingtonePreference;

public sealed record SaveRingtonePreferenceCommand(string RingtonePreference) : ICommand<SaveRingtonePreferenceResult>;
