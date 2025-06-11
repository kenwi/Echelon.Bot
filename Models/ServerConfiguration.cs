namespace Echelon.Bot.Models;

public class ServerConfiguration
{
    public string[] Channels { get; set; } = Array.Empty<string>();
    public bool AllowAllChannels { get; set; } = false;
} 