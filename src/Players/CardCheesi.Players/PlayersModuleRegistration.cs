using CardCheesi.Core;
using CardCheesi.Players.Features.RefreshToken;
using CardCheesi.Players.Features.RegisterPlayer;
using Microsoft.Extensions.DependencyInjection;

namespace CardCheesi.Players;

public static class PlayersModuleRegistration
{
    public static IServiceCollection AddPlayersModule(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<RegisterPlayerCommand, RegisterPlayerResult>, RegisterPlayerHandler>();
        services.AddScoped<ICommandHandler<RefreshTokenCommand, RefreshTokenResult?>, RefreshTokenHandler>();

        services.AddHostedService<DatabaseMigrationWorker>();
        services.AddHostedService<PlayerCleanupService>();

        return services;
    }
}
