using Discord.WebSocket;
using Echelon.Bot.Interfaces;
using Microsoft.Extensions.Logging;
using Echelon.Bot.Models;
using Microsoft.Extensions.Configuration;

namespace Echelon.Bot.Services;

public class SpotifyMessageParserService : IMessageParserService
{
    private readonly ILogger<SpotifyMessageParserService> _logger;
    private readonly IConfiguration _configuration;
    private readonly N8NService _n8nService;
    private readonly string _botName;
    private readonly List<string> _allowedChannels = new List<string> { "spotify" };
    public SpotifyMessageParserService(
        ILogger<SpotifyMessageParserService> logger,
        IConfiguration configuration,
        N8NService n8nService)
    {
        _logger = logger;
        _configuration = configuration;
        _n8nService = n8nService;
        _botName = _configuration["Discord:BotName"] ?? "Echelon";
    }

    public async Task ParseMessageAsync(SocketMessage message)
    {
        if (message.Author.IsBot) return;

        if (!IsAllowedChannel(message)) return;

        try
        {
            if (IsUserTalkingToBot(message))
            {
                await HandleBotMention(message);
            }
            else if (IsMessageSpotifyTrack(message.Content))
            {
                await HandleSpotifyTrack(message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message: {MessageId}", message.Id);
        }
    }

    private async Task HandleBotMention(SocketMessage message)
    {
        _logger.LogInformation("User {User} is talking to bot: {Message}", 
            message.Author.Username, message.Content);
            
        var notification = CreateNotification(message, "BotMention");
        await _n8nService.SendNotificationAsync(notification);
    }

    private async Task HandleSpotifyTrack(SocketMessage message)
    {
        var spotifyTrack = GetSpotifyTrack(message.Content);
        _logger.LogInformation("Spotify track detected from {User}: {Track}", 
            message.Author.Username, spotifyTrack);

        var notification = CreateNotification(message, "SpotifyTrack", spotifyTrack);
        await _n8nService.SendNotificationAsync(notification);
    }

    private static N8NNotification CreateNotification(SocketMessage message, string type, string? content = null)
    {
        return new N8NNotification
        {
            Id = message.Id.ToString(),
            Type = type,
            Channel = message.Channel.Name,
            ChannelId = message.Channel.Id.ToString(),
            Author = message.Author.Username,
            AuthorId = message.Author.Id.ToString(),
            GlobalName = message.Author.GlobalName,
            Content = content ?? message.Content,
            Attachments = string.Join(", ", message.Attachments.Select(a => a.Url)),
            Embeds = string.Join(", ", message.Embeds.Select(e => e.Type.ToString())),
            Mentions = string.Join(", ", message.MentionedUsers.Select(u => u.Username)),
            Timestamp = message.Timestamp.UtcDateTime
        };
    }

    private bool IsMessageSpotifyTrack(string? message)
    {
        return message is not null && message.Contains("https://open.spotify.com/track/");
    }

    private string GetSpotifyTrack(string? message)
    {
        if (message is null)
            return string.Empty;

        return message.Split(" ")
            .FirstOrDefault(word => word.Contains("https://open.spotify.com/track/")) 
            ?? string.Empty;
    }

    private bool IsUserTalkingToBot(SocketMessage message)
    {
        return message.MentionedUsers.Select(u => u.Username).Contains(_botName);
    }

    private bool IsAllowedChannel(SocketMessage message)
    {
        return _allowedChannels.Contains(message.Channel.Name);
    }
}