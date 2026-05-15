using System.Security.Claims;
using CardCheesi.Chat.Features.Chat;
using CardCheesi.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CardCheesi.Chat.Api.Endpoints;

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/chat").WithTags("Chat");

        group.MapPost("/{code}", SendChatMessage)
            .WithName("SendChatMessage")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi();

        return app;
    }

    private static async Task<IResult> SendChatMessage(
        string code,
        SendChatMessageRequest request,
        HttpContext httpContext,
        ICommandHandler<SendChatMessageCommand> handler,
        CancellationToken ct)
    {
        var playerId = Guid.Parse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                                  ?? httpContext.User.FindFirstValue("sub")!);
        var playerName = httpContext.User.FindFirstValue(ClaimTypes.Name)
                         ?? httpContext.User.FindFirstValue("name")!;

        try
        {
            await handler.Handle(new SendChatMessageCommand(code, playerId, playerName, request.Text), ct);
            return Results.Ok();
        }
        catch (DomainException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (ForbiddenException)
        {
            return Results.Forbid();
        }
        catch (NotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }

    private sealed record SendChatMessageRequest(string Text);
}
