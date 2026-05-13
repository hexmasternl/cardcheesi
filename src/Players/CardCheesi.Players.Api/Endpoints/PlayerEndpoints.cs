namespace CardCheesi.Players.Api.Endpoints;

public static class PlayerEndpoints
{
    public static IEndpointRouteBuilder MapPlayersEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapRegisterPlayer();
        app.MapRefreshToken();
        return app;
    }
}
