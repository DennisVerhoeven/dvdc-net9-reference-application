using Projects;

var builder = DistributedApplication.CreateBuilder(args);


// TODO : Not entirely convinced about aspire postgres local dev experience for now
// Docker compose (solution wide or 1 per project) with db/redis config feels easier to deploy and configure
// var postgres = builder.AddPostgres("fluffy-postgres")
//     .WithPgAdmin();
//

builder.AddProject<Fluffy>("fluffy-api");

builder.Build().Run();