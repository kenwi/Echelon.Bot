using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Echelon.Bot.Interfaces;

namespace Echelon.Bot.Services;

public class SimpleMessageParserService : IMessageParserService
{
    private readonly ILogger<SimpleMessageParserService> _logger;
    private readonly N8NService _n8nService;

    public SimpleMessageParserService(
        ILogger<SimpleMessageParserService> logger,
        N8NService n8nService)
    {
        _logger = logger;
        _n8nService = n8nService;
    }

    public async Task ParseMessageAsync(SocketMessage message)
    {
        if (message.Author.IsBot) return;

        _logger.LogInformation("Message received from {User}: {Content}", 
            message.Author.Username, message.Content);

        // Forward the message to N8N
        var webhookMessage = new N8NNotification
        {
            Id = message.Id.ToString(),
            Type = message.Type.ToString(),
            Channel = message.Channel.Name,
            Author = message.Author.Username,
            AuthorId = message.Author.Id.ToString(),
            GlobalName = message.Author.GlobalName,
            Content = message.Content,
            Attachments = string.Join(", ", message.Attachments.Select(a => a.Url)),
            Embeds = string.Join(", ", message.Embeds.Select(e => e.Type.ToString())),
            Mentions = string.Join(", ", message.MentionedUsers.Select(u => u.Username)),
            Timestamp = message.Timestamp.UtcDateTime
        };

        await _n8nService.SendNotificationAsync(webhookMessage);
    }
} 