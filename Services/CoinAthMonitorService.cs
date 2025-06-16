using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Echelon.Bot.Models;
using Echelon.Bot.Interfaces;

namespace Echelon.Bot.Services;

public class CoinAthMonitorService
{
    private readonly CoinGeckoService _coinGeckoService;
    private readonly N8NService _n8nService;
    private readonly ILogger<CoinAthMonitorService> _logger;
    private readonly string _dataFilePath;
    private readonly Dictionary<string, CoinAthData> _athData;
    private readonly string[] _trackedCoins;
    private readonly TimeSpan _notificationCooldown;

    public CoinAthMonitorService(
        CoinGeckoService coinGeckoService,
        IHttpClientFactory httpClientFactory,
        ILogger<CoinAthMonitorService> logger,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _coinGeckoService = coinGeckoService;
        _logger = logger;
        
        // Get configuration values
        var logDir = configuration["Logging:Settings:Directory"] ?? "Logs";
        _dataFilePath = Path.Combine(logDir, "coin_ath_data.json");
        
        // Get notification cooldown from configuration (default: 1 hour)
        var cooldownMinutes = configuration.GetValue<int>("CoinGecko:NotificationCooldownMinutes", 60);
        _notificationCooldown = TimeSpan.FromMinutes(cooldownMinutes);
        
        // Get tracked coins from configuration (default: bitcoin, ethereum, solana)
        _trackedCoins = configuration.GetSection("CoinGecko:TrackedCoins")
            .Get<string[]>() ?? new[] { "bitcoin", "ethereum", "solana" };

        // Get CoinGecko-specific N8N URL
        var n8nUrl = configuration["CoinGecko:N8NUrl"];
        if (string.IsNullOrEmpty(n8nUrl))
        {
            throw new InvalidOperationException("CoinGecko:N8NUrl is not configured in appsettings.json");
        }

        // Create N8NService with CoinGecko-specific URL
        var n8nLogger = serviceProvider.GetRequiredService<ILogger<N8NService>>();
        _n8nService = new N8NService(
            httpClientFactory.CreateClient(),
            n8nLogger,
            configuration,
            n8nUrl);

        _logger.LogInformation(
            "CoinAthMonitorService initialized with {CooldownMinutes} minute cooldown, {CoinCount} tracked coins, and N8N URL: {N8NUrl}",
            cooldownMinutes,
            _trackedCoins.Length,
            n8nUrl);
        
        _athData = LoadAthData();
    }

    private Dictionary<string, CoinAthData> LoadAthData()
    {
        try
        {
            if (File.Exists(_dataFilePath))
            {
                var json = File.ReadAllText(_dataFilePath);
                return JsonSerializer.Deserialize<Dictionary<string, CoinAthData>>(json) 
                    ?? new Dictionary<string, CoinAthData>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading ATH data from {FilePath}", _dataFilePath);
        }
        
        return new Dictionary<string, CoinAthData>();
    }

    private void SaveAthData()
    {
        try
        {
            var json = JsonSerializer.Serialize(_athData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_dataFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving ATH data to {FilePath}", _dataFilePath);
        }
    }

    public async Task CheckAthChangesAsync()
    {
        foreach (var coinId in _trackedCoins)
        {
            try
            {
                var athResponse = await _coinGeckoService.GetAllTimeHighAsync(coinId);
                if (athResponse == null) continue;

                var currentAth = new CoinAthData
                {
                    CoinId = coinId,
                    AllTimeHigh = athResponse.AllTimeHigh,
                    AllTimeHighDate = athResponse.AllTimeHighDate,
                    LastChecked = DateTime.UtcNow,
                    LastNotificationTime = _athData.TryGetValue(coinId, out var existing) 
                        ? existing.LastNotificationTime 
                        : null
                };

                // Log current ATH data
                _logger.LogInformation(
                    "{CoinId} ATH: ${Ath:F2} on {AthDate:d} (Current: ${Current:F2}, {Percentage:F2}% from ATH)",
                    coinId,
                    athResponse.AllTimeHigh,
                    athResponse.AllTimeHighDate,
                    athResponse.CurrentPrice,
                    athResponse.PercentageFromAth);

                // Check if we have previous data
                if (_athData.TryGetValue(coinId, out var previousAth))
                {
                    // If ATH has changed and cooldown period has passed
                    if (Math.Abs(previousAth.AllTimeHigh - currentAth.AllTimeHigh) > 0.01m && // Using small epsilon for decimal comparison
                        (!currentAth.LastNotificationTime.HasValue || 
                         DateTime.UtcNow - currentAth.LastNotificationTime.Value >= _notificationCooldown))
                    {
                        var notification = new N8NNotification
                        {
                            Type = "CoinAthUpdate",
                            Content = JsonSerializer.Serialize(new
                            {
                                CoinId = coinId,
                                PreviousAth = previousAth.AllTimeHigh,
                                PreviousAthDate = previousAth.AllTimeHighDate,
                                NewAth = currentAth.AllTimeHigh,
                                NewAthDate = currentAth.AllTimeHighDate,
                                CurrentPrice = athResponse.CurrentPrice,
                                PercentageFromAth = athResponse.PercentageFromAth,
                                TimeSinceLastNotification = currentAth.LastNotificationTime.HasValue
                                    ? Math.Round((DateTime.UtcNow - currentAth.LastNotificationTime.Value).TotalMinutes, 1)
                                    : (double?)null
                            }),
                            Timestamp = DateTime.UtcNow
                        };

                        await _n8nService.SendNotificationAsync(notification);
                        _logger.LogInformation(
                            "Sent ATH update notification for {CoinId} (Last notification: {LastNotification} minutes ago)",
                            coinId,
                            currentAth.LastNotificationTime.HasValue
                                ? (DateTime.UtcNow - currentAth.LastNotificationTime.Value).TotalMinutes
                                : "never");

                        // Update last notification time
                        currentAth.LastNotificationTime = DateTime.UtcNow;
                    }
                    else if (Math.Abs(previousAth.AllTimeHigh - currentAth.AllTimeHigh) > 0.01m)
                    {
                        _logger.LogInformation(
                            "Skipping ATH notification for {CoinId} due to cooldown (Last notification: {LastNotification} minutes ago)",
                            coinId,
                            currentAth.LastNotificationTime.HasValue
                                ? (DateTime.UtcNow - currentAth.LastNotificationTime.Value).TotalMinutes
                                : "never");
                    }
                }

                // Update stored data
                _athData[coinId] = currentAth;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking ATH for {CoinId}", coinId);
            }
        }

        // Save updated data
        SaveAthData();
    }
} 