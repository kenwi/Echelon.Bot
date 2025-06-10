using Discord.WebSocket;
using Echelon.Bot.Interfaces;
using Microsoft.Extensions.Logging;
using Echelon.Bot.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Echelon.Bot.Services;

public class SpotifyMessageParserService : IMessageParserService
{
    private readonly ILogger<SpotifyMessageParserService> _logger;
    private readonly N8NService _n8nService;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, List<string>> _allowedServersAndChannels;

    public SpotifyMessageParserService(
        ILogger<SpotifyMessageParserService> logger,
        IConfiguration configuration,
        [FromKeyedServices("Spotify")] N8NService n8nService)
    {
        _logger = logger;
        _n8nService = n8nService;
        _configuration = configuration;

        // Load allowed servers and channels from configuration
        _allowedServersAndChannels = configuration
            .GetSection("Discord:SpotifyMessageParserService")
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
            Type = IsMessageSpotifyTrack(message.Content) ? "SpotifyTrack" : "SpotifyRequest",
            Channel = message.Channel.Name,
            Author = message.Author.Username,
            AuthorId = message.Author.Id.ToString(),
            GlobalName = message.Author.GlobalName,
            Content = IsMessageSpotifyTrack(message.Content) ? GetSpotifyTrack(message.Content) : message.Content,
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

    private bool IsMessageSpotifyTrack(string? message)
    {
        return message is not null && message.Contains("https://open.spotify.com/track/");
    }

    private string GetSpotifyTrack(string? message)
    {
        if (message is null)
            return string.Empty;

        return $"spotify:track:{message.Split("https://open.spotify.com/track/").LastOrDefault() ?? string.Empty}";
    }

    private string GetSpotifyPlaylistId(string? message)
    {
        if (message is null)
            return string.Empty;

        return $"spotify:playlist:{message.Split("https://open.spotify.com/playlist/").LastOrDefault() ?? string.Empty}";
    }
}