using Echelon.Bot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Echelon.Bot.Interfaces;
using Echelon.Bot.Models;
using Microsoft.Extensions.Logging;
using Discord.Interactions;
using Discord.WebSocket;
using Discord;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureAppConfiguration((hostContext, config) =>
{
    config.SetBasePath(Directory.GetCurrentDirectory())
          .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
          .AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: false)
          .AddEnvironmentVariables();
});

builder.ConfigureServices((hostContext, services) =>
{
    // Register message parsers with their specific keys
    services.AddKeyedSingleton<IMessageParserService, DefaultMessageParserService>(ParserType.Default);
    services.AddKeyedSingleton<IMessageParserService, SpotifyMessageParserService>(ParserType.Spotify);
    
    services.AddSingleton<LoggingService>();
    services.AddSingleton<DiscordService>();
    services.AddHostedService<Worker>();
    
    // Add Discord Socket Client with proper configuration
    services.AddSingleton(provider =>
    {
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.MessageContent |
                            GatewayIntents.Guilds |
                            GatewayIntents.GuildMessages |
                            GatewayIntents.GuildMessageReactions |
                            GatewayIntents.GuildIntegrations  // Required for slash commands
        };
        return new DiscordSocketClient(config);
    });
    
    // Add Interaction Service for slash commands
    services.AddSingleton(provider =>
    {
        var client = provider.GetRequiredService<DiscordSocketClient>();
        return new Discord.Interactions.InteractionService(client);
    });
    services.AddSingleton<SlashCommandService>();
    
    // Add HTTP client
    services.AddHttpClient<N8NService>();
});

var host = builder.Build();
await host.RunAsync();

public class Worker : IHostedService
{
    private readonly DiscordService _discordService;

    public Worker(DiscordService discordService)
    {
        _discordService = discordService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _discordService.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _discordService.StopAsync();
    }
}
