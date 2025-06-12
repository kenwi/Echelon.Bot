using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Echelon.Bot.Interfaces;
using Echelon.Bot.Models;
using Echelon.Bot.Factories;


namespace Echelon.Bot.Services;

public abstract class BaseMessageParserService : IMessageParserService
{
    protected readonly ILogger _logger;
    protected readonly N8NService _n8nService;
    protected readonly Dictionary<string, ServerConfiguration> _allowedServersAndChannels;

    protected BaseMessageParserService(
        ILogger logger,
        N8NService n8nService,
        IConfiguration configuration,
        string configSection)
    {
        _logger = logger;
        _n8nService = n8nService;

        // Load allowed servers and channels from configuration
        _allowedServersAndChannels = configuration
            .GetSection($"Discord:{configSection}")
            .Get<Dictionary<string, ServerConfiguration>>()
            ?? new Dictionary<string, ServerConfiguration>();

        LogConfiguration();
    }

    private void LogConfiguration()
    {
        if (_allowedServersAndChannels.Count == 0)
        {
            _logger.LogWarning("No allowed servers and channels configured in appsettings.json");
            return;
        }

        _logger.LogInformation("Loaded {Count} server configurations", _allowedServersAndChannels.Count);
        foreach (var server in _allowedServersAndChannels)
        {
            if (server.Value.AllowAllChannels)
            {
                _logger.LogInformation("Server {Server} allows ALL channels", server.Key);
            }
            else
            {
                var channelNames = server.Value.Channels.Select(c => c.ChannelName);
                _logger.LogInformation("Server {Server} allows channels: {Channels}", 
                    server.Key, string.Join(", ", channelNames));
            }
        }
    }

    protected bool IsChannelAllowed(SocketMessage message)
    {
        var serverName = GetServerName(message);
        
        if (!_allowedServersAndChannels.TryGetValue(serverName, out var serverConfig))
        {
            _logger.LogInformation("Server {Server} is not in allowed list", serverName);
            return false;
        }

        // If AllowAllChannels is true, allow any channel
        if (serverConfig.AllowAllChannels)
        {
            _logger.LogDebug("Channel {Channel} allowed in server {Server} (AllowAllChannels=true)", 
                message.Channel.Name, serverName);
            return true;
        }

        // Check if the specific channel is in the allowed list (by name or ID)
        var channelId = message.Channel.Id.ToString();
        var channelName = message.Channel.Name;
        
        var isAllowed = serverConfig.Channels.Any(c => 
            c.ChannelId == channelId || c.ChannelName == channelName);
            
        if (!isAllowed)
        {
            _logger.LogInformation("Channel {Channel} (ID: {ChannelId}) is not allowed in server {Server}", 
                channelName, channelId, serverName);
        }
        
        return isAllowed;
    }

    protected string GetServerName(SocketMessage message)
    {
        return (message.Channel as SocketGuildChannel)?.Guild.Name ?? "Direct Message";
    }

    protected virtual N8NNotification CreateNotification(SocketMessage message)
    {
        return N8NNotificationFactory.FromMessage(message, message.Type.ToString());
    }

    public virtual async Task ParseMessageAsync(SocketMessage message)
    {
        if (message.Author.IsBot) return;
        if (!IsChannelAllowed(message)) return;

        _logger.LogInformation("Message received from {User}: {Content}", 
            message.Author.GlobalName, message.Content);

        var notification = CreateNotification(message);
        await _n8nService.SendNotificationAsync(notification);
    }
}
