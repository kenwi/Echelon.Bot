namespace Echelon.Bot.Models;

public class ServerConfiguration
{
    public bool AllowAllChannels { get; set; } = false;
    public ServerOwnerConfiguration[] Owners { get; set; } = Array.Empty<ServerOwnerConfiguration>();
    public ServerChannelConfiguration[] Channels { get; set; } = Array.Empty<ServerChannelConfiguration>();
    public string? N8NUrl { get; set; }
} 
