using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Echelon.Bot.Constants;
using Echelon.Bot.Services;
using Echelon.Bot.Models;
using Echelon.Bot.Factories;
using System.Text.Json;

namespace Echelon.Bot.Modules;

public class SpotifyModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<SpotifyModule> _logger;
    private readonly N8NService _n8nService;

    public SpotifyModule(
        ILogger<SpotifyModule> logger,
        [FromKeyedServices("Spotify")] N8NService n8nService)
    {
        _logger = logger;
        _n8nService = n8nService;
    }

    [SlashCommand("verbose", "Set verbose mode. True = verbose, False = silent")]
    public async Task VerboseAsync(
        [Summary("verbose", "Verbose mode")] bool state)
    {
        await DeferAsync();

        if (state)
        {
            await FollowupAsync("Verbose mode enabled");
        }
        else
        {
            await FollowupAsync("Verbose mode disabled");
        }

        // Send to N8N webhook using factory
        try
        {
            var content = new { verbose = state };
            var n8nNotification = N8NNotificationFactory.FromInteractionContext(Context, "VerboseCommand", 
                JsonSerializer.Serialize(content));
            
            await _n8nService.SendNotificationAsync(n8nNotification);
            _logger.LogInformation("Verbose command notification sent to N8N successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verbose command notification to N8N");
        }
    }
} 