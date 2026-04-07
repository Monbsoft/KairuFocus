using Kairu.Application.Tasks.Commands.AddTask;
using Monbsoft.BrilliantMediator;

[assembly: BrilliantMediatorGenerator(
    Namespace = "Kairu.Api.Generated",
    Assemblies = [typeof(AddTaskCommandHandler)])]
