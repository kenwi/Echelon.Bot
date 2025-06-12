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
        IServiceProvider serviceProvider,
        IConfiguration configuration)
        : base(logger, serviceProvider, configuration, "DefaultMessageParserService")
    {
    }
}