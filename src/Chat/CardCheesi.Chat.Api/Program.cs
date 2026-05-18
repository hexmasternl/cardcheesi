using CardCheesi.Chat;
using CardCheesi.Chat.Api;
using CardCheesi.Chat.Api.Endpoints;
using CardCheesi.Game;
using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddCorsDefaults();
builder.AddJwtBearerAuthentication();

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.AddNpgsqlDbContext<AppDbContext>("gamedb");
}

builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddSingleton<ISseConnectionManager, SseConnectionManager>();
builder.Services.AddChatModule();

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

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

var api = app.MapGroup("/api");
api.MapChatEndpoints();

app.Run();

public partial class Program { }
