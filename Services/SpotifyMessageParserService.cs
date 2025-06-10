using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Echelon.Bot.Models;

namespace Echelon.Bot.Services;

public class SpotifyMessageParserService : BaseMessageParserService
{
    public SpotifyMessageParserService(
        ILogger<SpotifyMessageParserService> logger,
        [FromKeyedServices("Spotify")] N8NService n8nService,
        IConfiguration configuration)
        : base(logger, n8nService, configuration, "SpotifyMessageParserService")
    {
    }

    protected override N8NNotification CreateNotification(SocketMessage message)
    {
        var notification = base.CreateNotification(message);
        notification.Type = IsMessageSpotifyTrack(message.Content) ? "SpotifyTrack" : "SpotifyRequest";
        if (IsMessageSpotifyTrack(message.Content))
        {
            notification.Content = GetSpotifyTrackID(message.Content);
        }
        return notification;
    }

    private bool IsMessageSpotifyTrack(string? message)
    {
        return message is not null && message.Contains("https://open.spotify.com/track/");
    }

    private string GetSpotifyTrackID(string? message)
    {
        if (message is null)
            return string.Empty;

        return $"spotify:track:{message.Split("https://open.spotify.com/track/").LastOrDefault() ?? string.Empty}";
    }
}