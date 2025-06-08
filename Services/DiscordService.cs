using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Echelon.Bot.Interfaces;
using Echelon.Bot.Models;

namespace Echelon.Bot.Services;

public class DiscordService
{
    private readonly DiscordSocketClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DiscordService> _logger;
    private readonly IMessageParserService _defaultParser;
    private readonly IMessageParserService _spotifyParser;
    private readonly LoggingService _loggingService;

    public DiscordService(
        IConfiguration configuration,
        ILogger<DiscordService> logger,
        [FromKeyedServices(ParserType.Default)] IMessageParserService defaultParser,
        [FromKeyedServices(ParserType.Spotify)] IMessageParserService spotifyParser,
        LoggingService loggingService)
    {
        _configuration = configuration;
        _logger = logger;
        _defaultParser = defaultParser;
        _spotifyParser = spotifyParser;
        _loggingService = loggingService;

        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.MessageContent | 
                            GatewayIntents.Guilds |
                            GatewayIntents.GuildMessages
        };

        _client = new DiscordSocketClient(config);
        
        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.MessageReceived += MessageReceivedAsync;
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

    private Task ReadyAsync()
    {
        _logger.LogInformation($"{_client.CurrentUser} is connected!");
        return Task.CompletedTask;
    }

    private async Task MessageReceivedAsync(SocketMessage message)
    {
        await _loggingService.LogMessageAsync(message);
        await _defaultParser.ParseMessageAsync(message);
        await _spotifyParser.ParseMessageAsync(message);
    }

    public async Task StopAsync()
    {
        await _client.StopAsync();
    }
} 