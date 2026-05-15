namespace CardCheesi.Game.Features.Chat;

public sealed record SendChatMessageCommand(
    string GameCode,
    Guid SenderId,
    string SenderName,
    string Text);
