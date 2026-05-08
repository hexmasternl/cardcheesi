var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .AddDatabase("gamedb");

var api = builder.AddProject<Projects.CardCheesi_Game_Api>("cardcheesi-game-api")
    .WithReference(postgres)
    .WaitFor(postgres);

#pragma warning disable ASPIREBROWSERLOGS001
builder.AddJavaScriptApp("cardcheesi-frontend", "../../App")
    .WithHttpEndpoint(port: 4200, env: "PORT")
    .WithArgs("--port", "4200", "--host", "0.0.0.0")
    .WithReference(api)
    .WithExternalHttpEndpoints()
    .WithBrowserLogs();
#pragma warning restore ASPIREBROWSERLOGS001

builder.Build().Run();

