namespace Echelon.Bot.Models;

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