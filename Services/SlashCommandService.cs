using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Echelon.Bot.Services;

public class SlashCommandService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IServiceProvider _services;
    private readonly ILogger<SlashCommandService> _logger;
    private readonly IConfiguration _configuration;

    public SlashCommandService(
        DiscordSocketClient client,
        InteractionService interactions,
        IServiceProvider services,
        ILogger<SlashCommandService> logger,
        IConfiguration configuration)
    {
        _client = client;
        _interactions = interactions;
        _services = services;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InitializeAsync()
    {
        // Add modules to the interaction service
        await _interactions.AddModulesAsync(Assembly.GetExecutingAssembly(), _services);

        // Subscribe to interaction events
        _client.InteractionCreated += HandleInteraction;
        _interactions.SlashCommandExecuted += SlashCommandExecuted;
    }

    private async Task HandleInteraction(SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(_client, interaction);
            await _interactions.ExecuteCommandAsync(context, _services);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling interaction");
            
            if (interaction.Type == InteractionType.ApplicationCommand)
            {
                await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) =>
                {
                    await interaction.FollowupAsync("An error occurred while processing your command.", ephemeral: true);
                });
            }
        }
    }

    private Task SlashCommandExecuted(SlashCommandInfo info, IInteractionContext context, IResult result)
    {
        if (!result.IsSuccess)
        {
            _logger.LogError("Slash command failed: {Error}", result.ErrorReason);
        }
        else
        {
            _logger.LogInformation("Slash command executed: {Command}", info.Name);
        }

        return Task.CompletedTask;
    }

    public async Task RegisterCommandsAsync()
    {
        try
        {
            // Check if we have test guild IDs configured for faster registration
            var guildIds = _configuration.GetSection("Discord:GuildIds").Get<string[]>();
            
            if (guildIds != null && guildIds.Length > 0)
            {
                foreach (var guildIdString in guildIds)
                {
                    if (!string.IsNullOrEmpty(guildIdString) && ulong.TryParse(guildIdString, out var guildId))
                    {
                        await _interactions.RegisterCommandsToGuildAsync(guildId, deleteMissing: true);
                        _logger.LogInformation("Slash commands cleared and re-registered to guild {GuildId}", guildId);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid guild ID: {GuildId}", guildIdString);
                    }
                }
            }
            else
            {
                // For global commands, we can also force a clean registration
                await _interactions.RegisterCommandsGloballyAsync(deleteMissing: true);
                _logger.LogInformation("Slash commands cleared and re-registered globally (may take up to 1 hour)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register slash commands");
        }
    }
} 