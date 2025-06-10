using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Echelon.Bot.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Echelon.Bot.Services;

public class DefaultMessageParserService : BaseMessageParserService
{
    public DefaultMessageParserService(
        ILogger<DefaultMessageParserService> logger,
        [FromKeyedServices("Default")] N8NService n8nService,
        IConfiguration configuration)
        : base(logger, n8nService, configuration, "DefaultMessageParserService")
    {
    }
}