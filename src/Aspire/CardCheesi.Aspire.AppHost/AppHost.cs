var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .AddDatabase("gamedb");

builder.AddProject<Projects.CardCheesi_Game_Api>("cardcheesi-game-api")
    .WithReference(postgres)
    .WaitFor(postgres);

builder.Build().Run();

