using System.Text.Json.Serialization;

namespace Echelon.Bot.Models;

public class CoinAthData
{
    [JsonPropertyName("coinId")]
    public string CoinId { get; set; } = string.Empty;

    [JsonPropertyName("allTimeHigh")]
    public decimal AllTimeHigh { get; set; }

    [JsonPropertyName("allTimeHighDate")]
    public DateTime AllTimeHighDate { get; set; }

    [JsonPropertyName("lastChecked")]
    public DateTime LastChecked { get; set; }

    [JsonPropertyName("lastNotificationTime")]
    public DateTime? LastNotificationTime { get; set; }
} 