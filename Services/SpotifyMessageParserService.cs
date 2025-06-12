using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Echelon.Bot.Models;
using Echelon.Bot.Constants;

namespace Echelon.Bot.Services;

public class SpotifyMessageParserService : BaseMessageParserService
{
    public SpotifyMessageParserService(
        ILogger<SpotifyMessageParserService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
        : base(logger, serviceProvider, configuration, "SpotifyMessageParserService")
    {
    }

    protected override N8NNotification CreateNotification(SocketMessage message)
    {
        var notification = base.CreateNotification(message);
        notification.Type = IsMessageSpotifyTrack(message.Content) 
            ? DiscordConstants.MessageTypes.SpotifyTrack 
            : DiscordConstants.MessageTypes.SpotifyRequest;

        if (IsMessageSpotifyTrack(message.Content))
        {
            notification.Content = GetSpotifyTrackID(message.Content);
        }
        else if (IsMessageSpotifyAlbum(message.Content))
        {
            notification.Content = GetSpotifyAlbumID(message.Content);
            notification.Type = DiscordConstants.MessageTypes.SpotifyAlbum;
        }
        
        return notification;
    }

    private bool IsMessageSpotifyTrack(string? message)
    {
        return message is not null && message.Contains(DiscordConstants.Urls.SpotifyTrackPrefix);
    }

    private bool IsMessageSpotifyAlbum(string? message)
    {
        return message is not null && message.Contains(DiscordConstants.Urls.SpotifyAlbumPrefix);
    }

    private string GetSpotifyTrackID(string? message)
    {
        if (message is null)
            return string.Empty;

        return $"spotify:track:{message.Split(DiscordConstants.Urls.SpotifyTrackPrefix).LastOrDefault() ?? string.Empty}";
    }

    private string GetSpotifyAlbumID(string? message)
    {
        if (message is null)
            return string.Empty;

        return $"spotify:album:{message.Split(DiscordConstants.Urls.SpotifyAlbumPrefix).LastOrDefault() ?? string.Empty}";
    }
}