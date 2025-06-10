using Echelon.Bot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Echelon.Bot.Interfaces;
using Echelon.Bot.Models;
using Microsoft.Extensions.Logging;

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
    services.AddKeyedSingleton<IMessageParserService, DefaultMessageParserService>(ParserType.Default);
    services.AddKeyedSingleton<IMessageParserService, SpotifyMessageParserService>(ParserType.Spotify);
    
    services.AddSingleton<LoggingService>();
    services.AddSingleton<DiscordService>();
    services.AddHostedService<Worker>();
    
    // Add HTTP client and N8N service
    services.AddHttpClient<N8NService>();

    // Register N8NService for DefaultMessageParser
    services.AddKeyedSingleton<N8NService>("Default", (sp, key) => {
        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
        var logger = sp.GetRequiredService<ILogger<N8NService>>();
        var configuration = sp.GetRequiredService<IConfiguration>();
        var endpointOverride = configuration["Discord:DefaultMessageParserService:N8NUrl"];
        return new N8NService(httpClientFactory.CreateClient(), logger, configuration, endpointOverride);
    });

    // Register N8NService for Spotify
    services.AddKeyedSingleton<N8NService>("Spotify", (sp, key) => {
        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
        var logger = sp.GetRequiredService<ILogger<N8NService>>();
        var configuration = sp.GetRequiredService<IConfiguration>();
        var endpointOverride = configuration["Discord:SpotifyMessageParserService:N8NUrl"];
        return new N8NService(httpClientFactory.CreateClient(), logger, configuration, endpointOverride);
    });
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
