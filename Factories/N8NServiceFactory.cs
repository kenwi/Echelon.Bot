using Echelon.Bot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Echelon.Bot.Factories;

public class N8NServiceFactory
{
    public static N8NService Create(IServiceProvider sp, string configPath)
    {
        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
        var logger = sp.GetRequiredService<ILogger<N8NService>>();
        var configuration = sp.GetRequiredService<IConfiguration>();
        var endpointOverride = configuration[$"Discord:{configPath}:N8NUrl"];
        return new N8NService(httpClientFactory.CreateClient(), logger, configuration, endpointOverride);
    }
}