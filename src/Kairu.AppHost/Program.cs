var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Kairu_Api>("api");

builder.AddProject<Projects.Kairu_Web>("web")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
