using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Echelon.Bot.Services;

public class N8NService : BaseHttpService
{
    private readonly string _n8nUrl;
    private readonly CultureInfo _culture;
    private readonly string _dateTimeFormat;
    private readonly bool _useLocalTime;

    public N8NService(
        HttpClient httpClient,
        ILogger<N8NService> logger,
        IConfiguration configuration) 
        : base(httpClient, logger)
    {
        _n8nUrl = configuration["N8N:Url"] 
            ?? throw new InvalidOperationException("N8N webhook URL not configured");

        // Get culture settings from configuration
        var cultureName = configuration["Logging:Settings:Culture"] ?? "nb-NO";
        _culture = new CultureInfo(cultureName);
        
        // Get datetime format from configuration
        _dateTimeFormat = configuration["Logging:Settings:DateTimeFormat"] ?? "dd.MM.yyyy HH:mm:ss";
        
        // Get time zone preference from configuration
        _useLocalTime = configuration.GetValue<bool>("Logging:Settings:UseLocalTime", true);
    }

    public override string Endpoint => _n8nUrl;

    public async Task SendNotificationAsync(N8NNotification notification)
    {
        // Format the timestamp according to configuration
        var timestamp = _useLocalTime 
            ? notification.Timestamp.ToLocalTime() 
            : notification.Timestamp;
            
        notification.FormattedTimestamp = timestamp.ToString(_dateTimeFormat, _culture);
        await PostJsonAsync(Endpoint, notification);
    }
}

public class N8NNotification
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public string GlobalName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Attachments { get; set; } = string.Empty;
    public string Embeds { get; set; } = string.Empty;
    public string Mentions { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string FormattedTimestamp { get; set; } = string.Empty;
} 