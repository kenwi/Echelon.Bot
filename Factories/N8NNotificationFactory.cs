using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Echelon.Bot.Models;

namespace Echelon.Bot.Factories;

public class N8NNotificationFactory
{
    public static N8NNotification FromMessage(SocketMessage message, string messageType = "Message")
    {
        return new N8NNotification
        {
            ServerName = GetServerName(message),
            Id = message.Id.ToString(),
            Type = messageType,
            Channel = message.Channel.Name,
            Author = message.Author.Username,
            AuthorId = message.Author.Id.ToString(),
            GlobalName = message.Author.GlobalName ?? message.Author.Username,
            Content = message.Content,
            ChannelId = message.Channel.Id.ToString(),
            Attachments = string.Join(", ", message.Attachments.Select(a => a.Url)),
            Embeds = string.Join(", ", message.Embeds.Select(e => e.Type.ToString())),
            Mentions = string.Join(", ", message.MentionedUsers.Select(u => u.Username)),
            Timestamp = message.Timestamp.UtcDateTime
        };
    }

    public static N8NNotification FromInteractionContext(IInteractionContext context, string messageType = "SlashCommand", string? customContent = null)
    {
        var commandName = "";
        
        if (context.Interaction is ISlashCommandInteraction slashCommand)
        {
            commandName = slashCommand.Data.Name;
        }

        return new N8NNotification
        {
            ServerName = context.Guild?.Name ?? "Direct Message",
            Id = context.Interaction.Id.ToString(),
            Type = messageType,
            Channel = context.Channel.Name,
            Author = context.User.Username,
            AuthorId = context.User.Id.ToString(),
            GlobalName = context.User.GlobalName ?? context.User.Username,
            Content = customContent ?? $"/{commandName}",
            ChannelId = context.Channel.Id.ToString(),
            Attachments = "", // Slash commands don't have attachments
            Embeds = "", // Slash commands don't have embeds
            Mentions = "", // Slash commands don't have mentions
            Timestamp = context.Interaction.CreatedAt.UtcDateTime
        };
    }

    private static string GetServerName(SocketMessage message)
    {
        return message.Channel switch
        {
            SocketGuildChannel guildChannel => guildChannel.Guild.Name,
            _ => "Direct Message"
        };
    }

    public static string GetSafeDisplayName(IUser user)
    {
        return user.GlobalName ?? user.Username;
    }

    public static string GetSafeServerName(IInteractionContext context)
    {
        return context.Guild?.Name ?? "Direct Message";
    }
} 