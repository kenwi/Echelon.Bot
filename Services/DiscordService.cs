using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Echelon.Bot.Interfaces;
using Echelon.Bot.Models;
using System.Text.Json;

namespace Echelon.Bot.Services;

public class DiscordService
{
    private readonly DiscordSocketClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DiscordService> _logger;
    private readonly IMessageParserService _defaultParser;
    private readonly IMessageParserService _spotifyParser;
    private readonly LoggingService _loggingService;
    private readonly SlashCommandService _slashCommandService;

    public DiscordService(
        IConfiguration configuration,
        ILogger<DiscordService> logger,
        [FromKeyedServices(ParserType.Default)] IMessageParserService defaultParser,
        [FromKeyedServices(ParserType.Spotify)] IMessageParserService spotifyParser,
        LoggingService loggingService,
        DiscordSocketClient client,
        SlashCommandService slashCommandService)
    {
        _configuration = configuration;
        _logger = logger;
        _defaultParser = defaultParser;
        _spotifyParser = spotifyParser;
        _loggingService = loggingService;
        _client = client;
        _slashCommandService = slashCommandService;

        // Subscribe to events
        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.MessageReceived += MessageReceivedAsync;
        _client.ReactionAdded += ReactionAddedAsync;
    }

    public async Task StartAsync()
    {
        var token = _configuration["Discord:Token"];
        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException("Discord Token not found in configuration.");
        }

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
    }

    private Task LogAsync(LogMessage log)
    {
        _logger.LogInformation(log.ToString());
        return Task.CompletedTask;
    }

    private async Task ReadyAsync()
    {
        _logger.LogInformation($"{_client.CurrentUser} is connected!");
        
        // Initialize slash commands when bot is ready
        await _slashCommandService.InitializeAsync();
        await _slashCommandService.RegisterCommandsAsync();
    }

    private async Task MessageReceivedAsync(SocketMessage message)
    {
        await _loggingService.LogMessageAsync(message);
        await _defaultParser.ParseMessageAsync(message);
        await _spotifyParser.ParseMessageAsync(message);
    }
    
    private async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
    {
        // Fetch the user and message from the cache or from the API if not in cache
        try
        {
            var user = _client.GetUser(reaction.UserId) ?? await _client.GetUserAsync(reaction.UserId);
            var reactedMessage = message.Value ?? await message.DownloadAsync();
            var messageChannel = channel.Value ?? await channel.DownloadAsync();

            _logger.LogInformation("Reaction {Emote} added in channel {ChannelName} ({ChannelID}) by {UserGlobalName} ({UserId}) to message \"{MessageContent}\" ({MessageID})",
            reaction.Emote, messageChannel.Name, channel.Id, user?.GlobalName ?? "Unknown User", user?.Id ?? reaction.UserId, reactedMessage.Content, reactedMessage.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return;
        }
    }

    public async Task StopAsync()
    {
        await _client.StopAsync();
    }
} 