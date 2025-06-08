using Discord.WebSocket;
using Echelon.Bot.Interfaces;
using Microsoft.Extensions.Logging;

namespace Echelon.Bot.Services;

public class SpotifyMessageParserService : IMessageParserService
{
    private readonly ILogger<SpotifyMessageParserService> _logger;
    private readonly N8NService _n8nService;

    public SpotifyMessageParserService(
        ILogger<SpotifyMessageParserService> logger,
        N8NService n8nService)
    {
        _logger = logger;
        _n8nService = n8nService;
    }

    public async Task ParseMessageAsync(SocketMessage message)
    {
        if (message.Author.IsBot) return;
        
        if (!IsMessageSpotifyTrack(message.Content)) return;

        var spotifyTrack = GetSpotifyTrack(message.Content);
        _logger.LogInformation("SpotifyService: ParseMessageAsync: {SpotifyTrack}", spotifyTrack);

        var webhookMessage = new N8NNotification
        {
            Id = message.Id.ToString(),
            Type = "SpotifyTrack",
            Channel = message.Channel.Name,
            Author = message.Author.Username,
            AuthorId = message.Author.Id.ToString(),
            GlobalName = message.Author.GlobalName,
            Content = GetSpotifyTrack(message.Content),
            Attachments = string.Join(", ", message.Attachments.Select(a => a.Url)),
            Embeds = string.Join(", ", message.Embeds.Select(e => e.Type.ToString())),
            Mentions = string.Join(", ", message.MentionedUsers.Select(u => u.Username)),
            Timestamp = message.Timestamp.UtcDateTime
        };

        await _n8nService.SendNotificationAsync(webhookMessage);
    }

    public bool IsMessageSpotifyTrack(string? message)
    {
        return message is not null && message.Contains("https://open.spotify.com/track/");
    }

    public string GetSpotifyTrack(string? message)
    {
        if (message is null)
            return string.Empty;

        return message.Split(" ").FirstOrDefault(word => word.Contains("https://open.spotify.com/track/")) ?? string.Empty;
    }
}