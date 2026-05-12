var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var gamedb = postgres.AddDatabase("gamedb");

var api = builder.AddProject<Projects.CardCheesi_Game_Api>("api")
    .WithReference(gamedb)
    .WaitFor(gamedb);

builder.AddNpmApp("frontend", "../App", "start:aspire")
    .WithHttpEndpoint(port: 4300, env: "PORT")
    .WithExternalHttpEndpoints()
    .WaitFor(api);

builder.Build().Run();
