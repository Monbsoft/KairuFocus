var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.KairuFocus_Api>("api");

builder.AddProject<Projects.KairuFocus_Web>("web")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
