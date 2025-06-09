using Discord.WebSocket;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Echelon.Bot.Services;

public class LoggingService
{
    private readonly string _baseLogDirectory;
    private readonly CultureInfo _culture;
    private readonly string _dateTimeFormat;
    private readonly bool _useLocalTime;

    public LoggingService(IConfiguration configuration)
    {
        // Get culture settings from configuration
        var cultureName = configuration["Logging:Settings:Culture"] ?? "nb-NO";
        _culture = new CultureInfo(cultureName);
        
        // Get datetime format from configuration
        _dateTimeFormat = configuration["Logging:Settings:DateTimeFormat"] ?? "dd.MM.yyyy HH:mm:ss";
        
        // Get time zone preference from configuration
        _useLocalTime = configuration.GetValue<bool>("Logging:Settings:UseLocalTime", true);

        // Get base directory from configuration and resolve full path
        var logDir = configuration["Logging:Settings:Directory"] ?? "Logs";
        _baseLogDirectory = Path.IsPathRooted(logDir) 
            ? logDir 
            : Path.Combine(AppContext.BaseDirectory, logDir);
            
        EnsureDirectoryExists(_baseLogDirectory);
    }

    public async Task LogMessageAsync(SocketMessage message)
    {
        if (message.Channel is not SocketGuildChannel guildChannel) return;

        var guild = guildChannel.Guild;
        var channel = message.Channel;
        
        // Create sanitized directory and file names
        var guildDirName = SanitizePath(guild.Name);
        var channelFileName = SanitizePath(channel.Name) + ".txt";
        
        // Ensure guild directory exists
        var guildPath = Path.Combine(_baseLogDirectory, guildDirName);
        EnsureDirectoryExists(guildPath);
        
        // Get the appropriate timestamp
        var timestamp = (_useLocalTime ? DateTime.Now : DateTime.UtcNow)
            .ToString(_dateTimeFormat, _culture);
            
        // Create the log entry
        var logEntry = $"[{timestamp}] <{message.Author.GlobalName}> {message.Content}";
        if (message.Attachments.Count > 0)
        {
            logEntry += $"\nVedlegg: {string.Join(", ", message.Attachments.Select(a => a.Url))}";
        }
        
        // Write to the channel's log file
        var logFilePath = Path.Combine(guildPath, channelFileName);
        await File.AppendAllTextAsync(logFilePath, logEntry + Environment.NewLine);
    }

    private void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    private string SanitizePath(string path)
    {
        // Remove invalid characters and replace spaces with underscores
        var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        var invalidRegEx = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
        return Regex.Replace(path, invalidRegEx, "_");
    }
} 