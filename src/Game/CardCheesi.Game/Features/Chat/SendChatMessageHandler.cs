using System.Text.Json;
using CardCheesi.Core;
using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Abstractions.DomainModels;

namespace CardCheesi.Game.Features.Chat;

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

        var message = new ChatMessage(
            command.GameCode,
            command.SenderId,
            command.SenderName,
            command.Text,
            DateTimeOffset.UtcNow);

        var payload = JsonSerializer.Serialize(new
        {
            senderId = message.SenderId.ToString(),
            senderName = message.SenderName,
            text = message.Text,
            timestamp = message.Timestamp.ToString("o"),
        });

        await connectionManager.BroadcastAsync(command.GameCode, new SseEvent("chat-message", payload), ct);
    }
}
