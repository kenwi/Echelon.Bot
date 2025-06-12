using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Echelon.Bot.Interfaces;
using Echelon.Bot.Models;
using Echelon.Bot.Factories;
using Microsoft.Extensions.DependencyInjection;

namespace Echelon.Bot.Services;

public abstract class BaseMessageParserService : IMessageParserService
{
    protected readonly ILogger _logger;
    protected readonly IServiceProvider _serviceProvider;
    protected readonly Dictionary<string, ServerConfiguration> _allowedServersAndChannels;

    protected BaseMessageParserService(
        ILogger logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        string configSection)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

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
            
        // if (!isAllowed)
        // {
        //     _logger.LogInformation("Channel {Channel} (ID: {ChannelId}) is not allowed in server {Server}", 
        //         channelName, channelId, serverName);
        // }
        
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

    protected N8NService GetN8NServiceForServer(string serverName)
    {
        if (_allowedServersAndChannels.TryGetValue(serverName, out var serverConfig) && 
            !string.IsNullOrEmpty(serverConfig.N8NUrl))
        {
            // Create N8NService with server-specific URL
            var httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
            var logger = _serviceProvider.GetRequiredService<ILogger<N8NService>>();
            var configuration = _serviceProvider.GetRequiredService<IConfiguration>();
            
            return new N8NService(httpClientFactory.CreateClient(), logger, configuration, serverConfig.N8NUrl);
        }
        
        // Fallback to default service if no specific URL configured
        throw new InvalidOperationException($"No N8N URL configured for server: {serverName}");
    }

    public virtual async Task ParseMessageAsync(SocketMessage message)
    {
        if (message.Author.IsBot) return;
        if (!IsChannelAllowed(message)) return;

        _logger.LogInformation("Message received from {User}: {Content}", 
            message.Author.GlobalName, message.Content);

        var serverName = GetServerName(message);
        var n8nService = GetN8NServiceForServer(serverName);
        var notification = CreateNotification(message);
        var response = await n8nService.SendNotificationAsync(notification);
    }
}
