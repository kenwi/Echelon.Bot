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

    private async Task<bool> EnsureGuildContextAsync()
    {
        if (Context.Guild == null)
        {
            await RespondAsync("This command can only be used in a server!", ephemeral: true);
            return false;
        }
        return true;
    }
    
    [SlashCommand("create-playlist", "Create a playlist")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task CreatePlaylistAsync(
        [Summary("playlist-name", "Name of the playlist to create")] string playlistName)
    {
        if (!await EnsureGuildContextAsync()) return;
        await DeferAsync();

        try
        {

            var n8nNotification = N8NNotificationFactory.FromInteractionContext(Context, "CreatePlaylistCommand", playlistName);
            await _n8nService.SendNotificationAsync(n8nNotification);
            _logger.LogInformation("Create playlist command notification sent to N8N successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verbose command notification to N8N");
        }
    }

    [SlashCommand("find-duplicates", "Find duplicates in a channel playlist")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task FindDuplicatesAsync()
    {
        if (!await EnsureGuildContextAsync()) return;
        await DeferAsync();

        try
        {
            var n8nNotification = N8NNotificationFactory.FromInteractionContext(Context, "FindDuplicatesCommand", string.Empty);
            await _n8nService.SendNotificationAsync(n8nNotification);
            _logger.LogInformation("Verbose command notification sent to N8N successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verbose command notification to N8N");
        }
        await FollowupAsync("Searching for duplicates");
    }

    [SlashCommand("verbose", "Set verbose mode. True = verbose, False = silent")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task VerboseAsync(
        [Summary("verbose", "Verbose mode")] bool state)
    {
        if (!await EnsureGuildContextAsync()) return;

        await DeferAsync();
        var stateString = state ? "Enabled" : "Disabled";
        await FollowupAsync($"Verbose mode {stateString}");

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