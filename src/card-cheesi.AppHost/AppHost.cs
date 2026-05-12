var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var gamedb = postgres.AddDatabase("gamedb");

var frontend = builder.AddNpmApp("frontend", "../App", "start:aspire")
    .WithHttpEndpoint(port: 4300, env: "PORT")
    .WithExternalHttpEndpoints();

var playersApi = builder.AddProject<Projects.CardCheesi_Players_Api>("cardcheesi-players-api")
    .WithReference(gamedb)
    .WaitFor(gamedb)
    .WithEnvironment("Cors__AllowedOrigin", frontend.GetEndpoint("http"));

var gameApi = builder.AddProject<Projects.CardCheesi_Game_Api>("cardcheesi-game-api")
    .WithReference(gamedb)
    .WaitFor(gamedb)
    .WithEnvironment("Cors__AllowedOrigin", frontend.GetEndpoint("http"));

var gateway = builder.AddYarp("gateway")
    .WithConfiguration(yarp =>
    {
        yarp.AddRoute("/api/players/{**catch-all}", playersApi);
        yarp.AddRoute("/api/games/{**catch-all}", gameApi);
    })
    .WaitFor(playersApi)
    .WaitFor(gameApi);

frontend
    .WithEnvironment("API_URL", gateway.GetEndpoint("http"))
    .WaitFor(gateway);

builder.Build().Run();
