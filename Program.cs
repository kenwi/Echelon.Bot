using Echelon.Bot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Echelon.Bot.Interfaces;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureAppConfiguration((hostContext, config) =>
{
    config.SetBasePath(Directory.GetCurrentDirectory())
          .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
          .AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
          .AddEnvironmentVariables();
});

builder.ConfigureServices((hostContext, services) =>
{
    // Register message parsers with their specific keys
    services.AddKeyedSingleton<IMessageParserService, SimpleMessageParserService>(ParserType.Default);
    services.AddKeyedSingleton<IMessageParserService, SpotifyMessageParserService>(ParserType.Spotify);
    
    services.AddSingleton<LoggingService>();
    services.AddSingleton<DiscordService>();
    services.AddHostedService<Worker>();
    
    // Add HTTP client and N8N service
    services.AddHttpClient<N8NService>();
    services.AddSingleton<N8NService>();
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
