using Kairudev.Application.Tasks.Commands.AddTask;
using Monbsoft.BrilliantMediator;

[assembly: BrilliantMediatorGenerator(
    Namespace = "Kairudev.Api.Generated",
    Assemblies = [typeof(AddTaskCommandHandler)])]
