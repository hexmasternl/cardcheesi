using System.Text;
using CardCheesi.Game;
using CardCheesi.Game.Api;
using CardCheesi.Game.Api.Auth;
using CardCheesi.Game.Api.Endpoints.Games;
using CardCheesi.Game.Api.Endpoints.Players;
using CardCheesi.Game.Api.Features.RefreshToken;
using CardCheesi.Game.Api.Features.RegisterPlayer;
using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Skip Npgsql registration in test environments to avoid real DB connections
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.AddNpgsqlDbContext<AppDbContext>("gamedb");
}

builder.Services.AddGameModule();

builder.Services.AddScoped<ICommandHandler<RegisterPlayerCommand, RegisterPlayerResult>, RegisterPlayerHandler>();
builder.Services.AddScoped<ICommandHandler<RefreshTokenCommand, RefreshTokenResult?>, RefreshTokenHandler>();

builder.Services.AddHostedService<DatabaseMigrationWorker>();
builder.Services.AddHostedService<PlayerCleanupService>();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

// JWT configuration
builder.Services.AddOptions<JwtSettings>()
    .BindConfiguration(JwtSettings.SectionName)
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<JwtSettings>, JwtSettingsValidator>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                          ?? new JwtSettings();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
            ClockSkew = TimeSpan.Zero,
        };
    });
builder.Services.AddAuthorization();

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

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapRegisterPlayer();
app.MapRefreshToken();
app.MapGameEndpoints();

app.Run();

public partial class Program { }


