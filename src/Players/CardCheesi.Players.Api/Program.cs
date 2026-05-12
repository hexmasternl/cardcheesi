using CardCheesi.Auth;
using CardCheesi.Core;
using CardCheesi.Game.Persistence;
using CardCheesi.Players.Api;
using CardCheesi.Players.Api.Endpoints;
using CardCheesi.Players.Api.Features.RefreshToken;
using CardCheesi.Players.Api.Features.RegisterPlayer;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddJwtBearerAuthentication();

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.AddNpgsqlDbContext<AppDbContext>("gamedb");
}

builder.Services.AddScoped<ICommandHandler<RegisterPlayerCommand, RegisterPlayerResult>, RegisterPlayerHandler>();
builder.Services.AddScoped<ICommandHandler<RefreshTokenCommand, RefreshTokenResult?>, RefreshTokenHandler>();

builder.Services.AddHostedService<DatabaseMigrationWorker>();
builder.Services.AddHostedService<PlayerCleanupService>();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            title = "An unexpected error occurred.",
            status = 500,
        });
    });
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

var api = app.MapGroup("/api");
api.MapRegisterPlayer();
api.MapRefreshToken();

app.Run();
