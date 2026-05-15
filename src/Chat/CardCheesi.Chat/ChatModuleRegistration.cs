using CardCheesi.Chat.Features.Chat;
using CardCheesi.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CardCheesi.Chat;

public static class ChatModuleRegistration
{
    public static IServiceCollection AddChatModule(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<SendChatMessageCommand>, SendChatMessageHandler>();
        return services;
    }
}
