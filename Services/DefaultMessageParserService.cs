using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Echelon.Bot.Interfaces;
using Echelon.Bot.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Echelon.Bot.Services;

public class DefaultMessageParserService : IMessageParserService
{
    private readonly ILogger<DefaultMessageParserService> _logger;
    private readonly N8NService _n8nService;
    private readonly Dictionary<string, List<string>> _allowedServersAndChannels;
    private readonly IConfiguration _configuration;

    public DefaultMessageParserService(
        ILogger<DefaultMessageParserService> logger,
        [FromKeyedServices("Default")] N8NService n8nService,
        IConfiguration configuration)
    {
        _logger = logger;
        _n8nService = n8nService;
        _configuration = configuration;

        // Load allowed servers and channels from configuration
        _allowedServersAndChannels = configuration
            .GetSection("Discord:DefaultMessageParserService")
            .Get<Dictionary<string, string[]>>()
            ?.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToList()
            ) ?? new Dictionary<string, List<string>>();

        if (_allowedServersAndChannels.Count == 0)
        {
            _logger.LogWarning("No allowed servers and channels configured in appsettings.json");
        }
        else
        {
            _logger.LogInformation("Loaded {Count} server configurations", _allowedServersAndChannels.Count);
            foreach (var server in _allowedServersAndChannels)
            {
                _logger.LogInformation("Server {Server} allows channels: {Channels}",
                    server.Key, string.Join(", ", server.Value));
            }
        }
    }

    public async Task ParseMessageAsync(SocketMessage message)
    {
        if (message.Author.IsBot) return;

        _logger.LogInformation("Message received from {User}: {Content}", 
            message.Author.GlobalName, message.Content);

        if (!IsChannelAllowed(message)) return;

        // Forward the message to N8N
        var webhookMessage = new N8NNotification
        {
            ServerName = GetServerName(message),
            Id = message.Id.ToString(),
            Type = message.Type.ToString(),
            Channel = message.Channel.Name,
            Author = message.Author.Username,
            AuthorId = message.Author.Id.ToString(),
            GlobalName = message.Author.GlobalName,
            Content = message.Content,
            ChannelId = message.Channel.Id.ToString(),
            Attachments = string.Join(", ", message.Attachments.Select(a => a.Url)),
            Embeds = string.Join(", ", message.Embeds.Select(e => e.Type.ToString())),
            Mentions = string.Join(", ", message.MentionedUsers.Select(u => u.Username)),
            Timestamp = message.Timestamp.UtcDateTime
        };

        await _n8nService.SendNotificationAsync(webhookMessage);
    }

    private bool IsChannelAllowed(SocketMessage message)
    {
        var serverName = GetServerName(message);
        
        // Check if server is in allowed list
        if (!_allowedServersAndChannels.TryGetValue(serverName, out var allowedChannels))
        {
            _logger.LogInformation("Server {Server} is not in allowed list", serverName);
            return false;
        }

        // Check if channel is allowed for this server
        var isAllowed = allowedChannels.Contains(message.Channel.Name);
        if (!isAllowed)
        {
            _logger.LogInformation("Channel {Channel} is not allowed in server {Server}", 
                message.Channel.Name, serverName);
        }
        
        return isAllowed;
    }

    private string GetServerName(SocketMessage message)
    {
        return (message.Channel as SocketGuildChannel)?.Guild.Name ?? "Direct Message";
    }
} 