namespace Echelon.Bot.Constants;

public static class DiscordConstants
{
    public static class Urls
    {
        public const string SpotifyTrackPrefix = "https://open.spotify.com/track/";
        public const string SpotifyPlaylistPrefix = "https://open.spotify.com/playlist/";
        public const string SpotifyAlbumPrefix = "https://open.spotify.com/album/";
        public const string SpotifyArtistPrefix = "https://open.spotify.com/artist/";
    }
    
    public static class ConfigSections
    {
        public const string Discord = "Discord";
        public const string LoggingSettings = "Logging:Settings";
        public const string SpotifyMessageParserService = "Discord:SpotifyMessageParserService";
        public const string DefaultMessageParserService = "Discord:DefaultMessageParserService";
    }
    
    public static class MessageTypes
    {
        public const string SpotifyTrack = "SpotifyTrack";
        public const string SpotifyAlbum = "SpotifyAlbum";
        public const string SpotifyRequest = "SpotifyRequest";
        public const string Default = "Default";
    }
}