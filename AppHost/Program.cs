var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Fluffy>("fluffy-api");

builder.Build().Run();