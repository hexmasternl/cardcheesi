namespace CardCheesi.Chat.Features.Chat;

public sealed record SendChatMessageCommand(
    string GameCode,
    Guid SenderId,
    string SenderName,
    string Text);
