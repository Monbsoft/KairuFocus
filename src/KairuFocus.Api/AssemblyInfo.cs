using KairuFocus.Application.Tasks.Commands.AddTask;
using Monbsoft.BrilliantMediator;

[assembly: BrilliantMediatorGenerator(
    Namespace = "KairuFocus.Api.Generated",
    Assemblies = [typeof(AddTaskCommandHandler)])]
