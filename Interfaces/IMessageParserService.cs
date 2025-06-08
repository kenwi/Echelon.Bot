using Discord.WebSocket;

namespace Echelon.Bot.Interfaces;

public interface IMessageParserService
{
    Task ParseMessageAsync(SocketMessage message);
} 