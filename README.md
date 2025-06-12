# Echelon Discord Bot

[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![Discord.Net](https://img.shields.io/badge/Discord.Net-3.17.4-blue.svg)](https://github.com/discord-net/Discord.Net)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

A powerful Discord bot for message parsing, automation integration, and Spotify playlist management. Built with .NET 9.0 and designed for seamless integration with N8N automation workflows.

## ✨ Features

### 🎵 Spotify Integration
- **Automatic Spotify URL Detection**: Monitors messages for Spotify track and album links
- **Track/Album Processing**: Extracts Spotify IDs and forwards them to automation workflows
- **Playlist Management**: Create and manage Spotify playlists through Discord commands
- **Duplicate Detection**: Find and manage duplicate tracks in channel playlists

### 🔗 N8N Automation Integration
- **Webhook Integration**: Sends structured notifications to N8N workflows
- **Server-Specific Endpoints**: Different N8N URLs per Discord server
- **Flexible Message Processing**: Handles both default messages and specialized Spotify content
- **Real-time Processing**: Instant forwarding of Discord events to automation systems

### 📝 Comprehensive Logging
- **Message Archiving**: Automatically logs all Discord messages to organized files
- **Server/Channel Organization**: Logs organized by server name and channel
- **Configurable Formatting**: Customizable timestamp formats and localization
- **Attachment Tracking**: Logs file attachments and their URLs

### ⚡ Slash Commands
- **Spotify Commands**:
  - `/create-playlist` - Create new Spotify playlists
  - `/find-duplicates` - Find duplicate tracks in playlists
  - `/verbose` - Toggle verbose mode for detailed logging

### 🛡️ Advanced Configuration
- **Channel Filtering**: Allow/restrict specific channels per server
- **Permission Management**: Role-based command access control
- **Owner Configuration**: Define server owners for special permissions
- **Flexible Deployment**: Guild-specific or global command registration

## 🚀 Quick Start

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Discord Bot Token
- N8N instance (optional but recommended)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-repo/echelon-bot.git
   cd echelon-bot/Echelon.Bot
   ```

2. **Configure the bot**
   ```bash
   cp "appsettings.sample.json" appsettings.json
   ```
   
   Edit `appsettings.json` with your configuration:
   ```json
   {
     "Discord": {
       "Token": "YOUR_DISCORD_BOT_TOKEN",
       "BotName": "YourBotName",
       "GuildIds": [ "123456789012345678" ]
     }
   }
   ```

3. **Install dependencies and run**
   ```bash
   dotnet restore
   dotnet run
   ```

### Docker Deployment

1. **Using Docker Compose (Recommended)**
   ```bash
   docker-compose up -d
   ```

2. **Using Docker directly**
   ```bash
   docker build -t echelon-bot .
   docker run -d --name echelon-bot \
     -v /var/log/echelon:/app/Logs \
     echelon-bot
   ```

## ⚙️ Configuration

### Basic Configuration

The bot uses `appsettings.json` for configuration. See `appsettings sample.json` for a complete example.

#### Discord Settings
```json
{
  "Discord": {
    "Token": "YOUR_DISCORD_BOT_TOKEN",
    "BotName": "Echelon",
    "GuildIds": [ "123456789012345678" ]
  }
}
```

#### Server-Specific Message Parsing
```json
{
  "Discord": {
    "SpotifyMessageParserService": {
      "YourServerName": {
        "N8NUrl": "http://your-n8n-instance.com/webhook/spotify",
        "AllowAllChannels": false,
        "Channels": [
          {
            "ChannelId": "123456789012345678",
            "ChannelName": "spotify"
          }
        ],
        "Owners": [
          {
            "OwnerId": "111111111111111111",
            "OwnerName": "YourUsername"
          }
        ]
      }
    }
  }
}
```

### Advanced Configuration

#### Logging Settings
```json
{
  "Logging": {
    "Settings": {
      "Directory": "/var/log/echelon",
      "Culture": "en-US",
      "DateTimeFormat": "yyyy-MM-dd HH:mm:ss",
      "UseLocalTime": true
    }
  }
}
```

#### Multiple Server Setup
The bot supports different configurations per Discord server, allowing:
- Different N8N webhook endpoints per server
- Server-specific channel restrictions
- Individual owner configurations

## 🏗️ Architecture

### Core Components

```
Echelon.Bot/
├── Services/                           # Core business logic
│   ├── DiscordService.cs               # Main Discord client service
│   ├── SlashCommandService.cs          # Slash command management
│   ├── LoggingService.cs               # Message logging
│   ├── BaseMessageParserService.cs     # Base parser functionality
│   ├── SpotifyMessageParserService.cs  # Spotify-specific parsing
│   ├── DefaultMessageParserService.cs  # General message parsing
│   └── N8NService.cs                   # N8N webhook integration
├── Modules/                            # Discord command modules
│   └── SpotifyModule.cs                # Spotify-related commands
├── Models/                             # Data models
├── Interfaces/                         # Service contracts
├── Factories/                          # Object creation
└── Constants/                          # Application constants
```

### Message Processing Flow

1. **Message Received** → Discord message event triggered
2. **Channel Validation** → Check if channel is allowed for processing
3. **Parser Selection** → Route to appropriate parser (Spotify/Default)
4. **Content Processing** → Extract relevant information
5. **N8N Integration** → Send structured data to automation workflows
6. **Logging** → Archive message to organized log files

## 🔧 Development

### Building from Source

```bash
# Clone repository
git clone https://github.com/your-repo/echelon-bot.git
cd echelon-bot/Echelon.Bot

# Restore dependencies
dotnet restore

# Build project
dotnet build

# Run tests (if available)
dotnet test

# Run in development mode
dotnet run --environment Development
```

### Project Structure

- **Program.cs**: Application entry point and dependency injection setup
- **Services/**: Core business logic and external integrations
- **Modules/**: Discord slash command implementations
- **Models/**: Data transfer objects and configuration models
- **Interfaces/**: Service contracts and abstractions

### Adding New Features

1. **Message Parsers**: Extend `BaseMessageParserService` for new message types
2. **Slash Commands**: Create new modules inheriting from `InteractionModuleBase`
3. **Integrations**: Implement new services following the existing patterns

## 📊 N8N Integration

The bot is designed to work seamlessly with N8N automation workflows. Each message type sends structured JSON data:

### Message Notification Structure
```json
{
  "serverName": "Discord Server Name",
  "id": "message-id",
  "type": "SpotifyTrack|SpotifyAlbum|Message|SlashCommand",
  "channel": "channel-name",
  "author": "username",
  "authorId": "user-id",
  "globalName": "User Display Name",
  "content": "message content or processed data",
  "channelId": "channel-id",
  "timestamp": "2023-01-01T00:00:00Z"
}
```

### Webhook Endpoints
- **Spotify Processing**: Dedicated endpoints for Spotify track/album handling
- **General Messages**: Default endpoint for all other message types
- **Command Notifications**: Slash command execution notifications

## 🚀 Deployment

### Production Deployment

1. **Environment Configuration**
   ```bash
   export DOTNET_ENVIRONMENT=Production
   export Logging__Settings__Directory=/var/log/echelon
   ```

2. **Service Configuration** (systemd example)
   ```ini
   [Unit]
   Description=Echelon Discord Bot
   After=network.target

   [Service]
   Type=notify
   ExecStart=/usr/bin/dotnet /opt/echelon-bot/Echelon.Bot.dll
   Restart=always
   User=echelon
   WorkingDirectory=/opt/echelon-bot

   [Install]
   WantedBy=multi-user.target
   ```

3. **Docker Production Setup**
   ```yaml
   services:
     discord-bot:
       image: echelon-bot:latest
       volumes:
         - /var/log/echelon:/app/Logs
         - ./appsettings.production.json:/app/appsettings.json:ro
       restart: unless-stopped
       environment:
         - DOTNET_ENVIRONMENT=Production
   ```

## 🔒 Security Considerations

- **Token Security**: Never commit Discord tokens to version control
- **Channel Restrictions**: Configure channel allowlists to prevent spam
- **Permission Management**: Use Discord role permissions for command access
- **Webhook Security**: Secure N8N endpoints with proper authentication

## 🐛 Troubleshooting

### Common Issues

**Bot not responding to commands**
- Verify bot has necessary Discord permissions
- Check if commands are registered (global vs guild-specific)
- Ensure `GuildIds` are correctly configured for faster registration

**Message parsing not working**
- Verify server name matches exactly in configuration
- Check channel allowlist configuration
- Confirm N8N webhook URLs are accessible

**Logging issues**
- Ensure log directory exists and is writable
- Check file permissions on the log directory
- Verify timestamp format configuration

### Debug Mode

Run with debug logging:
```bash
dotnet run --environment Development
```

Enable verbose logging in `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Echelon.Bot": "Debug"
    }
  }
}
```

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

**Built with ❤️ using .NET 9.0 and Discord.Net** 