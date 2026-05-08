var builder = DistributedApplication.CreateBuilder(args);


builder.AddProject<Projects.CardCheesi_Game_Api>("cardcheesi-game-api");


builder.Build().Run();
