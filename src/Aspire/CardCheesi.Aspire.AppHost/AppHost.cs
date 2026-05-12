var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var gamedb = postgres.AddDatabase("gamedb");

var playersApi = builder.AddProject<Projects.CardCheesi_Players_Api>("cardcheesi-players-api")
    .WithReference(gamedb)
    .WaitFor(gamedb);

var gameApi = builder.AddProject<Projects.CardCheesi_Game_Api>("cardcheesi-game-api")
    .WithReference(gamedb)
    .WaitFor(gamedb);

var proxy = builder.AddProject<Projects.CardCheesi_Proxy>("cardcheesi-proxy")
    .WithReference(playersApi)
    .WithReference(gameApi)
    .WaitFor(playersApi)
    .WaitFor(gameApi);

builder.AddNpmApp("frontend", "../../../App", "start:aspire")
    .WithHttpEndpoint(port: 4300, env: "PORT")
    .WithExternalHttpEndpoints()
    .WithEnvironment("API_URL", proxy.GetEndpoint("http"))
    .WaitFor(proxy);

builder.Build().Run();

