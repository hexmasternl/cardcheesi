using System.Text.Json;
using CardCheesi.Core;
using CardCheesi.Game.Abstractions;

namespace CardCheesi.Chat.Features.Chat;

public sealed class SendChatMessageHandler(
    IGameRepository repo,
    ISseConnectionManager connectionManager) : ICommandHandler<SendChatMessageCommand>
{
    private const int MaxTextLength = 500;

    public async Task Handle(SendChatMessageCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Text))
            throw new DomainException("Chat message text cannot be empty.");

        if (command.Text.Length > MaxTextLength)
            throw new DomainException($"Chat message text cannot exceed {MaxTextLength} characters.");

        var game = await repo.GetByCodeAsync(command.GameCode, ct)
            ?? throw new NotFoundException($"Game '{command.GameCode}' not found.");

        if (game.Players.All(p => p.Id != command.SenderId))
            throw new ForbiddenException("You are not a member of this game.");

        var payload = JsonSerializer.Serialize(new
        {
            senderId = command.SenderId.ToString(),
            senderName = command.SenderName,
            text = command.Text,
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
        });

        await connectionManager.BroadcastAsync(command.GameCode, new SseEvent("chat-message", payload), ct);
    }
}
