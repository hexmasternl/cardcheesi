using CardCheesi.Core;
using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Abstractions.DataTransferObjects;
using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.Features.CreateGame;
using CardCheesi.Game.Features.DisposeHand;
using CardCheesi.Game.Features.GetGame;
using CardCheesi.Game.Features.JoinGame;
using CardCheesi.Game.Features.MakeMove;
using CardCheesi.Game.Persistence;
using CardCheesi.Game.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CardCheesi.Game;

public static class GameModuleRegistration
{
    public static IServiceCollection AddGameModule(this IServiceCollection services)
    {
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddSingleton<ISseConnectionManager, SseConnectionManager>();
        services.AddSingleton<IPlayerPresenceTracker, PlayerPresenceTracker>();
        services.AddScoped<ISseGameEventService, SseGameEventService>();

        services.AddScoped<ICommandHandler<CreateGameCommand, CreateGameResult>, CreateGameHandler>();
        services.AddScoped<ICommandHandler<JoinGameCommand, JoinGameResult>, JoinGameHandler>();
        services.AddScoped<ICommandHandler<MakeMoveCommand, MakeMoveResult>, MakeMoveHandler>();
        services.AddScoped<ICommandHandler<DisposeHandCommand, DisposeHandResult>, DisposeHandHandler>();
        services.AddScoped<IQueryHandler<GetGameQuery, GameDto?>, GetGameHandler>();

        return services;
    }
}
