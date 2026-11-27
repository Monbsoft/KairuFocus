var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddDotnetProject("api", "../KairuFocus.Api/KairuFocus.Api.csproj");

builder.AddDotnetProject("web", "../KairuFocus.Web/KairuFocus.Web.csproj")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
