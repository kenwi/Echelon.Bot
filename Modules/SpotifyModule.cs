using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Echelon.Bot.Constants;
using Echelon.Bot.Services;
using Echelon.Bot.Models;
using Echelon.Bot.Factories;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Echelon.Bot.Modules;

public class SpotifyModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<SpotifyModule> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, ServerConfiguration> _allowedServersAndChannels;

    public SpotifyModule(
        ILogger<SpotifyModule> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        
        // Load allowed servers and channels from configuration
        _allowedServersAndChannels = configuration
            .GetSection("Discord:SpotifyMessageParserService")
            .Get<Dictionary<string, ServerConfiguration>>()
            ?? new Dictionary<string, ServerConfiguration>();
    }

    private async Task<bool> EnsureGuildContextAsync()
    {
        if (Context.Guild == null)
        {
            await RespondAsync("This command can only be used in a server channel!", ephemeral: true);
            return false;
        }
        return true;
    }

    private N8NService GetN8NServiceForGuild()
    {
        var guildName = Context.Guild?.Name ?? "Unknown";
        
        if (_allowedServersAndChannels.TryGetValue(guildName, out var serverConfig) && 
            !string.IsNullOrEmpty(serverConfig.N8NUrl))
        {
            // Create N8NService with server-specific URL
            var httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
            var logger = _serviceProvider.GetRequiredService<ILogger<N8NService>>();
            
            return new N8NService(httpClientFactory.CreateClient(), logger, _configuration, serverConfig.N8NUrl);
        }
        
        // Fallback to default service if no specific URL configured
        throw new InvalidOperationException($"No N8N URL configured for server: {guildName}");
    }

    [SlashCommand("create-playlist", "Create a playlist")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task CreatePlaylistAsync(
        [Summary("name", "Name of the playlist to create")] string playlistName,
        [Summary("description", "Description for playlist")] string playlistDescription)
    {
        if (!await EnsureGuildContextAsync()) return;
        await DeferAsync();
        
        try
        {
            var n8nNotification = N8NNotificationFactory.FromInteractionContext(Context, "CreatePlaylistCommand", $"{playlistName}::{playlistDescription}");
            var response = await GetN8NServiceForGuild().SendNotificationAsync(n8nNotification);
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Create playlist command notification sent to N8N successfully");
            await FollowupAsync($"{responseContent}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verbose command notification to N8N");
            await FollowupAsync("Failed to create playlist. Please try again.");
        }
    }

    [SlashCommand("get-playlist", "Gets the playlist for the current channel")]
    public async Task GetPlaylistAsync()
    {
        if (!await EnsureGuildContextAsync()) return;
        await DeferAsync();

        try
        {
            var n8nNotification = N8NNotificationFactory.FromInteractionContext(Context, "GetPlaylistCommand", string.Empty);
            var response = await GetN8NServiceForGuild().SendNotificationAsync(n8nNotification);
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Get playlist command notification sent to N8N successfully");
            await FollowupAsync($"{responseContent}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send get playlist command notification to N8N");
        }
    }

    [SlashCommand("bind-playlist", "Binds a playlist to the current channel")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task BindPlaylistAsync(
        [Summary("playlist-url", "The URL of the playlist to bind")] string playlistUrl)
    {
        if (!await EnsureGuildContextAsync()) return;
        await DeferAsync();

        try
        {
            var n8nNotification = N8NNotificationFactory.FromInteractionContext(Context, "BindPlaylistCommand", playlistUrl);
            var response = await GetN8NServiceForGuild().SendNotificationAsync(n8nNotification);
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Bind playlist command notification sent to N8N successfully");
            await FollowupAsync($"{responseContent}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send bind playlist command notification to N8N");
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
            var response = await GetN8NServiceForGuild().SendNotificationAsync(n8nNotification);
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Verbose command notification sent to N8N successfully");
            await FollowupAsync($"{responseContent}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verbose command notification to N8N");
        }
    }

    [SlashCommand("verbose", "Set verbose mode. True = verbose, False = silent")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task VerboseAsync(
        [Summary("verbose", "Verbose mode")] bool state)
    {
        if (!await EnsureGuildContextAsync()) return;

        await DeferAsync();
        var stateString = state ? "Enabled" : "Disabled";

        try
        {
            var content = new { verbose = state };
            var n8nNotification = N8NNotificationFactory.FromInteractionContext(Context, "VerboseCommand",
                JsonSerializer.Serialize(content));

            await GetN8NServiceForGuild().SendNotificationAsync(n8nNotification);
            _logger.LogInformation("Verbose command notification sent to N8N successfully");
            await FollowupAsync($"Verbose mode {stateString}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verbose command notification to N8N");
        }
    }
} 